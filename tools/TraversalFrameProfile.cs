using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Game;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

// Deterministic surface-relative camera/observer route; this isolates streaming
// from steering/AI, rather than claiming to be an input-driven flight playtest.
public partial class TraversalFrameProfile : Node3D
{
    private readonly record struct Sample(int Radius, string Phase, int Frame, double WallMs,
        double ProcessMs, double PhysicsMs, double RebuildMs, int Rebuilt, int Pending,
        int FineParents, int Tiles, int Meshes, int DrawCalls, int Objects, int Primitives,
        int Nodes, double StaticBytes, double RenderCpuMs, double RenderGpuMs,
        long AllocatedBytes, long HeapBytes, int Gen0, int Gen1, int Gen2,
        double GcPauseMs, int Resources, int RetiringCollisions,
        double WaitProcessMs, double RouteMs, double SceneMs, double BeforeDrawMs, double DrawMs,
        double MainThreadCpuMs, double ProcessCpuMs, int ManaBodies, double ManaLogicMs, double ManaIntegrationMs);
    // Tool-only Windows CPU accounting. Unsupported platforms report -1 rather
    // than treating unavailable CPU timing as evidence of an idle main thread.
    private static class ThreadClock
    {
        [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentThread();
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetThreadTimes(IntPtr thread, out long creation, out long exit, out long kernel, out long user);
        public static double Read() => OperatingSystem.IsWindows() &&
            GetThreadTimes(GetCurrentThread(), out _, out _, out long kernel, out long user)
            ? (kernel + user) / 10000.0 : -1;
    }
    private readonly record struct ManagedSnapshot(long Allocated, int Gen0, int Gen1, int Gen2, double PauseMs)
    {
        public static ManagedSnapshot Capture() => new(GC.GetTotalAllocatedBytes(false),
            GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2), GC.GetTotalPauseDuration().TotalMilliseconds);
    }
    private readonly List<Sample> _samples = new();
    private readonly Stopwatch _clock = new();
    private Game _game = null!;
    private HeightmapArena _arena = null!;
    private PlanetTerrain _terrain = null!;
    private CarpetFlightController _player = null!;
    private Camera3D _camera = null!;
    private Label _status = null!;
    private Vector3 _startUp;
    private readonly Vector3 _axis = new Vector3(1, 0, 1).Normalized();
    private int _radius, _rayChecks;
    private float _speed;
    private bool _unpaced;
    private bool _mixed;
    private int _mixedEdits, _expectedMana;
    private WorldManaTracker _tracker = null!;
    private long _sceneEnd, _preDraw;
    private readonly Process _process = Process.GetCurrentProcess();

    public override void _Process(double delta) => _sceneEnd = Stopwatch.GetTimestamp();
    private void BeforeDraw() => _preDraw = Stopwatch.GetTimestamp();
    private static double Milliseconds(long start, long end) => (end - start) * 1000.0 / Stopwatch.Frequency;

    public override async void _Ready()
    {
        int previousSize = PlanetSizes.SelectedIndex;
        var previousVsync = DisplayServer.WindowGetVsyncMode();
        int previousMaxFps = Engine.MaxFps;
        try
        {
            if (DisplayServer.GetName() == "headless")
                throw new InvalidOperationException("Traversal profiling requires a rendered display.");
            string[] args = OS.GetCmdlineUserArgs();
            _speed = args.Contains("--stress") ? 120 : 46;
            _mixed = args.Contains("--mixed");
            int[] sizes = args.Contains("--medium") ? new[] { 1 } : args.Contains("--xl") ? new[] { 3 }
                : args.Contains("--all") ? new[] { 0, 1, 2, 3 } : new[] { 1, 3 };
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            DisplayServer.WindowSetSize(new Vector2I(1280, 720));
            _unpaced = args.Contains("--unpaced");
            if (_unpaced)
            {
                DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
                Engine.MaxFps = 0;
            }
            RenderingServer.ViewportSetMeasureRenderTime(GetViewport().GetViewportRid(), true);
            ProcessPriority = int.MaxValue; // End-of-scene probe, after ordinary process callbacks.
            RenderingServer.FramePreDraw += BeforeDraw;
            var layer = new CanvasLayer { Layer = 100 };
            AddChild(layer);
            _status = new Label { Position = new Vector2(340, 24) };
            layer.AddChild(_status);
            foreach (int size in sizes) await RunWorld(size);
            WriteReports();
            GD.Print($"Traversal profile completed: {_samples.Count} samples, {_rayChecks} actual terrain collision probes.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            if (_samples.Count > 0) WriteReports();
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
        finally
        {
            PlanetSizes.SelectedIndex = previousSize;
            DisplayServer.WindowSetVsyncMode(previousVsync);
            Engine.MaxFps = previousMaxFps;
            RenderingServer.ViewportSetMeasureRenderTime(GetViewport().GetViewportRid(), false);
            RenderingServer.FramePreDraw -= BeforeDraw;
            _process.Dispose();
        }
    }

    private async Task RunWorld(int size)
    {
        PlanetSizes.SelectedIndex = size;
        _radius = PlanetSizes.SelectedRadius;
        GD.Seed(20261010);
        _game = new Game { UseSphericalWorld = true };
        AddChild(_game);
        _game.SetProcessUnhandledInput(false);
        _arena = (HeightmapArena)GetTree().GetFirstNodeInGroup("heightmap_arena");
        _terrain = _arena.GetChildren().OfType<PlanetTerrain>().Single();
        _player = (CarpetFlightController)GetTree().GetFirstNodeInGroup("player");
        _player.SetPhysicsProcess(false);
        _player.SetProcessUnhandledInput(false);
        foreach (Node node in _game.GetChildren())
            if (node is EnemyEcosystemDirector or LevelObjective) node.SetPhysicsProcess(false);
        foreach (string group in new[] { "ecosystem_enemy", "balloon", "loose_mana" })
            foreach (Node node in GetTree().GetNodesInGroup(group))
            {
                node.SetPhysicsProcess(false);
                if (node is RigidBody3D body) body.Freeze = true;
            }
        _startUp = _arena.UpAt(_player.GlobalPosition);
        _camera = new Camera3D { Far = 4000 };
        _game.AddChild(_camera);
        MoveTo(0);
        _camera.MakeCurrent();
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _arena.RefreshPlanetDetail();
        await Settle();
        for (int i = 0; i < 180; i++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        _mixedEdits = 0;
        if (_mixed)
        {
            _tracker = (WorldManaTracker)GetTree().GetFirstNodeInGroup("world_mana_tracker");
            _tracker.Refresh();
            _expectedMana = _tracker.TotalMana + 480;
            await Window("mixed-mana-create", 90, i =>
            {
                if (i != 0) return;
                for (int n = 0; n < 192; n++)
                    _game.AddChild(new ProfiledManaPickup
                    {
                        Position = _arena.OnSurface(_player.GlobalPosition + _player.GlobalBasis.X * ((n % 16 - 8) * 1.2f)
                            + _player.GlobalBasis.Z * ((n / 16 - 6) * 1.2f), 3),
                        ManaAmount = n % 4 + 1, Ownership = (ManaOwnership)(n % 3)
                    });
            });
            CheckMixedMana();
        }
        await Window("home-before", 120);
        (int FineParents, int BuiltTiles, int PublishedMeshes)? returnedHome = null;
        int frames = Mathf.CeilToInt(Mathf.Tau * _radius / (_speed / 60));
        for (int lap = 1; lap <= 2; lap++)
        {
            ulong revision = _arena.SurfaceRevision;
            await Window($"lap-{lap}", frames, i =>
            {
                MoveTo(Mathf.Tau * (i + 1) / frames);
                if (_mixed && i % 60 == 0)
                {
                    // Direct authoritative crater path isolates edit/streaming
                    // pressure from aiming and projectile travel variability.
                    _arena.ApplyCrater(_arena.OnSurface(_player.GlobalPosition - _player.GlobalBasis.Z * 16, 0));
                    _mixedEdits++;
                }
            });
            if (_mixed)
            {
                CheckMixedMana();
                if (_arena.SurfaceRevision <= revision || !_samples.Any(s => s.Radius == _radius && s.Phase == $"lap-{lap}" && s.ManaLogicMs > 0 && s.ManaIntegrationMs > 0))
                    throw new InvalidOperationException("Mixed traversal failed to exercise live mana and crater edits.");
            }
            if (!_samples.Any(s => s.Radius == _radius && s.Phase == $"lap-{lap}" && s.Rebuilt > 0))
                throw new InvalidOperationException("Traversal did not exercise LOD rebuilding.");
            MoveTo(0);
            _arena.RefreshPlanetDetail();
            await Settle();
            await Window($"home-after-{lap}", 120);
            var after = _terrain.GetDetailCounts();
            // Hysteresis can retain the arrival-side ring on the first return.
            // Identical laps must then return to the same bounded working set.
            if (returnedHome.HasValue && after != returnedHome.Value)
                throw new InvalidOperationException($"Terrain detail grew between identical laps: {returnedHome} -> {after}.");
            returnedHome = after;
        }
        if (_mixed) GD.Print($"Mixed traversal radius {_radius}: {_mixedEdits} craters, {_expectedMana} conserved world mana.");
        _game.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private void CheckMixedMana()
    {
        _tracker.Refresh();
        if (_tracker.TotalMana != _expectedMana)
            throw new InvalidOperationException($"Mixed traversal mana changed: {_expectedMana} -> {_tracker.TotalMana}.");
    }

    private void MoveTo(float angle)
    {
        var rotation = new Quaternion(_axis, angle);
        Vector3 up = rotation * _startUp;
        Vector3 forward = _axis.Cross(up).Normalized();
        Vector3 position = _arena.OnSurface(_arena.PlanetCenter + up * _radius, 10);
        _player.GlobalTransform = new Transform3D(PlanetGeometry.Frame(up, forward), position);
        _camera.GlobalTransform = new Transform3D(PlanetGeometry.Frame(up, forward), position);
        // Frame() keeps actor headings tangent; the view pitch is applied separately.
        _camera.RotateObjectLocal(Vector3.Right, -0.12f);
        // Unpaced rendering otherwise skips different observer positions between
        // the terrain's wall-time physics scans on each lap. Sample every route
        // step in this diagnostic mode so the identical-return guard stays useful.
        if (_unpaced) _arena.RefreshPlanetDetail();
    }

    private async Task Settle()
    {
        for (int i = 0; i < 2400; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            if (_terrain.PendingPatchCount == 0) return;
        }
        throw new InvalidOperationException("Terrain queue did not drain at a stationary observer.");
    }

    private async Task Window(string phase, int frames, Action<int>? action = null)
    {
        _status.Text = $"Traversal radius {_radius}: {phase}, {_speed} units/s at 60 route steps/s";
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        ManagedSnapshot previous = ManagedSnapshot.Capture();
        double previousCpu = _process.TotalProcessorTime.TotalMilliseconds;
        double previousThreadCpu = ThreadClock.Read();
        long start = Stopwatch.GetTimestamp();
        _clock.Restart();
        ProfiledManaPickup.ResetMeasurements();
        for (int i = 0; i < frames; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            long processStart = Stopwatch.GetTimestamp();
            action?.Invoke(i);
            long routeEnd = Stopwatch.GetTimestamp();
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            long end = Stopwatch.GetTimestamp();
            double wall = _clock.Elapsed.TotalMilliseconds;
            double cpu = _process.TotalProcessorTime.TotalMilliseconds;
            double threadCpu = ThreadClock.Read();
            if (!(start <= processStart && processStart <= routeEnd && routeEnd <= _sceneEnd &&
                _sceneEnd <= _preDraw && _preDraw <= end))
                throw new InvalidOperationException("Traversal phase probes are out of order.");
            ManagedSnapshot managed = ManagedSnapshot.Capture();
            var counts = _terrain.GetDetailCounts();
            _samples.Add(new Sample(_radius, phase, i, wall,
                Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000,
                Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000,
                _terrain.FrameRebuildMilliseconds, _terrain.FrameRebuiltPatchCount, _terrain.PendingPatchCount,
                counts.FineParents, counts.BuiltTiles, counts.PublishedMeshes,
                (int)Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame),
                (int)Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame),
                (int)Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame),
                (int)Performance.GetMonitor(Performance.Monitor.ObjectNodeCount),
                Performance.GetMonitor(Performance.Monitor.MemoryStatic),
                RenderingServer.ViewportGetMeasuredRenderTimeCpu(GetViewport().GetViewportRid()),
                RenderingServer.ViewportGetMeasuredRenderTimeGpu(GetViewport().GetViewportRid()),
                managed.Allocated - previous.Allocated, GC.GetTotalMemory(false),
                managed.Gen0 - previous.Gen0, managed.Gen1 - previous.Gen1, managed.Gen2 - previous.Gen2,
                managed.PauseMs - previous.PauseMs,
                (int)Performance.GetMonitor(Performance.Monitor.ObjectResourceCount),
                _terrain.RetiringCollisionCount,
                Milliseconds(start, processStart), Milliseconds(processStart, routeEnd),
                Milliseconds(routeEnd, _sceneEnd), Milliseconds(_sceneEnd, _preDraw), Milliseconds(_preDraw, end),
                threadCpu >= 0 && previousThreadCpu >= 0 ? threadCpu - previousThreadCpu : -1,
                cpu - previousCpu, GetTree().GetNodesInGroup("loose_mana").Count,
                ProfiledManaPickup.LogicMilliseconds, ProfiledManaPickup.IntegrationMilliseconds));
            if (i % 60 == 0) CheckCollision();
            if (_mixed && i % 60 == 0) CheckMixedMana();
            ProfiledManaPickup.ResetMeasurements();
            previous = ManagedSnapshot.Capture();
            previousCpu = _process.TotalProcessorTime.TotalMilliseconds;
            previousThreadCpu = ThreadClock.Read();
            start = Stopwatch.GetTimestamp();
            _clock.Restart(); // Diagnostic accounting/probes excluded from the next interval.
        }
        GD.Print(Summary(_samples.Where(s => s.Radius == _radius && s.Phase == phase).ToArray()));
    }

    private void CheckCollision()
    {
        Vector3 up = _arena.UpAt(_player.GlobalPosition);
        using var query = PhysicsRayQueryParameters3D.Create(_arena.PlanetCenter + up * (_radius + 50),
            _arena.PlanetCenter + up * (_radius - 30));
        var exclusions = new Godot.Collections.Array<Rid> { _player.GetRid() };
        // The probe verifies terrain coverage, not whichever live globe happens
        // to cross the radial ray above that terrain during mixed stress.
        foreach (Node actor in GetTree().GetNodesInGroup("loose_mana"))
            if (actor is ManaPickup orb && IsInstanceValid(orb) && !orb.IsQueuedForDeletion()) exclusions.Add(orb.GetRid());
        query.Exclude = exclusions;
        using var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0 || hit["collider"].AsGodotObject() is not Node node || !node.IsInGroup("deformable_terrain"))
            throw new InvalidOperationException($"Missing terrain collider during traversal at {up}; hit={hit}, pending={_terrain.PendingPatchCount}, rebuilt={_terrain.FrameRebuiltPatchCount}.");
        _rayChecks++;
    }

    private static string Summary(Sample[] samples)
    {
        double[] times = samples.Select(s => s.WallMs).OrderBy(t => t).ToArray();
        double P(double p) => times[Math.Clamp((int)Math.Ceiling(times.Length * p) - 1, 0, times.Length - 1)];
        return FormattableString.Invariant($"radius={samples[0].Radius} {samples[0].Phase}: n={times.Length}, p50/p95/p99/max={P(.5):F2}/{P(.95):F2}/{P(.99):F2}/{times[^1]:F2} ms; >33ms={times.Count(t => t > 33.333)}, >50ms={times.Count(t => t > 50)}; rebuildMax={samples.Max(s => s.RebuildMs):F2}ms, pendingMax={samples.Max(s => s.Pending)}, tilesMax={samples.Max(s => s.Tiles)}, meshesMax={samples.Max(s => s.Meshes)}, drawsMax={samples.Max(s => s.DrawCalls)}, nodesMax={samples.Max(s => s.Nodes)}, staticMaxMiB={samples.Max(s => s.StaticBytes) / 1048576:F1}; renderCpu/GpuMax={samples.Max(s => s.RenderCpuMs):F2}/{samples.Max(s => s.RenderGpuMs):F2}ms, allocatedMiB={samples.Sum(s => s.AllocatedBytes) / 1048576.0:F1}, heapMaxMiB={samples.Max(s => s.HeapBytes) / 1048576.0:F1}, GC0/1/2={samples.Sum(s => s.Gen0)}/{samples.Sum(s => s.Gen1)}/{samples.Sum(s => s.Gen2)}, gcPauseMax={samples.Max(s => s.GcPauseMs):F2}ms, resourcesMax={samples.Max(s => s.Resources)}");
    }

    private void WriteReports()
    {
        string directory = ProjectSettings.GlobalizePath("user://traversal-profiles");
        Directory.CreateDirectory(directory);
        string stem = Path.Combine(directory, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
        var csv = new StringBuilder("radius,phase,frame,wall_ms,process_ms,physics_ms,rebuild_ms,rebuilt,pending,fine_parents,built_tiles,published_meshes,draw_calls,render_objects,primitives,nodes,static_bytes,render_cpu_ms,render_gpu_ms,allocated_bytes,managed_heap_bytes,gc_gen0,gc_gen1,gc_gen2,gc_pause_ms,resources,retiring_collisions,wait_process_ms,route_ms,scene_ms,before_draw_ms,draw_ms,main_thread_cpu_ms,process_cpu_ms,mana_bodies,mana_logic_ms,mana_integration_ms\n");
        foreach (Sample s in _samples)
            csv.AppendLine(FormattableString.Invariant($"{s.Radius},{s.Phase},{s.Frame},{s.WallMs:F4},{s.ProcessMs:F4},{s.PhysicsMs:F4},{s.RebuildMs:F4},{s.Rebuilt},{s.Pending},{s.FineParents},{s.Tiles},{s.Meshes},{s.DrawCalls},{s.Objects},{s.Primitives},{s.Nodes},{s.StaticBytes:F0},{s.RenderCpuMs:F4},{s.RenderGpuMs:F4},{s.AllocatedBytes},{s.HeapBytes},{s.Gen0},{s.Gen1},{s.Gen2},{s.GcPauseMs:F4},{s.Resources},{s.RetiringCollisions},{s.WaitProcessMs:F4},{s.RouteMs:F4},{s.SceneMs:F4},{s.BeforeDrawMs:F4},{s.DrawMs:F4},{s.MainThreadCpuMs:F4},{s.ProcessCpuMs:F4},{s.ManaBodies},{s.ManaLogicMs:F4},{s.ManaIntegrationMs:F4}"));
        File.WriteAllText(stem + ".csv", csv.ToString());
        string header = $"UTC: {DateTime.UtcNow:O}\nGodot: {Engine.GetVersionInfo()["string"]}\nGPU: {RenderingServer.GetVideoAdapterName()}\nDisplay: {DisplayServer.GetName()}\nRenderer: {ProjectSettings.GetSetting("rendering/renderer/rendering_method")}\nViewport: {GetViewport().GetVisibleRect().Size}\nVSync: {DisplayServer.WindowGetVsyncMode()}\nMax FPS: {Engine.MaxFps}\nPhysics ticks: {Engine.PhysicsTicksPerSecond}\nSeed: 20261010\nRoute: two diagonal great-circle laps, {_speed} units/s at fixed 60 route steps/s. Slower rendering slows wall-clock travel. Actor observers fixed except player.\nWall intervals include pacing; diagnostic accounting excluded. Process/physics/render monitors may lag. Static memory is Godot's monitor. Published meshes count the whole terrain, not just visible draws.\n";
        header += "Attribution: viewport CPU/GPU timing is enabled, may lag and covers only viewport rendering (not pacing/presentation). GC counts/pause/allocation deltas cover the measured interval across managed threads, excluding diagnostic accounting; allocation counters are approximate. Heap is GC.GetTotalMemory(false), not native memory. Coincidence does not establish causality. Top wall frames include adjacent samples to expose lag.\n";
        if (_unpaced) header += "Unpaced diagnostic: VSync disabled, FPS unlimited, observer detail refreshed on every prescribed route step. This adds scan work and changes queue/physics timing; it is not a directly comparable gameplay-speed benchmark.\n";
        header += "Phases: wait_process=previous post-draw accounting end to ProcessFrame (includes pacing/physics/deferred cleanup); route=prescribed motion; scene=route end to last process-priority probe; before_draw=last process probe to FramePreDraw; draw=FramePreDraw to FramePostDraw. Ordering checked every sample. CPU is OS cumulative time, quantized, not wall time: Windows main thread, and all process threads (may exceed wall time); main-thread unavailable=-1. No forced render synchronization is inserted.\n";
        header += $"Mixed workload: {_mixed}. When enabled, 192 live stress globes (480 mana) at home, direct craters every 60 lap steps, baseline AI/hauling frozen. World mana checked every 60 samples and each lap. Mana timings cover only instrumented stress bodies across physics ticks per draw, exclude native solver work and overlap other phase timings. Stress globes stay at their physical locations while the observer travels.\n";
        string outliers = string.Join("\n", _samples.GroupBy(s => s.Radius).SelectMany(g =>
            g.OrderByDescending(s => s.WallMs).Take(12).Select(s =>
            {
                int index = _samples.IndexOf(s);
                return string.Join("\n", _samples.Skip(Math.Max(0, index - 1)).Take(3)
                    .Where(n => n.Radius == s.Radius && n.Phase == s.Phase)
                    .Select(n => FormattableString.Invariant($"{(n == s ? "OUTLIER" : "neighbor")} radius={n.Radius} {n.Phase}:{n.Frame} wall={n.WallMs:F2} process={n.ProcessMs:F2} physics={n.PhysicsMs:F2} rebuild={n.RebuildMs:F2} renderCPU/GPU={n.RenderCpuMs:F2}/{n.RenderGpuMs:F2} GC={n.Gen0}/{n.Gen1}/{n.Gen2} pause={n.GcPauseMs:F2} allocated={n.AllocatedBytes} retire={n.RetiringCollisions} resources={n.Resources} phases(wait/route/scene/pre/draw)={n.WaitProcessMs:F2}/{n.RouteMs:F2}/{n.SceneMs:F2}/{n.BeforeDrawMs:F2}/{n.DrawMs:F2} cpu(main/process)={n.MainThreadCpuMs:F2}/{n.ProcessCpuMs:F2}")));
            })));
        File.WriteAllText(stem + ".txt", header + string.Join("\n", _samples.GroupBy(s => (s.Radius, s.Phase)).Select(g => Summary(g.ToArray()))) + "\n\nLargest wall intervals with neighbors:\n" + outliers);
        GD.Print($"Reports: {stem}.csv and {stem}.txt");
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
        int Nodes, double StaticBytes);
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

    public override async void _Ready()
    {
        int previousSize = PlanetSizes.SelectedIndex;
        try
        {
            if (DisplayServer.GetName() == "headless")
                throw new InvalidOperationException("Traversal profiling requires a rendered display.");
            string[] args = OS.GetCmdlineUserArgs();
            _speed = args.Contains("--stress") ? 120 : 46;
            int[] sizes = args.Contains("--medium") ? new[] { 1 } : args.Contains("--xl") ? new[] { 3 }
                : args.Contains("--all") ? new[] { 0, 1, 2, 3 } : new[] { 1, 3 };
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            DisplayServer.WindowSetSize(new Vector2I(1280, 720));
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
        finally { PlanetSizes.SelectedIndex = previousSize; }
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
        await Window("home-before", 120);
        (int FineParents, int BuiltTiles, int PublishedMeshes)? returnedHome = null;
        int frames = Mathf.CeilToInt(Mathf.Tau * _radius / (_speed / 60));
        for (int lap = 1; lap <= 2; lap++)
        {
            await Window($"lap-{lap}", frames, i => MoveTo(Mathf.Tau * (i + 1) / frames));
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
        _game.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
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
        _clock.Restart();
        for (int i = 0; i < frames; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            action?.Invoke(i);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            double wall = _clock.Elapsed.TotalMilliseconds;
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
                Performance.GetMonitor(Performance.Monitor.MemoryStatic)));
            if (i % 60 == 0) CheckCollision();
            _clock.Restart(); // Diagnostic accounting/probes excluded from the next interval.
        }
        GD.Print(Summary(_samples.Where(s => s.Radius == _radius && s.Phase == phase).ToArray()));
    }

    private void CheckCollision()
    {
        Vector3 up = _arena.UpAt(_player.GlobalPosition);
        using var query = PhysicsRayQueryParameters3D.Create(_arena.PlanetCenter + up * (_radius + 50),
            _arena.PlanetCenter + up * (_radius - 30));
        query.Exclude = new Godot.Collections.Array<Rid> { _player.GetRid() };
        using var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0 || hit["collider"].AsGodotObject() is not Node node || !node.IsInGroup("deformable_terrain"))
            throw new InvalidOperationException($"Missing terrain collider during traversal at {up}; hit={hit}, pending={_terrain.PendingPatchCount}, rebuilt={_terrain.FrameRebuiltPatchCount}.");
        _rayChecks++;
    }

    private static string Summary(Sample[] samples)
    {
        double[] times = samples.Select(s => s.WallMs).OrderBy(t => t).ToArray();
        double P(double p) => times[Math.Clamp((int)Math.Ceiling(times.Length * p) - 1, 0, times.Length - 1)];
        return FormattableString.Invariant($"radius={samples[0].Radius} {samples[0].Phase}: n={times.Length}, p50/p95/p99/max={P(.5):F2}/{P(.95):F2}/{P(.99):F2}/{times[^1]:F2} ms; >33ms={times.Count(t => t > 33.333)}, >50ms={times.Count(t => t > 50)}; rebuildMax={samples.Max(s => s.RebuildMs):F2}ms, pendingMax={samples.Max(s => s.Pending)}, tilesMax={samples.Max(s => s.Tiles)}, meshesMax={samples.Max(s => s.Meshes)}, drawsMax={samples.Max(s => s.DrawCalls)}, nodesMax={samples.Max(s => s.Nodes)}, staticMaxMiB={samples.Max(s => s.StaticBytes) / 1048576:F1}");
    }

    private void WriteReports()
    {
        string directory = ProjectSettings.GlobalizePath("user://traversal-profiles");
        Directory.CreateDirectory(directory);
        string stem = Path.Combine(directory, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
        var csv = new StringBuilder("radius,phase,frame,wall_ms,process_ms,physics_ms,rebuild_ms,rebuilt,pending,fine_parents,built_tiles,published_meshes,draw_calls,render_objects,primitives,nodes,static_bytes\n");
        foreach (Sample s in _samples)
            csv.AppendLine(FormattableString.Invariant($"{s.Radius},{s.Phase},{s.Frame},{s.WallMs:F4},{s.ProcessMs:F4},{s.PhysicsMs:F4},{s.RebuildMs:F4},{s.Rebuilt},{s.Pending},{s.FineParents},{s.Tiles},{s.Meshes},{s.DrawCalls},{s.Objects},{s.Primitives},{s.Nodes},{s.StaticBytes:F0}"));
        File.WriteAllText(stem + ".csv", csv.ToString());
        string header = $"UTC: {DateTime.UtcNow:O}\nGodot: {Engine.GetVersionInfo()["string"]}\nGPU: {RenderingServer.GetVideoAdapterName()}\nDisplay: {DisplayServer.GetName()}\nRenderer: {ProjectSettings.GetSetting("rendering/renderer/rendering_method")}\nViewport: {GetViewport().GetVisibleRect().Size}\nVSync: {DisplayServer.WindowGetVsyncMode()}\nMax FPS: {Engine.MaxFps}\nPhysics ticks: {Engine.PhysicsTicksPerSecond}\nSeed: 20261010\nRoute: two diagonal great-circle laps, {_speed} units/s at fixed 60 route steps/s. Slower rendering slows wall-clock travel. Actor observers fixed except player.\nWall intervals include pacing; diagnostic accounting excluded. Process/physics/render monitors may lag; GPU time and managed heap are not measured. Static memory is Godot's monitor. Published meshes count the whole terrain, not just visible draws.\n";
        File.WriteAllText(stem + ".txt", header + string.Join("\n", _samples.GroupBy(s => (s.Radius, s.Phase)).Select(g => Summary(g.ToArray()))));
        GD.Print($"Reports: {stem}.csv and {stem}.txt");
    }
}

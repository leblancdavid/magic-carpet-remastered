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
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

// Automated workload: AI and hauling are held still, while real rendering,
// projectile physics, antimatter simulation, terrain queues, HUD and audio remain live.
public partial class CombatFrameProfile : Node3D
{
    private readonly record struct Sample(int Radius, string Phase, int Frame, double WallMs,
        double ProcessMs, double PhysicsMs, double RebuildMs, int Patches, int Pending, int AntimatterBodies,
        double EventMs, double AntimatterLogicMs, double AntimatterIntegrationMs, int LogicCalls, int IntegrationCalls,
        int ActiveBodies, int CollisionPairs, int Islands);
    private readonly List<Sample> _samples = new(5000);
    private readonly Stopwatch _clock = new();
    private Game _game = null!;
    private HeightmapArena _arena = null!;
    private PlanetTerrain _terrain = null!;
    private ShipFlightController _player = null!;
    private Camera3D _camera = null!;
    private Label _status = null!;
    private Vector3 _center;
    private int _radius;
    private int _claims;
    private bool _antimatterIsolation;

    public override async void _Ready()
    {
        int previousSize = PlanetSizes.SelectedIndex;
        try
        {
            if (DisplayServer.GetName() == "headless")
                throw new InvalidOperationException("Run CombatFrameProfile with a rendered display; headless timings are not comparable.");
            var args = OS.GetCmdlineUserArgs();
            _antimatterIsolation = args.Contains("--antimatter-isolation") || args.Contains("--mana-isolation");
            int[] sizes = args.Contains("--medium") ? new[] { 1 } : args.Contains("--xl") ? new[] { 3 } : new[] { 1, 3 };
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            DisplayServer.WindowSetSize(new Vector2I(1280, 720));
            var layer = new CanvasLayer { Layer = 100 };
            AddChild(layer);
            _status = new Label { Position = new Vector2(340, 24) };
            layer.AddChild(_status);
            foreach (int size in sizes) await RunWorld(size);
            WriteReports();
            GD.Print($"Combat frame profile completed: {_samples.Count} rendered samples; {_claims} successful claims.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
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
        _player = (ShipFlightController)GetTree().GetFirstNodeInGroup("player");
        _player.SetPhysicsProcess(false);
        _player.SetProcessUnhandledInput(false);
        foreach (Node child in _game.GetChildren())
            if (child is EnemyEcosystemDirector or LevelObjective) child.SetPhysicsProcess(false);
        foreach (string group in new[] { "ecosystem_enemy", "drone" })
            foreach (Node node in GetTree().GetNodesInGroup(group)) node.SetPhysicsProcess(false);
        _center = _arena.OnSurface(_player.GlobalPosition + _player.GlobalBasis.X * 35, 0);
        _camera = new Camera3D { Far = 4000 };
        _game.AddChild(_camera);
        _camera.GlobalPosition = _arena.OnSurface(_center + _player.GlobalBasis.Z * 22, 11);
        _camera.LookAt(_arena.OnSurface(_center, 2), _arena.UpAt(_center));
        _camera.MakeCurrent();
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _arena.RefreshPlanetDetail();
        _status.Text = $"Profiling radius {_radius}: warmup";
        for (int i = 0; i < 180; i++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        if (_antimatterIsolation)
        {
            await RunAntimatterIsolation();
            _game.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            return;
        }
        await Window("idle", 120);
        await Window("sky-first", 90, i => { if (i == 0) Fire(_arena.OnSurface(_center, 8), _arena.UpAt(_center)); });
        await Window("sky-repeat", 180, i => { if (i % 30 == 0) Fire(_arena.OnSurface(_center, 8), _arena.UpAt(_center)); });
        await Window("impact-first", 90, i => { if (i == 0) GroundImpact(0); });
        await Window("impact-repeat", 240, i => { if (i % 20 == 0) GroundImpact(i / 20); });
        if (!_samples.Any(s => s.Radius == _radius && s.Phase == "impact-first" && s.Patches > 0)
            || !_samples.Any(s => s.Radius == _radius && s.Phase == "impact-repeat" && s.Patches > 0))
            throw new InvalidOperationException("Ground-impact workload did not rebuild terrain.");
        await Window("death-first", 90, i => { if (i == 0) KillCreature(0); });
        await Window("death-repeat", 180, i => { if (i % 30 == 0) KillCreature(i / 30 + 1); });
        int previous = _claims;
        await Window("claim-first", 90, i => { if (i == 0) Claim(0); });
        await Window("claim-repeat", 180, i => { if (i % 30 == 0) Claim(i / 30 + 1); });
        if (_claims - previous != 7) throw new InvalidOperationException("Claim workload missed a target.");
        await Window("dense-antimatter-create", 90, i => { if (i == 0) CreateDenseAntimatter(); });
        await Window("dense-antimatter-active", 240);
        await Window("base-spill", 180, i =>
        {
            if (i != 0) return;
            var hub = new FactionBase { IsPlayerBase = false, Tier1StorageCapacity = 256, InitialStoredAntimatter = 256,
                Position = Point(12, 0, 2) };
            _game.AddChild(hub);
            hub.ApplyDamage(1000);
            if (!hub.IsDestroyed || hub.StoredAntimatter != 0) throw new InvalidOperationException("Base spill did not execute.");
        });
        await Window("sustained-edits", 360, i => { if (i % 12 == 0) GroundImpact(i / 12); });
        await Window("post-stress", 180);
        _clock.Stop();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _game.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private Vector3 Point(float x, float z, float clearance)
        => _arena.OnSurface(_center + _player.GlobalBasis.X * x + _player.GlobalBasis.Z * z, clearance);

    private void Fire(Vector3 position, Vector3 direction)
    {
        var bolt = new WeaponProjectile { Position = position };
        _game.AddChild(bolt);
        bolt.Launch(direction, _player);
    }

    private void GroundImpact(int index)
    {
        Vector3 point = Point((index % 5 - 2) * 3, (index / 5 % 5 - 2) * 3, 4);
        Fire(point, -_arena.UpAt(point));
    }

    private void KillCreature(int index)
    {
        var creature = new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.FlyingSwarm,
            Position = Point((index % 4 - 2) * 4, -8, 3) };
        _game.AddChild(creature);
        creature.SetPhysicsProcess(false);
        creature.ApplyDamage(1000);
        if (!creature.IsQueuedForDeletion()) throw new InvalidOperationException("Creature death workload did not execute.");
    }

    private void Claim(int index)
    {
        // Separate from death loot so first-hit targeting is repeatable.
        var target = new AntimatterPickup { Position = Point(-18 + index * 3, 15, 0.5f), AntimatterAmount = 2 };
        _game.AddChild(target);
        target.Freeze = true;
        target.SetPhysicsProcess(false);
        var bolt = new ClaimProjectile { Position = target.GlobalPosition + _arena.UpAt(target.GlobalPosition) * 3 };
        _game.AddChild(bolt);
        bolt.AntimatterClaimed += _ => { _claims++; target.QueueFree(); };
        bolt.Launch(-_arena.UpAt(target.GlobalPosition));
    }

    private void CreateDenseAntimatter()
    {
        for (int i = 0; i < 192; i++)
            _game.AddChild(new ProfiledAntimatterPickup { Position = Point((i % 16 - 8) * 1.2f, (i / 16 - 6) * 1.2f, 3),
                AntimatterAmount = i % 4 + 1, Ownership = (AntimatterOwnership)(i % 3) });
    }

    private async Task RunAntimatterIsolation()
    {
        // Remove seeded loose resources so every case starts with the same population.
        foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter")) node.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await Window("antimatter-isolation-idle", 120);
        foreach (string mode in new[] { "frozen", "solver-radial", "full" })
        {
            var bodies = new List<ProfiledAntimatterPickup>();
            GD.Seed(20261010);
            await Window($"antimatter-{mode}-create", 90, i =>
            {
                if (i != 0) return;
                for (int index = 0; index < 192; index++)
                {
                    var orb = new ProfiledAntimatterPickup
                    {
                        Position = Point((index % 16 - 8) * 1.2f, (index / 16 - 6) * 1.2f, 3),
                        AntimatterAmount = index % 4 + 1,
                        Ownership = (AntimatterOwnership)(index % 3),
                        Freeze = mode == "frozen"
                    };
                    _game.AddChild(orb);
                    // Bypass initial grounding in every case, preserving an identical falling field.
                    orb.LaunchScatter(orb.Position, Vector3.Zero);
                    orb.Freeze = mode == "frozen";
                    orb.SetPhysicsProcess(mode == "full");
                    bodies.Add(orb);
                }
            });
            await Window($"antimatter-{mode}-active", 360);
            var measurements = _samples.Where(s => s.Radius == _radius && s.Phase == $"antimatter-{mode}-active").ToArray();
            if ((mode == "full") != measurements.Any(s => s.LogicCalls > 0)
                || (mode != "frozen") != measurements.Any(s => s.IntegrationCalls > 0))
                throw new InvalidOperationException($"Antimatter isolation callback gates failed for {mode}.");
            int total = bodies.Where(IsInstanceValid).Sum(orb => orb.AntimatterAmount);
            if (total != 480) throw new InvalidOperationException($"Antimatter isolation lost resources in {mode}: {total}.");
            foreach (var orb in bodies)
                if (IsInstanceValid(orb) && !orb.IsQueuedForDeletion()) orb.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await Window($"antimatter-{mode}-recovery", 60);
        }
    }

    private async Task Window(string phase, int frames, Action<int>? action = null)
    {
        _status.Text = $"Profiling radius {_radius}: {phase}";
        // Establish a draw boundary outside the sample; event work below is included.
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        _clock.Restart();
        ProfiledAntimatterPickup.ResetMeasurements();
        for (int i = 0; i < frames; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            long eventStart = Stopwatch.GetTimestamp();
            action?.Invoke(i);
            double eventMs = action == null ? 0 : Stopwatch.GetElapsedTime(eventStart).TotalMilliseconds;
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            double ms = _clock.Elapsed.TotalMilliseconds;
            _clock.Restart();
            _samples.Add(new Sample(_radius, phase, i, ms,
                Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000,
                Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000,
                _terrain.FrameRebuildMilliseconds, _terrain.FrameRebuiltPatchCount,
                _terrain.PendingPatchCount, GetTree().GetNodesInGroup("loose_antimatter").Count,
                eventMs, ProfiledAntimatterPickup.LogicMilliseconds, ProfiledAntimatterPickup.IntegrationMilliseconds,
                ProfiledAntimatterPickup.LogicCalls, ProfiledAntimatterPickup.IntegrationCalls,
                (int)Performance.GetMonitor(Performance.Monitor.Physics3DActiveObjects),
                (int)Performance.GetMonitor(Performance.Monitor.Physics3DCollisionPairs),
                (int)Performance.GetMonitor(Performance.Monitor.Physics3DIslandCount)));
            ProfiledAntimatterPickup.ResetMeasurements();
        }
        var window = _samples.Where(s => s.Radius == _radius && s.Phase == phase).ToArray();
        GD.Print(Summary(window));
    }

    private static string Summary(Sample[] samples)
    {
        double[] times = samples.Select(s => s.WallMs).OrderBy(v => v).ToArray();
        double Percentile(double p) => times[Math.Clamp((int)Math.Ceiling(times.Length * p) - 1, 0, times.Length - 1)];
        return FormattableString.Invariant($"radius={samples[0].Radius} {samples[0].Phase}: n={times.Length}, p50={Percentile(.5):F2}, p95={Percentile(.95):F2}, p99={Percentile(.99):F2}, max={times[^1]:F2} ms; >33ms={times.Count(t => t > 33.333)}, >50ms={times.Count(t => t > 50)}; rebuildMax={samples.Max(s => s.RebuildMs):F2}ms, backlogMax={samples.Max(s => s.Pending)}, bodiesMax={samples.Max(s => s.AntimatterBodies)}; eventMax={samples.Max(s => s.EventMs):F2}ms, logicMax={samples.Max(s => s.AntimatterLogicMs):F2}ms, radialMax={samples.Max(s => s.AntimatterIntegrationMs):F2}ms, pairsMax={samples.Max(s => s.CollisionPairs)}");
    }

    private void WriteReports()
    {
        string directory = ProjectSettings.GlobalizePath("user://combat-profiles");
        Directory.CreateDirectory(directory);
        string stem = Path.Combine(directory, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
        var csv = new StringBuilder("radius,phase,frame,wall_ms,process_ms,physics_ms,rebuild_ms,rebuilt_patches,pending_patches,antimatter_bodies,event_ms,antimatter_logic_ms,antimatter_integration_ms,logic_calls,integration_calls,active_bodies,collision_pairs,islands\n");
        foreach (Sample s in _samples)
            csv.AppendLine(FormattableString.Invariant($"{s.Radius},{s.Phase},{s.Frame},{s.WallMs:F4},{s.ProcessMs:F4},{s.PhysicsMs:F4},{s.RebuildMs:F4},{s.Patches},{s.Pending},{s.AntimatterBodies},{s.EventMs:F4},{s.AntimatterLogicMs:F4},{s.AntimatterIntegrationMs:F4},{s.LogicCalls},{s.IntegrationCalls},{s.ActiveBodies},{s.CollisionPairs},{s.Islands}"));
        File.WriteAllText(stem + ".csv", csv.ToString());
        string header = $"UTC: {DateTime.UtcNow:O}\nGodot: {Engine.GetVersionInfo()["string"]}\nDisplay: {DisplayServer.GetName()}\nRenderer: {ProjectSettings.GetSetting("rendering/renderer/rendering_method")}\nGPU: {RenderingServer.GetVideoAdapterName()}\nViewport: {GetViewport().GetVisibleRect().Size}\nVSync: {DisplayServer.WindowGetVsyncMode()}\nMax FPS: {Engine.MaxFps}\nPhysics ticks: {Engine.PhysicsTicksPerSecond}\nSeed: 20261010 per world\nAntimatter isolation: {_antimatterIsolation}\nWall time is post-draw interval, including frame pacing; process/physics monitors may lag. GPU execution time is not measured.\nAntimatter callback timings include only the 192 instrumented stress bodies (summed across physics ticks per draw); integration is managed radial/support work, not native solver time. Event time measures synchronous injection; timings overlap and must not be added.\n";
        string summary = header + string.Join("\n", _samples.GroupBy(s => (s.Radius, s.Phase)).Select(g => Summary(g.ToArray())));
        File.WriteAllText(stem + ".txt", summary);
        GD.Print($"Reports: {stem}.csv and {stem}.txt");
    }
}

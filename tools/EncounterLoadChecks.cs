using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Game;
using MagicCarpetRemastered.Scripts.World;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;

namespace MagicCarpetRemastered.Tools;

// Live population smoke/profile: actors, rival, collection, capture and UI all
// run normally. Timings are observations, not a hardware-independent FPS gate.
public partial class EncounterLoadChecks : Node3D
{
    private int _checks;
    private void Check(bool value, string message)
    { _checks++; if (!value) throw new InvalidOperationException(message); }
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();
    private async Task Run()
    {
        int previous = PlanetSizes.SelectedIndex;
        try
        {
            foreach (int size in OS.GetCmdlineUserArgs().Contains("--medium") ? new[] { 1 }
                : OS.GetCmdlineUserArgs().Contains("--xl") ? new[] { 3 } : new[] { 1, 3 })
            {
                PlanetSizes.SelectedIndex = size; WorldSeeds.UseSeed(7319);
                var game = GD.Load<PackedScene>("res://scenes/PlanetGame.tscn").Instantiate<Game>(); AddChild(game);
                var arena = game.GetNode<HeightmapArena>("HeightmapArena");
                var ship = game.GetNode<ShipFlightController>("PlayerShip");
                var director = game.GetNode<EnemyEcosystemDirector>("EnemyEcosystemDirector");
                var initial = game.GetChildren().OfType<EnemyEcosystemEnemy>().ToArray();
                Check(initial.Length == director.InitialWaveCount, "Seeded dense world reaches its population target");
                var heavy = initial.First(e => e.IsHeavy);
                // Watch a heavy encounter through the normal player camera,
                // leaving damage, pilot deaths and objective respawns live.
                ship.RespawnAt(arena.OnSurface(heavy.GlobalPosition + heavy.GlobalBasis.X * 32, 2));
                arena.Orient(ship, arena.DirectionTo(ship.GlobalPosition, heavy.GlobalPosition));
                ship._PhysicsProcess(0); arena.RefreshPlanetDetail();
                for (int i = 0; i < 120; i++) await Frame();
                var wall = new double[480]; var process = new double[480]; var physics = new double[480];
                var clock = Stopwatch.StartNew(); long allocated = GC.GetTotalAllocatedBytes();
                for (int i = 0; i < wall.Length; i++)
                {
                    await Frame(); wall[i] = clock.Elapsed.TotalMilliseconds; clock.Restart();
                    process[i] = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000;
                    physics[i] = Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000;
                }
                Check(ship.GlobalPosition.IsFinite() && ship.Velocity.IsFinite(), "Live dense-world ship remains finite");
                Check(game.GetChildren().OfType<EnemyEcosystemEnemy>().Count(e => !e.IsRaider && e.Health > 0) <= director.MaxActiveEnemies,
                    "Live replacements preserve the population ceiling");
                Check(game.GetChildren().OfType<AntimatterPickup>().All(p => p.AntimatterAmount >= 0), "Combat drops retain nonnegative resource amounts");
                Array.Sort(wall); Array.Sort(process); Array.Sort(physics);
                GD.Print($"Encounter load R{arena.PlanetRadius}: {initial.Length} initial / {director.HabitatCount} habitats; "
                    + $"{wall.Length} samples ({DisplayServer.GetName()}), wall p95/max {wall[455]:0.00}/{wall[^1]:0.00} ms; "
                    + $"process p95/max {process[455]:0.00}/{process[^1]:0.00}, physics {physics[455]:0.00}/{physics[^1]:0.00} ms; "
                    + $"allocated {(GC.GetTotalAllocatedBytes() - allocated) / 1048576.0:0.0} MiB");
                if (DisplayServer.GetName() != "headless")
                {
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    using var image = GetViewport().GetTexture().GetImage();
                    Check(image.SavePng($"C:/Users/lblan/AppData/Local/Temp/opencode/EncounterLoad{arena.PlanetRadius}.png") == Error.Ok,
                        "Dense-world rendered capture saves");
                }
                game.QueueFree(); for (int i = 0; i < 12; i++) await Frame();
            }
            PlanetSizes.SelectedIndex = previous;
            GD.Print($"Encounter load checks passed: {_checks}"); GetTree().Quit();
        }
        catch (Exception error) { PlanetSizes.SelectedIndex = previous; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}

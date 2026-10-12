using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Ui;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class PlanetLayoutChecks : Node3D
{
    private int _checks;
    private void Check(bool value, string message)
    { _checks++; if (!value) throw new InvalidOperationException(message); }
    private async Task Frames(int count)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    public override void _Ready() => Callable.From(() => { _ = RunChecks(); }).CallDeferred();

    private void CheckGeometry(HeightmapArena arena, PlanetLayout layout)
    {
        float Distance(Vector3 a, Vector3 b) => arena.SurfaceDistance(PlanetLayout.Position(arena, a), PlanetLayout.Position(arena, b));
        Check(Distance(layout.Starts[0], layout.Starts[1]) + 1 >= layout.MinimumStartDistance,
            "All start pairs obey configurable minimum surface separation");
        Check(Mathf.Abs(Distance(layout.Starts[0], layout.Starts[1]) - Mathf.Pi * arena.PlanetRadius) < 1,
            "Two factions start on opposite sides, not one projected patch");
        Check(layout.Landmarks.Length == 5 && layout.EconomicSites.Length == 8 && layout.MatterFields.Length == 4,
            "Layout retains authored landmark/site/field populations");
        var directions = layout.Landmarks.Select(s => s.Up).Concat(layout.EconomicSites.Select(s => s.Up))
            .Concat(layout.MatterFields).Concat(layout.AntimatterFields).Concat(layout.Scenery).ToArray();
        Check(directions.All(up => up.IsFinite() && Mathf.Abs(up.Length() - 1) < 0.0001f), "All content uses finite unit surface directions");
        Check(directions.Select(PlanetLayout.RegionFor).Distinct().Count() >= 10, "Authored content reaches at least ten of twelve planetary regions");
        Check(layout.Scenery.Length >= 24 && layout.Scenery.Length <= 36, "Scenery remains bounded and globally clustered");
        Check(layout.Scenery.Select(PlanetLayout.RegionFor).Distinct().Count() >= 8, "Scenery occupies both hemispheres and multiple remote regions");
        foreach (Vector3 up in layout.Scenery)
        {
            Check(layout.Starts.All(start => Distance(start, up) >= 27.9f), "Scenery leaves both base approaches clear");
            Check(layout.EconomicSites.All(site => Distance(site.Up, up) >= Mathf.Clamp(arena.PlanetRadius * 0.06f, 8, 18) + 4.9f), "Scenery does not obstruct station capture rings");
        }
        foreach (Vector3 start in layout.Starts)
            Check(layout.Landmarks.All(site => Distance(start, site.Up) > arena.PlanetRadius * 0.85f), "Stable habitats are separated from faction starts");
        for (int role = 0; role < 4; role++)
        {
            var a = layout.EconomicSites[role]; var b = layout.EconomicSites[4 + role];
            float first = Distance(layout.Starts[0], a.Up), second = Distance(layout.Starts[1], b.Up);
            Check(a.Role == b.Role && Mathf.Abs(first - second) < 1 && first > 30,
                "Role-matched capture opportunities have equal surface travel and are outside home bases");
        }
        for (int side = 0; side < 2; side++)
        {
            Check(Mathf.Abs(Distance(layout.Starts[side], layout.AntimatterFields[side]) - 55) < 0.2f,
                "Each opening antimatter field is 55 surface units from home, independent of planet size");
            Check(Mathf.Abs(Distance(layout.Starts[side], layout.MatterFields[side * 2]) - 45) < 0.2f,
                "Each opening material field is 45 surface units from home");
            Check(Distance(layout.Starts[side], layout.MatterFields[side * 2 + 1]) > arena.PlanetRadius,
                "Remote material provides a genuine exploration route");
        }
    }

    private async Task RunChecks()
    {
        try
        {
            foreach (int radius in PlanetSizes.Radii)
            {
                var arena = new HeightmapArena { IsSpherical = true, PlanetRadius = radius }; AddChild(arena);
                foreach (int seed in new[] { 7319, 17, 901, -42 })
                {
                    var layout = PlanetLayout.Create(arena, seed);
                    CheckGeometry(arena, layout);
                    var same = PlanetLayout.Create(arena, seed);
                    Check(layout.Starts.SequenceEqual(same.Starts) && layout.Scenery.SequenceEqual(same.Scenery),
                        "Seeded generation is repeatable without consuming global gameplay randomness");
                    GD.Print($"Layout {radius}, seed {seed}: dry starts {layout.StartsOnDryGround}, scenery {layout.Scenery.Length}");
                }
                CheckGeometry(arena, PlanetLayout.Create(arena, 17, 180));
                bool rejected = false;
                try { PlanetLayout.Create(arena, 17, 181); } catch (ArgumentOutOfRangeException) { rejected = true; }
                Check(rejected, "Impossible minimum cannot silently fall back to nearby starts");
                arena.QueueFree(); await Frames(4);
            }

            foreach (int radius in DisplayServer.GetName() == "headless" ? PlanetSizes.Radii : new[] { 100, 200 })
            {
                PlanetSizes.SelectedIndex = Array.IndexOf(PlanetSizes.Radii, radius);
                var game = GD.Load<PackedScene>("res://scenes/PlanetGame.tscn").Instantiate<MagicCarpetRemastered.Scripts.Game.Game>();
                AddChild(game);
                var arena = game.GetNode<HeightmapArena>("HeightmapArena");
                var home = game.GetNode<FactionBase>("PlayerBase");
                var rival = game.GetNode<FactionBase>("EnemyBase");
                var ship = game.GetNode<ShipFlightController>("PlayerShip");
                ship.SetPhysicsProcess(false);
                game.GetNode<MagicCarpetRemastered.Scripts.Game.EnemyEcosystemDirector>("EnemyEcosystemDirector").SetPhysicsProcess(false);
                foreach (var enemy in game.GetChildren().OfType<EnemyEcosystemEnemy>()) enemy.SetPhysicsProcess(false);
                var drones = game.GetChildren().OfType<CollectorDrone>().ToArray();
                foreach (var drone in drones) drone.SetPhysicsProcess(false);
                Check(arena.SurfaceDistance(home.GlobalPosition, rival.GlobalPosition) >= game.Layout!.MinimumStartDistance,
                    "Live bases use the coordinated layout");
                Check(home.StoredAntimatter == 30 && rival.StoredAntimatter == 30 && home.StoredMatter == rival.StoredMatter,
                    "Live factions have equal starting reserves");
                Check(arena.SurfaceDistance(ship.GlobalPosition, home.GlobalPosition) < 18 && ship.CanInstallEquipment,
                    "Player spawns beside home with immediate installation access");
                var pilot = game.GetNode<ShipFlightController>("RivalPilot"); pilot.SetPhysicsProcess(false);
                Check(arena.SurfaceDistance(pilot.GlobalPosition, rival.GlobalPosition) < 18,
                    "Rival spawns beside its separated home");
                Check(drones.All(d => arena.SurfaceDistance(d.GlobalPosition, d.UsePlayerAntimatter ? home.GlobalPosition : rival.GlobalPosition) < 18),
                    "Both home collectors spawn relative to the actual bases");
                var orbs = game.GetChildren().OfType<AntimatterPickup>().ToArray();
                Check(orbs.Length == 18 && orbs.Sum(p => p.AntimatterAmount) == 204, "Layout preserves initial loose antimatter supply");
                foreach (FactionBase hub in new[] { home, rival })
                    Check(orbs.Where(o => arena.SurfaceDistance(hub.GlobalPosition, o.GlobalPosition) < 70).Sum(o => o.AntimatterAmount) == 48,
                        "Each home has the same four-globe opening field");
                Check(game.GetChildren().OfType<MatterPickup>().Sum(p => p.MatterAmount) == 240, "Layout preserves all four 60-matter fields");
                var wrecks = game.GetChildren().OfType<ExplorationWreck>().ToArray();
                var director = game.GetNode<ExplorationDirector>("ExplorationDirector");
                Check(wrecks.Length == 12 && wrecks.All(w => arena.SurfaceDistance(home.GlobalPosition, w.GlobalPosition) >= director.BaseExclusionDistance
                    && arena.SurfaceDistance(rival.GlobalPosition, w.GlobalPosition) >= director.BaseExclusionDistance), "Initial wrecks obey population/base exclusions");
                Check(wrecks.All(w => wrecks.All(other => other == w || arena.SurfaceDistance(w.GlobalPosition, other.GlobalPosition) >= director.MinimumSpacing)),
                    "Globally distributed wrecks retain minimum mutual spacing");
                Check(wrecks.Select(w => PlanetLayout.RegionFor(arena.UpAt(w.GlobalPosition))).Distinct().Count() >= 9,
                    "Initial wrecks cover planetary regions rather than only balancing nearest-base totals");
                foreach (var obstacle in game.GetChildren().OfType<StaticBody3D>().Where(p => p.IsInGroup("scenery")))
                    Check(Mathf.Abs(arena.Clearance(obstacle.GlobalPosition) - 1.5f) < 0.01f && obstacle.GlobalBasis.Y.Dot(arena.UpAt(obstacle.GlobalPosition)) > 0.999f,
                        "Scenery remains terrain/water grounded and radially upright");
                if (DisplayServer.GetName() != "headless")
                {
                    var map = game.GetNode<TacticalMap>("TacticalMap");
                    await Frames(12); await Capture($"PlanetLayoutGameplay{radius}.png");
                    ship._UnhandledInput(new InputEventAction { Action = "toggle_camera", Pressed = true });
                    ship.SetPhysicsProcess(true); await Frames(90); ship.SetPhysicsProcess(false);
                    await Capture($"PlanetLayoutChase{radius}.png");
                    map.SetExpanded(true); await Frames(4); await Capture($"PlanetLayoutSurvey{radius}.png"); map.SetExpanded(false);
                }
                game.QueueFree(); await Frames(10);
            }
            PlanetSizes.SelectedIndex = 1;
            GD.Print($"Planet layout checks passed: {_checks}"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    private async Task Capture(string filename)
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng($"C:/Users/lblan/AppData/Local/Temp/opencode/{filename}") == Error.Ok,
            "Rendered layout capture saves successfully");
    }
}

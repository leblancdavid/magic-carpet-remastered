using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Game;
using MagicCarpetRemastered.Scripts.World;
using MagicCarpetRemastered.Scripts.Enemies;

namespace MagicCarpetRemastered.Tools;

public partial class WorldSeedChecks : Node3D
{
    private int _checks;
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();
    private void Check(bool value, string message) { _checks++; if (!value) throw new Exception(message); }
    private async Task Frames(int count)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    private static float[] Heights(HeightmapArena arena) => Enumerable.Range(0, 64).Select(i =>
    {
        float y = 1 - 2 * (i + 0.5f) / 64, a = i * 2.39996323f, r = Mathf.Sqrt(1 - y * y);
        return arena.HeightAt(arena.PlanetCenter + new Vector3(r * Mathf.Cos(a), y, r * Mathf.Sin(a)) * arena.PlanetRadius);
    }).ToArray();

    private async Task Run()
    {
        try
        {
            WorldSeeds.UseSeed(0); Check(WorldSeeds.Current == 0, "Zero is a valid replay seed");
            int next = WorldSeeds.NewWorld(); Check(next != 0 && WorldSeeds.Current == next, "Fresh world selection persists and differs from current seed");
            Check(WorldSeeds.Derive(17, 1) == WorldSeeds.Derive(17, 1) && WorldSeeds.Derive(17, 1) != WorldSeeds.Derive(17, 2), "Subsystem streams are stable and independent");
            foreach (int radius in PlanetSizes.Radii)
            {
                float[]? first = null;
                foreach (int seed in new[] { 7319, 17, 901, -42 })
                {
                    var arena = new HeightmapArena { IsSpherical = true, PlanetRadius = radius, GenerateSeededLayout = true, WorldSeed = seed };
                    AddChild(arena);
                    GD.Print($"World {seed}, R{radius}: attempts {arena.GenerationAttempts}, terrain {arena.TerrainSeed}, error {arena.GenerationError}");
                    Check(arena.GeneratedLayout != null && arena.GenerationAttempts >= 1 && arena.GenerationAttempts <= 16,
                        "Random geography has bounded validated generation");
                    Check(PlanetGenerationValidation.Accept(arena, arena.GeneratedLayout!, out _), "Accepted world meets land/slope and opening approach requirements");
                    CheckIndependentBudget(arena);
                    var heights = Heights(arena);
                    if (first == null) first = heights;
                    else Check(!first.SequenceEqual(heights), "Different seeds change actual terrain, not only object placement");
                    if (seed == 7319)
                    {
                        var starts = arena.GeneratedLayout!.Starts.ToArray(); int accepted = arena.TerrainSeed;
                        arena.QueueFree(); await Frames(4);
                        arena = new HeightmapArena { IsSpherical = true, PlanetRadius = radius, GenerateSeededLayout = true, WorldSeed = seed };
                        AddChild(arena);
                        Check(heights.SequenceEqual(Heights(arena)) && starts.SequenceEqual(arena.GeneratedLayout!.Starts) && accepted == arena.TerrainSeed,
                            "Same seed/size reproduce geography, starts and accepted candidate");
                    }
                    arena.QueueFree(); await Frames(4);
                }
            }
            PlanetSizes.SelectedIndex = 1; WorldSeeds.UseSeed(7319);
            var scene = GD.Load<PackedScene>("res://scenes/PlanetGame.tscn");
            Vector3[]? wrecks = null, mobs = null; int[]? roles = null; float[]? terrain = null;
            for (int repeat = 0; repeat < 2; repeat++)
            {
                var game = scene.Instantiate<Game>(); AddChild(game);
                Check(game.GenerationSucceeded && game.ActiveWorldSeed == 7319, "Scene reload initialization retains the selected seed");
                game.ProcessMode = ProcessModeEnum.Disabled;
                var arena = game.GetNode<HeightmapArena>("HeightmapArena");
                var w = game.GetChildren().OfType<ExplorationWreck>().Select(n => n.GlobalPosition).ToArray();
                var m = game.GetChildren().OfType<EnemyEcosystemEnemy>().Select(n => n.GlobalPosition).ToArray();
                var r = game.GetChildren().OfType<EnemyEcosystemEnemy>().Select(n => (int)n.Role).ToArray();
                Check(w.Length == 12 && m.Length == 135, "Seeded initial content retains wrecks and reduced Medium population");
                if (repeat == 0) { wrecks = w; mobs = m; roles = r; terrain = Heights(arena); }
                else Check(wrecks!.SequenceEqual(w) && mobs!.SequenceEqual(m) && roles!.SequenceEqual(r) && terrain!.SequenceEqual(Heights(arena)),
                    "Restart reproduces initial wrecks, mob roles/positions and terrain");
                game.QueueFree(); await Frames(8);
            }
            await CheckSetupAndRestart(scene);
            var invalid = new Game { UseSphericalWorld = true, UseSessionWorldSeed = false, LayoutSeed = 0, MinimumFactionStartAngle = 181 };
            AddChild(invalid);
            Check(!invalid.GenerationSucceeded && invalid.GetNodeOrNull<Node>("PlayerShip") == null
                && invalid.GetNodeOrNull<PlanetSettings>("PlanetSettings") != null,
                "Invalid generation settings show recoverable setup without spawning an unsafe match");
            invalid.QueueFree(); await Frames(8);
            GD.Print($"World seed checks passed: {_checks}"); GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    private async Task CheckSetupAndRestart(PackedScene scene)
    {
        WorldSeeds.UseSeed(7319);
        var game = scene.Instantiate<Game>(); GetTree().Root.AddChild(game); GetTree().CurrentScene = game;
        var heights = Heights(game.GetNode<HeightmapArena>("HeightmapArena"));
        game._UnhandledInput(new InputEventAction { Action = "restart_arena", Pressed = true });
        await ToSignal(GetTree(), SceneTree.SignalName.SceneChanged);
        var restarted = (Game)GetTree().CurrentScene;
        Check(restarted != game && restarted.GenerationSucceeded && restarted.ActiveWorldSeed == 7319
            && heights.SequenceEqual(Heights(restarted.GetNode<HeightmapArena>("HeightmapArena"))),
            "Actual R scene reload keeps current seed and geography");
        var settings = restarted.GetNode<PlanetSettings>("PlanetSettings");
        settings._Input(new InputEventKey { Keycode = Key.F2, Pressed = true });
        Check(GetTree().Paused && WorldSeeds.Current == 7319, "Opening F2 does not generate a new world");
        var field = (LineEdit)settings.FindChild("WorldSeed", true, false);
        var button = (Button)settings.FindChild("StartNewPlanet", true, false);
        field.Text = "2147483648"; button.EmitSignal(BaseButton.SignalName.Pressed);
        Check(GetTree().Paused && WorldSeeds.Current == 7319 && GetTree().CurrentScene == restarted,
            "Invalid replay input cannot change the world or leave setup");
        if (DisplayServer.GetName() != "headless")
        {
            settings._Input(new InputEventKey { Keycode = Key.F2, Pressed = true });
            settings._Input(new InputEventKey { Keycode = Key.F2, Pressed = true });
            await Frames(3); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = GetViewport().GetTexture().GetImage();
            Check(image.SavePng("C:/Users/lblan/AppData/Local/Temp/opencode/RandomWorldSetup.png") == Error.Ok, "Seed setup capture saves");
        }
        field.Text = string.Empty; button.EmitSignal(BaseButton.SignalName.Pressed);
        await ToSignal(GetTree(), SceneTree.SignalName.SceneChanged);
        var fresh = (Game)GetTree().CurrentScene;
        Check(fresh.GenerationSucceeded && fresh.ActiveWorldSeed != 7319 && !GetTree().Paused
            && !heights.SequenceEqual(Heights(fresh.GetNode<HeightmapArena>("HeightmapArena"))),
            "Actual F2 blank-seed start generates new geography and resumes the match");
        settings = fresh.GetNode<PlanetSettings>("PlanetSettings");
        settings._Input(new InputEventKey { Keycode = Key.F2, Pressed = true });
        ((LineEdit)settings.FindChild("WorldSeed", true, false)).Text = "7319";
        ((Button)settings.FindChild("StartNewPlanet", true, false)).EmitSignal(BaseButton.SignalName.Pressed);
        await ToSignal(GetTree(), SceneTree.SignalName.SceneChanged);
        var replay = (Game)GetTree().CurrentScene;
        Check(replay.GenerationSucceeded && replay.ActiveWorldSeed == 7319 && heights.SequenceEqual(Heights(replay.GetNode<HeightmapArena>("HeightmapArena"))),
            "Actual F2 entered seed replays the original terrain");
        GetTree().CurrentScene = this;
        replay.QueueFree(); await Frames(8);
    }

    private void CheckIndependentBudget(HeightmapArena arena)
    {
        int land = 0, gentle = 0, steep = 0;
        const int count = 3072;
        for (int i = 0; i < count; i++)
        {
            float y = 1 - 2 * (i + 0.5f) / count, a = i * 2.39996323f + 1.11f, r = Mathf.Sqrt(1 - y * y);
            Vector3 up = new(r * Mathf.Cos(a), y, r * Mathf.Sin(a));
            Vector3 p = arena.PlanetCenter + up * arena.PlanetRadius;
            if (arena.HeightAt(p) <= arena.WaterHeight) continue;
            land++;
            Basis frame = PlanetGeometry.Frame(up, Mathf.Abs(up.Z) < 0.9f ? Vector3.Forward : Vector3.Right);
            float x = (arena.SurfaceHeight(p + frame.X) - arena.SurfaceHeight(p - frame.X)) / 2;
            float z = (arena.SurfaceHeight(p + frame.Z) - arena.SurfaceHeight(p - frame.Z)) / 2;
            float slope = new Vector2(x, z).Length();
            if (slope <= Mathf.Tan(Mathf.DegToRad(20))) gentle++;
            if (slope > Mathf.Tan(Mathf.DegToRad(40))) steep++;
        }
        GD.Print($"Independent random terrain R{arena.PlanetRadius}: {(float)gentle / land:P1} gentle, {(float)steep / land:P1} steep");
        Check(gentle >= land * 0.85f && steep >= land * 0.03f && steep <= land * 0.05f,
            "Random-world land budgets hold at independent sample locations");
    }
}

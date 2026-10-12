using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Ui;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class HoverMovementChecks : Node3D
{
    private int _checks;
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();
    private void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new Exception(message);
    }
    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }
    private void Space(bool pressed, bool echo = false) => GetViewport().PushInput(
        new InputEventKey { Keycode = Key.Space, PhysicalKeycode = Key.Space, Pressed = pressed, Echo = echo }, true);

    private HeightmapArena Surface(Node3D room, Func<float, float> height, float water = -30)
    {
        var arena = new HeightmapArena { CellsPerSide = 80, CellSize = 1, HeightScale = 0, WaterHeight = water };
        room.AddChild(arena);
        var grid = new float[81, 81];
        for (int z = 0; z <= 80; z++) for (int x = 0; x <= 80; x++) grid[x, z] = height(x - 40);
        typeof(HeightmapArena).GetField("_heights", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(arena, grid);
        typeof(HeightmapArena).GetMethod("BuildTerrain", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(arena, null);
        return arena;
    }

    private async Task Run()
    {
        try
        {
            await PaymentsAndInput();
            await ShapeChecks();
            await GlobeChecks();
            GD.Print($"Hover movement checks passed: {_checks}");
            await Frames(3);
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GetTree().Paused = false;
            Space(false);
            GD.PushError(error.ToString()); GetTree().Quit(1);
        }
    }

    private async Task PaymentsAndInput()
    {
        var room = new Node3D(); AddChild(room);
        var arena = Surface(room, _ => 0);
        var home = new FactionBase { Position = new Vector3(25, 0, 25), InitialStoredAntimatter = 120 };
        room.AddChild(home); home.SetPhysicsProcess(false);
        var ship = new ShipFlightController { Position = new Vector3(0, 2, 0), EnergyRegenFractionPerSecond = 0 };
        room.AddChild(ship);
        var ui = new BaseManagement(); room.AddChild(ui);
        await Frames(90);
        Check(Mathf.Abs(arena.Clearance(ship.GlobalPosition) - 2) < 0.15f && ship.IsHoverSupported,
            "Real collision/suspension settles at low hover without altitude input");
        int energy = ship.Energy, reserve = home.StoredAntimatter, hp = ship.Health;
        Space(true);
        Check(ship.Energy == energy - 2 && ship.Velocity.Y >= 11.9f, "Physical fresh Space pays exactly two energy and launches");
        Check(home.StoredAntimatter == reserve && ship.EnergyCapacity == reserve && home.SpentAntimatter == 0,
            "Jump does not spend storage or lower the energy limit");
        Check(!ship.TryActivateJump() && ship.Energy == energy - 2, "Airborne duplicate jump rejects atomically");
        float peak = 0;
        for (int i = 0; i < 150; i++) { await Frames(1); peak = Mathf.Max(peak, arena.Clearance(ship.GlobalPosition)); }
        Check(peak > 5.5f && peak < 6.5f, $"Level jump rises about four metres above hover (peak {peak})");
        Check(ship.IsHoverSupported && Mathf.Abs(arena.Clearance(ship.GlobalPosition) - 2) < 0.2f && ship.Health == hp,
            "Landing resumes hovering with no fall damage");
        Space(true, true); await Frames(5);
        Check(ship.Energy == energy - 2, "Held Space and key echo do not retrigger after landing");
        Space(false);
        ui.SetOpen(true); Space(true);
        Check(!ship.TryActivateJump() && ship.Energy == energy - 2, "Live management owns Space and guarded jump API");
        ui.SetOpen(false); Space(true, true); await Frames(10);
        Check(ship.Energy == energy - 2, "Closing UI while Space is held cannot trigger a jump");
        Space(false);
        GetTree().Paused = true;
        Check(!ship.TryActivateJump() && ship.Energy == energy - 2, "Paused jump rejects without payment");
        GetTree().Paused = false;
        ship.SpendEnergy(ship.Energy - 1);
        Check(!ship.TryActivateJump() && ship.Energy == 1, "Insufficient energy cannot launch or spend partial cost");
        ship.RespawnAt(new Vector3(0, 2.9f, 0)); ship.Velocity = Vector3.Down * 2;
        energy = ship.Energy;
        Space(true); await Frames(10); Space(false);
        Check(ship.Energy == energy - 2 && ship.Velocity.Y > 6,
            "A fresh press just before landing buffers one paid jump when support returns");
        ship.RespawnAt(new Vector3(0, 2, 0));
        Check(ship.TryActivateJump(), "Respawn resets support/jump state for a legal launch");
        Check(ship.TryActivateBooster() && ship.Velocity.Y > 11, "Booster activation preserves jump velocity");
        await Frames(8);
        Check(ship.Velocity.Y > 7 && arena.Tangent(ship.Velocity, ship.GlobalPosition).Length() > 35,
            "Active thrust retains ballistic radial motion while supplying horizontal speed");
        ship.SetPhysicsProcess(false);
        ship.GlobalPosition = new Vector3(0, 25, 0); ship.Velocity = Vector3.Zero;
        ship._PhysicsProcess(1f / 60);
        Check(ship.GlobalPosition.Y > 24 && ship.Velocity.Y < 0, "High airborne ship falls physically without twelve-metre ceiling snap");
        ship.SetPhysicsProcess(true); await Frames(240);
        Check(ship.Health == hp && arena.Clearance(ship.GlobalPosition) > 1.7f, "Long fall lands safely and recovers hover height");
        Check(home.Construction.TryStart("equipment-dash-1", out _), "Dash fixture pays for normal research");
        home.Construction.Advance(100);
        ship.RespawnAt(new Vector3(13, 2, 25));
        Check(ship.TryInstallEquipment("dash", 1, out _), "Dash fixture installs at the real home");
        await Frames(3);
        var camera = ship.GetNode<Camera3D>("CameraPivot/Camera3D");
        camera.LookAt(camera.GlobalPosition + Vector3.Up, Vector3.Right);
        energy = ship.Energy;
        Check(ship.TryActivateEquipmentSlot(1) && ship.Energy == energy - 10 && ship.Velocity.Y <= 12.01f,
            "Upward Dash pays normal cost with lift bounded by jump launch speed");
        Check(!ship.TryActivateJump(), "Dash leaves support immediately and cannot chain an airborne Space jump");
        ship.RespawnAt(new Vector3(0, 2, 0));
        typeof(ShipFlightController).GetField("_damageInvulnerabilityTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(ship, 0f);
        ship.ApplyDamage(1000); energy = ship.Energy;
        Check(!ship.TryActivateJump() && ship.Energy == energy, "Dead pilot cannot jump or pay");
        Space(false); room.QueueFree(); await Frames(4);
    }

    private async Task ShapeChecks()
    {
        foreach (string shape in new[] { "slope", "wall", "crest", "drop", "water" })
        {
            var room = new Node3D(); AddChild(room);
            Func<float, float> height = shape switch
            {
                "slope" => x => Mathf.Clamp(x * 0.25f, -5, 5),
                "wall" => x => Mathf.Clamp(x * 3, 0, 20),
                "crest" => x => Mathf.Clamp((x + 20) * 0.4f, 0, 8),
                "drop" => x => x <= 0 ? 15 : Mathf.Max(0, 15 - x * 5),
                _ => _ => -10
            };
            var arena = Surface(room, height, shape == "water" ? 0 : -30);
            Vector3 start = new(-12, 0, 0); start = arena.OnSurface(start, 2);
            var ship = new ShipFlightController { IsPlayerControlled = false, Position = start, EnergyRegenFractionPerSecond = 0 };
            room.AddChild(ship);
            ship.SetAutonomousControls(Vector3.Right, Vector3.Right);
            int hp = ship.Health;
            float maxClearance = 0, crestVelocity = 0;
            for (int i = 0; i < 150; i++)
            {
                await Frames(1);
                maxClearance = Mathf.Max(maxClearance, arena.Clearance(ship.GlobalPosition));
                if (ship.GlobalPosition.X > 0 && ship.GlobalPosition.X < 5) crestVelocity = Mathf.Max(crestVelocity, ship.Velocity.Y);
            }
            if (shape == "wall") Check(ship.GlobalPosition.X < 2 && ship.GlobalPosition.Y < 6,
                $"Steep terrain blocks climb instead of teleporting uphill ({ship.GlobalPosition})");
            if (shape == "slope") Check(ship.GlobalPosition.X > 10 && Mathf.Abs(arena.Clearance(ship.GlobalPosition) - 2) < 0.6f,
                $"Gentle slope remains traversable with ground following ({ship.GlobalPosition})");
            if (shape == "crest") Check(crestVelocity > 2 && maxClearance > 2.8f, "Uphill crest retains positive momentum and produces a free ramp launch");
            if (shape == "drop") Check(maxClearance > 12 && ship.Health == hp, "Steep drop creates real airborne travel above old ceiling with no fall damage");
            if (shape == "drop")
            {
                var motion = new ShipHoverMotion();
                Vector3 lip = new(-0.8f, 17, 0);
                motion.Reset(arena, lip);
                motion.Step(arena, new Vector3(2, 17, 0), Vector3.Right * 24, Vector3.Right * 24, 28, 9, false, 1f / 60);
                Check(!motion.Supported && motion.CanJump, "Leaving an edge preserves a brief jump grace");
                motion.Step(arena, new Vector3(2, 17, 0), Vector3.Right * 24, Vector3.Right * 24, 28, 9, false, 0.11f);
                Check(!motion.CanJump, "Edge grace expires without allowing an airborne double jump");
            }
            if (shape == "water") Check(ship.GlobalPosition.X > 10 && Mathf.Abs(arena.Clearance(ship.GlobalPosition) - 2) < 0.2f,
                "Ocean surface supports free low hovering and travel");
            room.QueueFree(); await Frames(4);
        }
    }

    private async Task GlobeChecks()
    {
        var room = new Node3D(); AddChild(room);
        var arena = new HeightmapArena { IsSpherical = true, PlanetRadius = 100 }; room.AddChild(arena);
        foreach (Vector3 up in new[] { Vector3.Down, new Vector3(1, 1, 0).Normalized() })
        {
            Vector3 start = arena.OnSurface(arena.PlanetCenter + up * 100, 2);
            arena.FlattenTerrain(start, 45, arena.SurfaceHeight(start));
            var home = new FactionBase { IsPlayerBase = false, Position = arena.OnSurface(start + Vector3.Right * 25), InitialStoredAntimatter = 120 };
            room.AddChild(home); home.SetPhysicsProcess(false);
            var ship = new ShipFlightController { IsPlayerControlled = false, Position = arena.OnSurface(start, 2), EnergyRegenFractionPerSecond = 0 };
            room.AddChild(ship);
            await Frames(90);
            Check(ship.IsHoverSupported, "Radial suspension supports underside/face-seam ship");
            int energy = ship.Energy;
            Check(ship.TryActivateJump() && ship.Energy == energy - 2 && ship.Velocity.Dot(arena.UpAt(ship.GlobalPosition)) > 11,
                "Shared autonomous jump launches away from local surface at the same paid cost");
            await Frames(150);
            Check(ship.IsHoverSupported && ship.Health == 100 && Mathf.Abs(arena.Clearance(ship.GlobalPosition) - 2) < 0.4f,
                "Radial gravity returns underside/seam jump to safe hovering");
            ship.QueueFree(); home.QueueFree(); await Frames(4);
        }
        room.QueueFree(); await Frames(4);
    }
}

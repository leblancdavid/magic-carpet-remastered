using System;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class RivalFlightChecks : Node3D
{
    private int _checks;

    public override void _Ready() => Callable.From(() => { _ = RunChecks(); }).CallDeferred();

    private async Task RunChecks()
    {
        try
        {
            var arena = new HeightmapArena();
            AddChild(arena);
            AddChild(new FactionBase { Position = Surface(arena, -22, 8) + Vector3.Up * 2 });
            var enemyBase = new FactionBase { IsPlayerBase = false, InitialStoredAntimatter = 0,
                Position = Surface(arena, 22, -12) + Vector3.Up * 2 };
            AddChild(enemyBase);
            var orb = new AntimatterPickup { AntimatterAmount = 8, Position = Surface(arena, 9, -13) + Vector3.Up * 0.45f };
            AddChild(orb);
            orb.Freeze = true;
            var pilot = new EnemyEcosystemEnemy
            {
                Role = EnemyEcosystemEnemy.EnemyRole.RivalShip,
                Position = Surface(arena, 17, -17) + Vector3.Up * 7
            };
            AddChild(pilot);
            float highest = 0;
            int highestReserve = 0;
            for (int frame = 0; frame < 600; frame++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                float clearance = pilot.GlobalPosition.Y - Surface(arena, pilot.GlobalPosition.X, pilot.GlobalPosition.Z).Y;
                highest = Mathf.Max(highest, clearance);
                highestReserve = Mathf.Max(highestReserve, pilot.AntimatterReserve);
                if (frame % 120 == 119)
                {
                    GD.Print($"Pilot frame {frame + 1}: position {pilot.GlobalPosition}, clearance {clearance:0.0}, reserve {pilot.AntimatterReserve}");
                }
            }
            Check(highest <= arena.MaxFlightClearance + 0.01f, "Pilot remains within shared reachable flight altitude instead of ascending indefinitely");
            Check(highestReserve == 8 && pilot.AntimatterReserve + enemyBase.StoredAntimatter == 8,
                "Pilot reaches, collects, and banks its targeted globe without losing antimatter");
            Check(pilot.GetNodeOrNull<MeshInstance3D>("Ship") != null
                && pilot.GetNodeOrNull<Label3D>("PilotLabel") != null,
                "Rival has a distinct ship silhouette and world label");

            var player = new ShipFlightController { Position = new Vector3(100, 100, 100) };
            AddChild(player);
            player.SetPhysicsProcess(false);
            bool bounded = true;
            for (int frame = 0; frame < 300; frame++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                float clearance = pilot.GlobalPosition.Y - Surface(arena, pilot.GlobalPosition.X, pilot.GlobalPosition.Z).Y;
                bounded &= clearance >= pilot.GroundClearance - 0.01f && clearance <= arena.MaxFlightClearance + 0.01f;
            }
            Check(bounded, "Combat height tracking cannot chase a high player out of the playable altitude band");
            enemyBase.ApplyDamage(1000);
            pilot.AbsorbAntimatter(20);
            Vector3 before = pilot.GlobalPosition;
            for (int frame = 0; frame < 60; frame++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Check(pilot.GlobalPosition.DistanceTo(before) > 1, "Pilot keeps moving after its hub is destroyed despite carrying retreat-level antimatter");
            arena.RaiseTerrain(pilot.GlobalPosition, 5, 20);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Check(pilot.GlobalPosition.Y >= Surface(arena, pilot.GlobalPosition.X, pilot.GlobalPosition.Z).Y + pilot.GroundClearance - 0.01f,
                "Raised terrain cannot bury the flying pilot");
            GD.Print($"Pilot flight checks passed: {_checks}");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }

    private static Vector3 Surface(HeightmapArena arena, float x, float z) =>
        new(x, Mathf.Max(arena.HeightAt(x, z), arena.WaterHeight), z);

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }
}

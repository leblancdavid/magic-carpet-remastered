using System;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class WizardFlightChecks : Node3D
{
    private int _checks;

    public override void _Ready() => Callable.From(() => { _ = RunChecks(); }).CallDeferred();

    private async Task RunChecks()
    {
        try
        {
            var arena = new HeightmapArena();
            AddChild(arena);
            AddChild(new CastleKeep { Position = Surface(arena, -22, 8) + Vector3.Up * 2 });
            var enemyCastle = new CastleKeep { IsPlayerCastle = false, InitialStoredMana = 0,
                Position = Surface(arena, 22, -12) + Vector3.Up * 2 };
            AddChild(enemyCastle);
            var orb = new ManaPickup { ManaAmount = 8, Position = Surface(arena, 9, -13) + Vector3.Up * 0.45f };
            AddChild(orb);
            orb.Freeze = true;
            var wizard = new EnemyEcosystemEnemy
            {
                Role = EnemyEcosystemEnemy.EnemyRole.EnemyWizard,
                Position = Surface(arena, 17, -17) + Vector3.Up * 7
            };
            AddChild(wizard);
            float highest = 0;
            int highestReserve = 0;
            for (int frame = 0; frame < 600; frame++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                float clearance = wizard.GlobalPosition.Y - Surface(arena, wizard.GlobalPosition.X, wizard.GlobalPosition.Z).Y;
                highest = Mathf.Max(highest, clearance);
                highestReserve = Mathf.Max(highestReserve, wizard.ManaReserve);
                if (frame % 120 == 119)
                {
                    GD.Print($"Wizard frame {frame + 1}: position {wizard.GlobalPosition}, clearance {clearance:0.0}, reserve {wizard.ManaReserve}");
                }
            }
            Check(highest <= arena.MaxFlightClearance + 0.01f, "Wizard remains within shared reachable flight altitude instead of ascending indefinitely");
            Check(highestReserve == 8 && wizard.ManaReserve + enemyCastle.StoredMana == 8,
                "Wizard reaches, collects, and banks its targeted globe without losing mana");
            Check(wizard.GetNodeOrNull<MeshInstance3D>("Carpet") != null
                && wizard.GetNodeOrNull<Label3D>("WizardLabel") != null,
                "Rival has a distinct carpet silhouette and world label");

            var player = new CarpetFlightController { Position = new Vector3(100, 100, 100) };
            AddChild(player);
            player.SetPhysicsProcess(false);
            bool bounded = true;
            for (int frame = 0; frame < 300; frame++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                float clearance = wizard.GlobalPosition.Y - Surface(arena, wizard.GlobalPosition.X, wizard.GlobalPosition.Z).Y;
                bounded &= clearance >= wizard.GroundClearance - 0.01f && clearance <= arena.MaxFlightClearance + 0.01f;
            }
            Check(bounded, "Combat height tracking cannot chase a high player out of the playable altitude band");
            enemyCastle.ApplyDamage(1000);
            wizard.AbsorbMana(20);
            Vector3 before = wizard.GlobalPosition;
            for (int frame = 0; frame < 60; frame++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Check(wizard.GlobalPosition.DistanceTo(before) > 1, "Wizard keeps moving after its castle is destroyed despite carrying retreat-level mana");
            arena.RaiseTerrain(wizard.GlobalPosition, 5, 20);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Check(wizard.GlobalPosition.Y >= Surface(arena, wizard.GlobalPosition.X, wizard.GlobalPosition.Z).Y + wizard.GroundClearance - 0.01f,
                "Raised terrain cannot bury the flying wizard");
            GD.Print($"Wizard flight checks passed: {_checks}");
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

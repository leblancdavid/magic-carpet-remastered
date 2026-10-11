using System;
using System.Reflection;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class CameraFriendlyFireChecks : Node3D
{
    private int _checks;

    public override void _Ready() => Callable.From(RunChecks).CallDeferred();

    private void RunChecks()
    {
        try
        {
            var player = new ShipFlightController();
            AddChild(player);
            player.SetPhysicsProcess(false);
            var ship = player.GetNode<MeshInstance3D>("ShipVisual");
            var camera = player.GetNode<Camera3D>("CameraPivot/Camera3D");
            Check(player.CameraMode == "First Person" && !ship.Visible,
                "Gameplay starts in first person with ship hidden");
            Check(camera.Position.IsEqualApprox(new Vector3(0, 0.25f, -0.45f)) && camera.Rotation.IsEqualApprox(Vector3.Zero),
                "Startup camera is immediately at the first-person pose");
            var toggle = new InputEventAction { Action = "toggle_camera", Pressed = true };
            player._UnhandledInput(toggle);
            Check(player.CameraMode == "Chase" && ship.Visible, "Chase toggle restores ship");
            player._UnhandledInput(toggle);
            Check(player.CameraMode == "First Person" && !ship.Visible, "First-person toggle hides ship immediately");

            var lab = new PlanetFlightController();
            AddChild(lab);
            lab.SetPhysicsProcess(false);
            var labShip = (Node3D)lab.GetChild(0);
            Check(lab.FirstPerson && !labShip.Visible, "Flight lab also starts in first person");
            lab._UnhandledInput(toggle);
            Check(!lab.FirstPerson && labShip.Visible, "Flight lab chase toggle restores ship");
            lab._UnhandledInput(toggle);
            Check(lab.FirstPerson && !labShip.Visible, "Flight lab first-person toggle hides ship");

            var home = new FactionBase { Position = new Vector3(-40, 20, 0) };
            var rival = new FactionBase { IsPlayerBase = false, Position = new Vector3(40, 20, 0) };
            AddChild(home);
            AddChild(rival);
            home.SetPhysicsProcess(false);
            rival.SetPhysicsProcess(false);
            int stored = home.StoredAntimatter;
            var homeShot = Shot(player, home.GlobalPosition);
            Invoke(homeShot, "OnBodyEntered", home);
            Check(home.Health == home.MaxHealth && home.StoredAntimatter == stored,
                "Player plasmaBolt cannot damage home hub or spill its antimatter");
            var rivalShot = Shot(player, rival.GlobalPosition);
            Invoke(rivalShot, "OnBodyEntered", rival);
            Check(rival.Health == rival.MaxHealth - rivalShot.Damage, "Player plasmaBolt still damages rival hub");

            var friendly = new CollectorDrone { Position = new Vector3(0, 20, -40) };
            var hostile = new CollectorDrone { UsePlayerAntimatter = false, Position = new Vector3(0, 20, 40) };
            AddChild(friendly);
            AddChild(hostile);
            friendly.SetPhysicsProcess(false);
            hostile.SetPhysicsProcess(false);
            var friendlyShot = Shot(player, friendly.GlobalPosition);
            friendlyShot._PhysicsProcess(0.001);
            Check(friendly.Health == friendly.MaxHealth && !friendlyShot.IsQueuedForDeletion(),
                "PlasmaBolt passes friendly drone without damage or consumption");
            var hostileShot = Shot(player, hostile.GlobalPosition);
            hostileShot._PhysicsProcess(0.001);
            Check(hostile.Health == hostile.MaxHealth - hostileShot.Damage && hostileShot.IsQueuedForDeletion(),
                "PlasmaBolt still hits and damages rival drone");
            Invoke(player, "DamageEnemiesInRadius", friendly.GlobalPosition, 8f, 3);
            Check(friendly.Health == friendly.MaxHealth, "Pulse Burst skips friendly drone");
            Invoke(player, "DamageEnemiesInRadius", hostile.GlobalPosition, 8f, 3);
            Check(hostile.Health == hostile.MaxHealth - hostileShot.Damage - 3, "Pulse Burst still damages rival drone");
            Invoke(player, "DamageEnemiesInRadius", home.GlobalPosition, 8f, 32);
            Check(home.Health == home.MaxHealth && home.StoredAntimatter == stored, "Pulse Burst leaves home hub intact");
            var enemyShot = new EnemyProjectile();
            AddChild(enemyShot);
            enemyShot.SetPhysicsProcess(false);
            Invoke(enemyShot, "OnBodyEntered", home);
            Check(home.Health == home.MaxHealth - enemyShot.Damage, "Enemy attacks can still damage home hub");
            GD.Print($"Camera/friendly-fire checks passed: {_checks} assertions.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private WeaponProjectile Shot(ShipFlightController owner, Vector3 position)
    {
        var shot = new WeaponProjectile { Position = position };
        AddChild(shot);
        shot.Launch(Vector3.Forward, owner);
        shot.SetPhysicsProcess(false);
        return shot;
    }

    private static void Invoke(object target, string method, params object[] args)
        => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }
}

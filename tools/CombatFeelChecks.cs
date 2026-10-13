using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Ui;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class CombatFeelChecks : Node3D
{
    private int _checks;
    private void Check(bool condition, string message) { _checks++; if (!condition) throw new InvalidOperationException(message); }
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();
    private async Task Run()
    {
        try
        {
            var room = new Node3D(); AddChild(room);
            var arena = new HeightmapArena(); room.AddChild(arena); arena.SetProcess(false); arena.SetPhysicsProcess(false);
            var home = new FactionBase { InitialStoredAntimatter = 30 }; room.AddChild(home); home.SetPhysicsProcess(false);
            var enemyHome = new FactionBase { IsPlayerBase = false, Position = new Vector3(60, 0, 60) }; room.AddChild(enemyHome); enemyHome.SetPhysicsProcess(false);
            var player = new ShipFlightController { Position = new Vector3(0, 12, 0), DamageInvulnerabilitySeconds = 0 };
            var rival = new ShipFlightController { IsPlayerControlled = false, Position = new Vector3(60, 12, 60), DamageInvulnerabilitySeconds = 0 };
            room.AddChild(player); room.AddChild(rival); player.SetPhysicsProcess(false); rival.SetPhysicsProcess(false);
            Input.MouseMode = Input.MouseModeEnum.Visible;
            var hud = new Hud(); room.AddChild(hud); hud.SetProcess(false);
            var camera = player.GetNode<Camera3D>("CameraPivot/Camera3D");
            foreach (var ship in new[] { player, rival })
            {
                ship.ApplyDamage(20);
                Check(ship.Health == 80, "Damage reaches either ship before recovery");
                ship._PhysicsProcess(4.75);
                Check(ship.Health == 80, "Neither ship heals inside the five-second combat delay");
                ship._PhysicsProcess(0.75);
                Check(ship.Health == 81, "Recovery starts only for the time beyond five seconds");
                ship._PhysicsProcess(0.25);
                Check(ship.Health == 81, "Fractional recovery accumulates without rounding up each frame");
                ship._PhysicsProcess(0.25);
                Check(ship.Health == 82, "Fractional recovery pays an exact HP over time");
                var victim = new EnemyEcosystemEnemy { IsHeavy = true, Role = EnemyEcosystemEnemy.EnemyRole.GroundBeast, Position = new Vector3(30, 12, 30) };
                room.AddChild(victim); victim.SetPhysicsProcess(false);
                FactionCombat.Damage(victim, 1, ship);
                ship._PhysicsProcess(4.75);
                Check(ship.Health == 82, "Dealing combat damage restarts the quiet delay");
                ship._PhysicsProcess(0.75);
                Check(ship.Health == 83, "Recovery resumes after outgoing combat ends");
                ship.ApplyDamage(1); ship._PhysicsProcess(4); ship.ApplyDamage(1); ship._PhysicsProcess(2);
                Check(ship.Health == 81, "A subsequent incoming hit restarts the whole delay");
                GetTree().Paused = true; ship._PhysicsProcess(20); GetTree().Paused = false;
                Check(ship.Health == 81, "Pause cannot advance health recovery");
                ship.GameplayInputBlocked = true; ship._PhysicsProcess(4);
                Check(ship.Health == 83, "Live interfaces continue quiet recovery");
                ship.GameplayInputBlocked = false;
                ship._PhysicsProcess(100);
                Check(ship.Health == ship.MaximumHealth, "Recovery clamps at maximum health");
                ship.ApplyDamage(1000); ship._PhysicsProcess(100);
                Check(ship.Health == 0, "Recovery never revives a defeated ship");
                ship.RespawnAt(new Vector3(0, 12, 0));
                Check(ship.Health == ship.MaximumHealth && ship.HitFeedbackStrength == 0, "Respawn restores health and clears hit feedback");
                victim.QueueFree();
            }
            player.DamageInvulnerabilitySeconds = 1;
            player._PhysicsProcess(2.1);
            player.ApplyDamage(10); hud._Process(1);
            var shade = (ColorRect)hud.FindChild("HitShade", true, false);
            Check(shade.Color.A > 0.2f, "An actual health hit briefly darkens the world");
            foreach (bool firstPerson in new[] { true, false })
            {
                typeof(ShipFlightController).GetField("_firstPersonCamera", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, firstPerson);
                player._PhysicsProcess(0.03);
                Check(Mathf.Abs(camera.HOffset) + Mathf.Abs(camera.VOffset) > 0.001f, "Both camera modes have bounded hit shake");
                if (DisplayServer.GetName() != "headless")
                {
                    hud._Process(1);
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    using var image = GetViewport().GetTexture().GetImage();
                    Check(image.SavePng($"C:/Users/lblan/AppData/Local/Temp/opencode/HitFeedback{(firstPerson ? "FirstPerson" : "Chase")}.png") == Error.Ok,
                        "Rendered camera-mode hit feedback capture saves");
                }
            }
            float hit = player.HitFeedbackStrength; int hp = player.Health;
            player.ApplyDamage(10);
            Check(player.Health == hp && player.HitFeedbackStrength == hit, "Invulnerable hits neither damage nor restart feedback");
            player._PhysicsProcess(0.3); hud._Process(1);
            Check(shade.Color.A == 0 && camera.HOffset == 0 && camera.VOffset == 0, "Shake and darkening fully fade without drift");
            player.RespawnAt(new Vector3(0, 12, 0)); player._PhysicsProcess(0);
            int energy = player.Energy;
            Check(player.TryActivateEquipmentSlot(0) && player.Energy == energy - 2, "Actual Plasma activation spends two energy");
            var bolt = room.GetChildren().OfType<WeaponProjectile>().Last();
            Check(bolt.Damage == 13, "Actual starter projectile deals thirteen damage");
            room.QueueFree();
            for (int i = 0; i < 12; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            GD.Print($"Combat feel checks passed: {_checks}"); GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}

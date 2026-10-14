using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.World;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Ui;

namespace MagicCarpetRemastered.Tools;

public partial class AutomaticFleetChecks : Node3D
{
    private int _checks;
    private void Check(bool condition, string message) { _checks++; if (!condition) throw new InvalidOperationException(message); }
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();
    private async Task Frames(int n) { for (int i = 0; i < n; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    private void Research(FactionBase home, string id, int rank)
    {
        home.DepositAntimatter(10000);
        Check(home.Construction.TryStart(UpgradeCatalog.ProjectId(id, rank), out string reason), $"Research {id}/{rank}: {reason}");
        home.Construction.Advance(500);
    }
    private async Task Run()
    {
        try
        {
            WorldSeeds.UseSeed(7319); PlanetSizes.SelectedIndex = 1;
            var game = GD.Load<PackedScene>("res://scenes/PlanetGame.tscn").Instantiate<MagicCarpetRemastered.Scripts.Game.Game>(); AddChild(game);
            foreach (Node node in game.GetChildren()) { node.SetPhysicsProcess(false); if (node is not CanvasLayer) node.SetProcess(false); }
            var home = game.GetNode<FactionBase>("PlayerBase"); var enemy = game.GetNode<FactionBase>("EnemyBase");
            foreach (var hub in new[] { home, enemy })
            { hub.Fleet.SetPhysicsProcess(false); hub.GetNode<BaseDefense>("BaseDefense").SetPhysicsProcess(false); }
            var arena = game.GetNode<HeightmapArena>("HeightmapArena");
            var ship = game.GetNode<ShipFlightController>("PlayerShip");
            var rival = game.GetNode<ShipFlightController>("RivalPilot");
            var ui = game.GetNode<BaseManagement>("BaseManagement");
            await Frames(2);
            BaseTacticalDrone[] Units(string id) => game.GetChildren().OfType<BaseTacticalDrone>()
                .Where(d => d.Home == home && !d.IsQueuedForDeletion() && (id == "scout-drone" ? d.Role == BaseTacticalDrone.DroneRole.Scout : d.Role == BaseTacticalDrone.DroneRole.Siphon)).ToArray();
            void FreezeUnits() { foreach (var d in game.GetChildren().OfType<BaseTacticalDrone>()) d.SetPhysicsProcess(false); foreach (var d in game.GetChildren().OfType<BaseStrikeDrone>()) d.SetPhysicsProcess(false); }
            Research(home, "scout-drone", 1);
            long spent = home.SpentAntimatter;
            home.Fleet._PhysicsProcess(9.9);
            Check(Units("scout-drone").Length == 0, "Initial fleet respects production delay");
            home.Fleet._PhysicsProcess(0.11); FreezeUnits();
            Check(Units("scout-drone").Length == 1 && home.SpentAntimatter == spent, "Bronze automatically builds one free unit");
            var first = Units("scout-drone").Single();
            Check(first.Target is StrategicPoint, "Automatic scout finds a point");
            first.ApplyDamage(10000);
            home.Fleet._PhysicsProcess(0.01); await Frames(2);
            home.Fleet._PhysicsProcess(9.9);
            Check(Units("scout-drone").Length == 0, "Destroyed slot waits for its full replacement clock");
            home.Fleet._PhysicsProcess(0.1); FreezeUnits();
            Check(Units("scout-drone").Length == 1 && home.SpentAntimatter == spent, "Death replaces without AM expenditure");
            Research(home, "scout-drone", 2); Research(home, "scout-drone", 3);
            home.DepositAntimatter(10000);
            string expansion = AutomaticDroneFleet.ExpansionId("scout-drone", 2);
            Check(home.Construction.TryStart(expansion, out _), "Gold unlocks fleet expansion");
            var project = home.Construction.GetActive(ConstructionSlot.Offense)!; int paid = project.PaidAntimatter;
            int beforeCancel = home.StoredAntimatter;
            Check(home.Construction.Cancel(ConstructionSlot.Offense, project.InstanceId) && home.StoredAntimatter == beforeCancel + paid,
                "Expansion cancellation refunds exact payment");
            Check(home.Construction.TryStart(expansion, out _), "Cancelled expansion can be restarted"); home.Construction.Advance(500);
            home.Fleet._PhysicsProcess(10); FreezeUnits();
            var scouts = Units("scout-drone");
            Check(scouts.Length == 2 && scouts.Select(d => d.Target).Distinct().Count() == 2, "Additional scouts receive different targets");
            Check(scouts.All(d => d.Health == 50), "Gold updates surviving unit durability and new production");
            foreach (var point in game.GetChildren().OfType<StrategicPoint>()) point.Advance(100, true, false);
            home.Fleet._PhysicsProcess(1);
            Check(scouts.All(d => d.Target == null), "Scouts wait when all points are friendly");
            var available = game.GetChildren().OfType<StrategicPoint>().First(); available.Advance(100, false, true);
            home.Fleet._PhysicsProcess(1);
            Check(scouts.Count(d => d.Target == available) == 1, "One available point gets exactly one assigned scout");
            Check(ui.SetOpen(true) && !ui.OpenTargetOrder("scout-drone"), "Management uses automatic fleet status instead of paid orders"); ui.SetOpen(false);

            Check(home.TryCompleteTierUpgrade(2) && enemy.TryCompleteTierUpgrade(2), "Both factions reach tier II");
            foreach (string id in new[] { "attack-drone", "defensive-drone" })
            {
                for (int rank = 1; rank <= 3; rank++) Research(home, id, rank);
                home.DepositAntimatter(10000);
                Check(home.Construction.TryStart(AutomaticDroneFleet.ExpansionId(id, 2), out _), "Gold combat unit permits a second fleet slot");
                home.Construction.Advance(500);
            }
            home.Fleet._PhysicsProcess(10); FreezeUnits();
            foreach (var role in new[] { BaseTacticalDrone.DroneRole.Attacker, BaseTacticalDrone.DroneRole.Defender })
                Check(game.GetChildren().OfType<BaseTacticalDrone>().Where(d => d.Home == home && d.Role == role).Select(d => d.Target).Distinct().Count() == 2,
                    $"Additional {role} drones spread over distinct objectives");
            Research(home, "harvester-drone", 1); home.Fleet._PhysicsProcess(10); FreezeUnits();
            var harvester = game.GetChildren().OfType<BaseTacticalDrone>().Single(d => d.Role == BaseTacticalDrone.DroneRole.Harvester);
            Check(BaseTacticalDrone.IsHarvestable(harvester.Target), "Spherical fleet assigns a real non-site habitat mob");
            harvester.GlobalPosition = arena.OnSurface(harvester.Target!.GlobalPosition, 5);
            var harvestBall = new AntimatterPickup { AntimatterAmount = 9, Ownership = AntimatterOwnership.Neutral,
                Position = harvester.GlobalPosition + arena.UpAt(harvester.GlobalPosition) * 2 };
            game.AddChild(harvestBall); harvestBall.Freeze = true; harvestBall.SetPhysicsProcess(false);
            var opponentBall = new AntimatterPickup { AntimatterAmount = 7, Ownership = AntimatterOwnership.Enemy,
                Position = harvester.GlobalPosition + arena.UpAt(harvester.GlobalPosition) * 4 };
            game.AddChild(opponentBall); opponentBall.Freeze = true; opponentBall.SetPhysicsProcess(false);
            await Frames(2); harvester._PhysicsProcess(0.35);
            Check(harvestBall.Ownership == AntimatterOwnership.Player && harvestBall.AntimatterAmount == 9
                && opponentBall.Ownership == AntimatterOwnership.Enemy, "Spherical Harvester claims neutral physical AM without stealing opposing-owned drops");
            harvestBall.QueueFree(); opponentBall.QueueFree();
            Research(home, "siphon-drone", 1); home.Fleet._PhysicsProcess(10); FreezeUnits();
            var siphon = Units("siphon-drone").Single();
            Check(siphon.Target == enemy, "Siphon selects the opposing base");
            siphon.GlobalPosition = arena.OnSurface(enemy.GlobalPosition + enemy.GlobalBasis.X * 5, 5);
            enemy.DepositAntimatter(100); int enemyBefore = enemy.StoredAntimatter;
            siphon._PhysicsProcess(1);
            Check(siphon.CarriedAntimatter == 2 && enemy.StoredAntimatter == enemyBefore - 2, "Theft physically transfers actual enemy storage into cargo");
            for (int i = 0; i < 4; i++) siphon._PhysicsProcess(1);
            Check(siphon.CarriedAntimatter == 10, "Bronze theft cargo is capped at 10 A");
            home.DepositAntimatter(10000);
            siphon.GlobalPosition = arena.OnSurface(home.GlobalPosition + home.GlobalBasis.X * 5, 5); siphon._PhysicsProcess(1);
            Check(siphon.CarriedAntimatter == 10, "Full home storage retains undeposited stolen cargo");
            home.WithdrawAntimatter(home.StoredAntimatter);
            siphon.GlobalPosition = arena.OnSurface(home.GlobalPosition + home.GlobalBasis.X * 5, 5); siphon._PhysicsProcess(1);
            Check(siphon.CarriedAntimatter == 0 && home.StoredAntimatter == 10, "Stolen cargo deposits at home without duplication");
            siphon.GlobalPosition = arena.OnSurface(enemy.GlobalPosition + enemy.GlobalBasis.X * 5, 5); siphon._PhysicsProcess(1);
            int cargo = siphon.CarriedAntimatter;
            Check(cargo > 0, "Siphon starts another raid");
            int looseBefore = game.GetChildren().OfType<AntimatterPickup>().Sum(o => o.AntimatterAmount);
            siphon.ApplyDamage(10000);
            Check(game.GetChildren().OfType<AntimatterPickup>().Sum(o => o.AntimatterAmount) == looseBefore + cargo && siphon.CarriedAntimatter == 0,
                "Intercepted theft cargo spills exactly once");
            Research(home, "strike-drone", 1); home.Fleet._PhysicsProcess(19.9); FreezeUnits();
            Check(!game.GetChildren().OfType<BaseStrikeDrone>().Any(), "Strike waits twenty seconds");
            home.Fleet._PhysicsProcess(0.11); FreezeUnits();
            Check(game.GetChildren().OfType<BaseStrikeDrone>().Count() == 1, "Strike auto-production uses its separate twenty-second timer");
            var strike = game.GetChildren().OfType<BaseStrikeDrone>().Single();
            int targetHealth = enemy.Health;
            strike.GlobalPosition = enemy.GlobalPosition + arena.UpAt(enemy.GlobalPosition) * 4;
            strike._PhysicsProcess(0.01);
            Check(strike.IsQueuedForDeletion() && enemy.Health == targetHealth - 120, "Strike spends itself once on impact");
            home.Fleet._PhysicsProcess(0.01); await Frames(2);
            home.ApplyDamage(400);
            Check(home.Tier == 1, "Fixture loses tier II");
            home.Fleet._PhysicsProcess(60);
            Check(!game.GetChildren().OfType<BaseStrikeDrone>().Any(), "Tier loss suspends automatic strike replacements");
            Check(home.TryCompleteTierUpgrade(2), "Fixture rebuilds tier II");
            home.Fleet._PhysicsProcess(19.8);
            Check(!game.GetChildren().OfType<BaseStrikeDrone>().Any(), "Suspended production clock does not advance during tier loss");
            home.Fleet._PhysicsProcess(0.2); FreezeUnits();
            Check(game.GetChildren().OfType<BaseStrikeDrone>().Count() == 1, "Rebuilding resumes the surviving fleet entitlement");
            Research(enemy, "scout-drone", 1); int enemyStored = enemy.StoredAntimatter;
            enemy.Fleet._PhysicsProcess(10); FreezeUnits();
            Check(game.GetChildren().OfType<BaseTacticalDrone>().Any(d => d.Home == enemy) && enemy.StoredAntimatter == enemyStored, "Rival receives the same free automatic fleet");

            Research(home, "claim-range", 1); Research(home, "claim-range", 2); Research(home, "claim-range", 3);
            var orb = new AntimatterPickup { Ownership = AntimatterOwnership.Player, Position = arena.OnSurface(home.GlobalPosition, 1) }; game.AddChild(orb); orb.Freeze = true; orb.SetPhysicsProcess(false);
            Check(orb.EffectiveAttractionGap == 12 && Mathf.IsEqualApprox(orb.EffectiveMergeContactSeconds, 0.6f) && ship.EffectiveClaimRange == 120,
                "Gold improves magnetism while retaining baseline Claim range");
            orb.ReassignOwnership(AntimatterOwnership.Neutral);
            Check(orb.EffectiveAttractionGap == 6 && orb.EffectiveMergeContactSeconds == 1.25f, "Neutral AM receives no faction magnetism bonus");
            orb.ReassignOwnership(AntimatterOwnership.Enemy);
            Check(orb.EffectiveAttractionGap == 6, "Enemy ownership uses enemy research rather than human research");

            Research(home, "teleport-home", 1);
            ship.GlobalPosition = arena.OnSurface(arena.StepToward(home.GlobalPosition, enemy.GlobalPosition, 75), 2);
            home.DepositAntimatter(10000); ship.RespawnAt(ship.GlobalPosition); ship.EnergyRegenFractionPerSecond = 0; ship.HealthRecoveryPerSecond = 0;
            Check(ship.TryActivateTeleport(), "Research unlocks built-in teleport away from home");
            ship.SpendEnergy(1);
            Check(!ship.IsTeleporting, "Using another energy action interrupts teleport");
            Check(ship.TryActivateTeleport(), "Interrupted teleport can restart without cooldown");
            typeof(ShipFlightController).GetField("_damageInvulnerabilityTimer", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(ship, 0f);
            ship.ApplyDamage(1);
            Check(!ship.IsTeleporting, "Incoming damage interrupts return channel");
            Check(ship.TryActivateTeleport(), "Damage interruption does not consume energy or cooldown");
            Input.ActionPress("move_forward"); ship._PhysicsProcess(0.01); Input.ActionRelease("move_forward");
            Check(!ship.IsTeleporting, "Movement input interrupts return channel");
            ship.Velocity = Vector3.Zero;
            Check(ship.TryActivateTeleport(), "Restart after movement interruption");
            ship.GameplayInputBlocked = true; ship._PhysicsProcess(0.1); ship.GameplayInputBlocked = false;
            Check(!ship.IsTeleporting, "Opening an input-owning interface interrupts channel");
            Check(ship.TryActivateTeleport(), "Restart after closing UI");
            var blockedHome = new StaticBody3D { Position = home.GlobalPosition };
            blockedHome.AddChild(new CollisionShape3D { Shape = new SphereShape3D { Radius = 30 } }); game.AddChild(blockedHome);
            await Frames(3);
            int blockedEnergy = ship.Energy;
            ship._PhysicsProcess(3.1);
            Check(!ship.IsTeleporting && ship.TeleportCooldownRemaining == 0 && ship.Energy == blockedEnergy
                && arena.SurfaceDistance(ship.GlobalPosition, home.GlobalPosition) > 30, "Blocked home rejects teleport without cost or cooldown");
            blockedHome.QueueFree(); await Frames(3);
            ship.Velocity = Vector3.Zero;
            Check(ship.TryActivateTeleport(), "Blocked return can retry when a safe arrival becomes available");
            int energy = ship.Energy; int hp = ship.Health;
            ship._PhysicsProcess(1.5);
            Check(ship.IsTeleporting && ship.Energy == energy, "Channel does not charge early");
            ship._PhysicsProcess(1.6);
            Check(!ship.IsTeleporting && arena.SurfaceDistance(ship.GlobalPosition, home.GlobalPosition) < 16, "Completed teleport returns to a safe home-adjacent point");
            Check(ship.Energy == energy - 20 && ship.Health == hp && ship.TeleportCooldownRemaining == 60, "Teleport pays once without healing/refill and starts bronze cooldown");
            Check(!ship.TryActivateTeleport(), "Cooldown rejects a second teleport");

            // Clear unrelated native bodies before a controlled visibility experiment.
            foreach (Node node in game.GetChildren())
                if (node is BaseTacticalDrone or BaseStrikeDrone or AntimatterPickup or MagicCarpetRemastered.Scripts.Enemies.EnemyEcosystemEnemy) node.QueueFree();
            ship.GlobalPosition = arena.OnSurface(home.GlobalPosition + home.GlobalBasis.X * 25, 10);
            rival.GlobalPosition = ship.GlobalPosition + ship.GlobalBasis.X * 15;
            await Frames(3);
            var navigation = game.GetNode<WorldNavigation>("WorldNavigation"); navigation.Refresh();
            var marker = navigation.Markers.Single(m => m.Actor == rival);
            Check(navigation.CanDisplay(marker), "Unobstructed nearby rival appears on radar");
            var blocker = new StaticBody3D { Position = (ship.AimOrigin + rival.GlobalPosition) * 0.5f };
            blocker.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = Vector3.One * 5 } }); game.AddChild(blocker);
            await Frames(3);
            Check(!navigation.CanDisplay(marker), "Occlusion hides the cached rival immediately on both maps");
            blocker.QueueFree(); await Frames(3);
            Check(navigation.CanDisplay(marker), "Rival reappears after line of sight returns");
            if (DisplayServer.GetName() != "headless")
            {
                GetViewport().GetWindow().Size = new Vector2I(960, 540);
                ui.SetOpen(true); ui.Refresh();
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Check(image.SavePng("C:/Users/lblan/AppData/Local/Temp/opencode/AutomaticFleetUi.png") == Error.Ok, "Capture compact automatic fleet UI");
            }
            ui.SetOpen(false); game.QueueFree(); await Frames(5);

            // Real native rigid-body experiment: only researched owned balls close an eight-metre gap.
            var floor = new StaticBody3D { Position = new Vector3(0, -0.5f, 0) };
            floor.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(100, 1, 100) } }); AddChild(floor);
            var magnetHome = new FactionBase { Position = new Vector3(35, 0, 35) }; AddChild(magnetHome); magnetHome.SetPhysicsProcess(false);
            for (int rank = 1; rank <= 3; rank++) Research(magnetHome, "claim-range", rank);
            AntimatterPickup Ball(float x, float z, AntimatterOwnership owner)
            {
                var ball = new AntimatterPickup { AntimatterAmount = 10, Ownership = owner, Position = new Vector3(x, 0.6f, z) };
                AddChild(ball); return ball;
            }
            var ownedA = Ball(-4.5f, 0, AntimatterOwnership.Player); var ownedB = Ball(4.5f, 0, AntimatterOwnership.Player);
            var neutralA = Ball(-4.5f, 20, AntimatterOwnership.Neutral); var neutralB = Ball(4.5f, 20, AntimatterOwnership.Neutral);
            await Frames(90);
            Check(ownedA.GlobalPosition.DistanceTo(ownedB.GlobalPosition) < 8 && neutralA.GlobalPosition.DistanceTo(neutralB.GlobalPosition) > 8.9f,
                "Gold magnetism physically attracts beyond baseline range while neutral balls remain still");
            await Frames(480);
            Check(GetChildren().OfType<AntimatterPickup>().Where(o => o.Ownership == AntimatterOwnership.Player).Sum(o => o.AntimatterAmount) == 20
                && GetChildren().OfType<AntimatterPickup>().Count(o => o.Ownership == AntimatterOwnership.Player && o.IsAvailable) == 1,
                "Enhanced attraction reaches real contact and merges with exact AM conservation");
            foreach (Node node in GetChildren()) node.QueueFree(); await Frames(5);
            GD.Print($"Automatic fleet checks passed: {_checks}"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}

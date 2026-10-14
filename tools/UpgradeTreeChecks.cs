using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Ui;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class UpgradeTreeChecks : Node3D
{
    private int _checks;
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();
    private void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private async Task Frames(int count) { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    private void Research(FactionBase home, string id, int rank)
    {
        home.DepositAntimatter(10000);
        Check(home.Construction.TryStart(UpgradeCatalog.ProjectId(id, rank), out string reason), $"Research {id}/{rank}: {reason}");
        home.Construction.Advance(500);
        Check(home.Construction.UpgradeRank(id) == rank, "Sequential completion grants the exact star once");
    }

    private async Task Run()
    {
        try
        {
            WorldSeeds.UseSeed(7319); PlanetSizes.SelectedIndex = 1;
            var game = GD.Load<PackedScene>("res://scenes/PlanetGame.tscn").Instantiate<MagicCarpetRemastered.Scripts.Game.Game>(); AddChild(game);
            foreach (Node node in game.GetChildren()) { node.SetPhysicsProcess(false); if (node is not CanvasLayer) node.SetProcess(false); }
            var home = game.GetNode<FactionBase>("PlayerBase"); var rival = game.GetNode<FactionBase>("EnemyBase");
            home.Fleet.SetPhysicsProcess(false); rival.Fleet.SetPhysicsProcess(false);
            home.GetNode<BaseDefense>("BaseDefense").SetPhysicsProcess(false); rival.GetNode<BaseDefense>("BaseDefense").SetPhysicsProcess(false);
            var ship = game.GetNode<ShipFlightController>("PlayerShip"); var arena = game.GetNode<HeightmapArena>("HeightmapArena");
            var navigation = game.GetNode<WorldNavigation>("WorldNavigation");
            var ui = game.GetNode<BaseManagement>("BaseManagement");
            await Frames(2);
            Check(UpgradeCatalog.Items.Count == 68 && UpgradeCatalog.Items.Select(item => item.Id).Distinct().Count() == 68, "68 unique upgrade types including Harvester");
            foreach (var slot in new[] { ConstructionSlot.Offense, ConstructionSlot.Defense, ConstructionSlot.Resources, ConstructionSlot.Ship })
                for (int tier = 1; tier <= 5; tier++) Check(UpgradeCatalog.Items.Count(item => item.Slot == slot && item.Tier == tier) == (slot == ConstructionSlot.Ship ? 4 : 3) + (tier == 2 && slot is ConstructionSlot.Ship or ConstructionSlot.Offense or ConstructionSlot.Resources ? 1 : 0), "Tier rows include new researched abilities");
            Check(!home.Construction.TryStart(UpgradeCatalog.ProjectId("repair-matrix", 2), out _), "Cannot skip the first star");
            Check(!home.Construction.TryStart(UpgradeCatalog.ProjectId("particle-cannon", 1), out _), "Pending content cannot take payment");
            for (int rank = 1; rank <= 3; rank++) Research(home, "repair-matrix", rank);
            Check(Mathf.IsEqualApprox(home.EffectiveRepairHealthPerSecond, home.RepairHealthPerSecond * 1.75f), "Gold repair scales once from original tuning");
            int stored = home.StoredAntimatter;
            Check(!home.Construction.TryStart(UpgradeCatalog.ProjectId("repair-matrix", 3), out _) && home.StoredAntimatter == stored, "Gold cannot be repurchased");
            for (int tier = 2; tier <= 4; tier++) Check(home.TryCompleteTierUpgrade(tier), "Fixture reaches the revised unlock tiers");
            foreach (string id in ShipEquipmentCatalog.ItemIds.Where(id => id != "plasma-bolt")) Research(home, id, 1);
            Vector3 atHome = arena.OnSurface(home.GlobalPosition, 6);
            ship.GlobalPosition = atHome;
            Check(ship.TryToggleEquipment("pulse-burst", out _) && ship.TryToggleEquipment("energy-shield", out _) && ship.TryToggleEquipment("dash", out _), "Home plans install automatically");
            Check(ship.EquipmentSlots.SequenceEqual(new[] { "plasma-bolt", "pulse-burst", "energy-shield", "dash" }), "Shared four-item set follows catalog order");
            ship.TrySelectEquipmentSlot(2);
            ship.GlobalPosition = arena.StepToward(atHome, rival.GlobalPosition, 80); ship.GlobalPosition = arena.OnSurface(ship.GlobalPosition, 6);
            var old = ship.EquipmentSlots.ToArray();
            Check(ship.TryToggleEquipment("pulse-burst", out _) && ship.TryToggleEquipment("guardian-drone", out _), "Remote plan swaps researched equipment");
            Check(ship.EquipmentSlots.SequenceEqual(old) && ship.HasPendingLoadout, "Remote planning preserves active weapons and marks pending changes");
            Check(!ship.TryToggleEquipment("pulse-burst", out _), "A fifth planned item is rejected atomically");
            Research(home, "energy-shield", 2);
            Check(ship.GetEquipmentRank("energy-shield") == 1 && ship.GetOwnedEquipmentRank("energy-shield") == 2, "Remote rank completion does not change active equipment");
            typeof(ShipFlightController).GetField("_fireCooldown", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(ship, 0.2f);
            int energy = ship.Energy;
            ship.GlobalPosition = atHome; ship._PhysicsProcess(0);
            Check(!ship.HasPendingLoadout && ship.GetEquipmentRank("energy-shield") == 2 && ship.EquipmentSlots[3] == "guardian-drone", "Home proximity automatically applies the whole plan and latest ranks");
            Check(ship.SelectedEquipmentSlot == 1 && ship.EquipmentSlots[1] == "energy-shield", "Synchronization preserves selected item identity after compaction");
            Check(ship.Energy == energy && Mathf.IsEqualApprox(ship.EquipmentCooldownRemaining("plasma-bolt"), 0.2f), "Synchronization cannot refill energy or reset cooldowns");
            ship.GlobalPosition = arena.OnSurface(arena.StepToward(atHome, rival.GlobalPosition, 80), 6);
            Check(ship.TryToggleEquipment("guardian-drone", out _) && ship.TryToggleEquipment("guardian-drone", out _) && !ship.HasPendingLoadout,
                "Undoing a remote plan clears pending markers");
            Research(home, "energy-grid", 1);
            Check(Mathf.IsEqualApprox(home.EnergyRechargeMultiplier, 1.15f), "Ship passive is effective remotely without occupying a slot");
            Research(home, "cargo-compression", 1); Research(home, "collector-drive", 1); Research(home, "scoop-array", 1);
            var collector = game.GetChildren().OfType<CollectorDrone>().First(drone => drone.UsePlayerAntimatter);
            Check(collector.EffectiveCarryCapacity == 120 && collector.EffectiveScoopRadius > collector.ScoopRadius && home.CollectorSpeedMultiplier == 1.15f,
                "Resource stars alter actual collectors");
            Research(home, "scout-drone", 1); Research(home, "defensive-drone", 1); Research(home, "strike-drone", 1);
            stored = home.StoredAntimatter;
            Check(!home.Construction.TryStart("strike-drone", out _) && home.StoredAntimatter == stored, "Research tile cannot accidentally launch a paid strike");
            home.Fleet._PhysicsProcess(0.01); home.Fleet._PhysicsProcess(20);
            var strike = game.GetChildren().OfType<BaseStrikeDrone>().Single(); strike.SetPhysicsProcess(false);
            Check(strike.Target == rival && strike.Damage == 120 && strike.Health == 30, "Automatic production launches an interceptable researched strike");
            FactionCombat.Damage(strike, 1000, ship); Check(strike.IsQueuedForDeletion(), "Ships can intercept automatic strikes");
            Check(ui.SetOpen(true) && !ui.OpenTargetOrder("scout-drone"), "Automatic fleets have no manual target dropdown");
            var scout = game.GetChildren().OfType<BaseTacticalDrone>().Single(drone => drone.Role == BaseTacticalDrone.DroneRole.Scout); scout.SetPhysicsProcess(false);
            Check(scout.Role == BaseTacticalDrone.DroneRole.Scout && scout.Target is StrategicPoint { Ownership: not AntimatterOwnership.Player }, "Scout immediately targets the nearest non-owned point");
            var site = (StrategicPoint)scout.Target!; site.Advance(100, true, false);
            home.Fleet._PhysicsProcess(1); Check(scout.Target != site, "Fleet retargets scout after its point becomes friendly");
            site.Advance(20, false, true); // Enemy-owned point must also be eligible.
            scout.GlobalPosition = arena.OnSurface(site.GlobalPosition, 5); scout.Target = site; ship.GlobalPosition = atHome;
            var rivalShip = game.GetNode<ShipFlightController>("RivalPilot"); rivalShip.GlobalPosition = arena.OnSurface(rival.GlobalPosition, 6);
            site.Advance(100, false, false); site._PhysicsProcess(10);
            Check(site.Ownership == AntimatterOwnership.Enemy && Math.Abs(site.CaptureProgress - 0.5) < 0.00001, "A lone scout neutralizes enemy sites at half ship speed");
            var second = new BaseTacticalDrone { Role = BaseTacticalDrone.DroneRole.Scout, Home = home, Target = site, Position = scout.Position }; game.AddChild(second); second.SetPhysicsProcess(false);
            site._PhysicsProcess(10); Check(site.Ownership == AntimatterOwnership.Neutral && site.CaptureProgress == 0, "Two scouts do not stack capture speed");
            var opposing = new BaseTacticalDrone { Role = BaseTacticalDrone.DroneRole.Scout, Home = rival, Target = site, Position = scout.Position }; game.AddChild(opposing); opposing.SetPhysicsProcess(false);
            site._PhysicsProcess(10); Check(site.IsContested && site.CaptureProgress == 0, "Opposing capture drones freeze progress");
            opposing.QueueFree(); await Frames(2); site._PhysicsProcess(20);
            Check(site.Ownership == AntimatterOwnership.Player && !site.PlayerPresent, "Scout completes capture without remote ship capture HUD");
            var defender = game.GetChildren().OfType<BaseTacticalDrone>().Single(drone => drone.Role == BaseTacticalDrone.DroneRole.Defender); defender.SetPhysicsProcess(false);
            defender.Target = site;
            defender.GlobalPosition = arena.OnSurface(site.GlobalPosition, 5);
            site.Advance(20, false, true); defender._PhysicsProcess(1);
            Check(defender.Ownership == AntimatterOwnership.Player && arena.SurfaceDistance(defender.GlobalPosition, home.GlobalPosition) < arena.SurfaceDistance(site.GlobalPosition, home.GlobalPosition), "Site loss sends defender toward its original home without ownership transfer");
            defender.GlobalPosition = arena.OnSurface(home.GlobalPosition, 5); defender._PhysicsProcess(0);
            Check(!defender.IsQueuedForDeletion() && defender.Target == home, "Retreating defender stays to protect home instead of disappearing");
            stored = home.StoredAntimatter;
            Check(!home.Construction.TryStartOrder("launch-defensive-drone", site, out _) && home.StoredAntimatter == stored, "Legacy paid deployment is unavailable");
            var hostileGuardian = new GuardianDrone { OwnerPilot = rivalShip, Position = scout.Position };
            game.AddChild(hostileGuardian); hostileGuardian.SetPhysicsProcess(false);
            int friendlyHp = scout.Health;
            typeof(ShipFlightController).GetMethod("DamageEnemiesInRadius", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(ship, new object[] { hostileGuardian.GlobalPosition, 2f, 10 });
            Check(hostileGuardian.Health == 20 && scout.Health == friendlyHp, "Pulse damages hostile deployables without friendly drone damage");
            FactionCombat.Damage(hostileGuardian, 1000, ship);
            Check(hostileGuardian.IsQueuedForDeletion(), "Guardian is destructible rather than invulnerable support");
            for (int rank = 1; rank <= 3; rank++) Research(home, "claim-range", rank);
            ui.SetOpen(false); ship.GlobalPosition = atHome; ship._PhysicsProcess(0);
            Check(ship.TryActivateClaim(), "Gold built-in Claim uses the normal paid action");
            var claim = game.GetChildren().OfType<ClaimProjectile>().Single(); claim.SetPhysicsProcess(false);
            Check(claim.MaxRange == 120 && Mathf.IsEqualApprox(claim.HitRadius, 1.3f) && claim.LifetimeSeconds * claim.Speed >= claim.MaxRange,
                "Gold magnetism research keeps Claim's baseline range and width");
            Research(rival, "scout-drone", 1); rival.DepositAntimatter(10000); int rivalStored = rival.StoredAntimatter;
            rival.Fleet._PhysicsProcess(10);
            Check(rival.StoredAntimatter == rivalStored, "Rival automatic production costs time only");
            var rivalScout = game.GetChildren().OfType<BaseTacticalDrone>().Single(drone => drone.Home == rival && !drone.IsQueuedForDeletion());
            rivalScout.SetPhysicsProcess(false);
            Check(rivalScout.Target is StrategicPoint { Ownership: not AntimatterOwnership.Enemy }, "Rival scout chooses a point not owned by its own faction");
            ship.GlobalPosition = arena.OnSurface(arena.StepToward(atHome, rival.GlobalPosition, 80), 6);
            Check(ship.TryToggleEquipment("guardian-drone", out _) && ship.HasPendingLoadout, "Plan an equipment removal before a remote death");
            typeof(ShipFlightController).GetField("_damageInvulnerabilityTimer", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(ship, 0f);
            ship.ApplyDamage(1000, rivalShip);
            Check(ship.Health == 0 && ship.HasPendingLoadout && !ship.TryToggleEquipment("dash", out _), "Death retains pending plans but blocks editing while down");
            ship.RespawnAt(atHome);
            Check(!ship.HasPendingLoadout && !ship.IsEquipmentInstalled("guardian-drone") && ship.GetOwnedEquipmentRank("guardian-drone") == 1,
                "Home respawn automatically installs the surviving plan without losing equipment ownership");
            foreach (var point in game.GetChildren().OfType<StrategicPoint>()) point.Advance(100, true, false);
            var returningScout = new BaseTacticalDrone { Role = BaseTacticalDrone.DroneRole.Scout, Home = home,
                Position = arena.OnSurface(arena.StepToward(home.GlobalPosition, site.GlobalPosition, 40), 5) };
            game.AddChild(returningScout); returningScout.SetPhysicsProcess(false); returningScout._PhysicsProcess(0);
            Check(returningScout.Target == null && !returningScout.IsQueuedForDeletion(), "Scout heads home while every point is friendly");
            site.Advance(20, false, true);
            float beforeRetarget = arena.SurfaceDistance(returningScout.GlobalPosition, site.GlobalPosition);
            returningScout._PhysicsProcess(1);
            Check(returningScout.Target == site && arena.SurfaceDistance(returningScout.GlobalPosition, site.GlobalPosition) < beforeRetarget - 10,
                "A returning scout resumes its mission when a point becomes enemy-owned");
            long spent = home.SpentAntimatter; GetTree().Paused = true;
            Check(!home.Construction.TryStartOrder("launch-scout-drone", null, out _) && home.SpentAntimatter == spent, "Pause rejects deployment payments"); GetTree().Paused = false;
            ui.SetOpen(false); game.QueueFree(); await Frames(5);
            GD.Print($"Upgrade tree checks passed: {_checks}"); GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}

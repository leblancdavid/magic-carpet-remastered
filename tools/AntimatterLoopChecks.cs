using System;
using System.Reflection;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

// Run AntimatterLoopChecks.tscn headlessly to exercise real Godot nodes and transfers.
public partial class AntimatterLoopChecks : Node3D
{
    private int _checks;

    public override void _Ready()
    {
        Callable.From(RunChecks).CallDeferred();
    }

    private void RunChecks()
    {
        try
        {
            CheckPlayerCannotCollect();
            ClearActors();
            CheckDroneDelivery();
            ClearActors();
            CheckBaseEconomy();
            ClearActors();
            CheckTierAndSpendingFoundation();
            ClearActors();
            CheckDroneArrival();
            ClearActors();
            CheckDronePop();
            ClearActors();
            CheckEnemyTransfers();
            ClearActors();
            CheckLegacyMonster();
            ClearActors();
            CheckClaim();
            GD.Print($"Antimatter loop checks passed: {_checks}");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }

    private T AddActor<T>(T actor) where T : Node
    {
        AddChild(actor);
        actor.SetPhysicsProcess(false);
        actor.SetProcess(false);
        return actor;
    }

    private void ClearActors()
    {
        foreach (Node child in GetChildren())
        {
            child.Free();
        }
    }

    private void CheckPlayerCannotCollect()
    {
        var player = AddActor(new ShipFlightController());
        player.GlobalPosition = new Vector3(0, 20, 0);
        var tracker = AddActor(new WorldAntimatterTracker());
        int claimed = player.EnergyCapacity;
        int antimatter = player.Energy;
        foreach (AntimatterOwnership owner in new[] { AntimatterOwnership.Neutral, AntimatterOwnership.Player, AntimatterOwnership.Enemy })
        {
            var orb = AddActor(new AntimatterPickup { Ownership = owner, AntimatterAmount = 10 });
            orb.GlobalPosition = player.GlobalPosition + new Vector3(6, 0, 0);
            Vector3 position = orb.GlobalPosition;
            orb._PhysicsProcess(1.0 / 60);
            Check(orb.GlobalPosition.IsEqualApprox(position) && orb.LinearVelocity.IsZeroApprox(), $"{owner} antimatter is not attracted to player");
            orb.GlobalPosition = player.GlobalPosition;
            orb._PhysicsProcess(1.0 / 60);
            Invoke(orb, "OnBodyEntered", player);
            Check(orb.IsAvailable && orb.AntimatterAmount == 10, $"Player proximity/contact cannot absorb {owner} antimatter");
            Check(player.EnergyCapacity == claimed && player.Energy == antimatter, "Player contact grants no energy limit or refill");
        }
        tracker.Refresh();
        int total = tracker.TotalAntimatter;
        Check(total == 30 && tracker.PlayerControlledAntimatter == 10, "Uncollected owned antimatter stays counted in world exactly once");
        player.SpendEnergy(5);
        Invoke(player, "RegenerateEnergy", 1.0f);
        tracker.Refresh();
        Check(tracker.TotalAntimatter == total, "Equipment spending and regeneration do not change world antimatter");

        var consumed = AddActor(new AntimatterPickup { AntimatterAmount = 9 });
        Check(consumed.TakeAntimatter(9) == 9 && !consumed.IsAvailable && consumed.TakeAntimatter(9) == 0, "Consumed orb cannot transfer twice");
    }

    private void CheckDroneDelivery()
    {
        var player = AddActor(new ShipFlightController());
        player.GlobalPosition = new Vector3(100, 20, 100);
        var hub = AddActor(new FactionBase { InitialStoredAntimatter = 0 });
        var drone = AddActor(new CollectorDrone { CarryCapacity = 2 });
        drone.GlobalPosition = new Vector3(8, 4.5f, 0);
        var first = AddActor(new AntimatterPickup { Ownership = AntimatterOwnership.Player, AntimatterAmount = 8 });
        first.GlobalPosition = new Vector3(8, 0, 0);
        var second = AddActor(new AntimatterPickup { Ownership = AntimatterOwnership.Player, AntimatterAmount = 11 });
        second.GlobalPosition = new Vector3(9, 0, 0);
        var tracker = AddActor(new WorldAntimatterTracker());
        int total = tracker.TotalAntimatter;
        int claimed = player.EnergyCapacity;
        for (int i = 0; i < 240 && hub.StoredAntimatter < 19; i++)
        {
            drone._PhysicsProcess(1.0 / 60);
            first._PhysicsProcess(1.0 / 60);
            second._PhysicsProcess(1.0 / 60);
            tracker.Refresh();
            Check(tracker.TotalAntimatter == total, "Scoop/transit/deposit conserves antimatter");
        }
        Check(hub.StoredAntimatter == 19 && drone.CarriedCount == 0, "Multiple orbs scoop, dock, and deposit without enumeration errors");
        Check(player.EnergyCapacity == claimed + 19 && player.Energy == 0, "Base delivery raises energy limit without refilling current energy");

        hub.DepositAntimatter(hub.AntimatterStorageCapacity - hub.StoredAntimatter - 4);
        var partial = AddActor(new AntimatterPickup { Ownership = AntimatterOwnership.Player, AntimatterAmount = 10 });
        partial.GlobalPosition = drone.GlobalPosition - Vector3.Up * drone.HoverHeight;
        Invoke(drone, "Scoop", partial);
        tracker.Refresh();
        total = tracker.TotalAntimatter;
        Invoke(drone, "DepositAll");
        tracker.Refresh();
        Check(hub.StoredAntimatter == hub.AntimatterStorageCapacity && partial.AntimatterAmount == 6, "Partial hub deposit removes accepted amount");
        Check(partial.IsCarried && partial.Ownership == AntimatterOwnership.Player && drone.CarriedCount == 1, "Partial delivery retains owned cargo while full");
        Check(tracker.TotalAntimatter == total, "Partial hub deposit conserves antimatter");

        var waiting = AddActor(new AntimatterPickup { Ownership = AntimatterOwnership.Player, AntimatterAmount = 3 });
        Invoke(drone, "Scoop", waiting);
        Check(!waiting.IsCarried, "Full storage prevents another scoop");
        Invoke(drone, "DepositAll");
        Check(partial.AntimatterAmount == 6 && drone.CarriedCount == 1, "Repeated full deposits preserve cargo");
        Check(hub.TrySpendAntimatter(6), "Payment opens storage room");
        Invoke(drone, "DepositAll");
        Check(partial.AntimatterAmount == 0 && drone.CarriedCount == 0 && hub.StoredAntimatter == hub.AntimatterStorageCapacity,
            "Waiting cargo deposits when construction opens room");

        var distant = AddActor(new AntimatterPickup { Ownership = AntimatterOwnership.Player });
        distant.GlobalPosition = drone.GlobalPosition + new Vector3(drone.MaxSeekRadius + 1, 0, 0);
        Invoke(drone, "HandleSeeking", 0.1f);
        Check(!distant.IsCarried, "Seek radius excludes distant antimatter");
    }

    private void CheckDronePop()
    {
        AddActor(new FactionBase { InitialStoredAntimatter = 0, IsPlayerBase = false });
        var drone = AddActor(new CollectorDrone { UsePlayerAntimatter = false });
        drone.GlobalPosition = new Vector3(5, 10, 5);
        var orb = AddActor(new AntimatterPickup { Ownership = AntimatterOwnership.Enemy, AntimatterAmount = 17 });
        Invoke(drone, "Scoop", orb);
        var tracker = AddActor(new WorldAntimatterTracker());
        int total = tracker.TotalAntimatter;
        orb._PhysicsProcess(1.0 / 60);
        Check(orb.GlobalPosition.Y < drone.GlobalPosition.Y, "Cargo sits below drone mesh");
        drone.ApplyDamage(drone.MaxHealth);
        tracker.Refresh();
        Check(orb.AntimatterAmount == 0 && orb.IsQueuedForDeletion() && drone.CarriedCount == 0, "Pop consumes cargo exactly once for fragmentation");
        Check(tracker.TotalAntimatter == total && tracker.NeutralLooseAntimatter == 17, "Pop changes ownership without creating antimatter");
    }

    private void CheckBaseEconomy()
    {
        // Base first: initial storage must contribute even before any delivery.
        var hub = AddActor(new FactionBase { InitialStoredAntimatter = 120, MaxHealth = 100, Tier1StorageCapacity = 120 });
        var player = AddActor(new ShipFlightController());
        var tracker = AddActor(new WorldAntimatterTracker());
        Check(player.EnergyCapacity == 120 && player.Energy == 40, "Initial storage alone backs energy with base-first spawn");
        Check(hub.Tier == 1, "Full storage does not automatically buy tiers");
        Invoke(player, "RegenerateEnergy", 100.0f);
        hub.ApplyDamage(50);
        tracker.Refresh();
        Check(hub.Health == 50 && hub.EffectiveStorageCapacity == 60 && hub.StoredAntimatter == 60, "Half health halves capacity and releases excess storage");
        Check(hub.Tier == 1 && player.EnergyCapacity == 60 && player.Energy == 60, "Storage damage clamps energy without altering purchased tier");
        Check(tracker.TotalAntimatter == 120 && tracker.NeutralLooseAntimatter == 60, "Base damage conserves antimatter and neutralizes spill");
        Check(hub.DepositAntimatter(10) == 0, "Damage-reduced capacity rejects further deposits when full");
        int withdrawn = hub.WithdrawAntimatter(15);
        var cargo = AddActor(new AntimatterPickup { AntimatterAmount = withdrawn });
        tracker.Refresh();
        Check(withdrawn == 15 && player.EnergyCapacity == 45 && tracker.TotalAntimatter == 120, "Withdrawal lowers energy limit while transferred physical antimatter stays accounted");
        var drone = AddActor(new CollectorDrone { CarryCapacity = 1 });
        cargo.ReassignOwnership(AntimatterOwnership.Player);
        Invoke(drone, "Scoop", cargo);
        Invoke(drone, "DepositAll");
        tracker.Refresh();
        Check(hub.StoredAntimatter == 60 && player.EnergyCapacity == 60 && player.Energy == 45 && tracker.TotalAntimatter == 120, "Re-delivery raises limit without refill or permanently stacking it");
        player.SpendEnergy(player.Energy);
        Invoke(player, "RegenerateEnergy", 1.0f);
        Check(player.Energy == 2, "Regeneration uses reduced storage-backed limit");
        // Recover a real damage spill through the same bolt/drone path as play.
        AntimatterPickup? spill = null;
        foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter"))
        {
            if (node is AntimatterPickup orb && orb.IsAvailable && orb.Name == "BaseAntimatterSpill")
            {
                spill = orb;
                break;
            }
        }
        Check(spill != null, "Damage creates a recoverable loose globe");
        var withdrawnOrb = AddActor(new AntimatterPickup { AntimatterAmount = hub.WithdrawAntimatter(spill!.AntimatterAmount) });
        withdrawnOrb.GlobalPosition = new Vector3(100, 10, 100);
        var bolt = AddActor(new ClaimProjectile());
        bolt.GlobalPosition = spill.GlobalPosition;
        bolt.Launch(Vector3.Right);
        bolt._PhysicsProcess(0.1);
        Check(spill.Ownership == AntimatterOwnership.Player && bolt.IsQueuedForDeletion(), "Actual claim bolt can reclaim a hub spill");
        Invoke(drone, "Scoop", spill);
        Invoke(drone, "DepositAll");
        tracker.Refresh();
        Check(hub.StoredAntimatter == 60 && player.EnergyCapacity == 60 && tracker.TotalAntimatter == 120,
            "Drone can re-deliver a reclaimed damage spill up to damaged capacity");
        hub.ApplyDamage(1000);
        tracker.Refresh();
        Check(hub.StoredAntimatter == 0 && hub.EffectiveStorageCapacity == 0 && hub.Tier == 0, "Destruction empties storage and leaves tier-zero ruins");
        Check(player.EnergyCapacity == 0 && player.Energy == 0, "Base destruction leaves no unsupported starting allowance");
        Check(tracker.TotalAntimatter == 120 && tracker.NeutralLooseAntimatter == 120, "Destruction releases the entire remaining reserve");
        hub.ApplyDamage(1000);
        Check(hub.DepositAntimatter(10) == 0 && hub.WithdrawAntimatter(10) == 0, "Destroyed hub rejects storage transfers");
        tracker.Refresh();
        Check(tracker.TotalAntimatter == 120, "Repeated destruction cannot duplicate antimatter");

        ClearActors();
        player = AddActor(new ShipFlightController());
        Check(player.EnergyCapacity == 0 && player.Energy == 0, "No home base means no energy allowance");
        hub = AddActor(new FactionBase { InitialStoredAntimatter = 37, Tier1StorageCapacity = 37, MaxHealth = 7 });
        var enemyBase = AddActor(new FactionBase { IsPlayerBase = false, InitialStoredAntimatter = 19, Tier1StorageCapacity = 19, MaxHealth = 7 });
        tracker = AddActor(new WorldAntimatterTracker());
        Check(player.EnergyCapacity == 37 && player.Energy == 37 && hub.StoredAntimatter == 37, "Player-first spawn initializes energy once from starting storage");
        hub.ApplyDamage(6);
        tracker.Refresh();
        Check(hub.EffectiveStorageCapacity == 6 && hub.StoredAntimatter == 6 && player.EnergyCapacity == 6, "Energy limit follows storage exactly after damage");
        enemyBase.ApplyDamage(1);
        tracker.Refresh();
        Check(enemyBase.StoredAntimatter == 17 && tracker.TotalAntimatter == 56 && tracker.NeutralLooseAntimatter == 33, "Uneven capacities round consistently and rival damage conserves antimatter");
        Check(player.EnergyCapacity == 6, "Rival storage changes cannot alter player energy limit");
        enemyBase.ApplyDamage(100);
        hub.ApplyDamage(100);
        tracker.Refresh();
        Check(tracker.TotalAntimatter == 56 && tracker.NeutralLooseAntimatter == 56, "Remainders survive both hub destruction spills exactly");
        foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter"))
        {
            if (node is not AntimatterPickup orb || orb.IsQueuedForDeletion()) continue;
            Check(orb.AntimatterAmount > 0 && orb.Ownership == AntimatterOwnership.Neutral && !orb.IsCarried
                && orb.Freeze == orb.IsDropPending && orb.IsAvailable != orb.IsDropPending,
                "Base spill reserves positive neutral antimatter and becomes physical/claimable only on release");
            Check(new Vector2(orb.GlobalPosition.X, orb.GlobalPosition.Z).Length() > 6 + orb.Radius - 0.01f,
                "Base spill clears the solid keep footprint");
        }
    }

    private void CheckTierAndSpendingFoundation()
    {
        var hub = AddActor(new FactionBase { InitialStoredAntimatter = 120 });
        var player = AddActor(new ShipFlightController { InitialEnergy = 120 });
        var tracker = AddActor(new WorldAntimatterTracker());
        Check(!hub.TryCompleteTierUpgrade(3) && !hub.TryCompleteTierUpgrade(1), "Tier completions cannot skip or repeat tiers");
        Check(!hub.TrySpendAntimatter(121) && !hub.TrySpendAntimatter(0) && !hub.TrySpendAntimatter(-10), "Unaffordable/nonpositive payments are atomic rejections");
        Check(hub.StoredAntimatter == 120 && hub.SpentAntimatter == 0 && player.Energy == 120, "Rejected spending preserves storage and energy");
        player.SpendEnergy(5);
        tracker.Refresh();
        Check(hub.StoredAntimatter == 120 && tracker.TotalAntimatter == 120 && tracker.SpentAntimatter == 0, "Equipment spends rechargeable energy only");
        Check(hub.TrySpendAntimatter(100), "Construction foundation accepts full affordable payment");
        tracker.Refresh();
        Check(hub.Tier == 1 && hub.StoredAntimatter == 20 && player.EnergyCapacity == 20 && player.Energy == 20, "Payment immediately lowers limit/clamps energy without demotion");
        Check(tracker.TotalAntimatter == 20 && tracker.SpentAntimatter == 100, "Intentional expenditure is a separate sink, never physical world antimatter");
        Check(hub.TryCompleteTierUpgrade(2) && hub.AntimatterStorageCapacity == 350, "Sequential explicit completion raises live capacity");
        Check(!hub.TryCompleteTierUpgrade(2) && hub.StoredAntimatter == 20 && player.EnergyCapacity == 20, "Duplicate completion rejected and completion never refunds investment");
        player.SpendEnergy(10);
        Check(hub.DepositAntimatter(100) == 100 && player.EnergyCapacity == 120 && player.Energy == 10, "Delivery raises limit without current-energy refill");
        Invoke(player, "RegenerateEnergy", 1.0f);
        Check(player.Energy == 14, "Recharge is configurable fraction of retained storage");
        hub.ApplyDamage(70);
        hub._PhysicsProcess(100);
        Check(hub.Tier == 2 && hub.AntimatterStorageCapacity == 350, "Legacy HP repair cannot advance explicit tiers");
        for (int tier = 3; tier <= FactionBase.MaximumTier; tier++)
            Check(hub.TryCompleteTierUpgrade(tier), "Foundation supports all five sequential tiers");
        Check(hub.AntimatterStorageCapacity == 1100 && !hub.TryCompleteTierUpgrade(6), "Tier five is maximum with no separate energy power cap");
        hub.DepositAntimatter(1100);
        Check(player.EnergyCapacity == 1100 && hub.Tier == 5, "Storage-backed energy can exceed legacy 400 cap");
        hub.TrySpendAntimatter(1100);
        Invoke(player, "RegenerateEnergy", 100.0f);
        player.RespawnAt(player.GlobalPosition);
        Check(player.EnergyCapacity == 0 && player.Energy == 0 && hub.Tier == 5, "Empty storage prevents recharge/respawn allowance without losing tier");
        hub.ApplyDamage(10000);
        Check(!hub.TryCompleteTierUpgrade(1), "Destroyed bases cannot recover through upgrade completion");
    }

    private void CheckEnemyTransfers()
    {
        var hub = AddActor(new FactionBase { IsPlayerBase = false, InitialStoredAntimatter = 0 });
        var pilot = AddActor(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.RivalShip });
        Check(pilot.AntimatterReserve == 0, "Pilot spawning does not mint antimatter");
        var orb = AddActor(new AntimatterPickup { AntimatterAmount = 19 });
        var tracker = AddActor(new WorldAntimatterTracker());
        int total = tracker.TotalAntimatter;
        pilot._PhysicsProcess(0.01);
        tracker.Refresh();
        Check(orb.AntimatterAmount == 0 && pilot.AntimatterReserve + hub.StoredAntimatter == 19, "Pilot acquires and absorbs neutral orb");
        Check(tracker.TotalAntimatter == total, "Pilot absorption/deposit conserves antimatter");
        pilot._PhysicsProcess(pilot.AttackCooldownSeconds + 0.01);
        Check(pilot.AntimatterReserve == 0 && hub.StoredAntimatter == 19, "Pilot docks after collection cooldown");
        pilot.GlobalPosition = new Vector3(100, 0, 0);

        var thief = AddActor(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.AntimatterThief });
        var owned = AddActor(new AntimatterPickup { Ownership = AntimatterOwnership.Player, AntimatterAmount = 15 });
        thief._PhysicsProcess(0.01);
        thief._PhysicsProcess(thief.AttackCooldownSeconds + 0.01);
        Check(owned.AntimatterAmount == 0 && thief.AntimatterReserve == 15 && hub.StoredAntimatter == 19,
            "Territorial scavenger retains stolen antimatter rather than funding the rival");
        tracker.Refresh(); int thiefTotal = tracker.TotalAntimatter;
        Check(tracker.NeutralCreatureAntimatter == 15, "Scavenged reserves belong to the neutral ecosystem");
        thief.ApplyDamage(1000); tracker.Refresh();
        Check(tracker.TotalAntimatter == thiefTotal && tracker.NeutralLooseAntimatter == 15,
            "Scavenger death releases exact neutral salvage");

        pilot.AbsorbAntimatter(19);
        tracker.Refresh();
        total = tracker.TotalAntimatter;
        int rivalTotal = tracker.EnemyControlledAntimatter;
        pilot.ApplyDamage(1000);
        tracker.Refresh();
        Check(pilot.AntimatterReserve == 0 && tracker.TotalAntimatter == total, "Death drops preserve remainder and clear reserve");
        Check(tracker.EnemyControlledAntimatter == rivalTotal, "Pilot drops retain rival ownership");
        pilot.ApplyDamage(1000);
        tracker.Refresh();
        Check(tracker.TotalAntimatter == total, "Repeated death damage cannot duplicate drops");
    }

    private void CheckLegacyMonster()
    {
        var monster = AddActor(new SimpleMonster { AntimatterDroppedOnDeath = 17 });
        var orb = AddActor(new AntimatterPickup { AntimatterAmount = 8 });
        var tracker = AddActor(new WorldAntimatterTracker());
        int total = tracker.TotalAntimatter;
        monster.AbsorbAntimatter(orb.TakeAntimatter(8));
        tracker.Refresh();
        Check(tracker.TotalAntimatter == total, "Legacy monster absorption stays counted");
        monster.ApplyDamage(1000);
        tracker.Refresh();
        Check(monster.AntimatterReserve == 0 && tracker.TotalAntimatter == total && tracker.NeutralLooseAntimatter == total, "Legacy monster death returns initial and absorbed antimatter exactly");
    }

    private void CheckDroneArrival()
    {
        var drone = AddActor(new CollectorDrone());
        drone.GlobalPosition = new Vector3(10, 6, 10);
        Vector3 target = drone.GlobalPosition + new Vector3(0.02f, 0, 0);
        Invoke(drone, "MoveToward", target, 1.0f);
        Check(drone.GlobalPosition.IsEqualApprox(target), "Drone capped step arrives exactly at target");
        Check((-drone.GlobalBasis.Z).Dot(Vector3.Right) > 0.99f, "Drone turns before exact arrival without a coincident LookAt");
        Basis facing = drone.GlobalBasis;
        Invoke(drone, "MoveToward", target, 1.0f);
        Check(drone.GlobalBasis.IsEqualApprox(facing), "Drone already at target keeps its facing");
        target += Vector3.Up;
        Invoke(drone, "MoveToward", target, 1.0f);
        Check(drone.GlobalPosition.IsEqualApprox(target) && drone.GlobalBasis.IsEqualApprox(facing), "Vertical-only drone movement preserves facing without invalid up-vector LookAt");
    }

    private void CheckClaim()
    {
        var arena = AddActor(new HeightmapArena { CellsPerSide = 24, CellSize = 2, HeightScale = 0, WaterHeight = -20 });
        arena.FlattenTerrain(Vector3.Zero, 100000, 0);
        arena.RaiseTerrain(new Vector3(8, 0, 0), 4, 5);
        var bolt = AddActor(new ClaimProjectile());
        bolt.GlobalPosition = new Vector3(-8, 4, 0);
        bolt.Launch(new Vector3(1, -1, 0));
        bolt._PhysicsProcess(0.2);
        Check(bolt.IsFollowingTerrain, "Downward claim bolt lands and starts following terrain");
        float x = bolt.GlobalPosition.X;
        bolt._PhysicsProcess(0.2);
        Check(bolt.GlobalPosition.X > x && Mathf.Abs(bolt.GlobalPosition.Z) < 0.001f, "Terrain-following bolt keeps straight horizontal heading");
        Check(Mathf.Abs(bolt.GlobalPosition.Y - arena.HeightAt(bolt.GlobalPosition.X, 0) - bolt.SurfaceClearance) < 0.001f, "Bolt climbs raised terrain at fixed clearance");
        arena.ApplyCrater(bolt.GlobalPosition + new Vector3(2, 0, 0), 4, 6);
        bolt._PhysicsProcess(0.05);
        Check(Mathf.Abs(bolt.GlobalPosition.Y - arena.HeightAt(bolt.GlobalPosition.X, 0) - bolt.SurfaceClearance) < 0.001f, "Bolt samples deformed terrain before collision rebuild");
        bolt.Free();

        arena.FlattenTerrain(Vector3.Zero, 100000, 0);
        // Add farther orb first to catch group-order selection, then skip owned,
        // carried, spent, and off-path orbs before hitting the nearer rival.
        var farther = AddActor(new AntimatterPickup { AntimatterAmount = 9 });
        farther.GlobalPosition = new Vector3(9, 12, 0);
        farther._PhysicsProcess(0);
        Check(Mathf.Abs(farther.GlobalPosition.Y - farther.Radius) < 0.001f, "Unclaimed antimatter touches terrain for claim");
        var owned = AddActor(new AntimatterPickup { Ownership = AntimatterOwnership.Player });
        owned.GlobalPosition = new Vector3(1, 0.9f, 0);
        var carried = AddActor(new AntimatterPickup());
        carried.GlobalPosition = new Vector3(2, 0.9f, 0);
        carried.BeginCarry(this, Vector3.Zero);
        var spent = AddActor(new AntimatterPickup());
        spent.GlobalPosition = new Vector3(3, 0.9f, 0);
        spent.TakeAntimatter(spent.AntimatterAmount);
        var offPath = AddActor(new AntimatterPickup());
        offPath.GlobalPosition = new Vector3(4, 0.9f, 4);
        var near = AddActor(new AntimatterPickup { Ownership = AntimatterOwnership.Enemy, AntimatterAmount = 17 });
        near.GlobalPosition = new Vector3(6, 1.3f, 0);
        var tracker = AddActor(new WorldAntimatterTracker());
        int total = tracker.TotalAntimatter;
        int playerTotal = tracker.PlayerControlledAntimatter;
        bolt = AddActor(new ClaimProjectile());
        bolt.GlobalPosition = new Vector3(0, 0.9f, 0);
        bolt.Launch(Vector3.Right);
        int claimed = 0;
        bolt.AntimatterClaimed += amount => claimed += amount;
        bolt._PhysicsProcess(0.3);
        tracker.Refresh();
        Check(near.Ownership == AntimatterOwnership.Player && near.AntimatterAmount == 17 && claimed == 17, "Swept hit claims first rival globe without consuming its antimatter");
        Check(farther.Ownership == AntimatterOwnership.Neutral && offPath.Ownership == AntimatterOwnership.Neutral && carried.Ownership == AntimatterOwnership.Neutral, "Bolt ignores off-path/carried globes and stops at first hit");
        Check(bolt.IsQueuedForDeletion() && tracker.TotalAntimatter == total && tracker.PlayerControlledAntimatter == playerTotal + 17, "Claim conserves world antimatter and terminates bolt");
        bolt._PhysicsProcess(0.3);
        Check(claimed == 17, "Claim cannot repeat after impact");

        bolt = AddActor(new ClaimProjectile());
        bolt.GlobalPosition = new Vector3(7, 0.9f, 0);
        bolt.Launch(Vector3.Right);
        bolt._PhysicsProcess(0.1);
        Check(farther.Ownership == AntimatterOwnership.Player && bolt.IsQueuedForDeletion(), "Neutral globe is also claimable by bolt impact");

        bolt = AddActor(new ClaimProjectile { MaxRange = 3 });
        bolt.GlobalPosition = new Vector3(0, 10, -10);
        bolt.Launch(Vector3.Right);
        bolt._PhysicsProcess(1);
        Check(bolt.IsQueuedForDeletion() && Mathf.Abs(bolt.GlobalPosition.X - 3) < 0.001f, "Miss expires at bounded range");
        bolt = AddActor(new ClaimProjectile { LifetimeSeconds = 0.1f });
        bolt.GlobalPosition = new Vector3(0, 10, -10);
        bolt.Launch(Vector3.Up);
        bolt._PhysicsProcess(1);
        Check(bolt.IsQueuedForDeletion() && !bolt.IsFollowingTerrain, "Airborne vertical miss expires safely by lifetime");

        arena.WaterHeight = 2;
        bolt = AddActor(new ClaimProjectile());
        bolt.GlobalPosition = new Vector3(-10, 0, -10);
        bolt.Launch(Vector3.Right);
        bolt._PhysicsProcess(0.02);
        Check(Mathf.Abs(bolt.GlobalPosition.Y - 2 - bolt.SurfaceClearance) < 0.001f, "Bolt skims water rather than submerged terrain");

        var hub = AddActor(new FactionBase { InitialStoredAntimatter = 200 });
        var player = AddActor(new ShipFlightController());
        Check(hub.AvailableStorage == 0, "Claim casting fixture has full storage");
        Check(InputMap.ActionHasEvent("activate_claim", new InputEventMouseButton { ButtonIndex = MouseButton.Right }), "RMB binds claim");
        Check(!InputMap.HasAction("cast_terrain") && !InputMap.HasAction("terrain_raise"), "Terrain tool input is hidden");
        for (int i = 0; i < 5; i++) Invoke(player, "CycleQuickEquipment");
        Check(player.PrimaryEquipmentName == "Plasma Bolt", "Tab cycles only the installed starter item");
        int antimatter = player.Energy;
        Invoke(player, "CastClaimEquipment");
        Check(player.Energy == antimatter - player.ClaimEquipmentEnergyCost, "Casting a miss still spends claim antimatter");
        foreach (Node child in GetChildren())
        {
            if (child is ClaimProjectile projectile) projectile.SetPhysicsProcess(false);
        }
    }

    private static void Invoke(object target, string method, params object[] arguments)
    {
        MethodInfo info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing method {method}");
        info.Invoke(target, arguments);
    }

    private void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
        _checks++;
    }
}

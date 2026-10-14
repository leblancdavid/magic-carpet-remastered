using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.Enemies;
using System.Reflection;

namespace MagicCarpetRemastered.Tools;

public partial class BaseConstructionChecks : Node3D
{
    private int _checks;
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();

    private async Task Run()
    {
        try
        {
            CheckLiveBalance();
            Clear();
            CheckSlotsAndRefunds();
            Clear();
            CheckSiegeAndRebuilding();
            Clear();
            CheckSingleTierAndRebuildLoss();
            Clear();
            CheckRivalRefund();
            Clear();
            await CheckRealTimeAndPause();
            Clear();
            CheckSphericalSpills();
            GD.Print($"Base construction checks passed: {_checks} assertions.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GetTree().Paused = false;
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }

    private T Add<T>(T actor) where T : Node
    {
        // Fixed diagnostic layers/capacities isolate lifecycle and rounding
        // expectations from live balance. Integrated suites use live defaults.
        if (actor is FactionBase hub)
        {
            if (hub.MaxHealth == 400) hub.MaxHealth = 140;
            hub.Tier1StorageCapacity = 120; hub.Tier2StorageCapacity = 200;
            hub.Tier3StorageCapacity = 300; hub.Tier4StorageCapacity = 400; hub.Tier5StorageCapacity = 500;
        }
        AddChild(actor);
        actor.SetPhysicsProcess(false);
        return actor;
    }
    private void Clear() { foreach (Node child in GetChildren()) child.Free(); }
    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }
    private int LooseTotal(AntimatterOwnership owner) => GetTree().GetNodesInGroup("loose_antimatter")
        .OfType<AntimatterPickup>().Where(p => !p.IsQueuedForDeletion()
            && p.Ownership == owner).Sum(p => p.AntimatterAmount);
    private void Balanced(WorldAntimatterTracker tracker, long supplied)
    {
        tracker.Refresh();
        Check(tracker.TotalAntimatter + tracker.NetCommittedAntimatter == supplied,
            "Physical total plus net payments equals supplied resources");
    }

    private void CheckSlotsAndRefunds()
    {
        var hub = Add(new FactionBase { InitialStoredAntimatter = 120, Tier2UpgradeCost = 40, Tier2UpgradeSeconds = 1 });
        var player = Add(new ShipFlightController { InitialEnergy = 120 });
        var tracker = Add(new WorldAntimatterTracker());
        var build = hub.Construction;
        Check(!build.Register(new("invalid", "Invalid", ConstructionSlot.Base, 5, 1)), "Base slot uses guarded tier definitions only");
        Check(!build.Register(new("invalid", "Invalid", ConstructionSlot.Ship, -1, 1)), "Reject invalid catalog cost");
        Check(!build.Register(new("invalid", "Invalid", ConstructionSlot.Ship, 1, double.NaN)), "Reject invalid duration");
        Check(!build.TryStart("unknown", out _), "Unknown project cannot spend");
        foreach (var slot in new[] { ConstructionSlot.Defense, ConstructionSlot.Resources, ConstructionSlot.Offense, ConstructionSlot.Ship })
        {
            var definition = new ConstructionDefinition(slot.ToString(), slot.ToString(), slot, 10, 1);
            Check(build.Register(definition) && !build.Register(definition), "Catalog registration cannot replace a live definition");
            Check(build.TryStart(definition.Id, out _), "Independent category starts remotely");
            Check(!build.TryStart(definition.Id, out _), "Same project cannot be paid twice");
        }
        Check(build.TryStartNextTier(out _) && !build.TryStartNextTier(out _), "Only one base project can run");
        Check(build.ActiveInvestment == 80 && hub.StoredAntimatter == 40 && player.Energy == 40 && player.EnergyCapacity == 40,
            "All five categories pay immediately and clamp energy");
        Balanced(tracker, 120);
        var defense = build.GetActive(ConstructionSlot.Defense)!;
        hub.DepositAntimatter(80); // Simulate deliveries refilling space before cancellation.
        Check(build.Cancel(ConstructionSlot.Defense, defense.InstanceId), "Cancel an active project");
        Check(defense.State == ConstructionState.Cancelled && !build.Cancel(ConstructionSlot.Defense, defense.InstanceId),
            "Cancellation has exactly one terminal outcome");
        Check(hub.StoredAntimatter == 120 && LooseTotal(AntimatterOwnership.Player) == 10,
            "Full-storage cancellation preserves exact player-owned overflow");
        Balanced(tracker, 200);
        hub.WithdrawAntimatter(10);
        Check(build.TryStart("Defense", out _), "Cancelled slot can restart");
        Check(!build.Cancel(ConstructionSlot.Defense, defense.InstanceId), "Stale cancellation cannot target replacement");
        var replacement = build.GetActive(ConstructionSlot.Defense)!;
        hub._PhysicsProcess(0.4);
        Check(replacement.Progress > 0.39 && replacement.State == ConstructionState.Active, "Projects expose intermediate progress");
        hub._PhysicsProcess(0.6);
        Check(hub.Tier == 2 && hub.HighestPurchasedTier == 2 && hub.StoredAntimatter == 100,
            "Tier completion increases purchased benefit without returning payment");
        Check(build.ActiveInvestment == 0 && build.CompletedInvestment == 80 && replacement.State == ConstructionState.Completed,
            "Concurrent categories finish once and clear all slots");
        Check(build.IsCompleted("Ship") && !build.TryStart("Ship", out _) && !build.Cancel(ConstructionSlot.Defense, replacement.InstanceId),
            "Completed research persists and cannot complete/refund twice");
        hub._PhysicsProcess(100);
        Check(build.CompletedInvestment == 80, "Repeated physics ticks cannot duplicate completion");
        Check(build.Register(new("locked", "Tier-three research", ConstructionSlot.Ship, 1, 1, 3)), "Register tier-gated research");
        Check(!build.TryStart("locked", out _) && hub.StoredAntimatter == 100, "Lost/unavailable tier prerequisite rejects without payment");
        Check(build.Register(new("dependent", "Dependent", ConstructionSlot.Ship, 1, 1, 1, "missing"))
            && !build.TryStart("dependent", out _), "Completed-project prerequisite enforced");
    }

    private void CheckLiveBalance()
    {
        var home = new FactionBase(); AddChild(home); home.SetPhysicsProcess(false);
        var ship = Add(new ShipFlightController());
        int[] capacities = { 300, 500, 750, 1000, 1500 }, costs = { 150, 250, 375, 500 };
        for (int tier = 1; tier <= 5; tier++)
        {
            Check(home.Tier == tier && home.MaxHealth == tier * 400 && home.AntimatterStorageCapacity == capacities[tier - 1],
                "Live base has the intended storage and 400-HP health layers");
            if (tier == 5) break;
            var next = home.Construction.NextTierDefinition()!;
            home.DepositAntimatter(10000);
            Check(next.Cost == costs[tier - 1] && next.Cost < home.EffectiveStorageCapacity
                && home.Construction.TryStartNextTier(out _), "Each live upgrade fits its preceding tier with an energy reserve");
            Check(ship.EnergyCapacity == capacities[tier - 1] - next.Cost, "Paid live upgrade immediately lowers storage-backed energy");
            home.Construction.Advance(next.DurationSeconds);
        }
        Check(home.SpentAntimatter == 1275, "Tier V requires 1,275 spent antimatter before optional research");
        Check(home.Construction.Catalog.Where(p => !p.Id.StartsWith("fleet-")).All(p => p.Cost <= home.StorageCapacityForTier(p.RequiredTier)),
            "Every research item is purchasable within its unlock-tier capacity");
        Check(home.Construction.Catalog.All(p => p.Cost <= home.StorageCapacityForTier(5)),
            "Optional fleet expansions remain affordable within the eventual storage ceiling");
        var rival = new FactionBase { IsPlayerBase = false }; AddChild(rival); rival.SetPhysicsProcess(false);
        var impact = typeof(WeaponProjectile).GetMethod("OnBodyEntered", BindingFlags.Instance | BindingFlags.NonPublic)!;
        for (int shot = 0; shot < 134; shot++)
        {
            var bolt = Add(new WeaponProjectile { Damage = ship.PlasmaBoltDamage }); bolt.Launch(Vector3.Forward, ship);
            impact.Invoke(bolt, new object[] { rival });
            Check(rival.Health == Math.Max(0, 400 - (shot + 1) * 3), "Actual Plasma I projectile applies three base damage per hit");
        }
        Check(rival.IsDestroyed && home.Health == 2000, "134 Plasma I hits destroy the opposing tier-I base without friendly damage");
        var beast = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.GroundBeast });
        var ordinaryBolt = Add(new WeaponProjectile { Damage = ship.PlasmaBoltDamage }); ordinaryBolt.Launch(Vector3.Forward, ship);
        impact.Invoke(ordinaryBolt, new object[] { beast });
        Check(beast.Health == 37, "Base resistance leaves normal 13-damage Plasma mob combat intact");
    }

    private void CheckSiegeAndRebuilding()
    {
        var hub = Add(new FactionBase { InitialStoredAntimatter = 120, Tier2UpgradeCost = 100, Tier2UpgradeSeconds = 20,
            Tier3UpgradeCost = 101, Tier3UpgradeSeconds = 1,
            Tier4UpgradeCost = 101, Tier4UpgradeSeconds = 1 });
        hub.TryCompleteTierUpgrade(2);
        hub.TryCompleteTierUpgrade(3);
        hub.DepositAntimatter(180);
        var player = Add(new ShipFlightController { InitialEnergy = 300 });
        var tracker = Add(new WorldAntimatterTracker());
        var build = hub.Construction;
        foreach (var slot in new[] { ConstructionSlot.Defense, ConstructionSlot.Resources, ConstructionSlot.Offense, ConstructionSlot.Ship })
        {
            Check(build.Register(new(slot.ToString(), slot.ToString(), slot, 11, 100, 3)) && build.TryStart(slot.ToString(), out _),
                "Prepare concurrent odd-cost projects for siege");
        }
        Check(build.TryStartNextTier(out _), "Tier advancement shares siege lifecycle");
        var projects = Enum.GetValues<ConstructionSlot>().Select(s => build.GetActive(s)!).ToArray();
        Check(hub.StoredAntimatter == 155 && build.ActiveInvestment == 145, "Investment is separate from remaining storage");
        hub.ApplyDamage(280); // Tier three -> one in one hit.
        Check(hub.Tier == 1 && hub.HighestPurchasedTier == 3 && hub.Health == 140 && hub.MaxHealth == 140,
            "Multi-tier hit reduces surviving tier but retains rebuild history");
        Check(projects.All(p => p.State == ConstructionState.Destroyed) && build.DestroyedProjectCount == 5 && build.ActiveInvestment == 0,
            "Multi-tier hit destroys all five active projects once");
        Check(hub.InvestmentSpilledAntimatter == 70 && build.DestroyedInvestmentLoss == 75,
            "Odd investment splits floor-half spill and exact remainder loss per project");
        Check(LooseTotal(AntimatterOwnership.Neutral) == 105 && hub.StoredAntimatter == 120,
            "Storage spill is separate from construction spill");
        Check(player.EnergyCapacity == 120 && player.Energy == 120 && player.GetEquipmentRank("plasma-bolt") == 1,
            "Siege clamps energy but retains installed equipment/rank");
        Balanced(tracker, 300);
        Check(tracker.DestroyedInvestmentLoss == 75 && tracker.ActiveConstructionInvestment == 0,
            "World ledger records investment loss separately");
        foreach (var project in projects)
            Check(!build.Cancel(project.Definition.Slot, project.InstanceId), "Destroyed project cannot refund");
        hub._PhysicsProcess(1000);
        Check(hub.Tier == 1 && hub.MaxHealth == 140 && build.DestroyedProjectCount == 5, "Quiet repair cannot restore lost tiers or projects");
        Check(!build.TryStart("Ship", out _), "Lost tier blocks new research");
        Check(build.NextTierDefinition()!.Cost == 50 && build.TryStartNextTier(out _), "Previously bought tier two rebuilds for half price");
        var rebuild2 = build.GetActive(ConstructionSlot.Base)!;
        Check(rebuild2.IsRebuild && rebuild2.TargetTier == 2, "Rebuild cannot skip to highest purchased tier");
        hub._PhysicsProcess(20);
        Check(hub.Tier == 2 && hub.StoredAntimatter == 70, "Sequential rebuild restores capacity, not spent antimatter");
        Check(build.NextTierDefinition()!.Cost == 51 && build.TryStartNextTier(out _), "Odd rebuild price rounds up");
        var rebuild3 = build.GetActive(ConstructionSlot.Base)!;
        Check(build.Cancel(ConstructionSlot.Base, rebuild3.InstanceId) && hub.StoredAntimatter == 70,
            "Cancelled rebuild refunds actual half-price payment");
        Check(build.TryStartNextTier(out _), "Restart cancelled rebuild");
        hub._PhysicsProcess(1);
        Check(hub.Tier == 3 && hub.HighestPurchasedTier == 3 && build.NextTierDefinition()!.Cost == 101,
            "Never-purchased next tier remains full price");
        Balanced(tracker, 300);
        hub.DepositAntimatter(101);
        Check(build.TryStartNextTier(out _), "Start final project before complete destruction");
        hub.ApplyDamage(int.MaxValue);
        Check(hub.Tier == 0 && build.GetActive(ConstructionSlot.Base) == null && !build.TryStartNextTier(out _),
            "Complete base loss destroys construction and forbids tier-zero recovery");
        Balanced(tracker, 401);
        int count = build.DestroyedProjectCount;
        hub.ApplyDamage(int.MaxValue);
        hub._PhysicsProcess(1000);
        Check(build.DestroyedProjectCount == count && hub.Tier == 0, "Repeated lethal hits/ticks cannot duplicate investment drops");
    }

    private void CheckRivalRefund()
    {
        var hub = Add(new FactionBase { IsPlayerBase = false, InitialStoredAntimatter = 120 });
        Check(hub.Construction.Register(new("rival", "Rival project", ConstructionSlot.Offense, 13, 1))
            && hub.Construction.TryStart("rival", out _), "Rivals use the same construction rules");
        var project = hub.Construction.GetActive(ConstructionSlot.Offense)!;
        hub.DepositAntimatter(13);
        hub.Construction.Cancel(ConstructionSlot.Offense, project.InstanceId);
        Check(LooseTotal(AntimatterOwnership.Enemy) == 13,
            "Rival overflow refund retains rival ownership");
    }

    private void CheckSingleTierAndRebuildLoss()
    {
        var hub = Add(new FactionBase { InitialStoredAntimatter = 120, MaxHealth = 100,
            Tier2UpgradeCost = 101, Tier2UpgradeSeconds = 1 });
        hub.TryCompleteTierUpgrade(2);
        hub.Construction.Register(new("durable", "Completed research", ConstructionSlot.Ship, 1, 0.1));
        hub.Construction.TryStart("durable", out _);
        hub._PhysicsProcess(0.1);
        hub.Construction.Register(new("slow", "Slow project", ConstructionSlot.Defense, 3, 100));
        hub.Construction.TryStart("slow", out _);
        var slow = hub.Construction.GetActive(ConstructionSlot.Defense)!;
        hub.ApplyDamage(99);
        Check(hub.Tier == 2 && hub.Health == 101 && hub.EffectiveStorageCapacity == 121
            && slow.State == ConstructionState.Active, "Damage within a health layer preserves tier/projects");
        hub.ApplyDamage(1);
        Check(hub.Tier == 1 && hub.EffectiveStorageCapacity == 120 && slow.State == ConstructionState.Destroyed,
            "Boundary hit loses exactly one tier and never increases capacity");
        Check(hub.Construction.IsCompleted("durable"), "Completed research record survives siege");
        Check(hub.Construction.TryStartNextTier(out _), "Start half-price rebuild after single-tier loss");
        var rebuild = hub.Construction.GetActive(ConstructionSlot.Base)!;
        Check(rebuild.PaidAntimatter == 51, "Rebuild snapshots odd reduced payment");
        hub.ApplyDamage(100);
        Check(rebuild.State == ConstructionState.Destroyed && hub.InvestmentSpilledAntimatter == 26
            && hub.Construction.DestroyedInvestmentLoss == 28,
            "Rebuild destruction splits actual 51 payment, not full upgrade cost");
        Check(!hub.Construction.Cancel(ConstructionSlot.Base, rebuild.InstanceId), "Destroyed rebuild cannot refund");
    }

    private async Task CheckRealTimeAndPause()
    {
        var hub = Add(new FactionBase { Tier2UpgradeCost = 10, Tier2UpgradeSeconds = 0.15f });
        hub.SetPhysicsProcess(true);
        Check(hub.Construction.TryStartNextTier(out _), "Live scene starts timed tier project");
        var project = hub.Construction.GetActive(ConstructionSlot.Base)!;
        GetTree().Paused = true;
        double before = project.ElapsedSeconds;
        for (int i = 0; i < 15; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(project.ElapsedSeconds == before && hub.Tier == 1, "Existing global pause freezes construction");
        GetTree().Paused = false;
        for (int i = 0; i < 30; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        Check(project.State == ConstructionState.Completed && hub.Tier == 2 && hub.StoredAntimatter == 20,
            "Actual physics frames complete construction with no resource refill");
    }

    private void CheckSphericalSpills()
    {
        var arena = Add(new HeightmapArena { IsSpherical = true, PlanetRadius = 500 });
        var hub = Add(new FactionBase { InitialStoredAntimatter = 120,
            Position = arena.OnSurface(arena.PlanetCenter + Vector3.Down * 500, 2) });
        hub.Construction.Register(new("radial", "Radial refund", ConstructionSlot.Resources, 17, 1));
        hub.Construction.TryStart("radial", out _);
        var project = hub.Construction.GetActive(ConstructionSlot.Resources)!;
        hub.DepositAntimatter(17);
        hub.Construction.Cancel(ConstructionSlot.Resources, project.InstanceId);
        Check(LooseTotal(AntimatterOwnership.Player) == 17, "Underside refund conserves owned resource total");
        foreach (var pickup in GetTree().GetNodesInGroup("loose_antimatter").OfType<AntimatterPickup>())
            Check(arena.Clearance(pickup.GlobalPosition) >= pickup.Radius - 0.01f
                && arena.SurfaceDistance(hub.GlobalPosition, pickup.GlobalPosition) > 6,
                "Refund fragments clear underside terrain and base footprint");
    }
}

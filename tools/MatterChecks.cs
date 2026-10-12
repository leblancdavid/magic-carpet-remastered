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

public partial class MatterChecks : Node3D
{
    private int _checks;
    private void Trace(string stage) { if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--trace") >= 0) GD.Print($"Matter checks: {stage} ({_checks})"); }
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();
    private void Check(bool value, string message) { if (!value) throw new Exception(message); _checks++; }
    private async Task Frames(int count) { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    private static void Invoke(object actor, string method, params object[] args)
        => actor.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(actor, args);
    private T Add<T>(Node parent, T actor) where T : Node { parent.AddChild(actor); actor.SetPhysicsProcess(false); return actor; }
    private long Loose(AntimatterOwnership owner) => GetTree().GetNodesInGroup("matter_pickup").OfType<MatterPickup>()
        .Where(p => !p.IsQueuedForDeletion() && p.Ownership == owner).Sum(p => (long)p.MatterAmount);
    private void Balanced(WorldMatterTracker tracker, long supply)
    { tracker.Refresh(); Check(tracker.PhysicalMatter + tracker.NetCommittedMatter == supply, "Material physical + committed exactly equals supplied"); }

    private async Task Run()
    {
        try
        {
            foreach (bool globe in new[] { false, true })
            {
                Trace($"fixture {globe}");
                var room = new Node3D(); AddChild(room);
                var arena = Add(room, new HeightmapArena { IsSpherical = globe, PlanetRadius = 500 });
                Vector3 origin = globe ? arena.PlanetCenter + Vector3.Down * 500 : new Vector3(45, 0, 45);
                var home = Add(room, new FactionBase { Position = arena.OnSurface(origin, 2), InitialStoredAntimatter = 120,
                    InitialStoredMatter = 120, Tier2UpgradeCost = 100, Tier2MatterCost = 20,
                    Tier2UpgradeSeconds = 1, Tier3UpgradeSeconds = 1, Tier3MatterCost = 31 });
                var rival = Add(room, new FactionBase { IsPlayerBase = false, Position = arena.OnSurface(origin + Vector3.Right * 100, 2), InitialStoredMatter = 0 });
                var ship = Add(room, new ShipFlightController { Position = arena.OnSurface(origin, 8), EnergyRegenFractionPerSecond = 0 });
                var tracker = Add(room, new WorldMatterTracker());
                Check(home.StoredMatter == 120 && home.EffectiveMatterCapacity == 120 && ship.EnergyCapacity == 120, "Independent initial material reserve and energy");
                Check(home.DepositMatter(-1) == 0 && home.WithdrawMatter(-1) == 0 && home.DepositMatter(1) == 0, "Invalid/full material transfers reject");
                Check(!home.TrySpendResources(121, 1) && !home.TrySpendResources(1, 121) && !home.TrySpendResources(-1, 0)
                    && home.StoredMatter == 120 && home.StoredAntimatter == 120 && home.SpentMatter == 0, "Atomic dual-resource rejection never partly pays");
                Check(home.Construction.Register(new("material-only", "Material only", ConstructionSlot.Resources, 0, 1, MatterCost: 7)), "Material-only definitions accepted");
                Check(!home.Construction.Register(new("negative", "Invalid", ConstructionSlot.Resources, 1, 1, MatterCost: -1)), "Negative material costs rejected");
                Check(!home.Construction.Register(new("free", "Invalid", ConstructionSlot.Resources, 0, 1)), "Zero-total cost rejected");
                int energy = ship.Energy;
                Check(home.Construction.TryStart("material-only", out _) && home.StoredMatter == 113 && ship.Energy == energy && ship.EnergyCapacity == 120,
                    "Material-only spending does not affect ship energy");
                Balanced(tracker, 120);
                var material = home.Construction.GetActive(ConstructionSlot.Resources)!;
                home.DepositMatter(7);
                Check(home.Construction.Cancel(ConstructionSlot.Resources, material.InstanceId) && home.StoredMatter == 120 && Loose(AntimatterOwnership.Player) == 7,
                    "Material cancellation preserves exact owned overflow");
                Check(!home.Construction.Cancel(ConstructionSlot.Resources, material.InstanceId), "Material cancellation cannot replay");
                Balanced(tracker, 127);
                home.WithdrawMatter(120);
                Check(!home.Construction.TryStart("repair-matrix", out string reason) && reason.Contains("matter") && home.StoredAntimatter == 120,
                    "Material insufficiency blocks real catalog without antimatter payment");
                home.DepositMatter(120);
                Check(home.Construction.TryStartNextTier(out _) && home.StoredMatter == 100 && home.StoredAntimatter == 20, "Tier snapshots/payments include both resources");
                var tier = home.Construction.GetActive(ConstructionSlot.Base)!;
                Check(tier.PaidMatter == 20 && tier.PaidAntimatter == 100, "Immutable paid material snapshot");
                home.Construction.Advance(1);
                Check(home.Tier == 2 && home.StoredMatter == 100 && home.MatterStorageCapacity == 200 && home.Construction.CompletedMatterInvestment == 20,
                    "Tier completion adds capacity without restoring material");
                home.DepositAntimatter(200);
                home.TryCompleteTierUpgrade(3);
                home.DepositMatter(200);
                home.Construction.Register(new("siege", "Odd material investment", ConstructionSlot.Defense, 11, 100, MatterCost: 11));
                Check(home.Construction.TryStart("siege", out _), "Odd dual-resource siege investment");
                var siege = home.Construction.GetActive(ConstructionSlot.Defense)!;
                tracker.Refresh(); long supply = tracker.PhysicalMatter + tracker.NetCommittedMatter;
                home.ApplyDamage(140);
                Check(home.Tier == 2 && siege.State == ConstructionState.Destroyed && home.InvestmentSpilledMatter == 5 && home.Construction.DestroyedMatterLoss == 6,
                    "Tier loss splits material investment once with odd remainder permanently lost");
                Check(home.StoredMatter == 200 && home.EffectiveMatterCapacity == 200, "Storage excess spills separately at tier boundary");
                Balanced(tracker, supply);
                Check(home.Construction.NextTierDefinition()!.MatterCost == 16, "Odd material rebuild cost rounds up half normal price");
                Check(home.Construction.TryStartNextTier(out _), "Sequential material-backed rebuild starts");
                var rebuild = home.Construction.GetActive(ConstructionSlot.Base)!;
                Check(rebuild.PaidMatter == 16 && rebuild.IsRebuild, "Rebuild snapshots reduced actual material payment");
                Check(home.Construction.Cancel(ConstructionSlot.Base, rebuild.InstanceId) && home.StoredMatter == 200, "Rebuild refunds actual material price");
                Balanced(tracker, supply);
                home.ApplyDamage(1);
                Check(home.EffectiveMatterCapacity == 200, "Rounded material capacity stays continuous near intact boundary");
                home.ApplyDamage(69);
                Check(home.Tier == 2 && home.EffectiveMatterCapacity == 160 && home.StoredMatter == 160, "Damage scales only top layer's incremental material capacity");
                Balanced(tracker, supply);
                home._PhysicsProcess(100);
                Check(home.StoredMatter == 160 && home.EffectiveMatterCapacity == 200 && home.Tier == 2, "Repair reopens room without material or lost tier recreation");
                await CheckClaimAndHauling(room, arena, home, rival, tracker);
                Trace("hauling complete");
                await CheckIncome(room, arena, home, rival, tracker);
                Trace("income complete");
                home.ApplyDamage(int.MaxValue);
                Check(home.StoredMatter == 0 && home.EffectiveMatterCapacity == 0 && home.DepositMatter(100) == 0,
                    "Complete destruction releases all stored material and rejects deposits");
                foreach (var crate in GetTree().GetNodesInGroup("matter_pickup").OfType<MatterPickup>().Where(p => p.IsAvailable))
                    Check(Math.Abs(arena.Clearance(crate.GlobalPosition) - 0.6) < 0.02, "Spill crates grounded/floating on flat or underside terrain");
                room.QueueFree(); await Frames(10);
                Trace("fixture cleared");
            }
            await CheckMatch();
            Trace("match cleared");
            await Frames(10);
            GD.Print($"Matter checks passed: {_checks} assertions."); GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    private async Task CheckClaimAndHauling(Node3D room, HeightmapArena arena, FactionBase home, FactionBase rival, WorldMatterTracker tracker)
    {
        // Existing refund crates must not distract the actual flight path.
        foreach (var crate in GetTree().GetNodesInGroup("matter_pickup").OfType<MatterPickup>()) crate.ReassignOwnership(AntimatterOwnership.Neutral);
        Vector3 start = arena.OnSurface(home.GlobalPosition + home.GlobalBasis.X * 25, 0.9f);
        Vector3 forward = arena.Tangent(-home.GlobalBasis.Z, start).Normalized();
        Vector3 target = arena.IsSpherical ? arena.StepToward(start, arena.OnSurface(start + forward * 10, 0.9f), 6) : start + forward * 6;
        var pickup = Add(room, new MatterPickup { Position = target, MatterAmount = 17 });
        var claim = Add(room, new ClaimProjectile { Position = start });
        int announced = 0; claim.MatterClaimed += amount => announced += amount;
        claim.Launch(forward); claim._PhysicsProcess(0.2);
        Check(pickup.Ownership == AntimatterOwnership.Player && claim.IsQueuedForDeletion() && announced == 17, "Actual swept terrain Claim hits material and reports exact amount");
        home.DepositAntimatter(1000); home.WithdrawMatter(home.StoredMatter);
        var drone = Add(room, new CollectorDrone { Position = pickup.GlobalPosition + arena.UpAt(pickup.GlobalPosition) * 4.5f,
            MaxSeekRadius = 500, CarryCapacity = 3 });
        drone._PhysicsProcess(0);
        Check(drone.CarriedMatterAmount == 17 && !pickup.IsAvailable, "Actual seeking gathers material despite full antimatter reserve");
        tracker.Refresh(); long supply = tracker.PhysicalMatter + tracker.NetCommittedMatter;
        // Delivery by real route progression, not a direct deposit call.
        for (int i = 0; i < 200 && drone.CarriedCount > 0; i++) drone._PhysicsProcess(0.1);
        Check(drone.CarriedCount == 0 && home.StoredMatter == 17, "Actual collector returns/docks material on flat and underside routes");
        Balanced(tracker, supply);
        await Frames(2);
        var mixed = Add(room, new MatterPickup { Position = start, MatterAmount = 19, Ownership = AntimatterOwnership.Player });
        home.WithdrawAntimatter(10);
        var orb = Add(room, new AntimatterPickup { Position = start, AntimatterAmount = 9, Ownership = AntimatterOwnership.Player }); orb.Freeze = true;
        Invoke(drone, "ScoopMatter", mixed); Invoke(drone, "Scoop", orb);
        Check(drone.CarriedCount == 2 && drone.CarriedMatterAmount == 19, "Mixed cargo shares existing object limit");
        var extra = Add(room, new MatterPickup { Position = start, MatterAmount = 3, Ownership = AntimatterOwnership.Player });
        Invoke(drone, "ScoopMatter", extra);
        var rejected = Add(room, new MatterPickup { Position = start, MatterAmount = 4, Ownership = AntimatterOwnership.Player });
        Invoke(drone, "ScoopMatter", rejected);
        Check(drone.CarriedCount == 3 && rejected.IsAvailable, "Shared limit prevents fourth cargo object");
        var logistics = Add(room, new StrategicPoint { Role = StrategicPointRole.Logistics, Position = start });
        logistics.Advance(10, true, false);
        var stationDrone = logistics.Drone!; stationDrone.SetPhysicsProcess(false);
        var stationCrate = Add(room, new MatterPickup { Position = start, MatterAmount = 23, Ownership = AntimatterOwnership.Player });
        Invoke(stationDrone, "ScoopMatter", stationCrate);
        var stationOrb = Add(room, new AntimatterPickup { Position = start, AntimatterAmount = 7, Ownership = AntimatterOwnership.Player }); stationOrb.Freeze = true;
        Invoke(stationDrone, "Scoop", stationOrb);
        tracker.Refresh(); supply = tracker.PhysicalMatter + tracker.NetCommittedMatter;
        rival.DepositMatter(1000); supply += 120;
        rival.DepositAntimatter(1000);
        logistics.Advance(20, false, true);
        Check(ReferenceEquals(logistics.Drone, stationDrone) && stationCrate.Ownership == AntimatterOwnership.Enemy && stationDrone.CarriedMatterAmount == 23
            && stationOrb.Ownership == AntimatterOwnership.Enemy && stationOrb.AntimatterAmount == 7 && stationDrone.CarriedCount == 2,
            "Logistics capture steals both attached cargo kinds on the same drone");
        Invoke(stationDrone, "ScoopMatter", rejected);
        Check(rejected.IsAvailable && stationDrone.CarriedCount == 2, "Stale previous-owner material pickup rejected");
        Invoke(stationDrone, "DepositAll");
        Check(stationCrate.MatterAmount == 23 && stationOrb.AntimatterAmount == 7 && home.StoredMatter == 17,
            "Full new home retains both stolen cargo kinds without old-home deposit");
        rival.WithdrawMatter(5); supply -= 5;
        Invoke(stationDrone, "DepositAll");
        Check(stationCrate.MatterAmount == 18 && stationDrone.CarriedMatterAmount == 18 && rival.StoredMatter == 120, "Partial stolen material deposit retains exact remainder");
        Balanced(tracker, supply);
        stationDrone.ApplyDamage(1000);
        Check(stationCrate.MatterAmount == 0 && stationOrb.AntimatterAmount == 0 && stationDrone.CarriedMatterAmount == 0 && Loose(AntimatterOwnership.Neutral) >= 18,
            "Drone destruction releases remaining neutral material exactly once");
        Balanced(tracker, supply);
        drone.ApplyDamage(1000);
        await Frames(2);
        home.DepositMatter(1000);
        home.WithdrawAntimatter(home.StoredAntimatter);
        var antiOnly = Add(room, new CollectorDrone { Position = start, MaxSeekRadius = 500 });
        var energyCargo = Add(room, new AntimatterPickup { Position = start, AntimatterAmount = 8, Ownership = AntimatterOwnership.Player }); energyCargo.Freeze = true;
        antiOnly.GlobalPosition = energyCargo.GlobalPosition + arena.UpAt(start) * antiOnly.HoverHeight;
        antiOnly._PhysicsProcess(0);
        Check(antiOnly.CarriedCount == 1 && !energyCargo.IsAvailable && antiOnly.CarriedMatterAmount == 0,
            "Full matter reserve does not block eligible antimatter gathering");
        antiOnly.ApplyDamage(1000); await Frames(2);

        // Both resource groups participate in one nearest segment-entry decision.
        Vector3 isolated = arena.OnSurface(home.GlobalPosition + home.GlobalBasis.X * 65, 0.9f);
        Vector3 heading = arena.Tangent(-home.GlobalBasis.Z, isolated).Normalized();
        var nearer = Add(room, new AntimatterPickup { Position = arena.OnSurface(isolated + heading * 4, 0.6f), AntimatterAmount = 1 }); nearer.Freeze = true;
        var farther = Add(room, new MatterPickup { Position = arena.OnSurface(isolated + heading * 8, 0.6f), MatterAmount = 5 });
        var mixedClaim = Add(room, new ClaimProjectile { Position = isolated }); mixedClaim.Launch(heading); mixedClaim._PhysicsProcess(0.3);
        Check(nearer.Ownership == AntimatterOwnership.Player && farther.Ownership == AntimatterOwnership.Neutral,
            "Nearest antimatter intercepts a farther material Claim");
        await Frames(2);
        var fullClaim = Add(room, new ClaimProjectile { Position = arena.OnSurface(isolated + heading * 5, 0.9f) });
        fullClaim.Launch(arena.Tangent(heading, fullClaim.GlobalPosition)); fullClaim._PhysicsProcess(0.2);
        Check(farther.Ownership == AntimatterOwnership.Player && home.AvailableMatterStorage == 0,
            "Material Claim remains usable at full material storage");
        var fullDrone = Add(room, new CollectorDrone { Position = farther.GlobalPosition + arena.UpAt(farther.GlobalPosition) * 4.5f });
        Invoke(fullDrone, "ScoopMatter", farther);
        Check(farther.IsAvailable && fullDrone.CarriedCount == 0, "Full material storage prevents material scoop without removing claimed crate");
        fullDrone.Free();
        nearer.Free(); farther.Free();
        foreach (Node node in GetTree().GetNodesInGroup("matter_pickup"))
            if (node is MatterPickup crate && crate.IsAvailable) crate.ReassignOwnership(AntimatterOwnership.Neutral);
    }

    private async Task CheckIncome(Node3D room, HeightmapArena arena, FactionBase home, FactionBase rival, WorldMatterTracker tracker)
    {
        home.WithdrawMatter(10); // Income probe needs room independently of prior full-storage hauling checks.
        var site = Add(room, new StrategicPoint { Role = StrategicPointRole.MatterExtractor, Position = home.GlobalPosition });
        int before = home.StoredMatter; int energyLimit = home.StoredAntimatter;
        site.Advance(5, true, false); site.Advance(20, true, true);
        Check(site.Ownership == AntimatterOwnership.Neutral && site.CaptureProgress == 0.5 && home.StoredMatter == before, "Incomplete/contested material capture produces nothing");
        site.Advance(5, true, false); site.Advance(0.25, false, false); site.Advance(0.25, false, false);
        Check(home.StoredMatter == before + 1 && site.GeneratedMatter == 1 && site.GeneratedAntimatter == 0 && home.StoredAntimatter == energyLimit,
            "Fractional material income creates exact accepted supply without antimatter");
        home.DepositMatter(1000); long generated = site.GeneratedMatter;
        site.Advance(1000, false, false);
        home.WithdrawMatter(1); site.Advance(0.25, false, false);
        Check(home.AvailableMatterStorage == 1 && site.GeneratedMatter == generated, "Full material storage discards backlog");
        site.Advance(0.25, false, false);
        Check(home.AvailableMatterStorage == 0 && site.GeneratedMatter == generated + 1, "Income accepts exact partial room");
        home.WithdrawMatter(10); site.Advance(5, false, true);
        Check(home.AvailableMatterStorage == 0 && site.Ownership == AntimatterOwnership.Player, "Previous material owner keeps income until capture completes");
        site.Advance(5, false, true);
        Check(site.Ownership == AntimatterOwnership.Neutral, "Material site stops income at neutralization");
        site.Advance(10, false, true); rival.WithdrawMatter(2); site.Advance(1, false, false);
        Check(site.Ownership == AntimatterOwnership.Enemy && rival.AvailableMatterStorage == 0, "Material income reroutes only to captured owner");
        GetTree().Paused = true; generated = site.GeneratedMatter; site.Advance(100, true, false);
        Check(site.GeneratedMatter == generated && site.Ownership == AntimatterOwnership.Enemy, "Pause freezes material capture/income"); GetTree().Paused = false;
        tracker.Refresh(); Check(tracker.SiteGeneratedMatter == site.GeneratedMatter, "Material tracker distinguishes site supply from transfers/refunds");
        var panel = Add(room, new BaseManagement()); panel.SetOpen(true); panel.Refresh();
        Check(panel.SummaryText.Contains("Matter") && panel.SummaryText.Contains("matter/s") && !GetTree().Paused, "Live management exposes material storage/income");
        panel.SetOpen(false);
        if (arena.IsSpherical)
        {
            var navigation = Add(room, new WorldNavigation());
            var crate = Add(room, new MatterPickup { Position = home.GlobalPosition, Ownership = AntimatterOwnership.Player });
            navigation.Refresh();
            Check(navigation.Markers.Any(m => m.Kind == MapMarkerKind.Matter && m.Title.Contains("matter")), "Material crates visible in survey/radar");
            crate.TakeMatter(crate.MatterAmount);
            Check(!navigation.Markers.First(m => ReferenceEquals(m.Actor, crate)).TryGetLiveUp(arena, out _), "Spent material marker disappears immediately");
        }
        await Frames(2);
        generated = site.GeneratedMatter;
        rival.ApplyDamage(int.MaxValue); site.Advance(100, false, true);
        Check(site.GeneratedMatter == generated, "Destroyed material owner's base cannot receive extractor output");
    }

    private async Task CheckMatch()
    {
        var game = GD.Load<PackedScene>("res://scenes/PlanetGame.tscn").Instantiate<Node3D>(); AddChild(game);
        foreach (Node child in game.GetChildren()) if (child is not CanvasLayer) { child.SetPhysicsProcess(false); child.SetProcess(false); }
        var home = (FactionBase)GetTree().GetFirstNodeInGroup("player_base");
        var pickups = GetTree().GetNodesInGroup("matter_pickup").OfType<MatterPickup>().ToArray();
        Check(home.StoredMatter == 30 && pickups.Sum(p => p.MatterAmount) == 240 && pickups.All(p => p.Ownership == AntimatterOwnership.Neutral),
            "Fresh match resets reserves and four neutral 60-material fields");
        Check(GetTree().GetNodesInGroup("strategic_point").OfType<StrategicPoint>().Count(s => s.Role == StrategicPointRole.MatterExtractor) == 2,
            "Fresh match activates two neutral material extractors");
        var tracker = (WorldMatterTracker)GetTree().GetFirstNodeInGroup("world_matter_tracker"); Balanced(tracker, 300);
        if (DisplayServer.GetName() != "headless")
        {
            await Frames(5); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using (var image = GetViewport().GetTexture().GetImage()) Check(image.SavePng("C:/Users/lblan/AppData/Local/Temp/opencode/MatterGameplay.png") == Error.Ok, "Save material HUD capture");
            var management = (BaseManagement)GetTree().GetFirstNodeInGroup("base_management"); management.SetOpen(true);
            await Frames(5); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using (var image = GetViewport().GetTexture().GetImage()) Check(image.SavePng("C:/Users/lblan/AppData/Local/Temp/opencode/MatterManagement.png") == Error.Ok, "Save material management capture");
            management.SetOpen(false);
        }
        game.QueueFree(); await Frames(10);
    }
}

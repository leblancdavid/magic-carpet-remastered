using System;
using System.Collections.Generic;
using System.Linq;
using MagicCarpetRemastered.Scripts.Equipment;

namespace MagicCarpetRemastered.Scripts.World;

public sealed record UpgradeType(string Id, string Name, ConstructionSlot Slot, int Tier,
    string Description, bool Playable = false, bool Equipment = false, bool Deployable = false);

// One type per tile; levels are still immutable paid construction definitions.
public static class UpgradeCatalog
{
    public static readonly IReadOnlyList<UpgradeType> Items = new UpgradeType[]
    {
        new("scout-drone", "Scout Drone", ConstructionSlot.Offense, 1, "Finds the nearest non-owned capture point, including enemy points. Captures at half ship speed; multiple scouts do not stack. Stars improve speed, sensing and durability.", true, Deployable: true),
        new("attack-drone", "Attack Drone", ConstructionSlot.Offense, 1, "Deploy to a known point or enemy base. Best against neutrals and light site defenders; weak siege. Stars improve damage and durability.", true, Deployable: true),
        new("drone-command-relay", "Drone Command Relay", ConstructionSlot.Offense, 1, "Deployed combat/scout drones: +10/20/30% travel speed and +5/10/15m combat acquisition range.", true),
        new("strike-drone", "Strike Drone", ConstructionSlot.Offense, 2, "One-shot siege unit: 120/160/200 structure damage, 30/45/60 HP. Interceptable; not a ship-hunting weapon.", true, Deployable: true),
        new("hunter-drone", "Hunter Drone", ConstructionSlot.Offense, 2, "Pursues enemy collectors and units; weak structural damage.", Deployable: true),
        new("rocket-battery", "Rocket Battery", ConstructionSlot.Offense, 2, "Bombards a selected known position within range; best against clustered defenders.", Deployable: true),
        new("particle-cannon", "Particle Cannon", ConstructionSlot.Offense, 3, "Local line-of-sight precision base cannon; charge-up exposes a window to react.", Deployable: true),
        new("capture-squadron", "Capture Squadron", ConstructionSlot.Offense, 3, "Scouts with escorts for contested points; capture speed does not stack.", Deployable: true),
        new("disruptor-drone", "Disruptor Drone", ConstructionSlot.Offense, 3, "Weakens barriers and defensive firing to support assaults.", Deployable: true),
        new("siege-torpedo", "Siege Torpedo", ConstructionSlot.Offense, 4, "Slow, interceptable long-range structural attack.", Deployable: true),
        new("assault-squadron", "Assault Squadron", ConstructionSlot.Offense, 4, "Sustained combat squad for clearing defended sites.", Deployable: true),
        new("siege-coordination", "Siege Coordination", ConstructionSlot.Offense, 4, "Improves offensive-unit focus fire and siege effectiveness."),
        new("siege-carrier", "Siege Carrier", ConstructionSlot.Offense, 5, "Durable assault unit with a bounded complement of attack drones.", Deployable: true),
        new("orbital-lance", "Orbital Lance", ConstructionSlot.Offense, 5, "Delayed known-position strike, visible warning and long cooldown; avoidable by mobile targets.", Deployable: true),
        new("electronic-warfare-array", "Electronic Warfare Array", ConstructionSlot.Offense, 5, "Targeted defensive suppression within operational range.", Deployable: true),
        new("repair-matrix", "Repair Matrix", ConstructionSlot.Defense, 1, "Quiet base repair +25/50/75%. Never rebuilds lost tiers.", true),
        new("pulse-turret", "Pulse Turret", ConstructionSlot.Defense, 1, "Retaliatory base pulse: 8/10/12 damage and 1.1/1.0/0.9s firing interval. Requires a known attacker and clear line of sight.", true),
        new("defensive-drone", "Defensive Drone", ConstructionSlot.Defense, 1, "Deploy to home or owned sites. Defends locally, retreats home on site loss. Stars improve HP and damage.", true, Deployable: true),
        new("base-armor", "Base Armor", ConstructionSlot.Defense, 2, "Reduces incoming base damage by 10/15/20%, with at least one damage per accepted hit.", true),
        new("flak-battery", "Flak Battery", ConstructionSlot.Defense, 2, "Area defense against swarms and drones."),
        new("site-fortification", "Site Fortification", ConstructionSlot.Defense, 2, "Defensive drones at owned points take 10/20/30% less damage.", true),
        new("shield-pylon", "Shield Pylon", ConstructionSlot.Defense, 3, "Rechargeable barrier around the base."),
        new("interceptor-array", "Interceptor Array", ConstructionSlot.Defense, 3, "Intercepts missiles and strike drones."),
        new("emergency-bulkheads", "Emergency Bulkheads", ConstructionSlot.Defense, 3, "Temporary resistance after losing a base layer."),
        new("heavy-defense-beam", "Heavy Defense Beam", ConstructionSlot.Defense, 4, "Sustained defense against heavy beasts and attacking ships."),
        new("repair-drones", "Repair Drones", ConstructionSlot.Defense, 4, "Local base/collector repair, reduced during combat."),
        new("suppression-battery", "Suppression Battery", ConstructionSlot.Defense, 4, "Defensive area fire slows attacking packs."),
        new("aegis-network", "Aegis Network", ConstructionSlot.Defense, 5, "Improves base shield coverage and resistance."),
        new("emergency-barrier", "Emergency Barrier", ConstructionSlot.Defense, 5, "Critical-health protection with a long recovery interval."),
        new("defense-coordination", "Defense Coordination", ConstructionSlot.Defense, 5, "Improves defensive targeting, range and focus fire."),
        new("cargo-compression", "Cargo Compression", ConstructionSlot.Resources, 1, "Collector capacity 40/50/60 A instead of 30 A. No object-count cap.", true),
        new("collector-drive", "Collector Drive", ConstructionSlot.Resources, 1, "Home and logistics collector travel speed +15/30/45%.", true),
        new("scoop-array", "Scoop Array", ConstructionSlot.Resources, 1, "Collector scoop radius +25/50/75%.", true),
        new("collector-armor", "Collector Armor", ConstructionSlot.Resources, 2, "Collectors take 15/30/45% less damage; never grants free cargo.", true),
        new("dock-automation", "Dock Automation", ConstructionSlot.Resources, 2, "Collector docking delay reduced by 20/40/60%.", true),
        new("extractor-optimization", "Extractor Optimization", ConstructionSlot.Resources, 2, "Owned extractor income +10/20/30%. Still stops at capacity.", true),
        new("collector-escort", "Collector Escort", ConstructionSlot.Resources, 3, "Weak defensive weapons on collectors."),
        new("research-coordination", "Research Coordination", ConstructionSlot.Resources, 3, "Improves research and construction speed."),
        new("storage-reinforcement", "Storage Reinforcement", ConstructionSlot.Resources, 3, "Preserves more damaged top-layer storage capacity."),
        new("collector-overdrive", "Collector Overdrive", ConstructionSlot.Resources, 4, "Automatic speed bursts on long return routes."),
        new("remote-depot", "Remote Depot", ConstructionSlot.Resources, 4, "Owned logistics points accept cargo with bounded transfer throughput."),
        new("salvage-reclaimer", "Salvage Reclaimer", ConstructionSlot.Resources, 4, "Returns part of destroyed friendly unit investment as claimed salvage."),
        new("quantum-cargo", "Quantum Cargo", ConstructionSlot.Resources, 5, "Advanced capacity and cargo theft protection."),
        new("extraction-network", "Extraction Network", ConstructionSlot.Resources, 5, "Bonus income for holding multiple extractors."),
        new("construction-efficiency", "Construction Efficiency", ConstructionSlot.Resources, 5, "Discounts future purchases; refunds only actual payments."),
        new("plasma-bolt", "Plasma Bolt", ConstructionSlot.Ship, 1, "Efficient general hunting/skirmishing: 13/18/23 damage, 2 E. Bases take only 20% damage. Bronze is starting equipment.", true, true),
        new("pulse-burst", "Pulse Burst", ConstructionSlot.Ship, 1, "Best against clustered neutrals; short-range area blast, 32/40/48 damage and 8/10/12m radius. Expensive versus isolated targets.", true, true),
        new("energy-shield", "Energy Shield", ConstructionSlot.Ship, 1, "Survive concentrated fire: 65% damage reduction for 4/6/8s. No offensive damage.", true, true),
        new("claim-range", "Claim Calibration", ConstructionSlot.Ship, 1, "Built-in Claim range 150/180/210m and wider sweeps. 1 E; no loadout slot or installation.", true),
        new("scatter-cannon", "Scatter Cannon", ConstructionSlot.Ship, 2, "Close-range heavies and ships; inaccurate at distance.", Equipment: true),
        new("seeker-missile", "Seeker Missile", ConstructionSlot.Ship, 2, "Tracks moving ships and aerial targets; interceptable and inefficient for siege.", Equipment: true),
        new("dash", "Dash", ConstructionSlot.Ship, 2, "Escape, chase and terrain crossing. Speed 46/55.2/64.4; consumes energy and a loadout slot.", true, true),
        new("energy-grid", "Energy Grid", ConstructionSlot.Ship, 2, "Ship energy recharge +15/30/45%. Passive; no equipment slot.", true),
        new("rail-lance", "Rail Lance", ConstructionSlot.Ship, 3, "Precision versus armored heavies/emplacements; narrow shot and slow firing.", Equipment: true),
        new("arc-emitter", "Arc Emitter", ConstructionSlot.Ship, 3, "Chains through dispersed packs and drone groups; weak isolated siege.", Equipment: true),
        new("repair-projector", "Repair Projector", ConstructionSlot.Ship, 3, "Active recovery for sustained exploration; limited healing under fire.", Equipment: true),
        new("hull-reinforcement", "Hull Reinforcement", ConstructionSlot.Ship, 3, "Increased maximum ship HP without a loadout slot."),
        new("siege-launcher", "Siege Launcher", ConstructionSlot.Ship, 4, "Structural/barrier damage; slow projectile and poor mobile-target efficiency.", Equipment: true),
        new("guardian-drone", "Guardian Drone", ConstructionSlot.Ship, 4, "Mobile combat/site holding support: 12/16/20 damage, 12/18/24s lifetime. Temporary, destructible ally.", true, true),
        new("decoy-projector", "Decoy Projector", ConstructionSlot.Ship, 4, "Counter guided weapons and drone pursuit; weak versus melee/unguided fire.", Equipment: true),
        new("vector-thrusters", "Vector Thrusters", ConstructionSlot.Ship, 4, "Better lateral control and acceleration/braking."),
        new("particle-beam", "Particle Beam", ConstructionSlot.Ship, 5, "Sustained aimed single-target pressure; continuous energy demand.", Equipment: true),
        new("disruptor-cannon", "Disruptor Cannon", ConstructionSlot.Ship, 5, "Weakens barriers and recovery; modest direct damage.", Equipment: true),
        new("phase-dash", "Phase Dash", ConstructionSlot.Ship, 5, "Evasive dash with a brief damage-avoidance window; longer cooldown than Dash.", Equipment: true),
        new("energy-recycling", "Energy Recycling", ConstructionSlot.Ship, 5, "Small capped energy returns from successful combat hits.")
    };
    public static UpgradeType? Find(string id) => Items.FirstOrDefault(item => item.Id == id);
    public static string ProjectId(string id, int rank) => Find(id)?.Equipment == true
        ? ShipEquipmentCatalog.ProjectId(id, rank)
        : id == "strike-drone" || rank > 1 ? $"upgrade-{id}-{rank}" : id;
    private static readonly int[,] Costs = { { 40, 80, 140 }, { 70, 140, 245 }, { 110, 220, 385 }, { 165, 330, 575 }, { 240, 480, 840 } };
    private static readonly int[,] Times = { { 30, 45, 60 }, { 40, 60, 80 }, { 50, 75, 100 }, { 60, 90, 120 }, { 75, 110, 150 } };
    public static ConstructionDefinition Definition(UpgradeType item, int rank) => new(ProjectId(item.Id, rank),
        $"{item.Name} level {rank}", item.Slot, Costs[item.Tier - 1, rank - 1], Times[item.Tier - 1, rank - 1], item.Tier,
        rank > 1 && !(item.Id == "plasma-bolt" && rank == 2) ? ProjectId(item.Id, rank - 1) : null,
        item.Description, UpgradeId: item.Id, UpgradeRank: rank);
    public static void Register(BaseConstruction construction)
    {
        foreach (var item in Items.Where(item => item.Playable))
            for (int rank = item.Id == "plasma-bolt" ? 2 : 1; rank <= 3; rank++) construction.Register(Definition(item, rank));
    }
}

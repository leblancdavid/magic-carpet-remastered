namespace MagicCarpetRemastered.Scripts.World;

public static class BaseProjectCatalog
{
    public static void Register(BaseConstruction construction)
    {
        Equipment.ShipEquipmentCatalog.Register(construction);
        construction.Register(new("repair-matrix", "Repair matrix", ConstructionSlot.Defense, 20, 8,
            Description: "Passive base repair +50%. Never restores lost tiers.", MatterCost: 5));
        construction.Register(new("energy-grid", "Energy grid", ConstructionSlot.Defense, 40, 12, 2,
            Description: "Ship energy recharge +25%. Suspends below tier 2.", MatterCost: 10));
        construction.Register(new("collector-drive", "Collector drive", ConstructionSlot.Resources, 25, 10,
            Description: "Home-base collector speed +25%.", MatterCost: 5));
        construction.Register(new("strike-drone", "Strike drone", ConstructionSlot.Offense, 60, 15, 2,
            Description: "Launch a one-shot drone at the rival base: 45 damage. Repeatable.", Repeatable: true, MatterCost: 15));
        construction.Register(new("claim-range", "Claim range calibration", ConstructionSlot.Ship, 15, 8,
            Description: "Built-in RMB Claim range +25%. No equipment slot or installation."));
    }
}

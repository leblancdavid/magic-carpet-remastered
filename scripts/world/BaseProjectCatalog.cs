namespace MagicCarpetRemastered.Scripts.World;

public static class BaseProjectCatalog
{
    public static void Register(BaseConstruction construction)
    {
        Equipment.ShipEquipmentCatalog.Register(construction);
        construction.Register(new("repair-matrix", "Repair matrix", ConstructionSlot.Defense, 70, 60,
            Description: "Passive base repair +50%. Never restores lost tiers."));
        construction.Register(new("energy-grid", "Energy grid", ConstructionSlot.Defense, 140, 90, 2,
            Description: "Ship energy recharge +25%. Suspends below tier 2."));
        construction.Register(new("collector-drive", "Collector drive", ConstructionSlot.Resources, 90, 60,
            Description: "Home-base collector speed +25%."));
        construction.Register(new("strike-drone", "Strike drone", ConstructionSlot.Offense, 100, 20, 2,
            Description: "Launch a one-shot siege drone: 120 base damage. Repeatable.", Repeatable: true));
        construction.Register(new("claim-range", "Claim range calibration", ConstructionSlot.Ship, 50, 60,
            Description: "Built-in RMB Claim range +25%. No equipment slot or installation."));
    }
}

using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Equipment;

public static class ShipEquipmentCatalog
{
    public static readonly string[] ItemIds = { "plasma-bolt", "pulse-burst", "energy-shield", "dash", "guardian-drone" };
    public const int MaximumRank = 3;
    public static string ProjectId(string id, int rank) => $"equipment-{id}-{rank}";
    public static string EffectSummary(string id, int rank) => id switch
    {
        "plasma-bolt" => $"Projectile damage {25 + 10 * (rank - 1)}. Bases take 20% Plasma damage.",
        "pulse-burst" => $"Area damage {32 + 8 * (rank - 1)}, radius {8 + 2 * (rank - 1)}.",
        "energy-shield" => $"65% damage reduction for {4 + 2 * (rank - 1)}s.",
        "dash" => $"Dash speed {46 * (1 + 0.2f * (rank - 1)):0.0} with radial lift.",
        "guardian-drone" => $"Ally damage {12 + 4 * (rank - 1)}, lifetime {12 + 6 * (rank - 1)}s.",
        _ => string.Empty
    };

    public static void Register(BaseConstruction construction)
    {
        string[] names = { "Plasma Bolt", "Pulse Burst", "Energy Shield", "Dash", "Guardian Drone" };
        int[] tiers = { 1, 1, 1, 1, 2 };
        int[] costs = { 15, 25, 20, 20, 40 };
        for (int i = 0; i < ItemIds.Length; i++)
        for (int rank = ItemIds[i] == "plasma-bolt" ? 2 : 1; rank <= MaximumRank; rank++)
        {
            string? prerequisite = rank > 1 && !(i == 0 && rank == 2) ? ProjectId(ItemIds[i], rank - 1) : null;
            construction.Register(new(ProjectId(ItemIds[i], rank), $"{names[i]} rank {rank}", ConstructionSlot.Ship,
                costs[i] * rank * (rank == 1 ? 3 : 4), 30 + rank * 30, System.Math.Min(5, tiers[i] + rank - 1), prerequisite,
                Description: EffectSummary(ItemIds[i], rank) + " Owned permanently on completion. Return home to install; upgrades do not apply remotely."));
        }
    }
}

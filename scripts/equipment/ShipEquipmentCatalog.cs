using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Equipment;

public static class ShipEquipmentCatalog
{
    public static readonly string[] ItemIds = { "plasma-bolt", "pulse-burst", "energy-shield", "scatter-cannon", "seeker-missile", "dash", "guardian-drone" };
    public const int MaximumRank = 3;
    public static string ProjectId(string id, int rank) => $"equipment-{id}-{rank}";
    public static string EffectSummary(string id, int rank) => id switch
    {
        "plasma-bolt" => $"Projectile damage {13 + 5 * (rank - 1)}, 2 energy per shot. Bases take 20% Plasma damage.",
        "pulse-burst" => $"Area damage {32 + 8 * (rank - 1)}, radius {8 + 2 * (rank - 1)}m. 5 E / 2.5s; aim at a pack.",
        "scatter-cannon" => $"7 × {8 + 3 * (rank - 1)} pellet damage. 5 E / 0.8s, 36m reach; strongest close up.",
        "seeker-missile" => $"Guided damage {42 + 14 * (rank - 1)}. 7 E / 1.8s; 90m visible-target lock, shootable missile.",
        "energy-shield" => $"65% damage reduction for {4 + 2 * (rank - 1)}s.",
        "dash" => $"Dash speed {46 * (1 + 0.2f * (rank - 1)):0.0} with radial lift.",
        "guardian-drone" => $"Ally damage {12 + 4 * (rank - 1)}, lifetime {12 + 6 * (rank - 1)}s.",
        _ => string.Empty
    };

    public static void Register(BaseConstruction construction)
    {
        foreach (var id in ItemIds)
            for (int rank = id == "plasma-bolt" ? 2 : 1; rank <= MaximumRank; rank++)
                construction.Register(UpgradeCatalog.Definition(UpgradeCatalog.Find(id)!, rank));
    }
}

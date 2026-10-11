namespace MagicCarpetRemastered.Scripts.Equipment;

public enum EquipmentKind
{
    Projectile,
    Terrain,
    Defense,
    Mobility,
    Summoning,
    Economy
}

public sealed class EquipmentDefinition
{
    public EquipmentDefinition(string id, string displayName, EquipmentKind kind, int energyCost, float cooldownSeconds, string description)
    {
        Id = id;
        DisplayName = displayName;
        Kind = kind;
        EnergyCost = energyCost;
        CooldownSeconds = cooldownSeconds;
        Description = description;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public EquipmentKind Kind { get; }
    public int EnergyCost { get; }
    public float CooldownSeconds { get; }
    public string Description { get; }
    public int Rank { get; private set; } = 1;
    public string RankedName => Rank > 1 ? $"{DisplayName} {(Rank == 2 ? "II" : "III")}" : DisplayName;

    public void InstallRank(int rank) => Rank = System.Math.Clamp(rank, 1, ShipEquipmentCatalog.MaximumRank);

    public bool TryUpgrade()
    {
        if (Rank >= ShipEquipmentCatalog.MaximumRank) return false;
        Rank++;
        return true;
    }
}

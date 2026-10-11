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
    public string RankedName => Rank > 1 ? $"{DisplayName} II" : DisplayName;

    public bool TryUpgrade()
    {
        if (Rank >= 2) return false;
        Rank = 2;
        return true;
    }
}

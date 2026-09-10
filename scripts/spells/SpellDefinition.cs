namespace MagicCarpetRemastered.Scripts.Spells;

public enum SpellKind
{
    Projectile,
    Terrain,
    Defense,
    Mobility,
    Summoning,
    Economy
}

public sealed class SpellDefinition
{
    public SpellDefinition(string id, string displayName, SpellKind kind, int manaCost, float cooldownSeconds, string description)
    {
        Id = id;
        DisplayName = displayName;
        Kind = kind;
        ManaCost = manaCost;
        CooldownSeconds = cooldownSeconds;
        Description = description;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public SpellKind Kind { get; }
    public int ManaCost { get; }
    public float CooldownSeconds { get; }
    public string Description { get; }
}

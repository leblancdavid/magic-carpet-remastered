using Godot;
using MagicCarpetRemastered.Scripts.Enemies;

namespace MagicCarpetRemastered.Scripts.World;

public static class PlanetLayout
{
    public sealed record Site(string Id, string Title, string Description, Vector3 Up,
        string SpellId, string SpellName, EnemyEcosystemEnemy.EnemyRole Habitat, Color Color);

    public static Site[] Sites(HeightmapArena arena) => new[]
    {
        new Site("ridge", "Windward Ridge", "Swarm habitat · neutral mana · Firebolt II",
            new Vector3(-140, arena.PlanetRadius, -100).Normalized(), "firebolt", "Firebolt",
            EnemyEcosystemEnemy.EnemyRole.FlyingSwarm, new Color(0.65f, 0.8f, 0.95f)),
        new Site("wellfield", "Wellfield", "Contested mana · thieves · Arcane Burst II",
            Vector3.Up, "arcane-burst", "Arcane Burst", EnemyEcosystemEnemy.EnemyRole.ManaThief,
            new Color(0.2f, 0.85f, 0.95f)),
        new Site("shore", "Tidal Reach", "Beast habitat · neutral mana · Mana Shield II",
            new Vector3(40, arena.PlanetRadius, 155).Normalized(), "mana-shield", "Mana Shield",
            EnemyEcosystemEnemy.EnemyRole.GroundBeast, new Color(0.4f, 0.85f, 0.45f)),
        new Site("spire", "Redglass Spire", "Caster habitat · distant exploration · Wind Dash II",
            new Quaternion(Vector3.Forward, 0.9f) * Vector3.Up, "wind-dash", "Wind Dash",
            EnemyEcosystemEnemy.EnemyRole.RangedCaster, new Color(0.95f, 0.4f, 0.3f)),
        new Site("observatory", "Starfall Observatory", "Far-side exploration · casters · Guardian II",
            new Quaternion(new Vector3(1, 0, 1).Normalized(), 2.25f) * Vector3.Up,
            "guardian", "Guardian", EnemyEcosystemEnemy.EnemyRole.RangedCaster,
            new Color(0.7f, 0.5f, 0.95f))
    };
}

using Godot;
using MagicCarpetRemastered.Scripts.Enemies;

namespace MagicCarpetRemastered.Scripts.World;

public static class PlanetLayout
{
    public sealed record Site(string Id, string Title, string Description, Vector3 Up,
        string EquipmentId, string EquipmentName, EnemyEcosystemEnemy.EnemyRole Habitat, Color Color);

    public static Site[] Sites(HeightmapArena arena) => new[]
    {
        new Site("ridge", "Windward Ridge", "Swarm habitat · neutral antimatter · Plasma Bolt II",
            new Vector3(-140, arena.PlanetRadius, -100).Normalized(), "plasma-bolt", "Plasma Bolt",
            EnemyEcosystemEnemy.EnemyRole.FlyingSwarm, new Color(0.65f, 0.8f, 0.95f)),
        new Site("wellfield", "Wellfield", "Contested antimatter · thieves · Pulse Burst II",
            Vector3.Up, "pulse-burst", "Pulse Burst", EnemyEcosystemEnemy.EnemyRole.AntimatterThief,
            new Color(0.2f, 0.85f, 0.95f)),
        new Site("shore", "Tidal Reach", "Beast habitat · neutral antimatter · Energy Shield II",
            new Vector3(40, arena.PlanetRadius, 155).Normalized(), "energy-shield", "Energy Shield",
            EnemyEcosystemEnemy.EnemyRole.GroundBeast, new Color(0.4f, 0.85f, 0.45f)),
        new Site("spire", "Redglass Spire", "Caster habitat · distant exploration · Dash II",
            new Quaternion(Vector3.Forward, 0.9f) * Vector3.Up, "dash", "Dash",
            EnemyEcosystemEnemy.EnemyRole.RangedCaster, new Color(0.95f, 0.4f, 0.3f)),
        new Site("observatory", "Starfall Observatory", "Far-side exploration · casters · Guardian Drone II",
            new Quaternion(new Vector3(1, 0, 1).Normalized(), 2.25f) * Vector3.Up,
            "guardian-drone", "Guardian Drone", EnemyEcosystemEnemy.EnemyRole.RangedCaster,
            new Color(0.7f, 0.5f, 0.95f))
    };
}

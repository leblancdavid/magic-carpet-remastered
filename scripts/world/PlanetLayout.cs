using System;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;

namespace MagicCarpetRemastered.Scripts.World;

public sealed class PlanetLayout
{
    public sealed record Site(string Id, string Title, string Description, Vector3 Up,
        string EquipmentId, string EquipmentName, EnemyEcosystemEnemy.EnemyRole Habitat, Color Color);

    public sealed record EconomicSite(int Side, StrategicPointRole Role, Vector3 Up);

    public Vector3[] Starts { get; }
    public Site[] Landmarks { get; }
    public EconomicSite[] EconomicSites { get; }
    public Vector3[] AntimatterFields { get; }
    public Vector3[] MatterFields { get; }
    public Vector3[] Scenery { get; }
    public float MinimumStartDistance { get; }
    public bool StartsOnDryGround { get; }
    public const float DefaultMinimumStartAngle = 120;

    private PlanetLayout(HeightmapArena arena, int seed, float minimumStartAngle)
    {
        if (!float.IsFinite(minimumStartAngle) || minimumStartAngle <= 0 || minimumStartAngle > 180)
            throw new ArgumentOutOfRangeException(nameof(minimumStartAngle), "Start separation must be greater than zero and at most 180 degrees.");
        MinimumStartDistance = Mathf.DegToRad(minimumStartAngle) * arena.PlanetRadius;
        using var random = new RandomNumberGenerator { Seed = unchecked((ulong)seed) };
        // Evaluate pairs together. Antipodal starts guarantee the minimum even
        // when the terrain is unsuitable; water-supported bases remain legal.
        Vector3 start = Vector3.Up;
        float bestScore = float.NegativeInfinity;
        for (int i = 0; i < 64; i++)
        {
            Vector3 candidate = RandomDirection(random);
            float score = Mathf.Min(StartTerrainScore(arena, candidate), StartTerrainScore(arena, -candidate));
            if (score <= bestScore) continue;
            start = candidate; bestScore = score;
        }
        Starts = new[] { start, -start };
        StartsOnDryGround = arena.HeightAt(Position(arena, start)) > arena.WaterHeight
            && arena.HeightAt(Position(arena, -start)) > arena.WaterHeight;
        if (arena.SurfaceDistance(Position(arena, start), Position(arena, -start)) + 1 < MinimumStartDistance)
            throw new InvalidOperationException("Planet layout cannot satisfy faction-start separation.");

        Vector3 forward = Mathf.Abs(start.Dot(Vector3.Forward)) < 0.9f ? Vector3.Forward : Vector3.Right;
        Basis frame = PlanetGeometry.Frame(start, forward);
        frame = new Basis(start, random.RandfRange(0, Mathf.Tau)) * frame;
        Landmarks = SitesInFrame(frame);
        EconomicSites = new EconomicSite[8];
        AntimatterFields = new Vector3[3];
        MatterFields = new Vector3[4];
        for (int side = 0; side < 2; side++)
        {
            float sign = side == 0 ? 1 : -1;
            Vector3 up = Starts[side];
            // Distances match for each faction and stay useful on the XL world.
            // Alternate bearings fan sites out rather than lining them up.
            float[] angles = { 0.38f, 0.92f, 0.68f, 0.38f };
            float[] bearings = { 0, 1.2f, 3.0f, 4.5f };
            for (int role = 0; role < 4; role++)
            {
                Vector3 tangent = frame.X.Rotated(up, bearings[role]);
                Vector3 direction = new Quaternion(up.Cross(tangent), angles[role]) * up;
                EconomicSites[side * 4 + role] = new EconomicSite(side, (StrategicPointRole)role, direction);
            }
            AntimatterFields[side] = Offset(up, frame.X * sign, 55 / arena.PlanetRadius);
            MatterFields[side * 2] = Offset(up, -frame.Z * sign, 45 / arena.PlanetRadius);
            MatterFields[side * 2 + 1] = frame * new Vector3(-0.65f * sign, 0, 0.76f * sign).Normalized();
        }
        AntimatterFields[2] = Landmarks[3].Up;

        // Twelve globally spread clusters, three small original obstacles each.
        // Keep all authored gameplay destinations and base approaches clear.
        var scenery = new System.Collections.Generic.List<Vector3>();
        for (int region = 0; region < RegionCount; region++)
        {
            Vector3 up = frame * RegionDirection(region);
            for (int i = 0; i < 3; i++)
            {
                Vector3 tangent = PlanetGeometry.Frame(up, Mathf.Abs(up.Z) < 0.9f ? Vector3.Forward : Vector3.Right).X.Rotated(up, i * Mathf.Tau / 3);
                Vector3 direction = Offset(up, tangent, (18 + i * 5) / arena.PlanetRadius);
                if (IsSceneryClear(direction, arena.PlanetRadius)) scenery.Add(direction);
            }
        }
        Scenery = scenery.ToArray();
    }

    public static PlanetLayout Create(HeightmapArena arena, int seed = 7319,
        float minimumStartAngle = DefaultMinimumStartAngle) => new(arena, seed, minimumStartAngle);

    // Pure geometry accessor retained for tools; live gameplay uses its one
    // terrain-aware layout instance rather than generating per-content layouts.
    public static Site[] Sites(HeightmapArena arena) => SitesInFrame(Basis.Identity);

    private static Site[] SitesInFrame(Basis frame) => new[]
    {
        new Site("ridge", "Windward Ridge", "Swarm habitat · northern frontier",
            frame * new Vector3(-0.76f, 0.65f, 0).Normalized(), "plasma-bolt", "Plasma Bolt",
            EnemyEcosystemEnemy.EnemyRole.FlyingSwarm, new Color(0.65f, 0.8f, 0.95f)),
        new Site("wellfield", "Wellfield", "Contested antimatter source · equatorial frontier",
            frame * Vector3.Right, "pulse-burst", "Pulse Burst", EnemyEcosystemEnemy.EnemyRole.AntimatterThief,
            new Color(0.2f, 0.85f, 0.95f)),
        new Site("shore", "Tidal Reach", "Beast habitat · remote frontier",
            frame * new Vector3(-0.5f, 0, -0.866f).Normalized(), "energy-shield", "Energy Shield",
            EnemyEcosystemEnemy.EnemyRole.GroundBeast, new Color(0.4f, 0.85f, 0.45f)),
        new Site("spire", "Redglass Spire", "Caster habitat · remote antimatter",
            frame * new Vector3(-0.5f, 0, 0.866f).Normalized(), "dash", "Dash",
            EnemyEcosystemEnemy.EnemyRole.RangedCaster, new Color(0.95f, 0.4f, 0.3f)),
        new Site("observatory", "Starfall Observatory", "Southern frontier · caster habitat",
            frame * new Vector3(0, -0.65f, 0.76f).Normalized(),
            "guardian-drone", "Guardian Drone", EnemyEcosystemEnemy.EnemyRole.RangedCaster,
            new Color(0.7f, 0.5f, 0.95f))
    };

    public static Vector3 Position(HeightmapArena arena, Vector3 up, float clearance = 0)
        => arena.OnSurface(arena.PlanetCenter + up * arena.PlanetRadius, clearance);

    private static Vector3 Offset(Vector3 up, Vector3 tangent, float angle)
        => new Quaternion(up.Cross(tangent.Normalized()).Normalized(), angle) * up;

    private bool IsSceneryClear(Vector3 up, float radius)
    {
        float Distance(Vector3 other) => Mathf.Acos(Mathf.Clamp(up.Dot(other), -1, 1)) * radius;
        foreach (Vector3 start in Starts) if (Distance(start) < 28) return false;
        foreach (Site site in Landmarks) if (Distance(site.Up) < 12) return false;
        foreach (EconomicSite site in EconomicSites) if (Distance(site.Up) < Mathf.Clamp(radius * 0.06f, 8, 18) + 5) return false;
        foreach (Vector3 field in AntimatterFields) if (Distance(field) < 18) return false;
        foreach (Vector3 field in MatterFields) if (Distance(field) < 14) return false;
        return true;
    }

    private static float StartTerrainScore(HeightmapArena arena, Vector3 up)
    {
        Vector3 point = Position(arena, up);
        float height = arena.HeightAt(point), variation = 0;
        Basis frame = PlanetGeometry.Frame(up, Mathf.Abs(up.Z) < 0.9f ? Vector3.Forward : Vector3.Right);
        foreach (Vector3 offset in new[] { frame.X * 9, -frame.X * 9, frame.Z * 9, -frame.Z * 9 })
            variation = Mathf.Max(variation, Mathf.Abs(arena.HeightAt(point + offset) - height));
        // Prefer ordinary lowlands over a calm-looking summit that would strand
        // the opening on a raised plateau when its footprint is flattened.
        return Mathf.Min(height - arena.WaterHeight, 4) - variation * 2
            - Mathf.Max(0, height - arena.WaterHeight - 4) * 0.75f;
    }

    private static Vector3 RandomDirection(RandomNumberGenerator random)
    {
        float y = random.RandfRange(-1, 1), angle = random.RandfRange(0, Mathf.Tau);
        float radial = Mathf.Sqrt(Mathf.Max(0, 1 - y * y));
        return new Vector3(radial * Mathf.Cos(angle), y, radial * Mathf.Sin(angle));
    }

    public const int RegionCount = 12;
    public static Vector3 RegionDirection(int index)
    {
        float y = 1 - 2 * (index + 0.5f) / RegionCount;
        float angle = index * 2.39996323f;
        float radial = Mathf.Sqrt(1 - y * y);
        return new Vector3(radial * Mathf.Cos(angle), y, radial * Mathf.Sin(angle));
    }

    public static int RegionFor(Vector3 up)
    {
        int region = 0; float best = float.NegativeInfinity;
        for (int i = 0; i < RegionCount; i++)
        {
            float dot = up.Dot(RegionDirection(i));
            if (dot > best) { best = dot; region = i; }
        }
        return region;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Game;

public partial class EnemyEcosystemDirector : Node
{
    private sealed record Habitat(string Id, Vector3 Position, EnemyEcosystemEnemy.EnemyRole Role,
        int Members = 3, bool Heavy = false, StrategicPoint? Site = null, float Spread = 24);
    [Export] public bool UseFixedSeed { get; set; }
    [Export] public int GenerationSeed { get; set; }
    private readonly RandomNumberGenerator _random = new();
    public override void _ExitTree() => _random.Dispose();
    [Export] public float SpawnIntervalSeconds { get; set; } = 8;
    [Export] public float ReplenishDelaySeconds { get; set; } = 35;
    [Export] public int MaxActiveEnemies { get; set; } = 24;
    [Export] public int InitialWaveCount { get; set; } = 24;
    [Export] public int MaximumPerHabitat { get; set; } = 10;
    [Export] public int MaxFlyingSwarms { get; set; } = 12;
    [Export] public int MaxGroundBeasts { get; set; } = 8;
    [Export] public int MaxRangedCasters { get; set; } = 8;
    [Export] public int MaxAntimatterThieves { get; set; } = 8;
    [Export] public float BaseExclusionDistance { get; set; } = 180;
    [Export] public float FlatBaseExclusionDistance { get; set; } = 30;
    [Export] public int PlacementAttempts { get; set; } = 64;
    [Export] public bool RaidsEnabled { get; set; } = true;
    [Export] public float RaidOpeningGraceSeconds { get; set; } = 180;
    [Export] public float RaidIntervalSeconds { get; set; } = 180;
    [Export] public int MaxBaseAttackers { get; set; } = 1;
    [Export] public float RaidSpawnDistance { get; set; } = 180;
    [Export] public float FlatRaidSpawnDistance { get; set; } = 45;
    [Export] public float RaidLifetimeSeconds { get; set; } = 90;
    [Export] public int RaidDamage { get; set; } = 4;
    [Export] public float RaidAttackCooldownSeconds { get; set; } = 3;

    public float RaidRemaining => _raidTimer;
    public long SpawnedRaids { get; private set; }
    public float EffectiveBaseExclusion => _arena?.IsSpherical == true ? BaseExclusionDistance : FlatBaseExclusionDistance;
    public string RaidFeedbackMessage => _feedbackRemaining > 0 ? _raidFeedback : string.Empty;
    public string StatusText => $"Ecosystem: {_activeByRole[0]} swarms, {_activeByRole[1]} beasts, {_activeByRole[2]} casters, {_activeByRole[4]} scavengers, {_activeRaiderCount} raiders, {_activeByRole[5]} pilots\n"
        + $"Local habitats · raids {(RaidsEnabled ? $"next check {_raidTimer:0}s · {SpawnedRaids} launched" : "disabled")}";

    private HeightmapArena? _arena;
    private readonly List<Habitat> _habitats = new();
    private readonly Dictionary<string, float> _replenishTimers = new();
    private readonly Dictionary<string, int> _lastPopulation = new();
    private readonly Dictionary<string, int> _populationTargets = new();
    private readonly Dictionary<string, int> _activeByHabitat = new();
    private readonly HashSet<string> _ownedHabitats = new();
    private readonly int[] _activeByRole = new int[6];
    private int _activeRaiderCount;
    private float _raidTimer;
    private float _feedbackRemaining;
    private string _raidFeedback = string.Empty;
    private int _habitatCursor;
    private bool _nextRaidPlayer = true;
    private bool _seeding = true;
    private float _populationClock;
    public int HabitatCount => _habitats.Count;

    public override void _Ready()
    {
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        AddToGroup("enemy_ecosystem_director");
        if (_arena == null) return;
        if (UseFixedSeed) _random.Seed = unchecked((ulong)GenerationSeed); else _random.Randomize();
        if (_arena.IsSpherical)
        {
            int population = _arena.PlanetRadius <= 200 ? 96 : _arena.PlanetRadius <= 300 ? 180 : _arena.PlanetRadius <= 400 ? 300 : 450;
            if (MaxActiveEnemies == 24) MaxActiveEnemies = population;
            if (InitialWaveCount == 24) InitialWaveCount = population;
            if (MaxFlyingSwarms == 12) MaxFlyingSwarms = population * 3 / 4;
            if (MaxGroundBeasts == 8) MaxGroundBeasts = population / 3;
            if (MaxRangedCasters == 8) MaxRangedCasters = population / 3;
            if (MaxAntimatterThieves == 8) MaxAntimatterThieves = population / 3;
            if (BaseExclusionDistance == 180) BaseExclusionDistance = Mathf.Clamp(_arena.PlanetRadius * 0.3f, 30, 80);
            if (RaidSpawnDistance == 180) RaidSpawnDistance = Mathf.Clamp(_arena.PlanetRadius * 0.35f, 36, 100);
        }
        foreach (Node node in GetTree().GetNodesInGroup("world_landmark"))
            if (node is WorldLandmark landmark && !landmark.IsQueuedForDeletion())
                _habitats.Add(new Habitat(landmark.SiteId, landmark.GlobalPosition, landmark.Habitat,
                    landmark.Habitat == EnemyEcosystemEnemy.EnemyRole.FlyingSwarm ? 6 : 2));
        // Interleave corresponding faction routes rather than exhausting one
        // hemisphere's population budget first on the smallest world.
        foreach (var site in GetTree().GetNodesInGroup("strategic_point").OfType<StrategicPoint>().OrderBy(site => site.Role).ThenBy(site => site.Name.ToString()))
            _habitats.Add(new Habitat($"route-{site.Name}", site.GlobalPosition,
                site.Role == StrategicPointRole.Research ? EnemyEcosystemEnemy.EnemyRole.RangedCaster
                : site.Role == StrategicPointRole.Logistics ? EnemyEcosystemEnemy.EnemyRole.AntimatterThief
                : EnemyEcosystemEnemy.EnemyRole.GroundBeast, Members: 2, Site: site));
        if (_arena.IsSpherical) AddRandomHabitats();
        if (_habitats.Count == 0)
        {
            // Fixed flat corners, never offsets from a pilot's travelling position.
            Vector2[] corners = { new(-46, 46), new(46, 46), new(-46, -46), new(46, -46) };
            for (int i = 0; i < corners.Length; i++)
                _habitats.Add(new Habitat($"region-{i}", _arena.FromMap(corners[i].X, corners[i].Y, 0), OrdinaryRoles[i]));
        }
        int initial = 0;
        // Seed every region first, then finish whole packs rather than thinning
        // all swarms to pairs. Bounded failures leave room for later habitats.
        foreach (var habitat in _habitats)
            if (initial < InitialWaveCount && TrySpawnNeutral(habitat)) initial++;
        foreach (var habitat in _habitats)
            for (int member = HabitatPopulation(habitat); member < HabitatCapacity(habitat) && initial < InitialWaveCount; member++)
                if (TrySpawnNeutral(habitat)) initial++;
        foreach (var habitat in _habitats)
            _populationTargets[habitat.Id] = _lastPopulation[habitat.Id] = HabitatPopulation(habitat);
        _raidTimer = Mathf.Max(0, RaidOpeningGraceSeconds);
        _seeding = false;
    }

    private void AddRandomHabitats()
    {
        if (_arena == null) return;
        int desired = InitialWaveCount / 4 + 8;
        // Seed broad coverage, then mix isolated habitats and nearby clusters.
        // Unequal spacing avoids a regular grid without abandoning any region.
        for (int index = 0; _habitats.Count < desired && index < desired * 2; index++)
        {
            int region = index % PlanetLayout.RegionCount;
            int roll = _random.RandiRange(0, 99);
            var role = roll < 55 ? EnemyEcosystemEnemy.EnemyRole.FlyingSwarm : roll < 80
                ? EnemyEcosystemEnemy.EnemyRole.GroundBeast : roll < 95 ? EnemyEcosystemEnemy.EnemyRole.RangedCaster
                : EnemyEcosystemEnemy.EnemyRole.AntimatterThief;
            bool heavy = role == EnemyEcosystemEnemy.EnemyRole.GroundBeast && _random.Randf() < 0.4f;
            int members = role == EnemyEcosystemEnemy.EnemyRole.FlyingSwarm ? _random.RandiRange(6, 10)
                : role == EnemyEcosystemEnemy.EnemyRole.GroundBeast ? heavy ? _random.RandiRange(1, 3) : _random.RandiRange(2, 5)
                : role == EnemyEcosystemEnemy.EnemyRole.RangedCaster ? _random.RandiRange(2, 4) : _random.RandiRange(1, 3);
            float spacing = _random.RandfRange(22, 65);
            var neighbors = _habitats.Where(h => h.Id.StartsWith("wild-") && PlanetLayout.RegionFor(_arena.UpAt(h.Position)) == region).ToArray();
            Habitat? cluster = neighbors.Length > 0 && _random.Randf() < 0.65f ? neighbors[_random.RandiRange(0, neighbors.Length - 1)] : null;
            for (int attempt = 0; attempt < PlacementAttempts * 12; attempt++)
            {
                float y = _random.RandfRange(-1, 1), angle = _random.RandfRange(0, Mathf.Tau);
                float radial = Mathf.Sqrt(Mathf.Max(0, 1 - y * y));
                Vector3 up = new(radial * Mathf.Cos(angle), y, radial * Mathf.Sin(angle));
                if (cluster != null)
                {
                    Vector3 clusterUp = _arena.UpAt(cluster.Position);
                    Basis frame = PlanetGeometry.Frame(clusterUp, Vector3.Forward);
                    Vector3 tangent = frame.X.Rotated(clusterUp, angle);
                    up = new Quaternion(clusterUp.Cross(tangent), _random.RandfRange(spacing, spacing + 45) / _arena.PlanetRadius) * clusterUp;
                }
                if (PlanetLayout.RegionFor(up) != region) continue;
                Vector3 center = PlanetLayout.Position(_arena, up);
                if (role != EnemyEcosystemEnemy.EnemyRole.FlyingSwarm && _arena.HeightAt(center) < _arena.WaterHeight) continue;
                if (!IsSpawnPositionValid(center) || _habitats.Any(h => _arena.SurfaceDistance(center, h.Position) < spacing)) continue;
                _habitats.Add(new Habitat($"wild-{index}", center, role, members, heavy,
                    Spread: _random.RandfRange(role == EnemyEcosystemEnemy.EnemyRole.FlyingSwarm ? 16 : 12, 32)));
                break;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GetTree().Paused || !double.IsFinite(delta) || delta <= 0 || _arena == null) return;
        float elapsed = (float)delta;
        _feedbackRemaining = Mathf.Max(0, _feedbackRemaining - elapsed);
        if (RaidsEnabled)
        {
            _raidTimer = Mathf.Max(0, _raidTimer - elapsed);
            if (_raidTimer <= 0)
                _raidTimer = TrySpawnRaid() ? Mathf.Max(1, RaidIntervalSeconds) : 10;
        }
        _populationClock += elapsed;
        if (_populationClock < 0.5f) return;
        elapsed = _populationClock; _populationClock = 0;
        RefreshCounts();
        foreach (var habitat in _habitats)
        {
            int count = HabitatPopulation(habitat);
            if (IsOwned(habitat))
            {
                _ownedHabitats.Add(habitat.Id);
                _lastPopulation[habitat.Id] = count;
                _replenishTimers[habitat.Id] = Mathf.Max(0, ReplenishDelaySeconds);
                continue;
            }
            if (_ownedHabitats.Remove(habitat.Id))
            {
                _replenishTimers[habitat.Id] = Mathf.Max(0, ReplenishDelaySeconds);
                _lastPopulation[habitat.Id] = count;
                continue;
            }
            if (_lastPopulation.GetValueOrDefault(habitat.Id) > count)
                _replenishTimers[habitat.Id] = Mathf.Max(0, ReplenishDelaySeconds);
            float remaining = _replenishTimers.GetValueOrDefault(habitat.Id);
            _replenishTimers[habitat.Id] = Mathf.Max(0, remaining - elapsed);
            if (_replenishTimers[habitat.Id] <= 0 && count < _populationTargets.GetValueOrDefault(habitat.Id))
            {
                TrySpawnNeutral(habitat);
                // Failed/capped placements are bounded retries, not 64 new
                // terrain probes per habitat on every physics frame.
                _replenishTimers[habitat.Id] = Mathf.Max(1, SpawnIntervalSeconds);
            }
            _lastPopulation[habitat.Id] = HabitatPopulation(habitat);
        }
        RefreshCounts();
    }

    private static readonly EnemyEcosystemEnemy.EnemyRole[] OrdinaryRoles =
    { EnemyEcosystemEnemy.EnemyRole.FlyingSwarm, EnemyEcosystemEnemy.EnemyRole.GroundBeast,
        EnemyEcosystemEnemy.EnemyRole.RangedCaster, EnemyEcosystemEnemy.EnemyRole.AntimatterThief };

    private void RefreshCounts()
    {
        Array.Clear(_activeByRole);
        _activeByHabitat.Clear();
        _activeRaiderCount = 0;
        foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
            if (node is EnemyEcosystemEnemy { Health: > 0 } enemy && !enemy.IsQueuedForDeletion())
            {
                _activeByRole[(int)enemy.Role]++;
                _activeByHabitat[enemy.HabitatId] = _activeByHabitat.GetValueOrDefault(enemy.HabitatId) + 1;
                if (enemy.IsRaider) _activeRaiderCount++;
            }
        foreach (Node node in GetTree().GetNodesInGroup("rival_ship"))
            if (node is MagicCarpetRemastered.Scripts.Player.ShipFlightController { Health: > 0 } ship && !ship.IsQueuedForDeletion()) _activeByRole[5]++;
    }

    private int RoleCap(EnemyEcosystemEnemy.EnemyRole role) => role switch
    {
        EnemyEcosystemEnemy.EnemyRole.FlyingSwarm => MaxFlyingSwarms,
        EnemyEcosystemEnemy.EnemyRole.GroundBeast => MaxGroundBeasts,
        EnemyEcosystemEnemy.EnemyRole.RangedCaster => MaxRangedCasters,
        EnemyEcosystemEnemy.EnemyRole.AntimatterThief => MaxAntimatterThieves,
        _ => 0
    };

    private int HabitatPopulation(Habitat habitat) => _activeByHabitat.GetValueOrDefault(habitat.Id);

    private int HabitatCapacity(Habitat habitat) => Mathf.Min(MaximumPerHabitat, habitat.Members);
    private static bool IsOwned(Habitat habitat) => IsInstanceValid(habitat.Site)
        && !habitat.Site!.IsQueuedForDeletion() && habitat.Site.Ownership != AntimatterOwnership.Neutral;

    public bool TrySpawnNeutral() => TrySpawnNeutral(null);

    private bool TrySpawnNeutral(Habitat? requested)
    {
        if (_arena == null || GetTree().Paused || _habitats.Count == 0) return false;
        RefreshCounts();
        if (_activeByRole[0] + _activeByRole[1] + _activeByRole[2] + _activeByRole[4] >= MaxActiveEnemies) return false;
        Habitat? habitat = requested; int smallest = MaximumPerHabitat;
        if (requested != null && IsOwned(requested)) return false;
        if (requested != null && HabitatPopulation(requested) >= HabitatCapacity(requested)) return false;
        if (requested == null)
        for (int i = 0; i < _habitats.Count; i++)
        {
            int index = (_habitatCursor + i) % _habitats.Count;
            var candidate = _habitats[index]; int count = HabitatPopulation(candidate);
            if (IsOwned(candidate) || count >= smallest || count >= HabitatCapacity(candidate) || _activeByRole[(int)candidate.Role] >= RoleCap(candidate.Role)) continue;
            habitat = candidate; smallest = count;
        }
        if (habitat == null) return false;
        if (!_seeding && GetTree().GetNodesInGroup("pilots").OfType<Node3D>()
            .Any(p => NeutralTerritory.IsLivingPilot(p) && _arena.SurfaceDistance(p.GlobalPosition, habitat.Position) < 64)) return false;
        _habitatCursor = (_habitats.IndexOf(habitat) + 1) % _habitats.Count;
        var role = habitat.Role;
        if (_activeByRole[(int)role] >= RoleCap(role))
        {
            return false;
        }
        float clearance = role is EnemyEcosystemEnemy.EnemyRole.GroundBeast or EnemyEcosystemEnemy.EnemyRole.AntimatterThief ? 1.1f : 5.5f;
        if (!TryPlace(habitat.Position, 2, _arena.IsSpherical ? habitat.Spread : 14, clearance, out Vector3 position,
            landOnly: habitat.Site == null && role != EnemyEcosystemEnemy.EnemyRole.FlyingSwarm)) return false;
        GetParent().AddChild(new EnemyEcosystemEnemy { Name = $"{role}Enemy", Role = role,
            Position = GetParent<Node3D>().ToLocal(position), HabitatPosition = habitat.Position, HabitatId = habitat.Id,
            IsHeavy = habitat.Heavy, WanderSeed = unchecked((int)_random.Randi()), NeutralAntimatterValue = habitat.Heavy ? 72 : role == EnemyEcosystemEnemy.EnemyRole.FlyingSwarm ? 6
                : role == EnemyEcosystemEnemy.EnemyRole.GroundBeast ? 36 : 20 });
        RefreshCounts();
        int population = HabitatPopulation(habitat);
        _lastPopulation[habitat.Id] = population;
        _populationTargets[habitat.Id] = Math.Max(population, _populationTargets.GetValueOrDefault(habitat.Id));
        return true;
    }

    // The same placement guards apply to normal populations and raids. Failure
    // allocates no actor and never falls back to an unsafe/player-relative point.
    private bool TryPlace(Vector3 center, float minimum, float maximum, float clearance, out Vector3 position, bool landOnly = false)
    {
        position = default;
        if (_arena == null) return false;
        Vector3 up = _arena.UpAt(center);
        Basis frame = PlanetGeometry.Frame(up, Mathf.Abs(up.Z) < 0.9f ? Vector3.Forward : Vector3.Right);
        for (int i = 0; i < PlacementAttempts; i++)
        {
            float angle = _random.Randf() * Mathf.Tau, distance = Mathf.Lerp(minimum, maximum, _random.Randf());
            Vector3 tangent = frame.X.Rotated(up, angle);
            Vector3 point = _arena.IsSpherical
                ? _arena.PlanetCenter + new Quaternion(up.Cross(tangent), distance / _arena.PlanetRadius) * up * _arena.PlanetRadius
                : center + tangent * distance;
            point = _arena.OnSurface(point, clearance);
            if (landOnly && _arena.HeightAt(point) < _arena.WaterHeight) continue;
            if (!IsSpawnPositionValid(point)) continue;
            position = point; return true;
        }
        return false;
    }

    public bool IsSpawnPositionValid(Vector3 point)
    {
        if (_arena == null || !point.IsFinite()) return false;
        if (!_arena.IsSpherical && (Mathf.Abs(point.X) > _arena.CellsPerSide * _arena.CellSize * 0.48f
            || Mathf.Abs(point.Z) > _arena.CellsPerSide * _arena.CellSize * 0.48f)) return false;
        foreach (string group in new[] { "player_base", "enemy_base" })
            foreach (Node node in GetTree().GetNodesInGroup(group))
                if (node is FactionBase hub && !hub.IsQueuedForDeletion() && _arena.SurfaceDistance(point, hub.GlobalPosition) < EffectiveBaseExclusion) return false;
        foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
            if (node is EnemyEcosystemEnemy { Health: > 0 } enemy && !enemy.IsQueuedForDeletion()
                && _arena.SurfaceDistance(point, enemy.GlobalPosition) < 6) return false;
        foreach (Node node in GetTree().GetNodesInGroup("scenery"))
            if (node is Node3D obstacle && _arena.SurfaceDistance(point, obstacle.GlobalPosition) < 4) return false;
        Basis frame = PlanetGeometry.Frame(_arena.UpAt(point), Mathf.Abs(_arena.UpAt(point).Z) < 0.9f ? Vector3.Forward : Vector3.Right);
        float surface = _arena.SurfaceHeight(point);
        foreach (Vector3 offset in new[] { frame.X * 2, -frame.X * 2, frame.Z * 2, -frame.Z * 2 })
            if (Mathf.Abs(_arena.SurfaceHeight(point + offset) - surface) > 4) return false;
        return true;
    }

    private bool TrySpawnRaid()
    {
        if (_arena == null || !RaidsEnabled || GetTree().Paused) return false;
        RefreshCounts();
        if (_activeRaiderCount >= MaxBaseAttackers) return false;
        FactionBase? target = GetTree().GetFirstNodeInGroup(_nextRaidPlayer ? "player_base" : "enemy_base") as FactionBase;
        if (!IsInstanceValid(target) || target!.IsDestroyed)
            target = GetTree().GetFirstNodeInGroup(_nextRaidPlayer ? "enemy_base" : "player_base") as FactionBase;
        if (!IsInstanceValid(target) || target!.IsDestroyed) return false;
        float distance = Mathf.Max(EffectiveBaseExclusion, _arena.IsSpherical ? RaidSpawnDistance : FlatRaidSpawnDistance);
        if (!TryPlace(target.GlobalPosition, distance + 1, distance + (_arena.IsSpherical ? 40 : 8), 1.1f, out Vector3 position)) return false;
        var raider = new EnemyEcosystemEnemy { Name = "NeutralRaider", Role = EnemyEcosystemEnemy.EnemyRole.BaseAttacker,
            Position = GetParent<Node3D>().ToLocal(position), RaidTarget = target, HabitatPosition = target.GlobalPosition,
            HabitatLeashRadius = distance + 80, RaidLifetimeSeconds = RaidLifetimeSeconds };
        GetParent().AddChild(raider);
        // Role setup runs in Ready. Apply the gentler raid-specific tuning after it.
        raider.ContactDamage = Mathf.Max(0, RaidDamage);
        raider.AttackCooldownSeconds = Mathf.Max(0.1f, RaidAttackCooldownSeconds);
        _nextRaidPlayer = !target.IsPlayerBase;
        SpawnedRaids++;
        _raidFeedback = target.IsPlayerBase ? "RAID INBOUND · defend home" : "RAID INBOUND · rival base";
        _feedbackRemaining = 8;
        RefreshCounts(); return true;
    }
}

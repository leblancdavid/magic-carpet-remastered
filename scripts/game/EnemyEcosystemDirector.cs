using System;
using System.Collections.Generic;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Game;

public partial class EnemyEcosystemDirector : Node
{
    private sealed record Habitat(string Id, Vector3 Position, EnemyEcosystemEnemy.EnemyRole Role);
    [Export] public float SpawnIntervalSeconds { get; set; } = 30;
    [Export] public int MaxActiveEnemies { get; set; } = 10;
    [Export] public int InitialWaveCount { get; set; } = 4;
    [Export] public int MaximumPerHabitat { get; set; } = 2;
    [Export] public int MaxFlyingSwarms { get; set; } = 3;
    [Export] public int MaxGroundBeasts { get; set; } = 2;
    [Export] public int MaxRangedCasters { get; set; } = 2;
    [Export] public int MaxAntimatterThieves { get; set; } = 2;
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
    private readonly int[] _activeByRole = new int[6];
    private int _activeRaiderCount;
    private float _spawnTimer;
    private float _raidTimer;
    private float _feedbackRemaining;
    private string _raidFeedback = string.Empty;
    private int _habitatCursor;
    private bool _nextRaidPlayer = true;

    public override void _Ready()
    {
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        AddToGroup("enemy_ecosystem_director");
        if (_arena == null) return;
        if (_arena.IsSpherical)
        {
            if (BaseExclusionDistance == 180) BaseExclusionDistance = Mathf.Clamp(_arena.PlanetRadius * 0.3f, 30, 80);
            if (RaidSpawnDistance == 180) RaidSpawnDistance = Mathf.Clamp(_arena.PlanetRadius * 0.35f, 36, 100);
        }
        foreach (Node node in GetTree().GetNodesInGroup("world_landmark"))
            if (node is WorldLandmark landmark && !landmark.IsQueuedForDeletion())
                _habitats.Add(new Habitat(landmark.SiteId, landmark.GlobalPosition, landmark.Habitat));
        if (_habitats.Count == 0)
        {
            // Fixed flat corners, never offsets from a pilot's travelling position.
            Vector2[] corners = { new(-46, 46), new(46, 46), new(-46, -46), new(46, -46) };
            for (int i = 0; i < corners.Length; i++)
                _habitats.Add(new Habitat($"region-{i}", _arena.FromMap(corners[i].X, corners[i].Y, 0), OrdinaryRoles[i]));
        }
        for (int i = 0; i < InitialWaveCount; i++) TrySpawnNeutral();
        _spawnTimer = Mathf.Max(1, SpawnIntervalSeconds);
        _raidTimer = Mathf.Max(0, RaidOpeningGraceSeconds);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GetTree().Paused || !double.IsFinite(delta) || delta <= 0 || _arena == null) return;
        float elapsed = (float)delta;
        _feedbackRemaining = Mathf.Max(0, _feedbackRemaining - elapsed);
        _spawnTimer -= elapsed;
        if (_spawnTimer <= 0)
        { TrySpawnNeutral(); _spawnTimer = Mathf.Max(1, SpawnIntervalSeconds); }
        if (RaidsEnabled)
        {
            _raidTimer = Mathf.Max(0, _raidTimer - elapsed);
            if (_raidTimer <= 0)
                _raidTimer = TrySpawnRaid() ? Mathf.Max(1, RaidIntervalSeconds) : 10;
        }
        RefreshCounts();
    }

    private static readonly EnemyEcosystemEnemy.EnemyRole[] OrdinaryRoles =
    { EnemyEcosystemEnemy.EnemyRole.FlyingSwarm, EnemyEcosystemEnemy.EnemyRole.GroundBeast,
        EnemyEcosystemEnemy.EnemyRole.RangedCaster, EnemyEcosystemEnemy.EnemyRole.AntimatterThief };

    private void RefreshCounts()
    {
        Array.Clear(_activeByRole);
        _activeRaiderCount = 0;
        foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
            if (node is EnemyEcosystemEnemy { Health: > 0 } enemy && !enemy.IsQueuedForDeletion())
            { _activeByRole[(int)enemy.Role]++; if (enemy.IsRaider) _activeRaiderCount++; }
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

    public bool TrySpawnNeutral()
    {
        if (_arena == null || GetTree().Paused || _habitats.Count == 0) return false;
        RefreshCounts();
        if (_activeByRole[0] + _activeByRole[1] + _activeByRole[2] + _activeByRole[4] >= MaxActiveEnemies) return false;
        Habitat? habitat = null; int smallest = MaximumPerHabitat;
        for (int i = 0; i < _habitats.Count; i++)
        {
            int index = (_habitatCursor + i) % _habitats.Count;
            var candidate = _habitats[index]; int count = 0;
            foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
                if (node is EnemyEcosystemEnemy { Health: > 0 } enemy && !enemy.IsQueuedForDeletion() && enemy.HabitatId == candidate.Id) count++;
            if (count >= smallest) continue;
            habitat = candidate; smallest = count;
        }
        if (habitat == null) return false;
        _habitatCursor = (_habitats.IndexOf(habitat) + 1) % _habitats.Count;
        var role = GD.Randf() < 0.65f ? habitat.Role : OrdinaryRoles[GD.Randi() % OrdinaryRoles.Length];
        if (_activeByRole[(int)role] >= RoleCap(role))
        {
            bool found = false;
            foreach (var fallback in OrdinaryRoles)
                if (_activeByRole[(int)fallback] < RoleCap(fallback)) { role = fallback; found = true; break; }
            if (!found) return false;
        }
        float clearance = role is EnemyEcosystemEnemy.EnemyRole.GroundBeast or EnemyEcosystemEnemy.EnemyRole.AntimatterThief ? 1.1f : 5.5f;
        if (!TryPlace(habitat.Position, _arena.IsSpherical ? 24 : 8, _arena.IsSpherical ? 40 : 14, clearance, out Vector3 position)) return false;
        GetParent().AddChild(new EnemyEcosystemEnemy { Name = $"{role}Enemy", Role = role,
            Position = GetParent<Node3D>().ToLocal(position), HabitatPosition = habitat.Position, HabitatId = habitat.Id });
        RefreshCounts(); return true;
    }

    // The same placement guards apply to normal populations and raids. Failure
    // allocates no actor and never falls back to an unsafe/player-relative point.
    private bool TryPlace(Vector3 center, float minimum, float maximum, float clearance, out Vector3 position)
    {
        position = default;
        if (_arena == null) return false;
        Vector3 up = _arena.UpAt(center);
        Basis frame = PlanetGeometry.Frame(up, Mathf.Abs(up.Z) < 0.9f ? Vector3.Forward : Vector3.Right);
        for (int i = 0; i < PlacementAttempts; i++)
        {
            float angle = GD.Randf() * Mathf.Tau, distance = Mathf.Lerp(minimum, maximum, GD.Randf());
            Vector3 tangent = frame.X.Rotated(up, angle);
            Vector3 point = _arena.IsSpherical
                ? _arena.PlanetCenter + new Quaternion(up.Cross(tangent), distance / _arena.PlanetRadius) * up * _arena.PlanetRadius
                : center + tangent * distance;
            point = _arena.OnSurface(point, clearance);
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

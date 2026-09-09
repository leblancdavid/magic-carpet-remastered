using System;
using Godot;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Game;

public partial class EnemyEcosystemDirector : Node
{
    private struct SpawnZone
    {
        public SpawnZone(string name, Vector3 position, float intensityBias)
        {
            Name = name;
            Position = position;
            IntensityBias = intensityBias;
        }

        public string Name { get; }
        public Vector3 Position { get; }
        public float IntensityBias { get; }
    }

    private readonly SpawnZone[] _zones =
    {
        new("Windward Ridge", new Vector3(-6.0f, 0.0f, 18.0f), 0.2f),
        new("Lowland Shore", new Vector3(20.0f, 0.0f, -8.0f), 0.45f),
        new("Wellfield", new Vector3(4.0f, 0.0f, 4.0f), 0.6f),
        new("Castle Ruins", new Vector3(18.0f, 0.0f, -14.0f), 0.85f)
    };

    [Export] public float SpawnIntervalSeconds { get; set; } = 7.5f;
    [Export] public int MaxActiveEnemies { get; set; } = 10;
    [Export] public int InitialWaveCount { get; set; } = 4;
    [Export] public float IntensityRampPerSecond { get; set; } = 0.02f;
    [Export] public int MaxFlyingSwarms { get; set; } = 3;
    [Export] public int MaxGroundBeasts { get; set; } = 2;
    [Export] public int MaxRangedCasters { get; set; } = 2;
    [Export] public int MaxCastleAttackers { get; set; } = 2;
    [Export] public int MaxManaThieves { get; set; } = 2;
    [Export] public int MaxEnemyWizards { get; set; } = 1;

    public float Intensity => _intensity;
    public string StatusText => $"Ecosystem: {_activeByRole[0]} swarm, {_activeByRole[1]} beasts, {_activeByRole[2]} casters, {_activeByRole[3]} siegers, {_activeByRole[4]} thieves, {_activeByRole[5]} wizards   Intensity {Intensity:0.00}";

    private HeightmapArena? _arena;
    private float _spawnTimer;
    private float _intensity = 0.25f;
    private readonly int[] _activeByRole = new int[6];

    public override void _Ready()
    {
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        AddToGroup("enemy_ecosystem_director");
        SpawnInitialWave();
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;
        _intensity = Mathf.Clamp(_intensity + IntensityRampPerSecond * deltaF, 0.1f, 1.0f);
        _spawnTimer -= deltaF;
        RefreshCounts();

        if (_spawnTimer > 0.0f || GetActiveEnemyCount() >= MaxActiveEnemies)
        {
            return;
        }

        SpawnEnemyForCurrentPressure();
        _spawnTimer = SpawnIntervalSeconds;
    }

    private void SpawnInitialWave()
    {
        for (int i = 0; i < InitialWaveCount; i++)
        {
            SpawnEnemyForCurrentPressure(initialWave: true, waveIndex: i);
        }

        _spawnTimer = 2.5f;
    }

    private void SpawnEnemyForCurrentPressure(bool initialWave = false, int waveIndex = 0)
    {
        if (_arena == null)
        {
            return;
        }

        SpawnZone zone = SelectSpawnZone();
        var role = SelectRole(zone, initialWave, waveIndex);
        if (GetRoleCount(role) >= GetRoleCap(role))
        {
            role = SelectFallbackRole(zone);
        }

        var enemy = new Enemies.EnemyEcosystemEnemy
        {
            Name = $"{role}Enemy",
            Role = role,
            Position = AboveTerrain(zone.Position.X, zone.Position.Z, role is Enemies.EnemyEcosystemEnemy.EnemyRole.GroundBeast or Enemies.EnemyEcosystemEnemy.EnemyRole.CastleAttacker ? 1.1f : 5.5f)
        };
        GetTree().CurrentScene.AddChild(enemy);
    }

    private SpawnZone SelectSpawnZone()
    {
        float pressure = Mathf.Clamp(_intensity, 0.0f, 1.0f);
        if (pressure < 0.35f)
        {
            return _zones[0];
        }

        if (pressure < 0.6f)
        {
            return _zones[1 + GD.Randi() % 2];
        }

        return _zones[2 + GD.Randi() % 2];
    }

    private Enemies.EnemyEcosystemEnemy.EnemyRole SelectRole(SpawnZone zone, bool initialWave, int waveIndex)
    {
        if (initialWave)
        {
            return waveIndex switch
            {
                0 => Enemies.EnemyEcosystemEnemy.EnemyRole.FlyingSwarm,
                1 => Enemies.EnemyEcosystemEnemy.EnemyRole.GroundBeast,
                2 => Enemies.EnemyEcosystemEnemy.EnemyRole.RangedCaster,
                _ => Enemies.EnemyEcosystemEnemy.EnemyRole.ManaThief
            };
        }

        float zonePressure = Mathf.Clamp(_intensity + zone.IntensityBias, 0.0f, 1.0f);
        if (zone.Name == "Wellfield")
        {
            return zonePressure < 0.55f
                ? Enemies.EnemyEcosystemEnemy.EnemyRole.ManaThief
                : Enemies.EnemyEcosystemEnemy.EnemyRole.EnemyWizard;
        }

        if (zone.Name == "Castle Ruins")
        {
            if (zonePressure < 0.45f)
            {
                return Enemies.EnemyEcosystemEnemy.EnemyRole.CastleAttacker;
            }

            return zonePressure < 0.7f
                ? Enemies.EnemyEcosystemEnemy.EnemyRole.EnemyWizard
                : Enemies.EnemyEcosystemEnemy.EnemyRole.CastleAttacker;
        }

        if (zone.Name == "Lowland Shore")
        {
            return zonePressure < 0.6f
                ? Enemies.EnemyEcosystemEnemy.EnemyRole.GroundBeast
                : Enemies.EnemyEcosystemEnemy.EnemyRole.FlyingSwarm;
        }

        return zonePressure < 0.45f
            ? Enemies.EnemyEcosystemEnemy.EnemyRole.FlyingSwarm
            : Enemies.EnemyEcosystemEnemy.EnemyRole.RangedCaster;
    }

    private int GetActiveEnemyCount()
    {
        return _activeByRole[0] + _activeByRole[1] + _activeByRole[2] + _activeByRole[3] + _activeByRole[4] + _activeByRole[5];
    }

    private void RefreshCounts()
    {
        Array.Clear(_activeByRole);
        foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
        {
            if (node is not Enemies.EnemyEcosystemEnemy enemy)
            {
                continue;
            }

            _activeByRole[(int)enemy.Role]++;
        }
    }

    private int GetRoleCount(Enemies.EnemyEcosystemEnemy.EnemyRole role)
    {
        return _activeByRole[(int)role];
    }

    private int GetRoleCap(Enemies.EnemyEcosystemEnemy.EnemyRole role)
    {
        return role switch
        {
            Enemies.EnemyEcosystemEnemy.EnemyRole.FlyingSwarm => MaxFlyingSwarms,
            Enemies.EnemyEcosystemEnemy.EnemyRole.GroundBeast => MaxGroundBeasts,
            Enemies.EnemyEcosystemEnemy.EnemyRole.RangedCaster => MaxRangedCasters,
            Enemies.EnemyEcosystemEnemy.EnemyRole.CastleAttacker => MaxCastleAttackers,
            Enemies.EnemyEcosystemEnemy.EnemyRole.ManaThief => MaxManaThieves,
            Enemies.EnemyEcosystemEnemy.EnemyRole.EnemyWizard => MaxEnemyWizards,
            _ => 1
        };
    }

    private Enemies.EnemyEcosystemEnemy.EnemyRole SelectFallbackRole(SpawnZone zone)
    {
        if (_activeByRole[(int)Enemies.EnemyEcosystemEnemy.EnemyRole.FlyingSwarm] < MaxFlyingSwarms)
        {
            return Enemies.EnemyEcosystemEnemy.EnemyRole.FlyingSwarm;
        }

        if (_activeByRole[(int)Enemies.EnemyEcosystemEnemy.EnemyRole.GroundBeast] < MaxGroundBeasts)
        {
            return Enemies.EnemyEcosystemEnemy.EnemyRole.GroundBeast;
        }

        if (zone.Name == "Castle Ruins" && _activeByRole[(int)Enemies.EnemyEcosystemEnemy.EnemyRole.CastleAttacker] < MaxCastleAttackers)
        {
            return Enemies.EnemyEcosystemEnemy.EnemyRole.CastleAttacker;
        }

        if (_activeByRole[(int)Enemies.EnemyEcosystemEnemy.EnemyRole.ManaThief] < MaxManaThieves)
        {
            return Enemies.EnemyEcosystemEnemy.EnemyRole.ManaThief;
        }

        return Enemies.EnemyEcosystemEnemy.EnemyRole.RangedCaster;
    }

    private Vector3 AboveTerrain(float x, float z, float clearance)
    {
        float height = _arena == null ? 0.0f : _arena.HeightAt(x, z);
        return new Vector3(x, height + clearance, z);
    }
}

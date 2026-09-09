using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Spells;

public partial class SpellProjectile : Area3D
{
    [Export] public float Speed { get; set; } = 42.0f;
    [Export] public float LifetimeSeconds { get; set; } = 2.0f;
    [Export] public float TerrainCraterRadius { get; set; } = 4.5f;
    [Export] public float TerrainCraterDepth { get; set; } = 2.2f;
    [Export] public int Damage { get; set; } = 25;

    private Vector3 _direction = Vector3.Forward;
    private CarpetFlightController? _owner;
    private float _lifeRemaining;

    public override void _Ready()
    {
        _lifeRemaining = LifetimeSeconds;
        BodyEntered += OnBodyEntered;

        var shape = new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 0.35f }
        };
        AddChild(shape);

        var mesh = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.35f, Height = 0.7f }
        };
        mesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(1.0f, 0.45f, 0.08f),
            EmissionEnabled = true,
            Emission = new Color(1.0f, 0.35f, 0.05f),
            EmissionEnergyMultiplier = 2.0f
        };
        AddChild(mesh);
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;
        GlobalPosition += _direction * Speed * deltaF;
        _lifeRemaining -= deltaF;

        if (_lifeRemaining <= 0.0f)
        {
            QueueFree();
        }
    }

    public void Launch(Vector3 direction, CarpetFlightController owner)
    {
        _direction = direction.Normalized();
        _owner = owner;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body == _owner)
        {
            return;
        }

        SpawnImpactBurst();

        if (body is SimpleMonster monster)
        {
            monster.ApplyDamage(Damage);
        }

        if (body is EnemyEcosystemEnemy ecosystemEnemy)
        {
            ecosystemEnemy.ApplyDamage(Damage);
        }

        if (body.IsInGroup("deformable_terrain") && GetTree().GetFirstNodeInGroup("heightmap_arena") is HeightmapArena arena)
        {
            arena.ApplyCrater(GlobalPosition, TerrainCraterRadius, TerrainCraterDepth);
        }

        QueueFree();
    }

    private void SpawnImpactBurst()
    {
        var burst = new SpellImpactBurst
        {
            Name = "SpellImpactBurst"
        };
        GetTree().CurrentScene.AddChild(burst);
        burst.GlobalPosition = GlobalPosition;
    }
}

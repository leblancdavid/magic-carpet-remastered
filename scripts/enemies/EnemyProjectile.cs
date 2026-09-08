using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Spells;

namespace MagicCarpetRemastered.Scripts.Enemies;

public partial class EnemyProjectile : Area3D
{
    [Export] public float Speed { get; set; } = 20.0f;
    [Export] public float LifetimeSeconds { get; set; } = 3.0f;
    [Export] public int Damage { get; set; } = 10;

    private Vector3 _direction = Vector3.Forward;
    private Node3D? _owner;
    private float _lifeRemaining;

    public override void _Ready()
    {
        _lifeRemaining = LifetimeSeconds;
        BodyEntered += OnBodyEntered;

        AddChild(new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 0.3f }
        });

        var mesh = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.3f, Height = 0.6f }
        };
        mesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.45f, 0.15f, 1.0f),
            EmissionEnabled = true,
            Emission = new Color(0.35f, 0.05f, 1.0f),
            EmissionEnergyMultiplier = 2.4f
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

    public void Launch(Vector3 direction, Node3D owner)
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

        if (body is CarpetFlightController player)
        {
            player.ApplyDamage(Damage);
        }

        QueueFree();
    }

    private void SpawnImpactBurst()
    {
        var burst = new SpellImpactBurst
        {
            Name = "EnemyImpactBurst"
        };
        GetTree().CurrentScene.AddChild(burst);
        burst.GlobalPosition = GlobalPosition;
    }
}

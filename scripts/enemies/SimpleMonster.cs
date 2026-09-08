using Godot;
using MagicCarpetRemastered.Scripts.Player;

namespace MagicCarpetRemastered.Scripts.Enemies;

public partial class SimpleMonster : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 7.0f;
    [Export] public int Health { get; set; } = 50;
    [Export] public int ContactDamage { get; set; } = 8;
    [Export] public float ContactCooldownSeconds { get; set; } = 0.8f;

    private CarpetFlightController? _target;
    private float _contactCooldown;

    public override void _Ready()
    {
        AddCollisionAndVisuals();
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;
        _contactCooldown = Mathf.Max(0.0f, _contactCooldown - deltaF);
        _target ??= GetTree().GetFirstNodeInGroup("player") as CarpetFlightController;

        if (_target == null)
        {
            return;
        }

        Vector3 toTarget = _target.GlobalPosition - GlobalPosition;
        float distance = toTarget.Length();
        Vector3 direction = distance > 0.1f ? toTarget.Normalized() : Vector3.Zero;

        Velocity = direction * MoveSpeed;
        MoveAndSlide();

        if (distance < 1.8f && _contactCooldown <= 0.0f)
        {
            _target.ApplyDamage(ContactDamage);
            _contactCooldown = ContactCooldownSeconds;
        }
    }

    public void ApplyDamage(int damage)
    {
        Health -= damage;
        if (Health <= 0)
        {
            QueueFree();
        }
    }

    private void AddCollisionAndVisuals()
    {
        var collision = new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 0.9f }
        };
        AddChild(collision);

        var mesh = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.9f, Height = 1.8f }
        };
        mesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.72f, 0.08f, 0.16f),
            Roughness = 0.9f
        };
        AddChild(mesh);
    }
}

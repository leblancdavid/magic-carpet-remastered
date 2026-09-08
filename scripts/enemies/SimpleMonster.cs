using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Enemies;

public partial class SimpleMonster : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 7.0f;
    [Export] public int Health { get; set; } = 50;
    [Export] public int ContactDamage { get; set; } = 8;
    [Export] public float ContactCooldownSeconds { get; set; } = 0.8f;
    [Export] public int ManaDroppedOnDeath { get; set; } = 15;

    private CarpetFlightController? _target;
    private StandardMaterial3D _material = null!;
    private float _contactCooldown;
    private float _hitFlashTimer;

    public override void _Ready()
    {
        AddCollisionAndVisuals();
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;
        _contactCooldown = Mathf.Max(0.0f, _contactCooldown - deltaF);
        _hitFlashTimer = Mathf.Max(0.0f, _hitFlashTimer - deltaF);
        _material.AlbedoColor = _hitFlashTimer > 0.0f
            ? new Color(1.0f, 0.9f, 0.55f)
            : _material.AlbedoColor.Lerp(new Color(0.72f, 0.08f, 0.16f), 10.0f * deltaF);
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
        _hitFlashTimer = 0.12f;
        if (Health <= 0)
        {
            DropMana();
            QueueFree();
        }
    }

    private void DropMana()
    {
        int dropCount = Mathf.Max(1, ManaDroppedOnDeath / 5);
        for (int i = 0; i < dropCount; i++)
        {
            float angle = Mathf.Tau * i / dropCount;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0.25f, Mathf.Sin(angle)) * 1.4f;
            var mana = new ManaPickup
            {
                Name = "DroppedMana",
                ManaAmount = ManaDroppedOnDeath / dropCount
            };
            GetTree().CurrentScene.AddChild(mana);
            mana.GlobalPosition = GlobalPosition + offset;
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
        _material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.72f, 0.08f, 0.16f),
            Roughness = 0.9f
        };
        mesh.MaterialOverride = _material;
        AddChild(mesh);
    }
}

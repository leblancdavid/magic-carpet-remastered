using Godot;
using MagicCarpetRemastered.Scripts.Enemies;

namespace MagicCarpetRemastered.Scripts.Spells;

public partial class SummonedGuardian : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 12.0f;
    [Export] public float LifetimeSeconds { get; set; } = 12.0f;
    [Export] public float AttackRadius { get; set; } = 2.0f;
    [Export] public float TargetRadius { get; set; } = 42.0f;
    [Export] public float AttackCooldownSeconds { get; set; } = 0.75f;
    [Export] public int Damage { get; set; } = 12;

    private StandardMaterial3D _material = null!;
    private float _lifeRemaining;
    private float _attackCooldown;

    public override void _Ready()
    {
        _lifeRemaining = LifetimeSeconds;
        AddCollisionAndVisuals();
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;
        _lifeRemaining -= deltaF;
        _attackCooldown = Mathf.Max(0.0f, _attackCooldown - deltaF);
        if (_lifeRemaining <= 0.0f)
        {
            QueueFree();
            return;
        }

        EnemyEcosystemEnemy? target = FindNearestTarget();
        if (target == null)
        {
            Velocity = Velocity.MoveToward(Vector3.Zero, MoveSpeed * deltaF);
            MoveAndSlide();
            PulseMaterial(deltaF);
            return;
        }

        Vector3 toTarget = target.GlobalPosition - GlobalPosition;
        float distance = toTarget.Length();
        Vector3 direction = distance > 0.001f ? toTarget / distance : Vector3.Zero;

        if (distance > AttackRadius)
        {
            Velocity = direction * MoveSpeed;
            MoveAndSlide();
        }
        else
        {
            Velocity = Vector3.Zero;
            if (_attackCooldown <= 0.0f)
            {
                target.ApplyDamage(Damage);
                _attackCooldown = AttackCooldownSeconds;
            }
        }

        PulseMaterial(deltaF);
    }

    private EnemyEcosystemEnemy? FindNearestTarget()
    {
        EnemyEcosystemEnemy? best = null;
        float bestDistance = TargetRadius;
        foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
        {
            if (node is not EnemyEcosystemEnemy enemy || enemy.Health <= 0)
            {
                continue;
            }

            float distance = GlobalPosition.DistanceTo(enemy.GlobalPosition);
            if (distance >= bestDistance)
            {
                continue;
            }

            best = enemy;
            bestDistance = distance;
        }

        return best;
    }

    private void PulseMaterial(float delta)
    {
        float pulse = 0.5f + Mathf.Sin(Time.GetTicksMsec() * 0.012f) * 0.5f;
        Color target = new Color(Mathf.Lerp(0.15f, 0.45f, pulse), 1.0f, Mathf.Lerp(0.45f, 0.95f, pulse));
        _material.AlbedoColor = _material.AlbedoColor.Lerp(target, Mathf.Clamp(delta * 12.0f, 0.0f, 1.0f));
    }

    private void AddCollisionAndVisuals()
    {
        AddChild(new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 0.7f }
        });

        var mesh = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.65f, Height = 1.3f }
        };
        _material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.2f, 1.0f, 0.65f),
            EmissionEnabled = true,
            Emission = new Color(0.08f, 0.75f, 0.45f),
            EmissionEnergyMultiplier = 1.8f,
            Roughness = 0.45f
        };
        mesh.MaterialOverride = _material;
        AddChild(mesh);
    }
}

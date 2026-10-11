using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Equipment;

public partial class GuardianDrone : CharacterBody3D
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
    private HeightmapArena? _arena;

    public override void _Ready()
    {
        AddToGroup("guardian-drone");
        _lifeRemaining = LifetimeSeconds;
        AddCollisionAndVisuals();
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        _arena?.ClampFlightBody(this);
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
            if (_arena != null) _arena.MoveBody(this); else MoveAndSlide();
            _arena?.ClampFlightBody(this);
            PulseMaterial(deltaF);
            return;
        }

        Vector3 toTarget = target.GlobalPosition - GlobalPosition;
        float distance = toTarget.Length();
        Vector3 direction = distance > 0.001f ? toTarget / distance : Vector3.Zero;
        if (_arena?.IsSpherical == true)
        {
            direction = _arena.DirectionTo(GlobalPosition, target.GlobalPosition);
            float radial = Mathf.Clamp((_arena.Clearance(target.GlobalPosition) - _arena.Clearance(GlobalPosition)) * 2, -4, 4);
            direction = (direction + _arena.UpAt(GlobalPosition) * radial / MoveSpeed).Normalized();
            _arena.Orient(this, _arena.Tangent(direction, GlobalPosition));
        }

        if (distance > AttackRadius)
        {
            Velocity = direction * MoveSpeed;
            if (_arena != null) _arena.MoveBody(this); else MoveAndSlide();
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
        _arena?.ClampFlightBody(this);
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

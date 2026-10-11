using Godot;
using MagicCarpetRemastered.Scripts.Audio;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Enemies;

public partial class SimpleMonster : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 7.0f;
    [Export] public int Health { get; set; } = 50;
    [Export] public int ContactDamage { get; set; } = 8;
    [Export] public float ContactCooldownSeconds { get; set; } = 0.8f;
    [Export] public int RangedDamage { get; set; } = 10;
    [Export] public float RangedAttackMinDistance { get; set; } = 6.0f;
    [Export] public float RangedAttackMaxDistance { get; set; } = 24.0f;
    [Export] public float RangedAttackCooldownSeconds { get; set; } = 2.4f;
    [Export] public float RangedAttackWindupSeconds { get; set; } = 0.45f;
    [Export] public int AntimatterDroppedOnDeath { get; set; } = 15;
    [Export] public float AntimatterContestRadius { get; set; } = 14.0f;
    [Export] public float AntimatterContestIntervalSeconds { get; set; } = 1.0f;
    [Export] public int AntimatterContestAmount { get; set; } = 4;
    public int AntimatterReserve => _antimatterReserve;

    private ShipFlightController? _target;
    private AntimatterSource? _antimatterSource;
    private StandardMaterial3D _material = null!;
    private float _contactCooldown;
    private float _rangedAttackCooldown = 1.2f;
    private float _rangedAttackWindup;
    private float _antimatterContestCooldown;
    private float _hitFlashTimer;
    private int _antimatterReserve;
    private HeightmapArena? _arena;

    public override void _Ready()
    {
        AddToGroup("simple_monster");
        _antimatterReserve = Mathf.Max(0, AntimatterDroppedOnDeath);
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        AddCollisionAndVisuals();
        _arena?.ClampFlightBody(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;
        _contactCooldown = Mathf.Max(0.0f, _contactCooldown - deltaF);
        _rangedAttackCooldown = Mathf.Max(0.0f, _rangedAttackCooldown - deltaF);
        _hitFlashTimer = Mathf.Max(0.0f, _hitFlashTimer - deltaF);
        _antimatterContestCooldown = Mathf.Max(0.0f, _antimatterContestCooldown - deltaF);
        UpdateColor(deltaF);
        _target ??= GetTree().GetFirstNodeInGroup("player") as ShipFlightController;
        _antimatterSource ??= GetTree().GetFirstNodeInGroup("antimatter_source") as AntimatterSource;

        if (_target == null)
        {
            return;
        }

        Vector3 toTarget = _target.GlobalPosition - GlobalPosition;
        float distance = toTarget.Length();
        Vector3 direction = distance > 0.1f ? toTarget.Normalized() : Vector3.Zero;
        if (_arena?.IsSpherical == true)
        {
            direction = _arena.DirectionTo(GlobalPosition, _target.GlobalPosition);
            float radial = Mathf.Clamp((_arena.Clearance(_target.GlobalPosition) - _arena.Clearance(GlobalPosition)) * 2, -5, 5);
            direction = (direction + _arena.UpAt(GlobalPosition) * radial / MoveSpeed).Normalized();
        }

        if (ShouldContestAntimatterSource(distance))
        {
            ContestAntimatterSource();
            if (_antimatterSource != null)
            {
                Vector3 toWell = _antimatterSource.GlobalPosition - GlobalPosition;
                float wellDistance = toWell.Length();
                direction = wellDistance > 0.1f ? toWell.Normalized() : Vector3.Zero;
                Velocity = direction * (MoveSpeed * 1.15f);
                if (_arena != null) _arena.MoveBody(this); else MoveAndSlide();
                _arena?.ClampFlightBody(this);
                UpdateRangedAttack(deltaF, wellDistance, direction);
                return;
            }
        }

        Velocity = direction * MoveSpeed;
        if (_arena != null) _arena.MoveBody(this); else MoveAndSlide();
        _arena?.ClampFlightBody(this);

        UpdateRangedAttack(deltaF, distance, direction);

        if (distance < 1.8f && _contactCooldown <= 0.0f)
        {
            _target.ApplyDamage(ContactDamage);
            _contactCooldown = ContactCooldownSeconds;
        }
    }

    public void ApplyDamage(int damage)
    {
        if (damage <= 0 || Health <= 0)
        {
            return;
        }

        Health -= damage;
        _hitFlashTimer = 0.12f;
        GameAudio.Instance?.PlayHit();
        if (Health <= 0)
        {
            DropAntimatter();
            SpawnDeathBurst();
            QueueFree();
        }
    }

    public void AbsorbAntimatter(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        _antimatterReserve += amount;
        Health = Mathf.Min(Health + Mathf.Max(1, amount / 3), 50);
    }

    private void DropAntimatter()
    {
        if (_antimatterReserve <= 0)
        {
            return;
        }

        int amount = _antimatterReserve;
        _antimatterReserve = 0;
        AntimatterScatter.Spawn(GetParent(), GlobalPosition, amount, name: "DroppedAntimatter");
    }

    private void SpawnDeathBurst()
    {
        var burst = new EquipmentImpactBurst
        {
            Name = "EnemyDeathBurst",
            BurstColor = new Color(1.0f, 0.12f, 0.08f, 0.9f),
            EmissionColor = new Color(1.0f, 0.05f, 0.02f),
            DurationSeconds = 0.45f,
            StartScale = 0.8f,
            EndScale = 3.8f
        };
        GetTree().CurrentScene.AddChild(burst);
        burst.GlobalPosition = GlobalPosition;
    }

    private void UpdateRangedAttack(float delta, float distanceToTarget, Vector3 directionToTarget)
    {
        if (_target == null)
        {
            return;
        }

        if (_rangedAttackWindup > 0.0f)
        {
            _rangedAttackWindup -= delta;
            if (_rangedAttackWindup <= 0.0f)
            {
                FireRangedAttack((_target.GlobalPosition - GlobalPosition).Normalized());
            }

            return;
        }

        bool targetInRange = distanceToTarget >= RangedAttackMinDistance && distanceToTarget <= RangedAttackMaxDistance;
        if (!targetInRange || _rangedAttackCooldown > 0.0f || directionToTarget == Vector3.Zero)
        {
            return;
        }

        _rangedAttackWindup = RangedAttackWindupSeconds;
        _rangedAttackCooldown = RangedAttackCooldownSeconds;
    }

    private bool ShouldContestAntimatterSource(float distanceToPlayer)
    {
        if (_antimatterSource == null || _antimatterSource.StoredAntimatter <= 0)
        {
            return false;
        }

        float distanceToWell = GlobalPosition.DistanceTo(_antimatterSource.GlobalPosition);
        return distanceToWell <= AntimatterContestRadius && distanceToPlayer > 18.0f;
    }

    private void ContestAntimatterSource()
    {
        if (_antimatterSource == null || _antimatterSource.StoredAntimatter <= 0 || _antimatterContestCooldown > 0.0f)
        {
            return;
        }

        if (GlobalPosition.DistanceTo(_antimatterSource.GlobalPosition) > 2.6f)
        {
            return;
        }

        int stolen = _antimatterSource.StealAntimatter(AntimatterContestAmount);
        if (stolen <= 0)
        {
            return;
        }

        AbsorbAntimatter(stolen);
        _antimatterContestCooldown = AntimatterContestIntervalSeconds;
    }

    private void FireRangedAttack(Vector3 direction)
    {
        var projectile = new EnemyProjectile
        {
            Name = "EnemyProjectile",
            Damage = RangedDamage
        };
        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = GlobalPosition + direction * 1.2f;
        projectile.Launch(direction, this);
    }

    private void UpdateColor(float delta)
    {
        if (_hitFlashTimer > 0.0f)
        {
            _material.AlbedoColor = new Color(1.0f, 0.9f, 0.55f);
            return;
        }

        Color targetColor = _rangedAttackWindup > 0.0f
            ? new Color(0.55f, 0.18f, 1.0f)
            : new Color(0.72f, 0.08f, 0.16f);
        if (_antimatterReserve > 0)
        {
            targetColor = targetColor.Lerp(new Color(0.25f, 0.9f, 1.0f), Mathf.Clamp(_antimatterReserve / 24.0f, 0.0f, 1.0f));
        }
        _material.AlbedoColor = _material.AlbedoColor.Lerp(targetColor, 10.0f * delta);
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

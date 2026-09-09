using Godot;
using MagicCarpetRemastered.Scripts.Audio;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Spells;
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
    [Export] public int ManaDroppedOnDeath { get; set; } = 15;
    [Export] public float ManaContestRadius { get; set; } = 14.0f;
    [Export] public float ManaContestIntervalSeconds { get; set; } = 1.0f;
    [Export] public int ManaContestAmount { get; set; } = 4;

    private CarpetFlightController? _target;
    private ManaWell? _manaWell;
    private StandardMaterial3D _material = null!;
    private float _contactCooldown;
    private float _rangedAttackCooldown = 1.2f;
    private float _rangedAttackWindup;
    private float _manaContestCooldown;
    private float _hitFlashTimer;
    private int _manaReserve;

    public override void _Ready()
    {
        AddToGroup("simple_monster");
        AddCollisionAndVisuals();
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;
        _contactCooldown = Mathf.Max(0.0f, _contactCooldown - deltaF);
        _rangedAttackCooldown = Mathf.Max(0.0f, _rangedAttackCooldown - deltaF);
        _hitFlashTimer = Mathf.Max(0.0f, _hitFlashTimer - deltaF);
        _manaContestCooldown = Mathf.Max(0.0f, _manaContestCooldown - deltaF);
        UpdateColor(deltaF);
        _target ??= GetTree().GetFirstNodeInGroup("player") as CarpetFlightController;
        _manaWell ??= GetTree().GetFirstNodeInGroup("mana_well") as ManaWell;

        if (_target == null)
        {
            return;
        }

        Vector3 toTarget = _target.GlobalPosition - GlobalPosition;
        float distance = toTarget.Length();
        Vector3 direction = distance > 0.1f ? toTarget.Normalized() : Vector3.Zero;

        if (ShouldContestManaWell(distance))
        {
            ContestManaWell();
            if (_manaWell != null)
            {
                Vector3 toWell = _manaWell.GlobalPosition - GlobalPosition;
                float wellDistance = toWell.Length();
                direction = wellDistance > 0.1f ? toWell.Normalized() : Vector3.Zero;
                Velocity = direction * (MoveSpeed * 1.15f);
                MoveAndSlide();
                UpdateRangedAttack(deltaF, wellDistance, direction);
                return;
            }
        }

        Velocity = direction * MoveSpeed;
        MoveAndSlide();

        UpdateRangedAttack(deltaF, distance, direction);

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
        GameAudio.Instance?.PlayHit();
        if (Health <= 0)
        {
            DropMana();
            SpawnDeathBurst();
            QueueFree();
        }
    }

    public void AbsorbMana(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        _manaReserve += amount;
        Health = Mathf.Min(Health + Mathf.Max(1, amount / 3), 50);
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

    private void SpawnDeathBurst()
    {
        var burst = new SpellImpactBurst
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

    private bool ShouldContestManaWell(float distanceToPlayer)
    {
        if (_manaWell == null || _manaWell.StoredMana <= 0)
        {
            return false;
        }

        float distanceToWell = GlobalPosition.DistanceTo(_manaWell.GlobalPosition);
        return distanceToWell <= ManaContestRadius && distanceToPlayer > 18.0f;
    }

    private void ContestManaWell()
    {
        if (_manaWell == null || _manaWell.StoredMana <= 0 || _manaContestCooldown > 0.0f)
        {
            return;
        }

        if (GlobalPosition.DistanceTo(_manaWell.GlobalPosition) > 2.6f)
        {
            return;
        }

        int stolen = _manaWell.StealMana(ManaContestAmount);
        if (stolen <= 0)
        {
            return;
        }

        AbsorbMana(stolen);
        _manaContestCooldown = ManaContestIntervalSeconds;
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
        if (_manaReserve > 0)
        {
            targetColor = targetColor.Lerp(new Color(0.25f, 0.9f, 1.0f), Mathf.Clamp(_manaReserve / 24.0f, 0.0f, 1.0f));
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

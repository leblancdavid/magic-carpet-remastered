using Godot;
using MagicCarpetRemastered.Scripts.Audio;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Spells;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Enemies;

public partial class EnemyEcosystemEnemy : CharacterBody3D
{
    public enum EnemyRole
    {
        FlyingSwarm,
        GroundBeast,
        RangedCaster,
        CastleAttacker,
        ManaThief,
        EnemyWizard
    }

    [Export] public EnemyRole Role { get; set; } = EnemyRole.FlyingSwarm;
    [Export] public float MoveSpeed { get; set; } = 8.0f;
    [Export] public int Health { get; set; } = 24;
    [Export] public int ContactDamage { get; set; } = 6;
    [Export] public int RangedDamage { get; set; } = 8;
    [Export] public int ManaReserve { get; set; } = 0;
    [Export] public float AwarenessRadius { get; set; } = 48.0f;
    [Export] public float AttackRadius { get; set; } = 2.0f;
    [Export] public float RangedMinDistance { get; set; } = 6.0f;
    [Export] public float RangedMaxDistance { get; set; } = 24.0f;
    [Export] public float AttackCooldownSeconds { get; set; } = 1.1f;
    [Export] public float RangedAttackCooldownSeconds { get; set; } = 2.1f;
    [Export] public float RangedWindupSeconds { get; set; } = 0.35f;
    [Export] public float GroundClearance { get; set; } = 1.25f;
    [Export] public float OrbitalRadius { get; set; } = 8.0f;
    [Export] public float ManaStealRadius { get; set; } = 2.7f;
    [Export] public int ManaStealAmount { get; set; } = 6;
    [Export] public int RetreatManaThreshold { get; set; } = 18;
    [Export] public float RetreatDistance { get; set; } = 14.0f;
    [Export] public float PlayerContactCooldownSeconds { get; set; } = 0.8f;

    public string RoleText => Role switch
    {
        EnemyRole.FlyingSwarm => "Swarm",
        EnemyRole.GroundBeast => "Beast",
        EnemyRole.RangedCaster => "Caster",
        EnemyRole.CastleAttacker => "Sieger",
        EnemyRole.ManaThief => "Thief",
        EnemyRole.EnemyWizard => "Wizard",
        _ => "Enemy"
    };

    private CarpetFlightController? _player;
    private ManaWell? _manaWell;
    private CastleKeep? _playerCastle;
    private CastleKeep? _enemyCastle;
    private HeightmapArena? _arena;
    private StandardMaterial3D _material = null!;
    private float _attackCooldown;
    private float _rangedCooldown = 0.75f;
    private float _rangedWindup;
    private float _hitFlashTimer;
    private float _playerContactCooldown;
    private float _orbitPhase;
    private Vector3 _wanderDirection = Vector3.Forward;

    public override void _Ready()
    {
        AddToGroup("ecosystem_enemy");
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        AddCollisionAndVisuals();
        ConfigureForRole();
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;
        _attackCooldown = Mathf.Max(0.0f, _attackCooldown - deltaF);
        _rangedCooldown = Mathf.Max(0.0f, _rangedCooldown - deltaF);
        _rangedWindup = Mathf.Max(0.0f, _rangedWindup - deltaF);
        _hitFlashTimer = Mathf.Max(0.0f, _hitFlashTimer - deltaF);
        _playerContactCooldown = Mathf.Max(0.0f, _playerContactCooldown - deltaF);

        _player ??= GetTree().GetFirstNodeInGroup("player") as CarpetFlightController;
        _manaWell ??= GetTree().GetFirstNodeInGroup("mana_well") as ManaWell;
        _playerCastle ??= GetTree().GetFirstNodeInGroup("player_castle") as CastleKeep;
        _enemyCastle ??= GetTree().GetFirstNodeInGroup("enemy_castle") as CastleKeep;
        _arena ??= GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;

        if (Health <= 0)
        {
            QueueFree();
            return;
        }

        Vector3 move = GetDesiredMove(deltaF);
        if (move.LengthSquared() > 0.0001f)
        {
            Velocity = move.Normalized() * GetMoveSpeed();
        }
        else
        {
            Velocity = Vector3.Zero;
        }

        MoveAndSlide();
        ClampToGroundIfNeeded();
        UpdateVisualColor(deltaF);
        HandleAttacks(deltaF);
    }

    public void ApplyDamage(int damage)
    {
        if (damage <= 0 || Health <= 0)
        {
            return;
        }

        Health = Mathf.Max(0, Health - damage);
        _hitFlashTimer = 0.14f;
        GameAudio.Instance?.PlayHit();

        if (Health <= 0)
        {
            SpawnDeathBurst();
            DropMana();
            QueueFree();
        }
    }

    public void AbsorbMana(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        ManaReserve += amount;
        Health = Mathf.Min(Health + Mathf.Max(1, amount / 3), GetBaseHealth() + 20);
    }

    private void ConfigureForRole()
    {
        switch (Role)
        {
            case EnemyRole.FlyingSwarm:
                MoveSpeed = 11.0f;
                Health = 16;
                ContactDamage = 5;
                AttackRadius = 1.6f;
                OrbitalRadius = 7.0f;
                break;
            case EnemyRole.GroundBeast:
                MoveSpeed = 6.0f;
                Health = 42;
                ContactDamage = 10;
                AttackRadius = 2.2f;
                GroundClearance = 0.8f;
                break;
            case EnemyRole.RangedCaster:
                MoveSpeed = 7.5f;
                Health = 28;
                ContactDamage = 4;
                RangedDamage = 10;
                RangedMinDistance = 2.5f;
                RangedMaxDistance = 40.0f;
                RangedAttackCooldownSeconds = 1.4f;
                RangedWindupSeconds = 0.15f;
                break;
            case EnemyRole.CastleAttacker:
                MoveSpeed = 7.0f;
                Health = 34;
                ContactDamage = 11;
                AttackRadius = 2.8f;
                break;
            case EnemyRole.ManaThief:
                MoveSpeed = 9.0f;
                Health = 22;
                ContactDamage = 3;
                ManaStealAmount = 8;
                ManaStealRadius = 3.0f;
                break;
            case EnemyRole.EnemyWizard:
                MoveSpeed = 8.5f;
                Health = 36;
                ContactDamage = 6;
                RangedDamage = 12;
                RangedMinDistance = 4.0f;
                RangedMaxDistance = 42.0f;
                RangedAttackCooldownSeconds = 1.8f;
                RangedWindupSeconds = 0.2f;
                OrbitalRadius = 10.0f;
                ManaReserve = 12;
                break;
        }
    }

    private float GetMoveSpeed()
    {
        return Role == EnemyRole.FlyingSwarm && _player != null && GlobalPosition.DistanceTo(_player.GlobalPosition) < 12.0f
            ? MoveSpeed * 1.2f
            : MoveSpeed;
    }

    private Vector3 GetDesiredMove(float delta)
    {
        if (ShouldRetreat())
        {
            return GetRetreatVector();
        }

        Vector3 targetPosition = GetPriorityTargetPosition();
        if (targetPosition == Vector3.Zero)
        {
            _orbitPhase += delta * 0.7f;
            _wanderDirection = _wanderDirection.Slerp(new Vector3(Mathf.Sin(_orbitPhase), 0.0f, Mathf.Cos(_orbitPhase)), 0.04f).Normalized();
            return _wanderDirection * MoveSpeed * 0.25f;
        }

        Vector3 toTarget = targetPosition - GlobalPosition;
        float distance = HorizontalDistanceTo(targetPosition);
        Vector3 direction = HorizontalDirectionTo(targetPosition);

        return Role switch
        {
            EnemyRole.FlyingSwarm => MoveFlyingSwarm(direction, distance),
            EnemyRole.GroundBeast => MoveGroundBeast(direction, distance),
            EnemyRole.RangedCaster => MoveRangedCaster(direction, distance),
            EnemyRole.CastleAttacker => MoveCastleAttacker(direction, distance),
            EnemyRole.ManaThief => MoveManaThief(direction, distance),
            EnemyRole.EnemyWizard => MoveEnemyWizard(direction, distance),
            _ => direction * MoveSpeed
        };
    }

    private Vector3 MoveFlyingSwarm(Vector3 direction, float distance)
    {
        Vector3 orbit = Vector3.Zero;
        if (_player != null)
        {
            Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
            Vector3 lateral = new Vector3(-toPlayer.Z, 0.0f, toPlayer.X).Normalized();
            orbit = lateral * Mathf.Sin(Time.GetTicksMsec() * 0.003f + GlobalPosition.X * 0.07f) * 4.0f;

            if (distance < AttackRadius * 1.6f)
            {
                return (-direction * MoveSpeed * 1.1f) + orbit;
            }
        }

        if (distance > OrbitalRadius)
        {
            return (direction * MoveSpeed) + orbit;
        }

        return (direction * (MoveSpeed * 0.65f)) + orbit;
    }

    private Vector3 MoveGroundBeast(Vector3 direction, float distance)
    {
        if (_arena != null)
        {
            float groundY = _arena.HeightAt(GlobalPosition.X, GlobalPosition.Z) + GroundClearance;
            GlobalPosition = new Vector3(GlobalPosition.X, Mathf.Lerp(GlobalPosition.Y, groundY, 0.16f), GlobalPosition.Z);
        }

        return distance > 2.0f ? new Vector3(direction.X, 0.0f, direction.Z) * MoveSpeed : Vector3.Zero;
    }

    private Vector3 MoveRangedCaster(Vector3 direction, float distance)
    {
        if (_player == null)
        {
            return direction * MoveSpeed;
        }

        if (distance < RangedMinDistance)
        {
            return (-direction * MoveSpeed) + Vector3.Up * 0.5f;
        }

        if (distance > RangedMaxDistance)
        {
            return (direction * MoveSpeed) + Vector3.Up * 0.2f;
        }

        Vector3 strafe = new Vector3(-direction.Z, 0.0f, direction.X) * Mathf.Sin(_orbitPhase += 0.03f) * 4.0f;
        return strafe + Vector3.Up * 0.3f;
    }

    private Vector3 MoveCastleAttacker(Vector3 direction, float distance)
    {
        if (_playerCastle == null)
        {
            return direction * MoveSpeed;
        }

        float ramDistance = AttackRadius + 4.0f;
        if (distance <= ramDistance)
        {
            return Vector3.Zero;
        }

        return direction * MoveSpeed;
    }

    private Vector3 MoveManaThief(Vector3 direction, float distance)
    {
        if (_manaWell != null && _manaWell.StoredMana > 0)
        {
            if (distance > ManaStealRadius)
            {
                return direction * (MoveSpeed * 1.15f);
            }

            if (ManaReserve >= 12)
            {
                return GetRetreatVector();
            }

            return Vector3.Zero;
        }

        return direction * (MoveSpeed * 0.7f);
    }

    private Vector3 MoveEnemyWizard(Vector3 direction, float distance)
    {
        if (_manaWell != null && ManaReserve < RetreatManaThreshold)
        {
            return distance > 8.0f ? direction * (MoveSpeed * 1.05f) + Vector3.Up * 0.4f : Vector3.Up * 0.35f;
        }

        if (_playerCastle != null && _playerCastle.IsDestroyed == false)
        {
            if (distance > RangedMaxDistance)
            {
                return direction * MoveSpeed;
            }

            if (distance < RangedMinDistance)
            {
                return -direction * (MoveSpeed * 0.75f);
            }

            Vector3 strafe = new Vector3(-direction.Z, 0.0f, direction.X) * Mathf.Sin(_orbitPhase += 0.035f) * 5.0f;
            return strafe + Vector3.Up * 0.45f;
        }

        return direction * MoveSpeed;
    }

    private bool ShouldRetreat()
    {
        if (Role == EnemyRole.ManaThief && ManaReserve >= 12)
        {
            return true;
        }

        return Role == EnemyRole.EnemyWizard && ManaReserve >= RetreatManaThreshold && _enemyCastle != null;
    }

    private Vector3 GetRetreatVector()
    {
        if (_enemyCastle != null)
        {
            Vector3 toCastle = _enemyCastle.GlobalPosition - GlobalPosition;
            if (toCastle.Length() > RetreatDistance)
            {
                return toCastle;
            }

            return toCastle * 0.35f;
        }

        return -GlobalBasis.Z * MoveSpeed;
    }

    private Vector3 GetPriorityTargetPosition()
    {
        switch (Role)
        {
            case EnemyRole.FlyingSwarm:
                if (_player != null)
                {
                    return _player.GlobalPosition;
                }
                break;
            case EnemyRole.GroundBeast:
                if (_playerCastle != null)
                {
                    return _playerCastle.GlobalPosition;
                }
                break;
            case EnemyRole.RangedCaster:
                if (_player != null)
                {
                    return _player.GlobalPosition;
                }
                break;
            case EnemyRole.CastleAttacker:
                if (_playerCastle != null && !_playerCastle.IsDestroyed)
                {
                    return _playerCastle.GlobalPosition;
                }
                break;
            case EnemyRole.ManaThief:
                if (_manaWell != null && _manaWell.StoredMana > 0)
                {
                    return _manaWell.GlobalPosition;
                }
                if (_playerCastle != null)
                {
                    return _playerCastle.GlobalPosition;
                }
                break;
            case EnemyRole.EnemyWizard:
                if (_manaWell != null && ManaReserve < 18 && _manaWell.StoredMana > 0)
                {
                    return _manaWell.GlobalPosition;
                }
                if (_playerCastle != null && !_playerCastle.IsDestroyed)
                {
                    return _playerCastle.GlobalPosition;
                }
                if (_player != null)
                {
                    return _player.GlobalPosition;
                }
                break;
        }

        return Vector3.Zero;
    }

    private void HandleAttacks(float delta)
    {
        if (_rangedWindup > 0.0f)
        {
            _rangedWindup -= delta;
            if (_rangedWindup <= 0.0f)
            {
                FireProjectile();
            }
        }

        if (Role == EnemyRole.ManaThief)
        {
            TryStealMana();
        }

        if (Role == EnemyRole.EnemyWizard)
        {
            TryDepositManaAtCastle();
        }

        if (Role == EnemyRole.CastleAttacker)
        {
            TryRamCastle();
        }

        TryDamagePlayerOnContact();

        if (ShouldStartRangedAttack())
        {
            _rangedWindup = RangedWindupSeconds;
            _rangedCooldown = RangedAttackCooldownSeconds;
        }
    }

    private bool ShouldStartRangedAttack()
    {
        if (_rangedCooldown > 0.0f)
        {
            return false;
        }

        if (Role is not (EnemyRole.RangedCaster or EnemyRole.EnemyWizard))
        {
            return false;
        }

        if (_player == null)
        {
            return false;
        }

        float distance = HorizontalDistanceTo(_player.GlobalPosition);
        return distance >= RangedMinDistance && distance <= RangedMaxDistance;
    }

    private void FireProjectile()
    {
        if (_player == null)
        {
            return;
        }

        Vector3 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
        if (direction == Vector3.Zero)
        {
            direction = -GlobalBasis.Z;
        }

        var projectile = new EnemyProjectile
        {
            Name = $"{RoleText}Projectile",
            Damage = RangedDamage
        };
        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = GlobalPosition + direction * 1.3f;
        projectile.Launch(direction, this);

        var launchBurst = new SpellImpactBurst
        {
            Name = $"{RoleText}LaunchBurst",
            BurstColor = new Color(0.6f, 0.25f, 1.0f, 0.8f),
            EmissionColor = new Color(0.45f, 0.1f, 1.0f),
            DurationSeconds = 0.2f,
            StartScale = 0.3f,
            EndScale = 1.2f
        };
        GetTree().CurrentScene.AddChild(launchBurst);
        launchBurst.GlobalPosition = projectile.GlobalPosition;
    }

    private void TryStealMana()
    {
        if (_manaWell == null || _manaWell.StoredMana <= 0)
        {
            return;
        }

        if (HorizontalDistanceTo(_manaWell.GlobalPosition) > ManaStealRadius || _attackCooldown > 0.0f)
        {
            return;
        }

        int stolen = _manaWell.StealMana(ManaStealAmount);
        if (stolen <= 0)
        {
            return;
        }

        AbsorbMana(stolen);
        _attackCooldown = AttackCooldownSeconds;
    }

    private void TryRamCastle()
    {
        if (_playerCastle == null || _playerCastle.IsDestroyed || _attackCooldown > 0.0f)
        {
            return;
        }

        float ramDistance = AttackRadius + 4.0f;
        if (HorizontalDistanceTo(_playerCastle.GlobalPosition) > ramDistance)
        {
            return;
        }

        _playerCastle.ApplyDamage(ContactDamage);
        _attackCooldown = AttackCooldownSeconds;
    }

    private void TryDepositManaAtCastle()
    {
        if (_enemyCastle == null || ManaReserve <= 0 || _attackCooldown > 0.0f)
        {
            return;
        }

        if (HorizontalDistanceTo(_enemyCastle.GlobalPosition) > RetreatDistance)
        {
            return;
        }

        int deposited = _enemyCastle.DepositMana(ManaReserve);
        if (deposited <= 0)
        {
            return;
        }

        ManaReserve -= deposited;
        _attackCooldown = AttackCooldownSeconds;
    }

    private void TryDamagePlayerOnContact()
    {
        if (_player == null || _player.Health <= 0 || _playerContactCooldown > 0.0f)
        {
            return;
        }

        float contactDistance = Role == EnemyRole.FlyingSwarm ? AttackRadius * 1.5f : AttackRadius;
        if (HorizontalDistanceTo(_player.GlobalPosition) > contactDistance)
        {
            return;
        }

        _player.ApplyDamage(ContactDamage);
        _playerContactCooldown = PlayerContactCooldownSeconds;
    }

    private void ClampToGroundIfNeeded()
    {
        if (Role is EnemyRole.FlyingSwarm or EnemyRole.RangedCaster or EnemyRole.EnemyWizard)
        {
            return;
        }

        if (_arena == null)
        {
            return;
        }

        float groundY = _arena.HeightAt(GlobalPosition.X, GlobalPosition.Z) + GroundClearance;
        GlobalPosition = new Vector3(GlobalPosition.X, Mathf.Max(GlobalPosition.Y, groundY), GlobalPosition.Z);
    }

    private void UpdateVisualColor(float delta)
    {
        if (_hitFlashTimer > 0.0f)
        {
            _material.AlbedoColor = new Color(1.0f, 0.9f, 0.55f);
            return;
        }

        Color targetColor = Role switch
        {
            EnemyRole.FlyingSwarm => new Color(0.9f, 0.42f, 0.1f),
            EnemyRole.GroundBeast => new Color(0.32f, 0.8f, 0.2f),
            EnemyRole.RangedCaster => new Color(0.25f, 0.52f, 1.0f),
            EnemyRole.CastleAttacker => new Color(0.75f, 0.12f, 0.14f),
            EnemyRole.ManaThief => new Color(0.95f, 0.82f, 0.2f),
            EnemyRole.EnemyWizard => new Color(0.56f, 0.18f, 1.0f),
            _ => new Color(0.72f, 0.08f, 0.16f)
        };

        if (ManaReserve > 0)
        {
            targetColor = targetColor.Lerp(new Color(0.22f, 0.9f, 1.0f), Mathf.Clamp(ManaReserve / 36.0f, 0.0f, 1.0f) * 0.45f);
        }

        _material.AlbedoColor = _material.AlbedoColor.Lerp(targetColor, 10.0f * delta);
    }

    private void AddCollisionAndVisuals()
    {
        AddChild(new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 0.85f }
        });

        var mesh = new MeshInstance3D();
        mesh.Mesh = Role == EnemyRole.GroundBeast
            ? new CylinderMesh { TopRadius = 0.65f, BottomRadius = 1.0f, Height = 1.6f }
            : new SphereMesh { Radius = 0.82f, Height = 1.6f };
        _material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.72f, 0.08f, 0.16f),
            Roughness = 0.9f
        };
        mesh.MaterialOverride = _material;
        if (Role == EnemyRole.GroundBeast)
        {
            mesh.Position = new Vector3(0.0f, 0.1f, 0.0f);
        }
        AddChild(mesh);
    }

    private void SpawnDeathBurst()
    {
        var burst = new SpellImpactBurst
        {
            Name = $"{RoleText}DeathBurst",
            BurstColor = new Color(1.0f, 0.45f, 0.1f, 0.9f),
            EmissionColor = new Color(1.0f, 0.2f, 0.05f),
            DurationSeconds = 0.45f,
            StartScale = 0.8f,
            EndScale = 3.5f
        };
        GetTree().CurrentScene.AddChild(burst);
        burst.GlobalPosition = GlobalPosition;
    }

    private void DropMana()
    {
        if (ManaReserve <= 0)
        {
            return;
        }

        int drops = Mathf.Clamp(ManaReserve / 6, 1, 4);
        int amountPerDrop = Mathf.Max(1, ManaReserve / drops);
        for (int i = 0; i < drops; i++)
        {
            float angle = Mathf.Tau * i / drops;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0.25f, Mathf.Sin(angle)) * 1.2f;
            var pickup = new ManaPickup
            {
                Name = "EnemyManaDrop",
                ManaAmount = amountPerDrop
            };
            GetTree().CurrentScene.AddChild(pickup);
            pickup.GlobalPosition = GlobalPosition + offset;
        }

        ManaReserve = 0;
    }

    private int GetBaseHealth()
    {
        return Role switch
        {
            EnemyRole.FlyingSwarm => 16,
            EnemyRole.GroundBeast => 42,
            EnemyRole.RangedCaster => 28,
            EnemyRole.CastleAttacker => 34,
            EnemyRole.ManaThief => 22,
            EnemyRole.EnemyWizard => 36,
            _ => 24
        };
    }

    private float HorizontalDistanceTo(Vector3 target)
    {
        Vector2 delta = new Vector2(GlobalPosition.X - target.X, GlobalPosition.Z - target.Z);
        return delta.Length();
    }

    private Vector3 HorizontalDirectionTo(Vector3 target)
    {
        Vector2 delta = new Vector2(target.X - GlobalPosition.X, target.Z - GlobalPosition.Z);
        if (delta.LengthSquared() <= 0.0001f)
        {
            return Vector3.Zero;
        }

        Vector2 normal = delta.Normalized();
        return new Vector3(normal.X, 0.0f, normal.Y);
    }
}

using System;
using Godot;
using MagicCarpetRemastered.Scripts.Audio;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Enemies;

public partial class EnemyEcosystemEnemy : CharacterBody3D
{
    public enum EnemyRole
    {
        FlyingSwarm,
        GroundBeast,
        RangedCaster,
        BaseAttacker,
        AntimatterThief,
        RivalShip
    }

    [Export] public EnemyRole Role { get; set; } = EnemyRole.FlyingSwarm;
    [Export] public float MoveSpeed { get; set; } = 8.0f;
    [Export] public int Health { get; set; } = 24;
    [Export] public int ContactDamage { get; set; } = 6;
    [Export] public int RangedDamage { get; set; } = 8;
    [Export] public int AntimatterReserve { get; set; } = 0;
    [Export] public int NeutralAntimatterValue { get; set; } = 12;
    [Export] public float AwarenessRadius { get; set; } = 25.0f;
    [Export] public float RetaliationRadius { get; set; } = 65;
    [Export] public float DisengagementRadius { get; set; } = 85;
    [Export] public float HabitatLeashRadius { get; set; } = 120;
    [Export] public float PatrolRadius { get; set; } = 12;
    [Export] public float AttackRadius { get; set; } = 2.0f;
    [Export] public float RangedMinDistance { get; set; } = 6.0f;
    [Export] public float RangedMaxDistance { get; set; } = 24.0f;
    [Export] public float AttackCooldownSeconds { get; set; } = 1.1f;
    [Export] public float RangedAttackCooldownSeconds { get; set; } = 2.1f;
    [Export] public float RangedWindupSeconds { get; set; } = 0.35f;
    [Export] public float GroundClearance { get; set; } = 1.25f;
    [Export] public float OrbitalRadius { get; set; } = 8.0f;
    [Export] public float AntimatterStealRadius { get; set; } = 2.7f;
    [Export] public int AntimatterStealAmount { get; set; } = 6;
    [Export] public int RetreatAntimatterThreshold { get; set; } = 18;
    [Export] public float RetreatDistance { get; set; } = 14.0f;
    [Export] public float PlayerContactCooldownSeconds { get; set; } = 0.8f;
    [Export] public float AntimatterTargetAcquireInterval { get; set; } = 0.6f;
    [Export] public float MaxOrbSeekRadius { get; set; } = 42.0f;
    [Export] public float PilotHoverClearance { get; set; } = 5.5f;
    [Export] public float PilotMaxFlightClearance { get; set; } = 12.0f;
    [Export] public float PilotVerticalSpeed { get; set; } = 6.0f;

    public event Action? Defeated;
    public bool IsTerritorial => Role != EnemyRole.RivalShip;
    public bool IsRaider => RaidTarget != null;
    public FactionBase? RaidTarget { get; set; }
    public float RaidLifetimeSeconds { get; set; } = 90;
    public Vector3? HabitatPosition { get; set; }
    public string HabitatId { get; set; } = string.Empty;
    public NeutralTerritory? Territory { get; private set; }
    public string BehaviorText => IsRaider ? $"Raid → {(IsInstanceValid(RaidTarget) && RaidTarget!.IsPlayerBase ? "your base" : "rival base")}"
        : Territory?.State.ToString() ?? "Rival pilot";
    private Node3D? CombatTarget => IsTerritorial ? Territory?.Target : _player;

    public string RoleText => Role switch
    {
        EnemyRole.FlyingSwarm => "Swarm",
        EnemyRole.GroundBeast => "Beast",
        EnemyRole.RangedCaster => "Caster",
        EnemyRole.BaseAttacker => IsRaider ? "Raider" : "Sieger",
        EnemyRole.AntimatterThief => "Scavenger",
        EnemyRole.RivalShip => "Pilot",
        _ => "Enemy"
    };

    private ShipFlightController? _player;
    private FactionBase? _playerBase;
    private FactionBase? _enemyBase;
    private HeightmapArena? _arena;
    private AntimatterPickup? _targetOrb;
    private float _targetOrbCooldown;
    private StandardMaterial3D _material = null!;
    private float _attackCooldown;
    private float _rangedCooldown = 0.75f;
    private float _rangedWindup;
    private Node3D? _rangedTarget;
    private float _raidRemaining;
    private float _hitFlashTimer;
    private float _playerContactCooldown;
    private float _orbitPhase;
    private Vector3 _wanderDirection = Vector3.Forward;
    private Vector3 LocalUp => _arena?.UpAt(GlobalPosition) ?? Vector3.Up;

    public override void _Ready()
    {
        AddToGroup("ecosystem_enemy");
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        AddCollisionAndVisuals();
        ConfigureForRole();
        if (Role is not (EnemyRole.AntimatterThief or EnemyRole.RivalShip))
        {
            AntimatterReserve += Mathf.Max(0, NeutralAntimatterValue);
        }
        _arena?.ClampFlightBody(this, GroundClearance);
        if (IsTerritorial)
        {
            if (_arena?.IsSpherical == false)
            {
                // Substitute flat defaults for unchanged globe range values.
                if (AwarenessRadius == 25) AwarenessRadius = 12;
                if (RetaliationRadius == 65) RetaliationRadius = 30;
                if (DisengagementRadius == 85) DisengagementRadius = 40;
                if (HabitatLeashRadius == 120) HabitatLeashRadius = 50;
                if (PatrolRadius == 12) PatrolRadius = 6;
            }
            else if (_arena != null && _arena.PlanetRadius <= 500)
            {
                if (AwarenessRadius == 25) AwarenessRadius = Mathf.Clamp(20 + _arena.PlanetRadius * 0.05f, 30, 45);
                if (RetaliationRadius == 65) RetaliationRadius = Mathf.Clamp(_arena.PlanetRadius * 0.2f, 65, 100);
                if (DisengagementRadius == 85) DisengagementRadius = Mathf.Clamp(_arena.PlanetRadius * 0.25f, 85, 125);
                if (HabitatLeashRadius == 120) HabitatLeashRadius = Mathf.Clamp(_arena.PlanetRadius * 0.3f, 100, 150);
                if (PatrolRadius == 12) PatrolRadius = Mathf.Clamp(_arena.PlanetRadius * 0.04f, 6, 12);
            }
            Territory = new NeutralTerritory(this, _arena, HabitatPosition ?? GlobalPosition);
        }
        _raidRemaining = RaidLifetimeSeconds;
        if (IsRaider)
        {
            AddToGroup("neutral_raider");
            AddChild(new Label3D { Text = RaidTarget!.IsPlayerBase ? "RAIDER → YOUR BASE" : "RAIDER → RIVAL BASE",
                Position = Vector3.Up * 2.5f, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                FontSize = 32, PixelSize = 0.012f, Modulate = Colors.OrangeRed, VisibilityRangeEnd = 140 });
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GetTree().Paused || !double.IsFinite(delta) || delta < 0) return;
        float deltaF = (float)delta;
        _attackCooldown = Mathf.Max(0.0f, _attackCooldown - deltaF);
        _rangedCooldown = Mathf.Max(0.0f, _rangedCooldown - deltaF);
        _hitFlashTimer = Mathf.Max(0.0f, _hitFlashTimer - deltaF);
        _playerContactCooldown = Mathf.Max(0.0f, _playerContactCooldown - deltaF);

        _player ??= GetTree().GetFirstNodeInGroup("player") as ShipFlightController;
        _playerBase ??= GetTree().GetFirstNodeInGroup("player_base") as FactionBase;
        _enemyBase ??= GetTree().GetFirstNodeInGroup("enemy_base") as FactionBase;
        _arena ??= GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        _targetOrbCooldown = Mathf.Max(0.0f, _targetOrbCooldown - deltaF);

        if (Health <= 0)
        {
            QueueFree();
            return;
        }

        if (IsRaider)
        {
            _raidRemaining -= deltaF;
            if (!IsInstanceValid(RaidTarget) || RaidTarget!.IsDestroyed || _raidRemaining <= 0)
            { ApplyDamage(Health); return; }
        }
        Territory?.Update(deltaF);
        Vector3 move = GetDesiredMove(deltaF);
        if (move.LengthSquared() > 0.0001f)
        {
            Velocity = move.Normalized() * (IsTerritorial ? Mathf.Min(move.Length(), GetMoveSpeed()) : GetMoveSpeed());
        }
        else
        {
            Velocity = Vector3.Zero;
        }

        if (Role is EnemyRole.RivalShip or EnemyRole.FlyingSwarm or EnemyRole.RangedCaster)
        {
            UpdateFlyingVelocity(move);
        }

        if (_arena != null) _arena.MoveBody(this); else MoveAndSlide();
        if (Role == EnemyRole.RivalShip)
        {
            ClampPilotAltitude();
        }
        ClampToGroundIfNeeded();
        _arena?.ClampFlightBody(this, GroundClearance);
        UpdateVisualColor(deltaF);
        HandleAttacks(deltaF);
    }

    public void ApplyDamage(int damage, Node3D? attacker = null)
    {
        if (damage <= 0 || Health <= 0)
        {
            return;
        }

        Health = Mathf.Max(0, Health - damage);
        if (Health > 0) Territory?.OnDamage(attacker);
        AlertHabitat(attacker);
        _hitFlashTimer = 0.14f;
        GameAudio.Instance?.PlayHit();

        if (Health <= 0)
        {
            SpawnDeathBurst();
            DropAntimatter();
            Defeated?.Invoke();
            QueueFree();
        }
    }

    public void AbsorbAntimatter(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        AntimatterReserve += amount;
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
            case EnemyRole.BaseAttacker:
                MoveSpeed = 7.0f;
                Health = 34;
                ContactDamage = 11;
                AttackRadius = 2.8f;
                break;
            case EnemyRole.AntimatterThief:
                MoveSpeed = 9.0f;
                Health = 22;
                ContactDamage = 3;
                AntimatterStealAmount = 8;
                AntimatterStealRadius = 3.0f;
                break;
            case EnemyRole.RivalShip:
                MoveSpeed = 8.5f;
                Health = 36;
                ContactDamage = 6;
                RangedDamage = 12;
                RangedMinDistance = 4.0f;
                RangedMaxDistance = 42.0f;
                RangedAttackCooldownSeconds = 1.8f;
                RangedWindupSeconds = 0.2f;
                OrbitalRadius = 10.0f;
                break;
        }
    }

    private float GetMoveSpeed()
    {
        return Role == EnemyRole.FlyingSwarm && NeutralTerritory.IsLivingPilot(CombatTarget) && GlobalPosition.DistanceTo(CombatTarget!.GlobalPosition) < 12.0f
            ? MoveSpeed * 1.2f
            : MoveSpeed;
    }

    private Vector3 GetDesiredMove(float delta)
    {
        if (IsTerritorial)
        {
            if (Territory!.State == NeutralTerritory.BehaviorState.Pursuit && NeutralTerritory.IsLivingPilot(CombatTarget))
            {
                _targetOrb = null;
                Vector3 chaseDirection = HorizontalDirectionTo(CombatTarget!.GlobalPosition);
                float chaseDistance = HorizontalDistanceTo(CombatTarget.GlobalPosition);
                return Role switch
                {
                    EnemyRole.FlyingSwarm => MoveFlyingSwarm(chaseDirection, chaseDistance),
                    EnemyRole.RangedCaster => MoveRangedCaster(chaseDirection, chaseDistance),
                    _ => MoveGroundBeast(chaseDirection, chaseDistance)
                };
            }
            if (IsRaider && IsInstanceValid(RaidTarget) && !RaidTarget!.IsDestroyed)
                return MoveBaseAttacker(HorizontalDirectionTo(RaidTarget.GlobalPosition), HorizontalDistanceTo(RaidTarget.GlobalPosition));
            if (Role == EnemyRole.AntimatterThief && Territory.State == NeutralTerritory.BehaviorState.Patrol && AntimatterReserve < 12)
            {
                AcquireTargetOrb(preferPlayerOwned: true);
                if (IsInstanceValid(_targetOrb) && IsEligibleOrb(_targetOrb!))
                    return HorizontalDistanceTo(_targetOrb.GlobalPosition) > AntimatterStealRadius ? HorizontalDirectionTo(_targetOrb.GlobalPosition) * MoveSpeed : Vector3.Zero;
            }
            return Territory.IdleMove();
        }
        if (ShouldRetreat())
        {
            return GetRetreatVector();
        }

        Vector3 targetPosition = GetPriorityTargetPosition();
        if (targetPosition == Vector3.Zero)
        {
            _orbitPhase += delta * 0.7f;
            _wanderDirection = _wanderDirection.Slerp(new Vector3(Mathf.Sin(_orbitPhase), 0.0f, Mathf.Cos(_orbitPhase)), 0.04f).Normalized();
            if (_arena?.IsSpherical == true) _wanderDirection = _arena.Tangent(-GlobalBasis.Z, GlobalPosition).Normalized();
            return _wanderDirection * MoveSpeed * 0.25f;
        }

        float distance = HorizontalDistanceTo(targetPosition);
        Vector3 direction = HorizontalDirectionTo(targetPosition);
        return MoveRivalShip(direction, distance);
    }

    private Vector3 MoveFlyingSwarm(Vector3 direction, float distance)
    {
        Vector3 orbit = Vector3.Zero;
        if (NeutralTerritory.IsLivingPilot(CombatTarget))
        {
            Vector3 toPlayer = CombatTarget!.GlobalPosition - GlobalPosition;
            Vector3 lateral = toPlayer.Cross(LocalUp).Normalized();
            orbit = lateral * Mathf.Sin(Time.GetTicksMsec() * 0.003f + GlobalPosition.X * 0.07f) * 4.0f;

            if (distance < AttackRadius * 1.6f && _playerContactCooldown > 0)
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
            GlobalPosition = GlobalPosition.Lerp(_arena.OnSurface(GlobalPosition, GroundClearance), 0.16f);
        }

        return distance > 2.0f ? direction * MoveSpeed : Vector3.Zero;
    }

    private Vector3 MoveRangedCaster(Vector3 direction, float distance)
    {
        if (!NeutralTerritory.IsLivingPilot(CombatTarget))
        {
            return direction * MoveSpeed;
        }

        if (distance < RangedMinDistance)
        {
            return (-direction * MoveSpeed) + LocalUp * 0.5f;
        }

        if (distance > RangedMaxDistance)
        {
            return (direction * MoveSpeed) + LocalUp * 0.2f;
        }

        Vector3 strafe = direction.Cross(LocalUp) * Mathf.Sin(_orbitPhase += 0.03f) * 4.0f;
        return strafe + LocalUp * 0.3f;
    }

    private Vector3 MoveBaseAttacker(Vector3 direction, float distance)
    {
        if (!IsInstanceValid(RaidTarget))
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

    private Vector3 MoveRivalShip(Vector3 direction, float distance)
    {
        if (_targetOrb != null && IsInstanceValid(_targetOrb) && AntimatterReserve < RetreatAntimatterThreshold)
        {
            if (distance > AntimatterStealRadius * 0.85f)
            {
                return direction * (MoveSpeed * 1.05f);
            }

            return Vector3.Zero;
        }

        _targetOrb = null;

        if (_playerBase != null && _playerBase.IsDestroyed == false)
        {
            if (distance > RangedMaxDistance)
            {
                return direction * MoveSpeed;
            }

            if (distance < RangedMinDistance)
            {
                return -direction * (MoveSpeed * 0.75f);
            }

            Vector3 strafe = direction.Cross(LocalUp) * Mathf.Sin(_orbitPhase += 0.035f) * 5.0f;
            return strafe;
        }

        return direction * MoveSpeed;
    }

    private void AlertHabitat(Node3D? attacker)
    {
        // Direct damage only: no recursive alert propagation or cross-habitat chase.
        // Lethal hits still let the surviving pack retaliate.
        if (Territory == null || string.IsNullOrEmpty(HabitatId) || !NeutralTerritory.IsLivingPilot(attacker)
            || Territory.Distance(GlobalPosition, attacker!.GlobalPosition) > RetaliationRadius
            || Territory.Distance(Territory.Home, attacker.GlobalPosition) > HabitatLeashRadius) return;
        using var members = (Godot.Collections.Array)GetTree().GetNodesInGroup("ecosystem_enemy");
        foreach (Variant node in members)
            if (node.As<Node>() is EnemyEcosystemEnemy member && member != this && member.Health > 0
                && !member.IsQueuedForDeletion() && member.HabitatId == HabitatId)
                member.Territory?.OnDamage(attacker);
    }

    private void UpdateFlyingVelocity(Vector3 desiredMove)
    {
        if (_arena?.IsSpherical == true)
        {
            Vector3 tangent = _arena.Tangent(desiredMove, GlobalPosition);
            tangent = tangent.LengthSquared() > 0.0001f ? tangent.Normalized() * (IsTerritorial ? Mathf.Min(desiredMove.Length(), GetMoveSpeed()) : MoveSpeed) : Vector3.Zero;
            float limit = Mathf.Min(PilotMaxFlightClearance, _arena.MaxFlightClearance);
            float clearance = Mathf.Min(PilotHoverClearance, limit);
            if (_targetOrb == null && !ShouldRetreat() && NeutralTerritory.IsLivingPilot(CombatTarget))
                clearance = Mathf.Clamp(_arena.Clearance(CombatTarget!.GlobalPosition), Role == EnemyRole.FlyingSwarm ? GroundClearance : clearance, limit);
            float radialSpeed = Mathf.Clamp((clearance - _arena.Clearance(GlobalPosition)) * 2, -PilotVerticalSpeed, PilotVerticalSpeed);
            Velocity = tangent + LocalUp * radialSpeed;
            _arena.Orient(this, tangent.LengthSquared() > 0.0001f ? tangent : null);
            return;
        }
        // Horizontal steering and altitude correction must not be normalized together:
        // a tiny upward hover bias otherwise becomes full-speed perpetual ascent.
        Vector3 horizontal = new(desiredMove.X, 0, desiredMove.Z);
        horizontal = horizontal.LengthSquared() > 0.0001f ? horizontal.Normalized() * (IsTerritorial ? Mathf.Min(horizontal.Length(), GetMoveSpeed()) : MoveSpeed) : Vector3.Zero;
        float surface = PilotSurfaceHeight();
        float maxClearance = Mathf.Min(PilotMaxFlightClearance, _arena?.MaxFlightClearance ?? 12.0f);
        float targetHeight = surface + Mathf.Min(PilotHoverClearance, maxClearance);
        if (_targetOrb == null && !ShouldRetreat() && NeutralTerritory.IsLivingPilot(CombatTarget))
        {
            targetHeight = Mathf.Clamp(CombatTarget!.GlobalPosition.Y, Role == EnemyRole.FlyingSwarm ? surface + GroundClearance : targetHeight, surface + maxClearance);
        }
        float vertical = Mathf.Clamp((targetHeight - GlobalPosition.Y) * 2.0f, -PilotVerticalSpeed, PilotVerticalSpeed);
        Velocity = new Vector3(horizontal.X, vertical, horizontal.Z);
        if (horizontal.LengthSquared() > 0.0001f)
        {
            Rotation = new Vector3(0, Mathf.Atan2(-horizontal.X, -horizontal.Z), 0);
        }
    }

    private float PilotSurfaceHeight() => _arena == null ? 0
        : Mathf.Max(_arena.HeightAt(GlobalPosition.X, GlobalPosition.Z), _arena.WaterHeight);

    private void ClampPilotAltitude()
    {
        if (_arena?.IsSpherical == true) { _arena.ClampFlightBody(this, GroundClearance, PilotMaxFlightClearance); return; }
        float surface = PilotSurfaceHeight();
        GlobalPosition = new Vector3(GlobalPosition.X,
            Mathf.Clamp(GlobalPosition.Y, surface + GroundClearance, surface + Mathf.Min(PilotMaxFlightClearance, _arena?.MaxFlightClearance ?? 12.0f)), GlobalPosition.Z);
    }

    private bool ShouldRetreat()
    {
        return Role == EnemyRole.RivalShip && AntimatterReserve >= RetreatAntimatterThreshold
            && _enemyBase != null && !_enemyBase.IsDestroyed;
    }

    private Vector3 GetRetreatVector()
    {
        if (_enemyBase != null)
        {
            Vector3 toBase = _enemyBase.GlobalPosition - GlobalPosition;
            if (_arena?.IsSpherical == true) toBase = _arena.DirectionTo(GlobalPosition, _enemyBase.GlobalPosition) * HorizontalDistanceTo(_enemyBase.GlobalPosition);
            if (toBase.Length() > RetreatDistance)
            {
                return toBase;
            }

            return toBase * 0.35f;
        }

        return -GlobalBasis.Z * MoveSpeed;
    }

    private Vector3 GetPriorityTargetPosition()
    {
        // Only the designated legacy rival retains its old strategic loop until
        // phase 3. Ordinary roles are steered by their local territory above.
        if (Role == EnemyRole.RivalShip)
        {
            if (AntimatterReserve < RetreatAntimatterThreshold)
            {
                AcquireTargetOrb(preferPlayerOwned: false);
                if (_targetOrb != null && IsInstanceValid(_targetOrb))
                {
                    return _targetOrb.GlobalPosition;
                }
            }
            if (_playerBase != null && !_playerBase.IsDestroyed) return _playerBase.GlobalPosition;
            if (_player != null) return _player.GlobalPosition;
        }

        return Vector3.Zero;
    }

    private void HandleAttacks(float delta)
    {
        if (_rangedWindup > 0.0f)
        {
            if (!CanShoot(_rangedTarget) || (IsTerritorial && _rangedTarget != CombatTarget))
            { _rangedWindup = 0; _rangedTarget = null; }
            else
            {
                _rangedWindup -= delta;
                if (_rangedWindup <= 0.0f) FireProjectile();
            }
        }

        if (Role is EnemyRole.AntimatterThief or EnemyRole.RivalShip)
        {
            TryAbsorbLooseAntimatter();
            if (Role == EnemyRole.RivalShip) TryDepositAntimatterAtBase();
        }

        if (IsRaider && !NeutralTerritory.IsLivingPilot(CombatTarget))
        {
            TryRamBase();
        }

        TryDamagePlayerOnContact();

        if (ShouldStartRangedAttack())
        {
            _rangedTarget = CombatTarget;
            _rangedWindup = RangedWindupSeconds;
            _rangedCooldown = RangedAttackCooldownSeconds;
        }
    }

    private bool ShouldStartRangedAttack()
    {
        if (_rangedCooldown > 0.0f || _rangedWindup > 0)
        {
            return false;
        }

        if (Role is not (EnemyRole.RangedCaster or EnemyRole.RivalShip))
        {
            return false;
        }

        return CanShoot(CombatTarget);
    }

    private bool CanShoot(Node3D? target)
    {
        if (!NeutralTerritory.IsLivingPilot(target)) return false;
        float distance = HorizontalDistanceTo(target!.GlobalPosition);
        if (distance < RangedMinDistance || distance > RangedMaxDistance) return false;
        if (target is CollisionObject3D collider)
        {
            using var ray = PhysicsRayQueryParameters3D.Create(GlobalPosition, target.GlobalPosition);
            ray.Exclude = new Godot.Collections.Array<Rid> { GetRid(), collider.GetRid() };
            using var hit = GetWorld3D().DirectSpaceState.IntersectRay(ray);
            if (hit.Count > 0) return false;
        }
        return true;
    }

    private void FireProjectile()
    {
        if (!CanShoot(_rangedTarget) || (IsTerritorial && _rangedTarget != CombatTarget))
        {
            return;
        }

        Vector3 direction = (_rangedTarget!.GlobalPosition - GlobalPosition).Normalized();
        if (direction == Vector3.Zero)
        {
            direction = -GlobalBasis.Z;
        }

        var projectile = new EnemyProjectile
        {
            Name = $"{RoleText}Projectile",
            Damage = RangedDamage
        };
        GetParent().AddChild(projectile);
        projectile.GlobalPosition = GlobalPosition + direction * 1.3f;
        projectile.Launch(direction, this);

        var launchBurst = new EquipmentImpactBurst
        {
            Name = $"{RoleText}LaunchBurst",
            BurstColor = new Color(0.6f, 0.25f, 1.0f, 0.8f),
            EmissionColor = new Color(0.45f, 0.1f, 1.0f),
            DurationSeconds = 0.2f,
            StartScale = 0.3f,
            EndScale = 1.2f
        };
        GetParent().AddChild(launchBurst);
        launchBurst.GlobalPosition = projectile.GlobalPosition;
    }

    private void TryAbsorbLooseAntimatter()
    {
        if (IsTerritorial && (Territory?.State != NeutralTerritory.BehaviorState.Patrol || AntimatterReserve >= 12)) return;
        if (ShouldRetreat() || _targetOrb == null || !IsInstanceValid(_targetOrb) || !_targetOrb.IsAvailable)
        {
            return;
        }

        if (HorizontalDistanceTo(_targetOrb.GlobalPosition) > AntimatterStealRadius || _attackCooldown > 0.0f)
        {
            return;
        }

        AntimatterPickup orb = _targetOrb;
        bool eligible = IsEligibleOrb(orb);
        if (!eligible)
        {
            _targetOrb = null;
            return;
        }

        AbsorbAntimatter(orb.TakeAntimatter(orb.AntimatterAmount));
        _targetOrb = null;
        _attackCooldown = AttackCooldownSeconds;
    }

    private void AcquireTargetOrb(bool preferPlayerOwned)
    {
        if (_targetOrbCooldown > 0.0f)
        {
            return;
        }

        _targetOrbCooldown = AntimatterTargetAcquireInterval;

        if (_targetOrb != null && IsInstanceValid(_targetOrb))
        {
            bool stillValid = IsEligibleOrb(_targetOrb);
            if (stillValid && _targetOrb.IsAvailable)
            {
                return;
            }

            _targetOrb = null;
        }

        AntimatterPickup? bestPlayer = null;
        AntimatterPickup? bestNeutral = null;
        float bestPlayerDistance = float.MaxValue;
        float bestNeutralDistance = float.MaxValue;

        foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter"))
        {
            if (node is not AntimatterPickup pickup || !IsEligibleOrb(pickup))
            {
                continue;
            }

            float distance = _arena?.IsSpherical == true ? _arena.SurfaceDistance(GlobalPosition, pickup.GlobalPosition) : GlobalPosition.DistanceTo(pickup.GlobalPosition);
            if (distance > MaxOrbSeekRadius)
            {
                continue;
            }

            if (pickup.Ownership != AntimatterOwnership.Neutral && distance < bestPlayerDistance)
            {
                bestPlayerDistance = distance;
                bestPlayer = pickup;
            }

            if (pickup.Ownership == AntimatterOwnership.Neutral && distance < bestNeutralDistance)
            {
                bestNeutralDistance = distance;
                bestNeutral = pickup;
            }
        }

        if (preferPlayerOwned)
        {
            _targetOrb = bestPlayer ?? bestNeutral ?? null;
        }
        else
        {
            _targetOrb = bestNeutral ?? null;
        }
    }

    private void TryRamBase()
    {
        if (!IsRaider || !IsInstanceValid(RaidTarget) || RaidTarget!.IsDestroyed || _attackCooldown > 0.0f)
        {
            return;
        }

        float ramDistance = AttackRadius + 4.0f;
        if (HorizontalDistanceTo(RaidTarget.GlobalPosition) > ramDistance)
        {
            return;
        }

        RaidTarget.ApplyDamage(ContactDamage);
        _attackCooldown = AttackCooldownSeconds;
    }

    private void TryDepositAntimatterAtBase()
    {
        if (_enemyBase == null || AntimatterReserve <= 0 || _attackCooldown > 0.0f)
        {
            return;
        }

        if (HorizontalDistanceTo(_enemyBase.GlobalPosition) > RetreatDistance)
        {
            return;
        }

        int deposited = _enemyBase.DepositAntimatter(AntimatterReserve);
        if (deposited <= 0)
        {
            return;
        }

        AntimatterReserve -= deposited;
        _attackCooldown = AttackCooldownSeconds;
    }

    private void TryDamagePlayerOnContact()
    {
        Node3D? target = CombatTarget;
        if (!NeutralTerritory.IsLivingPilot(target) || _playerContactCooldown > 0.0f)
        {
            return;
        }

        float contactDistance = Role == EnemyRole.FlyingSwarm ? AttackRadius * 1.5f : AttackRadius;
        if (HorizontalDistanceTo(target!.GlobalPosition) > contactDistance
            || Mathf.Abs((_arena?.Elevation(target.GlobalPosition) ?? target.GlobalPosition.Y)
                - (_arena?.Elevation(GlobalPosition) ?? GlobalPosition.Y)) > contactDistance + 0.8f)
        {
            return;
        }

        if (target is ShipFlightController ship) ship.ApplyDamage(ContactDamage, this);
        else if (target is EnemyEcosystemEnemy pilot) pilot.ApplyDamage(ContactDamage, this);
        _playerContactCooldown = PlayerContactCooldownSeconds;
    }

    private void ClampToGroundIfNeeded()
    {
        if (Role is EnemyRole.FlyingSwarm or EnemyRole.RangedCaster or EnemyRole.RivalShip)
        {
            return;
        }

        if (_arena == null)
        {
            return;
        }

        if (_arena.IsSpherical) { GlobalPosition = _arena.OnSurface(GlobalPosition, GroundClearance); _arena.Orient(this); return; }
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
            EnemyRole.BaseAttacker => new Color(0.75f, 0.12f, 0.14f),
            EnemyRole.AntimatterThief => new Color(0.95f, 0.82f, 0.2f),
            EnemyRole.RivalShip => new Color(0.56f, 0.18f, 1.0f),
            _ => new Color(0.72f, 0.08f, 0.16f)
        };

        if (AntimatterReserve > 0)
        {
            targetColor = targetColor.Lerp(new Color(0.22f, 0.9f, 1.0f), Mathf.Clamp(AntimatterReserve / 36.0f, 0.0f, 1.0f) * 0.45f);
        }

        _material.AlbedoColor = _material.AlbedoColor.Lerp(targetColor, 10.0f * delta);
    }

    private void AddCollisionAndVisuals()
    {
        AddChild(new CollisionShape3D
        {
            Shape = Role == EnemyRole.RivalShip
                ? new CapsuleShape3D { Radius = 0.65f, Height = 3.0f }
                : new SphereShape3D { Radius = 0.85f },
            Position = Role == EnemyRole.RivalShip ? new Vector3(0, 0.7f, 0) : Vector3.Zero
        });

        _material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.72f, 0.08f, 0.16f),
            Roughness = 0.9f
        };
        if (Role == EnemyRole.RivalShip)
        {
            BuildPilotVisuals();
            return;
        }

        var mesh = new MeshInstance3D();
        mesh.Mesh = Role == EnemyRole.GroundBeast
            ? new CylinderMesh { TopRadius = 0.65f, BottomRadius = 1.0f, Height = 1.6f }
            : new SphereMesh { Radius = 0.82f, Height = 1.6f };
        mesh.MaterialOverride = _material;
        if (Role == EnemyRole.GroundBeast)
        {
            mesh.Position = new Vector3(0.0f, 0.1f, 0.0f);
        }
        AddChild(mesh);
    }

    private void BuildPilotVisuals()
    {
        _material.AlbedoColor = new Color(0.56f, 0.18f, 1.0f);
        _material.EmissionEnabled = true;
        _material.Emission = new Color(0.22f, 0.04f, 0.45f);
        AddPilotPart("Robe", new CylinderMesh { TopRadius = 0.32f, BottomRadius = 0.58f, Height = 1.45f },
            new Vector3(0, 0.3f, 0), _material);
        var face = new StandardMaterial3D { AlbedoColor = new Color(0.92f, 0.72f, 0.52f) };
        AddPilotPart("Head", new SphereMesh { Radius = 0.28f, Height = 0.56f }, new Vector3(0, 1.23f, 0), face);
        AddPilotPart("Hat", new CylinderMesh { TopRadius = 0, BottomRadius = 0.45f, Height = 0.8f },
            new Vector3(0, 1.8f, 0), _material);
        var ship = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.65f, 0.12f, 0.55f),
            EmissionEnabled = true,
            Emission = new Color(0.28f, 0.04f, 0.2f)
        };
        AddPilotPart("Ship", new BoxMesh { Size = new Vector3(2.8f, 0.16f, 1.8f) },
            new Vector3(0, -0.52f, 0), ship);
        AddChild(new Label3D
        {
            Name = "PilotLabel",
            Text = "RIVAL PILOT",
            Position = new Vector3(0, 2.75f, 0),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 48,
            PixelSize = 0.008f,
            Modulate = new Color(0.9f, 0.7f, 1.0f)
        });
    }

    private void AddPilotPart(string name, Mesh mesh, Vector3 position, Material material)
    {
        AddChild(new MeshInstance3D { Name = name, Mesh = mesh, Position = position, MaterialOverride = material });
    }

    private void SpawnDeathBurst()
    {
        EquipmentImpactPool.Prepare(GetTree().CurrentScene).Emit(GlobalPosition,
            new Color(1, 0.45f, 0.1f, 0.9f), new Color(1, 0.2f, 0.05f), 0.45f, 0.8f, 3.5f);
    }

    private void DropAntimatter()
    {
        if (AntimatterReserve <= 0)
        {
            return;
        }

        AntimatterOwnership dropOwnership = Role == EnemyRole.RivalShip
            ? AntimatterOwnership.Enemy
            : AntimatterOwnership.Neutral;
        int amount = AntimatterReserve;
        AntimatterReserve = 0;
        AntimatterScatter.Spawn(GetParent(), GlobalPosition, amount, dropOwnership, name: "EnemyAntimatterDrop");
    }

    private int GetBaseHealth()
    {
        return Role switch
        {
            EnemyRole.FlyingSwarm => 16,
            EnemyRole.GroundBeast => 42,
            EnemyRole.RangedCaster => 28,
            EnemyRole.BaseAttacker => 34,
            EnemyRole.AntimatterThief => 22,
            EnemyRole.RivalShip => 36,
            _ => 24
        };
    }

    private float HorizontalDistanceTo(Vector3 target)
    {
        if (_arena?.IsSpherical == true) return _arena.SurfaceDistance(GlobalPosition, target);
        Vector2 delta = new Vector2(GlobalPosition.X - target.X, GlobalPosition.Z - target.Z);
        return delta.Length();
    }

    private bool IsEligibleOrb(AntimatterPickup pickup) => pickup.IsAvailable
        && (Role == EnemyRole.AntimatterThief || pickup.Ownership == AntimatterOwnership.Neutral)
        && (!IsTerritorial || (Territory != null
            && Territory.Distance(Territory.Home, pickup.GlobalPosition) <= Mathf.Min(MaxOrbSeekRadius, HabitatLeashRadius * 0.5f)));

    private Vector3 HorizontalDirectionTo(Vector3 target)
    {
        if (_arena?.IsSpherical == true) return _arena.DirectionTo(GlobalPosition, target);
        Vector2 delta = new Vector2(target.X - GlobalPosition.X, target.Z - GlobalPosition.Z);
        if (delta.LengthSquared() <= 0.0001f)
        {
            return Vector3.Zero;
        }

        Vector2 normal = delta.Normalized();
        return new Vector3(normal.X, 0.0f, normal.Y);
    }
}

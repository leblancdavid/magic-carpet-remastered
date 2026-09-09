using Godot;
using MagicCarpetRemastered.Scripts.Audio;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Spells;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Player;

public partial class CarpetFlightController : CharacterBody3D
{
    private enum TerrainSpellMode
    {
        Crater,
        Raise,
        Lower,
        Flatten
    }

    private enum QuickSpellSlot
    {
        Firebolt,
        ArcaneBurst,
        ManaShield,
        WindDash,
        Guardian
    }

    [Export] public float MoveSpeed { get; set; } = 24.0f;
    [Export] public float VerticalSpeed { get; set; } = 11.0f;
    [Export] public float Acceleration { get; set; } = 28.0f;
    [Export] public float Drag { get; set; } = 9.0f;
    [Export] public float BankDegrees { get; set; } = 24.0f;
    [Export] public float CameraSmoothing { get; set; } = 10.0f;
    [Export] public float BankSmoothing { get; set; } = 8.0f;
    [Export] public float MouseSensitivity { get; set; } = 0.0025f;
    [Export] public float MinimumAltitude { get; set; } = 2.0f;
    [Export] public float GroundProbeHeight { get; set; } = 24.0f;
    [Export] public float GroundProbeDepth { get; set; } = 96.0f;
    [Export] public float FireCooldownSeconds { get; set; } = 0.25f;
    [Export] public float TerrainSpellCooldownSeconds { get; set; } = 0.75f;
    [Export] public float TerrainSpellRange { get; set; } = 90.0f;
    [Export] public float TerrainSpellRadius { get; set; } = 10.0f;
    [Export] public float TerrainSpellDepth { get; set; } = 5.0f;
    [Export] public float TerrainSpellHeight { get; set; } = 4.0f;
    [Export] public float DamageInvulnerabilitySeconds { get; set; } = 1.0f;
    [Export] public float TerrainFollowStrength { get; set; } = 4.5f;
    [Export] public float TerrainFollowMaxLiftSpeed { get; set; } = 10.0f;
    [Export] public int ManaCapacity { get; set; } = 80;
    [Export] public int InitialClaimedMana { get; set; } = 80;
    [Export] public int MaxClaimedMana { get; set; } = 400;
    [Export] public float ManaRegenPerClaimedManaPerSecond { get; set; } = 0.04f;
    [Export] public float ImmediatePickupRefillFraction { get; set; } = 0.3f;
    [Export] public int ManaDepositReserve { get; set; } = 20;
    [Export] public float ManaDepositRadius { get; set; } = 6.0f;
    [Export] public int PrimarySpellManaCost { get; set; } = 5;
    [Export] public int TerrainSpellManaCost { get; set; } = 0;
    [Export] public int AreaSpellManaCost { get; set; } = 18;
    [Export] public float AreaSpellCooldownSeconds { get; set; } = 2.5f;
    [Export] public float AreaSpellRange { get; set; } = 70.0f;
    [Export] public float AreaSpellRadius { get; set; } = 8.0f;
    [Export] public int AreaSpellDamage { get; set; } = 32;
    [Export] public int ShieldSpellManaCost { get; set; } = 14;
    [Export] public float ShieldSpellCooldownSeconds { get; set; } = 6.0f;
    [Export] public float ShieldDurationSeconds { get; set; } = 4.0f;
    [Export] public float ShieldDamageReduction { get; set; } = 0.65f;
    [Export] public int MobilitySpellManaCost { get; set; } = 10;
    [Export] public float MobilitySpellCooldownSeconds { get; set; } = 3.0f;
    [Export] public float MobilityDashSpeed { get; set; } = 46.0f;
    [Export] public float MobilityDashLift { get; set; } = 5.0f;
    [Export] public int SummonSpellManaCost { get; set; } = 24;
    [Export] public float SummonSpellCooldownSeconds { get; set; } = 8.0f;
    [Export] public float SummonDistance { get; set; } = 5.0f;

    public int Mana { get; private set; } = 40;
    public int ClaimedMana { get; private set; }
    public bool CanClaimMana => ClaimedMana < MaxClaimedMana;
    public int Health { get; private set; } = 100;
    public float Speed => Velocity.Length();
    public float AltitudeAboveTerrain => _arena == null ? GlobalPosition.Y : GlobalPosition.Y - _arena.HeightAt(GlobalPosition.X, GlobalPosition.Z);
    public bool IsInvulnerable => _damageInvulnerabilityTimer > 0.0f;
    public string CameraMode => _firstPersonCamera ? "First Person" : "Chase";
    public string PrimarySpellName => GetSelectedQuickSpell()?.DisplayName ?? "Firebolt";
    public string TerrainSpellName => _terrainSpell?.DisplayName ?? "Terrain Shape";
    public string AreaSpellName => _areaSpell?.DisplayName ?? "Arcane Burst";
    public string ShieldSpellName => _shieldSpell?.DisplayName ?? "Mana Shield";
    public string MobilitySpellName => _mobilitySpell?.DisplayName ?? "Wind Dash";
    public string SummonSpellName => _summonSpell?.DisplayName ?? "Guardian";
    public bool IsShielded => _shieldTimer > 0.0f;
    public string ShieldStatusText => IsShielded ? $"Shield {_shieldTimer:0.0}s" : "Shield ready";
    public string SpellLoadoutText => _primarySpell == null || _terrainSpell == null || _areaSpell == null || _shieldSpell == null || _mobilitySpell == null || _summonSpell == null
        ? "Spells: loading"
        : $"Quick: LMB {GetSelectedQuickSpell()!.DisplayName} ({GetSelectedQuickSpell()!.ManaCost})   Tab cycle   Q {_areaSpell.DisplayName}   E {_shieldSpell.DisplayName}   F {_mobilitySpell.DisplayName}   G {_summonSpell.DisplayName}   RMB {_terrainSpell.DisplayName}";
    public string StatusMessage { get; private set; } = "Collect mana, bank excess at the well, and contest the red monsters.";
    public string TerrainDebugText => _arena == null
        ? "Terrain: unavailable"
        : $"Terrain edit: {_arena.LastEditedVertexCount} verts   Chunks: {_arena.LastRebuiltChunkCount}   Rebuild: {_arena.LastRebuildMilliseconds:0.00} ms";
    public string TerrainSpellModeText => _terrainSpellMode switch
    {
        TerrainSpellMode.Crater => "Crater",
        TerrainSpellMode.Raise => "Raise",
        TerrainSpellMode.Lower => "Lower",
        TerrainSpellMode.Flatten => "Flatten",
        _ => "Crater"
    };
    public int TerrainSpellManaCostForCurrentMode => _terrainSpell.ManaCost;
    public string TerrainSpellHelpText => "1 crater  2 raise  3 lower  4 flatten";

    private Node3D _cameraPivot = null!;
    private Camera3D _camera = null!;
    private MeshInstance3D _carpetVisual = null!;
    private StandardMaterial3D _carpetMaterial = null!;
    private HeightmapArena? _arena;
    private ManaWell? _manaWell;
    private float _yaw;
    private float _pitch = -0.2f;
    private float _fireCooldown;
    private float _areaSpellCooldown;
    private float _shieldSpellCooldown;
    private float _mobilitySpellCooldown;
    private float _summonSpellCooldown;
    private float _shieldTimer;
    private float _terrainSpellCooldown;
    private float _feedbackTimer;
    private float _damageInvulnerabilityTimer;
    private float _manaRegenAccumulator;
    private bool _firstPersonCamera;
    private TerrainSpellMode _terrainSpellMode = TerrainSpellMode.Crater;
    private QuickSpellSlot _selectedQuickSpell = QuickSpellSlot.Firebolt;
    private SpellDefinition _primarySpell = null!;
    private SpellDefinition _areaSpell = null!;
    private SpellDefinition _shieldSpell = null!;
    private SpellDefinition _mobilitySpell = null!;
    private SpellDefinition _summonSpell = null!;
    private SpellDefinition _terrainSpell = null!;

    public override void _Ready()
    {
        AddToGroup("player");
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        _manaWell = GetTree().GetFirstNodeInGroup("mana_well") as ManaWell;
        ClaimedMana = Mathf.Clamp(Mathf.Max(InitialClaimedMana, ManaCapacity), 1, MaxClaimedMana);
        ManaCapacity = ClaimedMana;
        Mana = Mathf.Clamp(Mana, 0, ManaCapacity);
        _primarySpell = new SpellDefinition("firebolt", "Firebolt", SpellKind.Projectile, PrimarySpellManaCost, FireCooldownSeconds, "Fast projectile damage spell.");
        _areaSpell = new SpellDefinition("arcane-burst", "Arcane Burst", SpellKind.Projectile, AreaSpellManaCost, AreaSpellCooldownSeconds, "Area damage blast at the aimed point.");
        _shieldSpell = new SpellDefinition("mana-shield", "Mana Shield", SpellKind.Defense, ShieldSpellManaCost, ShieldSpellCooldownSeconds, "Briefly reduces incoming damage.");
        _mobilitySpell = new SpellDefinition("wind-dash", "Wind Dash", SpellKind.Mobility, MobilitySpellManaCost, MobilitySpellCooldownSeconds, "Dash quickly in the aimed direction.");
        _summonSpell = new SpellDefinition("guardian", "Guardian", SpellKind.Summoning, SummonSpellManaCost, SummonSpellCooldownSeconds, "Summon a temporary ally that attacks nearby enemies.");
        _terrainSpell = new SpellDefinition("terrain-shape", "Terrain Shape", SpellKind.Terrain, TerrainSpellManaCost, TerrainSpellCooldownSeconds, "Shape terrain using the selected terrain mode.");

        _cameraPivot = new Node3D { Name = "CameraPivot" };
        AddChild(_cameraPivot);
        _cameraPivot.Position = new Vector3(0.0f, 1.2f, 0.0f);

        _camera = new Camera3D
        {
            Name = "Camera3D",
            Current = true,
            Position = new Vector3(0.0f, 2.0f, 8.0f),
            Fov = 75.0f
        };
        _cameraPivot.AddChild(_camera);
        _camera.LookAt(_cameraPivot.GlobalPosition, Vector3.Up);

        AddCarpetVisual();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }

        if (@event.IsActionPressed("toggle_camera"))
        {
            _firstPersonCamera = !_firstPersonCamera;
            StatusMessage = $"Camera: {CameraMode}";
            _feedbackTimer = 1.2f;
        }

        if (@event.IsActionPressed("cycle_quick_spell"))
        {
            CycleQuickSpell();
        }

        if (@event.IsActionPressed("terrain_crater"))
        {
            SetTerrainSpellMode(TerrainSpellMode.Crater);
        }

        if (@event.IsActionPressed("terrain_raise"))
        {
            SetTerrainSpellMode(TerrainSpellMode.Raise);
        }

        if (@event.IsActionPressed("terrain_lower"))
        {
            SetTerrainSpellMode(TerrainSpellMode.Lower);
        }

        if (@event.IsActionPressed("terrain_flatten"))
        {
            SetTerrainSpellMode(TerrainSpellMode.Flatten);
        }

        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _yaw -= motion.Relative.X * MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * MouseSensitivity, -1.2f, 0.6f);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;
        _fireCooldown = Mathf.Max(0.0f, _fireCooldown - deltaF);
        _areaSpellCooldown = Mathf.Max(0.0f, _areaSpellCooldown - deltaF);
        _shieldSpellCooldown = Mathf.Max(0.0f, _shieldSpellCooldown - deltaF);
        _mobilitySpellCooldown = Mathf.Max(0.0f, _mobilitySpellCooldown - deltaF);
        _summonSpellCooldown = Mathf.Max(0.0f, _summonSpellCooldown - deltaF);
        _shieldTimer = Mathf.Max(0.0f, _shieldTimer - deltaF);
        _terrainSpellCooldown = Mathf.Max(0.0f, _terrainSpellCooldown - deltaF);
        _feedbackTimer = Mathf.Max(0.0f, _feedbackTimer - deltaF);
        _damageInvulnerabilityTimer = Mathf.Max(0.0f, _damageInvulnerabilityTimer - deltaF);
        RegenerateMana(deltaF);

        Rotation = new Vector3(0.0f, _yaw, 0.0f);
        _cameraPivot.Rotation = new Vector3(_pitch, 0.0f, 0.0f);
        UpdateCamera(deltaF);

        Vector3 localInput = Vector3.Zero;
        localInput.X = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        localInput.Z = Input.GetActionStrength("move_back") - Input.GetActionStrength("move_forward");
        localInput.Y = Input.GetActionStrength("ascend") - Input.GetActionStrength("descend");

        Vector3 horizontalInput = new(localInput.X, 0.0f, localInput.Z);
        if (horizontalInput.LengthSquared() > 1.0f)
        {
            horizontalInput = horizontalInput.Normalized();
        }

        Vector3 horizontal = (GlobalBasis * horizontalInput) * MoveSpeed;
        Vector3 desiredVelocity = new(horizontal.X, localInput.Y * VerticalSpeed, horizontal.Z);
        ApplyTerrainFollow(ref desiredVelocity);
        bool isAccelerating = horizontalInput.LengthSquared() > 0.001f || Mathf.Abs(localInput.Y) > 0.001f;
        Velocity = Velocity.MoveToward(desiredVelocity, (isAccelerating ? Acceleration : Drag) * deltaF);
        GameAudio.Instance?.SetFlightIntensity(Mathf.Clamp(Velocity.Length() / MoveSpeed, 0.0f, 1.0f));
        UpdateCarpetBank(deltaF);
        MoveAndSlide();

        ClampAboveGround();
        if (Input.IsActionPressed("cast_primary"))
        {
            CastSelectedQuickSpell();
        }

        if (Input.IsActionPressed("cast_area") && _areaSpellCooldown <= 0.0f)
        {
            _areaSpellCooldown = _areaSpell.CooldownSeconds;
            CastAreaSpell();
        }

        if (Input.IsActionPressed("cast_shield") && _shieldSpellCooldown <= 0.0f)
        {
            _shieldSpellCooldown = _shieldSpell.CooldownSeconds;
            CastShieldSpell();
        }

        if (Input.IsActionPressed("cast_mobility") && _mobilitySpellCooldown <= 0.0f)
        {
            _mobilitySpellCooldown = _mobilitySpell.CooldownSeconds;
            CastMobilitySpell();
        }

        if (Input.IsActionPressed("cast_summon") && _summonSpellCooldown <= 0.0f)
        {
            _summonSpellCooldown = _summonSpell.CooldownSeconds;
            CastSummonSpell();
        }

        if (Input.IsActionPressed("cast_terrain") && _terrainSpellCooldown <= 0.0f)
        {
            _terrainSpellCooldown = _terrainSpell.CooldownSeconds;
            CastTerrainSpell(_terrainSpellMode);
        }
    }

    public int AddMana(int amount)
    {
        return ClaimMana(amount, ImmediatePickupRefillFraction);
    }

    public int ClaimMana(int amount, float immediateRefillFraction = 0.0f)
    {
        if (amount <= 0 || ClaimedMana >= MaxClaimedMana)
        {
            return 0;
        }

        int claimed = Mathf.Min(amount, MaxClaimedMana - ClaimedMana);
        ClaimedMana += claimed;
        ManaCapacity = ClaimedMana;

        int immediateRefill = Mathf.RoundToInt(claimed * Mathf.Clamp(immediateRefillFraction, 0.0f, 1.0f));
        if (immediateRefill > 0)
        {
            Mana = Mathf.Min(ManaCapacity, Mana + immediateRefill);
        }

        StatusMessage = immediateRefill > 0
            ? $"Claimed {claimed} mana (+{immediateRefill})"
            : $"Claimed {claimed} mana";
        _feedbackTimer = 1.0f;
        GameAudio.Instance?.PlayPickup();
        return claimed;
    }

    public int SpendMana(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        int spent = Mathf.Min(Mana, amount);
        Mana -= spent;
        return spent;
    }

    private void RegenerateMana(float delta)
    {
        if (Mana >= ManaCapacity || ClaimedMana <= 0 || ManaRegenPerClaimedManaPerSecond <= 0.0f)
        {
            return;
        }

        _manaRegenAccumulator += ClaimedMana * ManaRegenPerClaimedManaPerSecond * delta;
        int regenerated = Mathf.FloorToInt(_manaRegenAccumulator);
        if (regenerated <= 0)
        {
            return;
        }

        _manaRegenAccumulator -= regenerated;
        Mana = Mathf.Min(ManaCapacity, Mana + regenerated);
    }

    public void ApplyDamage(int amount)
    {
        if (IsInvulnerable || Health == 0)
        {
            return;
        }

        int finalAmount = IsShielded ? Mathf.Max(1, Mathf.RoundToInt(amount * (1.0f - ShieldDamageReduction))) : amount;
        Health = Mathf.Max(0, Health - finalAmount);
        StatusMessage = IsShielded ? $"Shield absorbed {amount - finalAmount}" : $"Hit for {finalAmount}";
        _feedbackTimer = 0.35f;
        _damageInvulnerabilityTimer = DamageInvulnerabilitySeconds;
        _carpetMaterial.AlbedoColor = IsShielded ? new Color(0.2f, 0.85f, 1.0f) : new Color(1.0f, 0.25f, 0.2f);
        GameAudio.Instance?.PlayHit();

        if (Health == 0)
        {
            CallDeferred(nameof(ReloadSceneDeferred));
        }
    }

    private void ReloadSceneDeferred()
    {
        GetTree().ReloadCurrentScene();
    }

    private void CycleQuickSpell()
    {
        int next = ((int)_selectedQuickSpell + 1) % 5;
        _selectedQuickSpell = (QuickSpellSlot)next;
        StatusMessage = $"Quick spell: {GetSelectedQuickSpell()?.DisplayName ?? "unknown"}";
        _feedbackTimer = 1.0f;
    }

    private SpellDefinition? GetSelectedQuickSpell()
    {
        return _selectedQuickSpell switch
        {
            QuickSpellSlot.Firebolt => _primarySpell,
            QuickSpellSlot.ArcaneBurst => _areaSpell,
            QuickSpellSlot.ManaShield => _shieldSpell,
            QuickSpellSlot.WindDash => _mobilitySpell,
            QuickSpellSlot.Guardian => _summonSpell,
            _ => _primarySpell
        };
    }

    private void CastSelectedQuickSpell()
    {
        switch (_selectedQuickSpell)
        {
            case QuickSpellSlot.Firebolt:
                if (_fireCooldown > 0.0f)
                {
                    return;
                }

                _fireCooldown = _primarySpell.CooldownSeconds;
                CastPrimarySpell();
                break;
            case QuickSpellSlot.ArcaneBurst:
                if (_areaSpellCooldown > 0.0f)
                {
                    return;
                }

                _areaSpellCooldown = _areaSpell.CooldownSeconds;
                CastAreaSpell();
                break;
            case QuickSpellSlot.ManaShield:
                if (_shieldSpellCooldown > 0.0f)
                {
                    return;
                }

                _shieldSpellCooldown = _shieldSpell.CooldownSeconds;
                CastShieldSpell();
                break;
            case QuickSpellSlot.WindDash:
                if (_mobilitySpellCooldown > 0.0f)
                {
                    return;
                }

                _mobilitySpellCooldown = _mobilitySpell.CooldownSeconds;
                CastMobilitySpell();
                break;
            case QuickSpellSlot.Guardian:
                if (_summonSpellCooldown > 0.0f)
                {
                    return;
                }

                _summonSpellCooldown = _summonSpell.CooldownSeconds;
                CastSummonSpell();
                break;
        }
    }

    private void CastPrimarySpell()
    {
        if (Mana < _primarySpell.ManaCost)
        {
            StatusMessage = "Not enough mana.";
            _feedbackTimer = 0.8f;
            return;
        }

        SpendMana(_primarySpell.ManaCost);
        StatusMessage = _primarySpell.DisplayName;
        _feedbackTimer = 0.35f;
        GameAudio.Instance?.PlaySpellCast();

        var projectile = new SpellProjectile();
        GetTree().CurrentScene.AddChild(projectile);

        Vector3 direction = -_camera.GlobalBasis.Z.Normalized();
        projectile.GlobalPosition = _camera.GlobalPosition + direction * 1.5f;
        projectile.Launch(direction, this);
    }

    private void CastAreaSpell()
    {
        if (Mana < _areaSpell.ManaCost)
        {
            StatusMessage = $"Not enough mana for {_areaSpell.DisplayName.ToLowerInvariant()}.";
            _feedbackTimer = 0.8f;
            return;
        }

        Vector3 direction = -_camera.GlobalBasis.Z.Normalized();
        Vector3 origin = _camera.GlobalPosition;
        Vector3 target = origin + direction * AreaSpellRange;
        if (!TryFindTerrainHit(origin, target, out Vector3 hitPosition))
        {
            hitPosition = target;
        }

        SpendMana(_areaSpell.ManaCost);
        int hits = DamageEnemiesInRadius(hitPosition, AreaSpellRadius, AreaSpellDamage);
        SpawnAreaSpellBurst(hitPosition);
        StatusMessage = $"{_areaSpell.DisplayName}: {hits} hit";
        _feedbackTimer = 0.7f;
        GameAudio.Instance?.PlaySpellCast();
    }

    private void CastShieldSpell()
    {
        if (Mana < _shieldSpell.ManaCost)
        {
            StatusMessage = $"Not enough mana for {_shieldSpell.DisplayName.ToLowerInvariant()}.";
            _feedbackTimer = 0.8f;
            return;
        }

        SpendMana(_shieldSpell.ManaCost);
        _shieldTimer = ShieldDurationSeconds;
        StatusMessage = $"{_shieldSpell.DisplayName} active";
        _feedbackTimer = 0.9f;
        _carpetMaterial.AlbedoColor = new Color(0.2f, 0.85f, 1.0f);
        SpawnShieldBurst();
        GameAudio.Instance?.PlaySpellCast();
    }

    private void CastMobilitySpell()
    {
        if (Mana < _mobilitySpell.ManaCost)
        {
            StatusMessage = $"Not enough mana for {_mobilitySpell.DisplayName.ToLowerInvariant()}.";
            _feedbackTimer = 0.8f;
            return;
        }

        SpendMana(_mobilitySpell.ManaCost);
        Vector3 direction = -_camera.GlobalBasis.Z.Normalized();
        Velocity = new Vector3(direction.X * MobilityDashSpeed, Mathf.Max(Velocity.Y, direction.Y * MobilityDashSpeed + MobilityDashLift), direction.Z * MobilityDashSpeed);
        StatusMessage = _mobilitySpell.DisplayName;
        _feedbackTimer = 0.6f;
        SpawnMobilityBurst();
        GameAudio.Instance?.PlaySpellCast();
    }

    private void CastSummonSpell()
    {
        if (Mana < _summonSpell.ManaCost)
        {
            StatusMessage = $"Not enough mana for {_summonSpell.DisplayName.ToLowerInvariant()}.";
            _feedbackTimer = 0.8f;
            return;
        }

        SpendMana(_summonSpell.ManaCost);
        Vector3 direction = -_camera.GlobalBasis.Z.Normalized();
        var guardian = new SummonedGuardian
        {
            Name = "SummonedGuardian"
        };
        GetTree().CurrentScene.AddChild(guardian);
        guardian.GlobalPosition = GlobalPosition + direction * SummonDistance + Vector3.Up * 1.0f;
        SpawnSummonBurst(guardian.GlobalPosition);
        StatusMessage = $"Summoned {_summonSpell.DisplayName}";
        _feedbackTimer = 0.8f;
        GameAudio.Instance?.PlaySpellCast();
    }

    private void CastTerrainSpell(TerrainSpellMode mode)
    {
        if (Mana < _terrainSpell.ManaCost)
        {
            StatusMessage = $"Not enough mana for {TerrainSpellModeText.ToLowerInvariant()}.";
            _feedbackTimer = 0.8f;
            return;
        }

        _arena ??= GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        if (_arena == null)
        {
            return;
        }

        Vector3 direction = -_camera.GlobalBasis.Z.Normalized();
        Vector3 origin = _camera.GlobalPosition;
        Vector3 target = origin + direction * TerrainSpellRange;
        if (!TryFindTerrainHit(origin, target, out Vector3 hitPosition))
        {
            StatusMessage = "Aim at terrain to shape it.";
            _feedbackTimer = 0.8f;
            return;
        }

        SpendMana(_terrainSpell.ManaCost);
        switch (mode)
        {
            case TerrainSpellMode.Crater:
                _arena.ApplyCrater(hitPosition, TerrainSpellRadius, TerrainSpellDepth);
                break;
            case TerrainSpellMode.Raise:
                _arena.RaiseTerrain(hitPosition, TerrainSpellRadius, TerrainSpellHeight);
                break;
            case TerrainSpellMode.Lower:
                _arena.LowerTerrain(hitPosition, TerrainSpellRadius, TerrainSpellHeight);
                break;
            case TerrainSpellMode.Flatten:
                _arena.FlattenTerrain(hitPosition, TerrainSpellRadius, hitPosition.Y);
                break;
        }

        SpawnTerrainSpellBurst(hitPosition);
        StatusMessage = $"Terrain {TerrainSpellModeText.ToLowerInvariant()} ({_arena.LastEditedVertexCount} verts)";
        _feedbackTimer = 0.5f;
        GameAudio.Instance?.PlaySpellCast();
    }

    private bool TryFindTerrainHit(Vector3 origin, Vector3 target, out Vector3 hitPosition)
    {
        var query = PhysicsRayQueryParameters3D.Create(origin, target);
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

        for (int i = 0; i < 8; i++)
        {
            var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
            if (result.Count == 0 || !result.ContainsKey("position") || !result.ContainsKey("collider"))
            {
                break;
            }

            hitPosition = (Vector3)result["position"];
            if (result["collider"].As<Node>() is Node collider && collider.IsInGroup("deformable_terrain"))
            {
                return true;
            }

            if (result["collider"].As<Node>() is CollisionObject3D collisionObject)
            {
                query.Exclude.Add(collisionObject.GetRid());
                continue;
            }

            break;
        }

        if (_arena != null)
        {
            Vector3 ray = target - origin;
            float rayLength = ray.Length();
            if (rayLength > 0.001f)
            {
                Vector3 stepDirection = ray / rayLength;
                const int steps = 96;
                float stepLength = rayLength / steps;
                for (int i = 0; i <= steps; i++)
                {
                    float distance = stepLength * i;
                    Vector3 point = origin + stepDirection * distance;
                    float terrainHeight = _arena.HeightAt(point.X, point.Z);
                    if (point.Y <= terrainHeight)
                    {
                        hitPosition = new Vector3(point.X, terrainHeight, point.Z);
                        return true;
                    }
                }
            }
        }

        hitPosition = default;
        return false;
    }

    private void SetTerrainSpellMode(TerrainSpellMode mode)
    {
        _terrainSpellMode = mode;
        StatusMessage = $"Terrain mode: {TerrainSpellModeText}";
        _feedbackTimer = 1.0f;
    }

    private void SpawnTerrainSpellBurst(Vector3 position)
    {
        var burst = new SpellImpactBurst
        {
            Name = "TerrainSpellBurst",
            BurstColor = new Color(0.55f, 0.9f, 1.0f),
            EmissionColor = new Color(0.25f, 0.75f, 1.0f),
            EndScale = 4.0f
        };
        GetTree().CurrentScene.AddChild(burst);
        burst.GlobalPosition = position;
    }

    private void SpawnAreaSpellBurst(Vector3 position)
    {
        var burst = new SpellImpactBurst
        {
            Name = "AreaSpellBurst",
            BurstColor = new Color(0.85f, 0.35f, 1.0f, 0.9f),
            EmissionColor = new Color(0.65f, 0.12f, 1.0f),
            DurationSeconds = 0.5f,
            StartScale = 1.2f,
            EndScale = AreaSpellRadius
        };
        GetTree().CurrentScene.AddChild(burst);
        burst.GlobalPosition = position;
    }

    private void SpawnShieldBurst()
    {
        var burst = new SpellImpactBurst
        {
            Name = "ShieldBurst",
            BurstColor = new Color(0.2f, 0.85f, 1.0f, 0.7f),
            EmissionColor = new Color(0.05f, 0.65f, 1.0f),
            DurationSeconds = 0.35f,
            StartScale = 0.9f,
            EndScale = 3.0f
        };
        GetTree().CurrentScene.AddChild(burst);
        burst.GlobalPosition = GlobalPosition;
    }

    private void SpawnMobilityBurst()
    {
        var burst = new SpellImpactBurst
        {
            Name = "MobilityBurst",
            BurstColor = new Color(0.7f, 1.0f, 0.95f, 0.75f),
            EmissionColor = new Color(0.35f, 1.0f, 0.9f),
            DurationSeconds = 0.28f,
            StartScale = 0.6f,
            EndScale = 2.6f
        };
        GetTree().CurrentScene.AddChild(burst);
        burst.GlobalPosition = GlobalPosition;
    }

    private void SpawnSummonBurst(Vector3 position)
    {
        var burst = new SpellImpactBurst
        {
            Name = "SummonBurst",
            BurstColor = new Color(0.25f, 1.0f, 0.55f, 0.85f),
            EmissionColor = new Color(0.08f, 0.8f, 0.35f),
            DurationSeconds = 0.4f,
            StartScale = 0.8f,
            EndScale = 3.2f
        };
        GetTree().CurrentScene.AddChild(burst);
        burst.GlobalPosition = position;
    }

    private int DamageEnemiesInRadius(Vector3 center, float radius, int damage)
    {
        int hits = 0;
        foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
        {
            if (node is not EnemyEcosystemEnemy enemy || enemy.Health <= 0)
            {
                continue;
            }

            if (enemy.GlobalPosition.DistanceTo(center) > radius)
            {
                continue;
            }

            enemy.ApplyDamage(damage);
            hits++;
        }

        foreach (Node node in GetTree().GetNodesInGroup("simple_monster"))
        {
            if (node is not SimpleMonster monster || monster.GlobalPosition.DistanceTo(center) > radius)
            {
                continue;
            }

            monster.ApplyDamage(damage);
            hits++;
        }

        return hits;
    }

    private void ClampAboveGround()
    {
        _arena ??= GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;

        if (_arena != null)
        {
            float terrainMinimumY = _arena.HeightAt(GlobalPosition.X, GlobalPosition.Z) + MinimumAltitude;
            if (GlobalPosition.Y < terrainMinimumY)
            {
                GlobalPosition = new Vector3(GlobalPosition.X, terrainMinimumY, GlobalPosition.Z);
                Velocity = new Vector3(Velocity.X, Mathf.Max(0.0f, Velocity.Y), Velocity.Z);
            }

            return;
        }

        Vector3 origin = GlobalPosition + Vector3.Up * GroundProbeHeight;
        Vector3 target = GlobalPosition + Vector3.Down * GroundProbeDepth;
        var query = PhysicsRayQueryParameters3D.Create(origin, target);
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (result.Count == 0 || !result.ContainsKey("position"))
        {
            return;
        }

        float minimumY = ((Vector3)result["position"]).Y + MinimumAltitude;
        if (GlobalPosition.Y >= minimumY)
        {
            return;
        }

        GlobalPosition = new Vector3(GlobalPosition.X, minimumY, GlobalPosition.Z);
        Velocity = new Vector3(Velocity.X, Mathf.Max(0.0f, Velocity.Y), Velocity.Z);
    }

    private void ApplyTerrainFollow(ref Vector3 desiredVelocity)
    {
        _arena ??= GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        if (_arena == null)
        {
            return;
        }

        float targetAltitude = _arena.HeightAt(GlobalPosition.X, GlobalPosition.Z) + MinimumAltitude;
        float altitudeError = targetAltitude - GlobalPosition.Y;
        if (altitudeError <= 0.0f)
        {
            return;
        }

        float terrainLift = Mathf.Min(altitudeError * TerrainFollowStrength, TerrainFollowMaxLiftSpeed);
        desiredVelocity.Y = Mathf.Max(desiredVelocity.Y, terrainLift);
    }

    private void RouteManaToWell()
    {
        _manaWell ??= GetTree().GetFirstNodeInGroup("mana_well") as ManaWell;
        if (_manaWell == null || Mana <= ManaDepositReserve)
        {
            return;
        }

        if (GlobalPosition.DistanceTo(_manaWell.GlobalPosition) > ManaDepositRadius)
        {
            return;
        }

        int depositAmount = Mana - ManaDepositReserve;
        int deposited = _manaWell.DepositMana(depositAmount);
        if (deposited <= 0)
        {
            return;
        }

        Mana -= deposited;
        StatusMessage = $"Banked {deposited} mana";
        _feedbackTimer = 0.6f;
    }

    private void UpdateCamera(float delta)
    {
        Vector3 targetPosition = _firstPersonCamera
            ? new Vector3(0.0f, 0.25f, -0.45f)
            : new Vector3(0.0f, 2.1f, Mathf.Lerp(7.2f, 9.4f, Mathf.Clamp(Speed / MoveSpeed, 0.0f, 1.0f)));

        float smoothingWeight = Mathf.Clamp(CameraSmoothing * delta, 0.0f, 1.0f);
        _camera.Position = _camera.Position.Lerp(targetPosition, smoothingWeight);
        _camera.Fov = Mathf.Lerp(_camera.Fov, Mathf.Lerp(74.0f, 82.0f, Mathf.Clamp(Speed / MoveSpeed, 0.0f, 1.0f)), smoothingWeight);
        if (_firstPersonCamera)
        {
            _camera.Rotation = Vector3.Zero;
        }
        else
        {
            _camera.LookAt(_cameraPivot.GlobalPosition, Vector3.Up);
        }

        if (IsInvulnerable)
        {
            float pulse = 0.5f + Mathf.Sin(Time.GetTicksMsec() * 0.02f) * 0.5f;
            _carpetMaterial.AlbedoColor = new Color(1.0f, Mathf.Lerp(0.25f, 0.8f, pulse), Mathf.Lerp(0.2f, 0.95f, pulse));
        }
        else if (IsShielded)
        {
            float pulse = 0.5f + Mathf.Sin(Time.GetTicksMsec() * 0.018f) * 0.5f;
            _carpetMaterial.AlbedoColor = new Color(Mathf.Lerp(0.08f, 0.25f, pulse), Mathf.Lerp(0.55f, 0.95f, pulse), 1.0f);
        }
        else if (_feedbackTimer <= 0.0f)
        {
            _carpetMaterial.AlbedoColor = _carpetMaterial.AlbedoColor.Lerp(new Color(0.38f, 0.16f, 0.82f), Mathf.Clamp(10.0f * delta, 0.0f, 1.0f));
        }
    }

    private void UpdateCarpetBank(float delta)
    {
        Vector3 localVelocity = GlobalBasis.Inverse() * Velocity;
        float targetRoll = Mathf.DegToRad(-Mathf.Clamp(localVelocity.X / MoveSpeed, -1.0f, 1.0f) * BankDegrees);
        float targetPitch = Mathf.DegToRad(Mathf.Clamp(localVelocity.Z / MoveSpeed, -1.0f, 1.0f) * 9.0f);
        float smoothingWeight = Mathf.Clamp(BankSmoothing * delta, 0.0f, 1.0f);
        _carpetVisual.Rotation = new Vector3(
            Mathf.LerpAngle(_carpetVisual.Rotation.X, targetPitch, smoothingWeight),
            0.0f,
            Mathf.LerpAngle(_carpetVisual.Rotation.Z, targetRoll, smoothingWeight));
    }

    private void AddCarpetVisual()
    {
        _carpetVisual = new MeshInstance3D { Name = "CarpetVisual" };
        _carpetVisual.Mesh = new BoxMesh { Size = new Vector3(2.0f, 0.12f, 3.0f) };
        _carpetVisual.Position = new Vector3(0.0f, -0.35f, 0.0f);

        _carpetMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.38f, 0.16f, 0.82f),
            Roughness = 0.65f
        };
        _carpetVisual.MaterialOverride = _carpetMaterial;
        AddChild(_carpetVisual);

        var collision = new CollisionShape3D
        {
            Name = "CollisionShape3D",
            Shape = new BoxShape3D { Size = new Vector3(1.6f, 0.6f, 2.4f) }
        };
        AddChild(collision);
    }
}

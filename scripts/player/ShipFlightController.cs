using System.Linq;
using Godot;
using MagicCarpetRemastered.Scripts.Audio;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Player;

public partial class ShipFlightController : CharacterBody3D
{
    [Export] public bool IsPlayerControlled { get; set; } = true;
    public AntimatterOwnership Ownership => IsPlayerControlled ? AntimatterOwnership.Player : AntimatterOwnership.Enemy;
    public FactionBase? HomeBase => IsInstanceValid(_homeBase) ? _homeBase : null;
    public event System.Action? Defeated;
    public Node3D? LastAttacker { get; private set; }
    public Vector3 AimOrigin => IsPlayerControlled ? _camera.GlobalPosition : GlobalPosition + LocalUp * 1.45f - _autonomousAim * 0.45f;
    public Vector3 AimDirection => IsPlayerControlled ? -_camera.GlobalBasis.Z.Normalized() : _autonomousAim;
    private Vector3 _autonomousMovement;
    private Vector3 _autonomousAim = Vector3.Forward;
    private Label3D? _pilotLabel;

    public void SetAutonomousControls(Vector3 movement, Vector3 aim)
    {
        if (IsPlayerControlled) return;
        _autonomousMovement = movement.IsFinite() ? movement : Vector3.Zero;
        if (!aim.IsFinite() || aim.LengthSquared() < 0.0001f) return;
        Vector3 tangent = _arena?.Tangent(aim, GlobalPosition) ?? new Vector3(aim.X, 0, aim.Z);
        Vector3 horizontal = tangent.Normalized();
        if (horizontal.LengthSquared() < 0.0001f) horizontal = -GlobalBasis.Z;
        float pitch = Mathf.Clamp(Mathf.Atan2(aim.Dot(LocalUp), tangent.Length()), -1.2f, 0.6f);
        _autonomousAim = (horizontal * Mathf.Cos(pitch) + LocalUp * Mathf.Sin(pitch)).Normalized();
    }

    public bool TrySelectEquipmentSlot(int slot)
    {
        if (slot < 0 || slot >= 4 || Health <= 0 || GetTree().Paused || GameplayInputBlocked) return false;
        _selectedEquipmentSlot = slot; return true;
    }

    public bool TrySelectEquipmentFromInterface(int slot)
    {
        if (!IsPlayerControlled || !TrySelectEquipmentSlot(slot)) return false;
        _waitForEquipmentRelease = true;
        return true;
    }

    public float EquipmentCooldownRemaining(string id) => id switch
    {
        "plasma-bolt" => _fireCooldown, "pulse-burst" => _areaEquipmentCooldown,
        "energy-shield" => _shieldEquipmentCooldown, "dash" => _mobilityEquipmentCooldown,
        "guardian-drone" => _summonEquipmentCooldown, "scatter-cannon" => _scatterCooldown,
        "seeker-missile" => _seekerCooldown, _ => 0
    };

    public bool TryActivateEquipmentSlot(int slot)
    {
        if (!TrySelectEquipmentSlot(slot) || (IsPlayerControlled && _waitForEquipmentRelease)
            || GetSelectedQuickEquipment() is not { } item || !CanActivateEquipment(item)
            || Energy < item.EnergyCost || EquipmentCooldownRemaining(item.Id) > 0) return false;
        int before = Energy; CastSelectedQuickEquipment(); return Energy < before;
    }

    public bool TryActivateClaim()
    {
        if (Health <= 0 || GetTree().Paused || GameplayInputBlocked || (IsPlayerControlled && _waitForEquipmentRelease)
            || _claimCooldown > 0 || Energy < _claimEquipment.EnergyCost) return false;
        CastClaimEquipment(); _claimCooldown = _claimEquipment.CooldownSeconds; return true;
    }
    private enum TerrainEquipmentMode
    {
        Crater,
        Raise,
        Lower,
        Flatten
    }

    [Export] public float MoveSpeed { get; set; } = 24.0f;
    [Export] public float Acceleration { get; set; } = 28.0f;
    [Export] public float Drag { get; set; } = 9.0f;
    [Export] public float BankDegrees { get; set; } = 24.0f;
    [Export] public float CameraSmoothing { get; set; } = 10.0f;
    [Export] public float BankSmoothing { get; set; } = 4.5f;
    private float _bankInput;
    [Export] public float MouseSensitivity { get; set; } = 0.0025f;
    [Export] public float MinimumAltitude { get; set; } = 2.0f;
    [Export] public int JumpEnergyCost { get; set; } = 2;
    [Export] public float JumpHeight { get; set; } = 4;
    [Export] public float HoverGravity { get; set; } = 18;
    [Export] public float MaximumHoverSlopeDegrees { get; set; } = 65;
    private readonly ShipHoverMotion _hover = new();
    private float _jumpBuffer;
    public bool IsHoverSupported => _hover.Supported;
    [Export] public float FireCooldownSeconds { get; set; } = 0.25f;
    [Export] public float TerrainEquipmentCooldownSeconds { get; set; } = 0.75f;
    [Export] public float TerrainEquipmentRange { get; set; } = 90.0f;
    [Export] public float TerrainEquipmentRadius { get; set; } = 10.0f;
    [Export] public float TerrainEquipmentDepth { get; set; } = 5.0f;
    [Export] public float TerrainEquipmentHeight { get; set; } = 4.0f;
    [Export] public float DamageInvulnerabilitySeconds { get; set; } = 1.0f;
    [Export] public float HealthRecoveryDelaySeconds { get; set; } = 5;
    [Export] public float HealthRecoveryPerSecond { get; set; } = 2;
    private float _combatRecoveryRemaining;
    private float _healthRecoveryAccumulator;
    private float _hitFeedbackRemaining;
    public float HitFeedbackStrength => Mathf.Clamp(_hitFeedbackRemaining / 0.28f, 0, 1);

    public void RecordCombatDamage()
    {
        _combatRecoveryRemaining = HealthRecoveryDelaySeconds;
        _healthRecoveryAccumulator = 0;
    }
    [Export] public float TerrainFollowMaxLiftSpeed { get; set; } = 10.0f;
    [Export] public int InitialEnergy { get; set; } = 40;
    [Export] public float EnergyRegenFractionPerSecond { get; set; } = 0.04f;
    [Export] public int EnergyDepositReserve { get; set; } = 20;
    [Export] public float EnergyDepositRadius { get; set; } = 6.0f;
    [Export] public int PrimaryEquipmentEnergyCost { get; set; } = 2;
    [Export] public int TerrainEquipmentEnergyCost { get; set; } = 0;
    [Export] public int AreaEquipmentEnergyCost { get; set; } = 5;
    [Export] public float AreaEquipmentCooldownSeconds { get; set; } = 2.5f;
    [Export] public float AreaEquipmentRange { get; set; } = 70.0f;
    [Export] public float AreaEquipmentRadius { get; set; } = 8.0f;
    [Export] public int AreaEquipmentDamage { get; set; } = 32;
    [Export] public int ShieldEquipmentEnergyCost { get; set; } = 14;
    [Export] public float ShieldEquipmentCooldownSeconds { get; set; } = 6.0f;
    [Export] public float ShieldDurationSeconds { get; set; } = 4.0f;
    [Export] public float ShieldDamageReduction { get; set; } = 0.65f;
    [Export] public int MobilityEquipmentEnergyCost { get; set; } = 10;
    [Export] public float MobilityEquipmentCooldownSeconds { get; set; } = 3.0f;
    [Export] public float MobilityDashSpeed { get; set; } = 46.0f;
    [Export] public float MobilityDashLift { get; set; } = 5.0f;
    [Export] public int BoosterEnergyCost { get; set; } = 4;
    [Export] public float BoosterSpeed { get; set; } = 36.0f;
    [Export] public float BoosterDurationSeconds { get; set; } = 0.65f;
    [Export] public float BoosterCooldownSeconds { get; set; } = 2.0f;
[Export] public int SummonEquipmentEnergyCost { get; set; } = 24;
    [Export] public float SummonEquipmentCooldownSeconds { get; set; } = 8.0f;
    [Export] public float SummonDistance { get; set; } = 5.0f;
    [Export] public int ClaimEquipmentEnergyCost { get; set; } = 1;
    [Export] public float ClaimEquipmentCooldownSeconds { get; set; } = 0.4f;
    [Export] public float EquipmentInstallationRadius { get; set; } = 18;
    private readonly string?[] _equipmentSlots = { "plasma-bolt", null, null, null };
    private readonly string?[] _plannedEquipmentSlots = { "plasma-bolt", null, null, null };
    private static readonly string[] EquipmentSelectionActions = { "select_equipment_1", "select_equipment_2", "select_equipment_3", "select_equipment_4" };
    private int _selectedEquipmentSlot;
    public int SelectedEquipmentSlot => _selectedEquipmentSlot;
    public System.Collections.Generic.IReadOnlyList<string?> EquipmentSlots => System.Array.AsReadOnly(_equipmentSlots);
    public System.Collections.Generic.IReadOnlyList<string?> PlannedEquipmentSlots => System.Array.AsReadOnly(_plannedEquipmentSlots);
    public bool IsEquipmentPlanned(string id) => _plannedEquipmentSlots.Contains(id);
    public bool HasPendingLoadout
    {
        get { for (int i = 0; i < 4; i++) if (IsLoadoutSlotPending(i)) return true; return false; }
    }
    public bool IsLoadoutSlotPending(int slot) => slot >= 0 && slot < 4 && (_equipmentSlots[slot] != _plannedEquipmentSlots[slot]
        || (_equipmentSlots[slot] is { } id && GetOwnedEquipmentRank(id) > GetEquipmentRank(id)));
    public bool IsEquipmentInstalled(string id) => _equipmentSlots.Contains(id);
    public bool CanInstallEquipment => Health > 0 && !GetTree().Paused && IsInstanceValid(_homeBase)
        && !_homeBase!.IsDestroyed && (_arena?.SurfaceDistance(GlobalPosition, _homeBase.GlobalPosition)
            ?? new Vector2(GlobalPosition.X - _homeBase.GlobalPosition.X, GlobalPosition.Z - _homeBase.GlobalPosition.Z).Length()) <= EquipmentInstallationRadius
        && Mathf.Abs((_arena?.Elevation(GlobalPosition) ?? GlobalPosition.Y)
            - (_arena?.Elevation(_homeBase.GlobalPosition) ?? _homeBase.GlobalPosition.Y)) <= 16;

    public int GetOwnedEquipmentRank(string id)
    {
        if (!ShipEquipmentCatalog.ItemIds.Contains(id)) return 0;
        int rank = id == "plasma-bolt" ? 1 : 0;
        if (IsInstanceValid(_homeBase))
            for (int next = 1; next <= ShipEquipmentCatalog.MaximumRank; next++)
                if (_homeBase!.Construction.IsCompleted(ShipEquipmentCatalog.ProjectId(id, next))) rank = next;
        return rank;
    }

    public bool TryInstallEquipment(string id, int slot, out string reason)
    {
        reason = !CanInstallEquipment ? $"Return to your home base ({EquipmentInstallationRadius:0} units) while alive to install."
            : slot < 0 || slot >= 4 ? "Choose one of four slots."
            : GetOwnedEquipmentRank(id) == 0 ? "Research this item first." : string.Empty;
        if (reason.Length > 0) return false;
        for (int i = 0; i < 4; i++) if (_plannedEquipmentSlots[i] == id) _plannedEquipmentSlots[i] = null;
        _plannedEquipmentSlots[slot] = id;
        TrySynchronizeLoadout();
        return true;
    }

    public bool TryRemoveEquipment(int slot)
    {
        if (!CanInstallEquipment || slot < 0 || slot >= 4) return false;
        _plannedEquipmentSlots[slot] = null;
        TrySynchronizeLoadout();
        return true;
    }

    public bool TryToggleEquipment(string id, out string reason)
    {
        reason = Health <= 0 || GetTree().Paused ? "Plan equipment while alive in live gameplay."
            : GetOwnedEquipmentRank(id) == 0 ? "Research this item first."
            : !IsEquipmentPlanned(id) && _plannedEquipmentSlots.All(item => item != null) ? "Turn off an item first; at most four can be planned." : string.Empty;
        if (reason.Length > 0) return false;
        bool remove = IsEquipmentPlanned(id);
        var enabled = _plannedEquipmentSlots.Where(item => item != null && item != id).ToHashSet();
        if (!remove) enabled.Add(id);
        var ordered = ShipEquipmentCatalog.ItemIds.Where(item => enabled.Contains(item)).ToArray();
        for (int i = 0; i < 4; i++) _plannedEquipmentSlots[i] = i < ordered.Length ? ordered[i] : null;
        TrySynchronizeLoadout();
        return true;
    }

    public bool TrySynchronizeLoadout()
    {
        if (!CanInstallEquipment || _primaryEquipment == null || !HasPendingLoadout) return false;
        // Validate the whole plan before changing any active slot.
        foreach (string? id in _plannedEquipmentSlots)
            if (id != null && (GetOwnedEquipmentRank(id) == 0 || FindEquipment(id) == null)) return false;
        string? selected = _equipmentSlots[_selectedEquipmentSlot];
        for (int i = 0; i < 4; i++)
        {
            _equipmentSlots[i] = _plannedEquipmentSlots[i];
            if (_equipmentSlots[i] is { } id) FindEquipment(id)!.InstallRank(GetOwnedEquipmentRank(id));
        }
        int selection = System.Array.IndexOf(_equipmentSlots, selected);
        _selectedEquipmentSlot = selected != null && selection >= 0 ? selection : 0;
        SelectInstalledEquipment();
        _waitForEquipmentRelease = true;
        return true;
    }

    private void SelectInstalledEquipment()
    {
        if (IsEquipmentInstalled(GetSelectedQuickEquipment()?.Id ?? "")) return;
        int first = System.Array.FindIndex(_equipmentSlots, id => id != null);
        if (first >= 0) _selectedEquipmentSlot = first;
    }

    public ExplorationBoons Boons { get; } = new();
    public int MaximumHealth => 100 + 20 * Boons.Stacks("ship-hp");
    public float EffectiveMoveSpeed => MoveSpeed * Boons.Multiplier("speed");
    public float EffectiveBoosterSpeed => BoosterSpeed * Boons.Multiplier("speed");
    public float EffectiveFireCooldown => FireCooldownSeconds / Boons.Multiplier("fire-rate");
    public float EffectiveProjectileSpeed => 42 * Boons.Multiplier("projectile-speed");
    public int EffectiveGuardianDamage => Mathf.RoundToInt((12 + 4 * Mathf.Max(0, GetEquipmentRank("guardian-drone") - 1))
        * Boons.Multiplier("energy-damage") * Boons.Multiplier("deployable-damage"));
    public int Energy { get; private set; }
    public int EnergyCapacity { get; private set; }
    public bool GameplayInputBlocked
    {
        get => _gameplayInputBlocked;
        set { _gameplayInputBlocked = value; if (value) { _waitForEquipmentRelease = true; _boosterTimer = 0; _jumpBuffer = 0; } }
    }
    public float EnergyRechargePerSecond => EnergyCapacity * EnergyRegenFractionPerSecond
        * (IsInstanceValid(_homeBase) ? _homeBase!.EnergyRechargeMultiplier : 1) * Boons.Multiplier("recharge");
    public float EffectiveClaimRange => 120;
    public int Health { get; private set; } = 100;
    public float Speed => Velocity.Length();
    public float AltitudeAboveTerrain => _arena == null ? GlobalPosition.Y : _arena.Elevation(GlobalPosition) - _arena.HeightAt(GlobalPosition);
    public bool IsInvulnerable => _damageInvulnerabilityTimer > 0.0f;
    public string CameraMode => _firstPersonCamera ? "First Person" : "Chase";
    public string PrimaryEquipmentName => IsEquipmentInstalled(GetSelectedQuickEquipment()?.Id ?? "") ? GetSelectedQuickEquipment()!.RankedName : "Empty";
    public string TerrainEquipmentName => _terrainEquipment?.DisplayName ?? "Terrain Shape";
    public string AreaEquipmentName => _areaEquipment?.RankedName ?? "Pulse Burst";
    public string ShieldEquipmentName => _shieldEquipment?.RankedName ?? "Energy Shield";
    public string MobilityEquipmentName => _mobilityEquipment?.RankedName ?? "Dash";
public string SummonEquipmentName => _summonEquipment?.RankedName ?? "Guardian Drone";
    public string ClaimEquipmentName => _claimEquipment?.DisplayName ?? "Claim";
    public bool IsShielded => _shieldTimer > 0.0f;
    public bool IsBoosting => _boosterTimer > 0;
    public float BoosterCooldownRemaining => _boosterCooldown;
    public string BoosterStatusText => IsBoosting ? $"Boost {_boosterTimer:0.0}s"
        : _boosterCooldown > 0 ? $"Boost ready in {_boosterCooldown:0.0}s"
        : Energy < BoosterEnergyCost ? $"Boost needs {BoosterEnergyCost} energy" : $"Boost ready · {BoosterEnergyCost} energy";
    public string ShieldStatusText => IsShielded ? $"Shield {_shieldTimer:0.0}s" : "Shield ready";
    public string EquipmentLoadoutText => _primaryEquipment == null || _terrainEquipment == null || _areaEquipment == null || _shieldEquipment == null || _mobilityEquipment == null || _summonEquipment == null || _claimEquipment == null
        ? "Equipment: loading"
        : $"LMB: {PrimaryEquipmentName} · 1–4 select slot · Tab cycles installed\n"
            + string.Join(" · ", _equipmentSlots.Select((id, i) => $"{i + 1}: {(id == null ? "Empty" : FindEquipment(id)!.RankedName)}"))
            + "\nRMB Claim · Shift boost · Space jump · B research/install at home";
    public string StatusMessage { get; private set; } = "Claim antimatter for your collector drone and defend your base.";
    public string ActiveFeedbackMessage => _feedbackTimer > 0.0f ? StatusMessage : string.Empty;
    public string DiscoveryFeedbackMessage => _discoveryFeedbackTimer > 0 ? _discoveryFeedback : string.Empty;
    public string EquipmentProgressionText => string.Join("\n", new[] { _primaryEquipment, _areaEquipment, _shieldEquipment, _mobilityEquipment, _summonEquipment }
        .Select(equipment => $"{equipment.DisplayName} — owned {GetOwnedEquipmentRank(equipment.Id)}/3 · installed {(IsEquipmentInstalled(equipment.Id) ? equipment.Rank : 0)}"));
    public int PlasmaBoltDamage => Mathf.RoundToInt((13 + 5 * Mathf.Max(0, GetEquipmentRank("plasma-bolt") - 1)) * Boons.Multiplier("energy-damage"));
    public int EffectiveAreaDamage => Mathf.RoundToInt(AreaEquipmentDamage * (1 + 0.25f * Mathf.Max(0, GetEquipmentRank("pulse-burst") - 1)) * Boons.Multiplier("explosive-damage"));
    public float EffectiveAreaRadius => AreaEquipmentRadius * (1 + 0.25f * Mathf.Max(0, GetEquipmentRank("pulse-burst") - 1));
    public float EffectiveShieldDuration => ShieldDurationSeconds + 2 * Mathf.Max(0, GetEquipmentRank("energy-shield") - 1);
    public float EffectiveDashSpeed => MobilityDashSpeed * (1 + 0.2f * Mathf.Max(0, GetEquipmentRank("dash") - 1)) * Boons.Multiplier("speed");
    public string TerrainDebugText => _arena == null
        ? "Terrain: unavailable"
        : $"Terrain edit: {_arena.LastEditedVertexCount} verts   Chunks: {_arena.LastRebuiltChunkCount}   Rebuild: {_arena.LastRebuildMilliseconds:0.00} ms";
    public string TerrainEquipmentModeText => _terrainEquipmentMode switch
    {
        TerrainEquipmentMode.Crater => "Crater",
        TerrainEquipmentMode.Raise => "Raise",
        TerrainEquipmentMode.Lower => "Lower",
        TerrainEquipmentMode.Flatten => "Flatten",
        _ => "Crater"
    };
    public int TerrainEquipmentEnergyCostForCurrentMode => _terrainEquipment.EnergyCost;
    public string TerrainEquipmentHelpText => "1 crater  2 raise  3 lower  4 flatten";

    private Node3D _cameraPivot = null!;
    private Camera3D _camera = null!;
    private MeshInstance3D _shipVisual = null!;
    private StandardMaterial3D _shipMaterial = null!;
    private HeightmapArena? _arena;
    private AntimatterSource? _antimatterSource;
    private float _yaw;
    private float _appliedYaw;
    private float _turnRate;
    private Vector3 LocalUp => _arena?.UpAt(GlobalPosition) ?? Vector3.Up;
    private float _pitch = -0.2f;
    private float _fireCooldown;
    private float _scatterCooldown, _seekerCooldown;
    private float _areaEquipmentCooldown;
    private float _shieldEquipmentCooldown;
    private float _mobilityEquipmentCooldown;
    private float _boosterTimer;
    private float _boosterCooldown;
    private float _summonEquipmentCooldown;
    private float _shieldTimer;
    private float _terrainEquipmentCooldown;
private float _feedbackTimer;
    private float _damageInvulnerabilityTimer;
    private float _energyRegenAccumulator;
    private float _claimCooldown;
    private bool _firstPersonCamera = true;
    private TerrainEquipmentMode _terrainEquipmentMode = TerrainEquipmentMode.Crater;
    private EquipmentDefinition _primaryEquipment = null!;
    private EquipmentDefinition _scatterEquipment = null!, _seekerEquipment = null!;
    private EquipmentDefinition _areaEquipment = null!;
    private EquipmentDefinition _shieldEquipment = null!;
    private EquipmentDefinition _mobilityEquipment = null!;
private EquipmentDefinition _summonEquipment = null!;
    private EquipmentDefinition _terrainEquipment = null!;
    private EquipmentDefinition _claimEquipment = null!;
    private bool _initialEnergySet;
    private FactionBase? _homeBase;
    private bool _waitForEquipmentRelease;
    private bool _gameplayInputBlocked;
    private uint _aliveCollisionLayer;
    private uint _aliveCollisionMask;
    private string _discoveryFeedback = string.Empty;
    private float _discoveryFeedbackTimer;

    public EquipmentDefinition? FindEquipment(string id) => id switch
    {
        "plasma-bolt" => _primaryEquipment,
        "pulse-burst" => _areaEquipment,
        "scatter-cannon" => _scatterEquipment,
        "seeker-missile" => _seekerEquipment,
        "energy-shield" => _shieldEquipment,
        "dash" => _mobilityEquipment,
        "guardian-drone" => _summonEquipment,
        _ => null
    };

    public int GetEquipmentRank(string id) => IsEquipmentInstalled(id) ? FindEquipment(id)?.Rank ?? 0 : 0;

    public bool DiscoverEquipmentUpgrade(string id)
    {
        // Legacy landmarks remain until wreck/boon conversion. Purchased ranks come only from research.
        return false;
    }

    public override void _Ready()
    {
        AddToGroup("pilots");
        AddToGroup(IsPlayerControlled ? "player" : "rival_ship");
        if (IsPlayerControlled) Input.MouseMode = Input.MouseModeEnum.Captured;
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        _arena?.Orient(this);
        _antimatterSource = GetTree().GetFirstNodeInGroup("antimatter_source") as AntimatterSource;
        RefreshBaseEnergyLimit();
        _primaryEquipment = new EquipmentDefinition("plasma-bolt", "Plasma Bolt", EquipmentKind.Projectile, PrimaryEquipmentEnergyCost, FireCooldownSeconds, "Fast projectile damage equipment.");
        _areaEquipment = new EquipmentDefinition("pulse-burst", "Pulse Burst", EquipmentKind.Projectile, AreaEquipmentEnergyCost, AreaEquipmentCooldownSeconds, "Area damage blast at the aimed point.");
        _scatterEquipment = new EquipmentDefinition("scatter-cannon", "Scatter Cannon", EquipmentKind.Projectile, 5, 0.8f, "Seven close-range pellets; wide spread and short reach.");
        _seekerEquipment = new EquipmentDefinition("seeker-missile", "Seeker Missile", EquipmentKind.Projectile, 7, 1.8f, "Shootable guided missile; lock a visible mobile enemy near the crosshair.");
        _shieldEquipment = new EquipmentDefinition("energy-shield", "Energy Shield", EquipmentKind.Defense, ShieldEquipmentEnergyCost, ShieldEquipmentCooldownSeconds, "Briefly reduces incoming damage.");
        _mobilityEquipment = new EquipmentDefinition("dash", "Dash", EquipmentKind.Mobility, MobilityEquipmentEnergyCost, MobilityEquipmentCooldownSeconds, "Dash quickly in the aimed direction.");
_summonEquipment = new EquipmentDefinition("guardian-drone", "Guardian Drone", EquipmentKind.Summoning, SummonEquipmentEnergyCost, SummonEquipmentCooldownSeconds, "Summon a temporary ally that attacks nearby enemies.");
        _terrainEquipment = new EquipmentDefinition("terrain-shape", "Terrain Shape", EquipmentKind.Terrain, TerrainEquipmentEnergyCost, TerrainEquipmentCooldownSeconds, "Shape terrain using the selected terrain mode.");
        _claimEquipment = new EquipmentDefinition("claim", "Claim", EquipmentKind.Economy, ClaimEquipmentEnergyCost, ClaimEquipmentCooldownSeconds, "Fire a penetrating terrain-following bolt to claim all neutral or rival antimatter along its path.");

        if (IsPlayerControlled)
        {
            _cameraPivot = new Node3D { Name = "CameraPivot" };
            AddChild(_cameraPivot);
            _cameraPivot.Position = new Vector3(0.0f, 1.2f, 0.0f);

            _camera = new Camera3D
            {
                Name = "Camera3D",
                Current = true,
                Position = new Vector3(0.0f, 2.0f, 8.0f),
                Fov = 75.0f,
                Far = 8000
            };
            _cameraPivot.AddChild(_camera);
            _camera.LookAt(_cameraPivot.GlobalPosition, LocalUp);
        }

        AddShipVisual();
        _aliveCollisionLayer = CollisionLayer;
        _aliveCollisionMask = CollisionMask;
        _arena?.ClampFlightBody(this, MinimumAltitude);
        ConfigureHover();
        _hover.Reset(_arena, GlobalPosition);
        if (!IsPlayerControlled)
        {
            _pilotLabel = new Label3D { Name = "PilotLabel", Text = "RIVAL PILOT", Position = Vector3.Up * 2,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, FontSize = 32, PixelSize = 0.012f,
                Modulate = new Color(1, 0.35f, 0.6f), VisibilityRangeEnd = 160 };
            AddChild(_pilotLabel);
        }
        UpdateCamera(1.0f);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsPlayerControlled || GameplayInputBlocked) return;
        if (@event is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.T })
        {
            TryActivateTeleport(); GetViewport().SetInputAsHandled(); return;
        }
        if (@event.IsActionPressed("jump") && !@event.IsEcho())
        {
            if (Health > 0 && !GetTree().Paused)
            {
                _jumpBuffer = 0.12f;
                if (TryActivateJump()) _jumpBuffer = 0;
            }
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed("activate_booster") && !@event.IsEcho())
        {
            TryActivateBooster();
            GetViewport().SetInputAsHandled();
            return;
        }
        for (int i = 0; i < EquipmentSelectionActions.Length; i++)
        {
            if (!@event.IsActionPressed(EquipmentSelectionActions[i]) || @event.IsEcho()) continue;
            if (Health > 0 && !GetTree().Paused)
            {
                _selectedEquipmentSlot = i;
                StatusMessage = $"Slot {i + 1}: {PrimaryEquipmentName}";
                _feedbackTimer = 1;
            }
            GetViewport().SetInputAsHandled();
            return;
        }
        if (@event.IsActionPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }

        if (@event.IsActionPressed("toggle_camera"))
        {
            _firstPersonCamera = !_firstPersonCamera;
            _shipVisual.Visible = !_firstPersonCamera;
            StatusMessage = $"Camera: {CameraMode}";
            _feedbackTimer = 1.2f;
        }

        if (@event.IsActionPressed("cycle_quick_equipment"))
        {
            CycleQuickEquipment();
        }

        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _yaw -= motion.Relative.X * MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * MouseSensitivity, -1.2f, 0.6f);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GetTree().Paused || !double.IsFinite(delta) || delta < 0) return;
        float deltaF = (float)delta;
        _teleportCooldown = Mathf.Max(0, _teleportCooldown - deltaF);
        _hitFeedbackRemaining = Mathf.Max(0, _hitFeedbackRemaining - deltaF);
        if (Health <= 0)
        {
            CancelTeleport();
            if (IsPlayerControlled) { _camera.HOffset = 0; _camera.VOffset = 0; }
            return;
        }
        TrySynchronizeLoadout();
        RecoverHealth(deltaF);
        _discoveryFeedbackTimer = Mathf.Max(0, _discoveryFeedbackTimer - deltaF);
        _fireCooldown = Mathf.Max(0.0f, _fireCooldown - deltaF);
        _areaEquipmentCooldown = Mathf.Max(0.0f, _areaEquipmentCooldown - deltaF);
        _scatterCooldown = Mathf.Max(0, _scatterCooldown - deltaF);
        _seekerCooldown = Mathf.Max(0, _seekerCooldown - deltaF);
        _shieldEquipmentCooldown = Mathf.Max(0.0f, _shieldEquipmentCooldown - deltaF);
        _mobilityEquipmentCooldown = Mathf.Max(0.0f, _mobilityEquipmentCooldown - deltaF);
        _boosterCooldown = Mathf.Max(0, _boosterCooldown - deltaF);
        _boosterTimer = Mathf.Max(0, _boosterTimer - deltaF);
        _summonEquipmentCooldown = Mathf.Max(0.0f, _summonEquipmentCooldown - deltaF);
        _shieldTimer = Mathf.Max(0.0f, _shieldTimer - deltaF);
_terrainEquipmentCooldown = Mathf.Max(0.0f, _terrainEquipmentCooldown - deltaF);
        _claimCooldown = Mathf.Max(0.0f, _claimCooldown - deltaF);
        _feedbackTimer = Mathf.Max(0.0f, _feedbackTimer - deltaF);
        _damageInvulnerabilityTimer = Mathf.Max(0.0f, _damageInvulnerabilityTimer - deltaF);
        RegenerateEnergy(deltaF);
        if (GameplayInputBlocked) _waitForEquipmentRelease = true;
        else if (IsPlayerControlled && _waitForEquipmentRelease && !Input.IsActionPressed("activate_primary") && !Input.IsActionPressed("activate_claim")) _waitForEquipmentRelease = false;

        Vector3 previousForward = -GlobalBasis.Z;
        if (!IsPlayerControlled)
        {
            Vector3 forward = (_arena?.Tangent(_autonomousAim, GlobalPosition) ?? new Vector3(_autonomousAim.X, 0, _autonomousAim.Z)).Normalized();
            if (_arena?.IsSpherical == true) _arena.Orient(this, forward);
            else if (forward.LengthSquared() > 0.0001f) Rotation = new Vector3(0, Mathf.Atan2(-forward.X, -forward.Z), 0);
        }
        else if (_arena?.IsSpherical == true)
        {
            _arena.Orient(this, (-GlobalBasis.Z).Rotated(LocalUp, _yaw - _appliedYaw));
            _appliedYaw = _yaw;
        }
        else Rotation = new Vector3(0.0f, _yaw, 0.0f);
        if (deltaF > 0)
            _turnRate = -Mathf.Atan2(previousForward.Cross(-GlobalBasis.Z).Dot(LocalUp),
                previousForward.Dot(-GlobalBasis.Z)) / deltaF;

        Vector3 localInput = Vector3.Zero;
        if (!GameplayInputBlocked)
        {
            if (!IsPlayerControlled) localInput = GlobalBasis.Inverse() * _autonomousMovement;
            else
            {
                localInput.X = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
                localInput.Z = Input.GetActionStrength("move_back") - Input.GetActionStrength("move_forward");
            }
        }

        Vector3 horizontalInput = new(localInput.X, 0.0f, localInput.Z);
        if (AdvanceTeleport(deltaF, horizontalInput.LengthSquared() > 0.001f)) { UpdateCamera(deltaF); return; }
        if (horizontalInput.LengthSquared() > 1.0f)
        {
            horizontalInput = horizontalInput.Normalized();
        }

        Vector3 horizontal = (GlobalBasis * horizontalInput) * EffectiveMoveSpeed;
        if (IsBoosting && !GameplayInputBlocked)
        {
            // Forward surface-relative thrust; no camera-pitch lift or diagonal speed stacking.
            horizontal = -GlobalBasis.Z * EffectiveBoosterSpeed;
        }
        ConfigureHover();
        if (_arena != null)
            Velocity = _hover.Step(_arena, GlobalPosition, Velocity, horizontal, Acceleration, Drag,
                IsBoosting && !GameplayInputBlocked, deltaF);
        else Velocity = Velocity.MoveToward(horizontal, Acceleration * deltaF);
        if (_jumpBuffer > 0)
        {
            if (TryActivateJump()) _jumpBuffer = 0;
            else _jumpBuffer = Mathf.Max(0, _jumpBuffer - deltaF);
        }
        if (IsPlayerControlled) GameAudio.Instance?.SetFlightIntensity(Mathf.Clamp(Velocity.Length() / MoveSpeed, 0.0f, 1.0f));
        UpdateShipBank(deltaF);
        if (IsPlayerControlled)
        {
            _cameraPivot.Rotation = new Vector3(_pitch, 0, 0);
            UpdateCamera(deltaF);
        }
        if (_arena != null) _arena.MoveBody(this); else MoveAndSlide();

        // The ocean has no solid collider. Treat its plane as a landing surface,
        // without teleporting onto terrain or imposing a ceiling above a drop.
        if (_arena != null && _arena.HeightAt(GlobalPosition) <= _arena.WaterHeight
            && _arena.Clearance(GlobalPosition) < 0.3f)
        {
            GlobalPosition = _arena.OnSurface(GlobalPosition, 0.3f);
            Vector3 up = LocalUp;
            Velocity -= up * Mathf.Min(0, Velocity.Dot(up));
        }

        if (IsPlayerControlled && _arena?.IsSpherical == true)
        {
            Vector3 safeCamera = _arena.OnSurface(_camera.GlobalPosition, 0.5f);
            if (_arena.Clearance(_camera.GlobalPosition) < 0.5f) _camera.GlobalPosition = safeCamera;
        }
        if (!IsPlayerControlled || GameplayInputBlocked || _waitForEquipmentRelease) return;
        if (Input.IsActionPressed("activate_primary"))
        {
            TryActivateEquipmentSlot(_selectedEquipmentSlot);
        }

        if (Input.IsActionPressed("activate_claim") && _claimCooldown <= 0.0f)
        {
            TryActivateClaim();
        }
    }

    public void RefreshBaseEnergyLimit()
    {
        var home = GetTree().GetFirstNodeInGroup(IsPlayerControlled ? "player_base" : "enemy_base") as FactionBase;
        _homeBase = home;
        int previous = EnergyCapacity;
        EnergyCapacity = home != null && !home.IsDestroyed && !home.IsQueuedForDeletion() ? home.StoredAntimatter : 0;
        if (!_initialEnergySet && home != null)
        {
            Energy = Mathf.Clamp(InitialEnergy, 0, EnergyCapacity);
            _initialEnergySet = true;
        }
        Energy = Mathf.Clamp(Energy, 0, EnergyCapacity);
        if (EnergyCapacity < previous) _energyRegenAccumulator = 0;
        if (previous != EnergyCapacity)
        {
            StatusMessage = EnergyCapacity > previous
                ? $"Energy limit +{EnergyCapacity - previous}"
                : $"Energy limit lost: {previous - EnergyCapacity}";
            _feedbackTimer = 1.5f;
            if (IsPlayerControlled && EnergyCapacity > previous) GameAudio.Instance?.PlayPickup();
        }
    }

    public int SpendEnergy(int amount)
    {
        CancelTeleport();
        if (amount <= 0)
        {
            return 0;
        }

        int spent = Mathf.Min(Energy, amount);
        Energy -= spent;
        return spent;
    }

    private void RecoverHealth(float delta)
    {
        float healingTime = Mathf.Max(0, delta - _combatRecoveryRemaining);
        _combatRecoveryRemaining = Mathf.Max(0, _combatRecoveryRemaining - delta);
        if (Health >= MaximumHealth || HealthRecoveryPerSecond <= 0)
        { _healthRecoveryAccumulator = 0; return; }
        _healthRecoveryAccumulator += healingTime * HealthRecoveryPerSecond;
        int restored = Mathf.FloorToInt(_healthRecoveryAccumulator);
        _healthRecoveryAccumulator -= restored;
        Health = Mathf.Min(MaximumHealth, Health + restored);
    }

    private void RegenerateEnergy(float delta)
    {
        if (Energy >= EnergyCapacity || EnergyCapacity <= 0 || EnergyRegenFractionPerSecond <= 0.0f)
        {
            _energyRegenAccumulator = 0;
            return;
        }

        _energyRegenAccumulator += EnergyRechargePerSecond * delta;
        int regenerated = Mathf.FloorToInt(_energyRegenAccumulator);
        if (regenerated <= 0)
        {
            return;
        }

        _energyRegenAccumulator -= regenerated;
        Energy = Mathf.Min(EnergyCapacity, Energy + regenerated);
    }

    public void ApplyDamage(int amount, Node3D? attacker = null)
    {
        if (amount <= 0 || IsInvulnerable || Health == 0)
        {
            return;
        }

        int finalAmount = IsShielded ? Mathf.Max(1, Mathf.RoundToInt(amount * (1.0f - ShieldDamageReduction))) : amount;
        CancelTeleport();
        Health = Mathf.Max(0, Health - finalAmount);
        RecordCombatDamage();
        _hitFeedbackRemaining = IsPlayerControlled ? 0.28f : 0;
        LastAttacker = IsInstanceValid(attacker) ? attacker : null;
        StatusMessage = IsShielded ? $"Shield absorbed {amount - finalAmount}" : $"Hit for {finalAmount}";
        _feedbackTimer = 0.35f;
        _damageInvulnerabilityTimer = DamageInvulnerabilitySeconds;
        _shipMaterial.AlbedoColor = IsShielded ? new Color(0.2f, 0.85f, 1.0f) : new Color(1.0f, 0.25f, 0.2f);
        GameAudio.Instance?.PlayHit();

        if (Health == 0)
        {
            Velocity = Vector3.Zero;
            _shieldTimer = 0;
            _boosterTimer = 0;
            _jumpBuffer = 0;
            CollisionLayer = 0;
            CollisionMask = 0;
            _shipVisual.Visible = false;
            if (_pilotLabel != null) _pilotLabel.Visible = false;
            Defeated?.Invoke();
        }
    }

    public void RespawnAt(Vector3 position)
    {
        CancelTeleport();
        GlobalPosition = position;
        Health = MaximumHealth;
        _combatRecoveryRemaining = _healthRecoveryAccumulator = _hitFeedbackRemaining = 0;
        if (IsPlayerControlled) { _camera.HOffset = 0; _camera.VOffset = 0; }
        RefreshBaseEnergyLimit();
        Energy = EnergyCapacity;
        Velocity = Vector3.Zero;
        _autonomousMovement = Vector3.Zero;
        _turnRate = 0;
        _bankInput = 0;
        _shipVisual.Rotation = Vector3.Zero;
        LastAttacker = null;
        _shieldTimer = 0;
        _boosterTimer = _boosterCooldown = 0;
        _energyRegenAccumulator = 0;
        _fireCooldown = _areaEquipmentCooldown = _shieldEquipmentCooldown = _mobilityEquipmentCooldown = _summonEquipmentCooldown = _claimCooldown = 0;
        _scatterCooldown = _seekerCooldown = 0;
        _damageInvulnerabilityTimer = 2.0f;
        CollisionLayer = _aliveCollisionLayer;
        CollisionMask = _aliveCollisionMask;
        _shipVisual.Visible = true;
        if (_pilotLabel != null) _pilotLabel.Visible = true;
        _arena?.ClampFlightBody(this, MinimumAltitude);
        _jumpBuffer = 0;
        ConfigureHover();
        _hover.Reset(_arena, GlobalPosition);
        StatusMessage = "Respawned at your base";
        TrySynchronizeLoadout();
        _feedbackTimer = 2.0f;
    }

private void CycleQuickEquipment()
    {
        for (int step = 1; step <= 4; step++)
        {
            int next = (_selectedEquipmentSlot + step) % 4;
            if (_equipmentSlots[next] == null) continue;
            _selectedEquipmentSlot = next;
            break;
        }
        StatusMessage = $"Slot {_selectedEquipmentSlot + 1}: {PrimaryEquipmentName}";
        _feedbackTimer = 1.0f;
    }

    private EquipmentDefinition? GetSelectedQuickEquipment()
    {
        return _equipmentSlots[_selectedEquipmentSlot] is { } id ? FindEquipment(id) : null;
    }

    private void CastSelectedQuickEquipment()
    {
        if (GetSelectedQuickEquipment() is not { } equipment) return;
        switch (equipment.Id)
        {
            case "scatter-cannon":
                if (_scatterCooldown > 0 || Energy < equipment.EnergyCost || !CanActivateEquipment(equipment)) return;
                _scatterCooldown = equipment.CooldownSeconds;
                CastScatterEquipment();
                break;
            case "seeker-missile":
                if (_seekerCooldown > 0 || Energy < equipment.EnergyCost || !CanActivateEquipment(equipment)) return;
                _seekerCooldown = equipment.CooldownSeconds;
                CastSeekerEquipment();
                break;
            case "plasma-bolt":
                if (_fireCooldown > 0.0f)
                {
                    return;
                }

                _fireCooldown = EffectiveFireCooldown;
                CastPrimaryEquipment();
                break;
            case "pulse-burst":
                if (_areaEquipmentCooldown > 0.0f)
                {
                    return;
                }

                _areaEquipmentCooldown = _areaEquipment.CooldownSeconds;
                CastAreaEquipment();
                break;
            case "energy-shield":
                if (_shieldEquipmentCooldown > 0.0f)
                {
                    return;
                }

                _shieldEquipmentCooldown = _shieldEquipment.CooldownSeconds;
                CastShieldEquipment();
                break;
            case "dash":
                if (_mobilityEquipmentCooldown > 0.0f)
                {
                    return;
                }

                _mobilityEquipmentCooldown = _mobilityEquipment.CooldownSeconds;
                CastMobilityEquipment();
                break;
            case "guardian-drone":
                if (_summonEquipmentCooldown > 0.0f)
                {
                    return;
                }

                _summonEquipmentCooldown = _summonEquipment.CooldownSeconds;
                CastSummonEquipment();
                break;
        }
    }

    private void CastPrimaryEquipment()
    {
        if (!CanActivateEquipment(_primaryEquipment)) return;
        if (Energy < _primaryEquipment.EnergyCost)
        {
            StatusMessage = "Not enough energy.";
            _feedbackTimer = 0.8f;
            return;
        }

        SpendEnergy(_primaryEquipment.EnergyCost);
        StatusMessage = _primaryEquipment.DisplayName;
        _feedbackTimer = 0.35f;
        PlayEquipmentSound();

        var projectile = new WeaponProjectile { Damage = PlasmaBoltDamage, Speed = EffectiveProjectileSpeed };
        GetParent().AddChild(projectile);

        Vector3 direction = AimDirection;
        projectile.GlobalPosition = AimOrigin + direction * 1.5f;
        projectile.Launch(direction, this);
    }

    private void CastAreaEquipment()
    {
        if (!CanActivateEquipment(_areaEquipment)) return;
        if (Energy < _areaEquipment.EnergyCost)
        {
            StatusMessage = $"Not enough energy for {_areaEquipment.DisplayName.ToLowerInvariant()}.";
            _feedbackTimer = 0.8f;
            return;
        }

        Vector3 direction = AimDirection;
        Vector3 origin = AimOrigin;
        Vector3 target = origin + direction * AreaEquipmentRange;
        Vector3 hitPosition = target;
        using (var ray = PhysicsRayQueryParameters3D.Create(origin, target))
        {
            ray.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
            using var hit = GetWorld3D().DirectSpaceState.IntersectRay(ray);
            if (hit.Count > 0) hitPosition = (Vector3)hit["position"];
            else if (TryFindTerrainHit(origin, target, out Vector3 terrainHit)) hitPosition = terrainHit;
        }

        SpendEnergy(_areaEquipment.EnergyCost);
        int hits = DamageEnemiesInRadius(hitPosition, EffectiveAreaRadius, EffectiveAreaDamage);
        SpawnAreaEquipmentBurst(hitPosition);
        StatusMessage = $"{_areaEquipment.DisplayName}: {hits} hit";
        _feedbackTimer = 0.7f;
        PlayEquipmentSound();
    }

    private void CastShieldEquipment()
    {
        if (!CanActivateEquipment(_shieldEquipment)) return;
        if (Energy < _shieldEquipment.EnergyCost)
        {
            StatusMessage = $"Not enough energy for {_shieldEquipment.DisplayName.ToLowerInvariant()}.";
            _feedbackTimer = 0.8f;
            return;
        }

        SpendEnergy(_shieldEquipment.EnergyCost);
        _shieldTimer = EffectiveShieldDuration;
        StatusMessage = $"{_shieldEquipment.DisplayName} active";
        _feedbackTimer = 0.9f;
        _shipMaterial.AlbedoColor = new Color(0.2f, 0.85f, 1.0f);
        SpawnShieldBurst();
        PlayEquipmentSound();
    }

    public bool TryActivateBooster()
    {
        if (Health <= 0 || GameplayInputBlocked || GetTree().Paused || _boosterCooldown > 0 || IsBoosting) return false;
        if (Energy < BoosterEnergyCost)
        {
            StatusMessage = $"Not enough energy for boost ({BoosterEnergyCost}).";
            _feedbackTimer = 0.8f;
            return false;
        }
        SpendEnergy(BoosterEnergyCost);
        _boosterTimer = BoosterDurationSeconds;
        _boosterCooldown = BoosterCooldownSeconds;
        StatusMessage = "Booster active";
        _feedbackTimer = BoosterDurationSeconds;
        PlayEquipmentSound();
        return true;
    }

    private void ConfigureHover()
    {
        _hover.HoverHeight = MinimumAltitude;
        _hover.Gravity = Mathf.Max(1, HoverGravity);
        _hover.JumpHeight = Mathf.Max(0.1f, JumpHeight);
        _hover.MaximumSlopeDegrees = Mathf.Clamp(MaximumHoverSlopeDegrees, 1, 80);
        _hover.MaximumClimbSpeed = Mathf.Max(1, TerrainFollowMaxLiftSpeed);
    }

    public bool TryActivateJump()
    {
        if (Health <= 0 || GameplayInputBlocked || GetTree().Paused || !_hover.CanJump
            || Energy < Mathf.Max(1, JumpEnergyCost)) return false;
        ConfigureHover();
        SpendEnergy(Mathf.Max(1, JumpEnergyCost));
        Velocity = _hover.Launch(Velocity, LocalUp);
        StatusMessage = "Jump";
        _feedbackTimer = 0.5f;
        return true;
    }

    private void CastMobilityEquipment()
    {
        if (!CanActivateEquipment(_mobilityEquipment)) return;
        if (Energy < _mobilityEquipment.EnergyCost)
        {
            StatusMessage = $"Not enough energy for {_mobilityEquipment.DisplayName.ToLowerInvariant()}.";
            _feedbackTimer = 0.8f;
            return;
        }

        SpendEnergy(_mobilityEquipment.EnergyCost);
        _boosterTimer = 0; // Dash supersedes thrust instead of stacking speeds or being pulled back to boost speed.
        _jumpBuffer = 0;
        _hover.LeaveSupport();
        Vector3 direction = AimDirection;
        Vector3 up = LocalUp;
        Velocity = (direction - up * direction.Dot(up)) * EffectiveDashSpeed
            + up * Mathf.Max(Velocity.Dot(up), Mathf.Clamp(direction.Dot(up) * EffectiveDashSpeed + MobilityDashLift,
                MobilityDashLift, Mathf.Max(MobilityDashLift, Mathf.Sqrt(2 * Mathf.Max(1, HoverGravity) * Mathf.Max(0.1f, JumpHeight)))));
        StatusMessage = _mobilityEquipment.DisplayName;
        _feedbackTimer = 0.6f;
        SpawnMobilityBurst();
        PlayEquipmentSound();
    }

    private void CastSummonEquipment()
    {
        if (!CanActivateEquipment(_summonEquipment)) return;
        if (Energy < _summonEquipment.EnergyCost)
        {
            StatusMessage = $"Not enough energy for {_summonEquipment.DisplayName.ToLowerInvariant()}.";
            _feedbackTimer = 0.8f;
            return;
        }

        SpendEnergy(_summonEquipment.EnergyCost);
        Vector3 direction = AimDirection;
        var guardian = new GuardianDrone
        {
            OwnerPilot = this,
            Name = "GuardianDrone",
            Damage = EffectiveGuardianDamage,
            LifetimeSeconds = 12 + 6 * Mathf.Max(0, GetEquipmentRank("guardian-drone") - 1)
        };
        GetParent().AddChild(guardian);
        guardian.GlobalPosition = GlobalPosition + direction * SummonDistance + LocalUp;
        _arena?.ClampFlightBody(guardian);
        SpawnSummonBurst(guardian.GlobalPosition);
        StatusMessage = $"Summoned {_summonEquipment.DisplayName}";
        _feedbackTimer = 0.8f;
        PlayEquipmentSound();
    }

    private bool CanActivateEquipment(EquipmentDefinition equipment) => Health > 0 && !GameplayInputBlocked
        && !GetTree().Paused && IsEquipmentInstalled(equipment.Id) && EnergyCapacity >= equipment.EnergyCost;

    private void PlayEquipmentSound()
    {
        // The placeholder mixer is non-spatial. Remote AI casts must not sound
        // like the human's own actions from the opposite side of the planet.
        if (IsPlayerControlled || (GetTree().GetFirstNodeInGroup("player") is Node3D listener
            && (_arena?.SurfaceDistance(GlobalPosition, listener.GlobalPosition) ?? GlobalPosition.DistanceTo(listener.GlobalPosition)) < 120))
            GameAudio.Instance?.PlayEquipmentCast();
    }

    private void CastClaimEquipment()
    {
        if (Health <= 0 || GetTree().Paused || GameplayInputBlocked) return;
        if (Energy < _claimEquipment.EnergyCost)
        {
            StatusMessage = $"Not enough energy for {_claimEquipment.DisplayName.ToLowerInvariant()}.";
            _feedbackTimer = 0.8f;
            return;
        }

        SpendEnergy(_claimEquipment.EnergyCost);
        Vector3 direction = AimDirection;
        var projectile = new ClaimProjectile { Name = "ClaimProjectile", MaxRange = EffectiveClaimRange, Ownership = Ownership,
            HitRadius = 1.3f,
            LifetimeSeconds = Mathf.Max(4, EffectiveClaimRange / 48f + 0.05f) };
        projectile.AntimatterClaimed += OnAntimatterClaimed;
        GetParent().AddChild(projectile);
        projectile.GlobalPosition = AimOrigin + direction * 1.5f;
        projectile.Launch(direction);
        StatusMessage = "Claim";
        _feedbackTimer = 0.6f;
        PlayEquipmentSound();
    }

    private void OnAntimatterClaimed(int amount)
    {
        if (!IsInstanceValid(this) || IsQueuedForDeletion())
        {
            return;
        }
        StatusMessage = $"Claimed {amount} antimatter";
        _feedbackTimer = 0.8f;
    }

    private void CastTerrainEquipment(TerrainEquipmentMode mode)
    {
        if (Energy < _terrainEquipment.EnergyCost)
        {
            StatusMessage = $"Not enough energy for {TerrainEquipmentModeText.ToLowerInvariant()}.";
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
        Vector3 target = origin + direction * TerrainEquipmentRange;
        if (!TryFindTerrainHit(origin, target, out Vector3 hitPosition))
        {
            StatusMessage = "Aim at terrain to shape it.";
            _feedbackTimer = 0.8f;
            return;
        }

        SpendEnergy(_terrainEquipment.EnergyCost);
        switch (mode)
        {
            case TerrainEquipmentMode.Crater:
                _arena.ApplyCrater(hitPosition, TerrainEquipmentRadius, TerrainEquipmentDepth);
                break;
            case TerrainEquipmentMode.Raise:
                _arena.RaiseTerrain(hitPosition, TerrainEquipmentRadius, TerrainEquipmentHeight);
                break;
            case TerrainEquipmentMode.Lower:
                _arena.LowerTerrain(hitPosition, TerrainEquipmentRadius, TerrainEquipmentHeight);
                break;
            case TerrainEquipmentMode.Flatten:
                _arena.FlattenTerrain(hitPosition, TerrainEquipmentRadius, _arena.Elevation(hitPosition));
                break;
        }

        SpawnTerrainEquipmentBurst(hitPosition);
        StatusMessage = $"Terrain {TerrainEquipmentModeText.ToLowerInvariant()} ({_arena.LastEditedVertexCount} verts)";
        _feedbackTimer = 0.5f;
        GameAudio.Instance?.PlayEquipmentCast();
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
            if (_arena.IsSpherical && _arena.TrySurfaceHit(origin, target, out hitPosition)) return true;
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
                    float terrainHeight = _arena.HeightAt(point);
                    if (_arena.Elevation(point) <= terrainHeight)
                    {
                        hitPosition = _arena.AtElevation(point, terrainHeight);
                        return true;
                    }
                }
            }
        }

        hitPosition = default;
        return false;
    }

    private void SetTerrainEquipmentMode(TerrainEquipmentMode mode)
    {
        _terrainEquipmentMode = mode;
        StatusMessage = $"Terrain mode: {TerrainEquipmentModeText}";
        _feedbackTimer = 1.0f;
    }

    private void SpawnTerrainEquipmentBurst(Vector3 position)
    {
        var burst = new EquipmentImpactBurst
        {
            Name = "TerrainEquipmentBurst",
            BurstColor = new Color(0.55f, 0.9f, 1.0f),
            EmissionColor = new Color(0.25f, 0.75f, 1.0f),
            EndScale = 4.0f
        };
        GetTree().CurrentScene.AddChild(burst);
        burst.GlobalPosition = position;
    }

    private void SpawnAreaEquipmentBurst(Vector3 position)
    {
        var burst = new EquipmentImpactBurst
        {
            Name = "AreaEquipmentBurst",
            BurstColor = new Color(0.85f, 0.35f, 1.0f, 0.9f),
            EmissionColor = new Color(0.65f, 0.12f, 1.0f),
            DurationSeconds = 0.5f,
            StartScale = 1.2f,
            EndScale = EffectiveAreaRadius
        };
        GetTree().CurrentScene.AddChild(burst);
        burst.GlobalPosition = position;
    }

    private void SpawnShieldBurst()
    {
        var burst = new EquipmentImpactBurst
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
        var burst = new EquipmentImpactBurst
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
        var burst = new EquipmentImpactBurst
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
        foreach (Node node in GetTree().GetNodesInGroup("exploration_wreck"))
            if (node is ExplorationWreck wreck && !wreck.IsRevealed && wreck.GlobalPosition.DistanceTo(center) <= radius)
            { wreck.ApplyDamage(damage); hits++; }
        foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
        {
            if (node is not EnemyEcosystemEnemy enemy || !FactionCombat.CanDamage(enemy, Ownership))
            {
                continue;
            }

            if (enemy.GlobalPosition.DistanceTo(center) > radius)
            {
                continue;
            }

            FactionCombat.Damage(enemy, damage, this);
            hits++;
        }

foreach (Node node in GetTree().GetNodesInGroup("simple_monster"))
        {
            if (node is not SimpleMonster monster || monster.GlobalPosition.DistanceTo(center) > radius)
            {
                continue;
            }

            FactionCombat.Damage(monster, damage, this);
            hits++;
        }

        foreach (Node node in GetTree().GetNodesInGroup("drone"))
        {
            if (node is not CollectorDrone drone || !FactionCombat.CanDamage(drone, Ownership) || drone.GlobalPosition.DistanceTo(center) > radius)
            {
                continue;
            }

            FactionCombat.Damage(drone, damage, this);
            hits++;
        }

        foreach (Node node in GetTree().GetNodesInGroup("pilots"))
            if (node is ShipFlightController ship && FactionCombat.CanDamage(ship, Ownership) && ship.GlobalPosition.DistanceTo(center) <= radius)
            { FactionCombat.Damage(ship, damage, this); hits++; }
        foreach (string group in new[] { "base_tactical_drone", "base_strike_drone", "guardian-drone", "guided_projectile" })
            foreach (Node node in GetTree().GetNodesInGroup(group))
                if (node is Node3D unit && FactionCombat.CanDamage(unit, Ownership) && unit.GlobalPosition.DistanceTo(center) <= radius)
                { FactionCombat.Damage(unit, damage, this); hits++; }

        return hits;
    }

    private void RouteAntimatterToWell()
    {
        _antimatterSource ??= GetTree().GetFirstNodeInGroup("antimatter_source") as AntimatterSource;
        if (_antimatterSource == null || Energy <= EnergyDepositReserve)
        {
            return;
        }

        if (GlobalPosition.DistanceTo(_antimatterSource.GlobalPosition) > EnergyDepositRadius)
        {
            return;
        }

        int depositAmount = Energy - EnergyDepositReserve;
        int deposited = _antimatterSource.DepositAntimatter(depositAmount);
        if (deposited <= 0)
        {
            return;
        }

        Energy -= deposited;
        StatusMessage = $"Banked {deposited} antimatter";
        _feedbackTimer = 0.6f;
    }

    private void UpdateCamera(float delta)
    {
        if (!IsPlayerControlled)
        {
            _shipVisual.Visible = Health > 0;
            _shipMaterial.AlbedoColor = IsShielded ? new Color(0.2f, 0.85f, 1) : new Color(0.9f, 0.15f, 0.4f);
            return;
        }
        _shipVisual.Visible = !_firstPersonCamera;
        float shake = HitFeedbackStrength;
        float phase = (0.28f - _hitFeedbackRemaining) * 95;
        _camera.HOffset = Mathf.Sin(phase) * 0.085f * shake;
        _camera.VOffset = Mathf.Sin(phase * 1.37f) * 0.055f * shake;
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
            _camera.LookAt(_cameraPivot.GlobalPosition, LocalUp);
        }
        _camera.RotateObjectLocal(Vector3.Back, _shipVisual.Rotation.Z * (10f / Mathf.Max(1, BankDegrees)));

        if (IsInvulnerable)
        {
            float pulse = 0.5f + Mathf.Sin(Time.GetTicksMsec() * 0.02f) * 0.5f;
            _shipMaterial.AlbedoColor = new Color(1.0f, Mathf.Lerp(0.25f, 0.8f, pulse), Mathf.Lerp(0.2f, 0.95f, pulse));
        }
        else if (IsShielded)
        {
            float pulse = 0.5f + Mathf.Sin(Time.GetTicksMsec() * 0.018f) * 0.5f;
            _shipMaterial.AlbedoColor = new Color(Mathf.Lerp(0.08f, 0.25f, pulse), Mathf.Lerp(0.55f, 0.95f, pulse), 1.0f);
        }
        else if (_feedbackTimer <= 0.0f)
        {
            _shipMaterial.AlbedoColor = _shipMaterial.AlbedoColor.Lerp(IsPlayerControlled
                ? new Color(0.38f, 0.16f, 0.82f) : new Color(0.8f, 0.18f, 0.12f), Mathf.Clamp(10.0f * delta, 0.0f, 1.0f));
        }
    }

    private void UpdateShipBank(float delta)
    {
        Vector3 localVelocity = GlobalBasis.Inverse() * Velocity;
        float speedWeight = Mathf.Clamp(Speed / MoveSpeed, 0, 1);
        float turn = Mathf.Clamp(_turnRate / 2.5f, -1, 1) * speedWeight;
        float smoothingWeight = 1 - Mathf.Exp(-Mathf.Max(0, BankSmoothing) * delta);
        _bankInput = Mathf.Lerp(_bankInput, Mathf.Clamp(turn - localVelocity.X / MoveSpeed * 0.6f, -1, 1), smoothingWeight);
        float targetRoll = Mathf.DegToRad(-_bankInput * BankDegrees);
        float targetPitch = Mathf.DegToRad(Mathf.Clamp(localVelocity.Z / MoveSpeed, -1.0f, 1.0f) * 9.0f);
        _shipVisual.Rotation = new Vector3(
            Mathf.LerpAngle(_shipVisual.Rotation.X, targetPitch, smoothingWeight),
            0.0f,
            Mathf.LerpAngle(_shipVisual.Rotation.Z, targetRoll, smoothingWeight));
    }

    private void AddShipVisual()
    {
        _shipVisual = new MeshInstance3D { Name = "ShipVisual" };
        _shipVisual.Mesh = HovercraftHull.Create();
        _shipVisual.Position = new Vector3(0.0f, -0.35f, 0.0f);

        _shipMaterial = new StandardMaterial3D
        {
            AlbedoColor = IsPlayerControlled ? new Color(0.38f, 0.16f, 0.82f) : new Color(0.8f, 0.18f, 0.12f),
            Roughness = 0.65f
        };
        _shipVisual.MaterialOverride = _shipMaterial;
        AddChild(_shipVisual);
        var thrusterMaterial = new StandardMaterial3D
        {
            AlbedoColor = IsPlayerControlled ? Colors.Cyan : Colors.Orange,
            EmissionEnabled = true,
            Emission = IsPlayerControlled ? Colors.Cyan : Colors.Orange
        };
        foreach (float x in new[] { -0.7f, 0.7f })
            _shipVisual.AddChild(new MeshInstance3D
            {
                Name = "RearThruster", Position = new Vector3(x, 0, 1.16f),
                Mesh = new BoxMesh { Size = new Vector3(0.32f, 0.16f, 0.12f) },
                MaterialOverride = thrusterMaterial
            });

        var collision = new CollisionShape3D
        {
            Name = "CollisionShape3D",
            Shape = new BoxShape3D { Size = new Vector3(1.6f, 0.6f, 2.4f) }
        };
        AddChild(collision);
    }
}

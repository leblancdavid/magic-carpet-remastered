using Godot;
using MagicCarpetRemastered.Scripts.Player;

namespace MagicCarpetRemastered.Scripts.World;

public partial class FactionBase : StaticBody3D
{
    [Export] public bool IsPlayerBase { get; set; } = true;
    [Export] public int Tier1StorageCapacity { get; set; } = 200;
    [Export] public int Tier2StorageCapacity { get; set; } = 350;
    [Export] public int Tier3StorageCapacity { get; set; } = 550;
    [Export] public int Tier4StorageCapacity { get; set; } = 800;
    [Export] public int Tier5StorageCapacity { get; set; } = 1100;
    [Export] public int InitialStoredAntimatter { get; set; } = 30;
    [Export] public int MaxHealth { get; set; } = 400;
    [Export] public float ScalePerTier { get; set; } = 0.18f;
    [Export] public float RepairDelaySeconds { get; set; } = 5.0f;
    [Export] public float RepairHealthPerSecond { get; set; } = 3.0f;
    [Export] public int Tier2UpgradeCost { get; set; } = 150;
    [Export] public int Tier3UpgradeCost { get; set; } = 275;
    [Export] public int Tier4UpgradeCost { get; set; } = 450;
    [Export] public int Tier5UpgradeCost { get; set; } = 650;
    [Export] public float Tier2UpgradeSeconds { get; set; } = 60;
    [Export] public float Tier3UpgradeSeconds { get; set; } = 80;
    [Export] public float Tier4UpgradeSeconds { get; set; } = 100;
    [Export] public float Tier5UpgradeSeconds { get; set; } = 120;

    public int StoredAntimatter { get; private set; }
    public int Health { get; private set; }
    public const int MaximumTier = 5;
    public int Tier { get; private set; } = 1;
    public int HighestPurchasedTier { get; private set; } = 1;
    public BaseConstruction Construction { get; private set; } = null!;
    public float EnergyRechargeMultiplier => 1 + 0.15f * Construction.UpgradeRank("energy-grid", true);
    public float CollectorSpeedMultiplier => 1 + 0.15f * Construction.UpgradeRank("collector-drive", true);
    public float EffectiveRepairHealthPerSecond => RepairHealthPerSecond * (1 + 0.25f * Construction.UpgradeRank("repair-matrix", true));
    public long RefundedAntimatter { get; private set; }
    public long InvestmentSpilledAntimatter { get; private set; }
    public long NetCommittedAntimatter => SpentAntimatter - RefundedAntimatter - InvestmentSpilledAntimatter;
    public int AntimatterStorageCapacity => StorageCapacityForTier(Tier);
    public int AvailableStorage => Mathf.Max(0, EffectiveStorageCapacity - StoredAntimatter);
    // Each layer contributes its incremental storage. This is continuous at tier
    // boundaries and prevents capacity increasing when a damaged tier falls.
    public int EffectiveStorageCapacity => IsDestroyed ? 0 : StorageCapacityForTier(Tier - 1)
        + (int)(((long)(AntimatterStorageCapacity - StorageCapacityForTier(Tier - 1))
            * (Health - (Tier - 1) * _healthPerTier) + _healthPerTier - 1) / _healthPerTier);
    public bool IsDestroyed => Health <= 0;
    public string BaseLabel => IsPlayerBase ? "Player Base" : "Enemy Base";
    public bool IsRepairing => !IsDestroyed && Health < MaxHealth && _secondsSinceHit >= RepairDelaySeconds;
    private float _secondsSinceHit;
    private float _repairAccumulator;
    private int _healthPerTier = 400;
    private BaseDefense _defense = null!;

    private StandardMaterial3D _baseMaterial = null!;
    private StandardMaterial3D _towerMaterial = null!;
    private MeshInstance3D _keep = null!;
    private MeshInstance3D _tower = null!;
    private MeshInstance3D _roof = null!;

    public override void _Ready()
    {
        AddToGroup(IsPlayerBase ? "player_base" : "enemy_base");
        (GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena)?.Orient(this);
        MaxHealth = Mathf.Max(1, MaxHealth);
        _healthPerTier = Mathf.Min(MaxHealth, int.MaxValue / MaximumTier);
        MaxHealth = _healthPerTier;
        Health = MaxHealth;
        Construction = new BaseConstruction(this);
        BaseProjectCatalog.Register(Construction);
        StoredAntimatter = Mathf.Clamp(InitialStoredAntimatter, 0, EffectiveStorageCapacity);
        BuildBaseVisuals();
        _defense = new BaseDefense { Name = "BaseDefense" };
        AddChild(_defense);
        UpdateEconomy();
    }

    public override void _PhysicsProcess(double delta)
    {
        Construction.Advance(delta);
        if (IsDestroyed || Health >= MaxHealth || RepairHealthPerSecond <= 0) return;
        float before = _secondsSinceHit;
        _secondsSinceHit += (float)delta;
        // Only the part of this tick after the quiet delay contributes repair.
        float activeTime = Mathf.Max(0, _secondsSinceHit - Mathf.Max(before, RepairDelaySeconds));
        _repairAccumulator += activeTime * EffectiveRepairHealthPerSecond;
        int restored = Mathf.FloorToInt(_repairAccumulator);
        if (restored <= 0) return;
        _repairAccumulator -= restored;
        Health = Mathf.Min(MaxHealth, Health + restored);
        UpdateEconomy(); // Capacity returns, but spilled antimatter is never recreated.
    }

    public int DepositAntimatter(int amount)
    {
        if (amount <= 0 || IsDestroyed)
        {
            return 0;
        }

        int accepted = Mathf.Min(amount, AvailableStorage);
        if (accepted <= 0)
        {
            return 0;
        }

        StoredAntimatter += accepted;
        UpdateEconomy();
        return accepted;
    }

    public int WithdrawAntimatter(int amount)
    {
        if (amount <= 0 || IsDestroyed)
        {
            return 0;
        }

        int withdrawn = Mathf.Min(amount, StoredAntimatter);
        StoredAntimatter -= withdrawn;
        UpdateEconomy();
        return withdrawn;
    }

    // Atomic payment for future construction callers. WithdrawAntimatter remains
    // a partial physical transfer; spending is an intentional resource sink.
    public long SpentAntimatter { get; private set; }
    public bool TrySpendAntimatter(int amount)
    {
        if (amount <= 0 || IsDestroyed || amount > StoredAntimatter) return false;
        StoredAntimatter -= amount;
        SpentAntimatter += amount;
        UpdateEconomy();
        return true;
    }

    public int StorageCapacityForTier(int tier) => tier switch
    {
        1 => Mathf.Max(0, Tier1StorageCapacity),
        2 => Mathf.Max(Mathf.Max(0, Tier1StorageCapacity), Tier2StorageCapacity),
        3 => Mathf.Max(StorageCapacityForTier(2), Tier3StorageCapacity),
        4 => Mathf.Max(StorageCapacityForTier(3), Tier4StorageCapacity),
        5 => Mathf.Max(StorageCapacityForTier(4), Tier5StorageCapacity),
        _ => 0
    };

    // Completion only: the construction lifecycle owns payment and timing.
    // An expected target prevents duplicate completions and skipped tiers.
    internal bool TryCompleteTierUpgrade(int targetTier)
    {
        if (IsDestroyed || targetTier != Tier + 1 || targetTier > MaximumTier) return false;
        Tier = targetTier;
        HighestPurchasedTier = Mathf.Max(HighestPurchasedTier, Tier);
        MaxHealth = Tier * _healthPerTier;
        Health = Mathf.Min(MaxHealth, Health + _healthPerTier);
        UpdateEconomy();
        return true;
    }

    public int UpgradeCostForTier(int tier) => tier switch
    {
        2 => Tier2UpgradeCost, 3 => Tier3UpgradeCost, 4 => Tier4UpgradeCost, 5 => Tier5UpgradeCost, _ => 0
    };

    public double UpgradeSecondsForTier(int tier) => tier switch
    {
        2 => Tier2UpgradeSeconds, 3 => Tier3UpgradeSeconds, 4 => Tier4UpgradeSeconds, 5 => Tier5UpgradeSeconds, _ => 0
    };

    internal void RefundConstruction(int amount)
    {
        RefundedAntimatter += amount;
        int overflow = amount - DepositAntimatter(amount);
        AntimatterScatter.Spawn(GetParent(), GlobalPosition, overflow,
            IsPlayerBase ? AntimatterOwnership.Player : AntimatterOwnership.Enemy,
            minimumRadius: 6, name: "ConstructionRefundSpill");
    }

    internal void SpillConstructionInvestment(int amount)
    {
        InvestmentSpilledAntimatter += amount;
        AntimatterScatter.Spawn(GetParent(), GlobalPosition, amount,
            minimumRadius: 6, name: "ConstructionInvestmentSpill");
    }

    public void ApplyDamage(int amount, Node3D? attacker = null)
    {
        if (amount <= 0 || IsDestroyed)
        {
            return;
        }

        int previousTier = Tier;
        _defense.NotifyAttack(attacker);
        int armorRank = Construction.UpgradeRank("base-armor", true);
        if (armorRank > 0) amount = Mathf.Max(1, Mathf.RoundToInt(amount * (1 - 0.05f * (armorRank + 1))));
        Health = Mathf.Max(0, Health - amount);
        Tier = Health == 0 ? 0 : Mathf.Min(Tier, (Health - 1) / _healthPerTier + 1);
        MaxHealth = Mathf.Max(_healthPerTier, Tier * _healthPerTier);
        if (Tier < previousTier) Construction.DestroyAll();
        _secondsSinceHit = 0;
        _repairAccumulator = 0;
        int spilled = Mathf.Max(0, StoredAntimatter - EffectiveStorageCapacity);
        StoredAntimatter -= spilled;
        SpillAntimatter(spilled);
        UpdateEconomy();
    }

    public string GetStatusText()
    {
        string destruction = IsDestroyed ? "Destroyed" : $"HP {Health}/{MaxHealth}";
        return $"{BaseLabel}: {destruction}   Antimatter {StoredAntimatter}/{EffectiveStorageCapacity}   Tier {Tier}";
    }

    private void UpdateEconomy()
    {
        UpdateBaseScale();
        UpdateVisualState();
        foreach (Node node in GetTree().GetNodesInGroup("pilots"))
            if (node is ShipFlightController ship && ship.IsPlayerControlled == IsPlayerBase) ship.RefreshBaseEnergyLimit();
    }

    private void SpillAntimatter(int amount)
    {
        AntimatterScatter.Spawn(GetParent(), GlobalPosition, amount, minimumRadius: 6.0f, name: "BaseAntimatterSpill");
    }

    private void UpdateBaseScale()
    {
        float scale = 1.0f + ScalePerTier * (Tier - 1);
        _keep.Scale = new Vector3(scale, 1.0f + 0.1f * (Tier - 1), scale);
        _tower.Scale = new Vector3(scale, scale, scale);
        if (IsDestroyed)
        {
            _keep.Scale = new Vector3(1, 0.3f, 1);
            _tower.Scale = new Vector3(0.6f, 0.3f, 0.6f);
            _tower.Position = new Vector3(0, 1.5f, 0);
            _roof.Visible = false;
        }
    }

    private void UpdateVisualState()
    {
        float damageBlend = 1.0f - Mathf.Clamp((float)Health / MaxHealth, 0.0f, 1.0f);
        Color keepColor = IsPlayerBase
            ? new Color(0.26f, 0.22f, 0.34f).Lerp(new Color(0.36f, 0.45f, 0.84f), Mathf.Min(1, 0.2f + 0.2f * Tier))
            : new Color(0.32f, 0.18f, 0.16f).Lerp(new Color(0.76f, 0.2f, 0.18f), 0.18f + 0.15f * Tier);
        Color towerColor = IsPlayerBase
            ? new Color(0.16f, 0.48f, 0.9f)
            : new Color(0.85f, 0.28f, 0.18f);

        _baseMaterial.AlbedoColor = keepColor.Lerp(new Color(0.95f, 0.15f, 0.12f), damageBlend * 0.55f);
        _towerMaterial.AlbedoColor = towerColor.Lerp(new Color(1.0f, 0.35f, 0.25f), damageBlend * 0.35f);
    }

    private void BuildBaseVisuals()
    {
        var collision = new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(8.0f, 5.0f, 8.0f) }
        };
        AddChild(collision);

        _keep = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(7.0f, 4.2f, 7.0f) }
        };
        _baseMaterial = new StandardMaterial3D
        {
            Roughness = 0.95f
        };
        _keep.MaterialOverride = _baseMaterial;
        AddChild(_keep);

        _tower = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 1.0f, BottomRadius = 1.5f, Height = 6.0f }
        };
        _tower.Position = new Vector3(0.0f, 4.5f, 0.0f);
        _towerMaterial = new StandardMaterial3D
        {
            Roughness = 0.85f
        };
        _tower.MaterialOverride = _towerMaterial;
        AddChild(_tower);

        _roof = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.2f, BottomRadius = 1.8f, Height = 2.5f }
        };
        _roof.Position = new Vector3(0.0f, 8.0f, 0.0f);
        _roof.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = IsPlayerBase ? new Color(0.68f, 0.82f, 1.0f) : new Color(1.0f, 0.62f, 0.58f),
            Roughness = 0.8f
        };
        AddChild(_roof);
    }
}

using Godot;

namespace MagicCarpetRemastered.Scripts.World;

public partial class CastleKeep : StaticBody3D
{
    [Export] public bool IsPlayerCastle { get; set; } = true;
    [Export] public int ManaStorageCapacity { get; set; } = 120;
    [Export] public int InitialStoredMana { get; set; } = 30;
    [Export] public int MaxHealth { get; set; } = 140;
    [Export] public float GrowthManaPerStage { get; set; } = 40.0f;
    [Export] public float GrowthScalePerStage { get; set; } = 0.18f;

    public int StoredMana { get; private set; }
    public int Health { get; private set; }
    public int GrowthStage { get; private set; } = 1;
    public bool IsDestroyed => Health <= 0;
    public string CastleLabel => IsPlayerCastle ? "Player Castle" : "Enemy Castle";

    private StandardMaterial3D _baseMaterial = null!;
    private StandardMaterial3D _towerMaterial = null!;
    private MeshInstance3D _keep = null!;
    private MeshInstance3D _tower = null!;

    public override void _Ready()
    {
        AddToGroup(IsPlayerCastle ? "player_castle" : "enemy_castle");
        StoredMana = Mathf.Clamp(InitialStoredMana, 0, ManaStorageCapacity);
        Health = MaxHealth;
        BuildCastleVisuals();
        UpdateGrowth();
    }

    public int DepositMana(int amount)
    {
        if (amount <= 0 || IsDestroyed)
        {
            return 0;
        }

        int accepted = Mathf.Min(amount, ManaStorageCapacity - StoredMana);
        if (accepted <= 0)
        {
            return 0;
        }

        StoredMana += accepted;
        UpdateGrowth();
        return accepted;
    }

    public int WithdrawMana(int amount)
    {
        if (amount <= 0 || IsDestroyed)
        {
            return 0;
        }

        int withdrawn = Mathf.Min(amount, StoredMana);
        StoredMana -= withdrawn;
        UpdateGrowth();
        return withdrawn;
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0 || IsDestroyed)
        {
            return;
        }

        Health = Mathf.Max(0, Health - amount);
        UpdateVisualState();
    }

    public string GetStatusText()
    {
        string destruction = IsDestroyed ? "Destroyed" : $"HP {Health}/{MaxHealth}";
        return $"{CastleLabel}: {destruction}   Mana {StoredMana}/{ManaStorageCapacity}   Stage {GrowthStage}";
    }

    private void UpdateGrowth()
    {
        int desiredStage = 1 + Mathf.FloorToInt(StoredMana / GrowthManaPerStage);
        desiredStage = Mathf.Clamp(desiredStage, 1, 4);
        if (desiredStage != GrowthStage)
        {
            GrowthStage = desiredStage;
            UpdateCastleScale();
        }

        UpdateVisualState();
    }

    private void UpdateCastleScale()
    {
        float scale = 1.0f + GrowthScalePerStage * (GrowthStage - 1);
        _keep.Scale = new Vector3(scale, 1.0f + 0.1f * (GrowthStage - 1), scale);
        _tower.Scale = new Vector3(scale, scale, scale);
    }

    private void UpdateVisualState()
    {
        float damageBlend = 1.0f - Mathf.Clamp((float)Health / MaxHealth, 0.0f, 1.0f);
        Color keepColor = IsPlayerCastle
            ? new Color(0.26f, 0.22f, 0.34f).Lerp(new Color(0.36f, 0.45f, 0.84f), 0.2f + 0.2f * GrowthStage)
            : new Color(0.32f, 0.18f, 0.16f).Lerp(new Color(0.76f, 0.2f, 0.18f), 0.18f + 0.15f * GrowthStage);
        Color towerColor = IsPlayerCastle
            ? new Color(0.16f, 0.48f, 0.9f)
            : new Color(0.85f, 0.28f, 0.18f);

        _baseMaterial.AlbedoColor = keepColor.Lerp(new Color(0.95f, 0.15f, 0.12f), damageBlend * 0.55f);
        _towerMaterial.AlbedoColor = towerColor.Lerp(new Color(1.0f, 0.35f, 0.25f), damageBlend * 0.35f);
    }

    private void BuildCastleVisuals()
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

        var roof = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.2f, BottomRadius = 1.8f, Height = 2.5f }
        };
        roof.Position = new Vector3(0.0f, 8.0f, 0.0f);
        roof.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = IsPlayerCastle ? new Color(0.68f, 0.82f, 1.0f) : new Color(1.0f, 0.62f, 0.58f),
            Roughness = 0.8f
        };
        AddChild(roof);
    }
}

using Godot;

namespace MagicCarpetRemastered.Scripts.World;

public partial class ManaWell : StaticBody3D
{
    [Export] public int StorageCapacity { get; set; } = 160;
    [Export] public int InitialStoredMana { get; set; } = 60;
    [Export] public float PickupSpawnRadius { get; set; } = 2.2f;
    [Export] public float PickupSpawnHeight { get; set; } = 2.8f;
    [Export] public int SeedOrbCount { get; set; } = 6;

    public int StoredMana { get; private set; }
    public bool HasSpace => StoredMana < StorageCapacity;

    public override void _Ready()
    {
        AddToGroup("mana_well");
        StoredMana = 0;
        AddCollisionAndVisuals();
        SeedNeutralField();
    }

    public int DepositMana(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        int accepted = Mathf.Min(amount, StorageCapacity - StoredMana);
        if (accepted <= 0)
        {
            return 0;
        }

        StoredMana += accepted;
        return accepted;
    }

    public int StealMana(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        int stolen = Mathf.Min(amount, StoredMana);
        StoredMana -= stolen;
        return stolen;
    }

    public int WithdrawMana(int amount)
    {
        return StealMana(amount);
    }

    private void SeedNeutralField()
    {
        int amountPerOrb = Mathf.Max(2, InitialStoredMana / SeedOrbCount);
        for (int i = 0; i < SeedOrbCount; i++)
        {
            float angle = Mathf.Tau * i / SeedOrbCount + GD.Randf() * 0.5f;
            float radius = 2.0f + GD.Randf() * 2.0f;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0.0f, Mathf.Sin(angle) * radius);

            var orb = new ManaPickup
            {
                Name = "WellMana",
                ManaAmount = amountPerOrb,
                Ownership = ManaOwnership.Neutral
            };
            GetTree().CurrentScene.AddChild(orb);
            orb.GlobalPosition = GlobalPosition + offset + Vector3.Up * PickupSpawnHeight;
        }
    }

    private void AddCollisionAndVisuals()
    {
        var collision = new CollisionShape3D
        {
            Shape = new CylinderShape3D { Radius = 2.0f, Height = 3.6f }
        };
        AddChild(collision);

        var baseMesh = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 1.8f, BottomRadius = 2.2f, Height = 1.8f }
        };
        baseMesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.14f, 0.18f, 0.3f),
            Roughness = 0.95f
        };
        AddChild(baseMesh);

        var crystal = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 1.05f, Height = 2.1f }
        };
        crystal.Position = new Vector3(0.0f, 1.8f, 0.0f);
        crystal.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.12f, 0.75f, 1.0f),
            EmissionEnabled = true,
            Emission = new Color(0.08f, 0.45f, 0.95f),
            EmissionEnergyMultiplier = 1.6f,
            Roughness = 0.25f
        };
        AddChild(crystal);
    }
}
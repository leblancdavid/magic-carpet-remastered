using Godot;
using MagicCarpetRemastered.Scripts.Player;

namespace MagicCarpetRemastered.Scripts.World;

public partial class ManaPickup : Area3D
{
    [Export] public int ManaAmount { get; set; } = 10;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;

        var collision = new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 0.8f }
        };
        AddChild(collision);

        var mesh = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.45f, Height = 0.9f }
        };
        mesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.15f, 0.8f, 1.0f),
            EmissionEnabled = true,
            Emission = new Color(0.05f, 0.6f, 1.0f),
            EmissionEnergyMultiplier = 1.5f
        };
        AddChild(mesh);
    }

    public override void _Process(double delta)
    {
        RotateY((float)delta * 2.0f);
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not CarpetFlightController player)
        {
            return;
        }

        player.AddMana(ManaAmount);
        QueueFree();
    }
}

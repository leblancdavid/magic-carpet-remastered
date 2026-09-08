using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Ui;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Game;

public partial class Game : Node3D
{
    private HeightmapArena _arena = null!;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("restart_arena"))
        {
            GetTree().ReloadCurrentScene();
        }
    }

    public override void _Ready()
    {
        CreateLighting();
        CreateArena();
        CreatePlayer();
        CreateManaPickups();
        CreateEnemies();
        AddChild(new Hud { Name = "Hud" });
    }

    private void CreateLighting()
    {
        AddChild(new WorldEnvironment
        {
            Environment = new Environment
            {
                BackgroundMode = Environment.BGMode.Color,
                BackgroundColor = new Color(0.04f, 0.05f, 0.12f),
                AmbientLightSource = Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.35f, 0.4f, 0.55f),
                AmbientLightEnergy = 0.8f
            }
        });

        var sun = new DirectionalLight3D
        {
            Name = "Sun",
            RotationDegrees = new Vector3(-55.0f, 35.0f, 0.0f),
            LightEnergy = 2.5f
        };
        AddChild(sun);
    }

    private void CreateArena()
    {
        _arena = new HeightmapArena { Name = "HeightmapArena" };
        AddChild(_arena);

        for (int i = 0; i < 18; i++)
        {
            float angle = Mathf.Tau * i / 18.0f;
            float radius = 28.0f + (i % 3) * 6.0f;
            Vector3 position = AboveTerrain(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 1.5f);
            AddPillar(position);
        }
    }

    private void CreatePlayer()
    {
        var player = new CarpetFlightController
        {
            Name = "PlayerCarpet",
            Position = AboveTerrain(0.0f, 14.0f, 9.0f)
        };
        AddChild(player);
    }

    private void CreateManaPickups()
    {
        Vector3[] positions =
        {
            AboveTerrain(-8.0f, -8.0f, 4.0f),
            AboveTerrain(8.0f, -10.0f, 5.0f),
            AboveTerrain(14.0f, 4.0f, 4.0f),
            AboveTerrain(-16.0f, 8.0f, 6.0f),
            AboveTerrain(0.0f, -22.0f, 7.0f)
        };

        foreach (Vector3 position in positions)
        {
            AddChild(new ManaPickup
            {
                Name = "ManaPickup",
                Position = position
            });
        }
    }

    private void CreateEnemies()
    {
        Vector3[] positions =
        {
            AboveTerrain(0.0f, -18.0f, 3.0f),
            AboveTerrain(18.0f, -4.0f, 4.0f),
            AboveTerrain(-18.0f, 2.0f, 5.0f)
        };

        foreach (Vector3 position in positions)
        {
            AddChild(new SimpleMonster
            {
                Name = "SimpleMonster",
                Position = position
            });
        }
    }

    private void AddPillar(Vector3 position)
    {
        var pillar = new StaticBody3D
        {
            Name = "StonePillar",
            Position = position
        };
        AddChild(pillar);

        pillar.AddChild(new CollisionShape3D
        {
            Shape = new CylinderShape3D { Radius = 1.5f, Height = 3.0f }
        });

        var mesh = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 1.3f, BottomRadius = 1.6f, Height = 3.0f }
        };
        mesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.42f, 0.39f, 0.34f),
            Roughness = 1.0f
        };
        pillar.AddChild(mesh);
    }

    private Vector3 AboveTerrain(float x, float z, float clearance)
    {
        return new Vector3(x, _arena.HeightAt(x, z) + clearance, z);
    }
}

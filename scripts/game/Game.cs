using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Ui;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Game;

public partial class Game : Node3D
{
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
        var groundBody = new StaticBody3D { Name = "ArenaGround" };
        AddChild(groundBody);

        var groundMesh = new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(120.0f, 120.0f) }
        };
        groundMesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.21f, 0.28f, 0.16f),
            Roughness = 1.0f
        };
        groundBody.AddChild(groundMesh);

        groundBody.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(120.0f, 0.2f, 120.0f) },
            Position = new Vector3(0.0f, -0.1f, 0.0f)
        });

        for (int i = 0; i < 18; i++)
        {
            float angle = Mathf.Tau * i / 18.0f;
            float radius = 28.0f + (i % 3) * 6.0f;
            AddPillar(new Vector3(Mathf.Cos(angle) * radius, 1.5f, Mathf.Sin(angle) * radius));
        }
    }

    private void CreatePlayer()
    {
        var player = new CarpetFlightController
        {
            Name = "PlayerCarpet",
            Position = new Vector3(0.0f, 4.0f, 14.0f)
        };
        AddChild(player);
    }

    private void CreateManaPickups()
    {
        Vector3[] positions =
        {
            new(-8.0f, 3.0f, -8.0f),
            new(8.0f, 5.0f, -10.0f),
            new(14.0f, 4.0f, 4.0f),
            new(-16.0f, 6.0f, 8.0f),
            new(0.0f, 7.0f, -22.0f)
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
            new(0.0f, 3.0f, -18.0f),
            new(18.0f, 4.0f, -4.0f),
            new(-18.0f, 5.0f, 2.0f)
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
}

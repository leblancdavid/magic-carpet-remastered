using Godot;
using System.Collections.Generic;
using MagicCarpetRemastered.Scripts.Audio;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.Ui;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Game;

public partial class Game : Node3D
{
    [Export] public bool UseSphericalWorld { get; set; }
    private HeightmapArena _arena = null!;
    private FactionBase _playerBase = null!;
    private FactionBase _enemyBase = null!;
    private ShipFlightController _player = null!;
    private readonly List<WorldLandmark> _landmarks = new();

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.F6)
        {
            GetTree().Paused = false;
            GetTree().ChangeSceneToFile("res://scenes/PlanetFlightTest.tscn");
            return;
        }
        if (@event.IsActionPressed("restart_arena"))
        {
            GetTree().Paused = false;
            GetTree().ReloadCurrentScene();
        }
    }

    public override void _Ready()
    {
        CreateLighting();
        EquipmentImpactPool.Prepare(this);
        AddChild(new GameAudio { Name = "GameAudio" });
        CreateArena();
        if (UseSphericalWorld) CreateLandmarks();
        CreateAntimatterSource();
        CreateBases();
        CreatePlayer();
        CreateDrones();
        CreateAntimatterPickups();
        CreateEquipmentDiscoveries();
        CreateEnemyEcosystem();
        CreateLevelObjective();
        AddChild(new WorldAntimatterTracker { Name = "WorldAntimatterTracker" });
        AddChild(new Hud { Name = "Hud" });
        if (UseSphericalWorld) AddChild(new PlanetSettings { Name = "PlanetSettings" });
        if (UseSphericalWorld)
        {
            var navigation = new WorldNavigation { Name = "WorldNavigation" };
            AddChild(navigation);
            AddChild(new TacticalMap { Name = "TacticalMap", Navigation = navigation });
        }
        AddChild(new BaseManagement { Name = "BaseManagement" });
        _arena.RefreshPlanetDetail();
        ProcessMode = ProcessModeEnum.Pausable;
        foreach (Node child in GetChildren())
        {
            child.ProcessMode = child is Hud or PlanetSettings or TacticalMap or BaseManagement ? ProcessModeEnum.Always : ProcessModeEnum.Pausable;
        }
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
            LightEnergy = UseSphericalWorld ? 1.2f : 2.5f
        };
        AddChild(sun);
    }

    private void CreateArena()
    {
        _arena = new HeightmapArena { Name = "HeightmapArena", IsSpherical = UseSphericalWorld, PlanetRadius = PlanetSizes.SelectedRadius };
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
        _player = new ShipFlightController
        {
            Name = "PlayerShip",
            Position = AboveTerrain(UseSphericalWorld ? -130 : 0, 14, 9)
        };
        AddChild(_player);
    }

    private void CreateAntimatterSource()
    {
        var well = new AntimatterSource
        {
            Name = "AntimatterSource",
            Position = AboveTerrain(3.0f, 3.0f, 1.5f)
        };
        AddChild(well);
    }

    private void CreateBases()
    {
        _playerBase = new FactionBase
        {
            Name = "PlayerBase",
            IsPlayerBase = true,
            Position = AboveTerrain(UseSphericalWorld ? -145 : -22, 8, 2)
        };
        AddChild(_playerBase);

        _enemyBase = new FactionBase
        {
            Name = "EnemyBase",
            IsPlayerBase = false,
            Position = AboveTerrain(UseSphericalWorld ? 145 : 22, -12, 2),
            InitialStoredAntimatter = 48
        };
        AddChild(_enemyBase);
    }

    private void CreateDrones()
    {
        AddChild(new CollectorDrone
        {
            Name = "PlayerDrone",
            UsePlayerAntimatter = true,
            Position = AboveTerrain(UseSphericalWorld ? -130 : -16, 4, 5),
            MaxSeekRadius = UseSphericalWorld ? Mathf.Pi * _arena.PlanetRadius : 46
        });

        AddChild(new CollectorDrone
        {
            Name = "EnemyDrone",
            UsePlayerAntimatter = false,
            Position = AboveTerrain(UseSphericalWorld ? 130 : 16, -8, 5),
            MaxSeekRadius = UseSphericalWorld ? Mathf.Pi * _arena.PlanetRadius : 46
        });
    }

    private void CreateAntimatterPickups()
    {
        if (UseSphericalWorld)
        {
            foreach (WorldLandmark landmark in _landmarks)
            {
                if (landmark.SiteId is not ("ridge" or "wellfield" or "shore")) continue;
                for (int i = 0; i < 4; i++)
                {
                    float angle = Mathf.Tau * i / 4;
                    Vector3 position = landmark.GlobalPosition + landmark.GlobalBasis * new Vector3(Mathf.Cos(angle) * 12, 0, Mathf.Sin(angle) * 12);
                    var orb = new AntimatterPickup { Name = "SiteAntimatter", AntimatterAmount = 12, Ownership = AntimatterOwnership.Neutral };
                    orb.Position = _arena.OnSurface(position, orb.Radius);
                    AddChild(orb);
                }
            }
            return;
        }
        Vector3[] wellClusters =
        {
            AboveTerrain(-10.0f, -3.0f, 4.0f),
            AboveTerrain(9.0f, -13.0f, 5.0f),
            AboveTerrain(13.0f, 7.0f, 4.0f),
            AboveTerrain(-18.0f, 6.0f, 6.0f)
        };

        for (int cluster = 0; cluster < wellClusters.Length; cluster++)
        {
            int orbCount = 3 + cluster % 2;
            for (int i = 0; i < orbCount; i++)
            {
                float angle = Mathf.Tau * i / orbCount + GD.Randf() * 0.6f;
                float radius = 3.0f + GD.Randf() * 4.0f;
                Vector3 up = _arena.UpAt(wellClusters[cluster]);
                Basis frame = PlanetGeometry.Frame(up, Vector3.Forward);
                Vector3 offset = frame * new Vector3(Mathf.Cos(angle) * radius, GD.Randf() * 2.0f, Mathf.Sin(angle) * radius);
                AddChild(new AntimatterPickup
                {
                    Name = "LooseAntimatter",
                    AntimatterAmount = 8,
                    Ownership = AntimatterOwnership.Neutral,
                    Position = wellClusters[cluster] + offset
                });
            }
        }

        AddChild(new AntimatterPickup
        {
            Name = "NeutralBeacon",
            AntimatterAmount = 30,
            Ownership = AntimatterOwnership.Neutral,
            Position = AboveTerrain(2.0f, 10.0f, 7.0f)
        });
    }

    private void CreateEnemyEcosystem()
    {
        AddChild(new EnemyEcosystemDirector { Name = "EnemyEcosystemDirector", SpawnRivalShips = false });
    }

    private void CreateEquipmentDiscoveries()
    {
        if (UseSphericalWorld)
        {
            var sites = PlanetLayout.Sites(_arena);
            for (int i = 0; i < sites.Length; i++)
            {
                WorldLandmark landmark = _landmarks[i];
                AddChild(new EquipmentDiscovery
                {
                    Name = $"EquipmentDiscovery{sites[i].EquipmentName.Replace(" ", "")}",
                    EquipmentId = sites[i].EquipmentId, EquipmentName = sites[i].EquipmentName,
                    Position = _arena.OnSurface(landmark.GlobalPosition + landmark.GlobalBasis * Vector3.Right * 15, 6)
                });
            }
            return;
        }
        (string Id, string Title, float X, float Z)[] discoveries =
        {
            ("plasma-bolt", "Plasma Bolt", -42, -20),
            ("pulse-burst", "Pulse Burst", 0, -45),
            ("energy-shield", "Energy Shield", -40, 32),
            ("dash", "Dash", 34, 34),
            ("guardian-drone", "Guardian Drone", 45, -30)
        };
        foreach (var discovery in discoveries)
        {
            AddChild(new EquipmentDiscovery
            {
                Name = $"EquipmentDiscovery{discovery.Title.Replace(" ", "")}",
                EquipmentId = discovery.Id,
                EquipmentName = discovery.Title,
                Position = AboveTerrain(discovery.X * (UseSphericalWorld ? 3 : 1), discovery.Z * (UseSphericalWorld ? 3 : 1), 6)
            });
        }
    }

    private void CreateLevelObjective()
    {
        var rival = new EnemyEcosystemEnemy
        {
            Name = "RivalPilot",
            Role = EnemyEcosystemEnemy.EnemyRole.RivalShip,
            Position = AboveTerrain(UseSphericalWorld ? 130 : 17, -17, 7)
        };
        AddChild(rival);
        var objective = new LevelObjective { Name = "LevelObjective" };
        objective.Initialize(_player, _playerBase, _enemyBase, rival);
        objective.Finished += () =>
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            GetTree().Paused = true;
        };
        AddChild(objective);
    }

    private void CreateLandmarks()
    {
        foreach (var site in PlanetLayout.Sites(_arena))
        {
            var landmark = new WorldLandmark
            {
                Name = $"Landmark{site.Id}", SiteId = site.Id, Title = site.Title, Description = site.Description,
                Habitat = site.Habitat, LandmarkColor = site.Color,
                Position = _arena.PlanetCenter + site.Up * _arena.PlanetRadius
            };
            AddChild(landmark);
            _landmarks.Add(landmark);
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
        _arena.Orient(pillar);

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
        return _arena.FromMap(x, z, clearance);
    }
}

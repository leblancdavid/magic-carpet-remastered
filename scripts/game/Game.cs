using Godot;
using System.Collections.Generic;
using MagicCarpetRemastered.Scripts.Audio;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Spells;
using MagicCarpetRemastered.Scripts.Ui;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Game;

public partial class Game : Node3D
{
    [Export] public bool UseSphericalWorld { get; set; }
    private HeightmapArena _arena = null!;
    private CastleKeep _playerCastle = null!;
    private CastleKeep _enemyCastle = null!;
    private CarpetFlightController _player = null!;
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
        SpellImpactPool.Prepare(this);
        AddChild(new GameAudio { Name = "GameAudio" });
        CreateArena();
        if (UseSphericalWorld) CreateLandmarks();
        CreateManaWell();
        CreateCastles();
        CreatePlayer();
        CreateBalloons();
        CreateManaPickups();
        CreateSpellDiscoveries();
        CreateEnemyEcosystem();
        CreateLevelObjective();
        AddChild(new WorldManaTracker { Name = "WorldManaTracker" });
        AddChild(new Hud { Name = "Hud" });
        if (UseSphericalWorld) AddChild(new PlanetSettings { Name = "PlanetSettings" });
        if (UseSphericalWorld)
        {
            var navigation = new WorldNavigation { Name = "WorldNavigation" };
            AddChild(navigation);
            AddChild(new TacticalMap { Name = "TacticalMap", Navigation = navigation });
        }
        _arena.RefreshPlanetDetail();
        ProcessMode = ProcessModeEnum.Pausable;
        foreach (Node child in GetChildren())
        {
            child.ProcessMode = child is Hud or PlanetSettings or TacticalMap ? ProcessModeEnum.Always : ProcessModeEnum.Pausable;
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
        _player = new CarpetFlightController
        {
            Name = "PlayerCarpet",
            Position = AboveTerrain(UseSphericalWorld ? -130 : 0, 14, 9)
        };
        AddChild(_player);
    }

    private void CreateManaWell()
    {
        var well = new ManaWell
        {
            Name = "ManaWell",
            Position = AboveTerrain(3.0f, 3.0f, 1.5f)
        };
        AddChild(well);
    }

    private void CreateCastles()
    {
        _playerCastle = new CastleKeep
        {
            Name = "PlayerCastle",
            IsPlayerCastle = true,
            Position = AboveTerrain(UseSphericalWorld ? -145 : -22, 8, 2)
        };
        AddChild(_playerCastle);

        _enemyCastle = new CastleKeep
        {
            Name = "EnemyCastle",
            IsPlayerCastle = false,
            Position = AboveTerrain(UseSphericalWorld ? 145 : 22, -12, 2),
            InitialStoredMana = 48
        };
        AddChild(_enemyCastle);
    }

    private void CreateBalloons()
    {
        AddChild(new ManaBalloon
        {
            Name = "PlayerBalloon",
            UsePlayerMana = true,
            Position = AboveTerrain(UseSphericalWorld ? -130 : -16, 4, 5),
            MaxSeekRadius = UseSphericalWorld ? Mathf.Pi * _arena.PlanetRadius : 46
        });

        AddChild(new ManaBalloon
        {
            Name = "EnemyBalloon",
            UsePlayerMana = false,
            Position = AboveTerrain(UseSphericalWorld ? 130 : 16, -8, 5),
            MaxSeekRadius = UseSphericalWorld ? Mathf.Pi * _arena.PlanetRadius : 46
        });
    }

    private void CreateManaPickups()
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
                    var orb = new ManaPickup { Name = "SiteMana", ManaAmount = 12, Ownership = ManaOwnership.Neutral };
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
                AddChild(new ManaPickup
                {
                    Name = "LooseMana",
                    ManaAmount = 8,
                    Ownership = ManaOwnership.Neutral,
                    Position = wellClusters[cluster] + offset
                });
            }
        }

        AddChild(new ManaPickup
        {
            Name = "NeutralBeacon",
            ManaAmount = 30,
            Ownership = ManaOwnership.Neutral,
            Position = AboveTerrain(2.0f, 10.0f, 7.0f)
        });
    }

    private void CreateEnemyEcosystem()
    {
        AddChild(new EnemyEcosystemDirector { Name = "EnemyEcosystemDirector", SpawnEnemyWizards = false });
    }

    private void CreateSpellDiscoveries()
    {
        if (UseSphericalWorld)
        {
            var sites = PlanetLayout.Sites(_arena);
            for (int i = 0; i < sites.Length; i++)
            {
                WorldLandmark landmark = _landmarks[i];
                AddChild(new SpellDiscovery
                {
                    Name = $"SpellDiscovery{sites[i].SpellName.Replace(" ", "")}",
                    SpellId = sites[i].SpellId, SpellName = sites[i].SpellName,
                    Position = _arena.OnSurface(landmark.GlobalPosition + landmark.GlobalBasis * Vector3.Right * 15, 6)
                });
            }
            return;
        }
        (string Id, string Title, float X, float Z)[] discoveries =
        {
            ("firebolt", "Firebolt", -42, -20),
            ("arcane-burst", "Arcane Burst", 0, -45),
            ("mana-shield", "Mana Shield", -40, 32),
            ("wind-dash", "Wind Dash", 34, 34),
            ("guardian", "Guardian", 45, -30)
        };
        foreach (var discovery in discoveries)
        {
            AddChild(new SpellDiscovery
            {
                Name = $"SpellDiscovery{discovery.Title.Replace(" ", "")}",
                SpellId = discovery.Id,
                SpellName = discovery.Title,
                Position = AboveTerrain(discovery.X * (UseSphericalWorld ? 3 : 1), discovery.Z * (UseSphericalWorld ? 3 : 1), 6)
            });
        }
    }

    private void CreateLevelObjective()
    {
        var rival = new EnemyEcosystemEnemy
        {
            Name = "RivalWizard",
            Role = EnemyEcosystemEnemy.EnemyRole.EnemyWizard,
            Position = AboveTerrain(UseSphericalWorld ? 130 : 17, -17, 7)
        };
        AddChild(rival);
        var objective = new LevelObjective { Name = "LevelObjective" };
        objective.Initialize(_player, _playerCastle, _enemyCastle, rival);
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

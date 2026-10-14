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
    [Export] public int LayoutSeed { get; set; } = 7319;
    [Export] public bool UseSessionWorldSeed { get; set; } = true;
    public int ActiveWorldSeed { get; private set; }
    public bool GenerationSucceeded { get; private set; }
    [Export] public float MinimumFactionStartAngle { get; set; } = PlanetLayout.DefaultMinimumStartAngle;
    public PlanetLayout? Layout { get; private set; }
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
        ActiveWorldSeed = UseSessionWorldSeed ? WorldSeeds.Current : LayoutSeed;
        CreateLighting();
        EquipmentImpactPool.Prepare(this);
        AddChild(new GameAudio { Name = "GameAudio" });
        CreateArena();
        if (UseSphericalWorld && Layout == null)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            AddChild(new PlanetSettings { Name = "PlanetSettings" });
            var layer = new CanvasLayer(); AddChild(layer);
            layer.AddChild(new Label { Text = $"World {ActiveWorldSeed} could not meet generation requirements.\nF2: choose a fresh world or another seed.\n{_arena.GenerationError}", Position = new Vector2(24, 100) });
            return;
        }
        GenerationSucceeded = true;
        if (UseSphericalWorld) CreateLandmarks();
        CreateAntimatterSource();
        CreateBases();
        CreatePlayer();
        CreateDrones();
        CreateAntimatterPickups();
        AddChild(new ExplorationDirector { Name = "ExplorationDirector", UseFixedSeed = UseSphericalWorld,
            GenerationSeed = WorldSeeds.Derive(ActiveWorldSeed, 3) });
        CreateStrategicPoints();
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
        AddChild(new BoonChoices { Name = "BoonChoices" });
        _arena.CompleteInitialPlanetDetail();
        ProcessMode = ProcessModeEnum.Pausable;
        foreach (Node child in GetChildren())
        {
            child.ProcessMode = child is Hud or PlanetSettings or TacticalMap or BaseManagement or BoonChoices ? ProcessModeEnum.Always : ProcessModeEnum.Pausable;
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
        _arena = new HeightmapArena { Name = "HeightmapArena", IsSpherical = UseSphericalWorld, PlanetRadius = PlanetSizes.SelectedRadius,
            GenerateSeededLayout = UseSphericalWorld, WorldSeed = ActiveWorldSeed, MinimumStartAngle = MinimumFactionStartAngle };
        AddChild(_arena);

        if (UseSphericalWorld)
        {
            Layout = _arena.GeneratedLayout;
            if (Layout == null) return;
            foreach (Vector3 up in Layout.Scenery) AddPillar(PlanetLayout.Position(_arena, up, 1.5f));
            return;
        }

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
            Position = UseSphericalWorld ? BesideBase(_playerBase, new Vector3(12, 0, 4), 6) : AboveTerrain(0, 14, 9)
        };
        AddChild(_player);
    }

    private void CreateAntimatterSource()
    {
        var well = new AntimatterSource
        {
            Name = "AntimatterSource",
            Position = UseSphericalWorld ? PlanetLayout.Position(_arena, Layout!.Landmarks[1].Up, 1.5f) : AboveTerrain(3.0f, 3.0f, 1.5f)
        };
        AddChild(well);
    }

    private void CreateBases()
    {
        _playerBase = new FactionBase
        {
            Name = "PlayerBase",
            IsPlayerBase = true,
            Position = UseSphericalWorld ? PlanetLayout.Position(_arena, Layout!.Starts[0], 2) : AboveTerrain(-22, 8, 2)
        };
        AddChild(_playerBase);

        _enemyBase = new FactionBase
        {
            Name = "EnemyBase",
            IsPlayerBase = false,
            Position = UseSphericalWorld ? PlanetLayout.Position(_arena, Layout!.Starts[1], 2) : AboveTerrain(22, -12, 2),
            InitialStoredAntimatter = 30
        };
        AddChild(_enemyBase);
    }

    private void CreateDrones()
    {
        AddChild(new HomeCollectorFleet { Name = "PlayerCollectorFleet", Home = _playerBase });
        AddChild(new HomeCollectorFleet { Name = "EnemyCollectorFleet", Home = _enemyBase });
    }

    private void CreateAntimatterPickups()
    {
        if (UseSphericalWorld)
        {
            foreach (Vector3 field in Layout!.AntimatterFields)
            {
                Vector3 center = PlanetLayout.Position(_arena, field);
                Basis frame = PlanetGeometry.Frame(field, Mathf.Abs(field.Z) < 0.9f ? Vector3.Forward : Vector3.Right);
                for (int i = 0; i < 4; i++)
                {
                    float angle = Mathf.Tau * i / 4;
                    Vector3 position = center + frame * new Vector3(Mathf.Cos(angle) * 12, 0, Mathf.Sin(angle) * 12);
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
        AddChild(new EnemyEcosystemDirector { Name = "EnemyEcosystemDirector", UseFixedSeed = UseSphericalWorld,
            GenerationSeed = WorldSeeds.Derive(ActiveWorldSeed, 4) });
    }

    private void CreateLevelObjective()
    {
        var rival = new ShipFlightController
        {
            Name = "RivalPilot",
            IsPlayerControlled = false,
            Position = UseSphericalWorld ? BesideBase(_enemyBase, new Vector3(12, 0, 4), 6) : AboveTerrain(17, -17, 7)
        };
        AddChild(rival);
        rival.AddChild(new RivalPilotController { Name = "RivalAI" });
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
        foreach (var site in Layout!.Landmarks)
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

    private void CreateStrategicPoints()
    {
        if (UseSphericalWorld)
        {
            int siteIndex = 0;
            foreach (var site in Layout!.EconomicSites)
                AddChild(new StrategicPoint { Name = $"StrategicPoint{site.Side}_{siteIndex++}", Role = site.Role,
                    Position = PlanetLayout.Position(_arena, site.Up), CaptureRadius = Mathf.Clamp(_arena.PlanetRadius * 0.06f, 8, 18) });
            return;
        }
        // Mirrored surface directions keep opening opportunities comparable for both bases.
        for (int side = -1; side <= 1; side += 2)
        for (int role = 0; role < 4; role++)
        {
            Vector3 position = UseSphericalWorld
                ? _arena.PlanetCenter + new Vector3(side * 0.32f, 1, role == 3 ? -0.95f : role == 0 ? -0.45f : role * 0.45f).Normalized() * _arena.PlanetRadius
                : _arena.FromMap(side * 48, role == 3 ? -65 : (role - 1) * 40, 0);
            AddChild(new StrategicPoint { Name = $"StrategicPoint{side}_{role}", Role = role == 3 ? StrategicPointRole.Extractor : (StrategicPointRole)role,
                Position = position, CaptureRadius = UseSphericalWorld ? 18 : 12 });
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
        pillar.AddToGroup("scenery");
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

    private Vector3 BesideBase(FactionBase home, Vector3 offset, float clearance)
        => _arena.OnSurface(home.GlobalPosition + home.GlobalBasis * offset, clearance);
}

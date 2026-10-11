using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Game;

public partial class PlanetFlightTest : Node3D
{
    [Export] public float PlanetRadius { get; set; } = 800;
    [Export] public float FlightSpeed { get; set; } = 24;
    [Export] public float FlightClearance { get; set; } = 6;
    private Node3D _world = null!;
    private PlanetFlightController _player = null!;
    private Label _readout = null!;
    private SpinBox _altitude = null!;
    private SpinBox _radiusSetting = null!;
    private Vector3 _rivalPosition;

    public override void _Ready()
    {
        AddChild(new WorldEnvironment { Environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.12f, 0.2f, 0.32f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = Colors.White,
            AmbientLightEnergy = 0.8f
        }});
        AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-35, -25, 0), LightEnergy = 1.2f });
        _player = new PlanetFlightController { Radius = PlanetRadius, MoveSpeed = FlightSpeed, TargetClearance = FlightClearance };
        AddChild(_player);
        BuildWorld();
        BuildUi();
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void BuildWorld()
    {
        if (_world != null) { RemoveChild(_world); _world.QueueFree(); }
        _world = new Node3D { Name = "Planet" };
        AddChild(_world);
        _world.AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = PlanetRadius, Height = PlanetRadius * 2, RadialSegments = 512, Rings = 256 },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.22f, 0.4f, 0.27f), Roughness = 1 }
        });
        // Two crossing great circles expose hemisphere and pole transitions.
        int count = Mathf.CeilToInt(Mathf.Tau * PlanetRadius / 100);
        for (int ring = 0; ring < 2; ring++)
        for (int i = 0; i < count; i++)
        {
            float angle = (i + 0.5f) * Mathf.Tau / count;
            Vector3 up = ring == 0 ? new Vector3(0, Mathf.Cos(angle), -Mathf.Sin(angle)) : new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0);
            Vector3 forward = ring == 0 ? new Vector3(0, -Mathf.Sin(angle), -Mathf.Cos(angle)) : new Vector3(Mathf.Cos(angle), -Mathf.Sin(angle), 0);
            Node3D marker = new Node3D { Transform = new Transform3D(PlanetGeometry.Frame(up, forward), up * PlanetRadius) };
            _world.AddChild(marker);
            AddBox(marker, new Vector3(1, 6, 1), new Vector3(0, 3, 0), ring == 0 ? Colors.Gold : Colors.Cyan);
            marker.AddChild(new Label3D { Text = $"{(i + 0.5f) * Mathf.Tau * PlanetRadius / count:0} m", Position = new Vector3(0, 8, 0), FontSize = 48, PixelSize = 0.02f, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled });
        }
        CreateBase(0, Colors.DodgerBlue, "HOME");
        _rivalPosition = CreateBase(300 / PlanetRadius, Colors.IndianRed, "RIVAL — 300 m from home");
        _player.Radius = PlanetRadius;
        _player.ResetFlight();
    }

    private Vector3 CreateBase(float angle, Color color, string text)
    {
        Vector3 up = new Vector3(0, Mathf.Cos(angle), -Mathf.Sin(angle));
        Vector3 forward = new Vector3(0, -Mathf.Sin(angle), -Mathf.Cos(angle));
        // Offset from the travel line so flight markers stay unobstructed.
        up = up.Rotated(forward, 18 / PlanetRadius);
        Node3D hub = new Node3D { Transform = new Transform3D(PlanetGeometry.Frame(up, forward), up * PlanetRadius) };
        _world.AddChild(hub);
        AddBox(hub, new Vector3(10, 8, 10), new Vector3(0, 4, 0), color);
        foreach (float x in new[] { -5f, 5f })
        foreach (float z in new[] { -5f, 5f })
            AddBox(hub, new Vector3(3, 12, 3), new Vector3(x, 6, z), color.Lightened(0.15f));
        hub.AddChild(new Label3D { Text = text, Position = new Vector3(0, 15, 0), FontSize = 48, PixelSize = 0.025f, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled });
        return hub.Position;
    }

    private static void AddBox(Node parent, Vector3 size, Vector3 position, Color color)
        => parent.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, Position = position,
            MaterialOverride = new StandardMaterial3D { AlbedoColor = color } });

    private void BuildUi()
    {
        CanvasLayer layer = new CanvasLayer();
        AddChild(layer);
        PanelContainer panel = new PanelContainer { Position = new Vector2(16, 16) };
        layer.AddChild(panel);
        VBoxContainer column = new VBoxContainer();
        panel.AddChild(column);
        column.AddChild(new Label { Text = "SPHERICAL FLIGHT LAB" });
        column.AddChild(new Label { Text = "WASD flight · Space/C altitude · Mouse look · V camera\nF dash (46 m/s) · R reset · Esc mouse/settings · F6 arena" });
        _radiusSetting = AddSetting(column, "Radius (m)", 500, 4000, 100, PlanetRadius, value => { PlanetRadius = (float)value; BuildWorld(); });
        AddSetting(column, "Flight speed (m/s)", 8, 60, 1, FlightSpeed, value => _player.MoveSpeed = FlightSpeed = (float)value);
        _altitude = AddSetting(column, "Clearance (m)", 2, 12, 0.5, FlightClearance, value => _player.TargetClearance = (float)value);
        HBoxContainer presets = new HBoxContainer();
        column.AddChild(presets);
        for (int i = 0; i < PlanetSizes.Radii.Length; i++)
        {
            int radius = PlanetSizes.Radii[i];
            Button button = new Button { Text = $"{PlanetSizes.Names[i]} {radius} m" };
            presets.AddChild(button);
            button.Pressed += () => _radiusSetting.Value = radius;
        }
        _readout = new Label();
        column.AddChild(_readout);
    }

    private static SpinBox AddSetting(Node parent, string title, double min, double max, double step, double value, System.Action<double> changed)
    {
        HBoxContainer row = new HBoxContainer();
        parent.AddChild(row);
        row.AddChild(new Label { Text = title, CustomMinimumSize = new Vector2(160, 0) });
        SpinBox spin = new SpinBox { MinValue = min, MaxValue = max, Step = step, Value = value, CustomMinimumSize = new Vector2(120, 0) };
        row.AddChild(spin);
        spin.ValueChanged += value => changed(value);
        return spin;
    }

    public override void _Process(double delta)
    {
        _altitude.SetValueNoSignal(_player.TargetClearance);
        float distance = PlanetGeometry.SurfaceDistance(_player.GlobalPosition, _rivalPosition, Vector3.Zero, PlanetRadius);
        _readout.Text = $"Radius {PlanetRadius:0} m · {(_player.FirstPerson ? "First person" : "Chase")}\nSpeed {_player.Speed:0.0} m/s · Clearance {_player.Clearance:0.0} m\nUp rotation {_player.UpDegreesPerSecond:0.00}°/s · Gravity {_player.Gravity.Length():0.0} m/s²\nSurface travel {_player.SurfaceTravel:0} m · Rival distance {distance:0} m\nLap at cruise: {Mathf.Tau * PlanetRadius / _player.MoveSpeed / 60:0.0} min";
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Escape) Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
            if (key.Keycode == Key.R) _player.ResetFlight();
            if (key.Keycode == Key.F6) GetTree().ChangeSceneToFile("res://scenes/PlanetGame.tscn");
        }
    }
}

using Godot;
using MagicCarpetRemastered.Scripts.World;
using MagicCarpetRemastered.Scripts.Ui;

namespace MagicCarpetRemastered.Scripts.Game;

public partial class PlanetSettings : CanvasLayer
{
    private PanelContainer _panel = null!;
    private bool _wasPaused;
    private Input.MouseModeEnum _previousMouse;
    private OptionButton _size = null!;
    private LineEdit _seed = null!;
    private Label _seedStatus = null!;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 5;
        Control root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _panel = new PanelContainer { Visible = false };
        root.AddChild(_panel);
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _panel.Position += new Vector2(-220, -170);
        _panel.CustomMinimumSize = new Vector2(440, 340);
        VBoxContainer column = new();
        _panel.AddChild(column);
        column.AddChild(new Label { Text = "NEW WORLD", HorizontalAlignment = HorizontalAlignment.Center });
        _size = new OptionButton();
        for (int i = 0; i < PlanetSizes.Radii.Length; i++) _size.AddItem($"{PlanetSizes.Names[i]} — {PlanetSizes.Radii[i]} m radius");
        _size.Select(PlanetSizes.SelectedIndex);
        column.AddChild(_size);
        int current = GetParent() is Game game ? game.ActiveWorldSeed : WorldSeeds.Current;
        _seedStatus = new Label { Text = $"Current seed: {current}\nF4 restarts this world", HorizontalAlignment = HorizontalAlignment.Center };
        column.AddChild(_seedStatus);
        _seed = new LineEdit { Name = "WorldSeed", PlaceholderText = "Optional seed to replay (blank = fresh random world)", MaxLength = 11 };
        column.AddChild(_seed);
        column.AddChild(new Label { Text = "Starting a planet resets the match and terrain edits.\nSame seed and size reproduce the geography/layout.", HorizontalAlignment = HorizontalAlignment.Center });
        Button restart = new() { Name = "StartNewPlanet", Text = "Start new planet" };
        column.AddChild(restart);
        restart.Pressed += () =>
        {
            if (string.IsNullOrWhiteSpace(_seed.Text)) StartNewPlanet();
            else if (int.TryParse(_seed.Text.Trim(), out int seed)) StartNewPlanet(seed);
            else _seedStatus.Text = "Enter a signed 32-bit whole-number seed,\nor leave blank for a fresh world.";
        };
        Button resume = new() { Text = "Resume (F2 / Esc)" };
        column.AddChild(resume);
        resume.Pressed += Toggle;
    }

    public void StartNewPlanet(int? seed = null)
    {
        if (seed.HasValue) WorldSeeds.UseSeed(seed.Value); else WorldSeeds.NewWorld();
        PlanetSizes.SelectedIndex = _size.Selected;
        GetTree().Paused = false;
        Callable.From(() => GetTree().ReloadCurrentScene()).CallDeferred();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode == Key.F2 || (_panel.Visible && key.Keycode == Key.Escape))
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
    }

    private void Toggle()
    {
        if (!_panel.Visible)
        {
            (GetTree().GetFirstNodeInGroup("base_management") as BaseManagement)?.SetOpen(false);
            (GetTree().GetFirstNodeInGroup("tactical_map_ui") as TacticalMap)?.SetExpanded(false);
            _wasPaused = GetTree().Paused;
            _previousMouse = Input.MouseMode;
            _size.Select(PlanetSizes.SelectedIndex);
            _seed.Text = string.Empty;
            int current = GetParent() is Game game ? game.ActiveWorldSeed : WorldSeeds.Current;
            _seedStatus.Text = $"Current seed: {current}\nF4 restarts this world";
            GetTree().Paused = true;
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _panel.Show();
        }
        else
        {
            _panel.Hide();
            GetTree().Paused = _wasPaused;
            Input.MouseMode = _previousMouse;
        }
    }
}

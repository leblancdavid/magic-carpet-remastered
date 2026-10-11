using Godot;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Game;

public partial class PlanetSettings : CanvasLayer
{
    private PanelContainer _panel = null!;
    private bool _wasPaused;
    private Input.MouseModeEnum _previousMouse;
    private OptionButton _size = null!;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 5;
        Control root = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        Label hint = new() { Text = $"{PlanetSizes.Names[PlanetSizes.SelectedIndex]} planet · {PlanetSizes.SelectedRadius} m radius\nF2 planet size · F6 flight lab", HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(hint);
        hint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        hint.Position += new Vector2(-150, 20);
        hint.CustomMinimumSize = new Vector2(300, 50);
        _panel = new PanelContainer { Visible = false };
        root.AddChild(_panel);
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _panel.Position += new Vector2(-200, -125);
        _panel.CustomMinimumSize = new Vector2(400, 250);
        VBoxContainer column = new();
        _panel.AddChild(column);
        column.AddChild(new Label { Text = "PLANET SIZE", HorizontalAlignment = HorizontalAlignment.Center });
        _size = new OptionButton();
        for (int i = 0; i < PlanetSizes.Radii.Length; i++) _size.AddItem($"{PlanetSizes.Names[i]} — {PlanetSizes.Radii[i]} m radius");
        _size.Select(PlanetSizes.SelectedIndex);
        column.AddChild(_size);
        column.AddChild(new Label { Text = "Starting a planet resets the match, antimatter,\nbases, terrain edits, and equipment discoveries.", HorizontalAlignment = HorizontalAlignment.Center });
        Button restart = new() { Text = "Start new planet" };
        column.AddChild(restart);
        restart.Pressed += () =>
        {
            PlanetSizes.SelectedIndex = _size.Selected;
            GetTree().Paused = false;
            Callable.From(() => GetTree().ReloadCurrentScene()).CallDeferred();
        };
        Button resume = new() { Text = "Resume (F2 / Esc)" };
        column.AddChild(resume);
        resume.Pressed += Toggle;
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
            _wasPaused = GetTree().Paused;
            _previousMouse = Input.MouseMode;
            _size.Select(PlanetSizes.SelectedIndex);
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

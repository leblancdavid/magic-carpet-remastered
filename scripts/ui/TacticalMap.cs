using Godot;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Ui;

public partial class TacticalMap : CanvasLayer
{
    public WorldNavigation Navigation { get; set; } = null!;
    public bool IsExpanded => _canvas.Expanded;
    private TacticalMapCanvas _canvas = null!;
    private Input.MouseModeEnum _previousMouse;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 6;
        _canvas = new TacticalMapCanvas { Name = "MapCanvas", Navigation = Navigation, MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_canvas);
        _canvas.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    public bool SetExpanded(bool expanded)
    {
        if (expanded == IsExpanded) return true;
        if (expanded && GetTree().Paused) return false; // Another menu/result owns pause.
        _canvas.Expanded = expanded;
        _canvas.MouseFilter = expanded ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
        if (expanded)
        {
            Navigation.Refresh();
            _previousMouse = Input.MouseMode;
            GetTree().Paused = true;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else
        {
            GetTree().Paused = false;
            Input.MouseMode = _previousMouse;
        }
        _canvas.QueueRedraw();
        return true;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey key) return;
        if (IsExpanded)
        {
            if (key.Pressed && !key.Echo)
            {
                if (key.Keycode is Key.M or Key.Escape) SetExpanded(false);
                else if (key.Keycode == Key.Delete) Navigation.ClearWaypoint();
                else if (key.Keycode == Key.R)
                {
                    SetExpanded(false);
                    Callable.From(() => GetTree().ReloadCurrentScene()).CallDeferred();
                }
            }
            GetViewport().SetInputAsHandled();
        }
        else if (key is { Pressed: true, Echo: false, Keycode: Key.M } && SetExpanded(true)) GetViewport().SetInputAsHandled();
    }

    public override void _ExitTree()
    {
        if (_canvas != null && IsExpanded)
        {
            GetTree().Paused = false;
            Input.MouseMode = _previousMouse;
        }
    }
}

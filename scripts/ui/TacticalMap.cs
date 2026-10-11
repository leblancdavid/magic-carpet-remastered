using Godot;
using MagicCarpetRemastered.Scripts.World;
using MagicCarpetRemastered.Scripts.Player;

namespace MagicCarpetRemastered.Scripts.Ui;

public partial class TacticalMap : CanvasLayer
{
    public WorldNavigation Navigation { get; set; } = null!;
    public bool IsExpanded => _canvas.Expanded;
    private TacticalMapCanvas _canvas = null!;
    private Input.MouseModeEnum _previousMouse;
    private ShipFlightController? _player;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        AddToGroup("tactical_map_ui");
        _player = GetTree().GetFirstNodeInGroup("player") as ShipFlightController;
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
            (GetTree().GetFirstNodeInGroup("boon_choices") as BoonChoices)?.SetOpen(false);
            (GetTree().GetFirstNodeInGroup("base_management") as BaseManagement)?.SetOpen(false);
            Navigation.Refresh();
            _previousMouse = Input.MouseMode;
            if (_player != null) _player.GameplayInputBlocked = true;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else
        {
            if (_player != null) _player.GameplayInputBlocked = false;
            if (!GetTree().Paused) Input.MouseMode = _previousMouse;
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
                else if (key.Keycode is Key.H or Key.B) { SetExpanded(false); return; }
                else if (key.Keycode == Key.Delete) Navigation.ClearWaypoint();
                else if (key.Keycode == Key.R)
                {
                    SetExpanded(false);
                    GetTree().Paused = false;
                    Callable.From(() => GetTree().ReloadCurrentScene()).CallDeferred();
                }
            }
            GetViewport().SetInputAsHandled();
        }
        else if (key is { Pressed: true, Echo: false, Keycode: Key.M } && SetExpanded(true)) GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (IsExpanded && GetTree().Paused) SetExpanded(false);
    }

    public override void _ExitTree()
    {
        if (_canvas != null && IsExpanded)
        {
            if (IsInstanceValid(_player)) _player!.GameplayInputBlocked = false;
            if (!GetTree().Paused) Input.MouseMode = _previousMouse;
        }
    }
}

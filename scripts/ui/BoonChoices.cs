using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Ui;

public partial class BoonChoices : CanvasLayer
{
    public bool IsOpen => _overlay.Visible;
    private ShipFlightController _player = null!;
    private FactionBase _home = null!;
    private Control _overlay = null!;
    private Label _title = null!;
    private readonly Button[] _choices = new Button[3];
    private Button _skip = null!;
    private Button _confirm = null!;
    private int _selection = -1;
    private long _offerId;
    private int _seenCount;
    private float _refreshTimer;
    private Input.MouseModeEnum _previousMouse;

    public override void _Ready()
    {
        AddToGroup("boon_choices"); ProcessMode = ProcessModeEnum.Always; Layer = 8;
        _player = (ShipFlightController)GetTree().GetFirstNodeInGroup("player");
        _home = (FactionBase)GetTree().GetFirstNodeInGroup("player_base");
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore }; AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _overlay = new Control { Name = "BoonOverlay", Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
        root.AddChild(_overlay); _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color(0.015f, 0.025f, 0.05f, 0.85f), MouseFilter = Control.MouseFilterEnum.Ignore };
        _overlay.AddChild(shade); shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var panel = new PanelContainer { Name = "BoonPanel" }; _overlay.AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        panel.OffsetLeft = -290; panel.OffsetRight = 290; panel.OffsetTop = -185; panel.OffsetBottom = 185;
        using var frame = new StyleBoxFlat { BgColor = new Color(0.035f, 0.055f, 0.09f),
            ContentMarginLeft = 20, ContentMarginRight = 20, ContentMarginTop = 18, ContentMarginBottom = 18 };
        panel.AddThemeStyleboxOverride("panel", frame);
        var column = new VBoxContainer(); panel.AddChild(column);
        column.AddThemeConstantOverride("separation", 10);
        _title = new Label(); column.AddChild(_title); _title.AddThemeFontSizeOverride("font_size", 22);
        column.AddChild(new Label { Text = "Select a bonus, then confirm. H / Esc to defer." });
        using var selected = UiTile.Frame(new Color(0.07f, 0.17f, 0.22f), new Color(0.5f, 0.94f, 1), 2);
        for (int i = 0; i < 3; i++)
        {
            int option = i;
            _choices[i] = new Button { Name = $"BoonChoice_{i}", ToggleMode = true, CustomMinimumSize = new Vector2(0, 64), FocusMode = Control.FocusModeEnum.None };
            _choices[i].AddThemeStyleboxOverride("pressed", selected);
            _choices[i].AddThemeStyleboxOverride("hover_pressed", selected);
            column.AddChild(_choices[i]); _choices[i].Pressed += () => Select(option);
        }
        var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 12); column.AddChild(row);
        _skip = new Button { Name = "SkipBoon", ToggleMode = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, FocusMode = Control.FocusModeEnum.None }; row.AddChild(_skip);
        _skip.AddThemeStyleboxOverride("pressed", selected); _skip.AddThemeStyleboxOverride("hover_pressed", selected);
        _skip.Pressed += () => Select(3);
        _confirm = new Button { Name = "ConfirmBoon", Text = "Confirm", CustomMinimumSize = new Vector2(150, 38), FocusMode = Control.FocusModeEnum.None }; row.AddChild(_confirm);
        _confirm.Pressed += () => Confirm(); Refresh();
    }

    public bool SetOpen(bool open)
    {
        if (open == IsOpen) return true;
        if (open && (GetTree().Paused || _home.IsDestroyed || _player.Health <= 0 || _player.Boons.Pending.Count == 0)) return false;
        if (open)
        {
            (GetTree().GetFirstNodeInGroup("base_management") as BaseManagement)?.SetOpen(false);
            (GetTree().GetFirstNodeInGroup("tactical_map_ui") as TacticalMap)?.SetExpanded(false);
            _previousMouse = Input.MouseMode; _player.GameplayInputBlocked = true; Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else
        {
            _player.GameplayInputBlocked = false;
            if (!GetTree().Paused) Input.MouseMode = _previousMouse;
        }
        _selection = -1;
        _overlay.Visible = open; Refresh(); return true;
    }
    public bool Select(int option)
    {
        if (!IsOpen || option < 0 || option > 3 || GetTree().Paused || _player.Health <= 0 || _home.IsDestroyed) return false;
        Refresh();
        if (option < 3 && _choices[option].Disabled) return false;
        _selection = option; Refresh(); return true;
    }
    public bool Confirm() => _selection == 3 ? Skip() : _selection >= 0 && Choose(_selection);
    public bool Choose(int option)
    {
        if (!IsOpen || GetTree().Paused || _player.Health <= 0 || _home.IsDestroyed || option < 0 || option >= 3 || _selection != option) return false;
        var offer = System.Linq.Enumerable.FirstOrDefault(_player.Boons.Pending, o => o.Id == _offerId);
        bool result = offer != null && _player.Boons.Choose(_offerId, offer.Choices[option].Id);
        if (result) { _seenCount = _player.Boons.Pending.Count; SetOpen(false); }
        else Refresh();
        return result;
    }
    public bool Skip()
    {
        if (!IsOpen || GetTree().Paused || _player.Health <= 0 || _home.IsDestroyed || _selection != 3) return false;
        bool result = _player.Boons.Skip(_offerId, _home);
        if (result) { _seenCount = _player.Boons.Pending.Count; SetOpen(false); }
        else Refresh();
        return result;
    }
    public void Refresh()
    {
        int count = _player.Boons.Pending.Count;
        var offer = count == 0 ? null : _player.Boons.Pending[0];
        if (_offerId != (offer?.Id ?? 0)) _selection = -1;
        _offerId = offer?.Id ?? 0;
        _title.Text = "Choose a boon";
        for (int i = 0; i < 3; i++)
        {
            _choices[i].Visible = offer != null;
            if (offer == null) continue;
            var boon = offer.Choices[i]; int stacks = _player.Boons.Stacks(boon.Id);
            string effect = boon.Id switch
            {
                "speed" => "+10% movement speed",
                "recharge" => "+10% energy recharge",
                "fire-rate" => "+10% Plasma fire rate",
                "projectile-speed" => "+10% Plasma projectile speed",
                "energy-damage" => "+10% Plasma and Guardian damage",
                "explosive-damage" => "+10% Pulse Burst damage",
                "deployable-damage" => "+10% Guardian damage",
                "ship-hp" => "+20 maximum HP",
                _ => boon.Effect
            };
            _choices[i].Text = $"{boon.Name}\n{effect}";
            _choices[i].TooltipText = $"{boon.Effect}\n{stacks}/{ExplorationBoons.MaximumStacks} stacks";
            _choices[i].Disabled = stacks >= ExplorationBoons.MaximumStacks;
            _choices[i].SetPressedNoSignal(_selection == i);
        }
        _skip.Visible = offer != null; _skip.Text = $"Reject · +{offer?.SkipAntimatter ?? 0} A";
        _skip.SetPressedNoSignal(_selection == 3);
        _confirm.Disabled = offer == null || _selection < 0 || (_selection < 3 && _choices[_selection].Disabled);
    }
    public override void _Process(double delta)
    {
        if (IsOpen && (GetTree().Paused || _home.IsDestroyed || _player.Health <= 0)) SetOpen(false);
        int count = _player.Boons.Pending.Count;
        if (IsOpen && count == 0) SetOpen(false);
        bool otherUi = (GetTree().GetFirstNodeInGroup("base_management") as BaseManagement)?.IsOpen == true
            || (GetTree().GetFirstNodeInGroup("tactical_map_ui") as TacticalMap)?.IsExpanded == true;
        if (count > _seenCount && !otherUi) SetOpen(true);
        _seenCount = count;
        _refreshTimer -= (float)delta;
        if (_refreshTimer <= 0) { _refreshTimer = 0.1f; Refresh(); }
    }
    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode == Key.H || (IsOpen && key.Keycode == Key.Escape))
        { SetOpen(!IsOpen); GetViewport().SetInputAsHandled(); return; }
        if (!IsOpen) return;
        if (key.Keycode is Key.B or Key.M or Key.F2 or Key.F6) { SetOpen(false); return; }
        if (@event.IsActionPressed("restart_arena"))
        { SetOpen(false); GetTree().Paused = false; Callable.From(() => GetTree().ReloadCurrentScene()).CallDeferred(); }
        GetViewport().SetInputAsHandled();
    }
    public override void _ExitTree()
    {
        if (_overlay != null && IsOpen && IsInstanceValid(_player))
        { _player.GameplayInputBlocked = false; if (!GetTree().Paused) Input.MouseMode = _previousMouse; }
    }
}

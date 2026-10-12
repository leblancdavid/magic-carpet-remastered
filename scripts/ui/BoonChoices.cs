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
    private Label _summary = null!;
    private readonly Button[] _choices = new Button[3];
    private Button _skip = null!;
    private Button _next = null!;
    private int _index;
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
        var panel = new PanelContainer(); _overlay.AddChild(panel); panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        panel.AnchorLeft = 0.16f; panel.AnchorRight = 0.84f; panel.AnchorTop = 0.1f; panel.AnchorBottom = 0.88f;
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.035f, 0.055f, 0.09f),
            ContentMarginLeft = 20, ContentMarginRight = 20, ContentMarginTop = 18, ContentMarginBottom = 18 });
        var column = new VBoxContainer(); panel.AddChild(column);
        _title = new Label(); column.AddChild(_title); _title.AddThemeFontSizeOverride("font_size", 22);
        column.AddChild(new Label { Text = "Choose one permanent match bonus, or take antimatter. Simulation continues." });
        for (int i = 0; i < 3; i++)
        {
            int option = i;
            _choices[i] = new Button { Name = $"BoonChoice_{i}", CustomMinimumSize = new Vector2(0, 64), FocusMode = Control.FocusModeEnum.None };
            column.AddChild(_choices[i]); _choices[i].Pressed += () => Choose(option);
        }
        _skip = new Button { Name = "SkipBoon", FocusMode = Control.FocusModeEnum.None }; column.AddChild(_skip);
        _skip.Pressed += () => Skip();
        var row = new HBoxContainer(); column.AddChild(row);
        _next = new Button { Text = "Next pending reward", FocusMode = Control.FocusModeEnum.None }; row.AddChild(_next);
        _next.Pressed += () => { _index++; Refresh(); };
        var defer = new Button { Text = "Defer / close · H / Esc", FocusMode = Control.FocusModeEnum.None }; row.AddChild(defer);
        defer.Pressed += () => SetOpen(false);
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        column.AddChild(scroll);
        _summary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(_summary); Refresh();
    }

    public bool SetOpen(bool open)
    {
        if (open == IsOpen) return true;
        if (open && (GetTree().Paused || _home.IsDestroyed || _player.Health <= 0)) return false;
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
        _overlay.Visible = open; Refresh(); return true;
    }
    public bool Choose(int option)
    {
        if (!IsOpen || GetTree().Paused || _player.Health <= 0 || _home.IsDestroyed || option < 0 || option >= 3) return false;
        var offer = System.Linq.Enumerable.FirstOrDefault(_player.Boons.Pending, o => o.Id == _offerId);
        bool result = offer != null && _player.Boons.Choose(_offerId, offer.Choices[option].Id);
        Refresh(); return result;
    }
    public bool Skip()
    {
        if (!IsOpen || GetTree().Paused || _player.Health <= 0) return false;
        bool result = _player.Boons.Skip(_offerId, _home); Refresh(); return result;
    }
    public void Refresh()
    {
        int count = _player.Boons.Pending.Count;
        _index = count == 0 ? 0 : _index % count;
        var offer = count == 0 ? null : _player.Boons.Pending[_index]; _offerId = offer?.Id ?? 0;
        _title.Text = offer == null ? "MATCH BOONS · REAL TIME" : $"SECURED SALVAGE {_index + 1}/{count} · REAL TIME";
        for (int i = 0; i < 3; i++)
        {
            _choices[i].Visible = offer != null;
            if (offer == null) continue;
            var boon = offer.Choices[i]; int stacks = _player.Boons.Stacks(boon.Id);
            _choices[i].Text = $"{boon.Name} · {stacks}/{ExplorationBoons.MaximumStacks} stacks\n{boon.Effect}";
            _choices[i].Disabled = stacks >= ExplorationBoons.MaximumStacks;
        }
        _skip.Visible = offer != null; _skip.Text = $"Skip for {offer?.SkipAntimatter ?? 0} antimatter · direct deposit / owned overflow";
        _next.Visible = count > 1;
        string summary = _player.Boons.Summary;
        _summary.Text = "COLLECTED BOONS · additive stacks within each family; 10-stack cap\n"
            + (summary.Length == 0 ? "None yet. Find wrecks, destroy them and collect the gold salvage." : summary)
            + $"\n\nEffective: speed {_player.EffectiveMoveSpeed:0.#} · boost {_player.EffectiveBoosterSpeed:0.#} · Dash {_player.EffectiveDashSpeed:0.#}"
            + $"\nRecharge {_player.EnergyRechargePerSecond:0.##}/s · Plasma {_player.PlasmaBoltDamage} damage / {_player.EffectiveFireCooldown:0.###}s"
            + $"\nShip HP {_player.Health}/{_player.MaximumHealth}. Hull acquisition does not heal; respawn refills maximum HP.";
    }
    public override void _Process(double delta)
    {
        if (IsOpen && (GetTree().Paused || _home.IsDestroyed || _player.Health <= 0)) SetOpen(false);
        int count = _player.Boons.Pending.Count;
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
        if (key.Keycode == Key.R)
        { SetOpen(false); GetTree().Paused = false; Callable.From(() => GetTree().ReloadCurrentScene()).CallDeferred(); }
        GetViewport().SetInputAsHandled();
    }
    public override void _ExitTree()
    {
        if (_overlay != null && IsOpen && IsInstanceValid(_player))
        { _player.GameplayInputBlocked = false; if (!GetTree().Paused) Input.MouseMode = _previousMouse; }
    }
}

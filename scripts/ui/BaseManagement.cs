using System.Collections.Generic;
using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Ui;

public partial class BaseManagement : CanvasLayer
{
    public bool IsOpen => _overlay.Visible;
    public string SummaryText => _summary.Text;
    public string AlertText => _alert.Text;
    private Control _overlay = null!;
    private Label _summary = null!;
    private Label _alert = null!;
    private Button _hint = null!;
    private Button _tierOrder = null!;
    private Label _tierDescription = null!;
    private readonly Label[] _slotLabels = new Label[5];
    private readonly ProgressBar[] _progress = new ProgressBar[5];
    private readonly Button[] _cancel = new Button[5];
    private readonly long[] _cancelInstanceIds = new long[5];
    private readonly Dictionary<string, (Button Button, Label Status)> _orders = new();
    private FactionBase _base = null!;
    private ShipFlightController _player = null!;
    private Input.MouseModeEnum _previousMouse;
    private float _refresh;
    private string _actionFeedback = string.Empty;

    public override void _Ready()
    {
        AddToGroup("base_management");
        ProcessMode = ProcessModeEnum.Always;
        Layer = 7;
        _base = (FactionBase)GetTree().GetFirstNodeInGroup("player_base");
        _player = (ShipFlightController)GetTree().GetFirstNodeInGroup("player");
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _hint = new Button { Name = "ManagementHint", Text = "B  Base management", FocusMode = Control.FocusModeEnum.None };
        root.AddChild(_hint);
        _hint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
        _hint.Position += new Vector2(-125, -48);
        _hint.CustomMinimumSize = new Vector2(250, 32);
        _hint.Pressed += () => SetOpen(true);
        _overlay = new Control { Name = "ManagementOverlay", Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
        root.AddChild(_overlay);
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color(0.015f, 0.025f, 0.05f, 0.8f), MouseFilter = Control.MouseFilterEnum.Ignore };
        _overlay.AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var panel = new PanelContainer { Name = "ManagementPanel" };
        _overlay.AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        panel.AnchorLeft = 0.06f; panel.AnchorRight = 0.94f; panel.AnchorTop = 0.07f; panel.AnchorBottom = 0.91f;
        var style = new StyleBoxFlat { BgColor = new Color(0.035f, 0.055f, 0.09f), BorderColor = new Color(0.24f, 0.62f, 0.76f),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            ContentMarginLeft = 18, ContentMarginRight = 18, ContentMarginTop = 14, ContentMarginBottom = 14 };
        panel.AddThemeStyleboxOverride("panel", style);
        var column = new VBoxContainer(); panel.AddChild(column);
        var header = new HBoxContainer(); column.AddChild(header);
        var title = Text("HOME BASE DEVELOPMENT  ·  REAL TIME");
        title.AddThemeFontSizeOverride("font_size", 22);
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(title);
        var close = new Button { Text = "Close · B / Esc", FocusMode = Control.FocusModeEnum.None };
        header.AddChild(close); close.Pressed += () => SetOpen(false);
        _summary = Text(""); column.AddChild(_summary);
        _alert = Text(""); _alert.AddThemeColorOverride("font_color", new Color(1, 0.7f, 0.3f)); column.AddChild(_alert);
        var slots = new GridContainer { Columns = 5 }; column.AddChild(slots);
        for (int i = 0; i < 5; i++)
        {
            var slot = (ConstructionSlot)i;
            var card = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            slots.AddChild(card);
            _slotLabels[i] = Text($"{slot}\nIdle"); card.AddChild(_slotLabels[i]);
            _progress[i] = new ProgressBar { MaxValue = 1, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 12) };
            card.AddChild(_progress[i]);
            _cancel[i] = new Button { Name = $"Cancel_{slot}", Text = "Cancel · full refund", FocusMode = Control.FocusModeEnum.None };
            card.AddChild(_cancel[i]);
            _cancel[i].Pressed += () =>
            {
                CancelProject(slot, _cancelInstanceIds[(int)slot]);
            };
        }
        var tabs = new TabContainer { Name = "ProjectTabs", SizeFlagsVertical = Control.SizeFlags.ExpandFill }; column.AddChild(tabs);
        for (int i = 0; i < 5; i++)
        {
            var slot = (ConstructionSlot)i;
            var scroll = new ScrollContainer { Name = slot.ToString(), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
            tabs.AddChild(scroll);
            var projects = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; scroll.AddChild(projects);
            if (slot == ConstructionSlot.Base)
            {
                _tierDescription = Text(""); projects.AddChild(_tierDescription);
                _tierOrder = new Button { Name = "Order_Tier", FocusMode = Control.FocusModeEnum.None };
                projects.AddChild(_tierOrder); _tierOrder.Pressed += () => OrderNextTier();
            }
            foreach (var definition in _base.Construction.Catalog)
            {
                if (definition.Slot != slot) continue;
                projects.AddChild(Text($"{definition.Name}\n{definition.Description}\n{definition.Cost} antimatter · {definition.DurationSeconds:0}s · tier {definition.RequiredTier}"));
                var row = new HBoxContainer(); projects.AddChild(row);
                var button = new Button { Name = $"Order_{definition.Id}", Text = "Start research", FocusMode = Control.FocusModeEnum.None };
                row.AddChild(button);
                var status = Text(""); status.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; row.AddChild(status);
                _orders.Add(definition.Id, (button, status));
                button.Pressed += () => OrderProject(definition.Id);
            }
            if (slot == ConstructionSlot.Ship)
                projects.AddChild(Text("Claim is built in and uses no equipment slot.\nOwned equipment research, pending installation and the four-slot loadout arrive in the next milestone."));
        }
        column.AddChild(Text("Remote orders · five independent slots · payments lower your energy limit immediately.\nCancellation refunds the payment; overflow stays owned. Losing a tier destroys every active project."));
        Refresh();
    }

    private static Label Text(string text) => new() { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart,
        MouseFilter = Control.MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(0, 24) };

    public bool SetOpen(bool open)
    {
        if (open == IsOpen) return true;
        if (open && (GetTree().Paused || _base.IsDestroyed)) return false;
        if (open)
        {
            (GetTree().GetFirstNodeInGroup("tactical_map_ui") as TacticalMap)?.SetExpanded(false);
            _previousMouse = Input.MouseMode;
            _player.GameplayInputBlocked = true;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else
        {
            _player.GameplayInputBlocked = false;
            if (!GetTree().Paused) Input.MouseMode = _previousMouse;
        }
        _overlay.Visible = open;
        _hint.Visible = !open;
        if (open) Refresh();
        return true;
    }

    public bool OrderNextTier()
    {
        if (!IsOpen || GetTree().Paused) return false;
        bool result = _base.Construction.TryStartNextTier(out string reason);
        _actionFeedback = result ? string.Empty : reason; Refresh(); return result;
    }
    public bool OrderProject(string id)
    {
        if (!IsOpen || GetTree().Paused) return false;
        bool result = _base.Construction.TryStart(id, out string reason);
        _actionFeedback = result ? string.Empty : reason; Refresh(); return result;
    }
    public bool CancelProject(ConstructionSlot slot, long instanceId)
    {
        if (!IsOpen || GetTree().Paused) return false;
        bool result = _base.Construction.Cancel(slot, instanceId);
        _actionFeedback = result ? string.Empty : "Project already finished or replaced";
        Refresh(); return result;
    }

    public void Refresh()
    {
        _summary.Text = $"Surviving tier {_base.Tier} / purchased {_base.HighestPurchasedTier} · HP {_base.Health}/{_base.MaxHealth} · "
            + $"{(_base.IsRepairing ? "Repairing" : "Repair waiting / intact")}\nAntimatter {_base.StoredAntimatter}/{_base.EffectiveStorageCapacity} "
            + $"(intact {_base.AntimatterStorageCapacity}) · Energy {_player.Energy}/{_player.EnergyCapacity} · Recharge {_player.EnergyRechargePerSecond:0.0}/s";
        _alert.Text = _base.IsDestroyed ? "HOME BASE DESTROYED"
            : _base.Tier < _base.HighestPurchasedTier ? $"SIEGE: lost tiers require rebuilding. {_base.Construction.LastStatus}"
            : _base.Health < _base.MaxHealth ? $"UNDER ATTACK / DAMAGED: {_base.Construction.LastStatus}" : _actionFeedback.Length > 0 ? _actionFeedback : _base.Construction.LastStatus;
        _hint.Text = _base.Tier < _base.HighestPurchasedTier || _base.Health < _base.MaxHealth ? "B  Base management · SIEGE" : "B  Base management";
        for (int i = 0; i < 5; i++)
        {
            var project = _base.Construction.GetActive((ConstructionSlot)i);
            _slotLabels[i].Text = project == null ? $"{(ConstructionSlot)i}\nIdle"
                : $"{(ConstructionSlot)i}\n{project.Definition.Name}\n{project.Progress:P0} · paid {project.PaidAntimatter}";
            _progress[i].Value = project?.Progress ?? 0;
            _cancel[i].Disabled = project == null;
            _cancelInstanceIds[i] = project?.InstanceId ?? 0;
        }
        var next = _base.Construction.NextTierDefinition();
        _tierDescription.Text = next == null ? "No next tier available" : $"{next.Name} · {next.Cost} antimatter · {next.DurationSeconds:0}s\n"
            + $"Capacity {_base.AntimatterStorageCapacity} → {_base.StorageCapacityForTier(_base.Tier + 1)}. Adds one HP layer; spent antimatter stays spent.";
        string tierReason = _base.Construction.GetNextTierStartReason();
        _tierOrder.Text = tierReason.Length == 0 ? "Start tier project" : tierReason;
        _tierOrder.Disabled = tierReason.Length > 0;
        foreach (var pair in _orders)
        {
            string reason = _base.Construction.GetStartReason(pair.Key);
            pair.Value.Button.Disabled = reason.Length > 0;
            pair.Value.Status.Text = reason.Length == 0 ? "Available" : reason == "Completed"
                ? _base.Construction.IsEffectActive(pair.Key) ? "Completed · active" : "Completed · suspended until tier rebuilt" : reason;
        }
    }

    public override void _Process(double delta)
    {
        if (IsOpen && (GetTree().Paused || _base.IsDestroyed)) SetOpen(false);
        _hint.Visible = !IsOpen && !GetTree().Paused
            && (GetTree().GetFirstNodeInGroup("tactical_map_ui") as TacticalMap)?.IsExpanded != true;
        _refresh -= (float)delta;
        if (_refresh > 0) return;
        _refresh = 0.1f; Refresh();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode == Key.B || (IsOpen && key.Keycode == Key.Escape))
        { SetOpen(!IsOpen); GetViewport().SetInputAsHandled(); return; }
        if (!IsOpen) return;
        if (key.Keycode is Key.F2 or Key.F6 or Key.M) { SetOpen(false); return; }
        if (key.Keycode == Key.R)
        {
            SetOpen(false);
            GetTree().Paused = false;
            Callable.From(() => GetTree().ReloadCurrentScene()).CallDeferred();
        }
        GetViewport().SetInputAsHandled();
    }

    public override void _ExitTree()
    {
        if (_overlay != null && IsOpen && IsInstanceValid(_player))
        {
            _player.GameplayInputBlocked = false;
            if (!GetTree().Paused) Input.MouseMode = _previousMouse;
        }
    }
}

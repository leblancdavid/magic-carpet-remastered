using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Ui;

public partial class BaseManagement : CanvasLayer
{
    public bool IsOpen => _overlay.Visible;
    public string SummaryText => _summary.Text;
    public string AlertText => _alert.Text;
    private bool CanIssueOrders => IsOpen && !GetTree().Paused && _player.Health > 0 && !_base.IsDestroyed;
    private Control _overlay = null!;
    private PanelContainer _panel = null!, _orderPanel = null!;
    private Label _summary = null!, _alert = null!, _installationStatus = null!, _orderInfo = null!;
    private UiTile _tierOrder = null!;
    private readonly Dictionary<string, UiTile> _orders = new();
    private readonly Dictionary<string, Button> _equipmentToggles = new(), _launchButtons = new();
    private TabContainer _tabs = null!;
    private OptionButton _targets = null!;
    private Button _confirmOrder = null!;
    private readonly List<Node3D?> _targetActors = new();
    private string _selectedOrder = "";
    private static readonly ConstructionSlot[] TabSlots = { ConstructionSlot.Offense, ConstructionSlot.Defense, ConstructionSlot.Resources, ConstructionSlot.Ship };
    private FactionBase _base = null!;
    private ShipFlightController _player = null!;
    private Input.MouseModeEnum _previousMouse;
    private float _refresh;
    private string _actionFeedback = "";

    public override void _Ready()
    {
        AddToGroup("base_management"); ProcessMode = ProcessModeEnum.Always; Layer = 7;
        _base = (FactionBase)GetTree().GetFirstNodeInGroup("player_base");
        _player = (ShipFlightController)GetTree().GetFirstNodeInGroup("player");
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore }; AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _overlay = new Control { Name = "ManagementOverlay", Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
        root.AddChild(_overlay); _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color(0.015f, 0.025f, 0.05f, 0.8f), MouseFilter = Control.MouseFilterEnum.Ignore };
        _overlay.AddChild(shade); shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _panel = new PanelContainer { Name = "ManagementPanel" }; _overlay.AddChild(_panel);
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _panel.AnchorLeft = 0.04f; _panel.AnchorRight = 0.96f; _panel.AnchorTop = 0.04f; _panel.AnchorBottom = 0.95f;
        using var panelStyle = UiTile.Frame(new Color(0.035f, 0.055f, 0.09f), new Color(0.24f, 0.62f, 0.76f));
        _panel.AddThemeStyleboxOverride("panel", panelStyle);
        var column = new VBoxContainer(); column.AddThemeConstantOverride("separation", 6); _panel.AddChild(column);
        var header = new HBoxContainer(); column.AddChild(header);
        _summary = Text("", 16); _summary.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; header.AddChild(_summary);
        _tierOrder = Tile("Order_Tier", "base"); _tierOrder.Compact = true; _tierOrder.CustomMinimumSize = new Vector2(230, 72);
        header.AddChild(_tierOrder); _tierOrder.Pressed += () => OrderNextTier();
        _tierOrder.CancelButton.Name = "Cancel_Base";
        _tierOrder.CancelButton.Pressed += () => CancelProject(ConstructionSlot.Base, _tierOrder.ProjectInstanceId);
        var close = Button("CloseManagement", "Close · B / Esc"); header.AddChild(close); close.Pressed += () => SetOpen(false);
        _alert = Text("", 13); _alert.AddThemeColorOverride("font_color", Colors.Orange); column.AddChild(_alert);
        _tabs = new TabContainer { Name = "ProjectTabs", SizeFlagsVertical = Control.SizeFlags.ExpandFill }; column.AddChild(_tabs);
        foreach (var slot in TabSlots)
        {
            var scroll = new ScrollContainer { Name = slot.ToString(), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; _tabs.AddChild(scroll);
            var tiers = new VBoxContainer { Name = "TierRows", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; tiers.AddThemeConstantOverride("separation", 12); scroll.AddChild(tiers);
            if (slot == ConstructionSlot.Ship) { _installationStatus = Text(""); tiers.AddChild(_installationStatus); }
            for (int tier = 1; tier <= 5; tier++)
            {
                var row = new HBoxContainer { Name = $"TierRow_{tier}" }; row.AddThemeConstantOverride("separation", 8); tiers.AddChild(row);
                var label = Text($"TIER\n{Rank(tier)}", 14); label.CustomMinimumSize = new Vector2(48, 0); row.AddChild(label);
                var grid = new GridContainer { Name = $"TierGrid_{tier}", Columns = Math.Max(slot == ConstructionSlot.Ship ? 4 : 3, UpgradeCatalog.Items.Count(item => item.Slot == slot && item.Tier == tier)),
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                grid.AddThemeConstantOverride("h_separation", 8); row.AddChild(grid);
                foreach (var item in UpgradeCatalog.Items.Where(item => item.Slot == slot && item.Tier == tier))
                {
                    var cell = new VBoxContainer { Name = $"Upgrade_{item.Id}", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                    grid.AddChild(cell);
                    var tile = Tile($"Order_{item.Id}", item.Id); tile.CustomMinimumSize = new Vector2(136, 134);
                    cell.AddChild(tile); _orders.Add(item.Id, tile);
                    tile.Pressed += () => OrderUpgrade(item.Id);
                    tile.CancelButton.Name = $"Cancel_{item.Id}";
                    tile.CancelButton.Pressed += () => CancelProject(item.Slot, tile.ProjectInstanceId);
                    if (item.Equipment)
                    {
                        var equip = Button($"Equip_{item.Id}", "Equip"); equip.ToggleMode = true; cell.AddChild(equip);
                        _equipmentToggles.Add(item.Id, equip); equip.Pressed += () => ToggleEquipment(item.Id);
                    }
                    else if (item.Deployable)
                    {
                        var launch = Button($"Launch_{item.Id}", "Deploy"); launch.ClipText = true; cell.AddChild(launch);
                        _launchButtons.Add(item.Id, launch); launch.Pressed += () =>
                        {
                            if (AutomaticDroneFleet.Supports(item.Id))
                                OrderProject(AutomaticDroneFleet.ExpansionId(item.Id, _base.Fleet.Entitlement(item.Id) + 1));
                            else OpenTargetOrder(item.Id);
                        };
                    }
                    else
                    {
                        var passive = Text(item.Id == "teleport-home" ? "Built-in · T" : "Passive", 12); passive.HorizontalAlignment = HorizontalAlignment.Center; cell.AddChild(passive);
                    }
                }
            }
        }
        column.AddChild(Text("Click an upgrade for its next star · bronze ★ / silver ★★ / gold ★★★ · × refunds · gameplay stays live", 12));
        BuildTargetOrderPanel(); Refresh();
    }

    private static Label Text(string text, int size = 14)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size); return label;
    }
    private static Button Button(string name, string text) => new() { Name = name, Text = text, FocusMode = Control.FocusModeEnum.None };
    private static UiTile Tile(string name, string icon) { var tile = new UiTile { Name = name }; tile.Glyph.IconId = icon; return tile; }
    private void BuildTargetOrderPanel()
    {
        var blocker = new ColorRect { Name = "TargetOrderBackdrop", Color = new Color(0, 0, 0, 0.65f),
            Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
        _overlay.AddChild(blocker); blocker.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _orderPanel = new PanelContainer { Name = "TargetOrderPanel", Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
        _overlay.AddChild(_orderPanel); _orderPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _orderPanel.VisibilityChanged += () => blocker.Visible = _orderPanel.Visible;
        _orderPanel.OffsetLeft = -210; _orderPanel.OffsetRight = 210; _orderPanel.OffsetTop = -100; _orderPanel.OffsetBottom = 100;
        using var style = UiTile.Frame(new Color(0.04f, 0.08f, 0.12f), Colors.Cyan, 2); _orderPanel.AddThemeStyleboxOverride("panel", style);
        var column = new VBoxContainer(); _orderPanel.AddChild(column);
        _orderInfo = Text(""); column.AddChild(_orderInfo);
        _targets = new OptionButton { Name = "OrderTargets", FocusMode = Control.FocusModeEnum.None }; column.AddChild(_targets);
        _targets.ItemSelected += _ => RefreshOrder();
        var actions = new HBoxContainer(); column.AddChild(actions);
        _confirmOrder = Button("ConfirmTargetOrder", "Confirm paid order"); actions.AddChild(_confirmOrder); _confirmOrder.Pressed += () => ConfirmTargetOrder();
        var back = Button("CancelTargetOrder", "Back"); actions.AddChild(back); back.Pressed += () => _orderPanel.Visible = false;
    }
    public bool OpenTargetOrder(string capability)
    {
        if (!CanIssueOrders || _orderPanel.Visible || AutomaticDroneFleet.Supports(capability) || UpgradeCatalog.Find(capability) is not { Deployable: true, Playable: true }) return false;
        _selectedOrder = BaseOrders.OrderId(capability); _targetActors.Clear(); _targets.Clear();
        foreach (var target in BaseOrders.Targets(_base, _selectedOrder))
        {
            _targetActors.Add(target);
            _targets.AddItem(target == null ? "Nearest non-owned point (automatic)" : target == _base ? "Home base"
                : target is StrategicPoint point ? $"{point.Name} · {point.Role}" : "Discovered rival base");
        }
        _targets.Selected = _targetActors.Count > 0 ? 0 : -1;
        _orderPanel.Visible = true; RefreshOrder(); return true;
    }
    private void RefreshOrder()
    {
        var definition = _base.Construction.Catalog.FirstOrDefault(project => project.Id == _selectedOrder);
        string reason = definition == null ? "Unavailable capability" : _base.Construction.GetStartReason(_selectedOrder);
        if (reason.Length == 0) reason = _targets.Selected < 0 || _targets.Selected >= _targetActors.Count ? "No eligible known target"
            : BaseOrders.TargetReason(_base, _selectedOrder, _targetActors[_targets.Selected]);
        _orderInfo.Text = definition == null ? reason : $"{definition.Name}\n{definition.Cost} A · {definition.DurationSeconds:0}s\n{(reason.Length == 0 ? "Choose a target and confirm payment." : reason)}";
        _confirmOrder.Disabled = reason.Length > 0;
    }
    public bool ConfirmTargetOrder()
    {
        if (!CanIssueOrders || !_orderPanel.Visible || _targets.Selected < 0 || _targets.Selected >= _targetActors.Count) return false;
        bool result = _base.Construction.TryStartOrder(_selectedOrder, _targetActors[_targets.Selected], out string reason);
        _actionFeedback = result ? "" : reason;
        if (result) _orderPanel.Visible = false; Refresh(); return result;
    }
    public bool OrderUpgrade(string id)
    {
        var item = UpgradeCatalog.Find(id);
        if (item == null || !item.Playable) return false;
        int rank = _base.Construction.UpgradeRank(id);
        return rank < 3 && OrderProject(UpgradeCatalog.ProjectId(id, rank + 1));
    }
    public bool ToggleEquipment(string id)
    {
        if (!CanIssueOrders || _orderPanel.Visible) return false;
        bool result = _player.TryToggleEquipment(id, out string reason); _actionFeedback = result ? "" : reason; Refresh(); return result;
    }
    // Shared diagnostic/rival installation API; player UI uses remote plans and auto-installation.
    public bool InstallEquipment(string id, int slot)
    {
        if (!CanIssueOrders || _orderPanel.Visible) return false;
        bool result = _player.TryInstallEquipment(id, slot, out string reason); _actionFeedback = result ? "" : reason; Refresh(); return result;
    }
    public bool SetOpen(bool open)
    {
        if (open == IsOpen) return true;
        if (open && (GetTree().Paused || _base.IsDestroyed || _player.Health <= 0)) return false;
        if (open)
        {
            (GetTree().GetFirstNodeInGroup("boon_choices") as BoonChoices)?.SetOpen(false);
            (GetTree().GetFirstNodeInGroup("tactical_map_ui") as TacticalMap)?.SetExpanded(false);
            _previousMouse = Input.MouseMode; _player.GameplayInputBlocked = true; Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else { _orderPanel.Visible = false; _player.GameplayInputBlocked = false; if (!GetTree().Paused) Input.MouseMode = _previousMouse; }
        _overlay.Visible = open; if (open) Refresh(); return true;
    }
    public bool OrderNextTier()
    {
        if (!CanIssueOrders || _orderPanel.Visible) return false;
        bool result = _base.Construction.TryStartNextTier(out string reason); _actionFeedback = result ? "" : reason; Refresh(); return result;
    }
    public bool OrderProject(string id)
    {
        if (!CanIssueOrders || _orderPanel.Visible) return false;
        bool result = _base.Construction.TryStart(id, out string reason); _actionFeedback = result ? "" : reason; Refresh(); return result;
    }
    public bool CancelProject(ConstructionSlot slot, long instanceId)
    {
        if (!CanIssueOrders || _orderPanel.Visible) return false;
        bool result = _base.Construction.Cancel(slot, instanceId); _actionFeedback = result ? "" : "Project finished or replaced"; Refresh(); return result;
    }
    public void Refresh()
    {
        _installationStatus.Text = _player.CanInstallEquipment ? "AT HOME · planned loadout and researched levels apply automatically"
            : _player.HasPendingLoadout ? "LOADOUT UPDATE · return home to install your plan" : "REMOTE · select up to four items; return home to install";
        _installationStatus.TooltipText = "Equip/Unequip changes the planned loadout anywhere. Active equipment changes automatically within the living home's installation area. Passives apply on completion.";
        _summary.Text = $"ANTIMATTER  {_base.StoredAntimatter}/{_base.EffectiveStorageCapacity}\nBASE  {Rank(_base.Tier)}";
        _summary.TooltipText = StrategicPoint.Summary(_base) + $"\nBase HP {_base.Health}/{_base.MaxHealth} · Energy {_player.Energy}/{_player.EnergyCapacity}";
        _alert.Text = _base.IsDestroyed ? "HOME BASE DESTROYED" : _base.Tier < _base.HighestPurchasedTier ? "SIEGE · lost tiers require rebuilding"
            : _base.Health < _base.MaxHealth ? $"BASE DAMAGED · {(_base.IsRepairing ? "repairing" : "waiting for quiet")}" : _actionFeedback;
        _alert.Visible = _alert.Text.Length > 0;
        for (int i = 0; i < TabSlots.Length; i++)
        {
            var active = _base.Construction.GetActive(TabSlots[i]); _tabs.SetTabTitle(i, TabSlots[i] + (active == null ? "" : $" · {active.Progress:P0}"));
        }
        var next = _base.Construction.NextTierDefinition(); string tierReason = _base.Construction.GetNextTierStartReason();
        var baseProject = _base.Construction.GetActive(ConstructionSlot.Base);
        _tierOrder.Display(baseProject?.Definition.Name ?? next?.Name ?? "Maximum tier", next == null ? "" : Costs(next),
            baseProject == null ? tierReason.Length == 0 ? "Build →" : "Unavailable" : $"Building {baseProject.Progress:P0}",
            next == null ? "No next tier available." : $"{next.Name}\n{Costs(next)}\nCapacity {_base.AntimatterStorageCapacity} → {_base.StorageCapacityForTier(_base.Tier + 1)}\n{tierReason}", tierReason.Length > 0);
        _tierOrder.SetProject(baseProject, 0, tierReason.Length > 0);
        foreach (var item in UpgradeCatalog.Items)
        {
            int rank = _base.Construction.UpgradeRank(item.Id);
            var tile = _orders[item.Id]; var definition = UpgradeCatalog.Definition(item, Math.Min(3, rank + 1));
            var active = _base.Construction.GetActive(item.Slot);
            var research = active?.Definition.UpgradeId == item.Id ? active : null;
            var production = item.Deployable && active?.Definition.Id == BaseOrders.OrderId(item.Id) ? active : null;
            string reason = !item.Playable ? "Planned content · not yet playable" : rank == 3 ? "Maximum level" : _base.Construction.GetStartReason(definition.Id);
            string status = research != null ? $"Building {research.Progress:P0}" : !item.Playable ? "Coming later" : rank == 3 ? "Maximum level"
                : reason.Length == 0 ? "Research next star →" : "Unavailable";
            bool suspended = rank > 0 && item.Slot != ConstructionSlot.Ship && _base.Tier < item.Tier;
            if (suspended) status = "Effect suspended";
            tile.Display(item.Name, !item.Playable ? "Not yet available" : rank == 3 ? "Fully researched" : Costs(definition), status,
                $"{item.Name} · unlock tier {Rank(item.Tier)}\n{item.Description}\nResearched level {rank}/3\n"
                    + (rank < 3 ? $"Next: {definition.Cost} antimatter · {definition.DurationSeconds:0}s\n" : "")
                    + (suspended ? $"Effect suspended until tier {Rank(item.Tier)} is rebuilt.\n" : "")
                    + (reason.Length == 0 ? "Click for the next sequential level." : reason), reason.Length > 0 || _orderPanel.Visible);
            tile.SetStars(rank); tile.SetProject(research ?? production, item.Tier, reason.Length > 0 && rank < 3);
            if (research != null || production != null)
            {
                var project = research ?? production!;
                tile.State.Text = $"{(production == null ? "Building" : "Producing")} {project.Progress:P0} · {Math.Max(0, project.Definition.DurationSeconds - project.ElapsedSeconds) / StrategicPoint.ProjectSpeed(_base):0}s";
                tile.CancelButton.TooltipText = $"Cancel {project.Definition.Name}: refund {project.PaidAntimatter} A. Overflow stays owned.";
            }
            if (_equipmentToggles.TryGetValue(item.Id, out var equip))
            {
                bool planned = _player.IsEquipmentPlanned(item.Id);
                equip.SetPressedNoSignal(planned); equip.Text = planned ? "Unequip" : "Equip";
                equip.Disabled = !item.Playable || rank == 0 || !planned && _player.PlannedEquipmentSlots.All(id => id != null) || _orderPanel.Visible;
                equip.TooltipText = $"Planned {(planned ? "equipped" : "unequipped")} · installed level {_player.GetEquipmentRank(item.Id)}\nSelect remotely; return home for automatic installation. Keys 1–4 follow catalog row order.";
            }
            if (_launchButtons.TryGetValue(item.Id, out var launch))
            {
                if (AutomaticDroneFleet.Supports(item.Id))
                {
                    int count = _base.Fleet.Entitlement(item.Id);
                    string expansion = AutomaticDroneFleet.ExpansionId(item.Id, count + 1);
                    string expansionReason = count >= AutomaticDroneFleet.MaximumUnitsPerType ? "Fleet limit reached" : _base.Construction.GetStartReason(expansion);
                    var expanding = active?.Definition.Id == expansion ? active : null;
                    launch.Text = rank < 3 ? _base.Fleet.Status(item.Id) : count >= AutomaticDroneFleet.MaximumUnitsPerType
                        ? $"Fleet {count}/{AutomaticDroneFleet.MaximumUnitsPerType}" : $"+1 drone · {UpgradeCatalog.Definition(item, 3).Cost * count} A";
                    launch.Disabled = rank < 3 || expansionReason.Length > 0 || _orderPanel.Visible;
                    launch.TooltipText = _base.Fleet.Status(item.Id) + "\nFree automatic replacements; independent production clocks.\n" + expansionReason;
                    if (expanding != null)
                    {
                        tile.SetProject(expanding, item.Tier, false);
                        tile.State.Text = $"Expanding fleet {expanding.Progress:P0}";
                    }
                    else if (rank > 0 && research == null) tile.State.Text = _base.Fleet.Status(item.Id);
                    continue;
                }
                var order = _base.Construction.Catalog.FirstOrDefault(project => project.Id == BaseOrders.OrderId(item.Id));
                string launchReason = order == null ? "Not yet playable" : _base.Construction.GetStartReason(order.Id);
                string action = item.Id is "particle-cannon" or "rocket-battery" or "orbital-lance" or "electronic-warfare-array" ? "Fire"
                    : item.Id == "strike-drone" ? "Launch" : "Deploy";
                launch.Text = order == null ? $"{action} · coming later" : $"{action} · {order.Cost} A";
                launch.Disabled = launchReason.Length > 0 || _orderPanel.Visible;
                launch.TooltipText = launchReason.Length == 0 ? "Choose a known target, then confirm the paid order." : launchReason;
            }
        }
        if (_orderPanel.Visible) RefreshOrder();
    }
    private static string Costs(ConstructionDefinition project) => $"{project.Cost} A · {project.DurationSeconds:0}s";
    public static string Rank(int rank) => rank switch { 1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", _ => "—" };
    public override void _Process(double delta)
    {
        if (IsOpen && (GetTree().Paused || _base.IsDestroyed || _player.Health <= 0)) SetOpen(false);
        if (!IsOpen) return;
        _refresh -= (float)delta; if (_refresh > 0) return; _refresh = 0.1f; Refresh();
    }
    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (IsOpen && _orderPanel.Visible && key.Keycode == Key.Escape) { _orderPanel.Visible = false; Refresh(); GetViewport().SetInputAsHandled(); return; }
        if (key.Keycode == Key.B || IsOpen && key.Keycode == Key.Escape) { SetOpen(!IsOpen); GetViewport().SetInputAsHandled(); return; }
        if (!IsOpen) return;
        if (key.Keycode is Key.F2 or Key.F6 or Key.M or Key.H) { SetOpen(false); return; }
        if (key.Keycode == Key.R) { SetOpen(false); GetTree().Paused = false; Callable.From(() => GetTree().ReloadCurrentScene()).CallDeferred(); }
        GetViewport().SetInputAsHandled();
    }
    public override void _ExitTree()
    {
        if (_overlay != null && IsOpen && IsInstanceValid(_player))
        { _player.GameplayInputBlocked = false; if (!GetTree().Paused) Input.MouseMode = _previousMouse; }
    }
}

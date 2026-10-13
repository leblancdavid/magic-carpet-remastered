using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;
using MagicCarpetRemastered.Scripts.Equipment;

namespace MagicCarpetRemastered.Scripts.Ui;

public partial class BaseManagement : CanvasLayer
{
    public bool IsOpen => _overlay.Visible;
    public string SummaryText => _summary.Text;
    public string AlertText => _alert.Text;
    private Control _overlay = null!;
    private PanelContainer _panel = null!;
    private Label _summary = null!, _alert = null!, _installationStatus = null!;
    private UiTile _tierOrder = null!;
    private readonly Dictionary<string, UiTile> _orders = new();
    private readonly Dictionary<string, UiTile> _equipmentToggles = new();
    private readonly Dictionary<string, Button> _applyRanks = new();
    private readonly List<GridContainer> _grids = new();
    private TabContainer _tabs = null!;
    private static readonly ConstructionSlot[] TabSlots = { ConstructionSlot.Offense, ConstructionSlot.Defense, ConstructionSlot.Resources, ConstructionSlot.Ship };
    private FactionBase _base = null!;
    private ShipFlightController _player = null!;
    private Input.MouseModeEnum _previousMouse;
    private float _refresh;
    private string _actionFeedback = string.Empty;

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
        _tierOrder = Tile("Order_Tier", "base"); _tierOrder.Compact = true; _tierOrder.CustomMinimumSize = new Vector2(250, 72);
        header.AddChild(_tierOrder); _tierOrder.Pressed += () => OrderNextTier();
        _tierOrder.CancelButton.Name = "Cancel_Base";
        _tierOrder.CancelButton.Pressed += () => CancelProject(ConstructionSlot.Base, _tierOrder.ProjectInstanceId);
        var close = new Button { Text = "Close · B / Esc", FocusMode = Control.FocusModeEnum.None }; header.AddChild(close); close.Pressed += () => SetOpen(false);
        _alert = Text("", 13); _alert.AddThemeColorOverride("font_color", new Color(1, 0.7f, 0.3f)); column.AddChild(_alert);
        var tabs = new TabContainer { Name = "ProjectTabs", SizeFlagsVertical = Control.SizeFlags.ExpandFill }; column.AddChild(tabs);
        _tabs = tabs;
        foreach (var slot in TabSlots)
        {
            var scroll = new ScrollContainer { Name = slot.ToString(), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; tabs.AddChild(scroll);
            var projects = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; scroll.AddChild(projects);
            VBoxContainer research = projects;
            if (slot == ConstructionSlot.Ship)
            {
                _installationStatus = Text(""); research.AddChild(_installationStatus);
            }
            if (slot == ConstructionSlot.Ship)
            {
                AddResearchRows(research);
                continue;
            }
            var grid = Grid(); research.AddChild(grid);
            foreach (var definition in _base.Construction.Catalog)
            {
                if (definition.Slot != slot) continue;
                var tile = Tile($"Order_{definition.Id}", UiIcon.ForProject(definition.Id)); grid.AddChild(tile);
                _orders.Add(definition.Id, tile); tile.Pressed += () => OrderProject(definition.Id);
                WireCancel(tile, definition.Slot);
            }
        }
        column.AddChild(Text("Click to purchase · hover for details · × cancels for a full refund · gameplay stays live", 12));
        _panel.Resized += LayoutGrids;
        LayoutGrids(); Refresh();
    }

    private static Label Text(string text, int size = 14)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size); return label;
    }
    private void AddResearchRows(VBoxContainer research)
    {
        var table = new GridContainer { Name = "EquipmentResearchTable", Columns = 4,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        table.AddThemeConstantOverride("h_separation", 8); table.AddThemeConstantOverride("v_separation", 8);
        research.AddChild(table);
        foreach (string heading in new[] { "Equipment", "Rank I", "Rank II", "Rank III" }) table.AddChild(ResearchLabel(heading));
        foreach (string id in ShipEquipmentCatalog.ItemIds)
        {
            var item = new VBoxContainer { CustomMinimumSize = new Vector2(136, 0) }; table.AddChild(item);
            var toggle = ResearchTile($"Equip_{id}", id); toggle.ToggleMode = true;
            item.AddChild(toggle); _equipmentToggles.Add(id, toggle);
            toggle.Pressed += () => ToggleEquipment(id);
            var apply = new Button { Name = $"ApplyRank_{id}", Text = "Apply rank", FocusMode = Control.FocusModeEnum.None };
            apply.AddThemeFontSizeOverride("font_size", 12); item.AddChild(apply); _applyRanks.Add(id, apply);
            apply.Pressed += () => InstallEquipment(id, _player.EquipmentSlots.ToList().IndexOf(id));
            for (int rank = 1; rank <= ShipEquipmentCatalog.MaximumRank; rank++)
            {
                string projectId = ShipEquipmentCatalog.ProjectId(id, rank);
                if (id == "plasma-bolt" && rank == 1) { table.AddChild(ResearchLabel("Starting · Owned")); continue; }
                var tile = ResearchTile($"Order_{projectId}", id); table.AddChild(tile);
                _orders.Add(projectId, tile); tile.Pressed += () => OrderProject(projectId);
                WireCancel(tile, ConstructionSlot.Ship);
            }
        }
        table.AddChild(ResearchLabel("Built-in Claim"));
        var claim = ResearchTile("Order_claim-range", UiIcon.ForProject("claim-range")); table.AddChild(claim);
        _orders.Add("claim-range", claim); claim.Pressed += () => OrderProject("claim-range");
        WireCancel(claim, ConstructionSlot.Ship);
        table.AddChild(ResearchLabel("—")); table.AddChild(ResearchLabel("—"));
    }
    private static Label ResearchLabel(string text)
    {
        var label = Text(text, 13); label.CustomMinimumSize = new Vector2(136, 0); return label;
    }
    private GridContainer Grid()
    {
        var grid = new GridContainer { Columns = 5, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 8); grid.AddThemeConstantOverride("v_separation", 8); _grids.Add(grid); return grid;
    }
    private static UiTile Tile(string name, string icon) { var tile = new UiTile { Name = name }; tile.Glyph.IconId = icon; return tile; }
    private static UiTile ResearchTile(string name, string icon)
    {
        var tile = Tile(name, icon); tile.CustomMinimumSize = new Vector2(136, 94);
        tile.Glyph.CustomMinimumSize = new Vector2(24, 24); return tile;
    }
    private void WireCancel(UiTile tile, ConstructionSlot slot)
    {
        tile.CancelButton.Name = $"Cancel_{tile.Name}";
        tile.CancelButton.Pressed += () => CancelProject(slot, tile.ProjectInstanceId);
    }
    private void LayoutGrids()
    {
        int count = Math.Clamp((int)((_panel.Size.X - 24) / 166), 2, 6);
        foreach (var grid in _grids) grid.Columns = count;
    }

    public bool ToggleEquipment(string id)
    {
        if (!IsOpen || GetTree().Paused) return false;
        bool result = _player.TryToggleEquipment(id, out string reason);
        _actionFeedback = result ? string.Empty : reason; Refresh(); return result;
    }

    public bool InstallEquipment(string id, int slot)
    {
        if (!IsOpen || GetTree().Paused) return false;
        bool result = _player.TryInstallEquipment(id, slot, out string reason);
        _actionFeedback = result ? "Equipment rank applied." : reason; Refresh(); return result;
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
        else { _player.GameplayInputBlocked = false; if (!GetTree().Paused) Input.MouseMode = _previousMouse; }
        _overlay.Visible = open; if (open) { LayoutGrids(); Refresh(); } return true;
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
        _actionFeedback = result ? string.Empty : "Project finished or replaced"; Refresh(); return result;
    }

    public void Refresh()
    {
        _installationStatus.Text = _player.CanInstallEquipment ? "AT HOME · equip / unequip and apply ranks for free" : "REMOTE · return home to change equipment";
        _installationStatus.TooltipText = $"Installation requires a living ship within {_player.EquipmentInstallationRadius:0} surface units and 16 elevation units of your surviving home base. Research is remote; applying ranks is explicit.";
        foreach (var pair in _equipmentToggles)
        {
            var equipment = _player.FindEquipment(pair.Key)!;
            int owned = _player.GetOwnedEquipmentRank(pair.Key), installed = _player.GetEquipmentRank(pair.Key);
            int slot = _player.EquipmentSlots.ToList().IndexOf(pair.Key);
            string reason = owned == 0 ? "Research this item first." : !_player.CanInstallEquipment ? "Return home while alive to change equipment."
                : slot < 0 && _player.EquipmentSlots.All(id => id != null) ? "Turn off an item first; at most four can be equipped." : string.Empty;
            string tooltip = $"{equipment.DisplayName}\n{equipment.Description}\n{equipment.EnergyCost} energy · owned {Rank(owned)} · installed {Rank(installed)}\n"
                + (owned > 0 ? ShipEquipmentCatalog.EffectSummary(pair.Key, owned) + "\n" : "")
                + (reason.Length == 0 ? "Toggle equipment for free. Keys 1–4 follow row order. Ownership and research are retained." : reason)
                + (equipment.EnergyCost > _player.EnergyCapacity ? "\nEnergy cost exceeds current capacity; installation is still allowed at home." : "");
            pair.Value.SetPressedNoSignal(slot >= 0);
            pair.Value.Display(equipment.DisplayName, owned > 0 ? $"Owned {Rank(owned)}" : "Not researched",
                slot >= 0 ? $"[{slot + 1}] Equipped {Rank(installed)}" : "Click to equip", tooltip, reason.Length > 0);
            pair.Value.SetProject(null, 0, owned == 0);
            pair.Value.State.Modulate = slot >= 0 ? new Color(0.5f, 1, 0.7f) : Colors.White;
            var apply = _applyRanks[pair.Key];
            apply.Visible = slot >= 0 && owned > installed;
            apply.Disabled = !_player.CanInstallEquipment;
            apply.Text = $"Apply rank {Rank(owned)}";
            apply.TooltipText = _player.CanInstallEquipment ? "Apply completed research to this equipped item for free." : "Return home to apply the researched rank.";
        }
        _summary.Text = $"ANTIMATTER  {_base.StoredAntimatter}/{_base.EffectiveStorageCapacity}\nBASE  {Rank(_base.Tier)}";
        _summary.TooltipText = StrategicPoint.Summary(_base) + $"\nBase HP {_base.Health}/{_base.MaxHealth} · Energy {_player.Energy}/{_player.EnergyCapacity}";
        _alert.Text = _base.IsDestroyed ? "HOME BASE DESTROYED" : _base.Tier < _base.HighestPurchasedTier ? "SIEGE · lost tiers require rebuilding"
            : _base.Health < _base.MaxHealth ? $"BASE DAMAGED · {(_base.IsRepairing ? "repairing" : "waiting for quiet")}" : _actionFeedback;
        _alert.Visible = _alert.Text.Length > 0;
        for (int i = 0; i < TabSlots.Length; i++)
        {
            var project = _base.Construction.GetActive(TabSlots[i]);
            _tabs.SetTabTitle(i, TabSlots[i] + (project == null ? "" : $" · {project.Progress:P0}"));
        }
        var next = _base.Construction.NextTierDefinition(); string tierReason = _base.Construction.GetNextTierStartReason();
        var baseProject = _base.Construction.GetActive(ConstructionSlot.Base);
        _tierOrder.Display(baseProject?.Definition.Name ?? next?.Name ?? "Maximum tier", next == null ? "" : Costs(next),
            baseProject == null ? tierReason.Length == 0 ? "Build →" : "Unavailable" : $"Building {baseProject.Progress:P0}",
            next == null ? "No next tier available." : $"{next.Name}\n{next.Cost} antimatter · {next.DurationSeconds:0}s\n"
                + $"Capacity: {_base.AntimatterStorageCapacity} → {_base.StorageCapacityForTier(_base.Tier + 1)}.\nAdds an HP layer without restoring spent resources.\n"
                + (tierReason.Length == 0 ? "Click to start." : tierReason), tierReason.Length > 0);
        _tierOrder.SetProject(baseProject, 0, tierReason.Length > 0);
        _tierOrder.CancelButton.TooltipText = "Cancel base construction for its exact paid antimatter refund.";
        foreach (var definition in _base.Construction.Catalog)
        {
            var tile = _orders[definition.Id]; string reason = _base.Construction.GetStartReason(definition.Id);
            var active = _base.Construction.GetActive(definition.Slot);
            string status = reason.Length == 0 ? "Research →" : active?.Definition.Id == definition.Id ? $"Researching {active.Progress:P0}"
                : reason == "Completed" ? definition.Id.StartsWith("equipment-") ? "Owned" : _base.Construction.IsEffectActive(definition.Id) ? "Completed" : "Suspended" : "Locked";
            string tooltip = $"{definition.Name}\n{definition.Description}\n{definition.Cost} antimatter · {definition.DurationSeconds:0}s\nRequires tier {definition.RequiredTier}"
                + (definition.RequiredProject == null ? "." : $" and {ProjectTitle(definition.RequiredProject)}.")
                + "\n" + (reason.Length == 0 ? "Click to start paid research." : reason == "Completed"
                    ? status == "Suspended" ? "Completed · suspended until the required tier is rebuilt." : status + " · ownership retained"
                    : reason.StartsWith("Requires equipment-") ? "Requires " + ProjectTitle(reason[9..]) : reason);
            tile.Display(ProjectName(definition), Costs(definition), status, tooltip, reason.Length > 0);
            var project = active?.Definition.Id == definition.Id ? active : null;
            tile.SetProject(project, definition.RequiredTier, reason.Length > 0 && reason != "Completed");
            if (project != null)
            {
                tile.State.Text = $"Building {project.Progress:P0} · {Math.Max(0, project.Definition.DurationSeconds - project.ElapsedSeconds) / StrategicPoint.ProjectSpeed(_base):0}s";
                tile.CancelButton.TooltipText = $"Cancel {definition.Name}: refund {project.PaidAntimatter} antimatter. Overflow stays owned.";
            }
        }
    }
    private static string Costs(ConstructionDefinition project) => $"{project.Cost} A · {project.DurationSeconds:0}s";
    private string ProjectTitle(string id) => _base.Construction.Catalog.FirstOrDefault(project => project.Id == id)?.Name ?? id;
    private static string ProjectName(ConstructionDefinition project) => project.Id.StartsWith("equipment-")
        ? $"{UiIcon.ShortName(UiIcon.ForProject(project.Id))} {Rank(int.Parse(project.Id[^1..]))}"
        : project.Id.StartsWith("base-tier-") ? project.Name : UiIcon.ShortName(project.Id);
    public static string Rank(int rank) => rank switch { 1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", _ => "—" };

    public override void _Process(double delta)
    {
        if (IsOpen && (GetTree().Paused || _base.IsDestroyed || _player.Health <= 0)) SetOpen(false);
        _refresh -= (float)delta; if (_refresh > 0) return; _refresh = 0.1f; Refresh();
    }
    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode == Key.B || (IsOpen && key.Keycode == Key.Escape)) { SetOpen(!IsOpen); GetViewport().SetInputAsHandled(); return; }
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

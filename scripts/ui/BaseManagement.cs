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
    private readonly Label[] _slotLabels = new Label[5];
    private readonly ProgressBar[] _progress = new ProgressBar[5];
    private readonly Button[] _cancel = new Button[5];
    private readonly long[] _cancelInstanceIds = new long[5];
    private readonly Dictionary<string, UiTile> _orders = new(), _inventory = new();
    private readonly List<GridContainer> _grids = new();
    private readonly Label[] _equipmentLabels = new Label[4];
    private readonly Button[] _removeEquipment = new Button[4];
    private GridContainer _loadoutSlots = null!;
    private OptionButton _installationSlot = null!;
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
        var title = Text("BASE DEVELOPMENT  ·  REAL TIME", 19); title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; header.AddChild(title);
        var close = new Button { Text = "Close · B / Esc", FocusMode = Control.FocusModeEnum.None }; header.AddChild(close); close.Pressed += () => SetOpen(false);
        _summary = Text(""); column.AddChild(_summary);
        _alert = Text("", 13); _alert.AddThemeColorOverride("font_color", new Color(1, 0.7f, 0.3f)); column.AddChild(_alert);
        var slots = new GridContainer { Name = "ActiveProjects", Columns = 5 }; column.AddChild(slots);
        for (int i = 0; i < 5; i++)
        {
            var slot = (ConstructionSlot)i;
            var card = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; slots.AddChild(card);
            _slotLabels[i] = Text($"{slot} · idle", 13); _slotLabels[i].CustomMinimumSize = new Vector2(0, 38); card.AddChild(_slotLabels[i]);
            _progress[i] = new ProgressBar { MaxValue = 1, ShowPercentage = false, CustomMinimumSize = new Vector2(0, 7) }; card.AddChild(_progress[i]);
            _cancel[i] = new Button { Name = $"Cancel_{slot}", Text = "Cancel", FocusMode = Control.FocusModeEnum.None,
                TooltipText = "Cancel this project for a full refund of both payments. Overflow stays owned." };
            _cancel[i].AddThemeFontSizeOverride("font_size", 12); card.AddChild(_cancel[i]);
            _cancel[i].Pressed += () => CancelProject(slot, _cancelInstanceIds[(int)slot]);
        }
        var tabs = new TabContainer { Name = "ProjectTabs", SizeFlagsVertical = Control.SizeFlags.ExpandFill }; column.AddChild(tabs);
        for (int i = 0; i < 5; i++)
        {
            var slot = (ConstructionSlot)i;
            var scroll = new ScrollContainer { Name = slot.ToString(), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; tabs.AddChild(scroll);
            var projects = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; scroll.AddChild(projects);
            VBoxContainer research = projects;
            if (slot == ConstructionSlot.Ship)
            {
                var sections = new TabContainer { Name = "ShipSections", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; projects.AddChild(sections);
                research = new VBoxContainer { Name = "Research" }; sections.AddChild(research);
                var loadout = new VBoxContainer { Name = "Loadout" }; sections.AddChild(loadout); AddEquipmentUi(loadout);
                research.AddChild(Text("Research remotely · install completed items/ranks at home in Loadout", 13));
            }
            else research.AddChild(Text("Click a tile to order · hover for effects and requirements", 13));
            var grid = Grid(); research.AddChild(grid);
            if (slot == ConstructionSlot.Base)
            {
                _tierOrder = Tile("Order_Tier", "base"); grid.AddChild(_tierOrder); _tierOrder.Pressed += () => OrderNextTier();
            }
            foreach (var definition in _base.Construction.Catalog)
            {
                if (definition.Slot != slot) continue;
                var tile = Tile($"Order_{definition.Id}", UiIcon.ForProject(definition.Id)); grid.AddChild(tile);
                _orders.Add(definition.Id, tile); tile.Pressed += () => OrderProject(definition.Id);
            }
        }
        column.AddChild(Text("A = antimatter · M = matter · payments lower energy capacity · hover tiles for details", 12));
        _panel.Resized += LayoutGrids;
        LayoutGrids(); Refresh();
    }

    private static Label Text(string text, int size = 14)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size); return label;
    }
    private GridContainer Grid()
    {
        var grid = new GridContainer { Columns = 5, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 8); grid.AddThemeConstantOverride("v_separation", 8); _grids.Add(grid); return grid;
    }
    private static UiTile Tile(string name, string icon) { var tile = new UiTile { Name = name }; tile.Glyph.IconId = icon; return tile; }
    private void LayoutGrids()
    {
        int count = Math.Clamp((int)((_panel.Size.X - 24) / 166), 2, 6);
        foreach (var grid in _grids) grid.Columns = count;
        if (_loadoutSlots != null) _loadoutSlots.Columns = _panel.Size.X < 800 ? 2 : 4;
    }

    private void AddEquipmentUi(VBoxContainer projects)
    {
        _installationStatus = Text(""); projects.AddChild(_installationStatus);
        _loadoutSlots = new GridContainer { Name = "LoadoutSlots", Columns = 4 }; projects.AddChild(_loadoutSlots);
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            var card = new VBoxContainer { CustomMinimumSize = new Vector2(132, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; _loadoutSlots.AddChild(card);
            _equipmentLabels[i] = Text("", 14); card.AddChild(_equipmentLabels[i]);
            _removeEquipment[i] = new Button { Name = $"RemoveEquipment_{i}", Text = "Remove", FocusMode = Control.FocusModeEnum.None };
            card.AddChild(_removeEquipment[i]); _removeEquipment[i].Pressed += () => { if (IsOpen) _player.TryRemoveEquipment(index); Refresh(); };
        }
        projects.AddChild(Text("INSTALL OWNED EQUIPMENT · select destination, then click an item", 13));
        _installationSlot = new OptionButton { Name = "InstallationSlot", FocusMode = Control.FocusModeEnum.None };
        for (int i = 0; i < 4; i++) _installationSlot.AddItem($"Replace slot {i + 1}");
        _installationSlot.ItemSelected += _ => Refresh(); projects.AddChild(_installationSlot);
        var grid = Grid(); projects.AddChild(grid);
        foreach (string id in ShipEquipmentCatalog.ItemIds)
        {
            var tile = Tile($"Install_{id}", id); grid.AddChild(tile); _inventory.Add(id, tile);
            tile.Pressed += () => InstallEquipment(id, _installationSlot.Selected);
        }
        projects.AddChild(Text("RMB Claim / Shift booster are built in · 1–4 selects, LMB activates, Tab cycles", 12));
    }

    public bool InstallEquipment(string id, int slot)
    {
        if (!IsOpen || GetTree().Paused) return false;
        bool result = _player.TryInstallEquipment(id, slot, out string reason);
        _actionFeedback = result ? "Installed. Replaced equipment remains owned." : reason; Refresh(); return result;
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
        _installationStatus.Text = _player.CanInstallEquipment ? "AT HOME · free installation / replacement" : "REMOTE · return home to install";
        _installationStatus.TooltipText = $"Installation requires a living ship within {_player.EquipmentInstallationRadius:0} surface units and 16 elevation units of your surviving home base. Research is remote; applying ranks is explicit.";
        for (int i = 0; i < 4; i++)
        {
            string? id = _player.EquipmentSlots[i];
            string name = id == null ? "Empty" : $"{UiIcon.ShortName(id)} {Rank(_player.GetEquipmentRank(id))}";
            _equipmentLabels[i].Text = $"[{i + 1}] {name}";
            _removeEquipment[i].Disabled = !_player.CanInstallEquipment || id == null;
            _removeEquipment[i].TooltipText = !_player.CanInstallEquipment ? "Return home while alive to remove equipment." : "Remove from this slot. Ownership and research are retained.";
            _installationSlot.SetItemText(i, $"Slot {i + 1} · replace {name}");
        }
        foreach (var pair in _inventory)
        {
            var equipment = _player.FindEquipment(pair.Key)!;
            int owned = _player.GetOwnedEquipmentRank(pair.Key), installed = _player.GetEquipmentRank(pair.Key);
            string reason = owned == 0 ? "Research this item first." : !_player.CanInstallEquipment ? "Return home while alive to install." : string.Empty;
            string occupant = _player.EquipmentSlots[_installationSlot.Selected] is { } target ? _player.FindEquipment(target)!.RankedName : "Empty";
            string tooltip = $"{equipment.DisplayName}\n{equipment.Description}\n{equipment.EnergyCost} energy · owned {Rank(owned)} · installed {Rank(installed)}\n"
                + (owned > 0 ? ShipEquipmentCatalog.EffectSummary(pair.Key, owned) + "\n" : "")
                + $"Install into slot {_installationSlot.Selected + 1}, replacing {occupant}. Replaced items remain owned.\n"
                + (reason.Length == 0 ? "Click to install / apply owned rank for free." : reason)
                + (equipment.EnergyCost > _player.EnergyCapacity ? "\nEnergy cost exceeds current capacity; installation is still allowed at home." : "");
            pair.Value.Display(UiIcon.ShortName(pair.Key), $"Owned {Rank(owned)} · applied {Rank(installed)}",
                reason.Length == 0 ? $"Install → {_installationSlot.Selected + 1}" : owned == 0 ? "Not researched" : "Return home", tooltip, reason.Length > 0);
        }
        _summary.Text = $"Base tier {_base.Tier}/{_base.HighestPurchasedTier} · HP {_base.Health}/{_base.MaxHealth} · Energy {_player.Energy}/{_player.EnergyCapacity}\n"
            + $"Antimatter {_base.StoredAntimatter}/{_base.EffectiveStorageCapacity} · Matter {_base.StoredMatter}/{_base.EffectiveMatterCapacity}\n"
            + StrategicPoint.Summary(_base);
        _summary.AddThemeFontSizeOverride("font_size", 13);
        _alert.Text = _base.IsDestroyed ? "HOME BASE DESTROYED" : _base.Tier < _base.HighestPurchasedTier ? "SIEGE · lost tiers require rebuilding"
            : _base.Health < _base.MaxHealth ? $"BASE DAMAGED · {(_base.IsRepairing ? "repairing" : "waiting for quiet")}" : _actionFeedback;
        _alert.Visible = _alert.Text.Length > 0;
        for (int i = 0; i < 5; i++)
        {
            var project = _base.Construction.GetActive((ConstructionSlot)i);
            _slotLabels[i].Text = project == null ? $"{(ConstructionSlot)i}\nIdle" : $"{(ConstructionSlot)i} · {project.Progress:P0}\n{ProjectName(project.Definition)}";
            _progress[i].Value = project?.Progress ?? 0; _cancel[i].Disabled = project == null; _cancel[i].Visible = project != null;
            _cancelInstanceIds[i] = project?.InstanceId ?? 0;
            _cancel[i].TooltipText = project == null ? "No active project" : $"Cancel {project.Definition.Name}\nRefund {project.PaidAntimatter} antimatter + {project.PaidMatter} matter; owned overflow.\nLosing a tier destroys active investments instead.";
        }
        var next = _base.Construction.NextTierDefinition(); string tierReason = _base.Construction.GetNextTierStartReason();
        _tierOrder.Display(next?.Name ?? "Maximum tier", next == null ? "" : Costs(next), tierReason.Length == 0 ? "Build →" : "Unavailable",
            next == null ? "No next tier available." : $"{next.Name}\n{next.Cost} antimatter + {next.MatterCost} matter · {next.DurationSeconds:0}s\n"
                + $"Capacity: A {_base.AntimatterStorageCapacity} → {_base.StorageCapacityForTier(_base.Tier + 1)}; M {_base.MatterStorageCapacity} → {_base.MatterCapacityForTier(_base.Tier + 1)}.\nAdds an HP layer without restoring spent resources.\n"
                + (tierReason.Length == 0 ? "Click to start." : tierReason), tierReason.Length > 0);
        foreach (var definition in _base.Construction.Catalog)
        {
            var tile = _orders[definition.Id]; string reason = _base.Construction.GetStartReason(definition.Id);
            var active = _base.Construction.GetActive(definition.Slot);
            string status = reason.Length == 0 ? "Research →" : active?.Definition.Id == definition.Id ? $"Researching {active.Progress:P0}"
                : reason == "Completed" ? definition.Id.StartsWith("equipment-") ? "Owned · install" : _base.Construction.IsEffectActive(definition.Id) ? "Completed" : "Suspended" : "Locked";
            string tooltip = $"{definition.Name}\n{definition.Description}\n{definition.Cost} antimatter + {definition.MatterCost} matter · {definition.DurationSeconds:0}s\nRequires tier {definition.RequiredTier}"
                + (definition.RequiredProject == null ? "." : $" and {ProjectTitle(definition.RequiredProject)}.")
                + "\n" + (reason.Length == 0 ? "Click to start paid research." : reason == "Completed"
                    ? status == "Suspended" ? "Completed · suspended until the required tier is rebuilt." : status + " · ownership retained"
                    : reason.StartsWith("Requires equipment-") ? "Requires " + ProjectTitle(reason[9..]) : reason);
            tile.Display(ProjectName(definition), Costs(definition), status, tooltip, reason.Length > 0);
        }
    }
    private static string Costs(ConstructionDefinition project) => $"{project.Cost} A · {project.MatterCost} M · {project.DurationSeconds:0}s";
    private string ProjectTitle(string id) => _base.Construction.Catalog.FirstOrDefault(project => project.Id == id)?.Name ?? id;
    private static string ProjectName(ConstructionDefinition project) => project.Id.StartsWith("equipment-")
        ? $"{UiIcon.ShortName(UiIcon.ForProject(project.Id))} {Rank(int.Parse(project.Id[^1..]))}"
        : project.Id.StartsWith("base-tier-") ? project.Name : UiIcon.ShortName(project.Id);
    public static string Rank(int rank) => rank switch { 1 => "I", 2 => "II", 3 => "III", _ => "—" };

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

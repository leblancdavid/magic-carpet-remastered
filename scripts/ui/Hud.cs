using Godot;
using MagicCarpetRemastered.Scripts.Game;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Ui;

public partial class Hud : CanvasLayer
{
    private Label _vitals = null!;
    private Label _objective = null!;
    private Label _loadout = null!;
    private Label _feedback = null!;
    private Label _debugHint = null!;
    private PanelContainer _feedbackPanel = null!;
    private PanelContainer _debugPanel = null!;
    private RichTextLabel _debugText = null!;
    private float _refreshTimer;
    private ShipFlightController? _player;
    private AntimatterSource? _well;
    private FactionBase? _playerBase;
    private FactionBase? _enemyBase;
    private EnemyEcosystemDirector? _enemyEcosystem;
    private WorldAntimatterTracker? _antimatterTracker;
    private WorldMatterTracker? _matterTracker;
    private LevelObjective? _levelObjective;
    private PanelContainer _resultPanel = null!;
    private Label _result = null!;

    public override void _Ready()
    {
        var root = new Control { Name = "HudRoot", MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        AddLabelPanel(root, "Vitals", Control.LayoutPreset.TopLeft,
            new Vector2(20, 20), new Vector2(310, 132), out _vitals);
        AddLabelPanel(root, "Objective", Control.LayoutPreset.TopRight,
            new Vector2(-300, 20), new Vector2(280, 86), out _objective);
        AddLabelPanel(root, "Loadout", Control.LayoutPreset.BottomLeft,
            new Vector2(20, -124), new Vector2(360, 104), out _loadout);
        _feedbackPanel = AddLabelPanel(root, "Feedback", Control.LayoutPreset.BottomLeft,
            new Vector2(20, -188), new Vector2(360, 54), out _feedback);
        _feedbackPanel.Visible = false;

        _resultPanel = AddLabelPanel(root, "LevelResult", Control.LayoutPreset.Center,
            new Vector2(-240, -95), new Vector2(480, 190), out _result, 24);
        _result.HorizontalAlignment = HorizontalAlignment.Center;
        _resultPanel.Visible = false;

        AddLabelPanel(root, "DebugHint", Control.LayoutPreset.BottomRight,
            new Vector2(-230, -56), new Vector2(210, 36), out _debugHint, 14);
        _debugHint.Text = "F3  Show debug / controls";

        _debugPanel = CreatePanel("DebugPanel");
        root.AddChild(_debugPanel);
        Place(_debugPanel, Control.LayoutPreset.TopRight, new Vector2(-420, 120), new Vector2(400, 480));
        _debugPanel.AnchorBottom = 1;
        _debugPanel.OffsetBottom = -116;
        _debugPanel.Visible = false;

        var content = new VBoxContainer { Name = "DebugContent", MouseFilter = Control.MouseFilterEnum.Ignore };
        content.AddThemeConstantOverride("separation", 10);
        _debugPanel.AddChild(content);
        content.AddChild(CreateLabel("DEBUG / CONTROLS", 18));
        content.AddChild(CreateLabel("F3 close  ·  PgUp / PgDn scroll", 14));
        _debugText = new RichTextLabel
        {
            Name = "DebugDetails",
            BbcodeEnabled = true,
            ScrollActive = true,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            FocusMode = Control.FocusModeEnum.None
        };
        _debugText.AddThemeFontSizeOverride("normal_font_size", 16);
        _debugText.AddThemeFontSizeOverride("bold_font_size", 16);
        _debugText.AddThemeColorOverride("default_color", new Color(0.88f, 0.92f, 0.97f));
        _debugText.AddThemeConstantOverride("line_separation", 4);
        content.AddChild(_debugText);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("toggle_debug_ui") && !@event.IsEcho())
        {
            _debugPanel.Visible = !_debugPanel.Visible;
            _debugHint.Text = _debugPanel.Visible ? "F3  Hide debug / controls" : "F3  Show debug / controls";
            _refreshTimer = 0;
            GetViewport().SetInputAsHandled();
        }
        else if (_debugPanel.Visible && @event is InputEventKey { Pressed: true } key
            && (key.Keycode == Key.Pageup || key.Keycode == Key.Pagedown))
        {
            VScrollBar scroll = _debugText.GetVScrollBar();
            scroll.Value += (key.Keycode == Key.Pagedown ? 1 : -1) * scroll.Page * 0.8;
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (GetTree().Paused && @event.IsActionPressed("restart_arena") && !@event.IsEcho())
        {
            GetViewport().SetInputAsHandled();
            GetTree().Paused = false;
            GetTree().ReloadCurrentScene();
        }
    }

    public override void _Process(double delta)
    {
        _refreshTimer -= (float)delta;
        if (_refreshTimer > 0)
        {
            return;
        }
        _refreshTimer = 0.1f;

        _player ??= GetTree().GetFirstNodeInGroup("player") as ShipFlightController;
        _well ??= GetTree().GetFirstNodeInGroup("antimatter_source") as AntimatterSource;
        _playerBase ??= GetTree().GetFirstNodeInGroup("player_base") as FactionBase;
        _enemyBase ??= GetTree().GetFirstNodeInGroup("enemy_base") as FactionBase;
        _enemyEcosystem ??= GetTree().GetFirstNodeInGroup("enemy_ecosystem_director") as EnemyEcosystemDirector;
        _antimatterTracker ??= GetTree().GetFirstNodeInGroup("world_antimatter_tracker") as WorldAntimatterTracker;
        _matterTracker ??= GetTree().GetFirstNodeInGroup("world_matter_tracker") as WorldMatterTracker;
        _levelObjective ??= GetTree().GetFirstNodeInGroup("level_objective") as LevelObjective;
        if (_player == null)
        {
            return;
        }

        _vitals.Text = $"HP {_player.Health}/{_player.MaximumHealth}   Energy {_player.Energy}/{_player.EnergyCapacity}\n"
            + (_player.Health <= 0 ? $"Respawn in {_levelObjective?.PlayerRespawnRemaining:0.0}s"
                : _player.IsShielded ? _player.ShieldStatusText : _player.IsInvulnerable ? "Protected after hit" : "Ship ready")
            + (_playerBase == null ? "" : $"\nBase T{_playerBase.Tier}   Antimatter {_playerBase.StoredAntimatter}/{_playerBase.EffectiveStorageCapacity}\nMatter {_playerBase.StoredMatter}/{_playerBase.EffectiveMatterCapacity}");
        _objective.Text = _levelObjective == null ? "Eliminate the rival\nDestroy base and pilot"
            : $"Rival base: {(_levelObjective.RivalBaseDestroyed ? "Destroyed" : $"{_enemyBase?.Health} HP")}\n"
                + $"Rival pilot: {(_levelObjective.RivalPilotDefeated ? _levelObjective.RivalBaseDestroyed ? "Defeated" : $"Respawn {_levelObjective.RivalRespawnRemaining:0}s" : "Alive")}";
        _resultPanel.Visible = _levelObjective != null && _levelObjective.State != LevelObjective.LevelState.Playing;
        if (_resultPanel.Visible)
        {
            _result.Text = $"{(_levelObjective!.State == LevelObjective.LevelState.Victory ? "VICTORY" : "DEFEAT")}\n"
                + $"{_levelObjective.ResultMessage}\n\nPress R to restart";
        }
        _loadout.Text = $"Slot {_player.SelectedEquipmentSlot + 1}: {_player.PrimaryEquipmentName}   ·   LMB activate\n1–4 select   ·   Tab cycle   ·   RMB Claim\nShift: {_player.BoosterStatusText}";
        _feedback.Text = _player.Health <= 0 ? "Pilot down — your base will respawn you"
            : !string.IsNullOrEmpty(_player.DiscoveryFeedbackMessage) ? _player.DiscoveryFeedbackMessage : _player.ActiveFeedbackMessage;
        _feedbackPanel.Visible = !_resultPanel.Visible && !string.IsNullOrEmpty(_feedback.Text);

        if (!_debugPanel.Visible)
        {
            return;
        }

        string details = Section("FLIGHT / PLAYER")
            + $"Camera: {_player.CameraMode}\nSpeed: {_player.Speed:0.0}\nTerrain clearance: {_player.AltitudeAboveTerrain:0.0}\n"
            + $"Storage-backed energy limit: {_player.EnergyCapacity}\nRecharge: {_player.EnergyRechargePerSecond:0.0}/s\nAntimatter collection: collector drone delivery\n{_player.ShieldStatusText}\n\n"
            + Section("EQUIPMENT / MATCH BOONS")
            + _player.EquipmentProgressionText + "\nB researches ranks; install explicitly at home.\n"
            + $"H choices / summary · {_player.Boons.Pending.Count} secured rewards\n{_player.Boons.Summary}\n"
            + "Destroy wrecks; fly within 4 surface units to secure gold salvage.\nBoons survive respawn/tier loss; R resets them.\n\n"
            + Section("ANTIMATTER ECONOMY")
            + (_antimatterTracker == null ? "Tracker unavailable\n" : $"World total: {_antimatterTracker.TotalAntimatter}\nNeutral loose: {_antimatterTracker.NeutralLooseAntimatter}\nNeutral creatures: {_antimatterTracker.NeutralCreatureAntimatter}\nPlayer: {_antimatterTracker.PlayerControlledAntimatter}\nRival: {_antimatterTracker.EnemyControlledAntimatter}\n")
            + $"Extractor supply accepted: {_antimatterTracker?.SiteGeneratedAntimatter ?? 0}\n"
            + (_well == null ? "Source: unavailable\n\n" : "Source: finite neutral landmark\n\n")
            + Section("MATTER ECONOMY")
            + (_matterTracker == null ? "Tracker unavailable\n" : $"Physical: {_matterTracker.PhysicalMatter} · committed: {_matterTracker.NetCommittedMatter}\nExtractor supply accepted: {_matterTracker.SiteGeneratedMatter}\nActive: {_matterTracker.ActiveInvestment} · permanent siege loss: {_matterTracker.DestroyedInvestmentLoss}\n")
            + "Orange crates: Claim then drone hauling. Matter does not power equipment.\n\n"
            + Section("BASES / ENEMIES")
            + BaseDetails(_playerBase, "Player") + BaseDetails(_enemyBase, "Rival")
            + (_enemyEcosystem?.StatusText.Replace("   Intensity", "\nIntensity") ?? "Ecosystem unavailable") + "\n\n"
            + Section("TERRAIN") + _player.TerrainDebugText.Replace("   ", "\n")
            + "\n\n"
            + Section("CONTROLS")
            + "WASD — fly\nSpace / C — ascend / descend\nShift — built-in forward boost (fresh press)\nMouse — look\nV — camera mode\nB — base management (real time)\nM — world map / waypoint (real time, planet)\nEsc — close panel / capture / release mouse\nR — restart arena\n\n"
            + $"1–4 — select equipped slot (no activation)\nLMB — activate {_player.PrimaryEquipmentName}\nTab — cycle installed slots\n"
            + $"RMB — Claim ({_player.ClaimEquipmentEnergyCost} energy): aim at a globe/material crate or the ground before it; the bolt follows terrain after landing.\n\n"
            + Section("OWNERSHIP / ROLE GUIDE")
            + "Antimatter: gray neutral · cyan yours · red rival\nDrones haul owned antimatter to bases. Base damage reduces storage; excess spills neutral. Stored antimatter supports equipment power. Pop rivals to spill their cargo.\n\n"
            + "Orange — swarm\nGreen — beast\nBlue — caster\nRed — sieger\nYellow — thief\nPurple — pilot";
        if (_debugText.Text != details)
        {
            VScrollBar scroll = _debugText.GetVScrollBar();
            double position = scroll.Value;
            _debugText.Text = details;
            scroll.SetDeferred(Godot.Range.PropertyName.Value, position);
        }
    }

    private static string Section(string title) => $"[b][color=#82dbea]{title}[/color][/b]\n";

    private static string BaseDetails(FactionBase? hub, string side) => hub == null
        ? $"{side} base: unavailable\n"
        : $"{side} base: {(hub.IsDestroyed ? "Destroyed" : $"HP {hub.Health}/{hub.MaxHealth}")}\n"
            + $"Storage: {hub.StoredAntimatter}/{hub.EffectiveStorageCapacity} (intact {hub.AntimatterStorageCapacity})  ·  Tier {hub.Tier} / purchased {hub.HighestPurchasedTier}\n"
            + $"Payments: {hub.SpentAntimatter} · Refunds: {hub.RefundedAntimatter} · Investment spill: {hub.InvestmentSpilledAntimatter}\n"
            + $"Matter: {hub.StoredMatter}/{hub.EffectiveMatterCapacity} · Paid: {hub.SpentMatter} · Refunded: {hub.RefundedMatter} · Investment spill: {hub.InvestmentSpilledMatter}\n"
            + $"Construction: {hub.Construction.ActiveInvestment} invested · Siege loss: {hub.Construction.DestroyedInvestmentLoss}\n{hub.Construction.LastStatus}\n"
            + ConstructionDetails(hub) + StrategicPoint.Summary(hub) + "\n"
            + $"Repair: {(hub.IsDestroyed ? "Ruined" : hub.IsRepairing ? "Repairing" : hub.Health < hub.MaxHealth ? "Waiting for quiet" : "Intact")}\n\n";

    private static string ConstructionDetails(FactionBase hub)
    {
        string text = string.Empty;
        for (int i = 0; i < 5; i++)
        {
            var slot = (ConstructionSlot)i;
            var project = hub.Construction.GetActive(slot);
            text += project == null ? $"{slot}: idle\n"
                : $"{slot}: {project.Definition.Name} {project.Progress:P0} · paid {project.PaidAntimatter} A + {project.PaidMatter} M\n";
        }
        return text;
    }

    private static Label CreateLabel(string text, int fontSize)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", new Color(0.94f, 0.96f, 1));
        return label;
    }

    private static PanelContainer CreatePanel(string name)
    {
        var panel = new PanelContainer { Name = name, MouseFilter = Control.MouseFilterEnum.Ignore };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.025f, 0.04f, 0.075f, 0.88f),
            BorderColor = new Color(0.22f, 0.42f, 0.5f, 0.7f),
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 10,
            ContentMarginBottom = 10
        };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(6);
        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    private static PanelContainer AddLabelPanel(Control root, string name, Control.LayoutPreset anchor,
        Vector2 offset, Vector2 size, out Label label, int fontSize = 18)
    {
        var panel = CreatePanel(name);
        root.AddChild(panel);
        Place(panel, anchor, offset, size);
        label = CreateLabel(string.Empty, fontSize);
        panel.AddChild(label);
        return panel;
    }

    private static void Place(Control control, Control.LayoutPreset anchor, Vector2 offset, Vector2 size)
    {
        control.SetAnchorsAndOffsetsPreset(anchor);
        control.OffsetLeft = offset.X;
        control.OffsetTop = offset.Y;
        control.OffsetRight = offset.X + size.X;
        control.OffsetBottom = offset.Y + size.Y;
    }
}

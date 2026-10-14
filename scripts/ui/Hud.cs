using Godot;
using MagicCarpetRemastered.Scripts.Game;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Ui;

public partial class Hud : CanvasLayer
{
    private HBoxContainer _vitalBars = null!;
    private readonly ProgressBar[] _gauges = new ProgressBar[3];
    private readonly Label[] _gaugeValues = new Label[3];
    private PanelContainer _baseHealthPanel = null!;
    private ProgressBar _baseHealth = null!;
    private Label _baseHealthText = null!;
    private StyleBoxFlat _healthFill = null!;
    private PanelContainer _capturePanel = null!;
    private Label _captureLabel = null!;
    private ProgressBar _captureProgress = null!;
    private EquipmentBar _equipmentBar = null!;
    private HBoxContainer _actions = null!;
    private Button _baseAction = null!, _boonAction = null!, _mapAction = null!;
    private Control _root = null!;
    private Label _feedback = null!;
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
    private LevelObjective? _levelObjective;
    private PanelContainer _resultPanel = null!;
    private Label _result = null!;
    private ColorRect _hitShade = null!;

    public override void _Ready()
    {
        var root = new Control { Name = "HudRoot", MouseFilter = Control.MouseFilterEnum.Ignore };
        _root = root;
        AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _hitShade = new ColorRect { Name = "HitShade", Color = Colors.Transparent, MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(_hitShade); _hitShade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        BuildVitals(root);
        _equipmentBar = new EquipmentBar(); root.AddChild(_equipmentBar);
        Place(_equipmentBar, Control.LayoutPreset.CenterBottom, new Vector2(-190, -144), new Vector2(380, 126));
        _feedbackPanel = AddLabelPanel(root, "Feedback", Control.LayoutPreset.BottomLeft,
            new Vector2(16, -110), new Vector2(300, 46), out _feedback, 14);
        _feedbackPanel.Visible = false;
        _capturePanel = CreatePanel("CapturePanel"); root.AddChild(_capturePanel);
        Place(_capturePanel, Control.LayoutPreset.CenterBottom, new Vector2(-190, -222), new Vector2(380, 66));
        var captureColumn = new VBoxContainer(); _capturePanel.AddChild(captureColumn);
        _captureLabel = CreateLabel("", 14); captureColumn.AddChild(_captureLabel);
        _captureProgress = new ProgressBar { Name = "CaptureProgress", MaxValue = 1, ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 12), MouseFilter = Control.MouseFilterEnum.Ignore };
        captureColumn.AddChild(_captureProgress); _capturePanel.Visible = false;

        _resultPanel = AddLabelPanel(root, "LevelResult", Control.LayoutPreset.Center,
            new Vector2(-240, -95), new Vector2(480, 190), out _result, 24);
        _result.HorizontalAlignment = HorizontalAlignment.Center;
        _resultPanel.Visible = false;

        _actions = new HBoxContainer { Name = "InterfaceActions", MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(_actions); Place(_actions, Control.LayoutPreset.BottomLeft, new Vector2(16, -46), new Vector2(252, 30));
        _actions.AddThemeConstantOverride("separation", 5);
        _baseAction = ActionButton("BaseAction", "B  Base", () => (GetTree().GetFirstNodeInGroup("base_management") as BaseManagement)?.SetOpen(true));
        _mapAction = ActionButton("MapAction", "M  Map", () => (GetTree().GetFirstNodeInGroup("tactical_map_ui") as TacticalMap)?.SetExpanded(true));
        _boonAction = ActionButton("BoonAction", "H  Boons", () => (GetTree().GetFirstNodeInGroup("boon_choices") as BoonChoices)?.SetOpen(true));
        _actions.AddChild(_baseAction); _actions.AddChild(_mapAction); _actions.AddChild(_boonAction);
        _baseHealthPanel = CreatePanel("BaseHealthPanel"); root.AddChild(_baseHealthPanel);
        using var baseFrame = UiTile.Frame(new Color(0.025f, 0.04f, 0.075f, 0.88f), new Color(0.22f, 0.42f, 0.5f));
        baseFrame.ContentMarginTop = baseFrame.ContentMarginBottom = 3;
        _baseHealthPanel.AddThemeStyleboxOverride("panel", baseFrame);
        _baseHealth = new ProgressBar { Name = "BaseHealth", ShowPercentage = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        _baseHealthPanel.AddChild(_baseHealth);
        using var baseFill = UiTile.Frame(new Color(0.15f, 0.65f, 0.7f), Colors.Transparent, 0);
        _baseHealth.AddThemeStyleboxOverride("fill", baseFill);
        _baseHealthText = CreateLabel("", 11); _baseHealthText.HorizontalAlignment = HorizontalAlignment.Center;
        _baseHealth.AddChild(_baseHealthText); _baseHealthText.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.Resized += LayoutHud;
        LayoutHud();

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
        _player ??= GetTree().GetFirstNodeInGroup("player") as ShipFlightController;
        float hit = _player?.HitFeedbackStrength ?? 0;
        _hitShade.Color = new Color(0.01f, 0.005f, 0.015f, hit * 0.28f);
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
        _levelObjective ??= GetTree().GetFirstNodeInGroup("level_objective") as LevelObjective;
        if (_player == null)
        {
            return;
        }

        RefreshGauge(0, _player.Health, _player.MaximumHealth);
        RefreshGauge(1, _player.Energy, _player.EnergyCapacity);
        RefreshGauge(2, _playerBase?.StoredAntimatter ?? 0, _playerBase?.EffectiveStorageCapacity ?? 0);
        float healthRatio = (float)_player.Health / Mathf.Max(1, _player.MaximumHealth);
        _healthFill.BgColor = healthRatio <= 0.25f ? new Color(0.95f, 0.18f, 0.15f)
            : healthRatio <= 0.5f ? new Color(0.95f, 0.75f, 0.12f) : new Color(0.2f, 0.8f, 0.3f);
        _baseHealth.MaxValue = Mathf.Max(1, _playerBase?.MaxHealth ?? 1); _baseHealth.Value = _playerBase?.Health ?? 0;
        _baseHealthText.Text = $"Base {_playerBase?.Health ?? 0}/{_playerBase?.MaxHealth ?? 0}";
        _resultPanel.Visible = _levelObjective != null && _levelObjective.State != LevelObjective.LevelState.Playing;
        if (_resultPanel.Visible)
        {
            _result.Text = $"{(_levelObjective!.State == LevelObjective.LevelState.Victory ? "VICTORY" : "DEFEAT")}\n"
                + $"{_levelObjective.ResultMessage}\n\nPress R to restart";
        }
        bool livePanel = _player.GameplayInputBlocked;
        StrategicPoint? capture = null;
        foreach (Node node in GetTree().GetNodesInGroup("strategic_point"))
            if (node is StrategicPoint site && !site.IsQueuedForDeletion() && site.PlayerPresent
                && (capture == null || site.CaptureProgress > capture.CaptureProgress)) capture = site;
        _capturePanel.Visible = capture != null && !livePanel && !GetTree().Paused && _player.Health > 0 && !_resultPanel.Visible;
        if (capture != null)
        {
            bool restoring = capture.Claimant == AntimatterOwnership.Enemy;
            string stage = capture.IsContested ? "Contested" : restoring ? "Restoring"
                : capture.Ownership == AntimatterOwnership.Player ? "Owned" : capture.CaptureStage;
            double progress = capture.IsContested ? capture.CaptureProgress
                : restoring ? 1 - capture.CaptureProgress : capture.Ownership == AntimatterOwnership.Player ? 1 : capture.CaptureProgress;
            _captureLabel.Text = $"{capture.Role} · {stage} · {progress:P0}";
            _captureProgress.Value = progress;
        }
        _vitalBars.Visible = _baseHealthPanel.Visible = !livePanel && !GetTree().Paused;
        _actions.Visible = !GetTree().Paused && !livePanel && _player.Health > 0;
        _baseAction.Text = _playerBase is { Health: var hp, MaxHealth: var max } && hp < max ? "B  Base !" : "B  Base";
        _baseAction.TooltipText = "Remote research and base development. Click owned equipment in Ship to equip at home. Gameplay stays live.";
        _mapAction.Visible = GetTree().GetFirstNodeInGroup("tactical_map_ui") != null;
        _mapAction.TooltipText = "World survey / waypoint. Gameplay stays live.";
        _boonAction.Text = _player.Boons.Pending.Count > 0 ? $"H  Boons {_player.Boons.Pending.Count}" : "H  Boons";
        _boonAction.TooltipText = "Select and confirm a secured boon or reject it for antimatter. Gameplay stays live. F3 shows collected bonuses.";
        _equipmentBar.Refresh(); LayoutHud();
        _feedback.Text = _player.Health <= 0 ? $"Pilot down · respawn {_levelObjective?.PlayerRespawnRemaining:0.0}s" : _player.TeleportStatus;
        _feedbackPanel.Visible = !_resultPanel.Visible && !livePanel && !string.IsNullOrEmpty(_feedback.Text);

        if (!_debugPanel.Visible)
        {
            return;
        }

        string details = Section("FLIGHT / PLAYER")
            + $"Camera: {_player.CameraMode}\nSpeed: {_player.Speed:0.0}\nTerrain clearance: {_player.AltitudeAboveTerrain:0.0}\n"
            + $"Storage-backed energy limit: {_player.EnergyCapacity}\nRecharge: {_player.EnergyRechargePerSecond:0.0}/s\nAntimatter collection: collector drone delivery\n{_player.ShieldStatusText}\n\n"
            + Section("EQUIPMENT / MATCH BOONS")
            + _player.EquipmentProgressionText + "\nB researches ranks; install explicitly at home.\n"
            + $"H pending choices · {_player.Boons.Pending.Count} secured rewards\nCollected bonuses:\n{_player.Boons.Summary}\n"
            + "Destroy wrecks; fly within 4 surface units to secure gold salvage.\nBoons survive respawn/tier loss; R resets them.\n\n"
            + Section("ANTIMATTER ECONOMY")
            + (_antimatterTracker == null ? "Tracker unavailable\n" : $"World total: {_antimatterTracker.TotalAntimatter}\nNeutral loose: {_antimatterTracker.NeutralLooseAntimatter}\nNeutral creatures: {_antimatterTracker.NeutralCreatureAntimatter}\nPlayer: {_antimatterTracker.PlayerControlledAntimatter}\nRival: {_antimatterTracker.EnemyControlledAntimatter}\n")
            + $"Extractor supply accepted: {_antimatterTracker?.SiteGeneratedAntimatter ?? 0}\n"
            + (_well == null ? "Source: unavailable\n\n" : "Source: finite neutral landmark\n\n")
            + Section("BASES / ENEMIES")
            + BaseDetails(_playerBase, "Player") + BaseDetails(_enemyBase, "Rival")
            + (_enemyEcosystem?.StatusText ?? "Ecosystem unavailable") + "\n\n"
            + "Rival AI: " + ((GetTree().GetFirstNodeInGroup("rival_ai") as MagicCarpetRemastered.Scripts.Enemies.RivalPilotController)?.StatusText ?? "unavailable") + "\n\n"
            + Section("TERRAIN")
            + (GetParent() is MagicCarpetRemastered.Scripts.Game.Game world ? $"World seed: {world.ActiveWorldSeed}\nR retries this world; F2 creates a fresh world or replays a seed.\n" : string.Empty)
            + (GetParent().GetNodeOrNull<HeightmapArena>("HeightmapArena") is { } seedArena
                ? $"Terrain seed: {seedArena.TerrainSeed} · generation attempts: {seedArena.GenerationAttempts}\n" : string.Empty)
            + _player.TerrainDebugText.Replace("   ", "\n")
            + "\n\n"
            + Section("CONTROLS")
            + "WASD — hover / steer\nSpace — jump (2 current energy, fresh press)\nShift — built-in forward boost (fresh press)\nMouse — look / aim\nV — camera mode\nB — base management (real time)\nM — world map / waypoint (real time, planet)\nEsc — close panel / capture / release mouse\nR — restart arena\n\n"
            + $"1–4 — select equipped slot (no activation)\nLMB — activate {_player.PrimaryEquipmentName}\nTab — cycle installed slots\n"
            + $"RMB — Claim ({_player.ClaimEquipmentEnergyCost} energy): aim at a globe or the ground before it; the bolt follows terrain after landing.\n\n"
            + Section("OWNERSHIP / ROLE GUIDE")
            + "Antimatter: gray neutral · cyan yours · red rival\nDrones haul owned antimatter to bases. Base damage reduces storage; excess spills neutral. Stored antimatter supports equipment power. Pop rivals to spill their cargo.\n\n"
            + "Orange — swarm\nGreen — beast\nBlue — caster\nRed — occasional raider\nYellow — local neutral scavenger\nPurple — rival pilot";
        if (_debugText.Text != details)
        {
            VScrollBar scroll = _debugText.GetVScrollBar();
            double position = scroll.Value;
            _debugText.Text = details;
            scroll.SetDeferred(Godot.Range.PropertyName.Value, position);
        }
    }

    private static string Section(string title) => $"[b][color=#82dbea]{title}[/color][/b]\n";

    private static Button ActionButton(string name, string text, System.Action action)
    {
        var button = new Button { Name = name, Text = text, FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(76, 30) };
        button.AddThemeFontSizeOverride("font_size", 13); button.ButtonDown += action; return button;
    }
    private void LayoutHud()
    {
        if (_actions == null) return;
        Place(_equipmentBar, Control.LayoutPreset.CenterBottom, new Vector2(FlightHudLayout.WeaponsLeft, -186), new Vector2(380, 144));
        Place(_vitalBars, Control.LayoutPreset.CenterBottom, new Vector2(FlightHudLayout.Left, -186), new Vector2(112, 144));
        Place(_actions, Control.LayoutPreset.CenterBottom, new Vector2(FlightHudLayout.WeaponsLeft, -36), new Vector2(380, 28));
        Place(_baseHealthPanel, Control.LayoutPreset.CenterBottom, new Vector2(FlightHudLayout.RadarLeft, -38), new Vector2(176, 30));
        Place(_capturePanel, Control.LayoutPreset.CenterBottom, new Vector2(FlightHudLayout.WeaponsLeft, -264), new Vector2(380, 66));
        Place(_feedbackPanel, Control.LayoutPreset.CenterBottom, new Vector2(FlightHudLayout.WeaponsLeft, -322), new Vector2(380, 46));
    }

    private void BuildVitals(Control root)
    {
        _vitalBars = new HBoxContainer { Name = "VitalBars", MouseFilter = Control.MouseFilterEnum.Ignore };
        _vitalBars.AddThemeConstantOverride("separation", 5); root.AddChild(_vitalBars);
        string[] titles = { "HP", "Energy", "Antimatter" };
        string[] captions = { "H", "E", "A" };
        Color[] colors = { new(0.2f, 0.8f, 0.3f), new(0.15f, 0.5f, 1), new(1, 0.65f, 0.15f) };
        for (int i = 0; i < 3; i++)
        {
            var column = new VBoxContainer { CustomMinimumSize = new Vector2(34, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
            _vitalBars.AddChild(column);
            _gaugeValues[i] = CreateLabel("0", 11); _gaugeValues[i].HorizontalAlignment = HorizontalAlignment.Center;
            column.AddChild(_gaugeValues[i]);
            var center = new CenterContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill }; column.AddChild(center);
            _gauges[i] = new ProgressBar { Name = $"{titles[i]}Gauge", FillMode = (int)ProgressBar.FillModeEnum.BottomToTop,
                ShowPercentage = false, CustomMinimumSize = new Vector2(24, 76), MouseFilter = Control.MouseFilterEnum.Ignore };
            center.AddChild(_gauges[i]);
            var fill = UiTile.Frame(colors[i], Colors.Transparent, 0);
            _gauges[i].AddThemeStyleboxOverride("fill", fill);
            if (i == 0) _healthFill = fill; else fill.Dispose();
            var caption = CreateLabel(captions[i], 11); caption.HorizontalAlignment = HorizontalAlignment.Center; column.AddChild(caption);
        }
    }

    private void RefreshGauge(int index, int value, int maximum)
    {
        _gauges[index].MaxValue = Mathf.Max(1, maximum); _gauges[index].Value = value;
        _gaugeValues[index].Text = $"{value}";
    }
    public override void _ExitTree() => _healthFill?.Dispose();

    private static string BaseDetails(FactionBase? hub, string side) => hub == null
        ? $"{side} base: unavailable\n"
        : $"{side} base: {(hub.IsDestroyed ? "Destroyed" : $"HP {hub.Health}/{hub.MaxHealth}")}\n"
            + $"Storage: {hub.StoredAntimatter}/{hub.EffectiveStorageCapacity} (intact {hub.AntimatterStorageCapacity})  ·  Tier {hub.Tier} / purchased {hub.HighestPurchasedTier}\n"
            + $"Payments: {hub.SpentAntimatter} · Refunds: {hub.RefundedAntimatter} · Investment spill: {hub.InvestmentSpilledAntimatter}\n"
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
                : $"{slot}: {project.Definition.Name} {project.Progress:P0} · paid {project.PaidAntimatter} A\n";
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

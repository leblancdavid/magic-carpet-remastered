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
    private CarpetFlightController? _player;
    private ManaWell? _well;
    private CastleKeep? _playerCastle;
    private CastleKeep? _enemyCastle;
    private EnemyEcosystemDirector? _enemyEcosystem;
    private WorldManaTracker? _manaTracker;
    private LevelObjective? _levelObjective;
    private PanelContainer _resultPanel = null!;
    private Label _result = null!;

    public override void _Ready()
    {
        var root = new Control { Name = "HudRoot", MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(root);
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        AddLabelPanel(root, "Vitals", Control.LayoutPreset.TopLeft,
            new Vector2(20, 20), new Vector2(310, 86), out _vitals);
        AddLabelPanel(root, "Objective", Control.LayoutPreset.TopRight,
            new Vector2(-300, 20), new Vector2(280, 86), out _objective);
        AddLabelPanel(root, "Loadout", Control.LayoutPreset.BottomLeft,
            new Vector2(20, -100), new Vector2(360, 80), out _loadout);
        _feedbackPanel = AddLabelPanel(root, "Feedback", Control.LayoutPreset.BottomLeft,
            new Vector2(20, -164), new Vector2(360, 54), out _feedback);
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

        _player ??= GetTree().GetFirstNodeInGroup("player") as CarpetFlightController;
        _well ??= GetTree().GetFirstNodeInGroup("mana_well") as ManaWell;
        _playerCastle ??= GetTree().GetFirstNodeInGroup("player_castle") as CastleKeep;
        _enemyCastle ??= GetTree().GetFirstNodeInGroup("enemy_castle") as CastleKeep;
        _enemyEcosystem ??= GetTree().GetFirstNodeInGroup("enemy_ecosystem_director") as EnemyEcosystemDirector;
        _manaTracker ??= GetTree().GetFirstNodeInGroup("world_mana_tracker") as WorldManaTracker;
        _levelObjective ??= GetTree().GetFirstNodeInGroup("level_objective") as LevelObjective;
        if (_player == null)
        {
            return;
        }

        _vitals.Text = $"Health  {_player.Health}     Mana  {_player.Mana} / {_player.ManaCapacity}\n"
            + (_player.IsShielded ? _player.ShieldStatusText : _player.IsInvulnerable ? "Protected after hit" : "Carpet ready");
        _objective.Text = _levelObjective == null ? "Eliminate the rival\nDestroy castle and wizard"
            : $"Rival castle: {(_levelObjective.RivalCastleDestroyed ? "Destroyed" : $"{_enemyCastle?.Health} HP")}\n"
                + $"Rival wizard: {(_levelObjective.RivalWizardDefeated ? "Defeated" : "Alive")}";
        _resultPanel.Visible = _levelObjective != null && _levelObjective.State != LevelObjective.LevelState.Playing;
        if (_resultPanel.Visible)
        {
            _result.Text = $"{(_levelObjective!.State == LevelObjective.LevelState.Victory ? "VICTORY" : "DEFEAT")}\n"
                + $"{_levelObjective.ResultMessage}\n\nPress R to restart";
        }
        _loadout.Text = $"LMB  {_player.PrimarySpellName}   ·   Tab cycle\nRMB  {_player.PossessSpellName}   ·   claim mana";
        _feedback.Text = _player.Health <= 0 ? "Defeated — press R to restart" : _player.ActiveFeedbackMessage;
        _feedbackPanel.Visible = !_resultPanel.Visible && !string.IsNullOrEmpty(_feedback.Text);

        if (!_debugPanel.Visible)
        {
            return;
        }

        string details = Section("FLIGHT / PLAYER")
            + $"Camera: {_player.CameraMode}\nSpeed: {_player.Speed:0.0}\nTerrain clearance: {_player.AltitudeAboveTerrain:0.0}\n"
            + $"Claimed power: {_player.ClaimedMana}\nMana collection: balloon delivery\n{_player.ShieldStatusText}\n\n"
            + Section("MANA ECONOMY")
            + (_manaTracker == null ? "Tracker unavailable\n" : $"World total: {_manaTracker.TotalMana}\nNeutral: {_manaTracker.NeutralLooseMana}\nPlayer: {_manaTracker.PlayerControlledMana}\nRival: {_manaTracker.EnemyControlledMana}\n")
            + (_well == null ? "Well: unavailable\n\n" : "Well: finite neutral landmark\n\n")
            + Section("CASTLES / ENEMIES")
            + CastleDetails(_playerCastle, "Player") + CastleDetails(_enemyCastle, "Rival")
            + (_enemyEcosystem?.StatusText.Replace("   Intensity", "\nIntensity") ?? "Ecosystem unavailable") + "\n\n"
            + Section("TERRAIN") + _player.TerrainDebugText.Replace("   ", "\n")
            + "\n\n"
            + Section("CONTROLS")
            + "WASD — fly\nSpace / C — ascend / descend\nMouse — look\nV — camera mode\nEsc — capture / release mouse\nR — restart arena\n\n"
            + $"LMB — {_player.PrimarySpellName}\nTab — cycle quick spell\nQ — {_player.AreaSpellName}\nE — {_player.ShieldSpellName}\nF — {_player.MobilitySpellName}\nG — {_player.SummonSpellName}\n"
            + $"RMB — Possess ({_player.PossessSpellManaCost} mana): aim at a globe or the ground before it; the bolt follows terrain after landing.\n\n"
            + Section("OWNERSHIP / ROLE GUIDE")
            + "Mana: gray neutral · cyan yours · red rival\nBalloons haul owned mana to castles. Pop rivals to spill their cargo.\n\n"
            + "Orange — swarm\nGreen — beast\nBlue — caster\nRed — sieger\nYellow — thief\nPurple — wizard";
        if (_debugText.Text != details)
        {
            VScrollBar scroll = _debugText.GetVScrollBar();
            double position = scroll.Value;
            _debugText.Text = details;
            scroll.SetDeferred(Godot.Range.PropertyName.Value, position);
        }
    }

    private static string Section(string title) => $"[b][color=#82dbea]{title}[/color][/b]\n";

    private static string CastleDetails(CastleKeep? castle, string side) => castle == null
        ? $"{side} castle: unavailable\n"
        : $"{side} castle: {(castle.IsDestroyed ? "Destroyed" : $"HP {castle.Health}/{castle.MaxHealth}")}\n"
            + $"Storage: {castle.StoredMana}/{castle.ManaStorageCapacity}  ·  Stage {castle.GrowthStage}\n\n";

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

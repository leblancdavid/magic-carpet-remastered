using Godot;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Ui;

public partial class UiTile : Button
{
    public UiIcon Glyph { get; } = new();
    public Label Title { get; } = new();
    public Label Detail { get; } = new();
    public Label State { get; } = new();
    public Label TierBadge { get; } = new();
    public ProgressBar BuildProgress { get; } = new();
    public Button CancelButton { get; } = new();
    public bool Compact { get; set; }
    public long ProjectInstanceId { get; private set; }
    public UiTile()
    {
        CustomMinimumSize = new Vector2(154, 114); SizeFlagsHorizontal = SizeFlags.ExpandFill;
        FocusMode = FocusModeEnum.None; MouseFilter = MouseFilterEnum.Stop;
    }
    public override void _Ready()
    {
        Style("normal", new Color(0.08f, 0.14f, 0.19f), new Color(0.25f, 0.5f, 0.6f));
        Style("hover", new Color(0.12f, 0.23f, 0.29f), new Color(0.5f, 0.9f, 1));
        Style("pressed", new Color(0.12f, 0.28f, 0.32f), new Color(0.6f, 1, 1));
        Style("disabled", new Color(0.05f, 0.075f, 0.1f), new Color(0.2f, 0.28f, 0.32f));
        BuildProgress.Name = "BuildProgress"; BuildProgress.MaxValue = 1; BuildProgress.ShowPercentage = false;
        BuildProgress.MouseFilter = MouseFilterEnum.Ignore; BuildProgress.Visible = false;
        AddChild(BuildProgress); BuildProgress.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        using var progressBackground = new StyleBoxEmpty(); BuildProgress.AddThemeStyleboxOverride("background", progressBackground);
        using var progressFill = Frame(new Color(0.12f, 0.6f, 0.7f, 0.3f), Colors.Transparent, 0);
        BuildProgress.AddThemeStyleboxOverride("fill", progressFill);
        var inset = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore }; AddChild(inset);
        inset.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (string edge in new[] { "left", "right", "top", "bottom" }) inset.AddThemeConstantOverride($"margin_{edge}", 7);
        var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore }; inset.AddChild(column);
        column.AddThemeConstantOverride("separation", 2);
        if (!Compact)
        {
            var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore }; column.AddChild(center); center.AddChild(Glyph);
        }
        foreach (Label label in new[] { Title, Detail, State })
        {
            label.MouseFilter = MouseFilterEnum.Ignore; label.HorizontalAlignment = HorizontalAlignment.Center;
            label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            label.AddThemeFontSizeOverride("font_size", label == Title ? 14 : 12); column.AddChild(label);
        }
        TierBadge.Name = "RequiredTier"; TierBadge.MouseFilter = MouseFilterEnum.Ignore;
        TierBadge.AddThemeFontSizeOverride("font_size", 12); TierBadge.HorizontalAlignment = HorizontalAlignment.Right;
        AddChild(TierBadge); TierBadge.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        TierBadge.OffsetLeft = -40; TierBadge.OffsetRight = -7; TierBadge.OffsetTop = 5; TierBadge.OffsetBottom = 22;
        CancelButton.Text = "×"; CancelButton.FocusMode = FocusModeEnum.None; CancelButton.Visible = false;
        CancelButton.CustomMinimumSize = new Vector2(24, 24); AddChild(CancelButton);
        CancelButton.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        CancelButton.OffsetLeft = -27; CancelButton.OffsetRight = -3; CancelButton.OffsetTop = -27; CancelButton.OffsetBottom = -3;
    }
    public void SetProject(ConstructionProject? project, int requiredTier = 0, bool unavailable = false)
    {
        ProjectInstanceId = project?.InstanceId ?? 0;
        BuildProgress.Visible = project != null; BuildProgress.Value = project?.Progress ?? 0;
        CancelButton.Visible = project != null;
        TierBadge.Text = requiredTier > 0 ? BaseManagement.Rank(requiredTier) : "";
        SelfModulate = unavailable && project == null ? new Color(1, 1, 1, 0.55f) : Colors.White;
        Title.Modulate = Detail.Modulate = unavailable && project == null ? new Color(1, 1, 1, 0.65f) : Colors.White;
    }
    public void Display(string title, string detail, string state, string tooltip, bool disabled)
    {
        Title.Text = title; Detail.Text = detail; State.Text = state; TooltipText = tooltip; Disabled = disabled;
        Glyph.Modulate = disabled ? new Color(0.5f, 0.58f, 0.62f) : Colors.White;
        State.Modulate = disabled ? new Color(0.7f, 0.73f, 0.76f) : new Color(0.5f, 0.9f, 0.9f);
    }
    public override Control _MakeCustomTooltip(string forText)
    {
        var panel = new PanelContainer();
        using var frame = Frame(new Color(0.035f, 0.06f, 0.09f), new Color(0.4f, 0.7f, 0.8f)); panel.AddThemeStyleboxOverride("panel", frame);
        var label = new Label { Text = forText, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(310, 0) };
        label.AddThemeFontSizeOverride("font_size", 14); panel.AddChild(label); return panel;
    }
    public override void _ExitTree() { if (Compact && GodotObject.IsInstanceValid(Glyph)) Glyph.Free(); }
    private void Style(string name, Color background, Color border)
    { using var frame = Frame(background, border); AddThemeStyleboxOverride(name, frame); }
    public static StyleBoxFlat Frame(Color background, Color border, int width = 1)
    {
        var style = new StyleBoxFlat { BgColor = background, BorderColor = border,
            ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 6, ContentMarginBottom = 6 };
        style.SetBorderWidthAll(width); style.SetCornerRadiusAll(4); return style;
    }
}

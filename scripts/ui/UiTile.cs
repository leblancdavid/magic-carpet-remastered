using Godot;

namespace MagicCarpetRemastered.Scripts.Ui;

public partial class UiTile : Button
{
    public UiIcon Glyph { get; } = new();
    public Label Title { get; } = new();
    public Label Detail { get; } = new();
    public Label State { get; } = new();
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
        var inset = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore }; AddChild(inset);
        inset.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (string edge in new[] { "left", "right", "top", "bottom" }) inset.AddThemeConstantOverride($"margin_{edge}", 7);
        var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore }; inset.AddChild(column);
        column.AddThemeConstantOverride("separation", 2);
        var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore }; column.AddChild(center); center.AddChild(Glyph);
        foreach (Label label in new[] { Title, Detail, State })
        {
            label.MouseFilter = MouseFilterEnum.Ignore; label.HorizontalAlignment = HorizontalAlignment.Center;
            label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            label.AddThemeFontSizeOverride("font_size", label == Title ? 14 : 12); column.AddChild(label);
        }
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
    private void Style(string name, Color background, Color border)
    { using var frame = Frame(background, border); AddThemeStyleboxOverride(name, frame); }
    public static StyleBoxFlat Frame(Color background, Color border, int width = 1)
    {
        var style = new StyleBoxFlat { BgColor = background, BorderColor = border,
            ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 6, ContentMarginBottom = 6 };
        style.SetBorderWidthAll(width); style.SetCornerRadiusAll(4); return style;
    }
}

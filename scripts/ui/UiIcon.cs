using Godot;

namespace MagicCarpetRemastered.Scripts.Ui;

// Original line glyphs; shared by catalog tiles and the HUD, without imported art.
public partial class UiIcon : Control
{
    private string _id = "empty";
    public string IconId { get => _id; set { if (_id == value) return; _id = value; QueueRedraw(); } }
    public Color Ink { get; set; } = new(0.48f, 0.87f, 0.97f);
    public UiIcon() { CustomMinimumSize = new Vector2(32, 32); MouseFilter = MouseFilterEnum.Ignore; }
    public override void _Draw()
    {
        float scale = Mathf.Min(Size.X, Size.Y) / 32;
        DrawSetTransform((Size - Vector2.One * 32 * scale) * 0.5f, 0, Vector2.One * scale);
        void Line(float x, float y, float a, float b) => DrawLine(new Vector2(x, y), new Vector2(a, b), Ink, 2, true);
        void Ring(float radius) => DrawArc(new Vector2(16, 16), radius, 0, Mathf.Tau, 32, Ink, 2, true);
        switch (_id)
        {
            case "plasma-bolt": Ring(5); Line(4, 16, 11, 16); Line(21, 16, 28, 16); Line(22, 10, 28, 16); Line(22, 22, 28, 16); break;
            case "pulse-burst":
                Ring(7); for (int i = 0; i < 8; i++) { Vector2 v = Vector2.Right.Rotated(i * Mathf.Tau / 8); DrawLine(new Vector2(16, 16) + v * 10, new Vector2(16, 16) + v * 14, Ink, 2, true); } break;
            case "energy-shield": DrawPolyline(new[] { new Vector2(6, 7), new Vector2(16, 3), new Vector2(26, 7), new Vector2(24, 21), new Vector2(16, 29), new Vector2(8, 21), new Vector2(6, 7) }, Ink, 2, true); Line(16, 9, 16, 21); break;
            case "dash": Line(5, 7, 14, 16); Line(14, 16, 5, 25); Line(15, 7, 24, 16); Line(24, 16, 15, 25); break;
            case "guardian-drone": DrawRect(new Rect2(11, 11, 10, 10), Ink, false, 2); Line(5, 5, 11, 11); Line(27, 5, 21, 11); Line(5, 27, 11, 21); Line(27, 27, 21, 21); Ring(13); break;
            case "repair-matrix": DrawRect(new Rect2(5, 5, 22, 22), Ink, false, 2); Line(16, 9, 16, 23); Line(9, 16, 23, 16); break;
            case "energy-grid": DrawPolyline(new[] { new Vector2(19, 3), new Vector2(8, 17), new Vector2(16, 17), new Vector2(13, 29), new Vector2(25, 13), new Vector2(17, 13), new Vector2(19, 3) }, Ink, 2, true); break;
            case "collector-drive": DrawRect(new Rect2(9, 8, 14, 16), Ink, false, 2); Line(3, 12, 7, 12); Line(3, 20, 7, 20); Line(25, 12, 29, 12); Line(25, 20, 29, 20); Line(12, 28, 20, 28); break;
            case "claim-range": Ring(9); Line(16, 2, 16, 10); Line(16, 22, 16, 30); Line(2, 16, 10, 16); Line(22, 16, 30, 16); break;
            case "strike-drone": Line(16, 3, 27, 24); Line(27, 24, 16, 20); Line(16, 20, 5, 24); Line(5, 24, 16, 3); Line(12, 25, 12, 30); Line(20, 25, 20, 30); break;
            case "base": DrawRect(new Rect2(5, 15, 22, 13), Ink, false, 2); DrawRect(new Rect2(11, 6, 10, 9), Ink, false, 2); Line(16, 2, 16, 6); break;
            default: DrawRect(new Rect2(6, 6, 20, 20), new Color(Ink, 0.4f), false, 1); Line(12, 16, 20, 16); break;
        }
    }
    public static string ForProject(string id)
    {
        if (id.StartsWith("base-tier-")) return "base";
        if (!id.StartsWith("equipment-")) return id;
        return id.Substring(10, id.Length - 12);
    }
    public static string ShortName(string id) => id switch
    { "plasma-bolt" => "Plasma", "pulse-burst" => "Burst", "energy-shield" => "Shield", "dash" => "Dash", "guardian-drone" => "Guardian",
        "repair-matrix" => "Repair matrix", "energy-grid" => "Energy grid", "collector-drive" => "Collector drive", "claim-range" => "Claim range", "strike-drone" => "Strike drone", _ => id };
}

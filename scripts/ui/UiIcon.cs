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
            case "teleport-home": Ring(12); Line(7, 17, 16, 9); Line(16, 9, 25, 17); DrawRect(new Rect2(11, 17, 10, 9), Ink, false, 2); break;
            case "siphon-drone": Ring(8); Line(3, 16, 12, 16); Line(20, 16, 29, 16); Line(24, 11, 29, 16); Line(24, 21, 29, 16); break;
            case "harvester-drone": Ring(5); Line(3, 5, 16, 27); Line(16, 27, 29, 5); Line(5, 28, 27, 28); break;
            case "scatter-cannon": Line(4, 16, 14, 16); Line(14, 16, 27, 5); Line(14, 16, 29, 16); Line(14, 16, 27, 27); break;
            case "seeker-missile": Ring(12); Line(7, 25, 23, 9); Line(23, 9, 23, 18); Line(14, 9, 23, 9); break;
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
            case "scout-drone": Ring(7); Ring(12); Line(16, 16, 27, 6); Line(4, 27, 10, 22); break;
            case "attack-drone": Line(16, 3, 28, 25); Line(28, 25, 16, 20); Line(16, 20, 4, 25); Line(4, 25, 16, 3); Line(16, 11, 16, 25); break;
            case "defensive-drone": Ring(10); DrawRect(new Rect2(12, 10, 8, 12), Ink, false, 2); Line(16, 5, 16, 10); Line(16, 22, 16, 28); break;
            case "drone-command-relay": Line(16, 8, 16, 28); Line(9, 28, 23, 28); DrawArc(new Vector2(16, 14), 10, -Mathf.Pi * 0.85f, -Mathf.Pi * 0.15f, 12, Ink, 2, true); Ring(3); break;
            case "pulse-turret": DrawRect(new Rect2(7, 21, 18, 7), Ink, false, 2); Line(16, 21, 16, 10); Line(16, 10, 27, 4); Line(19, 14, 29, 8); break;
            case "cargo-compression": DrawRect(new Rect2(9, 9, 14, 14), Ink, false, 2); Line(2, 16, 8, 16); Line(24, 16, 30, 16); Line(16, 2, 16, 8); Line(16, 24, 16, 30); break;
            case "scoop-array": DrawArc(new Vector2(16, 10), 12, 0, Mathf.Pi, 20, Ink, 2, true); Line(8, 29, 8, 20); Line(24, 29, 24, 20); Ring(3); break;
            case "base-armor":
            case "collector-armor": DrawPolyline(new[] { new Vector2(5, 8), new Vector2(16, 3), new Vector2(27, 8), new Vector2(24, 24), new Vector2(16, 29), new Vector2(8, 24), new Vector2(5, 8) }, Ink, 2, true); DrawRect(new Rect2(11, 11, 10, 10), Ink, false, 2); break;
            case "site-fortification": Ring(12); Line(8, 21, 8, 11); Line(16, 21, 16, 7); Line(24, 21, 24, 11); Line(5, 23, 27, 23); break;
            case "dock-automation": DrawRect(new Rect2(4, 21, 24, 7), Ink, false, 2); Line(16, 3, 16, 19); Line(10, 13, 16, 19); Line(16, 19, 22, 13); break;
            case "extractor-optimization": Ring(10); Line(16, 3, 16, 11); Line(16, 21, 16, 30); Line(3, 16, 11, 16); Line(21, 16, 30, 16); Ring(4); break;
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
        "scatter-cannon" => "Scatter", "seeker-missile" => "Seeker",
        "repair-matrix" => "Repair matrix", "energy-grid" => "Energy grid", "collector-drive" => "Collector drive", "claim-range" => "Claim magnetism", "strike-drone" => "Strike drone", _ => id };
}

using Godot;

namespace MagicCarpetRemastered.Scripts.Ui;

// Shared by the control-based HUD and the drawn radar.
public static class FlightHudLayout
{
    public const float Left = -346;
    public const float WeaponsLeft = -222;
    public const float RadarLeft = 170;
    public static Vector2 RadarOrigin(Vector2 viewport) => new(viewport.X * 0.5f + RadarLeft, viewport.Y - 222);
}

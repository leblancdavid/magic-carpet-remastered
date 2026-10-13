using Godot;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Ui;

public partial class TacticalMapCanvas : Control
{
    public WorldNavigation Navigation { get; set; } = null!;
    public bool Expanded { get; set; }
    private float _timer;
    private Font _font = null!;
    private string _targetTitle = "";
    private string _targetGuidance = "";
    private readonly Vector2[] _diamond = new Vector2[4];
    private readonly Vector2[] _diamondOutline = new Vector2[5];
    private readonly Vector2[] _arrow = new Vector2[3];
    private static readonly Color PanelColor = new(0.025f, 0.045f, 0.07f, 0.94f);
    private static readonly Color LineColor = new(0.3f, 0.5f, 0.62f, 0.65f);
    private static readonly Color Gold = new(1, 0.8f, 0.2f);

    public Rect2 SurveyRect
    {
        get
        {
            float width = Mathf.Max(240, Mathf.Min(Size.X - 120, (Size.Y - 260) * 2));
            return new Rect2(new Vector2((Size.X - width) * 0.5f, 140), new Vector2(width, width * 0.5f));
        }
    }

    public override void _Ready() => _font = GetThemeDefaultFont();

    public override void _Process(double delta)
    {
        _timer -= (float)delta;
        bool refreshText = _timer <= 0;
        if (refreshText && Navigation != null && GodotObject.IsInstanceValid(Navigation.Player))
        {
            _timer = 0.1f + Mathf.Max(_timer, -0.1f);
            _targetTitle = Navigation.TargetName.Length > 24 ? Navigation.TargetName[..21] + "…" : Navigation.TargetName;
            _targetGuidance = $"{Navigation.TargetDistance:0} m · {Navigation.BearingText()}";
        }
        // Survey hover/text updates remain low frequency; gameplay stays live.
        // Flight-relative radar positions must follow every rendered frame.
        if (Expanded && !refreshText) return;
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Expanded || @event is not InputEventMouseButton { Pressed: true } mouse) return;
        if (mouse.ButtonIndex == MouseButton.Right) Navigation.ClearWaypoint();
        else if (mouse.ButtonIndex == MouseButton.Left) SelectDestination(mouse.Position);
        AcceptEvent();
        QueueRedraw();
    }

    public bool SelectDestination(Vector2 position)
    {
        if (!Expanded || !SurveyRect.HasPoint(position)) return false;
        MapMarker? nearest = null;
        float best = 12 * 12;
        foreach (MapMarker marker in Navigation.Markers)
        {
            if (marker.Kind == MapMarkerKind.Player || !Navigation.CanDisplay(marker) || !marker.TryGetLiveUp(Navigation.Arena, out Vector3 up)) continue;
            Vector2 projected = SurveyRect.Position + MapProjection.Project(up) * SurveyRect.Size;
            float distance = projected.DistanceSquaredTo(position);
            if (distance < best) { best = distance; nearest = marker; }
        }
        if (nearest != null && nearest.TryGetLiveUp(Navigation.Arena, out Vector3 destination)) Navigation.SetWaypoint(destination, Navigation.VisibleTitle(nearest).Split('·', '—')[0].Trim());
        else Navigation.SetWaypoint(MapProjection.Unproject((position - SurveyRect.Position) / SurveyRect.Size));
        return true;
    }

    public override void _Draw()
    {
        if (Navigation == null || !GodotObject.IsInstanceValid(Navigation.Player)) return;
        if (Expanded) DrawSurvey();
        else if (!Navigation.Player.GameplayInputBlocked && !GetTree().Paused) DrawRadar();
    }

    private void DrawRadar()
    {
        DrawRect(new Rect2(16, 124, 196, 224), PanelColor);
        Text(new Vector2(28, 146), $"RADAR · {WorldNavigation.RadarRange:0} m", Colors.White, 13);
        Vector2 center = new(114, 227);
        const float radius = 72;
        DrawCircle(center, radius, new Color(0.05f, 0.1f, 0.14f));
        DrawArc(center, radius, 0, Mathf.Tau, 64, LineColor, 1.5f, true);
        DrawArc(center, radius * 0.5f, 0, Mathf.Tau, 48, LineColor.Darkened(0.4f), 1, true);
        DrawLine(center - Vector2.Right * radius, center + Vector2.Right * radius, LineColor.Darkened(0.4f));
        DrawLine(center - Vector2.Down * radius, center + Vector2.Down * radius, LineColor.Darkened(0.4f));
        Basis playerFrame = Navigation.Player.GlobalBasis;
        foreach (MapMarker marker in Navigation.Markers)
        {
            if (marker.Kind == MapMarkerKind.Player || !Navigation.CanDisplay(marker)) continue;
            if (!marker.TryGetLiveUp(Navigation.Arena, out Vector3 up)) continue;
            Vector2 offset = MapProjection.LocalOffset(up, playerFrame, Navigation.Arena.PlanetRadius);
            bool outside = offset.Length() > WorldNavigation.RadarRange;
            if (outside && !marker.AlwaysVisible) continue;
            Vector2 point = center + offset.LimitLength(WorldNavigation.RadarRange) / WorldNavigation.RadarRange * radius;
            Glyph(point, marker, outside ? 3 : 4);
        }
        Vector2 target = MapProjection.LocalOffset(Navigation.TargetUp, playerFrame, Navigation.Arena.PlanetRadius);
        DrawLine(center, center + target.LimitLength(WorldNavigation.RadarRange) / WorldNavigation.RadarRange * radius, new Color(1, 0.8f, 0.2f, 0.5f), 1.5f);
        Cross(center + target.LimitLength(WorldNavigation.RadarRange) / WorldNavigation.RadarRange * radius, Gold);
        Arrow(center, Vector2.Up, Colors.White);
        Text(new Vector2(86, 169), "FORWARD", new Color(0.6f, 0.7f, 0.8f), 10);
        Text(new Vector2(28, 319), _targetTitle, Gold, 13, 172);
        Text(new Vector2(28, 339), _targetGuidance, Colors.White, 12, 172);
    }

    private void DrawSurvey()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.015f, 0.025f, 0.045f, 0.97f));
        Rect2 rect = SurveyRect;
        Text(new Vector2(rect.Position.X, 78), "TACTICAL RADAR / WORLD SURVEY", Colors.White, 26);
        Text(new Vector2(rect.Position.X, 108), "M / Esc close  ·  Left click waypoint  ·  Right click / Delete clear  ·  R restart  ·  REAL TIME", new Color(0.7f, 0.8f, 0.9f), 15);
        DrawTextureRect(Navigation.SurveyTexture, rect, false);
        for (int i = 1; i < 12; i++) DrawLine(rect.Position + new Vector2(rect.Size.X * i / 12, 0), rect.Position + new Vector2(rect.Size.X * i / 12, rect.Size.Y), LineColor.Darkened(0.5f));
        for (int i = 1; i < 6; i++) DrawLine(rect.Position + new Vector2(0, rect.Size.Y * i / 6), rect.Position + new Vector2(rect.Size.X, rect.Size.Y * i / 6), LineColor.Darkened(0.5f));
        DrawRect(rect, LineColor, false, 2);
        MapMarker? hover = null;
        float nearest = 15 * 15;
        Vector2 mouse = GetLocalMousePosition();
        foreach (MapMarker marker in Navigation.Markers)
        {
            if (!Navigation.CanDisplay(marker) || !marker.TryGetLiveUp(Navigation.Arena, out Vector3 up)) continue;
            Vector2 point = rect.Position + MapProjection.Project(up) * rect.Size;
            if (marker.Kind == MapMarkerKind.Player)
            {
                Vector3 nextUp = Navigation.Arena.UpAt(Navigation.Player.GlobalPosition - Navigation.Player.GlobalBasis.Z * 5);
                Vector2 heading = MapProjection.WrappedDifference(MapProjection.Project(nextUp), MapProjection.Project(marker.Up)) * rect.Size;
                Arrow(point, heading.LengthSquared() > 0.00001f ? heading.Normalized() : Vector2.Up, marker.Color);
            }
            else Glyph(point, marker, marker.Kind is MapMarkerKind.Antimatter or MapMarkerKind.Matter or MapMarkerKind.Enemy ? 3 : 6);
            float distance = point.DistanceSquaredTo(mouse);
            if (distance < nearest) { nearest = distance; hover = marker; }
        }
        Cross(rect.Position + MapProjection.Project(Navigation.TargetUp) * rect.Size, Gold);
        float bottom = rect.End.Y + 26;
        Text(new Vector2(rect.Position.X, bottom), "■ Base   □ Wreck   ◇ Landmark / drone   ● Mob: black · Antimatter: orange · Matter: gray · Factions: cyan/red", new Color(0.8f, 0.87f, 0.95f), 14);
        Text(new Vector2(rect.Position.X, bottom + 27), $"Target: {Navigation.TargetName} · {Navigation.TargetDistance:0} m · {Navigation.BearingText()}", Gold, 16);
        Text(new Vector2(rect.Position.X, bottom + 53), hover != null ? Navigation.VisibleTitle(hover) : "Economic sites: ring/cross markers. Hover for role/owner/benefit. Remain in ring to capture; opposing pilots contest.", Colors.White, 14);
    }

    private void Glyph(Vector2 point, MapMarker marker, float size)
    {
        if (marker.Kind is MapMarkerKind.Home or MapMarkerKind.RivalBase)
            DrawRect(new Rect2(point - Vector2.One * size, Vector2.One * size * 2), marker.Color);
        else if (marker.Kind == MapMarkerKind.StrategicPoint)
        {
            DrawCircle(point, size + 2, marker.Color, false, 1.5f);
            DrawLine(point - Vector2.Right * size, point + Vector2.Right * size, marker.Color, 1.5f);
            DrawLine(point - Vector2.Down * size, point + Vector2.Down * size, marker.Color, 1.5f);
        }
        else if (marker.Kind == MapMarkerKind.Wreck)
            DrawRect(new Rect2(point - Vector2.One * size, Vector2.One * size * 2), marker.Color, false, 1.5f);
        else if (marker.Kind is MapMarkerKind.Matter or MapMarkerKind.Antimatter || marker.Actor is MagicCarpetRemastered.Scripts.Enemies.EnemyEcosystemEnemy)
        {
            if (marker.Color == Colors.Black) DrawCircle(point, size + 1, Colors.LightGray);
            DrawCircle(point, size, marker.Color);
        }
        else if (marker.Kind is MapMarkerKind.Landmark or MapMarkerKind.Upgrade or MapMarkerKind.Drone or MapMarkerKind.Reward)
        {
            _diamond[0] = point + Vector2.Up * size;
            _diamond[1] = point + Vector2.Right * size;
            _diamond[2] = point + Vector2.Down * size;
            _diamond[3] = point + Vector2.Left * size;
            if (marker.Charted) DrawColoredPolygon(_diamond, marker.Color);
            else
            {
                System.Array.Copy(_diamond, _diamondOutline, 4);
                _diamondOutline[4] = _diamond[0];
                DrawPolyline(_diamondOutline, marker.Color, 1.5f, true);
            }
        }
        else if (marker.Kind == MapMarkerKind.Enemy) Arrow(point, Vector2.Up, marker.Color);
        else DrawCircle(point, size, marker.Color);
    }

    private void Cross(Vector2 point, Color color)
    {
        DrawArc(point, 9, 0, Mathf.Tau, 24, color, 1.5f, true);
        DrawLine(point - Vector2.Right * 12, point + Vector2.Right * 12, color);
        DrawLine(point - Vector2.Down * 12, point + Vector2.Down * 12, color);
    }

    private void Arrow(Vector2 point, Vector2 direction, Color color)
    {
        Vector2 side = new(-direction.Y, direction.X);
        _arrow[0] = point + direction * 8;
        _arrow[1] = point - direction * 5 + side * 5;
        _arrow[2] = point - direction * 5 - side * 5;
        DrawColoredPolygon(_arrow, color);
    }

    private void Text(Vector2 point, string text, Color color, int size, float width = -1)
        => DrawString(_font, point, text, HorizontalAlignment.Left, width, size, color);
}

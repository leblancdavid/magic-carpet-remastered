using Godot;
using System.Collections.Generic;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;

namespace MagicCarpetRemastered.Scripts.World;

public enum MapMarkerKind { Player, Home, RivalBase, Landmark, Upgrade, Antimatter, Drone, Enemy }
public sealed record MapMarker(Vector3 Up, string Title, MapMarkerKind Kind, Color Color, bool Charted = true)
{
    public Node3D? Actor { get; init; }

    // Membership/metadata is sampled slowly; visible actors move at render cadence.
    public bool TryGetLiveUp(HeightmapArena arena, out Vector3 up)
    {
        up = Up;
        if (Actor == null) return true;
        if (!GodotObject.IsInstanceValid(Actor) || Actor.IsQueuedForDeletion() || !Actor.IsInsideTree()) return false;
        if (Actor is AntimatterPickup orb && !orb.IsAvailable) return false;
        if (Actor is EnemyEcosystemEnemy enemy && enemy.Health <= 0) return false;
        if (Actor is CollectorDrone drone && drone.Health <= 0) return false;
        if (Actor is EquipmentDiscovery discovery && discovery.IsCollected) return false;
        up = arena.UpAt(Actor.GlobalPosition);
        return true;
    }
}

public partial class WorldNavigation : Node
{
    public const float LocalRevealRange = 120;
    public const float RadarRange = 220;
    public HeightmapArena Arena { get; private set; } = null!;
    public ShipFlightController Player { get; private set; } = null!;
    public ImageTexture SurveyTexture { get; private set; } = null!;
    public IReadOnlyList<MapMarker> Markers => _markers;
    public Vector3? WaypointUp { get; private set; }
    public string WaypointName { get; private set; } = "";
    public Vector3 TargetUp => WaypointUp ?? _homeUp;
    public string TargetName => WaypointUp.HasValue ? WaypointName : "Home base";
    public float TargetDistance => MapProjection.LocalOffset(TargetUp, Player.GlobalBasis, Arena.PlanetRadius).Length();
    private readonly List<MapMarker> _markers = new();
    private readonly List<WorldLandmark> _landmarks = new();
    private Vector3 _homeUp = Vector3.Up;
    private float _timer;

    public override void _Ready()
    {
        Arena = (HeightmapArena)GetTree().GetFirstNodeInGroup("heightmap_arena");
        Player = (ShipFlightController)GetTree().GetFirstNodeInGroup("player");
        AddToGroup("world_navigation");
        BuildSurvey();
        Refresh();
    }

    public override void _Process(double delta)
    {
        _timer -= (float)delta;
        if (_timer > 0) return;
        _timer = 0.2f + Mathf.Max(_timer, -0.2f);
        Refresh();
    }

    public Vector3 WorldPosition(Vector3 up) => Arena.OnSurface(Arena.PlanetCenter + up * Arena.PlanetRadius);

    public void SetWaypoint(Vector3 up, string title = "Waypoint")
    {
        if (!up.IsFinite() || up.LengthSquared() < 0.000001f) return;
        WaypointUp = up.Normalized();
        WaypointName = title;
    }

    public void ClearWaypoint()
    {
        WaypointUp = null;
        WaypointName = "";
    }

    public string BearingText()
    {
        if (TargetDistance < 8) return "Arrived";
        Vector2 offset = MapProjection.LocalOffset(TargetUp, Player.GlobalBasis, Arena.PlanetRadius);
        float angle = Mathf.RadToDeg(Mathf.Atan2(offset.X, -offset.Y));
        if (Mathf.Abs(angle) < 8) return "Ahead";
        return $"{(angle < 0 ? "Left" : "Right")} {Mathf.Abs(angle):0}°";
    }

    public void Refresh()
    {
        _markers.Clear();
        _landmarks.Clear();
        _markers.Add(new MapMarker(Arena.UpAt(Player.GlobalPosition), "You", MapMarkerKind.Player, Colors.White) { Actor = Player });
        foreach (string group in new[] { "player_base", "enemy_base" })
            foreach (Node node in GetTree().GetNodesInGroup(group))
            {
                if (node is not FactionBase hub || hub.IsQueuedForDeletion()) continue;
                if (hub.IsPlayerBase) _homeUp = Arena.UpAt(hub.GlobalPosition);
                Add(hub, hub.IsPlayerBase ? "Home" : "Rival base",
                    hub.IsPlayerBase ? MapMarkerKind.Home : MapMarkerKind.RivalBase,
                    hub.IsPlayerBase ? new Color(0.15f, 0.85f, 1) : new Color(1, 0.25f, 0.3f),
                    hub.IsDestroyed ? "Ruins" : $"{hub.Health} HP · {hub.StoredAntimatter} antimatter");
            }
        foreach (Node node in GetTree().GetNodesInGroup("world_landmark"))
        {
            if (node is not WorldLandmark landmark || landmark.IsQueuedForDeletion()) continue;
            _landmarks.Add(landmark);
            _markers.Add(new MapMarker(Arena.UpAt(landmark.GlobalPosition), landmark.IsDiscovered
                ? $"{landmark.Title} — {landmark.Description}" : "Uncharted landmark — fly within 65 m to identify",
                MapMarkerKind.Landmark, landmark.IsDiscovered ? landmark.LandmarkColor : new Color(0.6f, 0.65f, 0.72f), landmark.IsDiscovered));
        }
        foreach (Node node in GetTree().GetNodesInGroup("equipment_discovery"))
        {
            if (node is not EquipmentDiscovery discovery || discovery.IsQueuedForDeletion() || discovery.IsCollected) continue;
            bool revealed = Nearby(discovery);
            foreach (var landmark in _landmarks)
                if (landmark.IsDiscovered && Arena.SurfaceDistance(landmark.GlobalPosition, discovery.GlobalPosition) < 30) revealed = true;
            if (revealed) Add(discovery, $"{discovery.EquipmentName} II", MapMarkerKind.Upgrade, new Color(1, 0.8f, 0.2f));
        }
        foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter"))
        {
            if (node is not AntimatterPickup orb || !orb.IsAvailable || (orb.Ownership != AntimatterOwnership.Player && !Nearby(orb))) continue;
            Color color = orb.Ownership switch
            {
                AntimatterOwnership.Player => new Color(0.15f, 0.85f, 1),
                AntimatterOwnership.Enemy => new Color(1, 0.3f, 0.4f),
                _ => new Color(0.85f, 0.88f, 0.95f)
            };
            Add(orb, $"{orb.Ownership} antimatter · {orb.AntimatterAmount}", MapMarkerKind.Antimatter, color);
        }
        foreach (Node node in GetTree().GetNodesInGroup("drone"))
        {
            if (node is not CollectorDrone drone || drone.IsQueuedForDeletion() || drone.Health <= 0 || (!drone.UsePlayerAntimatter && !Nearby(drone))) continue;
            Add(drone, drone.UsePlayerAntimatter ? "Your drone" : "Rival drone", MapMarkerKind.Drone,
                drone.UsePlayerAntimatter ? new Color(0.15f, 0.85f, 1) : new Color(1, 0.3f, 0.4f), $"{drone.CarriedCount} cargo globes");
        }
        foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
            if (node is EnemyEcosystemEnemy enemy && !enemy.IsQueuedForDeletion() && enemy.Health > 0 && Nearby(enemy))
                Add(enemy, enemy.RoleText, MapMarkerKind.Enemy, new Color(1, 0.5f, 0.25f));
    }

    private bool Nearby(Node3D actor) => Arena.SurfaceDistance(Player.GlobalPosition, actor.GlobalPosition) <= LocalRevealRange;

    private void Add(Node3D actor, string title, MapMarkerKind kind, Color color, string detail = "")
        => _markers.Add(new MapMarker(Arena.UpAt(actor.GlobalPosition), string.IsNullOrEmpty(detail) ? title : $"{title} · {detail}", kind, color) { Actor = actor });

    private void BuildSurvey()
    {
        // A coarse starting survey generated once during loading, not per draw.
        using var image = Image.CreateEmpty(192, 96, false, Image.Format.Rgb8);
        for (int y = 0; y < 96; y++)
        for (int x = 0; x < 192; x++)
        {
            Vector3 up = MapProjection.Unproject(new Vector2((x + 0.5f) / 192, (y + 0.5f) / 96));
            float height = Arena.HeightAt(Arena.PlanetCenter + up * Arena.PlanetRadius);
            Color color = height <= Arena.WaterHeight ? new Color(0.055f, 0.16f, 0.25f)
                : height < 1 ? new Color(0.42f, 0.36f, 0.22f)
                : height < 8 ? new Color(0.15f, 0.3f, 0.19f) : new Color(0.3f, 0.38f, 0.27f);
            image.SetPixel(x, y, color);
        }
        SurveyTexture = ImageTexture.CreateFromImage(image);
    }
}

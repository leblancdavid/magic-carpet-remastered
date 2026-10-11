using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;

namespace MagicCarpetRemastered.Scripts.World;

public partial class WorldLandmark : Node3D
{
    public string SiteId { get; set; } = "";
    public string Title { get; set; } = "Landmark";
    public string Description { get; set; } = "";
    public Color LandmarkColor { get; set; } = Colors.White;
    public EnemyEcosystemEnemy.EnemyRole Habitat { get; set; }
    public bool IsDiscovered { get; private set; }
    public const float DiscoveryRange = 65;
    private HeightmapArena? _arena;
    private CarpetFlightController? _player;
    private float _timer;

    public override void _Ready()
    {
        AddToGroup("world_landmark");
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        _player = GetTree().GetFirstNodeInGroup("player") as CarpetFlightController;
        var stone = new StandardMaterial3D { AlbedoColor = LandmarkColor.Darkened(0.45f), Roughness = 1 };
        var glow = new StandardMaterial3D { AlbedoColor = LandmarkColor, EmissionEnabled = true, Emission = LandmarkColor, EmissionEnergyMultiplier = 1.2f };
        for (int i = 0; i < 3; i++)
        {
            float angle = i * Mathf.Tau / 3;
            AddChild(new MeshInstance3D
            {
                Position = new Vector3(Mathf.Cos(angle) * 7, 2.5f + i, Mathf.Sin(angle) * 7),
                Mesh = new CylinderMesh { TopRadius = 0.7f, BottomRadius = 1.2f, Height = 5 + i * 2, RadialSegments = 8 },
                MaterialOverride = stone
            });
        }
        AddChild(new MeshInstance3D { Position = Vector3.Up * 9, Mesh = new PrismMesh { Size = new Vector3(2, 3, 2) }, MaterialOverride = glow });
        AddChild(new Label3D
        {
            Position = Vector3.Up * 12, Text = Title.ToUpperInvariant(), FontSize = 32, PixelSize = 0.018f,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, Modulate = LandmarkColor,
            VisibilityRangeEnd = 180, VisibilityRangeEndMargin = 20
        });
        FollowSurface();
    }

    public override void _PhysicsProcess(double delta)
    {
        _timer -= (float)delta;
        if (_timer > 0) return;
        _timer = 0.25f;
        FollowSurface();
        _player ??= GetTree().GetFirstNodeInGroup("player") as CarpetFlightController;
        if (_player is { Health: > 0 } && _arena != null
            && _arena.SurfaceDistance(GlobalPosition, _player.GlobalPosition) <= DiscoveryRange) IsDiscovered = true;
    }

    private void FollowSurface()
    {
        if (_arena == null) return;
        GlobalPosition = _arena.OnSurface(GlobalPosition);
        _arena.Orient(this);
    }
}

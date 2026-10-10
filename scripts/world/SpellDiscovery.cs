using Godot;
using MagicCarpetRemastered.Scripts.Player;

namespace MagicCarpetRemastered.Scripts.World;

public partial class SpellDiscovery : Node3D
{
    [Export] public string SpellId { get; set; } = "firebolt";
    [Export] public string SpellName { get; set; } = "Firebolt";
    [Export] public float CollectionRadius { get; set; } = 4;
    public bool IsCollected { get; private set; }

    private HeightmapArena? _arena;
    private MeshInstance3D _rune = null!;

    public override void _Ready()
    {
        AddToGroup("spell_discovery");
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(1, 0.78f, 0.18f),
            EmissionEnabled = true,
            Emission = new Color(1, 0.55f, 0.08f),
            EmissionEnergyMultiplier = 1.5f
        };
        _rune = new MeshInstance3D
        {
            Name = "Rune",
            Mesh = new PrismMesh { Size = new Vector3(1.8f, 2.2f, 1.8f) },
            MaterialOverride = material
        };
        AddChild(_rune);
        AddChild(new MeshInstance3D
        {
            Name = "Beacon",
            Position = Vector3.Down * 2,
            Mesh = new CylinderMesh { TopRadius = 0.12f, BottomRadius = 0.35f, Height = 6 },
            MaterialOverride = material
        });
        AddChild(new Label3D
        {
            Name = "DiscoveryLabel",
            Text = $"{SpellName.ToUpperInvariant()} II\nFly here to discover",
            Position = Vector3.Up * 2,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 32,
            PixelSize = 0.012f,
            Modulate = new Color(1, 0.88f, 0.4f)
        });
        FollowSurface();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsCollected) return;
        FollowSurface();
        _rune.RotateY((float)delta);
        foreach (Node node in GetTree().GetNodesInGroup("player"))
        {
            if (node is CarpetFlightController player && TryCollect(player)) break;
        }
    }

    public bool TryCollect(CarpetFlightController player)
    {
        if (IsCollected || IsQueuedForDeletion() || player.Health <= 0) return false;
        Vector3 offset = player.GlobalPosition - GlobalPosition;
        float halfHeight = (_arena?.MaxFlightClearance ?? 12) * 0.5f;
        if (_arena?.IsSpherical == true)
        {
            if (_arena.SurfaceDistance(player.GlobalPosition, GlobalPosition) > CollectionRadius
                || Mathf.Abs(_arena.Elevation(player.GlobalPosition) - _arena.Elevation(GlobalPosition)) > halfHeight) return false;
            offset = Vector3.Zero;
        }
        if (new Vector2(offset.X, offset.Z).LengthSquared() > CollectionRadius * CollectionRadius
            || Mathf.Abs(offset.Y) > halfHeight) return false;
        if (!player.DiscoverSpellUpgrade(SpellId)) return false;
        IsCollected = true;
        Hide();
        QueueFree();
        return true;
    }

    private void FollowSurface()
    {
        if (_arena == null) return;
        GlobalPosition = _arena.OnSurface(GlobalPosition, _arena.MaxFlightClearance * 0.5f);
        _arena.Orient(this);
    }
}

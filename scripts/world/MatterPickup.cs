using Godot;

namespace MagicCarpetRemastered.Scripts.World;

// Material crates are surface salvage, not rolling energy globes. Carried crates
// stay in the group so the material tracker counts each physical unit once.
public partial class MatterPickup : Node3D
{
    [Export] public int MatterAmount { get; set; } = 15;
    [Export] public AntimatterOwnership Ownership { get; set; }
    public bool IsCarried { get; private set; }
    public bool IsAvailable => MatterAmount > 0 && !IsCarried && !IsQueuedForDeletion();
    private HeightmapArena? _arena;
    private StandardMaterial3D _material = null!;

    public override void _Ready()
    {
        AddToGroup("matter_pickup");
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        _material = new StandardMaterial3D { Roughness = 0.8f, EmissionEnabled = true, EmissionEnergyMultiplier = 0.3f };
        AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(1.2f, 1.2f, 1.2f) }, MaterialOverride = _material });
        ReassignOwnership(Ownership);
        FollowSurface();
    }

    public override void _PhysicsProcess(double delta) { if (!IsCarried) FollowSurface(); }

    private void FollowSurface()
    {
        if (_arena == null) return;
        GlobalPosition = _arena.OnSurface(GlobalPosition, 0.6f);
        _arena.Orient(this);
    }

    public void ReassignOwnership(AntimatterOwnership owner)
    {
        Ownership = owner;
        _material.AlbedoColor = owner switch { AntimatterOwnership.Player => new Color(0.15f, 0.85f, 1),
            AntimatterOwnership.Enemy => new Color(1, 0.25f, 0.3f), _ => new Color(1, 0.65f, 0.18f) };
        _material.Emission = _material.AlbedoColor;
    }

    public bool BeginCarry(CollectorDrone drone, Vector3 offset)
    {
        if (!IsAvailable) return false;
        IsCarried = true;
        Reparent(drone);
        Position = offset;
        return true;
    }

    public int TakeMatter(int amount)
    {
        int taken = Mathf.Min(Mathf.Max(0, amount), Mathf.Max(0, MatterAmount));
        MatterAmount -= taken;
        if (MatterAmount == 0) QueueFree();
        return taken;
    }

    public static void Spawn(Node parent, Vector3 position, int amount, AntimatterOwnership owner = AntimatterOwnership.Neutral)
    {
        if (amount <= 0) return;
        var arena = parent.GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        Basis frame = PlanetGeometry.Frame(arena?.UpAt(position) ?? Vector3.Up, Vector3.Forward);
        // Bound object count even for large storage/cancellation spills.
        int count = Mathf.Min(8, (int)(((long)amount + 14) / 15));
        for (int i = 0; i < count; i++)
        {
            int part = amount / count + (i < amount % count ? 1 : 0);
            float angle = Mathf.Tau * i / count;
            Vector3 world = position + frame * new Vector3(Mathf.Cos(angle) * 7, 0, Mathf.Sin(angle) * 7);
            world = arena?.OnSurface(world, 0.6f) ?? world;
            var pickup = new MatterPickup { Name = "MatterCrate", MatterAmount = part, Ownership = owner,
                Position = parent is Node3D spatial ? spatial.ToLocal(world) : world };
            parent.AddChild(pickup);
        }
    }
}

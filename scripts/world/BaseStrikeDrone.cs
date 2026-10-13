using Godot;

namespace MagicCarpetRemastered.Scripts.World;

// A finite, one-shot offensive unit. Launching never teleports it to the rival.
public partial class BaseStrikeDrone : Node3D
{
    public FactionBase Target { get; set; } = null!;
    public bool IsPlayerOwned { get; set; } = true;
    [Export] public float MoveSpeed { get; set; } = 18;
    [Export] public int Damage { get; set; } = 120;
    [Export] public float LifetimeSeconds { get; set; } = 60;
    private HeightmapArena? _arena;
    private bool _spent;

    public override void _Ready()
    {
        AddToGroup("base_strike_drone");
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        if (_arena?.IsSpherical == true && IsInstanceValid(Target))
            LifetimeSeconds = Mathf.Max(LifetimeSeconds,
                _arena.SurfaceDistance(GlobalPosition, Target.GlobalPosition) / Mathf.Max(1, MoveSpeed) + 15);
        AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.2f, BottomRadius = 0.7f, Height = 1.6f },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = IsPlayerOwned ? Colors.Cyan : Colors.OrangeRed,
                Metallic = 0.7f, Roughness = 0.3f }
        });
        _arena?.Orient(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_spent || IsQueuedForDeletion()) return;
        LifetimeSeconds -= (float)delta;
        if (LifetimeSeconds <= 0 || !IsInstanceValid(Target) || Target.IsQueuedForDeletion() || Target.IsDestroyed)
        { _spent = true; QueueFree(); return; }
        Vector3 destination = Target.GlobalPosition + (_arena?.UpAt(Target.GlobalPosition) ?? Vector3.Up) * 4;
        Vector3 heading = _arena?.DirectionTo(GlobalPosition, destination) ?? destination - GlobalPosition;
        GlobalPosition = _arena?.IsSpherical == true
            ? _arena.ClampFlightPosition(_arena.StepToward(GlobalPosition, destination, MoveSpeed * (float)delta), 4)
            : GlobalPosition.MoveToward(destination, MoveSpeed * (float)delta);
        if (_arena != null) _arena.Orient(this, heading);
        if (GlobalPosition.DistanceTo(destination) > 5) return;
        _spent = true;
        Target.ApplyDamage(Damage);
        QueueFree();
    }
}

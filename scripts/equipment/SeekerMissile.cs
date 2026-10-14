using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Equipment;

public partial class SeekerMissile : StaticBody3D
{
    public ShipFlightController OwnerShip { get; set; } = null!;
    public AntimatterOwnership Ownership { get; set; }
    public int Damage { get; set; } = 42;
    public int Health { get; private set; } = 10;
    public Node3D? Target { get; private set; }
    private Vector3 _direction;
    private float _life = 4;
    private HeightmapArena? _arena;
    private static readonly SphereShape3D Shape = new() { Radius = 0.45f };
    private static readonly SphereMesh Mesh = new() { Radius = 0.45f, Height = 0.9f, RadialSegments = 8, Rings = 4 };
    private static readonly StandardMaterial3D Material = new() { AlbedoColor = Colors.Gold, EmissionEnabled = true, Emission = Colors.Orange, EmissionEnergyMultiplier = 2 };

    public override void _Ready()
    {
        AddToGroup("guided_projectile");
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        AddChild(new CollisionShape3D { Shape = Shape });
        AddChild(new MeshInstance3D { Mesh = Mesh, MaterialOverride = Material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
    }

    public void Launch(Vector3 direction)
    {
        _direction = direction.Normalized();
        float best = float.MaxValue;
        foreach (string group in new[] { "pilots", "ecosystem_enemy", "drone", "base_tactical_drone", "base_strike_drone", "guardian-drone" })
            foreach (Node node in GetTree().GetNodesInGroup(group))
                if (node is Node3D candidate && candidate.GetParent() == GetParent() && FactionCombat.CanDamage(candidate, Ownership))
                {
                    Vector3 offset = candidate.GlobalPosition - GlobalPosition;
                    float distance = offset.Length();
                    float alignment = distance > 0.01f ? _direction.Dot(offset / distance) : 1;
                    float score = (1 - alignment) * 200 + distance;
                    if (distance <= 90 && alignment >= 0.9f && score < best && ClearPath(candidate))
                    { Target = candidate; best = score; }
                }
    }

    private Godot.Collections.Array<Rid> Exclusions()
    {
        var excluded = new Godot.Collections.Array<Rid> { GetRid() };
        if (IsInstanceValid(OwnerShip)) excluded.Add(OwnerShip.GetRid());
        return excluded;
    }

    private bool ClearPath(Node3D target)
    {
        using var ray = PhysicsRayQueryParameters3D.Create(GlobalPosition, target.GlobalPosition);
        ray.Exclude = Exclusions();
        using var hit = GetWorld3D().DirectSpaceState.IntersectRay(ray);
        return hit.Count == 0 || hit["collider"].As<Node3D>() == target;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GetTree().Paused || IsQueuedForDeletion() || Health <= 0 || !double.IsFinite(delta) || delta <= 0) return;
        float dt = (float)delta;
        if (IsInstanceValid(Target) && FactionCombat.CanDamage(Target!, Ownership))
        {
            Vector3 desired = (Target!.GlobalPosition - GlobalPosition).Normalized();
            float angle = _direction.AngleTo(desired);
            if (angle > 0.001f) _direction = _direction.Slerp(desired, Mathf.Min(1, 2.2f * dt / angle)).Normalized();
        }
        else Target = null; // No remote reacquisition after losing a lock.
        Vector3 before = GlobalPosition, next = before + _direction * 40 * dt;
        using var ray = PhysicsRayQueryParameters3D.Create(before, next);
        ray.Exclude = Exclusions();
        using var hit = GetWorld3D().DirectSpaceState.IntersectRay(ray);
        if (hit.Count > 0)
        {
            GlobalPosition = (Vector3)hit["position"];
            var body = hit["collider"].As<Node3D>();
            if (FactionCombat.CanDamage(body, Ownership)) FactionCombat.Damage(body,
                body is FactionBase ? Mathf.Max(1, Mathf.RoundToInt(Damage * 0.2f)) : Damage,
                IsInstanceValid(OwnerShip) ? OwnerShip : null);
            Explode(); return;
        }
        if (_arena?.IsSpherical == true && _arena.TrySurfaceHit(before, next, out Vector3 ground))
        { GlobalPosition = ground; Explode(); return; }
        GlobalPosition = next;
        _life -= dt;
        if (_life <= 0) QueueFree();
    }

    public void ApplyDamage(int damage)
    {
        if (damage <= 0 || Health <= 0 || IsQueuedForDeletion()) return;
        Health = Mathf.Max(0, Health - damage);
        if (Health == 0) Explode();
    }

    private void Explode()
    {
        EquipmentImpactPool.Prepare(GetTree().CurrentScene).Emit(GlobalPosition, Colors.Gold, Colors.Orange, 0.25f, 0.5f, 2);
        QueueFree();
    }
}

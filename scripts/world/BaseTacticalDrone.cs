using System;
using Godot;
using MagicCarpetRemastered.Scripts.Equipment;

namespace MagicCarpetRemastered.Scripts.World;

public partial class BaseTacticalDrone : StaticBody3D
{
    public enum DroneRole { Scout, Attacker, Defender }
    public DroneRole Role { get; set; }
    public FactionBase Home { get; set; } = null!;
    public Node3D? Target { get; set; }
    public int Rank { get; set; } = 1;
    public int Health { get; private set; }
    public AntimatterOwnership Ownership { get; private set; }
    public bool CanCapture => Role == DroneRole.Scout && Health > 0 && !IsQueuedForDeletion()
        && IsInstanceValid(Home) && !Home.IsDestroyed;
    private HeightmapArena? _arena;
    private float _lifetime = 180, _cooldown, _scan;
    private bool _returning;
    private Node3D? _combatTarget;
    private static readonly SphereShape3D SharedShape = new() { Radius = 0.7f };
    private static readonly Mesh[] SharedMeshes =
    {
        new SphereMesh { Radius = 0.7f, Height = 0.8f, RadialSegments = 12, Rings = 6 },
        new PrismMesh { Size = new Vector3(1.5f, 0.6f, 1.8f) },
        new CylinderMesh { TopRadius = 0.7f, BottomRadius = 0.7f, Height = 0.8f, RadialSegments = 6 }
    };

    public override void _Ready()
    {
        AddToGroup("base_tactical_drone");
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        Ownership = Home.IsPlayerBase ? AntimatterOwnership.Player : AntimatterOwnership.Enemy;
        Health = Role == DroneRole.Scout ? 20 + Rank * 10 : 30 + Rank * 15;
        AddChild(new CollisionShape3D { Shape = SharedShape });
        Color color = Home.IsPlayerBase ? Colors.Cyan : Colors.OrangeRed;
        AddChild(new MeshInstance3D { Mesh = SharedMeshes[(int)Role],
            MaterialOverride = new StandardMaterial3D { AlbedoColor = color, EmissionEnabled = true, Emission = color, EmissionEnergyMultiplier = 0.6f } });
        if (Role == DroneRole.Scout && Target == null) Target = NearestCapturePoint();
        if (IsInstanceValid(Target)) _lifetime = Mathf.Max(_lifetime, Distance(Target!.GlobalPosition) / 12 + 90);
        _arena?.Orient(this);
    }

    private float Distance(Vector3 point) => _arena?.SurfaceDistance(GlobalPosition, point) ?? GlobalPosition.DistanceTo(point);
    private StrategicPoint? NearestCapturePoint()
    {
        StrategicPoint? best = null; float nearest = float.MaxValue;
        foreach (Node node in GetTree().GetNodesInGroup("strategic_point"))
            if (node is StrategicPoint point && point.GetParent() == Home.GetParent() && !point.IsQueuedForDeletion() && point.Ownership != Ownership)
            {
                float distance = Distance(point.GlobalPosition);
                if (distance < nearest) { best = point; nearest = distance; }
            }
        return best;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GetTree().Paused || Health <= 0 || IsQueuedForDeletion() || !double.IsFinite(delta) || delta < 0) return;
        if (!IsInstanceValid(Home) || Home.IsDestroyed) { QueueFree(); return; }
        _lifetime -= (float)delta;
        if (_lifetime <= 0) { QueueFree(); return; }
        _cooldown = Mathf.Max(0, _cooldown - (float)delta); _scan -= (float)delta;
        if (Role == DroneRole.Scout)
        {
            if (!IsInstanceValid(Target) || Target!.IsQueuedForDeletion() || Target is StrategicPoint point && point.Ownership == Ownership)
                Target = NearestCapturePoint();
            _returning = Target == null;
        }
        if (Role == DroneRole.Defender && Target is StrategicPoint assigned && assigned.Ownership != Ownership) _returning = true;
        if (!IsInstanceValid(Target) || Target!.IsQueuedForDeletion() || Target is FactionBase { IsDestroyed: true }) _returning = true;
        Vector3 anchor = _returning ? Home.GlobalPosition : Target!.GlobalPosition;
        if (_returning && Distance(anchor) <= 6)
        {
            if (Role != DroneRole.Defender) { QueueFree(); return; }
            Target = Home; _returning = false; // Continue defending home after a site is lost.
        }
        int relay = Home.Construction.UpgradeRank("drone-command-relay", true);
        if (_scan <= 0)
        {
            _scan = 0.35f;
            _combatTarget = Role == DroneRole.Scout || _returning ? null : FindEnemy(anchor, 25 + relay * 5);
            if (Role == DroneRole.Scout && Home.IsPlayerBase)
                foreach (Node node in GetTree().GetNodesInGroup("strategic_point"))
                    if (node is StrategicPoint discovered && Distance(discovered.GlobalPosition) <= 40 + Rank * 20)
                        (GetTree().GetFirstNodeInGroup("world_navigation") as WorldNavigation)?.DiscoverPoint(discovered);
        }
        Vector3 destination = anchor;
        if (_returning || Role == DroneRole.Scout || !IsInstanceValid(_combatTarget)
            || !FactionCombat.CanDamage(_combatTarget!, Ownership)
            || (_arena?.SurfaceDistance(anchor, _combatTarget!.GlobalPosition) ?? anchor.DistanceTo(_combatTarget!.GlobalPosition)) > 25 + relay * 5)
            _combatTarget = null;
        if (IsInstanceValid(_combatTarget) && FactionCombat.CanDamage(_combatTarget!, Ownership)) destination = _combatTarget!.GlobalPosition;
        float speed = (Role == DroneRole.Scout ? 12 + Rank * 2 : 12) * (1 + relay * 0.1f);
        Vector3 heading = _arena?.DirectionTo(GlobalPosition, destination) ?? destination - GlobalPosition;
        if (Distance(destination) > (Role == DroneRole.Scout ? 3 : 8))
            GlobalPosition = _arena != null
                ? _arena.OnSurface(_arena.StepToward(GlobalPosition, destination, speed * (float)delta), 5)
                : GlobalPosition.MoveToward(destination + Vector3.Up * 5, speed * (float)delta);
        else if (_arena != null) GlobalPosition = _arena.OnSurface(GlobalPosition, 5);
        _arena?.Orient(this, heading);
        if (_cooldown > 0 || !IsInstanceValid(_combatTarget) || !FactionCombat.CanDamage(_combatTarget!, Ownership)
            || GlobalPosition.DistanceTo(_combatTarget!.GlobalPosition) > 16) return;
        using var ray = PhysicsRayQueryParameters3D.Create(GlobalPosition, _combatTarget.GlobalPosition);
        var excluded = new Godot.Collections.Array<Rid> { GetRid() };
        if (_combatTarget is CollisionObject3D collider) excluded.Add(collider.GetRid());
        ray.Exclude = excluded;
        using var hit = GetWorld3D().DirectSpaceState.IntersectRay(ray);
        if (hit.Count > 0) return;
        int damage = 4 + Rank * 2;
        if (_combatTarget is FactionBase) damage = Mathf.Max(1, damage / 5);
        FactionCombat.Damage(_combatTarget, damage, this);
        EquipmentImpactPool.Prepare(GetTree().CurrentScene).Emit(_combatTarget.GlobalPosition,
            Home.IsPlayerBase ? Colors.Cyan : Colors.OrangeRed, Colors.White, 0.2f, 0.3f, 1.5f);
        _cooldown = 1.2f;
    }

    private Node3D? FindEnemy(Vector3 anchor, float radius)
    {
        Node3D? best = null; float nearest = float.MaxValue;
        foreach (string group in new[] { "pilots", "ecosystem_enemy", "base_tactical_drone", "base_strike_drone", "guardian-drone" })
            foreach (Node node in GetTree().GetNodesInGroup(group))
                if (node is Node3D candidate && FactionCombat.CanDamage(candidate, Ownership)
                    && (_arena?.SurfaceDistance(anchor, candidate.GlobalPosition) ?? anchor.DistanceTo(candidate.GlobalPosition)) <= radius)
                {
                    float distance = GlobalPosition.DistanceTo(candidate.GlobalPosition);
                    if (distance < nearest) { best = candidate; nearest = distance; }
                }
        if (best == null && Role == DroneRole.Attacker && Target is FactionBase hub && FactionCombat.CanDamage(hub, Ownership)) best = hub;
        return best;
    }

    public void ApplyDamage(int damage)
    {
        if (damage <= 0 || Health <= 0) return;
        if (IsInstanceValid(Home) && Role == DroneRole.Defender && IsInstanceValid(Target) && Target is StrategicPoint point
            && point.Ownership == Ownership && Distance(point.GlobalPosition) <= point.CaptureRadius)
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * (1 - 0.1f * Home.Construction.UpgradeRank("site-fortification", true))));
        Health = Mathf.Max(0, Health - damage);
        if (Health == 0) QueueFree(); // Paid unit investment is expended, not physical cargo.
    }
}

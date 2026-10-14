using System;
using Godot;
using MagicCarpetRemastered.Scripts.Equipment;

namespace MagicCarpetRemastered.Scripts.World;

public partial class BaseTacticalDrone : StaticBody3D
{
    public enum DroneRole { Scout, Attacker, Defender, Siphon, Harvester }
    public bool Automatic { get; set; }
    public int CarriedAntimatter { get; private set; }
    private float _theftCooldown;
    private bool _delivering;
    public DroneRole Role { get; set; }
    public FactionBase Home { get; set; } = null!;
    public Node3D? Target { get; set; }
    public int Rank { get; set; } = 1;
    public int Health { get; private set; }
    private int _healthRank = 1;
    public void ApplyResearchRank(int rank)
    {
        int gain = (rank - _healthRank) * (Role == DroneRole.Scout ? 10 : Role is DroneRole.Attacker or DroneRole.Harvester ? 25 : 15);
        if (gain > 0) Health += gain;
        Rank = _healthRank = rank;
    }
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
        new CylinderMesh { TopRadius = 0.7f, BottomRadius = 0.7f, Height = 0.8f, RadialSegments = 6 },
        new CylinderMesh { TopRadius = 0.3f, BottomRadius = 0.8f, Height = 0.8f, RadialSegments = 8 },
        new PrismMesh { Size = new Vector3(2.1f, 0.8f, 1.2f) }
    };

    public override void _Ready()
    {
        AddToGroup("base_tactical_drone");
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        Ownership = Home.IsPlayerBase ? AntimatterOwnership.Player : AntimatterOwnership.Enemy;
        Health = Role == DroneRole.Scout ? 20 + Rank * 10 : Role is DroneRole.Attacker or DroneRole.Harvester ? 50 + Rank * 25 : 30 + Rank * 15;
        _healthRank = Rank;
        AddChild(new CollisionShape3D { Shape = SharedShape });
        Color color = Home.IsPlayerBase ? Colors.Cyan : Colors.OrangeRed;
        AddChild(new MeshInstance3D { Mesh = SharedMeshes[(int)Role % SharedMeshes.Length],
            MaterialOverride = new StandardMaterial3D { AlbedoColor = color, EmissionEnabled = true, Emission = color, EmissionEnergyMultiplier = 0.6f } });
        if (!Automatic && Role == DroneRole.Scout && Target == null) Target = NearestCapturePoint();
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
        if (!IsInstanceValid(Home) || Home.IsDestroyed) { Retire(); return; }
        if (Automatic && Home.Tier < (Role is DroneRole.Siphon or DroneRole.Harvester ? 2 : 1)) return;
        if (Role == DroneRole.Siphon) { ProcessSiphon((float)delta); return; }
        if (Role == DroneRole.Harvester) { ProcessHarvester((float)delta); return; }
        if (!Automatic) _lifetime -= (float)delta;
        if (_lifetime <= 0) { QueueFree(); return; }
        _cooldown = Mathf.Max(0, _cooldown - (float)delta); _scan -= (float)delta;
        if (Role == DroneRole.Scout && !Automatic)
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
            if (Role != DroneRole.Defender && !Automatic) { QueueFree(); return; }
            Target = Home; _returning = false; // Continue defending home after a site is lost.
        }
        if (Automatic) _returning = !IsInstanceValid(Target) || Target!.IsQueuedForDeletion();
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
        Vector3 aim = _combatTarget.GlobalPosition;
        if (_combatTarget is FactionBase) aim += (_arena?.UpAt(aim) ?? Vector3.Up) * 3;
        using var ray = PhysicsRayQueryParameters3D.Create(GlobalPosition, aim);
        var excluded = new Godot.Collections.Array<Rid> { GetRid() };
        if (_combatTarget is CollisionObject3D collider) excluded.Add(collider.GetRid());
        ray.Exclude = excluded;
        using var hit = GetWorld3D().DirectSpaceState.IntersectRay(ray);
        if (hit.Count > 0) return;
        int damage = Role == DroneRole.Attacker ? 6 + Rank * 4 : 4 + Rank * 2;
        if (_combatTarget is FactionBase) damage = Mathf.Max(1, damage / 5);
        CombatBeam.Emit(GetParent(), GlobalPosition, aim, Ownership);
        FactionCombat.Damage(_combatTarget, damage, this);
        EquipmentImpactPool.Prepare(GetTree().CurrentScene).Emit(_combatTarget.GlobalPosition,
            Home.IsPlayerBase ? Colors.Cyan : Colors.OrangeRed, Colors.White, 0.2f, 0.3f, 1.5f);
        _cooldown = Role == DroneRole.Attacker ? 1 : 1.2f;
    }

    private Node3D? FindEnemy(Vector3 anchor, float radius)
    {
        Node3D? best = null; float nearest = float.MaxValue;
        foreach (string group in new[] { "pilots", "ecosystem_enemy", "base_tactical_drone", "base_strike_drone", "guardian-drone", "guided_projectile" })
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
        if (Health == 0) Retire();
    }

    public void Retire()
    {
        if (CarriedAntimatter > 0)
        {
            AntimatterScatter.Spawn(GetParent(), GlobalPosition, CarriedAntimatter, AntimatterOwnership.Neutral);
            CarriedAntimatter = 0;
        }
        QueueFree();
    }

    private void ProcessSiphon(float delta)
    {
        _theftCooldown = Mathf.Max(0, _theftCooldown - delta);
        bool validTarget = IsInstanceValid(Target) && Target is FactionBase { IsDestroyed: false };
        if (!validTarget && CarriedAntimatter > 0) _delivering = true;
        Node3D destination = _delivering || !validTarget ? Home : Target!;
        float speed = 12 * (1 + 0.1f * Home.Construction.UpgradeRank("drone-command-relay", true));
        if (Distance(destination.GlobalPosition) > 8)
        {
            Vector3 heading = _arena?.DirectionTo(GlobalPosition, destination.GlobalPosition) ?? destination.GlobalPosition - GlobalPosition;
            GlobalPosition = _arena != null ? _arena.OnSurface(_arena.StepToward(GlobalPosition, destination.GlobalPosition, speed * delta), 5)
                : GlobalPosition.MoveToward(destination.GlobalPosition + Vector3.Up * 5, speed * delta);
            _arena?.Orient(this, heading);
            return;
        }
        if (_delivering)
        {
            CarriedAntimatter -= Home.DepositAntimatter(CarriedAntimatter);
            if (CarriedAntimatter == 0) _delivering = false;
        }
        else if (validTarget && _theftCooldown <= 0)
        {
            var victim = (FactionBase)Target!;
            int stolen = victim.WithdrawAntimatter(Mathf.Min(2 * Rank, 10 * Rank - CarriedAntimatter));
            CarriedAntimatter += stolen;
            if (stolen > 0) victim.NotifyHostileAction(this);
            _theftCooldown = 1;
            if (CarriedAntimatter >= 10 * Rank || victim.StoredAntimatter == 0) _delivering = true;
        }
    }
}

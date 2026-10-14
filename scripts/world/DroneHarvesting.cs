using System.Collections.Generic;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Equipment;

namespace MagicCarpetRemastered.Scripts.World;

public partial class BaseTacticalDrone
{
    private readonly List<AntimatterPickup> _harvestDrops = new();
    private Vector3 _harvestPosition;
    private float _harvestCleanup;
    public bool AwaitingHarvest => _harvestCleanup > 0 && !IsHarvestable(Target);
    public static bool IsHarvestable(Node3D? target) => IsInstanceValid(target)
        && target is EnemyEcosystemEnemy { Health: > 0 } mob && !mob.IsQueuedForDeletion()
        && mob.Role is EnemyEcosystemEnemy.EnemyRole.FlyingSwarm or EnemyEcosystemEnemy.EnemyRole.GroundBeast
            or EnemyEcosystemEnemy.EnemyRole.RangedCaster or EnemyEcosystemEnemy.EnemyRole.AntimatterThief
        && !string.IsNullOrEmpty(mob.HabitatId) && !mob.HabitatId.StartsWith("route-");

    private void ProcessHarvester(float delta)
    {
        _cooldown = Mathf.Max(0, _cooldown - delta);
        _scan -= delta;
        bool live = IsHarvestable(Target);
        if (live)
        {
            _harvestPosition = Target!.GlobalPosition;
            // The largest staggered drop takes <6s; leave time to claim its last fragments.
            _harvestCleanup = 8;
        }
        else _harvestCleanup = Mathf.Max(0, _harvestCleanup - delta);
        Vector3 destination = live || _harvestCleanup > 0 ? _harvestPosition : Home.GlobalPosition;
        Vector3 heading = _arena?.DirectionTo(GlobalPosition, destination) ?? destination - GlobalPosition;
        float speed = 12 * (1 + 0.1f * Home.Construction.UpgradeRank("drone-command-relay", true));
        if (Distance(destination) > 8)
            GlobalPosition = _arena != null
                ? _arena.OnSurface(_arena.StepToward(GlobalPosition, destination, speed * delta), 5)
                : GlobalPosition.MoveToward(destination + Vector3.Up * 5, speed * delta);
        _arena?.Orient(this, heading);

        if (_scan <= 0)
        {
            _scan = 0.35f;
            float radius = 8 + Rank * 4;
            AntimatterNeighborhood.Query(GetTree(), GlobalPosition, radius, _harvestDrops);
            foreach (var drop in _harvestDrops)
                if (drop.GetParent() == GetParent() && drop.IsAvailable && drop.Ownership == AntimatterOwnership.Neutral
                    && GlobalPosition.DistanceTo(drop.GlobalPosition) <= radius && HarvestLineOfSight(drop))
                {
                    drop.ReassignOwnership(Ownership);
                    EquipmentImpactPool.Prepare(GetTree().CurrentScene).Emit(drop.GlobalPosition,
                        Home.IsPlayerBase ? Colors.Cyan : Colors.OrangeRed, Colors.White, 0.12f, 0.2f, 0.7f);
                }
        }
        if (!live || _cooldown > 0 || GlobalPosition.DistanceTo(Target!.GlobalPosition) > 16 || !HarvestLineOfSight(Target)) return;
        CombatBeam.Emit(GetParent(), GlobalPosition, Target.GlobalPosition, Ownership);
        FactionCombat.Damage(Target, 4 + Rank * 4, this);
        EquipmentImpactPool.Prepare(GetTree().CurrentScene).Emit(Target.GlobalPosition,
            Home.IsPlayerBase ? Colors.Cyan : Colors.OrangeRed, Colors.White, 0.2f, 0.3f, 1.5f);
        _cooldown = 1;
    }

    private bool HarvestLineOfSight(Node3D target)
    {
        using var ray = PhysicsRayQueryParameters3D.Create(GlobalPosition, target.GlobalPosition);
        var excluded = new Godot.Collections.Array<Rid> { GetRid() };
        if (target is CollisionObject3D collider) excluded.Add(collider.GetRid());
        ray.Exclude = excluded;
        using var hit = GetWorld3D().DirectSpaceState.IntersectRay(ray);
        return hit.Count == 0;
    }
}

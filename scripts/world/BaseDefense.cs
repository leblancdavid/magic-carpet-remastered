using Godot;
using System.Collections.Generic;
using MagicCarpetRemastered.Scripts.Equipment;

namespace MagicCarpetRemastered.Scripts.World;

// A bounded retaliatory weapon, not an ecosystem-wide acquisition scan.
public partial class BaseDefense : Node
{
    public float Range { get; set; } = 30;
    public int Damage { get; set; } = 6;
    public float CooldownSeconds { get; set; } = 1.2f;
    private FactionBase _home = null!;
    private HeightmapArena? _arena;
    private readonly Dictionary<Node3D, float> _attackers = new();
    private readonly List<Node3D> _expired = new();
    private float _clock, _cooldown;

    public override void _Ready()
    {
        _home = GetParent<FactionBase>();
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
    }

    public void NotifyAttack(Node3D? attacker)
    {
        if (GodotObject.IsInstanceValid(attacker) && FactionCombat.CanDamage(attacker!, FactionCombat.OwnershipOf(_home)))
            _attackers[attacker!] = _clock + 8;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GetTree().Paused || _home.IsDestroyed) return;
        _clock += (float)delta;
        _cooldown = Mathf.Max(0, _cooldown - (float)delta);
        Node3D? target = null;
        float nearest = float.MaxValue;
        _expired.Clear();
        foreach (var pair in _attackers)
        {
            if (pair.Value <= _clock || !FactionCombat.CanDamage(pair.Key, FactionCombat.OwnershipOf(_home)))
            { _expired.Add(pair.Key); continue; }
            float distance = _arena?.SurfaceDistance(_home.GlobalPosition, pair.Key.GlobalPosition)
                ?? _home.GlobalPosition.DistanceTo(pair.Key.GlobalPosition);
            if (distance > Range || distance >= nearest) continue;
            if (_home.GlobalPosition.DistanceTo(pair.Key.GlobalPosition) > Range) continue;
            Vector3 origin = _home.GlobalPosition + (_arena?.UpAt(_home.GlobalPosition) ?? Vector3.Up) * 5;
            using var ray = PhysicsRayQueryParameters3D.Create(origin, pair.Key.GlobalPosition);
            var exclusions = new Godot.Collections.Array<Rid> { _home.GetRid() };
            if (pair.Key is CollisionObject3D collider) exclusions.Add(collider.GetRid());
            ray.Exclude = exclusions;
            using var hit = _home.GetWorld3D().DirectSpaceState.IntersectRay(ray);
            if (hit.Count != 0) continue;
            target = pair.Key; nearest = distance;
        }
        foreach (var expired in _expired) _attackers.Remove(expired);
        if (target == null || _cooldown > 0) return;
        int rank = _home.Construction.UpgradeRank("pulse-turret", true);
        FactionCombat.Damage(target, Damage + 2 * rank, _home);
        EquipmentImpactPool.Prepare(GetTree().CurrentScene).Emit(_home.GlobalPosition
            + (_arena?.UpAt(_home.GlobalPosition) ?? Vector3.Up) * 5,
            new Color(0.2f, 0.9f, 1), Colors.Cyan, 0.15f, 0.3f, 1.0f);
        EquipmentImpactPool.Prepare(GetTree().CurrentScene).Emit(target.GlobalPosition,
            new Color(0.2f, 0.9f, 1), Colors.Cyan, 0.2f, 0.4f, 1.5f);
        _cooldown = Mathf.Max(0.1f, CooldownSeconds - 0.1f * rank);
    }
}

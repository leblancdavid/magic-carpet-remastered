using Godot;
using System.Collections.Generic;
using System.Linq;

namespace MagicCarpetRemastered.Scripts.World;

// Each researched slot owns its own free replacement clock, independent of research.
public partial class AutomaticDroneFleet : Node
{
    public static readonly string[] Types = { "scout-drone", "attack-drone", "defensive-drone", "strike-drone", "siphon-drone", "harvester-drone" };
    public const int MaximumUnitsPerType = 5; // Provisional tuning, including the first researched unit.
    public static bool Supports(string id) => System.Array.IndexOf(Types, id) >= 0;
    public static float ProductionSeconds(string id) => id == "strike-drone" ? 20 : 10;
    public static string ExpansionId(string id, int count) => $"fleet-{id}-{count}";
    public FactionBase Home { get; set; } = null!;
    private sealed class Slot { public Node3D? Unit; public float Remaining; }
    private readonly Dictionary<string, List<Slot>> _slots = new();
    private float _assignmentTimer;

    public static void Register(BaseConstruction construction)
    {
        foreach (string id in Types)
        {
            var item = UpgradeCatalog.Find(id)!;
            for (int count = 2; count <= MaximumUnitsPerType; count++)
                construction.Register(new ConstructionDefinition(ExpansionId(id, count), $"{item.Name} fleet +1 ({count})",
                    item.Slot, UpgradeCatalog.Definition(item, 3).Cost * (count - 1), 60, item.Tier,
                    count == 2 ? UpgradeCatalog.ProjectId(id, 3) : ExpansionId(id, count - 1),
                    "One additional permanent unit slot. Free automatic replacements."));
        }
    }
    public int Entitlement(string id)
    {
        if (Home.Construction.UpgradeRank(id) == 0) return 0;
        int count = 1;
        for (int i = 2; i <= MaximumUnitsPerType; i++)
            if (Home.Construction.IsCompleted(ExpansionId(id, i))) count = i;
        return count;
    }
    public string Status(string id)
    {
        int count = Entitlement(id);
        if (count == 0) return "Research to unlock fleet";
        if (Home.IsDestroyed || Home.Tier < UpgradeCatalog.Find(id)!.Tier) return $"Fleet suspended · {count} slots";
        if (!_slots.TryGetValue(id, out var slots)) return $"0/{count} · building {ProductionSeconds(id):0}s";
        int alive = slots.Count(slot => Alive(slot.Unit));
        float remaining = slots.Where(slot => !Alive(slot.Unit)).Select(slot => slot.Remaining).DefaultIfEmpty(0).Min();
        return $"{alive}/{count} active" + (alive < count ? $" · replacing {remaining:0}s" : " · automatic targets");
    }
    private static bool Alive(Node3D? unit) => IsInstanceValid(unit) && !unit!.IsQueuedForDeletion()
        && unit switch { BaseTacticalDrone tactical => tactical.Health > 0, BaseStrikeDrone strike => strike.Health > 0, _ => false };

    public override void _PhysicsProcess(double delta)
    {
        if (GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        _assignmentTimer -= (float)delta;
        bool assign = _assignmentTimer <= 0;
        if (assign) _assignmentTimer = 1;
        foreach (string id in Types)
        {
            if (!_slots.TryGetValue(id, out var slots)) _slots[id] = slots = new();
            while (slots.Count < Entitlement(id)) slots.Add(new Slot { Remaining = ProductionSeconds(id) });
            foreach (var slot in slots)
            {
                if (Home.IsDestroyed)
                {
                    if (Alive(slot.Unit)) { if (slot.Unit is BaseTacticalDrone drone) drone.Retire(); else slot.Unit!.QueueFree(); }
                    continue;
                }
                if (Home.Tier < UpgradeCatalog.Find(id)!.Tier) continue;
                if (Alive(slot.Unit))
                {
                    if (slot.Unit is BaseTacticalDrone drone && assign)
                    {
                        drone.ApplyResearchRank(Home.Construction.UpgradeRank(id, true));
                        if (id != "harvester-drone" || !BaseTacticalDrone.IsHarvestable(drone.Target) && !drone.AwaitingHarvest)
                            drone.Target = ChooseTarget(id, drone, drone.GlobalPosition);
                    }
                    continue;
                }
                if (slot.Unit != null) { slot.Unit = null; slot.Remaining = ProductionSeconds(id); }
                slot.Remaining = Mathf.Max(0, slot.Remaining - (float)delta);
                if (slot.Remaining > 0) continue;
                Node3D? target = ChooseTarget(id, null, Home.GlobalPosition);
                if (target == null && id == "strike-drone") continue;
                Spawn(id, slot, target);
            }
        }
    }

    private Node3D? ChooseTarget(string id, Node3D? self, Vector3 origin)
    {
        var candidates = new List<Node3D>();
        if (id == "harvester-drone")
            foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
                if (node is Node3D mob && mob.GetParent() == Home.GetParent() && BaseTacticalDrone.IsHarvestable(mob)) candidates.Add(mob);
        bool defender = id == "defensive-drone";
        if (defender) candidates.Add(Home);
        if (id is "scout-drone" or "attack-drone" or "defensive-drone")
            foreach (Node node in GetTree().GetNodesInGroup("strategic_point"))
                if (node is StrategicPoint point && point.GetParent() == Home.GetParent() && !point.IsQueuedForDeletion()
                    && ((point.Ownership == (Home.IsPlayerBase ? AntimatterOwnership.Player : AntimatterOwnership.Enemy)) == defender))
                    candidates.Add(point);
        if (id is "attack-drone" or "strike-drone" or "siphon-drone")
            foreach (Node node in GetTree().GetNodesInGroup(Home.IsPlayerBase ? "enemy_base" : "player_base"))
                if (node is FactionBase hub && hub.GetParent() == Home.GetParent() && !hub.IsDestroyed && !hub.IsQueuedForDeletion())
                    candidates.Add(hub);
        var arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        Node3D? best = null; float score = float.MaxValue;
        foreach (var candidate in candidates)
        {
            int assigned = _slots.TryGetValue(id, out var slots) ? slots.Count(slot => Alive(slot.Unit) && slot.Unit != self
                && (slot.Unit is BaseTacticalDrone tactical && (tactical.Target == candidate
                        || id == "harvester-drone" && IsInstanceValid(tactical.Target)
                        && tactical.Target is Enemies.EnemyEcosystemEnemy assignedMob && candidate is Enemies.EnemyEcosystemEnemy candidateMob
                        && assignedMob.HabitatId == candidateMob.HabitatId)
                    || slot.Unit is BaseStrikeDrone strike && strike.Target == candidate)) : 0;
            if (id == "scout-drone" && assigned > 0) continue;
            float value = assigned * 100000 + (arena?.SurfaceDistance(origin, candidate.GlobalPosition) ?? origin.DistanceTo(candidate.GlobalPosition));
            // Keep stable assignments when alternatives have equivalent coverage.
            if (self is BaseTacticalDrone current && current.Target == candidate) value -= 10;
            if (defender)
            {
                bool threatened = false;
                foreach (string group in new[] { "pilots", "ecosystem_enemy", "base_tactical_drone", "base_strike_drone" })
                {
                    foreach (Node node in GetTree().GetNodesInGroup(group))
                        if (node is Node3D threat && Equipment.FactionCombat.CanDamage(threat, Home.IsPlayerBase ? AntimatterOwnership.Player : AntimatterOwnership.Enemy)
                            && (arena?.SurfaceDistance(threat.GlobalPosition, candidate.GlobalPosition) ?? threat.GlobalPosition.DistanceTo(candidate.GlobalPosition)) < 40)
                        { threatened = true; break; }
                    if (threatened) break;
                }
                if (threatened) value -= 1000;
            }
            if (value < score) { best = candidate; score = value; }
        }
        return best;
    }

    private void Spawn(string id, Slot slot, Node3D? target)
    {
        var arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        Vector3 start = Home.GlobalPosition + Home.GlobalBasis.X * 10;
        if (arena != null) start = arena.OnSurface(start, 5);
        int rank = Home.Construction.UpgradeRank(id, true);
        Node3D unit = id == "strike-drone"
            ? new BaseStrikeDrone { Home = Home, Target = (FactionBase)target!, IsPlayerOwned = Home.IsPlayerBase,
                MaxHealth = 15 + 15 * rank, Damage = 80 + 40 * rank }
            : new BaseTacticalDrone { Home = Home, Target = target, Rank = rank, Automatic = true,
                Role = id == "scout-drone" ? BaseTacticalDrone.DroneRole.Scout : id == "defensive-drone" ? BaseTacticalDrone.DroneRole.Defender
                     : id == "siphon-drone" ? BaseTacticalDrone.DroneRole.Siphon
                     : id == "harvester-drone" ? BaseTacticalDrone.DroneRole.Harvester : BaseTacticalDrone.DroneRole.Attacker };
        unit.Position = Home.GetParent<Node3D>().ToLocal(start);
        Home.GetParent().AddChild(unit); slot.Unit = unit;
    }
}

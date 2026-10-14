using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MagicCarpetRemastered.Scripts.World;

public static class BaseOrders
{
    public static string OrderId(string capability) => capability == "strike-drone" ? "strike-drone" : $"launch-{capability}";
    public static bool IsOrder(string id) => id is "strike-drone" or "launch-scout-drone" or "launch-attack-drone" or "launch-defensive-drone";
    public static string Capability(string id) => id == "strike-drone" ? id : id[7..];
    public static void Register(BaseConstruction construction)
    {
        foreach (string id in new[] { "scout-drone", "attack-drone", "defensive-drone", "strike-drone" })
        {
            var item = UpgradeCatalog.Find(id)!;
            int cost = id == "scout-drone" ? 25 : id == "strike-drone" ? 100 : 40;
            construction.Register(new(OrderId(id), $"Deploy {item.Name}", item.Slot, cost,
                id == "strike-drone" ? 20 : 10, item.Tier, UpgradeCatalog.ProjectId(id, 1),
                "Paid unit production. Requires an eligible known target; cancellation refunds exactly. Target loss during production cancels with a full refund.", true));
        }
    }

    public static IEnumerable<Node3D?> Targets(FactionBase home, string order)
    {
        string capability = Capability(order);
        if (capability == "scout-drone") yield return null; // Auto nearest, no hidden point list.
        if (capability == "defensive-drone") yield return home;
        foreach (Node node in home.GetTree().GetNodesInGroup("strategic_point"))
            if (node is StrategicPoint point && TargetReason(home, order, point).Length == 0) yield return point;
        foreach (string group in new[] { "player_base", "enemy_base" })
            foreach (Node node in home.GetTree().GetNodesInGroup(group))
                if (node is FactionBase hub && hub != home && TargetReason(home, order, hub).Length == 0) yield return hub;
    }

    public static string TargetReason(FactionBase home, string order, Node3D? target, bool requireKnown = true)
    {
        string capability = Capability(order);
        if (capability == "scout-drone" && target == null)
            return home.GetTree().GetNodesInGroup("strategic_point").OfType<StrategicPoint>()
                .Any(point => point.GetParent() == home.GetParent() && !point.IsQueuedForDeletion()
                    && point.Ownership != (home.IsPlayerBase ? AntimatterOwnership.Player : AntimatterOwnership.Enemy)) ? "" : "No non-owned point remains";
        if (!GodotObject.IsInstanceValid(target) || target!.IsQueuedForDeletion() || !target.IsInsideTree()) return "Target is no longer available";
        if (target.GetTree() != home.GetTree() || target.GetParent() != home.GetParent()) return "Target is not in this match";
        var owner = home.IsPlayerBase ? AntimatterOwnership.Player : AntimatterOwnership.Enemy;
        if (target is FactionBase hub)
        {
            if (hub.IsDestroyed) return "Target base destroyed";
            if (capability == "defensive-drone") return hub == home ? "" : "Defend your own home or owned points";
            if (capability == "scout-drone" || hub.IsPlayerBase == home.IsPlayerBase) return "Choose a non-owned capture point";
            return !requireKnown || Known(home, hub) ? "" : "Target not discovered";
        }
        if (target is not StrategicPoint point) return "Choose a base or capture point";
        if (capability == "strike-drone") return "Strike drones target enemy bases";
        if (capability == "defensive-drone" && point.Ownership != owner) return "Defensive deployment requires an owned point";
        if (capability == "scout-drone" && point.Ownership == owner) return "Point is already owned";
        if (capability == "attack-drone" && point.Ownership == owner) return "Choose a neutral or enemy point";
        return !requireKnown || Known(home, point) ? "" : "Target not discovered";
    }

    private static bool Known(FactionBase home, Node3D target)
    {
        if (!home.IsPlayerBase) return true; // Rival policy already navigates the seeded layout.
        var navigation = home.GetTree().GetFirstNodeInGroup("world_navigation") as WorldNavigation;
        if (navigation != null) return navigation.IsKnownTarget(target);
        var ship = home.GetTree().GetFirstNodeInGroup("player") as Player.ShipFlightController;
        var arena = home.GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        return target == home || target is StrategicPoint { Ownership: AntimatterOwnership.Player }
            || ship != null && (arena?.SurfaceDistance(ship.GlobalPosition, target.GlobalPosition)
                ?? ship.GlobalPosition.DistanceTo(target.GlobalPosition)) <= WorldNavigation.LocalRevealRange;
    }

    public static void Complete(FactionBase home, ConstructionProject project)
    {
        string capability = Capability(project.Definition.Id);
        // A target may die/change hands during production; an exact refund is
        // handled by construction before it marks the project completed.
        int rank = home.Construction.UpgradeRank(capability, true);
        var arena = home.GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        Vector3 start = home.GlobalPosition + home.GlobalBasis.X * 8 + home.GlobalBasis.Y * 4;
        if (arena != null) start = arena.OnSurface(start, 6);
        if (capability == "strike-drone" && project.OrderedTarget is FactionBase target)
        {
            var drone = new BaseStrikeDrone { Home = home, Target = target, IsPlayerOwned = home.IsPlayerBase,
                Damage = 80 + 40 * rank, MaxHealth = 15 + 15 * rank, Position = home.GetParent<Node3D>().ToLocal(start) };
            home.GetParent().AddChild(drone);
        }
        else
        {
            var role = capability == "scout-drone" ? BaseTacticalDrone.DroneRole.Scout
                : capability == "defensive-drone" ? BaseTacticalDrone.DroneRole.Defender : BaseTacticalDrone.DroneRole.Attacker;
            var drone = new BaseTacticalDrone { Home = home, Role = role, Target = project.OrderedTarget,
                Rank = rank, Position = home.GetParent<Node3D>().ToLocal(start) };
            home.GetParent().AddChild(drone);
        }
    }
}

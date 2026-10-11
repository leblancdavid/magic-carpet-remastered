using Godot;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MagicCarpetRemastered.Scripts.World;

// One broad-phase snapshot per physics frame, shared by attraction and possession.
// Exact availability, ownership and hit/contact tests remain with the callers.
public static class ManaNeighborhood
{
    private const float CellSize = 8;
    private sealed class Snapshot
    {
        public ulong Frame = ulong.MaxValue;
        public float MaximumRadius;
        public readonly Dictionary<Vector3I, List<ManaPickup>> Cells = new();
        public readonly List<ManaPickup> All = new();
        public readonly Stack<List<ManaPickup>> SpareLists = new();
    }
    private static readonly ConditionalWeakTable<SceneTree, Snapshot> Snapshots = new();

    public static void Invalidate(SceneTree tree) => Snapshots.GetOrCreateValue(tree).Frame = ulong.MaxValue;

    public static void Query(SceneTree tree, Vector3 center, float radius, List<ManaPickup> result)
    {
        Snapshot snapshot = Snapshots.GetOrCreateValue(tree);
        ulong frame = Engine.GetPhysicsFrames();
        if (snapshot.Frame != frame)
        {
            foreach (var occupants in snapshot.Cells.Values)
            {
                occupants.Clear();
                snapshot.SpareLists.Push(occupants);
            }
            snapshot.Cells.Clear();
            snapshot.All.Clear();
            snapshot.MaximumRadius = 0;
            var nodes = tree.GetNodesInGroup("loose_mana");
            foreach (Node node in nodes)
            {
                if (node is not ManaPickup orb || orb.IsQueuedForDeletion()) continue;
                snapshot.All.Add(orb);
                snapshot.MaximumRadius = Mathf.Max(snapshot.MaximumRadius, Mathf.Max(orb.Radius, orb.TargetRadius));
                Vector3I cell = Cell(orb.GlobalPosition);
                if (!snapshot.Cells.TryGetValue(cell, out var occupants))
                    snapshot.Cells[cell] = occupants = snapshot.SpareLists.Count > 0 ? snapshot.SpareLists.Pop() : new List<ManaPickup>();
                occupants.Add(orb);
            }
            snapshot.Frame = frame;
        }
        result.Clear();
        // Include neighbor radii and one cell of motion/growth tolerance between
        // the snapshot and callers. Narrow-phase tests reject the extra candidates.
        float extent = radius + snapshot.MaximumRadius + CellSize;
        Vector3I min = Cell(center - Vector3.One * extent);
        Vector3I max = Cell(center + Vector3.One * extent);
        long cells = (long)(max.X - min.X + 1) * (max.Y - min.Y + 1) * (max.Z - min.Z + 1);
        if (cells > snapshot.All.Count * 8L)
        {
            foreach (var orb in snapshot.All)
                if (GodotObject.IsInstanceValid(orb)) result.Add(orb);
            return;
        }
        for (int z = min.Z; z <= max.Z; z++)
        for (int y = min.Y; y <= max.Y; y++)
        for (int x = min.X; x <= max.X; x++)
            if (snapshot.Cells.TryGetValue(new Vector3I(x, y, z), out var occupants))
                foreach (var orb in occupants)
                    if (GodotObject.IsInstanceValid(orb)) result.Add(orb);
    }

    private static Vector3I Cell(Vector3 position) => new(
        Mathf.FloorToInt(position.X / CellSize), Mathf.FloorToInt(position.Y / CellSize), Mathf.FloorToInt(position.Z / CellSize));
}

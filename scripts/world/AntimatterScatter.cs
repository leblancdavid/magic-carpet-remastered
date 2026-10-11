using Godot;
using System.Collections.Generic;

namespace MagicCarpetRemastered.Scripts.World;

// Shared death/spill drop: physical fragments, not extra resource creation.
public static class AntimatterScatter
{
    public const float DropIntervalSeconds = 0.05f;
    public static void Spawn(Node parent, Vector3 origin, int amount,
        AntimatterOwnership ownership = AntimatterOwnership.Neutral, float minimumRadius = 0,
        string name = "ScatteredAntimatter")
    {
        if (amount <= 0) return;
        int count = Mathf.Min(128, (amount - 1) / 2 + 1);
        // Random integer cuts partition the reserve exactly, with no zero-sized
        // fragments or work proportional to a potentially enormous antimatter total.
        var cuts = new HashSet<int>();
        while (cuts.Count < count - 1) cuts.Add(GD.RandRange(1, amount - 1));
        var sortedCuts = new List<int>(cuts);
        sortedCuts.Sort();
        sortedCuts.Add(amount);
        var amounts = new List<int>();
        int previousCut = 0;
        foreach (int cut in sortedCuts)
        {
            amounts.Add(cut - previousCut);
            previousCut = cut;
        }
        // A random equal partition can still occur for small reserves. Ensure
        // visible variation whenever the amount allows it (two antimatter cannot).
        if (count > 1 && amount > count && amounts.TrueForAll(value => value == amounts[0]))
        {
            int donor = GD.RandRange(0, count - 1);
            int receiver = (donor + GD.RandRange(1, count - 1)) % count;
            amounts[donor]--;
            amounts[receiver]++;
        }
        var arena = parent.GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        float phase = GD.Randf() * Mathf.Tau;
        var placed = new List<AntimatterPickup>();
        for (int i = 0; i < count; i++)
        {
            var orb = new AntimatterPickup
            {
                Name = name,
                AntimatterAmount = amounts[i],
                Ownership = ownership
            };
            // Compact placement needs only enough separation to prevent the
            // solver explosively pushing overlapping fragments apart. Base
            // drops retain their explicit minimum distance outside the keep.
            float angle = phase + i * 2.399963f;
            Vector3 up = arena?.UpAt(origin) ?? Vector3.Up;
            Vector3 forward = Mathf.Abs(up.Dot(Vector3.Forward)) < 0.9f ? Vector3.Forward : Vector3.Right;
            Basis frame = PlanetGeometry.Frame(up, forward);
            Vector3 outward = frame * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 position = origin + outward * (minimumRadius + orb.Radius + (i % 3) * 0.15f);
            if (arena != null)
            {
                position = arena.ClampFlightPosition(position, orb.Radius + 0.2f);
            }
            bool overlaps;
            do
            {
                overlaps = false;
                foreach (AntimatterPickup previous in placed)
                {
                    if (position.DistanceTo(previous.GlobalPosition) >= orb.Radius + previous.Radius + 0.05f) continue;
                    position += outward * (orb.Radius * 0.5f);
                    if (arena != null) position = arena.ClampFlightPosition(position, orb.Radius + 0.2f);
                    overlaps = true;
                    break;
                }
            } while (overlaps);
            parent.AddChild(orb);
            placed.Add(orb);
            // No outward/upward impulse: gravity drops the loose cluster from
            // its release height (radially inward on a spherical world).
            orb.ScheduleDrop(position, i * DropIntervalSeconds);
        }
    }
}

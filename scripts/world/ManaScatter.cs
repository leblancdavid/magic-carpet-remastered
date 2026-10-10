using Godot;

namespace MagicCarpetRemastered.Scripts.World;

// Shared death/spill burst: physical fragments, not extra resource creation.
public static class ManaScatter
{
    public static void Spawn(Node parent, Vector3 origin, int amount,
        ManaOwnership ownership = ManaOwnership.Neutral, float minimumRadius = 1.5f,
        string name = "ScatteredMana")
    {
        if (amount <= 0) return;
        int count = Mathf.Min(128, (amount - 1) / 2 + 1);
        var arena = parent.GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        float phase = GD.Randf() * Mathf.Tau;
        for (int i = 0; i < count; i++)
        {
            var orb = new ManaPickup
            {
                Name = name,
                ManaAmount = amount / count + (i < amount % count ? 1 : 0),
                Ownership = ownership
            };
            // Golden-angle placement avoids aligned columns and overlapping fragments.
            float angle = phase + i * 2.399963f;
            Vector3 up = arena?.UpAt(origin) ?? Vector3.Up;
            Vector3 forward = Mathf.Abs(up.Dot(Vector3.Forward)) < 0.9f ? Vector3.Forward : Vector3.Right;
            Basis frame = PlanetGeometry.Frame(up, forward);
            Vector3 outward = frame * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 position = origin + outward * (minimumRadius + orb.Radius + (i % 3) * 0.65f);
            if (arena != null)
            {
                position = arena.ClampFlightPosition(position, orb.Radius + 0.2f);
            }
            parent.AddChild(orb);
            orb.LaunchScatter(position, outward * (6.0f + (i % 4) * 1.3f) + up * (5.0f + i % 3));
        }
    }
}

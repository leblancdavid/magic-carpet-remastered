using Godot;

namespace MagicCarpetRemastered.Scripts.World;

// Validate candidate height data before allocating terrain meshes or actors.
public static class PlanetGenerationValidation
{
    public static bool Accept(HeightmapArena arena, PlanetLayout layout, out string reason)
    {
        int land = 0, gentle = 0, steep = 0;
        const int count = 4096;
        for (int i = 0; i < count; i++)
        {
            float y = 1 - 2 * (i + 0.5f) / count, a = i * 2.39996323f + 0.37f, r = Mathf.Sqrt(1 - y * y);
            Vector3 up = new(r * Mathf.Cos(a), y, r * Mathf.Sin(a));
            Vector3 point = arena.PlanetCenter + up * arena.PlanetRadius;
            float height = arena.HeightAt(point);
            if (!float.IsFinite(height)) { reason = "Non-finite terrain"; return false; }
            if (height <= arena.WaterHeight) continue;
            land++;
            float slope = Slope(arena, up);
            if (slope <= Mathf.Tan(Mathf.DegToRad(20))) gentle++;
            if (slope > Mathf.Tan(Mathf.DegToRad(40))) steep++;
        }
        if (land < count * 0.25f || land > count * 0.85f || gentle < land * 0.85f
            || steep < land * 0.03f || steep > land * 0.05f)
        { reason = $"Land/slope budget: {land}/{count} land, {(float)gentle / Mathf.Max(1, land):P1} gentle, {(float)steep / Mathf.Max(1, land):P1} steep"; return false; }
        for (int side = 0; side < 2; side++)
        {
            Vector3 start = layout.Starts[side];
            if (!Link(arena, start, layout.AntimatterFields[side]) || !Link(arena, start, layout.MatterFields[side * 2]))
            { reason = "Opening resource approach"; return false; }
            for (int role = 0; role < 4; role++)
                if (!Link(arena, start, layout.EconomicSites[side * 4 + role].Up))
                { reason = "Opening station approach"; return false; }
        }
        reason = string.Empty; return true;
    }

    private static float Slope(HeightmapArena arena, Vector3 up)
    {
        Vector3 p = arena.PlanetCenter + up * arena.PlanetRadius;
        Basis frame = PlanetGeometry.Frame(up, Mathf.Abs(up.Z) < 0.9f ? Vector3.Forward : Vector3.Right);
        return new Vector2((arena.SurfaceHeight(p + frame.X) - arena.SurfaceHeight(p - frame.X)) / 2,
            (arena.SurfaceHeight(p + frame.Z) - arena.SurfaceHeight(p - frame.Z)) / 2).Length();
    }

    private static bool Link(HeightmapArena arena, Vector3 from, Vector3 to)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(arena.PlanetRadius * Mathf.Acos(Mathf.Clamp(from.Dot(to), -1, 1))));
        for (int i = 0; i <= steps; i++)
            if (Slope(arena, from.Slerp(to, (float)i / steps).Normalized()) > Mathf.Tan(Mathf.DegToRad(30))) return false;
        return true;
    }
}

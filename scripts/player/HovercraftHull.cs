using Godot;

namespace MagicCarpetRemastered.Scripts.Player;

// Original faceted delta hull, nose along local -Z. Banking belongs to its visual parent.
public static class HovercraftHull
{
    public static ArrayMesh Create()
    {
        Vector3[] points = {
            new(0, 0.08f, -1.65f), new(-1.15f, 0.08f, 1.1f), new(1.15f, 0.08f, 1.1f),
            new(0, -0.18f, -1.65f), new(-1.15f, -0.18f, 1.1f), new(1.15f, -0.18f, 1.1f),
            new(0, 0.48f, 0.35f)
        };
        int[] triangles = { 0, 6, 1, 1, 6, 2, 2, 6, 0,
            3, 4, 5, 0, 1, 4, 0, 4, 3, 1, 2, 5, 1, 5, 4, 2, 0, 3, 2, 3, 5 };
        using var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = points[triangles[i]], b = points[triangles[i + 1]], c = points[triangles[i + 2]];
            surface.SetNormal((c - a).Cross(b - a).Normalized());
            surface.AddVertex(a); surface.AddVertex(b); surface.AddVertex(c);
        }
        return surface.Commit();
    }
}

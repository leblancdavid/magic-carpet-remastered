using Godot;
using System.Collections.Generic;

namespace MagicCarpetRemastered.Scripts.World;

// Cube-sphere patches: shared direction-based height evaluation keeps edits
// consistent across face boundaries. Fine collision/detail follows world actors.
public partial class PlanetTerrain : Node3D
{
    public HeightmapArena Arena { get; set; } = null!;
    private const int PatchesPerFace = 8;
    private sealed record Edit(Vector3 Up, float Radius, float Amount, bool Flatten);
    private sealed class Patch
    {
        public int Face, X, Y, Resolution;
        public Vector3 Up;
        public float Bound;
        public MeshInstance3D Mesh = null!;
        public CollisionShape3D Collision = null!;
    }
    private readonly List<Patch> _patches = new();
    private readonly List<Edit> _edits = new();
    private readonly HashSet<Patch> _dirty = new();
    private readonly FastNoiseLite _noise = new() { Seed = 27031, Frequency = 0.004f, FractalOctaves = 3 };
    private readonly StandardMaterial3D _material = new() { AlbedoColor = new Color(0.65f, 0.65f, 0.65f), VertexColorUseAsAlbedo = true, Roughness = 1 };
    private float _refreshTimer;
    private bool _rebuildQueued;

    public override void _Ready()
    {
        for (int face = 0; face < 6; face++)
        for (int y = 0; y < PatchesPerFace; y++)
        for (int x = 0; x < PatchesPerFace; x++)
        {
            Patch patch = new() { Face = face, X = x, Y = y, Up = Direction(face, x + 0.5f, y + 0.5f) };
            patch.Bound = Arena.PlanetRadius * patch.Up.DistanceTo(Direction(face, x, y)) * 1.5f;
            patch.Mesh = new MeshInstance3D();
            AddChild(patch.Mesh);
            StaticBody3D body = new();
            body.AddToGroup("deformable_terrain");
            patch.Collision = new CollisionShape3D();
            body.AddChild(patch.Collision);
            AddChild(body);
            _patches.Add(patch);
            BuildPatch(patch, 4);
        }
        float sea = Arena.PlanetRadius + Arena.WaterHeight;
        AddChild(new MeshInstance3D
        {
            Name = "Ocean",
            Position = Arena.PlanetCenter,
            Mesh = new SphereMesh { Radius = sea, Height = sea * 2, RadialSegments = 512, Rings = 256 },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.03f, 0.15f, 0.3f), Roughness = 0.35f }
        });
        RefreshDetail();
    }

    public float HeightAt(Vector3 up)
    {
        Vector3 point = up * Arena.PlanetRadius;
        float height = 4 + _noise.GetNoise3Dv(point) * 18;
        // Guarantee a broad playable landmass around the initial strategic loop.
        float homeDistance = Arena.PlanetRadius * Mathf.Acos(Mathf.Clamp(up.Dot(Vector3.Up), -1, 1));
        height = Mathf.Lerp(height, 7 + Mathf.Sin(point.X * 0.028f) * 2 + Mathf.Cos(point.Z * 0.022f) * 2,
            Mathf.Clamp(1 - (homeDistance - 90) / 120, 0, 1));
        foreach (Edit edit in _edits)
        {
            float chord = up.DistanceTo(edit.Up) * Arena.PlanetRadius;
            if (chord >= edit.Radius) continue;
            float t = chord / edit.Radius;
            float weight = 1 - t * t * (3 - 2 * t);
            height = edit.Flatten ? Mathf.Lerp(height, edit.Amount, weight)
                : Mathf.Max(Arena.WaterHeight - 2, height + edit.Amount * weight);
        }
        return height;
    }

    public override void _PhysicsProcess(double delta)
    {
        _refreshTimer -= (float)delta;
        if (_refreshTimer > 0) return;
        _refreshTimer = 0.5f;
        RefreshDetail();
    }

    public void RefreshDetail()
    {
        List<Vector3> observers = new();
        foreach (string group in new[] { "player", "loose_mana", "ecosystem_enemy", "simple_monster", "balloon", "guardian" })
            foreach (Node node in GetTree().GetNodesInGroup(group))
                if (node is Node3D actor && !actor.IsQueuedForDeletion()) observers.Add(Arena.UpAt(actor.GlobalPosition));
        int rebuilt = 0;
        ulong start = Time.GetTicksUsec();
        foreach (Patch patch in _patches)
        {
            bool nearby = observers.Count == 0 && patch.Up.DistanceTo(Vector3.Up) * Arena.PlanetRadius < 180 + patch.Bound;
            foreach (Vector3 observer in observers)
                if (patch.Up.DistanceTo(observer) * Arena.PlanetRadius < 100 + patch.Bound) { nearby = true; break; }
            // Fixed world-space resolution (about 2 units at face center).
            int resolution = nearby ? Mathf.CeilToInt(Arena.PlanetRadius / 16) * 2 : 4;
            if (resolution == patch.Resolution) continue;
            BuildPatch(patch, resolution);
            rebuilt++;
        }
        if (rebuilt > 0) Arena.ReportPlanetRebuild(rebuilt, (Time.GetTicksUsec() - start) / 1000.0);
    }

    public int Deform(Vector3 up, float radius, float amount, bool flatten)
    {
        if (radius <= 0) return 0;
        _edits.Add(new Edit(up, radius, amount, flatten));
        int editedVertices = 0;
        foreach (Patch patch in _patches)
        {
            if (up.DistanceTo(patch.Up) * Arena.PlanetRadius > radius + patch.Bound) continue;
            _dirty.Add(patch);
            int resolution = Mathf.Max(patch.Resolution, Mathf.CeilToInt(Arena.PlanetRadius / 16) * 2);
            for (int y = 0; y <= resolution; y++)
            for (int x = 0; x <= resolution; x++)
                if (up.DistanceTo(Direction(patch.Face, patch.X + (float)x / resolution, patch.Y + (float)y / resolution)) * Arena.PlanetRadius < radius)
                    editedVertices++;
        }
        if (!_rebuildQueued)
        {
            _rebuildQueued = true;
            Callable.From(RebuildDirty).CallDeferred();
        }
        return editedVertices;
    }

    private void RebuildDirty()
    {
        ulong start = Time.GetTicksUsec();
        foreach (Patch patch in _dirty) BuildPatch(patch, Mathf.Max(patch.Resolution, Mathf.CeilToInt(Arena.PlanetRadius / 16) * 2));
        Arena.ReportPlanetRebuild(_dirty.Count, (Time.GetTicksUsec() - start) / 1000.0);
        _dirty.Clear();
        _rebuildQueued = false;
    }

    private void BuildPatch(Patch patch, int resolution)
    {
        patch.Resolution = resolution;
        int side = resolution + 1;
        Vector3[] vertices = new Vector3[side * side];
        Vector3[] normals = new Vector3[vertices.Length];
        Color[] colors = new Color[vertices.Length];
        int[] indices = new int[resolution * resolution * 6];
        for (int y = 0; y <= resolution; y++)
        for (int x = 0; x <= resolution; x++)
        {
            int index = y * side + x;
            Vector3 up = Direction(patch.Face, patch.X + (float)x / resolution, patch.Y + (float)y / resolution);
            float height = HeightAt(up);
            vertices[index] = Arena.PlanetCenter + up * (Arena.PlanetRadius + height);
            Vector3 tangent = up.Cross(Mathf.Abs(up.Y) < 0.9f ? Vector3.Up : Vector3.Right).Normalized();
            Vector3 bitangent = up.Cross(tangent);
            float dx = HeightAt((up + tangent / Arena.PlanetRadius).Normalized()) - HeightAt((up - tangent / Arena.PlanetRadius).Normalized());
            float dy = HeightAt((up + bitangent / Arena.PlanetRadius).Normalized()) - HeightAt((up - bitangent / Arena.PlanetRadius).Normalized());
            normals[index] = (up - tangent * dx * 0.5f - bitangent * dy * 0.5f).Normalized();
            colors[index] = height < 0.5f ? new Color(0.62f, 0.52f, 0.28f)
                : height < 6 ? new Color(0.22f, 0.42f, 0.16f)
                : height < 10 ? new Color(0.32f, 0.48f, 0.22f) : new Color(0.44f, 0.42f, 0.35f);
        }
        int cursor = 0;
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            int a = y * side + x, b = a + 1, c = a + side, d = c + 1;
            WriteTriangle(a, b, c);
            WriteTriangle(b, d, c);
        }
        void WriteTriangle(int a, int b, int c)
        {
            if ((vertices[b] - vertices[a]).Cross(vertices[c] - vertices[a]).Dot(vertices[a] - Arena.PlanetCenter) > 0) (b, c) = (c, b);
            indices[cursor++] = a; indices[cursor++] = b; indices[cursor++] = c;
        }
        Godot.Collections.Array arrays = new();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.Color] = colors;
        arrays[(int)Mesh.ArrayType.Index] = indices;
        ArrayMesh mesh = new();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        // Visual skirts close the geometric gap between different patch LODs.
        // Nearby colliders share the same fine sampling, including cube-face edges.
        SurfaceTool skirt = new();
        skirt.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < resolution; i++)
        {
            Edge(i, i + 1); Edge(resolution * side + i + 1, resolution * side + i);
            Edge((i + 1) * side, i * side); Edge(i * side + resolution, (i + 1) * side + resolution);
        }
        void Edge(int a, int b)
        {
            Vector3 bottomA = vertices[a] - (vertices[a] - Arena.PlanetCenter).Normalized() * 8;
            Vector3 bottomB = vertices[b] - (vertices[b] - Arena.PlanetCenter).Normalized() * 8;
            skirt.SetColor(colors[a]); skirt.AddVertex(vertices[a]);
            skirt.SetColor(colors[b]); skirt.AddVertex(vertices[b]);
            skirt.SetColor(colors[a]); skirt.AddVertex(bottomA);
            skirt.SetColor(colors[b]); skirt.AddVertex(vertices[b]);
            skirt.AddVertex(bottomB);
            skirt.SetColor(colors[a]); skirt.AddVertex(bottomA);
        }
        skirt.GenerateNormals();
        skirt.Commit(mesh);
        patch.Mesh.Mesh = mesh;
        patch.Mesh.MaterialOverride = _material;
        Vector3[] faces = new Vector3[indices.Length];
        for (int i = 0; i < indices.Length; i++) faces[i] = vertices[indices[i]];
        patch.Collision.Shape = new ConcavePolygonShape3D { Data = faces };
    }

    private static Vector3 Direction(int face, float x, float y)
    {
        float u = x * 2 / PatchesPerFace - 1, v = y * 2 / PatchesPerFace - 1;
        return (face switch
        {
            0 => new Vector3(u, 1, v), 1 => new Vector3(u, -1, -v),
            2 => new Vector3(1, v, u), 3 => new Vector3(-1, v, -u),
            4 => new Vector3(u, v, -1), _ => new Vector3(-u, v, 1)
        }).Normalized();
    }
}

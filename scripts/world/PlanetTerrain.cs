using Godot;
using System.Collections.Generic;

namespace MagicCarpetRemastered.Scripts.World;

// Cube-sphere patches: shared direction-based height evaluation keeps edits
// consistent across face boundaries. Fine collision/detail follows world actors.
public partial class PlanetTerrain : Node3D
{
    public HeightmapArena Arena { get; set; } = null!;
    private const int PatchesPerFace = 8;
    public const int MaxTileResolution = 25;
    private sealed record Edit(Vector3 Up, float Radius, float Amount, bool Flatten);
    private sealed class Patch
    {
        public int Face, Resolution, DesiredResolution;
        public float X, Y, Span = 1;
        public Patch? Parent;
        public readonly List<Patch> Tiles = new();
        public bool TilesPublished;
        public Vector3 Up;
        public float Bound;
        public MeshInstance3D Mesh = null!;
        public CollisionShape3D Collision = null!;
        public Vector3[] Vertices = System.Array.Empty<Vector3>();
        public Vector3[] Normals = System.Array.Empty<Vector3>();
        public Color[] Colors = System.Array.Empty<Color>();
        public readonly List<Edit> PendingEdits = new();
    }
    private readonly List<Patch> _patches = new();
    private const float EditCellSize = 32;
    private readonly Dictionary<Vector3I, List<Edit>> _editCells = new();
    private readonly HashSet<Patch> _dirty = new();
    private readonly FastNoiseLite _noise = new() { Seed = 27031, Frequency = 0.004f, FractalOctaves = 3 };
    private readonly StandardMaterial3D _material = new() { AlbedoColor = new Color(0.65f, 0.65f, 0.65f), VertexColorUseAsAlbedo = true, Roughness = 1 };
    private float _refreshTimer;
    public int PendingPatchCount => _dirty.Count;
    public double RebuildBudgetMilliseconds { get; set; } = 2;
    public double FrameRebuildMilliseconds { get; private set; }
    public int FrameRebuiltPatchCount { get; private set; }
    public int FrameLargestTileResolution { get; private set; }

    public override void _Ready()
    {
        for (int face = 0; face < 6; face++)
        for (int y = 0; y < PatchesPerFace; y++)
        for (int x = 0; x < PatchesPerFace; x++)
        {
            Patch patch = new() { Face = face, X = x, Y = y, Up = Direction(face, x + 0.5f, y + 0.5f) };
            SetBound(patch);
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
        // Loading establishes actor collision before gameplay starts. Runtime
        // detail changes and craters use the bounded queue instead.
        while (_dirty.Count > 0) RebuildDirty();
    }

    public float HeightAt(Vector3 up)
    {
        Vector3 point = up * Arena.PlanetRadius;
        float height = 4 + _noise.GetNoise3Dv(point) * 18;
        // Guarantee a broad playable landmass around the initial strategic loop.
        float homeDistance = Arena.PlanetRadius * Mathf.Acos(Mathf.Clamp(up.Dot(Vector3.Up), -1, 1));
        height = Mathf.Lerp(height, 7 + Mathf.Sin(point.X * 0.028f) * 2 + Mathf.Cos(point.Z * 0.022f) * 2,
            Mathf.Clamp(1 - (homeDistance - 90) / 120, 0, 1));
        if (!_editCells.TryGetValue(EditCell(point), out var edits)) return height;
        foreach (Edit edit in edits)
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
        if (_refreshTimer <= 0)
        {
            _refreshTimer = 0.5f;
            RefreshDetail();
        }
    }

    public override void _Process(double delta)
    {
        FrameRebuildMilliseconds = 0;
        FrameRebuiltPatchCount = 0;
        FrameLargestTileResolution = 0;
        if (_dirty.Count > 0) RebuildDirty();
        if (FrameRebuiltPatchCount > 0) FrameRebuildMilliseconds = Arena.LastRebuildMilliseconds;
    }

    public void RefreshDetail()
    {
        List<Vector3> observers = new();
        foreach (string group in new[] { "player", "loose_mana", "ecosystem_enemy", "simple_monster", "balloon", "guardian" })
            foreach (Node node in GetTree().GetNodesInGroup(group))
                if (node is Node3D actor && !actor.IsQueuedForDeletion() && node is not ManaPickup { IsAvailable: false })
                    observers.Add(Arena.UpAt(actor.GlobalPosition));
        foreach (Patch patch in _patches)
        {
            bool nearby = observers.Count == 0 && patch.Up.DistanceTo(Vector3.Up) * Arena.PlanetRadius < 180 + patch.Bound;
            foreach (Vector3 observer in observers)
                if (patch.Up.DistanceTo(observer) * Arena.PlanetRadius < (patch.Tiles.Count > 0 ? 125 : 100) + patch.Bound) { nearby = true; break; }
            // Fixed world-space resolution (about 2 units at face center).
            int resolution = nearby ? Mathf.CeilToInt(Arena.PlanetRadius / 16) * 2 : 4;
            patch.DesiredResolution = resolution;
            if (resolution > 4)
            {
                _dirty.Remove(patch);
                if (patch.Tiles.Count == 0) PrepareTiles(patch, resolution);
            }
            else if (patch.Tiles.Count > 0 || patch.Resolution != resolution) _dirty.Add(patch);
        }
    }

    public int Deform(Vector3 up, float radius, float amount, bool flatten)
    {
        if (radius <= 0) return 0;
        var edit = new Edit(up, radius, amount, flatten);
        Vector3 center = up * Arena.PlanetRadius;
        Vector3I min = EditCell(center - Vector3.One * radius);
        Vector3I max = EditCell(center + Vector3.One * radius);
        for (int z = min.Z; z <= max.Z; z++)
        for (int y = min.Y; y <= max.Y; y++)
        for (int x = min.X; x <= max.X; x++)
        {
            Vector3I cell = new(x, y, z);
            if (!_editCells.TryGetValue(cell, out var edits)) _editCells[cell] = edits = new List<Edit>();
            edits.Add(edit); // Preserve chronological flatten/additive semantics.
        }
        int editedVertices = 0;
        foreach (Patch patch in _patches)
        {
            if (up.DistanceTo(patch.Up) * Arena.PlanetRadius > radius + 2 + patch.Bound) continue;
            // An active fine patch is rebuilt tile-by-tile. Pending initial tiles
            // evaluate the latest authoritative edit history when they are built.
            if (patch.Tiles.Count > 0 && patch.DesiredResolution > 4)
            {
                // Invalidate the dormant coarse cache rather than retaining an
                // ever-growing pending list while the fine surface stays active.
                patch.Vertices = System.Array.Empty<Vector3>();
                patch.PendingEdits.Clear();
                foreach (Patch tile in patch.Tiles)
                    if (up.DistanceTo(tile.Up) * Arena.PlanetRadius <= radius + 2 + tile.Bound)
                    {
                        _dirty.Add(tile);
                        tile.PendingEdits.Add(edit);
                    }
            }
            else
            {
                _dirty.Add(patch);
                patch.PendingEdits.Add(edit);
            }
            // Diagnostic estimate only: height changes are exact and immediate.
            // Avoid resampling entire fine patches merely to count edited vertices.
            float spacing = Arena.PlanetRadius / (4f * Mathf.Max(4, patch.DesiredResolution));
            editedVertices += Mathf.Max(1, Mathf.CeilToInt(Mathf.Pi * radius * radius / (spacing * spacing)));
        }
        return editedVertices;
    }

    private void RebuildDirty()
    {
        ulong start = Time.GetTicksUsec();
        int rebuilt = 0;
        while (_dirty.Count > 0)
        {
            Patch? selected = null;
            // Fine actor-supporting collision takes precedence over distant LOD.
            foreach (Patch candidate in _dirty)
                if (selected == null || candidate.DesiredResolution > selected.DesiredResolution) selected = candidate;
            _dirty.Remove(selected!);
            BuildPatch(selected!, Mathf.Max(4, selected!.DesiredResolution));
            FrameLargestTileResolution = Mathf.Max(FrameLargestTileResolution, selected!.Resolution);
            if (selected.Parent is Patch parent)
            {
                if (!parent.TilesPublished && parent.Tiles.TrueForAll(t => t.Resolution > 0 && !_dirty.Contains(t)))
                {
                    // Publish the complete replacement before removing its old
                    // collider. No holes while initial fine tiles are queued.
                    foreach (Patch tile in parent.Tiles)
                    {
                        tile.Mesh.Visible = true;
                        tile.Collision.Disabled = false;
                    }
                    parent.Mesh.Visible = false;
                    parent.Collision.Disabled = true;
                    parent.TilesPublished = true;
                }
            }
            else if (selected.Tiles.Count > 0)
            {
                // A coarse replacement is ready. Retire the fine geometry only
                // now, not when the LOD request first enters the queue.
                selected.Mesh.Visible = true;
                selected.Collision.Disabled = false;
                foreach (Patch tile in selected.Tiles)
                {
                    _dirty.Remove(tile);
                    if (tile.Resolution == 0) continue;
                    tile.Mesh.Visible = false;
                    tile.Collision.Disabled = true;
                    tile.Mesh.QueueFree();
                    tile.Collision.GetParent().QueueFree();
                }
                selected.Tiles.Clear();
                selected.TilesPublished = false;
            }
            rebuilt++;
            if ((Time.GetTicksUsec() - start) / 1000.0 >= RebuildBudgetMilliseconds) break;
        }
        FrameRebuiltPatchCount += rebuilt;
        Arena.ReportPlanetRebuild(rebuilt, (Time.GetTicksUsec() - start) / 1000.0);
    }

    private static Vector3I EditCell(Vector3 point) => new(Mathf.FloorToInt(point.X / EditCellSize),
        Mathf.FloorToInt(point.Y / EditCellSize), Mathf.FloorToInt(point.Z / EditCellSize));

    private void SetBound(Patch patch)
    {
        patch.Bound = 0;
        foreach (Vector2 corner in new[] { Vector2.Zero, Vector2.Right, Vector2.Down, Vector2.One })
            patch.Bound = Mathf.Max(patch.Bound, Arena.PlanetRadius * patch.Up.DistanceTo(
                Direction(patch.Face, patch.X + corner.X * patch.Span, patch.Y + corner.Y * patch.Span)));
        patch.Bound *= 1.1f;
    }

    private void PrepareTiles(Patch parent, int resolution)
    {
        // Pick a divisor, preserving every vertex of the original fine grid.
        int divisions = Mathf.CeilToInt((float)resolution / MaxTileResolution);
        while (resolution % divisions != 0) divisions++;
        float span = 1f / divisions;
        for (int y = 0; y < divisions; y++)
        for (int x = 0; x < divisions; x++)
        {
            Patch tile = new() { Parent = parent, Face = parent.Face,
                X = parent.X + x * span, Y = parent.Y + y * span, Span = span,
                DesiredResolution = resolution / divisions };
            tile.Up = Direction(tile.Face, tile.X + span * 0.5f, tile.Y + span * 0.5f);
            SetBound(tile);
            parent.Tiles.Add(tile);
            _dirty.Add(tile);
        }
    }

    private void BuildPatch(Patch patch, int resolution)
    {
        if (patch.Resolution == 0)
        {
            patch.Mesh = new MeshInstance3D { Visible = patch.Parent == null };
            AddChild(patch.Mesh);
            var body = new StaticBody3D();
            body.AddToGroup("deformable_terrain");
            patch.Collision = new CollisionShape3D { Disabled = patch.Parent != null };
            body.AddChild(patch.Collision);
            AddChild(body);
        }
        int side = resolution + 1;
        bool fullRebuild = patch.Resolution != resolution || patch.Vertices.Length == 0;
        patch.Resolution = resolution;
        Vector3[] vertices = fullRebuild ? new Vector3[side * side] : patch.Vertices;
        Vector3[] normals = fullRebuild ? new Vector3[vertices.Length] : patch.Normals;
        Color[] colors = fullRebuild ? new Color[vertices.Length] : patch.Colors;
        int[] indices = new int[resolution * resolution * 6];
        for (int y = 0; y <= resolution; y++)
        for (int x = 0; x <= resolution; x++)
        {
            int index = y * side + x;
            Vector3 up = Direction(patch.Face, patch.X + (float)x / resolution * patch.Span, patch.Y + (float)y / resolution * patch.Span);
            if (!fullRebuild)
            {
                bool affected = false;
                foreach (Edit edit in patch.PendingEdits)
                {
                    float reach = (edit.Radius + 2) / Arena.PlanetRadius;
                    if (up.DistanceSquaredTo(edit.Up) <= reach * reach) { affected = true; break; }
                }
                if (!affected) continue;
            }
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
        patch.Vertices = vertices;
        patch.Normals = normals;
        patch.Colors = colors;
        patch.PendingEdits.Clear();
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
        using SurfaceTool skirt = new();
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

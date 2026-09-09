using Godot;
using System.Collections.Generic;

namespace MagicCarpetRemastered.Scripts.World;

public partial class HeightmapArena : Node3D
{
    [Export] public int CellsPerSide { get; set; } = 72;
    [Export] public int ChunkCells { get; set; } = 12;
    [Export] public float CellSize { get; set; } = 2.0f;
    [Export] public float HeightScale { get; set; } = 9.0f;
    [Export] public float WaterHeight { get; set; } = -0.8f;

    public int LastEditedVertexCount { get; private set; }
    public int LastRebuiltChunkCount { get; private set; }
    public double LastRebuildMilliseconds { get; private set; }

    private float[,] _heights = null!;
    private readonly Dictionary<Vector2I, TerrainChunk> _chunks = new();
    private readonly HashSet<Vector2I> _pendingChunks = new();
    private StandardMaterial3D? _terrainMaterial;
    private int _pendingEditedVertexCount;
    private bool _rebuildQueued;

    public override void _Ready()
    {
        AddToGroup("heightmap_arena");
        GenerateHeightmap();
        BuildTerrain();
        BuildWater();
    }

    public void ApplyCrater(Vector3 worldPosition, float radius = 5.0f, float depth = 2.6f)
    {
        ApplyRadialDeformation(worldPosition, radius, -Mathf.Abs(depth));
    }

    public void RaiseTerrain(Vector3 worldPosition, float radius, float amount)
    {
        ApplyRadialDeformation(worldPosition, radius, Mathf.Abs(amount));
    }

    public void LowerTerrain(Vector3 worldPosition, float radius, float amount)
    {
        ApplyRadialDeformation(worldPosition, radius, -Mathf.Abs(amount));
    }

    public void FlattenTerrain(Vector3 worldPosition, float radius, float targetHeight)
    {
        if (_heights == null)
        {
            GenerateHeightmap();
        }

        GetGridBounds(worldPosition, radius, out int minX, out int maxX, out int minZ, out int maxZ);

        int editedVertices = 0;
        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector3 vertex = PointAt(x, z);
                float distance = new Vector2(vertex.X - worldPosition.X, vertex.Z - worldPosition.Z).Length();
                if (distance > radius)
                {
                    continue;
                }

                float weight = SmoothFalloff(distance / radius);
                _heights![x, z] = Mathf.Lerp(_heights[x, z], targetHeight, weight);
                editedVertices++;
            }
        }

        RegisterTerrainEdit(editedVertices, minX, maxX, minZ, maxZ);
    }

    private void BuildTerrain()
    {
        ulong rebuildStartUsec = Time.GetTicksUsec();

        EnsureTerrainMaterial();
        int chunkCount = Mathf.CeilToInt((float)CellsPerSide / ChunkCells);
        for (int chunkZ = 0; chunkZ < chunkCount; chunkZ++)
        {
            for (int chunkX = 0; chunkX < chunkCount; chunkX++)
            {
                RebuildChunk(new Vector2I(chunkX, chunkZ));
            }
        }

        LastRebuiltChunkCount = chunkCount * chunkCount;
        LastRebuildMilliseconds = (Time.GetTicksUsec() - rebuildStartUsec) / 1000.0;
    }

    private void BuildWater()
    {
        var water = new MeshInstance3D
        {
            Name = "Water",
            Position = new Vector3(0.0f, WaterHeight, 0.0f),
            Mesh = new PlaneMesh { Size = new Vector2(CellsPerSide * CellSize * 1.5f, CellsPerSide * CellSize * 1.5f) }
        };
        water.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.03f, 0.12f, 0.28f),
            Roughness = 0.35f,
            Metallic = 0.0f
        };
        AddChild(water);
    }

    private Vector3 PointAt(int x, int z)
    {
        float half = CellsPerSide * CellSize * 0.5f;
        float worldX = x * CellSize - half;
        float worldZ = z * CellSize - half;
        return new Vector3(worldX, _heights[x, z], worldZ);
    }

    public float HeightAt(float x, float z)
    {
        if (_heights == null)
        {
            GenerateHeightmap();
        }

        float half = CellsPerSide * CellSize * 0.5f;
        float gridX = Mathf.Clamp((x + half) / CellSize, 0.0f, CellsPerSide);
        float gridZ = Mathf.Clamp((z + half) / CellSize, 0.0f, CellsPerSide);
        int x0 = Mathf.FloorToInt(gridX);
        int z0 = Mathf.FloorToInt(gridZ);
        int x1 = Mathf.Min(x0 + 1, CellsPerSide);
        int z1 = Mathf.Min(z0 + 1, CellsPerSide);
        float tx = gridX - x0;
        float tz = gridZ - z0;

        float north = Mathf.Lerp(_heights![x0, z0], _heights[x1, z0], tx);
        float south = Mathf.Lerp(_heights[x0, z1], _heights[x1, z1], tx);
        return Mathf.Lerp(north, south, tz);
    }

    private void GenerateHeightmap()
    {
        _heights = new float[CellsPerSide + 1, CellsPerSide + 1];
        for (int z = 0; z <= CellsPerSide; z++)
        {
            for (int x = 0; x <= CellsPerSide; x++)
            {
                float half = CellsPerSide * CellSize * 0.5f;
                float worldX = x * CellSize - half;
                float worldZ = z * CellSize - half;
                _heights[x, z] = ProceduralHeightAt(worldX, worldZ);
            }
        }
    }

    private float ProceduralHeightAt(float x, float z)
    {
        float halfWidth = CellsPerSide * CellSize * 0.5f;
        float radial = new Vector2(x, z).Length() / halfWidth;
        float islandMask = Mathf.Clamp(1.0f - radial * radial, 0.0f, 1.0f);
        float rolling = Mathf.Sin(x * 0.095f) * 0.45f + Mathf.Cos(z * 0.085f) * 0.4f;
        float ridges = Mathf.Sin((x + z) * 0.045f) * 0.7f + Mathf.Cos((x - z) * 0.07f) * 0.35f;
        return WaterHeight + islandMask * (HeightScale + rolling * 2.5f + ridges * 1.75f);
    }

    private void ApplyRadialDeformation(Vector3 worldPosition, float radius, float amount)
    {
        if (_heights == null)
        {
            GenerateHeightmap();
        }

        GetGridBounds(worldPosition, radius, out int minX, out int maxX, out int minZ, out int maxZ);

        int editedVertices = 0;
        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector3 vertex = PointAt(x, z);
                float distance = new Vector2(vertex.X - worldPosition.X, vertex.Z - worldPosition.Z).Length();
                if (distance > radius)
                {
                    continue;
                }

                float weight = SmoothFalloff(distance / radius);
                _heights![x, z] = Mathf.Max(WaterHeight - 2.0f, _heights[x, z] + amount * weight);
                editedVertices++;
            }
        }

        RegisterTerrainEdit(editedVertices, minX, maxX, minZ, maxZ);
    }

    private void RegisterTerrainEdit(int editedVertices, int minX, int maxX, int minZ, int maxZ)
    {
        if (editedVertices <= 0)
        {
            LastEditedVertexCount = 0;
            return;
        }

        _pendingEditedVertexCount += editedVertices;
        LastEditedVertexCount = _pendingEditedVertexCount;
        QueueAffectedChunks(minX, maxX, minZ, maxZ);
        QueueTerrainRebuild();
    }

    private void QueueTerrainRebuild()
    {
        if (_rebuildQueued)
        {
            return;
        }

        _rebuildQueued = true;
        CallDeferred(nameof(RebuildTerrain));
    }

    private void RebuildTerrain()
    {
        ulong rebuildStartUsec = Time.GetTicksUsec();
        int rebuiltChunks = 0;

        _rebuildQueued = false;
        foreach (Vector2I chunkKey in _pendingChunks)
        {
            RebuildChunk(chunkKey);
            rebuiltChunks++;
        }

        _pendingChunks.Clear();
        _pendingEditedVertexCount = 0;
        LastRebuiltChunkCount = rebuiltChunks;
        LastRebuildMilliseconds = (Time.GetTicksUsec() - rebuildStartUsec) / 1000.0;
    }

    private void RebuildChunk(Vector2I chunkKey)
    {
        EnsureTerrainMaterial();

        int startX = chunkKey.X * ChunkCells;
        int startZ = chunkKey.Y * ChunkCells;
        int endX = Mathf.Min(startX + ChunkCells, CellsPerSide);
        int endZ = Mathf.Min(startZ + ChunkCells, CellsPerSide);
        if (startX >= CellsPerSide || startZ >= CellsPerSide)
        {
            return;
        }

        TerrainChunk chunk = GetOrCreateChunk(chunkKey);
        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);

        var collisionTriangles = new List<Vector3>();
        for (int z = startZ; z < endZ; z++)
        {
            for (int x = startX; x < endX; x++)
            {
                Vector3 a = PointAt(x, z);
                Vector3 b = PointAt(x + 1, z);
                Vector3 c = PointAt(x, z + 1);
                Vector3 d = PointAt(x + 1, z + 1);

                AddTriangle(surface, collisionTriangles, a, c, b);
                AddTriangle(surface, collisionTriangles, b, c, d);
            }
        }

        surface.GenerateNormals();
        chunk.Mesh.Mesh = surface.Commit();
        chunk.Mesh.MaterialOverride = _terrainMaterial;
        chunk.Collision.Shape = new ConcavePolygonShape3D { Data = collisionTriangles.ToArray() };
    }

    private TerrainChunk GetOrCreateChunk(Vector2I chunkKey)
    {
        if (_chunks.TryGetValue(chunkKey, out TerrainChunk? existingChunk))
        {
            return existingChunk;
        }

        var mesh = new MeshInstance3D { Name = $"TerrainMesh_{chunkKey.X}_{chunkKey.Y}" };
        AddChild(mesh);

        var body = new StaticBody3D { Name = $"TerrainCollision_{chunkKey.X}_{chunkKey.Y}" };
        body.AddToGroup("deformable_terrain");
        var collision = new CollisionShape3D { Name = "CollisionShape3D" };
        body.AddChild(collision);
        AddChild(body);

        var chunk = new TerrainChunk(mesh, collision);
        _chunks[chunkKey] = chunk;
        return chunk;
    }

    private void EnsureTerrainMaterial()
    {
        _terrainMaterial ??= new StandardMaterial3D
        {
            AlbedoColor = new Color(0.24f, 0.34f, 0.18f),
            Roughness = 1.0f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            VertexColorUseAsAlbedo = true
        };
    }

    private static float SmoothFalloff(float normalizedDistance)
    {
        float t = Mathf.Clamp(normalizedDistance, 0.0f, 1.0f);
        return 1.0f - t * t * (3.0f - 2.0f * t);
    }

    private void GetGridBounds(Vector3 worldPosition, float radius, out int minX, out int maxX, out int minZ, out int maxZ)
    {
        float half = CellsPerSide * CellSize * 0.5f;
        minX = Mathf.Clamp(Mathf.FloorToInt((worldPosition.X - radius + half) / CellSize), 0, CellsPerSide);
        maxX = Mathf.Clamp(Mathf.CeilToInt((worldPosition.X + radius + half) / CellSize), 0, CellsPerSide);
        minZ = Mathf.Clamp(Mathf.FloorToInt((worldPosition.Z - radius + half) / CellSize), 0, CellsPerSide);
        maxZ = Mathf.Clamp(Mathf.CeilToInt((worldPosition.Z + radius + half) / CellSize), 0, CellsPerSide);
    }

    private void QueueAffectedChunks(int minX, int maxX, int minZ, int maxZ)
    {
        int minCellX = Mathf.Max(0, minX - 1);
        int maxCellX = Mathf.Min(CellsPerSide - 1, maxX);
        int minCellZ = Mathf.Max(0, minZ - 1);
        int maxCellZ = Mathf.Min(CellsPerSide - 1, maxZ);

        for (int chunkZ = minCellZ / ChunkCells; chunkZ <= maxCellZ / ChunkCells; chunkZ++)
        {
            for (int chunkX = minCellX / ChunkCells; chunkX <= maxCellX / ChunkCells; chunkX++)
            {
                _pendingChunks.Add(new Vector2I(chunkX, chunkZ));
            }
        }
    }

    private static void AddTriangle(SurfaceTool surface, List<Vector3> collisionTriangles, Vector3 a, Vector3 b, Vector3 c)
    {
        AddTerrainVertex(surface, a);
        AddTerrainVertex(surface, b);
        AddTerrainVertex(surface, c);

        collisionTriangles.Add(a);
        collisionTriangles.Add(b);
        collisionTriangles.Add(c);
    }

    private static void AddTerrainVertex(SurfaceTool surface, Vector3 vertex)
    {
        surface.SetColor(TerrainColor(vertex.Y));
        surface.AddVertex(vertex);
    }

    private static Color TerrainColor(float height)
    {
        if (height < 0.3f)
        {
            return new Color(0.62f, 0.52f, 0.28f);
        }

        if (height < 6.0f)
        {
            return new Color(0.22f, 0.42f, 0.16f);
        }

        return height < 9.0f
            ? new Color(0.32f, 0.48f, 0.22f)
            : new Color(0.44f, 0.42f, 0.35f);
    }

    private sealed record TerrainChunk(MeshInstance3D Mesh, CollisionShape3D Collision);
}

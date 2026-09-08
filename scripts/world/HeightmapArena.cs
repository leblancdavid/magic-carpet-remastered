using Godot;
using System.Collections.Generic;

namespace MagicCarpetRemastered.Scripts.World;

public partial class HeightmapArena : Node3D
{
    [Export] public int CellsPerSide { get; set; } = 72;
    [Export] public float CellSize { get; set; } = 2.0f;
    [Export] public float HeightScale { get; set; } = 9.0f;
    [Export] public float WaterHeight { get; set; } = -0.8f;

    private float[,] _heights = null!;
    private MeshInstance3D? _terrainMesh;
    private StaticBody3D? _terrainBody;

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

        bool changed = false;
        for (int z = 0; z <= CellsPerSide; z++)
        {
            for (int x = 0; x <= CellsPerSide; x++)
            {
                Vector3 vertex = PointAt(x, z);
                float distance = new Vector2(vertex.X - worldPosition.X, vertex.Z - worldPosition.Z).Length();
                if (distance > radius)
                {
                    continue;
                }

                float weight = SmoothFalloff(distance / radius);
                _heights![x, z] = Mathf.Lerp(_heights[x, z], targetHeight, weight);
                changed = true;
            }
        }

        if (changed)
        {
            RebuildTerrain();
        }
    }

    private void BuildTerrain()
    {
        _terrainMesh?.QueueFree();
        _terrainBody?.QueueFree();

        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);

        var collisionTriangles = new List<Vector3>();

        for (int z = 0; z < CellsPerSide; z++)
        {
            for (int x = 0; x < CellsPerSide; x++)
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
        var mesh = surface.Commit();

        _terrainMesh = new MeshInstance3D
        {
            Name = "TerrainMesh",
            Mesh = mesh
        };
        _terrainMesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.24f, 0.34f, 0.18f),
            Roughness = 1.0f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            VertexColorUseAsAlbedo = true
        };
        AddChild(_terrainMesh);

        _terrainBody = new StaticBody3D { Name = "TerrainCollision" };
        _terrainBody.AddToGroup("deformable_terrain");
        _terrainBody.AddChild(new CollisionShape3D
        {
            Shape = new ConcavePolygonShape3D { Data = collisionTriangles.ToArray() }
        });
        AddChild(_terrainBody);
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

        bool changed = false;
        for (int z = 0; z <= CellsPerSide; z++)
        {
            for (int x = 0; x <= CellsPerSide; x++)
            {
                Vector3 vertex = PointAt(x, z);
                float distance = new Vector2(vertex.X - worldPosition.X, vertex.Z - worldPosition.Z).Length();
                if (distance > radius)
                {
                    continue;
                }

                float weight = SmoothFalloff(distance / radius);
                _heights![x, z] = Mathf.Max(WaterHeight - 2.0f, _heights[x, z] + amount * weight);
                changed = true;
            }
        }

        if (changed)
        {
            RebuildTerrain();
        }
    }

    private void RebuildTerrain()
    {
        BuildTerrain();
    }

    private static float SmoothFalloff(float normalizedDistance)
    {
        float t = Mathf.Clamp(normalizedDistance, 0.0f, 1.0f);
        return 1.0f - t * t * (3.0f - 2.0f * t);
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
}

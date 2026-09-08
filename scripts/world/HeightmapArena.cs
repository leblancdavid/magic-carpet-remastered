using Godot;
using System.Collections.Generic;

namespace MagicCarpetRemastered.Scripts.World;

public partial class HeightmapArena : Node3D
{
    [Export] public int CellsPerSide { get; set; } = 72;
    [Export] public float CellSize { get; set; } = 2.0f;
    [Export] public float HeightScale { get; set; } = 9.0f;
    [Export] public float WaterHeight { get; set; } = -0.8f;

    public override void _Ready()
    {
        AddToGroup("heightmap_arena");
        BuildTerrain();
        BuildWater();
    }

    private void BuildTerrain()
    {
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

        var terrainMesh = new MeshInstance3D
        {
            Name = "TerrainMesh",
            Mesh = mesh
        };
        terrainMesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.24f, 0.34f, 0.18f),
            Roughness = 1.0f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            VertexColorUseAsAlbedo = true
        };
        AddChild(terrainMesh);

        var terrainBody = new StaticBody3D { Name = "TerrainCollision" };
        terrainBody.AddChild(new CollisionShape3D
        {
            Shape = new ConcavePolygonShape3D { Data = collisionTriangles.ToArray() }
        });
        AddChild(terrainBody);
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
        return new Vector3(worldX, HeightAt(worldX, worldZ), worldZ);
    }

    public float HeightAt(float x, float z)
    {
        float halfWidth = CellsPerSide * CellSize * 0.5f;
        float radial = new Vector2(x, z).Length() / halfWidth;
        float islandMask = Mathf.Clamp(1.0f - radial * radial, 0.0f, 1.0f);
        float rolling = Mathf.Sin(x * 0.095f) * 0.45f + Mathf.Cos(z * 0.085f) * 0.4f;
        float ridges = Mathf.Sin((x + z) * 0.045f) * 0.7f + Mathf.Cos((x - z) * 0.07f) * 0.35f;
        return WaterHeight + islandMask * (HeightScale + rolling * 2.5f + ridges * 1.75f);
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

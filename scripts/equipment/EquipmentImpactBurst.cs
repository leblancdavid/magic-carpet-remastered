using Godot;

namespace MagicCarpetRemastered.Scripts.Equipment;

public partial class EquipmentImpactBurst : Node3D
{
    [Export] public Color BurstColor { get; set; } = new(1.0f, 0.58f, 0.12f, 0.85f);
    [Export] public Color EmissionColor { get; set; } = new(1.0f, 0.32f, 0.05f);
    [Export] public float DurationSeconds { get; set; } = 0.28f;
    [Export] public float StartScale { get; set; } = 0.4f;
    [Export] public float EndScale { get; set; } = 2.2f;

    private MeshInstance3D _mesh = null!;
    private StandardMaterial3D _material = null!;
    private float _age;
    internal EquipmentImpactPool? Pool { get; set; }
    private static readonly SphereMesh SharedMesh = new() { Radius = 0.6f, Height = 1.2f, RadialSegments = 16, Rings = 8 };

    public override void _Ready()
    {
        _material = new StandardMaterial3D
        {
            AlbedoColor = BurstColor,
            EmissionEnabled = true,
            Emission = EmissionColor,
            EmissionEnergyMultiplier = 2.2f,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha
        };

        _mesh = new MeshInstance3D
        {
            Mesh = SharedMesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = _material
        };
        AddChild(_mesh);
        if (Pool != null)
        {
            Visible = false;
            SetProcess(false);
        }
    }

    internal void Activate(Vector3 position, Color color, Color emission, float duration, float startScale, float endScale)
    {
        GlobalPosition = position;
        BurstColor = color;
        EmissionColor = emission;
        DurationSeconds = duration;
        StartScale = startScale;
        EndScale = endScale;
        _age = 0;
        Scale = Vector3.One * StartScale;
        _material.AlbedoColor = BurstColor;
        _material.Emission = EmissionColor;
        Visible = true;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        float t = Mathf.Clamp(_age / DurationSeconds, 0.0f, 1.0f);
        Scale = Vector3.One * Mathf.Lerp(StartScale, EndScale, t);
        _material.AlbedoColor = new Color(BurstColor.R, BurstColor.G, BurstColor.B, 1.0f - t);

        if (t >= 1.0f)
        {
            if (Pool != null) Pool.Recycle(this);
            else QueueFree();
        }
    }
}

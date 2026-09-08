using Godot;

namespace MagicCarpetRemastered.Scripts.Spells;

public partial class SpellImpactBurst : Node3D
{
    [Export] public Color BurstColor { get; set; } = new(1.0f, 0.58f, 0.12f, 0.85f);
    [Export] public Color EmissionColor { get; set; } = new(1.0f, 0.32f, 0.05f);
    [Export] public float DurationSeconds { get; set; } = 0.28f;
    [Export] public float StartScale { get; set; } = 0.4f;
    [Export] public float EndScale { get; set; } = 2.2f;

    private MeshInstance3D _mesh = null!;
    private StandardMaterial3D _material = null!;
    private float _age;

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
            Mesh = new SphereMesh { Radius = 0.6f, Height = 1.2f },
            MaterialOverride = _material
        };
        AddChild(_mesh);
    }

    public override void _Process(double delta)
    {
        _age += (float)delta;
        float t = Mathf.Clamp(_age / DurationSeconds, 0.0f, 1.0f);
        Scale = Vector3.One * Mathf.Lerp(StartScale, EndScale, t);
        _material.AlbedoColor = new Color(BurstColor.R, BurstColor.G, BurstColor.B, 1.0f - t);

        if (t >= 1.0f)
        {
            QueueFree();
        }
    }
}

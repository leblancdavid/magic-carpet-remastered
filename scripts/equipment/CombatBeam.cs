using Godot;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Equipment;

// Short original tracer for instantaneous drone weapons; damage remains authoritative.
public partial class CombatBeam : MeshInstance3D
{
    private float _remaining = 0.12f;
    private static readonly BoxMesh BeamMesh = new() { Size = Vector3.One };
    private static readonly StandardMaterial3D Friendly = BeamMaterial(Colors.Cyan);
    private static readonly StandardMaterial3D Hostile = BeamMaterial(Colors.OrangeRed);
    private static StandardMaterial3D BeamMaterial(Color color) => new()
    { AlbedoColor = color, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };

    public static void Emit(Node parent, Vector3 from, Vector3 to, AntimatterOwnership ownership)
    {
        if (from.DistanceSquaredTo(to) < 0.001f) return;
        var beam = new CombatBeam { Mesh = BeamMesh, MaterialOverride = ownership == AntimatterOwnership.Player ? Friendly : Hostile,
            CastShadow = ShadowCastingSetting.Off };
        parent.AddChild(beam);
        beam.GlobalPosition = (from + to) * 0.5f;
        beam.LookAt(to, Mathf.Abs((to - from).Normalized().Dot(Vector3.Up)) > 0.99f ? Vector3.Right : Vector3.Up);
        beam.Scale = new Vector3(0.08f, 0.08f, from.DistanceTo(to));
    }
    public override void _Process(double delta)
    {
        _remaining -= (float)delta;
        if (_remaining <= 0) QueueFree();
    }
}

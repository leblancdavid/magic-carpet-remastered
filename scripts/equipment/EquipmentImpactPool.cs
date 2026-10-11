using Godot;
using System.Collections.Generic;

namespace MagicCarpetRemastered.Scripts.Equipment;

public partial class EquipmentImpactPool : Node3D
{
    private readonly Stack<EquipmentImpactBurst> _idle = new();

    public override void _Ready()
    {
        for (int i = 0; i < 8; i++) _idle.Push(CreateBurst());
    }

    private EquipmentImpactBurst CreateBurst()
    {
        var burst = new EquipmentImpactBurst { Pool = this };
        AddChild(burst);
        return burst;
    }

    public EquipmentImpactBurst Emit(Vector3 position, Color color, Color emission, float duration, float startScale, float endScale)
    {
        var burst = _idle.Count > 0 ? _idle.Pop() : CreateBurst();
        burst.Activate(position, color, emission, duration, startScale, endScale);
        return burst;
    }

    internal void Recycle(EquipmentImpactBurst burst)
    {
        burst.Visible = false;
        burst.SetProcess(false);
        if (_idle.Count < 16) _idle.Push(burst);
        else burst.QueueFree();
    }

    public static EquipmentImpactPool Prepare(Node parent)
    {
        var pool = parent.GetNodeOrNull<EquipmentImpactPool>("EquipmentImpactPool");
        if (pool != null) return pool;
        pool = new EquipmentImpactPool { Name = "EquipmentImpactPool" };
        parent.AddChild(pool);
        return pool;
    }
}

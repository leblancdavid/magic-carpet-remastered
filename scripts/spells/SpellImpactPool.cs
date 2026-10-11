using Godot;
using System.Collections.Generic;

namespace MagicCarpetRemastered.Scripts.Spells;

public partial class SpellImpactPool : Node3D
{
    private readonly Stack<SpellImpactBurst> _idle = new();

    public override void _Ready()
    {
        for (int i = 0; i < 8; i++) _idle.Push(CreateBurst());
    }

    private SpellImpactBurst CreateBurst()
    {
        var burst = new SpellImpactBurst { Pool = this };
        AddChild(burst);
        return burst;
    }

    public SpellImpactBurst Emit(Vector3 position, Color color, Color emission, float duration, float startScale, float endScale)
    {
        var burst = _idle.Count > 0 ? _idle.Pop() : CreateBurst();
        burst.Activate(position, color, emission, duration, startScale, endScale);
        return burst;
    }

    internal void Recycle(SpellImpactBurst burst)
    {
        burst.Visible = false;
        burst.SetProcess(false);
        if (_idle.Count < 16) _idle.Push(burst);
        else burst.QueueFree();
    }

    public static SpellImpactPool Prepare(Node parent)
    {
        var pool = parent.GetNodeOrNull<SpellImpactPool>("SpellImpactPool");
        if (pool != null) return pool;
        pool = new SpellImpactPool { Name = "SpellImpactPool" };
        parent.AddChild(pool);
        return pool;
    }
}

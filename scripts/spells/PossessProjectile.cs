using System;
using Godot;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Spells;

public partial class PossessProjectile : Node3D
{
    [Export] public float Speed { get; set; } = 48.0f;
    [Export] public float LifetimeSeconds { get; set; } = 4.0f;
    [Export] public float MaxRange { get; set; } = 120.0f;
    [Export] public float SurfaceClearance { get; set; } = 0.9f;
    [Export] public float HitRadius { get; set; } = 1.3f;

    public bool IsFollowingTerrain { get; private set; }
    public event Action<int>? ManaPossessed;

    private HeightmapArena? _arena;
    private Vector3 _direction = Vector3.Forward;
    private Vector3 _heading = Vector3.Forward;
    private float _age;
    private float _travelled;

    public override void _Ready()
    {
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.18f, Height = 0.36f },
            Scale = new Vector3(1, 1, 3.5f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.2f, 0.95f, 1),
                EmissionEnabled = true,
                Emission = new Color(0.05f, 0.8f, 1),
                EmissionEnergyMultiplier = 3.0f
            }
        });
    }

    public void Launch(Vector3 direction)
    {
        _direction = direction.Normalized();
        if (_direction.LengthSquared() < 0.001f)
        {
            _direction = Vector3.Forward;
        }
        _heading = new Vector3(_direction.X, 0, _direction.Z).Normalized();
        if (_heading.LengthSquared() < 0.001f)
        {
            _heading = Vector3.Forward;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsQueuedForDeletion())
        {
            return;
        }

        float activeTime = Mathf.Min((float)delta, Mathf.Max(0, LifetimeSeconds - _age));
        float remaining = Mathf.Min(Speed * activeTime, Mathf.Max(0, MaxRange - _travelled));
        _age += (float)delta;
        // Sample the live height grid, not deferred physics meshes. Short sweeps
        // follow hills/craters and cannot skip an orb on a long physics frame.
        while (remaining > 0.0001f)
        {
            float step = Mathf.Min(remaining, 0.5f);
            Vector3 previous = GlobalPosition;
            Vector3 next = previous + (IsFollowingTerrain ? _heading : _direction) * step;
            if (_arena != null)
            {
                float surfaceY = Mathf.Max(_arena.HeightAt(next.X, next.Z), _arena.WaterHeight) + SurfaceClearance;
                if (IsFollowingTerrain || next.Y <= surfaceY)
                {
                    IsFollowingTerrain = true;
                    next.Y = surfaceY;
                }
            }

            ManaPickup? hit = FindFirstHit(previous, next);
            GlobalPosition = next;
            if (hit != null)
            {
                int amount = hit.ManaAmount;
                hit.ReassignOwnership(ManaOwnership.Player);
                var burst = new SpellImpactBurst
                {
                    BurstColor = new Color(0.2f, 0.9f, 1, 0.85f),
                    EmissionColor = new Color(0.05f, 0.7f, 1)
                };
                GetParent().AddChild(burst);
                burst.GlobalPosition = hit.GlobalPosition;
                QueueFree();
                ManaPossessed?.Invoke(amount);
                return;
            }

            Vector3 motion = next - previous;
            if (motion.LengthSquared() > 0.0001f)
            {
                LookAt(next + motion, Mathf.Abs(motion.Normalized().Dot(Vector3.Up)) > 0.99f ? Vector3.Forward : Vector3.Up);
            }
            _travelled += step;
            remaining -= step;
        }

        if (_age >= LifetimeSeconds || _travelled >= MaxRange)
        {
            QueueFree();
        }
    }

    private ManaPickup? FindFirstHit(Vector3 from, Vector3 to)
    {
        ManaPickup? first = null;
        float firstEntry = float.PositiveInfinity;
        Vector3 segment = to - from;
        float lengthSquared = segment.LengthSquared();
        foreach (Node node in GetTree().GetNodesInGroup("loose_mana"))
        {
            if (node is not ManaPickup pickup || !pickup.IsAvailable || pickup.Ownership == ManaOwnership.Player)
            {
                continue;
            }

            // Segment/sphere entry, rather than endpoint distance or group order.
            Vector3 offset = from - pickup.GlobalPosition;
            float c = offset.LengthSquared() - HitRadius * HitRadius;
            float entry = 0;
            if (c > 0)
            {
                if (lengthSquared <= 0.000001f)
                {
                    continue;
                }
                float b = offset.Dot(segment);
                float discriminant = b * b - lengthSquared * c;
                if (discriminant < 0)
                {
                    continue;
                }
                entry = (-b - Mathf.Sqrt(discriminant)) / lengthSquared;
                if (entry < 0 || entry > 1)
                {
                    continue;
                }
            }
            if (entry < firstEntry)
            {
                firstEntry = entry;
                first = pickup;
            }
        }
        return first;
    }
}

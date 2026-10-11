using System;
using System.Collections.Generic;
using Godot;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Equipment;

public partial class ClaimProjectile : Node3D
{
    [Export] public float Speed { get; set; } = 48.0f;
    [Export] public float LifetimeSeconds { get; set; } = 4.0f;
    [Export] public float MaxRange { get; set; } = 120.0f;
    [Export] public float SurfaceClearance { get; set; } = 0.9f;
    [Export] public float HitRadius { get; set; } = 1.3f;

    public bool IsFollowingTerrain { get; private set; }
    public event Action<int>? AntimatterClaimed;
    public event Action<int>? MatterClaimed;

    private HeightmapArena? _arena;
    private Vector3 _direction = Vector3.Forward;
    private Vector3 _heading = Vector3.Forward;
    private float _age;
    private float _travelled;
    private readonly List<AntimatterPickup> _candidates = new();
    private static readonly SphereMesh SharedMesh = new() { Radius = 0.18f, Height = 0.36f, RadialSegments = 12, Rings = 6 };

    public override void _Ready()
    {
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        AddChild(new MeshInstance3D
        {
            Mesh = SharedMesh,
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
        _heading = (_arena?.Tangent(_direction, GlobalPosition) ?? new Vector3(_direction.X, 0, _direction.Z)).Normalized();
        if (_heading.LengthSquared() < 0.001f)
        {
            _heading = _arena?.IsSpherical == true ? PlanetGeometry.Frame(_arena.UpAt(GlobalPosition), Mathf.Abs(_arena.UpAt(GlobalPosition).Z) < 0.9f ? Vector3.Forward : Vector3.Right).Z * -1 : Vector3.Forward;
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
            if (_arena?.IsSpherical == true && IsFollowingTerrain)
            {
                Quaternion rotation = PlanetGeometry.TravelRotation(_arena.UpAt(previous), _heading, previous.DistanceTo(_arena.PlanetCenter), step);
                _heading = rotation * _heading;
                next = _arena.PlanetCenter + rotation * (previous - _arena.PlanetCenter);
            }
            if (_arena != null)
            {
                if (IsFollowingTerrain || _arena.Clearance(next) <= SurfaceClearance)
                {
                    if (!IsFollowingTerrain) _heading = _arena.Tangent(_heading, next).Normalized();
                    IsFollowingTerrain = true;
                    next = _arena.OnSurface(next, SurfaceClearance);
                }
            }

            Node3D? hit = FindFirstHit(previous, next);
            GlobalPosition = next;
            if (hit != null)
            {
                int amount = hit is AntimatterPickup orb ? orb.AntimatterAmount : ((MatterPickup)hit).MatterAmount;
                if (hit is AntimatterPickup energy) energy.ReassignOwnership(AntimatterOwnership.Player);
                else ((MatterPickup)hit).ReassignOwnership(AntimatterOwnership.Player);
                EquipmentImpactPool.Prepare(GetTree().CurrentScene).Emit(hit.GlobalPosition,
                    new Color(0.2f, 0.9f, 1, 0.85f), new Color(0.05f, 0.7f, 1), 0.28f, 0.4f, 2.2f);
                QueueFree();
                if (hit is MatterPickup) MatterClaimed?.Invoke(amount);
                else AntimatterClaimed?.Invoke(amount);
                return;
            }

            Vector3 motion = next - previous;
            if (motion.LengthSquared() > 0.0001f)
            {
                Vector3 up = _arena?.UpAt(next) ?? Vector3.Up;
                LookAt(next + motion, Mathf.Abs(motion.Normalized().Dot(up)) > 0.99f ? _heading : up);
            }
            _travelled += step;
            remaining -= step;
        }

        if (_age >= LifetimeSeconds || _travelled >= MaxRange)
        {
            QueueFree();
        }
    }

    private Node3D? FindFirstHit(Vector3 from, Vector3 to)
    {
        Node3D? first = null;
        float firstEntry = float.PositiveInfinity;
        Vector3 segment = to - from;
        float lengthSquared = segment.LengthSquared();
        AntimatterNeighborhood.Query(GetTree(), (from + to) * 0.5f, HitRadius + segment.Length() * 0.5f, _candidates);
        foreach (AntimatterPickup pickup in _candidates)
        {
            if (!pickup.IsAvailable || pickup.Ownership == AntimatterOwnership.Player)
            {
                continue;
            }

            // Segment/sphere entry, rather than endpoint distance or group order.
            Vector3 offset = from - pickup.GlobalPosition;
            float hitRadius = HitRadius + pickup.Radius;
            float c = offset.LengthSquared() - hitRadius * hitRadius;
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
        foreach (Node node in GetTree().GetNodesInGroup("matter_pickup"))
        {
            if (node is not MatterPickup matter || !matter.IsAvailable || matter.Ownership == AntimatterOwnership.Player) continue;
            Vector3 offset = from - matter.GlobalPosition;
            float radius = HitRadius + 0.6f;
            float c = offset.LengthSquared() - radius * radius;
            float entry = 0;
            if (c > 0)
            {
                if (lengthSquared <= 0.000001f) continue;
                float b = offset.Dot(segment);
                float discriminant = b * b - lengthSquared * c;
                if (discriminant < 0) continue;
                entry = (-b - Mathf.Sqrt(discriminant)) / lengthSquared;
                if (entry < 0 || entry > 1) continue;
            }
            if (entry < firstEntry) { firstEntry = entry; first = matter; }
        }
        return first;
    }
}

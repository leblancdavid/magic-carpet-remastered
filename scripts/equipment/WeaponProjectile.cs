using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Equipment;

public partial class WeaponProjectile : Area3D
{
    [Export] public float Speed { get; set; } = 42.0f;
    [Export] public float LifetimeSeconds { get; set; } = 2.0f;
    [Export] public float TerrainCraterRadius { get; set; } = 4.5f;
    [Export] public float TerrainCraterDepth { get; set; } = 2.2f;
    [Export] public int Damage { get; set; } = 13;

    private Vector3 _direction = Vector3.Forward;
    private ShipFlightController? _owner;
    private AntimatterOwnership _ownership = AntimatterOwnership.Player;
    private float _lifeRemaining;
    private HeightmapArena? _arena;
    private static readonly SphereMesh SharedMesh = new() { Radius = 0.35f, Height = 0.7f, RadialSegments = 12, Rings = 6 };
    private static readonly SphereShape3D SharedShape = new() { Radius = 0.35f };

    public override void _Ready()
    {
        _lifeRemaining = LifetimeSeconds;
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        BodyEntered += OnBodyEntered;

        var shape = new CollisionShape3D
        {
            Shape = SharedShape
        };
        AddChild(shape);

        var mesh = new MeshInstance3D
        {
            Mesh = SharedMesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        mesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(1.0f, 0.45f, 0.08f),
            EmissionEnabled = true,
            Emission = new Color(1.0f, 0.35f, 0.05f),
            EmissionEnergyMultiplier = 2.0f
        };
        AddChild(mesh);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsQueuedForDeletion()) return;
        float deltaF = (float)delta;
        Vector3 previous = GlobalPosition;
        GlobalPosition += _direction * Speed * deltaF;
        _lifeRemaining -= deltaF;
        if (_arena?.IsSpherical == true)
        {
            using var ray = PhysicsRayQueryParameters3D.Create(previous, GlobalPosition);
            if (IsInstanceValid(_owner)) ray.Exclude = new Godot.Collections.Array<Rid> { _owner!.GetRid() };
            using var result = GetWorld3D().DirectSpaceState.IntersectRay(ray);
            if (result.Count > 0 && result["collider"].As<Node3D>() is Node3D body)
            {
                GlobalPosition = (Vector3)result["position"];
                OnBodyEntered(body);
                return;
            }
            if (_arena.TrySurfaceHit(previous, GlobalPosition, out Vector3 hit))
            {
                GlobalPosition = hit;
                if (TerrainCraterRadius > 0 && TerrainCraterDepth > 0) _arena.ApplyCrater(hit, TerrainCraterRadius, TerrainCraterDepth);
                SpawnImpactBurst();
                QueueFree();
                return;
            }
        }

        float stepDistance = GlobalPosition.DistanceTo(previous);
        foreach (Node node in GetTree().GetNodesInGroup("drone"))
        {
            if (node is not CollectorDrone drone || !FactionCombat.CanDamage(drone, _ownership))
            {
                continue;
            }

            if (GlobalPosition.DistanceTo(drone.GlobalPosition) <= stepDistance + 0.6f)
            {
                FactionCombat.Damage(drone, Damage, IsInstanceValid(_owner) ? _owner : null);
                SpawnImpactBurst();
                QueueFree();
                return;
            }
        }

        if (_lifeRemaining <= 0.0f)
        {
            QueueFree();
        }
    }

    public void Launch(Vector3 direction, ShipFlightController owner)
    {
        _direction = direction.Normalized();
        _owner = owner;
        _ownership = owner.Ownership;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (IsQueuedForDeletion())
        {
            return;
        }

        if (IsInstanceValid(_owner) && body == _owner)
        {
            return;
        }

        SpawnImpactBurst();

        if (body is ExplorationWreck wreck) wreck.ApplyDamage(Damage);

        if (FactionCombat.CanDamage(body, _ownership))
            FactionCombat.Damage(body, body is FactionBase && Damage > 0 ? Mathf.Max(1, Mathf.RoundToInt(Damage * 0.2f)) : Damage,
                IsInstanceValid(_owner) ? _owner : null);

        if (TerrainCraterRadius > 0 && TerrainCraterDepth > 0 && body.IsInGroup("deformable_terrain") && GetTree().GetFirstNodeInGroup("heightmap_arena") is HeightmapArena arena)
        {
            arena.ApplyCrater(GlobalPosition, TerrainCraterRadius, TerrainCraterDepth);
        }

        QueueFree();
    }

    private void SpawnImpactBurst()
    {
        EquipmentImpactPool.Prepare(GetTree().CurrentScene).Emit(GlobalPosition,
            new Color(1, 0.58f, 0.12f, 0.85f), new Color(1, 0.32f, 0.05f), 0.28f, 0.4f, 2.2f);
    }
}

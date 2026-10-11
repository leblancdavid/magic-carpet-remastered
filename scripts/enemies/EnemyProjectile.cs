using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Enemies;

public partial class EnemyProjectile : Area3D
{
    [Export] public float Speed { get; set; } = 28.0f;
    [Export] public float LifetimeSeconds { get; set; } = 4.0f;
    [Export] public int Damage { get; set; } = 10;

    private Vector3 _direction = Vector3.Forward;
    private Node3D? _owner;
    private float _lifeRemaining;
    private HeightmapArena? _arena;

    public override void _Ready()
    {
        _lifeRemaining = LifetimeSeconds;
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        BodyEntered += OnBodyEntered;
        Monitoring = true;
        Monitorable = true;

        AddChild(new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 0.45f }
        });

        var mesh = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.45f, Height = 0.9f }
        };
        mesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.55f, 0.2f, 1.0f),
            EmissionEnabled = true,
            Emission = new Color(0.55f, 0.15f, 1.0f),
            EmissionEnergyMultiplier = 3.0f
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
            var ray = PhysicsRayQueryParameters3D.Create(previous, GlobalPosition);
            // A projectile can outlive its shooter. A disposed Godot wrapper
            // still passes a C# type check, but native calls such as GetRid fail.
            if (IsInstanceValid(_owner) && _owner is CollisionObject3D owner)
                ray.Exclude = new Godot.Collections.Array<Rid> { owner.GetRid() };
            var result = GetWorld3D().DirectSpaceState.IntersectRay(ray);
            if (result.Count > 0 && result["collider"].As<Node3D>() is Node3D body)
            {
                GlobalPosition = (Vector3)result["position"];
                OnBodyEntered(body);
                return;
            }
            if (_arena.TrySurfaceHit(previous, GlobalPosition, out Vector3 hit))
            {
                GlobalPosition = hit;
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

    public void Launch(Vector3 direction, Node3D owner)
    {
        _direction = new Vector3(direction.X, 0.0f, direction.Z).Normalized();
        if (_arena?.IsSpherical == true) _direction = direction.Normalized();
        if (_direction == Vector3.Zero)
        {
            _direction = direction.Normalized();
        }
        _owner = owner;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (IsQueuedForDeletion() || (IsInstanceValid(_owner) && body == _owner))
        {
            return;
        }

        SpawnImpactBurst();

        if (body is ShipFlightController player)
        {
            player.ApplyDamage(Damage);
        }

        if (body is FactionBase hub)
        {
            hub.ApplyDamage(Damage);
        }

        QueueFree();
    }

    private void SpawnImpactBurst()
    {
        var burst = new EquipmentImpactBurst
        {
            Name = "EnemyImpactBurst",
            BurstColor = new Color(0.48f, 0.18f, 1.0f, 0.85f),
            EmissionColor = new Color(0.35f, 0.05f, 1.0f)
        };
        GetTree().CurrentScene.AddChild(burst);
        burst.GlobalPosition = GlobalPosition;
    }
}

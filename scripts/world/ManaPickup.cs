using Godot;
using MagicCarpetRemastered.Scripts.Enemies;

namespace MagicCarpetRemastered.Scripts.World;

public enum ManaOwnership
{
    Neutral,
    Player,
    Enemy
}

public partial class ManaPickup : RigidBody3D
{
    private int _manaAmount = 10;
    [Export] public int ManaAmount
    {
        get => _manaAmount;
        set
        {
            _manaAmount = Mathf.Max(0, value);
            UpdateSize();
        }
    }
    [Export] public ManaOwnership Ownership { get; set; } = ManaOwnership.Neutral;
    // Constant mana density: eight times the mana doubles the radius.
    public float Radius => 0.45f * Mathf.Pow(Mathf.Max(1, ManaAmount) / 10.0f, 1.0f / 3.0f);

    public Node3D? Hauler { get; private set; }
    public bool IsCarried => Hauler != null;
    public bool IsAvailable => ManaAmount > 0 && !IsQueuedForDeletion() && !IsCarried;

    private HeightmapArena? _arena;
    private MeshInstance3D _mesh = null!;
    private StandardMaterial3D _material = null!;
    private Vector3 _carryOffset;
    private SphereShape3D _sphere = null!;
    private SphereMesh _sphereMesh = null!;
    private bool _surfaceInitialized;
    private float _mergeGraceRemaining;

    public override void _Ready()
    {
        AddToGroup("loose_mana");
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        if (_arena?.IsSpherical == true) GravityScale = 0;
        ContactMonitor = true;
        MaxContactsReported = 16;
        ContinuousCd = true;
        CanSleep = false; // React to terrain edits even when previously resting.
        LinearDamp = 0.15f;
        AngularDamp = 0.2f;
        PhysicsMaterialOverride = new PhysicsMaterial { Friction = 0.35f, Bounce = 0.05f };
        BodyEntered += body => Callable.From(() => OnBodyEntered(body)).CallDeferred();

        _sphere = new SphereShape3D { Radius = Radius };
        var collision = new CollisionShape3D
        {
            Shape = _sphere
        };
        AddChild(collision);

        _sphereMesh = new SphereMesh { Radius = Radius, Height = Radius * 2 };
        _mesh = new MeshInstance3D
        {
            Mesh = _sphereMesh
        };
        _material = new StandardMaterial3D
        {
            Roughness = 0.3f
        };
        _mesh.MaterialOverride = _material;
        AddChild(_mesh);

        ApplyOwnershipVisual();
        UpdateSize();
        SetPhysicsProcess(true);
    }

    public override void _PhysicsProcess(double delta)
    {
        _mergeGraceRemaining = Mathf.Max(0, _mergeGraceRemaining - (float)delta);
        if (IsQueuedForDeletion() || ManaAmount <= 0)
        {
            return;
        }

        if (IsCarried && Hauler != null)
        {
            GlobalPosition = Hauler.GlobalPosition + (_arena?.IsSpherical == true ? Hauler.GlobalBasis * _carryOffset : _carryOffset);
            return;
        }

        _arena ??= GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        if (!_surfaceInitialized)
        {
            if (_arena != null)
            {
                GlobalPosition = _arena.OnSurface(GlobalPosition, Radius);
            }
            _surfaceInitialized = true;
        }

        // Also check sustained contacts: ownership can change while two orbs
        // are already touching, without a second BodyEntered signal.
        foreach (Node3D body in GetCollidingBodies())
        {
            Callable.From(() => OnBodyEntered(body)).CallDeferred();
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (IsCarried || !IsAvailable || _arena == null) return;
        if (_arena.IsSpherical)
        {
            Vector3 position = state.Transform.Origin;
            Vector3 up = _arena.UpAt(position);
            state.LinearVelocity -= up * 9.8f * state.Step;
            float clearance = _arena.Clearance(position);
            float radial = state.LinearVelocity.Dot(up);
            Transform3D spherical = state.Transform;
            if (clearance > _arena.MaxFlightClearance)
            {
                spherical.Origin = _arena.OnSurface(position, _arena.MaxFlightClearance);
                state.LinearVelocity -= up * Mathf.Max(0, radial);
                state.Transform = spherical;
            }
            bool water = _arena.HeightAt(position) <= _arena.WaterHeight;
            if (clearance < Radius - (water ? 0 : 0.15f))
            {
                spherical.Origin = _arena.OnSurface(position, Radius);
                state.LinearVelocity -= up * Mathf.Min(0, radial);
                state.Transform = spherical;
                if (water) state.LinearVelocity *= Mathf.Max(0, 1 - state.Step * 0.4f);
            }
            return;
        }
        Transform3D transform = state.Transform;
        float terrain = _arena.HeightAt(transform.Origin.X, transform.Origin.Z);
        bool onWater = _arena.WaterHeight >= terrain;
        float floor = Mathf.Max(terrain, _arena.WaterHeight) + Radius;
        float ceiling = _arena.SurfaceHeight(transform.Origin.X, transform.Origin.Z) + _arena.MaxFlightClearance;
        if (transform.Origin.Y > ceiling)
        {
            transform.Origin = new Vector3(transform.Origin.X, ceiling, transform.Origin.Z);
            state.Transform = transform;
            state.LinearVelocity = new Vector3(state.LinearVelocity.X, Mathf.Min(0, state.LinearVelocity.Y), state.LinearVelocity.Z);
        }
        // Leave ordinary terrain contacts to the solver. Snapping away tiny
        // penetrations every tick cancels gravity before it can produce rolling.
        if (transform.Origin.Y < floor - (onWater ? 0 : 0.15f))
        {
            // Water has no collider. The live height grid also protects against
            // terrain being raised before its deferred collision rebuild.
            transform.Origin = new Vector3(transform.Origin.X, floor, transform.Origin.Z);
            state.Transform = transform;
            Vector3 velocity = state.LinearVelocity;
            if (velocity.Y < 0) velocity.Y = 0;
            state.LinearVelocity = velocity;
        }
    }

    private float SurfaceHeight(Vector3 position)
    {
        return Mathf.Max(_arena!.HeightAt(position.X, position.Z), _arena.WaterHeight);
    }

    private void UpdateSize()
    {
        if (_sphere == null) return;
        _sphere.Radius = Radius;
        _sphereMesh.Radius = Radius;
        _sphereMesh.Height = Radius * 2;
        Mass = Mathf.Max(0.1f, ManaAmount / 10.0f);
        Inertia = Vector3.Zero; // Recalculate for the new sphere and mass.
    }

    public int TakeMana(int amount)
    {
        if (amount <= 0 || IsQueuedForDeletion())
        {
            return 0;
        }

        int taken = Mathf.Min(amount, ManaAmount);
        ManaAmount -= taken;
        if (ManaAmount == 0)
        {
            QueueFree();
        }

        return taken;
    }

    public void BeginCarry(Node3D hauler, Vector3 carryOffset)
    {
        if (!IsAvailable) return;
        Hauler = hauler;
        _carryOffset = carryOffset;
        Freeze = true;
        CollisionLayer = 0;
        CollisionMask = 0;
        LinearVelocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;
    }

    public void EndCarry()
    {
        Hauler = null;
        Freeze = false;
        CollisionLayer = 1;
        CollisionMask = 1;
        GravityScale = _arena?.IsSpherical == true ? 0 : 1;
    }

    public void SpillAt(Vector3 position)
    {
        EndCarry();
        GlobalPosition = position;
        LinearVelocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;
        _surfaceInitialized = true; // Spilled cargo falls from its release height.
    }

    public void LaunchScatter(Vector3 position, Vector3 velocity)
    {
        SpillAt(position);
        LinearVelocity = velocity;
        _mergeGraceRemaining = 0.8f;
    }

    public void ReassignOwnership(ManaOwnership ownership)
    {
        Ownership = ownership;
        ApplyOwnershipVisual();
    }

    private void OnBodyEntered(Node body)
    {
        if (!IsInstanceValid(body) || !IsAvailable) return;
        if (body is ManaPickup other && other.IsAvailable && Ownership == other.Ownership
            && _mergeGraceRemaining <= 0 && other._mergeGraceRemaining <= 0)
        {
            // Stable survivor avoids two reciprocal contact callbacks merging
            // the same amounts twice. Deferred callbacks may refer to freed nodes.
            ManaPickup survivor = GetInstanceId() < other.GetInstanceId() ? this : other;
            ManaPickup consumed = survivor == this ? other : this;
            if ((long)survivor.ManaAmount + consumed.ManaAmount > int.MaxValue) return;
            float oldRadius = survivor.Radius;
            Vector3 momentum = survivor.LinearVelocity * survivor.ManaAmount + consumed.LinearVelocity * consumed.ManaAmount;
            int amount = consumed.TakeMana(consumed.ManaAmount);
            survivor.ManaAmount += amount;
            survivor.LinearVelocity = momentum / survivor.ManaAmount;
            survivor.GlobalPosition += (_arena?.UpAt(survivor.GlobalPosition) ?? Vector3.Up) * (survivor.Radius - oldRadius);
            return;
        }
        if (body is SimpleMonster monster && monster.Health > 0 && Ownership == ManaOwnership.Neutral && IsAvailable)
        {
            monster.AbsorbMana(TakeMana(ManaAmount));
        }
    }

    private void ApplyOwnershipVisual()
    {
        if (_material == null)
        {
            return;
        }

        (Color albedo, Color emission) palette = Ownership switch
        {
            ManaOwnership.Neutral => (new Color(0.85f, 0.85f, 0.9f), new Color(0.55f, 0.55f, 0.65f)),
            ManaOwnership.Player => (new Color(0.15f, 0.8f, 1.0f), new Color(0.05f, 0.6f, 1.0f)),
            ManaOwnership.Enemy => (new Color(0.95f, 0.28f, 0.4f), new Color(0.85f, 0.15f, 0.3f)),
            _ => (new Color(0.85f, 0.85f, 0.9f), new Color(0.55f, 0.55f, 0.65f))
        };

        _material.AlbedoColor = palette.albedo;
        _material.EmissionEnabled = true;
        _material.Emission = palette.emission;
        _material.EmissionEnergyMultiplier = 1.5f;
    }
}

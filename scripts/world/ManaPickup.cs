using Godot;
using System.Collections.Generic;
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
    // Deliberately exaggerate resource value rather than using constant density.
    public float TargetRadius => (0.35f / 3) * Mathf.Pow(Mathf.Max(1, ManaAmount), 0.65f);
    public float Radius => _sphere?.Radius ?? TargetRadius;
    public bool IsGrowing { get; private set; }
    public const float MergeGrowthSeconds = 0.6f;
    public const float MergeContactSeconds = 1.25f;
    private const float MergeRelativeSpeed = 0.8f;
    private const float ContactSkin = 0.12f;
    private const float AttractionGap = 6.0f;
    private const float AttractionAcceleration = 3.0f;
    private const float AttractionSpeed = 1.5f;

    public Node3D? Hauler { get; private set; }
    public bool IsCarried => Hauler != null;
    public bool IsDropPending { get; private set; }
    public bool IsAvailable => ManaAmount > 0 && !IsQueuedForDeletion() && !IsCarried && !IsDropPending;

    private HeightmapArena? _arena;
    private MeshInstance3D _mesh = null!;
    private StandardMaterial3D _material = null!;
    private Vector3 _carryOffset;
    private SphereShape3D _sphere = null!;
    private SphereMesh _sphereMesh = null!;
    private bool _surfaceInitialized;
    private float _mergeGraceRemaining;
    private Dictionary<ulong, float> _contactSeconds = new();
    private Vector3 _attractionAcceleration;
    private float _growthStartRadius;
    private float _growthElapsed;
    private float _dropDelayRemaining;

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
        if (IsQueuedForDeletion() || ManaAmount <= 0)
        {
            return;
        }
        if (IsDropPending)
        {
            _dropDelayRemaining -= (float)delta;
            if (_dropDelayRemaining > 0.00001f) return;
            LaunchScatter(ClearDropPosition(), Vector3.Zero);
        }
        _mergeGraceRemaining = Mathf.Max(0, _mergeGraceRemaining - (float)delta);
        UpdateMergeGrowth((float)delta);

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

        _attractionAcceleration = Vector3.Zero;
        bool grounded = IsGrounded();
        var contacts = new Dictionary<ulong, float>();
        if (grounded)
        {
            Vector3 up = _arena?.UpAt(GlobalPosition) ?? Vector3.Up;
            float nearestGap = float.MaxValue;
            Vector3 attractionDirection = Vector3.Zero;
            foreach (Node node in GetTree().GetNodesInGroup("loose_mana"))
            {
                if (node is not ManaPickup other || other == this || !other.IsAvailable
                    || other.Ownership != Ownership) continue;
                Vector3 offset = other.GlobalPosition - GlobalPosition;
                float gap = offset.Length() - Radius - other.Radius;
                if (gap > AttractionGap || !other.IsGrounded()) continue;
                // Support/contact jitter and shared downhill motion must not
                // continually reset the timer. Fast relative impacts still wait.
                if (!IsGrowing && !other.IsGrowing && gap <= ContactSkin && GetInstanceId() < other.GetInstanceId()
                    && LinearVelocity.DistanceTo(other.LinearVelocity) <= MergeRelativeSpeed)
                {
                    ulong id = other.GetInstanceId();
                    float seconds = _contactSeconds.GetValueOrDefault(id) + (float)delta;
                    contacts[id] = seconds;
                    if (seconds >= MergeContactSeconds)
                        Callable.From(() => MergeWith(other)).CallDeferred();
                }
                if (gap < nearestGap)
                {
                    nearestGap = gap;
                    attractionDirection = offset.Slide(up).Normalized();
                }
            }
            // Nearest-neighbor attraction avoids cancellation inside dense fields.
            // Limit only speed toward the neighbor, not unrelated downhill rolling.
            float approachSpeed = LinearVelocity.Dot(attractionDirection);
            if (nearestGap < float.MaxValue && approachSpeed < AttractionSpeed)
                _attractionAcceleration = attractionDirection * Mathf.Min(
                    AttractionAcceleration * (1 - 0.75f * Mathf.Clamp(nearestGap / AttractionGap, 0, 1)),
                    (AttractionSpeed - approachSpeed) / (float)delta);
        }

        // Only the stable survivor tracks each pair. Losing contact, changing
        // ownership, or fast relative motion/falling resets the contact dwell.
        _contactSeconds = contacts;
    }

    private bool IsGrounded()
    {
        if (!IsAvailable || _mergeGraceRemaining > 0) return false;
        Vector3 up = _arena?.UpAt(GlobalPosition) ?? Vector3.Up;
        if (Mathf.Abs(LinearVelocity.Dot(up)) > 1.5f) return false;
        foreach (Node3D body in GetCollidingBodies())
            if (body is StaticBody3D) return true;
        if (_arena != null) return _arena.Clearance(GlobalPosition) <= Radius + 0.35f;
        // Resting solver manifolds can briefly omit the floor while spheres
        // press against each other. Verify support instead of losing the dwell.
        using var ray = PhysicsRayQueryParameters3D.Create(GlobalPosition,
            GlobalPosition - up * (Radius + 0.35f), CollisionMask);
        ray.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        using var hit = GetWorld3D().DirectSpaceState.IntersectRay(ray);
        return hit.Count > 0 && hit["collider"].AsGodotObject() is StaticBody3D;
    }

    private bool CanMergeWith(ManaPickup other)
    {
        return IsInstanceValid(other) && !IsGrowing && !other.IsGrowing && IsGrounded() && other.IsGrounded()
            && Ownership == other.Ownership
            && LinearVelocity.DistanceTo(other.LinearVelocity) <= MergeRelativeSpeed;
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (IsCarried || !IsAvailable) return;
        state.LinearVelocity += _attractionAcceleration * state.Step;
        if (_arena == null) return;
        if (_arena.IsSpherical)
        {
            Vector3 position = state.Transform.Origin;
            Vector3 up = _arena.UpAt(position);
            state.LinearVelocity -= up * 9.8f * state.Step;
            float clearance = _arena.Clearance(position);
            float radial = state.LinearVelocity.Dot(up);
            Transform3D spherical = state.Transform;
            float maximumClearance = Mathf.Max(Radius, _arena.MaxFlightClearance);
            if (clearance > maximumClearance)
            {
                spherical.Origin = _arena.OnSurface(position, maximumClearance);
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
        float ceiling = _arena.SurfaceHeight(transform.Origin.X, transform.Origin.Z) + Mathf.Max(Radius, _arena.MaxFlightClearance);
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
        IsGrowing = false;
        if (_sphere == null) return;
        SetRadius(TargetRadius);
        UpdateMass();
    }

    private void SetRadius(float radius)
    {
        _sphere.Radius = radius;
        _sphereMesh.Radius = radius;
        _sphereMesh.Height = radius * 2;
        Inertia = Vector3.Zero;
    }

    private void UpdateMass()
    {
        Mass = Mathf.Max(0.1f, ManaAmount / 10.0f);
        Inertia = Vector3.Zero; // Recalculate for the new sphere and mass.
    }

    private void UpdateMergeGrowth(float delta)
    {
        if (!IsGrowing) return;
        float oldRadius = Radius;
        _growthElapsed = Mathf.Min(MergeGrowthSeconds, _growthElapsed + delta);
        float t = _growthElapsed / MergeGrowthSeconds;
        float smooth = t * t * (3 - 2 * t);
        SetRadius(Mathf.Lerp(_growthStartRadius, TargetRadius, smooth));
        // Lift gradually with the collider so the growing globe stays grounded.
        if (!IsCarried) GlobalPosition += (_arena?.UpAt(GlobalPosition) ?? Vector3.Up) * (Radius - oldRadius);
        if (_growthElapsed >= MergeGrowthSeconds) IsGrowing = false;
    }

    public int TakeMana(int amount)
    {
        if (amount <= 0 || IsQueuedForDeletion() || IsDropPending)
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
        _contactSeconds.Clear();
        _attractionAcceleration = Vector3.Zero;
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
        IsDropPending = false;
        Visible = true;
        _contactSeconds.Clear();
        _attractionAcceleration = Vector3.Zero;
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

    public void ScheduleDrop(Vector3 position, float delaySeconds)
    {
        LaunchScatter(position, Vector3.Zero);
        if (delaySeconds <= 0) return;
        // The reserved fragment is already counted by WorldManaTracker, but
        // cannot interact with gameplay until its own pause-aware release tick.
        _dropDelayRemaining = delaySeconds;
        IsDropPending = true;
        Visible = false;
        Freeze = true;
        CollisionLayer = 0;
        CollisionMask = 0;
    }

    private Vector3 ClearDropPosition()
    {
        Vector3 position = GlobalPosition;
        Vector3 up = _arena?.UpAt(position) ?? Vector3.Up;
        bool overlaps;
        do
        {
            overlaps = false;
            foreach (Node node in GetTree().GetNodesInGroup("loose_mana"))
            {
                if (node is not ManaPickup other || other == this || !other.IsAvailable) continue;
                Vector3 offset = position - other.GlobalPosition;
                float clearance = Radius + other.Radius + 0.05f;
                if (offset.LengthSquared() >= clearance * clearance) continue;
                // Earlier fragments may roll/grow into a reserved release spot.
                // Lift above them instead of causing another sideways explosion.
                float vertical = offset.Dot(up);
                float lateralSquared = offset.Slide(up).LengthSquared();
                position += up * (Mathf.Sqrt(Mathf.Max(0, clearance * clearance - lateralSquared)) - vertical + 0.01f);
                overlaps = true;
                break;
            }
        } while (overlaps);
        return position;
    }

    public void ReassignOwnership(ManaOwnership ownership)
    {
        _contactSeconds.Clear();
        Ownership = ownership;
        ApplyOwnershipVisual();
    }

    private void OnBodyEntered(Node body)
    {
        if (!IsInstanceValid(body) || !IsAvailable) return;
        if (body is SimpleMonster monster && monster.Health > 0 && Ownership == ManaOwnership.Neutral && IsAvailable)
        {
            monster.AbsorbMana(TakeMana(ManaAmount));
        }
    }

    private void MergeWith(ManaPickup other)
    {
        if (!CanMergeWith(other) || !_contactSeconds.TryGetValue(other.GetInstanceId(), out float seconds)
            || seconds < MergeContactSeconds || GlobalPosition.DistanceTo(other.GlobalPosition) > Radius + other.Radius + ContactSkin
            || (long)ManaAmount + other.ManaAmount > int.MaxValue) return;
        _growthStartRadius = Radius;
        Vector3 momentum = LinearVelocity * ManaAmount + other.LinearVelocity * other.ManaAmount;
        int amount = other.TakeMana(other.ManaAmount);
        // Accounting commits once; only physical/visual growth is animated.
        _manaAmount += amount;
        UpdateMass();
        _growthElapsed = 0;
        IsGrowing = true;
        LinearVelocity = momentum / ManaAmount;
        _contactSeconds.Clear();
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

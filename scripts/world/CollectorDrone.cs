using Godot;
using MagicCarpetRemastered.Scripts.Audio;
using MagicCarpetRemastered.Scripts.Equipment;
using System.Collections.Generic;

namespace MagicCarpetRemastered.Scripts.World;

public partial class CollectorDrone : Node3D
{
    private enum DroneState
    {
        Seeking,
        Returning,
        Docked
    }

    [Export] public bool UsePlayerAntimatter { get; set; } = true;
    [Export] public float MoveSpeed { get; set; } = 10.0f;
    [Export] public float ScoopRadius { get; set; } = 2.2f;
    [Export] public int CarryCapacity { get; set; } = 3;
    [Export] public float HomeRadius { get; set; } = 1.8f;
    [Export] public float MaxSeekRadius { get; set; } = 46.0f;
    [Export] public float DockDelaySeconds { get; set; } = 0.6f;
    [Export] public int MaxHealth { get; set; } = 30;
    [Export] public float HoverHeight { get; set; } = 4.5f;

    public int Health { get; private set; }
    public int CarriedCount => _carried.Count;

    private DroneState _state = DroneState.Seeking;
    private readonly List<AntimatterPickup> _carried = new();
    private FactionBase? _base;
    private StandardMaterial3D _material = null!;
    private float _dockTimer;
    private Vector3 _homeAnchor;
    private bool _homeAnchorSet;
    private HeightmapArena? _arena;

    public override void _Ready()
    {
        AddToGroup("drone");
        AddToGroup(UsePlayerAntimatter ? "player_drone" : "enemy_drone");
        Health = MaxHealth;
        SetPhysicsProcess(true);
        _base = GetTree().GetFirstNodeInGroup(UsePlayerAntimatter ? "player_base" : "enemy_base") as FactionBase;
        BuildVisuals();
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        if (_arena != null) GlobalPosition = _arena.ClampFlightPosition(GlobalPosition);
        _arena?.Orient(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;
        if (!_homeAnchorSet)
        {
            _homeAnchor = GlobalPosition;
            _homeAnchorSet = true;
        }

        if (_base == null)
        {
            _base = GetTree().GetFirstNodeInGroup(UsePlayerAntimatter ? "player_base" : "enemy_base") as FactionBase;
            if (_base == null)
            {
                return;
            }
        }

        if (_base.IsDestroyed)
        {
            ReturnToAnchor(deltaF);
            return;
        }

        switch (_state)
        {
            case DroneState.Seeking:
                HandleSeeking(deltaF);
                break;
            case DroneState.Returning:
                HandleReturning(deltaF);
                break;
            case DroneState.Docked:
                _dockTimer = Mathf.Max(0.0f, _dockTimer - deltaF);
                if (_dockTimer <= 0.0f)
                {
                    _state = DroneState.Seeking;
                }

                break;
        }
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0 || Health <= 0)
        {
            return;
        }

        Health = Mathf.Max(0, Health - amount);
        GameAudio.Instance?.PlayHit();
        UpdateHealthColor();

        if (Health <= 0)
        {
            Pop();
        }
    }

    private void HandleSeeking(float delta)
    {
        AntimatterPickup? best = null;
        float bestDistance = float.MaxValue;
        foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter"))
        {
            if (node is not AntimatterPickup pickup || pickup.Ownership != (UsePlayerAntimatter ? AntimatterOwnership.Player : AntimatterOwnership.Enemy) || !pickup.IsAvailable)
            {
                continue;
            }

            float distance = _arena?.IsSpherical == true ? _arena.SurfaceDistance(GlobalPosition, pickup.GlobalPosition) : GlobalPosition.DistanceTo(pickup.GlobalPosition);
            if (distance > MaxSeekRadius)
            {
                continue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = pickup;
            }
        }

        if (best != null)
        {
            Vector3 scoopPosition = best.GlobalPosition + (_arena?.UpAt(best.GlobalPosition) ?? Vector3.Up) * HoverHeight;
            MoveToward(scoopPosition, delta);
            if (GlobalPosition.DistanceTo(scoopPosition) <= ScoopRadius)
            {
                Scoop(best);
            }

            return;
        }

        if (_carried.Count > 0)
        {
            _state = DroneState.Returning;
        }
        else
        {
            MoveToward(_homeAnchor, delta);
        }
    }

    private void Scoop(AntimatterPickup pickup)
    {
        if (_carried.Count >= CarryCapacity || !pickup.IsAvailable)
        {
            return;
        }

        Vector3 offset = new Vector3((_carried.Count - 1) * 1.0f, -1.7f, 0.0f);
        pickup.BeginCarry(this, offset);
        _carried.Add(pickup);

        if (_carried.Count >= CarryCapacity)
        {
            _state = DroneState.Returning;
        }
    }

    private void HandleReturning(float delta)
    {
        if (_base == null || _carried.Count == 0)
        {
            _state = DroneState.Seeking;
            return;
        }

        Vector3 dockPosition = _base.GlobalPosition + (_arena?.UpAt(_base.GlobalPosition) ?? Vector3.Up) * HoverHeight;
        MoveToward(dockPosition, delta);
        if (GlobalPosition.DistanceTo(dockPosition) <= HomeRadius)
        {
            DepositAll();
            _dockTimer = DockDelaySeconds;
            _state = DroneState.Docked;
        }
    }

    private void DepositAll()
    {
        if (_base == null)
        {
            return;
        }
        foreach (AntimatterPickup pickup in _carried)
        {
            int accepted = _base.DepositAntimatter(pickup.AntimatterAmount);
            pickup.TakeAntimatter(accepted);
            if (pickup.AntimatterAmount > 0)
            {
                // Release overflow outside the solid keep, not inside its collider.
                pickup.SpillAt(_base.GlobalPosition + _base.GlobalBasis * new Vector3(5.0f + pickup.Radius, 2.0f, GD.Randf() * 2.0f - 1.0f));
                pickup.ReassignOwnership(AntimatterOwnership.Neutral);
            }
        }
        _carried.Clear();
    }

    private void Pop()
    {
        int amount = 0;
        foreach (AntimatterPickup pickup in _carried)
        {
            amount += pickup.TakeAntimatter(pickup.AntimatterAmount);
        }

        _carried.Clear();
        AntimatterScatter.Spawn(GetParent(), GlobalPosition, amount, name: "DroneAntimatterSpill");

        var burst = new EquipmentImpactBurst
        {
            Name = "DronePopBurst",
            BurstColor = UsePlayerAntimatter
                ? new Color(0.25f, 0.85f, 1.0f, 0.9f)
                : new Color(0.95f, 0.3f, 0.45f, 0.9f),
            EmissionColor = UsePlayerAntimatter
                ? new Color(0.1f, 0.6f, 0.95f)
                : new Color(0.85f, 0.15f, 0.35f),
            DurationSeconds = 0.5f,
            StartScale = 1.0f,
            EndScale = 4.0f
        };
        GetTree().CurrentScene.AddChild(burst);
        burst.GlobalPosition = GlobalPosition;

        QueueFree();
    }

    private void ReturnToAnchor(float delta)
    {
        MoveToward(_homeAnchor, delta);
    }

    private void MoveToward(Vector3 target, float delta)
    {
        if (_arena?.IsSpherical == true)
        {
            Vector3 heading = _arena.DirectionTo(GlobalPosition, target);
            GlobalPosition = _arena.ClampFlightPosition(_arena.StepToward(GlobalPosition, target, MoveSpeed * delta));
            _arena.Orient(this, heading.LengthSquared() > 0.001f ? heading : null);
            return;
        }
        Vector3 toTarget = target - GlobalPosition;
        float distance = toTarget.Length();
        if (distance <= 0.001f)
        {
            return;
        }

        if (new Vector2(toTarget.X, toTarget.Z).LengthSquared() > 0.0001f)
        {
            LookAt(target, Vector3.Up);
        }
        // Turn from the departure position: a capped step can land exactly on
        // target, which would make LookAt(target) invalid after moving.
        GlobalPosition = GlobalPosition.MoveToward(target, MoveSpeed * delta);
        if (_arena != null) GlobalPosition = _arena.ClampFlightPosition(GlobalPosition);
    }

    private void BuildVisuals()
    {
        var droneMesh = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.9f, Height = 2.0f }
        };
        _material = new StandardMaterial3D
        {
            AlbedoColor = UsePlayerAntimatter ? new Color(0.2f, 0.84f, 1.0f) : new Color(0.95f, 0.3f, 0.45f),
            Roughness = 0.35f,
            EmissionEnabled = true,
            Emission = UsePlayerAntimatter ? new Color(0.1f, 0.55f, 0.95f) : new Color(0.85f, 0.15f, 0.3f),
            EmissionEnergyMultiplier = 0.6f
        };
        droneMesh.MaterialOverride = _material;
        AddChild(droneMesh);

        var basket = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.5f, 0.35f, 0.5f) }
        };
        basket.Position = new Vector3(0.0f, -1.05f, 0.0f);
        basket.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.5f, 0.34f, 0.16f),
            Roughness = 0.9f
        };
        AddChild(basket);
    }

    private void UpdateHealthColor()
    {
        float damageBlend = 1.0f - Mathf.Clamp((float)Health / MaxHealth, 0.0f, 1.0f);
        Color baseColor = UsePlayerAntimatter ? new Color(0.2f, 0.84f, 1.0f) : new Color(0.95f, 0.3f, 0.45f);
        _material.AlbedoColor = baseColor.Lerp(new Color(0.25f, 0.25f, 0.3f), damageBlend * 0.6f);
    }
}

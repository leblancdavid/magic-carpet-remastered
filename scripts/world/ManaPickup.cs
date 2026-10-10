using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;

namespace MagicCarpetRemastered.Scripts.World;

public enum ManaOwnership
{
    Neutral,
    Player,
    Enemy
}

public partial class ManaPickup : Area3D
{
    [Export] public int ManaAmount { get; set; } = 10;
    [Export] public ManaOwnership Ownership { get; set; } = ManaOwnership.Neutral;
    [Export] public float CollectionRadius { get; set; } = 1.4f;
    [Export] public float PlayerHomingRadius { get; set; } = 8.0f;
    [Export] public float HomingSpeed { get; set; } = 13.0f;
    [Export] public float BobHeight { get; set; } = 0.5f;
    [Export] public float BobSpeed { get; set; } = 1.7f;

    public Node3D? Hauler { get; private set; }
    public bool IsCarried => Hauler != null;
    public bool IsAvailable => ManaAmount > 0 && !IsQueuedForDeletion() && !IsCarried;

    private CarpetFlightController? _player;
    private MeshInstance3D _mesh = null!;
    private StandardMaterial3D _material = null!;
    private Vector3 _carryOffset;
    private Vector3 _homePosition;
    private float _bobPhase;
    private bool _homePositionInitialized;

    public override void _Ready()
    {
        AddToGroup("loose_mana");
        BodyEntered += OnBodyEntered;

        var collision = new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 0.8f }
        };
        AddChild(collision);

        _mesh = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.45f, Height = 0.9f }
        };
        _material = new StandardMaterial3D
        {
            Roughness = 0.3f
        };
        _mesh.MaterialOverride = _material;
        AddChild(_mesh);

        ApplyOwnershipVisual();
        SetPhysicsProcess(true);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsQueuedForDeletion() || ManaAmount <= 0)
        {
            return;
        }

        float deltaF = (float)delta;

        if (!_homePositionInitialized)
        {
            _homePosition = GlobalPosition;
            _homePositionInitialized = true;
        }

        if (IsCarried && Hauler != null)
        {
            GlobalPosition = Hauler.GlobalPosition + _carryOffset;
            return;
        }

        _bobPhase += deltaF * BobSpeed;
        float bob = Mathf.Sin(_bobPhase) * BobHeight * 0.5f;
        GlobalPosition = _homePosition + Vector3.Up * bob;

        _player ??= GetTree().GetFirstNodeInGroup("player") as CarpetFlightController;
        if (Ownership != ManaOwnership.Player || _player == null)
        {
            return;
        }

        Vector3 toPlayer = _player.GlobalPosition - GlobalPosition;
        float distance = toPlayer.Length();
        if (distance <= CollectionRadius)
        {
            TakeMana(_player.CollectWorldMana(ManaAmount));

            return;
        }

        if (distance <= PlayerHomingRadius)
        {
            Vector3 nextPosition = GlobalPosition.MoveToward(_player.GlobalPosition, HomingSpeed * deltaF);
            _homePosition += nextPosition - GlobalPosition;
            GlobalPosition = nextPosition;
        }
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
        Hauler = hauler;
        _carryOffset = carryOffset;
    }

    public void EndCarry()
    {
        Hauler = null;
    }

    public void SpillAt(Vector3 position)
    {
        Hauler = null;
        _homePosition = position;
        GlobalPosition = position;
        _homePositionInitialized = true;
    }

    public void ReassignOwnership(ManaOwnership ownership)
    {
        Ownership = ownership;
        ApplyOwnershipVisual();
    }

    private void OnBodyEntered(Node3D body)
    {
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

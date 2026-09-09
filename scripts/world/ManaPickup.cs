using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;

namespace MagicCarpetRemastered.Scripts.World;

public partial class ManaPickup : Area3D
{
    [Export] public int ManaAmount { get; set; } = 10;
    [Export] public float AttractionRadius { get; set; } = 18.0f;
    [Export] public float CollectionRadius { get; set; } = 1.2f;
    [Export] public float HomingSpeed { get; set; } = 14.0f;

    private CarpetFlightController? _player;
    private ManaWell? _well;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;

        var collision = new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 0.8f }
        };
        AddChild(collision);

        var mesh = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.45f, Height = 0.9f }
        };
        mesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.15f, 0.8f, 1.0f),
            EmissionEnabled = true,
            Emission = new Color(0.05f, 0.6f, 1.0f),
            EmissionEnergyMultiplier = 1.5f
        };
        AddChild(mesh);

        SetPhysicsProcess(true);
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;
        RotateY(deltaF * 2.0f);

        _player ??= GetTree().GetFirstNodeInGroup("player") as CarpetFlightController;
        _well ??= GetTree().GetFirstNodeInGroup("mana_well") as ManaWell;

        Node3D? target = SelectTarget();
        if (target == null)
        {
            return;
        }

        Vector3 toTarget = target.GlobalPosition - GlobalPosition;
        float distance = toTarget.Length();
        if (distance <= CollectionRadius)
        {
            TryTransferToTarget(target);
            return;
        }

        Vector3 direction = distance > 0.001f ? toTarget / distance : Vector3.Zero;
        GlobalPosition += direction * HomingSpeed * deltaF;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is CarpetFlightController player)
        {
            int accepted = player.AddMana(ManaAmount);
            ManaAmount -= accepted;
            if (ManaAmount <= 0)
            {
                QueueFree();
            }

            return;
        }

        if (body is SimpleMonster monster)
        {
            monster.AbsorbMana(ManaAmount);
            QueueFree();
        }
    }

    private Node3D? SelectTarget()
    {
        Node3D? target = null;
        float bestDistance = float.MaxValue;

        if (_player != null && (_player.Mana < _player.ManaCapacity || _player.CanClaimMana))
        {
            float distanceToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);
            if (distanceToPlayer <= AttractionRadius)
            {
                target = _player;
                bestDistance = distanceToPlayer;
            }
        }

        if (_well != null && _well.HasSpace)
        {
            float distanceToWell = GlobalPosition.DistanceTo(_well.GlobalPosition);
            if (distanceToWell <= AttractionRadius && distanceToWell < bestDistance)
            {
                target = _well;
                bestDistance = distanceToWell;
            }
        }

        return target;
    }

    private void TryTransferToTarget(Node3D target)
    {
        if (target is CarpetFlightController player)
        {
            int accepted = player.AddMana(ManaAmount);
            ManaAmount -= accepted;
            if (ManaAmount <= 0)
            {
                QueueFree();
                return;
            }

            if (_well != null)
            {
                int deposited = _well.DepositMana(ManaAmount);
                if (deposited > 0)
                {
                    QueueFree();
                }
            }

            return;
        }

        if (target is ManaWell well && well.DepositMana(ManaAmount) > 0)
        {
            QueueFree();
        }
    }
}

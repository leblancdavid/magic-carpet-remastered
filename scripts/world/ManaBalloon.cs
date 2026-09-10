using Godot;
using MagicCarpetRemastered.Scripts.Audio;
using MagicCarpetRemastered.Scripts.Spells;
using System.Collections.Generic;

namespace MagicCarpetRemastered.Scripts.World;

public partial class ManaBalloon : Node3D
{
    private enum BalloonState
    {
        Seeking,
        Returning,
        Docked
    }

    [Export] public bool UsePlayerMana { get; set; } = true;
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

    private BalloonState _state = BalloonState.Seeking;
    private readonly List<ManaPickup> _carried = new();
    private CastleKeep? _castle;
    private StandardMaterial3D _material = null!;
    private float _dockTimer;
    private Vector3 _homeAnchor;
    private bool _homeAnchorSet;

    public override void _Ready()
    {
        AddToGroup("balloon");
        AddToGroup(UsePlayerMana ? "player_balloon" : "enemy_balloon");
        Health = MaxHealth;
        SetPhysicsProcess(true);
        _castle = GetTree().GetFirstNodeInGroup(UsePlayerMana ? "player_castle" : "enemy_castle") as CastleKeep;
        BuildVisuals();
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;
        if (!_homeAnchorSet)
        {
            _homeAnchor = GlobalPosition;
            _homeAnchorSet = true;
        }

        if (_castle == null)
        {
            _castle = GetTree().GetFirstNodeInGroup(UsePlayerMana ? "player_castle" : "enemy_castle") as CastleKeep;
            if (_castle == null)
            {
                return;
            }
        }

        if (_castle.IsDestroyed)
        {
            ReturnToAnchor(deltaF);
            return;
        }

        switch (_state)
        {
            case BalloonState.Seeking:
                HandleSeeking(deltaF);
                break;
            case BalloonState.Returning:
                HandleReturning(deltaF);
                break;
            case BalloonState.Docked:
                _dockTimer = Mathf.Max(0.0f, _dockTimer - deltaF);
                if (_dockTimer <= 0.0f)
                {
                    _state = BalloonState.Seeking;
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
        Vector3 center = _homeAnchor;
        ManaPickup? best = null;
        float bestDistance = float.MaxValue;
        foreach (Node node in GetTree().GetNodesInGroup("loose_mana"))
        {
            if (node is not ManaPickup pickup || pickup.Ownership != (UsePlayerMana ? ManaOwnership.Player : ManaOwnership.Enemy) || pickup.IsCarried)
            {
                continue;
            }

            float distance = GlobalPosition.DistanceTo(pickup.GlobalPosition);
            if (distance > MaxSeekRadius * MaxSeekRadius)
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
            MoveToward(best.GlobalPosition, delta);
            if (bestDistance <= ScoopRadius)
            {
                Scoop(best);
            }

            return;
        }

        _state = BalloonState.Returning;
    }

    private void Scoop(ManaPickup pickup)
    {
        if (_carried.Count >= CarryCapacity)
        {
            return;
        }

        Vector3 offset = new Vector3(0.0f, 0.0f, 0.3f * _carried.Count + 0.3f);
        pickup.BeginCarry(this, offset);
        _carried.Add(pickup);

        if (_carried.Count >= CarryCapacity)
        {
            _state = BalloonState.Returning;
        }
    }

    private void HandleReturning(float delta)
    {
        if (_castle == null || _carried.Count == 0)
        {
            _state = BalloonState.Seeking;
            return;
        }

        MoveToward(_castle.GlobalPosition, delta);
        if (GlobalPosition.DistanceTo(_castle.GlobalPosition) <= HomeRadius)
        {
            DepositAll();
            _dockTimer = DockDelaySeconds;
            _state = BalloonState.Docked;
        }
    }

    private void DepositAll()
    {
        if (_castle == null)
        {
            return;
        }
        foreach (ManaPickup pickup in _carried)
        {
            int amount = pickup.ManaAmount;
            int accepted = _castle.DepositMana(amount);
            if (accepted >= amount)
            {
                _carried.Remove(pickup);
                pickup.QueueFree();
            }
            else
            {
                pickup.SpillAt(_castle.GlobalPosition + new Vector3(GD.Randf() * 2.0f - 1.0f, 2.0f, GD.Randf() * 2.0f - 1.0f));
                pickup.ReassignOwnership(ManaOwnership.Neutral);
                _carried.Remove(pickup);
            }
        }
    }

    private void Pop()
    {
        foreach (ManaPickup pickup in _carried)
        {
            Vector3 offset = new Vector3(GD.Randf() * 2.4f - 1.2f, 1.0f, GD.Randf() * 2.4f - 1.2f);
            pickup.SpillAt(GlobalPosition + offset);
            pickup.ReassignOwnership(ManaOwnership.Neutral);
        }

        _carried.Clear();

        var burst = new SpellImpactBurst
        {
            Name = "BalloonPopBurst",
            BurstColor = UsePlayerMana
                ? new Color(0.25f, 0.85f, 1.0f, 0.9f)
                : new Color(0.95f, 0.3f, 0.45f, 0.9f),
            EmissionColor = UsePlayerMana
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
        Vector3 targetAtHover = target + Vector3.Up * HoverHeight;
        Vector3 toTarget = targetAtHover - GlobalPosition;
        float distance = toTarget.Length();
        if (distance <= 0.001f)
        {
            return;
        }

        GlobalPosition += (toTarget / distance) * MoveSpeed * delta;
        LookAt(target, Vector3.Up);
    }

    private void BuildVisuals()
    {
        var balloonMesh = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.9f, Height = 2.0f }
        };
        _material = new StandardMaterial3D
        {
            AlbedoColor = UsePlayerMana ? new Color(0.2f, 0.84f, 1.0f) : new Color(0.95f, 0.3f, 0.45f),
            Roughness = 0.35f,
            EmissionEnabled = true,
            Emission = UsePlayerMana ? new Color(0.1f, 0.55f, 0.95f) : new Color(0.85f, 0.15f, 0.3f),
            EmissionEnergyMultiplier = 0.6f
        };
        balloonMesh.MaterialOverride = _material;
        AddChild(balloonMesh);

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
        Color baseColor = UsePlayerMana ? new Color(0.2f, 0.84f, 1.0f) : new Color(0.95f, 0.3f, 0.45f);
        _material.AlbedoColor = baseColor.Lerp(new Color(0.25f, 0.25f, 0.3f), damageBlend * 0.6f);
    }
}
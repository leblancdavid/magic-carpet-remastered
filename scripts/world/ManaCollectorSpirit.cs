using Godot;

namespace MagicCarpetRemastered.Scripts.World;

public partial class ManaCollectorSpirit : Node3D
{
    [Export] public float MoveSpeed { get; set; } = 12.0f;
    [Export] public float CollectionRadius { get; set; } = 1.2f;
    [Export] public float HomeRadius { get; set; } = 0.9f;
    [Export] public int CarryAmount { get; set; } = 12;
    [Export] public float ReturnToSourceDelaySeconds { get; set; } = 0.65f;
    [Export] public bool DeliverToPlayerCastle { get; set; } = true;

    private ManaWell? _source;
    private CastleKeep? _castle;
    private Vector3 _hoverOffset;
    private bool _carrying;
    private float _returnTimer;

    public override void _Ready()
    {
        AddToGroup("mana_collector");
        _source = GetTree().GetFirstNodeInGroup("mana_well") as ManaWell;
        _castle = GetTree().GetFirstNodeInGroup(DeliverToPlayerCastle ? "player_castle" : "enemy_castle") as CastleKeep;
        _hoverOffset = new Vector3(0.0f, 4.0f, 0.0f);
        BuildVisuals();
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;
        _returnTimer = Mathf.Max(0.0f, _returnTimer - deltaF);

        if (_source == null || _castle == null || _castle.IsDestroyed)
        {
            return;
        }

        Vector3 sourceTarget = _source.GlobalPosition + _hoverOffset;
        Vector3 castleTarget = _castle.GlobalPosition + _hoverOffset;

        if (!_carrying)
        {
            MoveToward(sourceTarget, deltaF);
            if (GlobalPosition.DistanceTo(sourceTarget) <= CollectionRadius && _returnTimer <= 0.0f)
            {
                int withdrawn = _source.WithdrawMana(CarryAmount);
                if (withdrawn > 0)
                {
                    _carrying = true;
                    _returnTimer = ReturnToSourceDelaySeconds;
                    Pulse(new Color(0.15f, 0.8f, 1.0f));
                }
            }

            return;
        }

        MoveToward(castleTarget, deltaF);
        if (GlobalPosition.DistanceTo(castleTarget) <= HomeRadius && _returnTimer <= 0.0f)
        {
            int deposited = _castle.DepositMana(CarryAmount);
            if (deposited > 0)
            {
                _carrying = false;
                _returnTimer = ReturnToSourceDelaySeconds;
                Pulse(new Color(0.92f, 0.82f, 0.24f));
            }
        }
    }

    private void MoveToward(Vector3 target, float delta)
    {
        Vector3 toTarget = target - GlobalPosition;
        float distance = toTarget.Length();
        if (distance <= 0.001f)
        {
            return;
        }

        Vector3 direction = toTarget / distance;
        GlobalPosition += direction * MoveSpeed * delta;
        LookAt(target, Vector3.Up);
    }

    private void BuildVisuals()
    {
        var mesh = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.45f, Height = 0.9f }
        };
        mesh.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.2f, 0.84f, 1.0f),
            EmissionEnabled = true,
            Emission = new Color(0.12f, 0.54f, 1.0f),
            EmissionEnergyMultiplier = 1.8f,
            Roughness = 0.2f
        };
        AddChild(mesh);
    }

    private void Pulse(Color color)
    {
        if (GetChildCount() > 0 && GetChild(0) is MeshInstance3D mesh && mesh.MaterialOverride is StandardMaterial3D material)
        {
            material.AlbedoColor = color;
        }
    }
}

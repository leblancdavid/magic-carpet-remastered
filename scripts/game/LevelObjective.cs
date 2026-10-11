using System;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Game;

public partial class LevelObjective : Node
{
    public enum LevelState { Playing, Victory, Defeat }
    [Export] public float PilotRespawnDelaySeconds { get; set; } = 10.0f;

    public LevelState State { get; private set; }
    public bool RivalPilotDefeated { get; private set; }
    public bool RivalBaseDestroyed => IsInstanceValid(_rivalBase) && _rivalBase!.IsDestroyed;
    public string ResultMessage { get; private set; } = string.Empty;
    public float PlayerRespawnRemaining { get; private set; }
    public float RivalRespawnRemaining { get; private set; }
    public event Action? Finished;

    private ShipFlightController? _player;
    private FactionBase? _playerBase;
    private FactionBase? _rivalBase;
    private EnemyEcosystemEnemy? _rivalPilot;
    private bool _playerWaiting;

    public override void _Ready() => AddToGroup("level_objective");

    public void Initialize(ShipFlightController player, FactionBase playerBase,
        FactionBase rivalBase, EnemyEcosystemEnemy rivalPilot)
    {
        _player = player;
        _playerBase = playerBase;
        _rivalBase = rivalBase;
        _rivalPilot = rivalPilot;
        rivalPilot.Defeated += OnRivalDefeated;
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_rivalPilot))
        {
            _rivalPilot!.Defeated -= OnRivalDefeated;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Evaluate();
        if (State != LevelState.Playing) return;
        if (_playerWaiting)
        {
            PlayerRespawnRemaining = Mathf.Max(0, PlayerRespawnRemaining - (float)delta);
            if (PlayerRespawnRemaining <= 0)
            {
                _player!.RespawnAt(RespawnPosition(_playerBase!));
                _playerWaiting = false;
            }
        }
        if (RivalPilotDefeated && !RivalBaseDestroyed)
        {
            RivalRespawnRemaining = Mathf.Max(0, RivalRespawnRemaining - (float)delta);
            if (RivalRespawnRemaining <= 0)
            {
                if (IsInstanceValid(_rivalPilot))
                {
                    _rivalPilot!.Defeated -= OnRivalDefeated;
                    _rivalPilot.Name = "DefeatedRival";
                }
                var rival = new EnemyEcosystemEnemy
                {
                    Name = "RivalPilot",
                    Role = EnemyEcosystemEnemy.EnemyRole.RivalShip,
                    Position = GetParent<Node3D>().ToLocal(RespawnPosition(_rivalBase!))
                };
                GetParent().AddChild(rival);
                _rivalPilot = rival;
                rival.Defeated += OnRivalDefeated;
                RivalPilotDefeated = false;
            }
        }
        Evaluate();
    }

    public void Evaluate()
    {
        if (State != LevelState.Playing || !IsInstanceValid(_player)
            || !IsInstanceValid(_playerBase) || !IsInstanceValid(_rivalBase))
        {
            return;
        }

        // Resolve simultaneous lethal outcomes consistently in the player's disfavor.
        if (_playerBase!.IsDestroyed)
        {
            State = LevelState.Defeat;
            ResultMessage = "Your base was destroyed.";
        }
        else if (RivalBaseDestroyed && RivalPilotDefeated)
        {
            State = LevelState.Victory;
            ResultMessage = "The rival base is destroyed and its pilot defeated.";
        }
        else
        {
            if (_player!.Health <= 0 && !_playerWaiting)
            {
                _playerWaiting = true;
                PlayerRespawnRemaining = PilotRespawnDelaySeconds;
            }
            return;
        }

        Finished?.Invoke();
    }

    private void OnRivalDefeated()
    {
        RivalPilotDefeated = true;
        RivalRespawnRemaining = PilotRespawnDelaySeconds;
    }

    private Vector3 RespawnPosition(FactionBase hub)
    {
        // Spawn beside the solid keep, never within its collider.
        Vector3 position = hub.GlobalPosition + hub.GlobalBasis * new Vector3(9, 4, 0);
        var arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        if (arena != null)
        {
            position = arena.OnSurface(position, 6);
            position = arena.ClampFlightPosition(position, 2);
        }
        return position;
    }
}

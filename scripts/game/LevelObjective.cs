using System;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Game;

public partial class LevelObjective : Node
{
    public enum LevelState { Playing, Victory, Defeat }
    [Export] public float WizardRespawnDelaySeconds { get; set; } = 10.0f;

    public LevelState State { get; private set; }
    public bool RivalWizardDefeated { get; private set; }
    public bool RivalCastleDestroyed => IsInstanceValid(_rivalCastle) && _rivalCastle!.IsDestroyed;
    public string ResultMessage { get; private set; } = string.Empty;
    public float PlayerRespawnRemaining { get; private set; }
    public float RivalRespawnRemaining { get; private set; }
    public event Action? Finished;

    private CarpetFlightController? _player;
    private CastleKeep? _playerCastle;
    private CastleKeep? _rivalCastle;
    private EnemyEcosystemEnemy? _rivalWizard;
    private bool _playerWaiting;

    public override void _Ready() => AddToGroup("level_objective");

    public void Initialize(CarpetFlightController player, CastleKeep playerCastle,
        CastleKeep rivalCastle, EnemyEcosystemEnemy rivalWizard)
    {
        _player = player;
        _playerCastle = playerCastle;
        _rivalCastle = rivalCastle;
        _rivalWizard = rivalWizard;
        rivalWizard.Defeated += OnRivalDefeated;
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_rivalWizard))
        {
            _rivalWizard!.Defeated -= OnRivalDefeated;
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
                _player!.RespawnAt(RespawnPosition(_playerCastle!));
                _playerWaiting = false;
            }
        }
        if (RivalWizardDefeated && !RivalCastleDestroyed)
        {
            RivalRespawnRemaining = Mathf.Max(0, RivalRespawnRemaining - (float)delta);
            if (RivalRespawnRemaining <= 0)
            {
                if (IsInstanceValid(_rivalWizard))
                {
                    _rivalWizard!.Defeated -= OnRivalDefeated;
                    _rivalWizard.Name = "DefeatedRival";
                }
                var rival = new EnemyEcosystemEnemy
                {
                    Name = "RivalWizard",
                    Role = EnemyEcosystemEnemy.EnemyRole.EnemyWizard,
                    Position = GetParent<Node3D>().ToLocal(RespawnPosition(_rivalCastle!))
                };
                GetParent().AddChild(rival);
                _rivalWizard = rival;
                rival.Defeated += OnRivalDefeated;
                RivalWizardDefeated = false;
            }
        }
        Evaluate();
    }

    public void Evaluate()
    {
        if (State != LevelState.Playing || !IsInstanceValid(_player)
            || !IsInstanceValid(_playerCastle) || !IsInstanceValid(_rivalCastle))
        {
            return;
        }

        // Resolve simultaneous lethal outcomes consistently in the player's disfavor.
        if (_playerCastle!.IsDestroyed)
        {
            State = LevelState.Defeat;
            ResultMessage = "Your castle was destroyed.";
        }
        else if (RivalCastleDestroyed && RivalWizardDefeated)
        {
            State = LevelState.Victory;
            ResultMessage = "The rival castle is destroyed and its wizard defeated.";
        }
        else
        {
            if (_player!.Health <= 0 && !_playerWaiting)
            {
                _playerWaiting = true;
                PlayerRespawnRemaining = WizardRespawnDelaySeconds;
            }
            return;
        }

        Finished?.Invoke();
    }

    private void OnRivalDefeated()
    {
        RivalWizardDefeated = true;
        RivalRespawnRemaining = WizardRespawnDelaySeconds;
    }

    private Vector3 RespawnPosition(CastleKeep castle)
    {
        // Spawn beside the solid keep, never within its collider.
        Vector3 position = castle.GlobalPosition + new Vector3(9, 4, 0);
        var arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        if (arena != null)
        {
            position.Y = arena.SurfaceHeight(position.X, position.Z) + 6;
            position = arena.ClampFlightPosition(position, 2);
        }
        return position;
    }
}

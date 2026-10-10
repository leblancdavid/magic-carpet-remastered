using System;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Game;

public partial class LevelObjective : Node
{
    public enum LevelState { Playing, Victory, Defeat }

    public LevelState State { get; private set; }
    public bool RivalWizardDefeated { get; private set; }
    public bool RivalCastleDestroyed => IsInstanceValid(_rivalCastle) && _rivalCastle!.IsDestroyed;
    public string ResultMessage { get; private set; } = string.Empty;
    public event Action? Finished;

    private CarpetFlightController? _player;
    private CastleKeep? _playerCastle;
    private CastleKeep? _rivalCastle;
    private EnemyEcosystemEnemy? _rivalWizard;

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

    public override void _PhysicsProcess(double delta) => Evaluate();

    public void Evaluate()
    {
        if (State != LevelState.Playing || !IsInstanceValid(_player)
            || !IsInstanceValid(_playerCastle) || !IsInstanceValid(_rivalCastle))
        {
            return;
        }

        // Resolve simultaneous lethal outcomes consistently in the player's disfavor.
        if (_player!.Health <= 0 || _playerCastle!.IsDestroyed)
        {
            State = LevelState.Defeat;
            ResultMessage = _player.Health <= 0 ? "Your wizard was defeated." : "Your castle was destroyed.";
        }
        else if (RivalCastleDestroyed && RivalWizardDefeated)
        {
            State = LevelState.Victory;
            ResultMessage = "The rival castle is destroyed and its wizard defeated.";
        }
        else
        {
            return;
        }

        Finished?.Invoke();
    }

    private void OnRivalDefeated() => RivalWizardDefeated = true;
}

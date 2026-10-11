using Godot;
using MagicCarpetRemastered.Scripts.Enemies;

namespace MagicCarpetRemastered.Scripts.World;

public partial class WorldManaTracker : Node
{
    [Export] public float UpdateIntervalSeconds { get; set; } = 0.25f;
    [Export] public float EquilibriumFraction { get; set; } = 0.5f;

    public int NeutralLooseMana { get; private set; }
    public int NeutralCreatureMana { get; private set; }
    public int PlayerControlledMana { get; private set; }
    public int EnemyControlledMana { get; private set; }
    public int TotalMana => NeutralLooseMana + NeutralCreatureMana + PlayerControlledMana + EnemyControlledMana;
    public bool EquilibriumMet => TotalMana > 0 && PlayerControlledMana >= TotalMana * EquilibriumFraction;

    public float PlayerShare01 => TotalMana <= 0
        ? 0.0f
        : (float)PlayerControlledMana / TotalMana;

    private float _timer;

    public override void _Ready()
    {
        AddToGroup("world_mana_tracker");
        SetPhysicsProcess(true);
        Refresh();
    }

    public override void _PhysicsProcess(double delta)
    {
        _timer += (float)delta;
        if (_timer < UpdateIntervalSeconds)
        {
            return;
        }

        _timer = 0.0f;
        Refresh();
    }

    public void Refresh()
    {
        NeutralLooseMana = 0;
        NeutralCreatureMana = 0;
        PlayerControlledMana = 0;
        EnemyControlledMana = 0;

        foreach (Node node in GetTree().GetNodesInGroup("loose_mana"))
        {
            // Hidden, pending drop fragments reserve real mana too. Count
            // them immediately so staggered release never alters world totals.
            if (node is not ManaPickup pickup || pickup.IsQueuedForDeletion())
            {
                continue;
            }

            switch (pickup.Ownership)
            {
                case ManaOwnership.Neutral:
                    NeutralLooseMana += pickup.ManaAmount;
                    break;
                case ManaOwnership.Player:
                    PlayerControlledMana += pickup.ManaAmount;
                    break;
                case ManaOwnership.Enemy:
                    EnemyControlledMana += pickup.ManaAmount;
                    break;
            }
        }

        foreach (Node node in GetTree().GetNodesInGroup("player_castle"))
        {
            if (node is CastleKeep castle)
            {
                PlayerControlledMana += castle.StoredMana;
            }
        }

        foreach (Node node in GetTree().GetNodesInGroup("enemy_castle"))
        {
            if (node is CastleKeep castle)
            {
                EnemyControlledMana += castle.StoredMana;
            }
        }

        foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
        {
            if (node is EnemyEcosystemEnemy enemy)
            {
                if (enemy.Role is EnemyEcosystemEnemy.EnemyRole.ManaThief or EnemyEcosystemEnemy.EnemyRole.EnemyWizard)
                    EnemyControlledMana += enemy.ManaReserve;
                else
                    NeutralCreatureMana += enemy.ManaReserve;
            }
        }

        foreach (Node node in GetTree().GetNodesInGroup("simple_monster"))
        {
            if (node is SimpleMonster monster)
            {
                NeutralCreatureMana += monster.ManaReserve;
            }
        }
    }
}

using Godot;
using MagicCarpetRemastered.Scripts.Enemies;

namespace MagicCarpetRemastered.Scripts.World;

public partial class WorldAntimatterTracker : Node
{
    [Export] public float UpdateIntervalSeconds { get; set; } = 0.25f;
    [Export] public float EquilibriumFraction { get; set; } = 0.5f;

    public int NeutralLooseAntimatter { get; private set; }
    public int NeutralCreatureAntimatter { get; private set; }
    public int PlayerControlledAntimatter { get; private set; }
    public int EnemyControlledAntimatter { get; private set; }
    public long SpentAntimatter { get; private set; }
    public long SiteGeneratedAntimatter { get; private set; }
    public long RefundedAntimatter { get; private set; }
    public long InvestmentSpilledAntimatter { get; private set; }
    public long ActiveConstructionInvestment { get; private set; }
    public long DestroyedInvestmentLoss { get; private set; }
    public long NetCommittedAntimatter => SpentAntimatter - RefundedAntimatter - InvestmentSpilledAntimatter;
    public int TotalAntimatter => NeutralLooseAntimatter + NeutralCreatureAntimatter + PlayerControlledAntimatter + EnemyControlledAntimatter;
    public bool EquilibriumMet => TotalAntimatter > 0 && PlayerControlledAntimatter >= TotalAntimatter * EquilibriumFraction;

    public float PlayerShare01 => TotalAntimatter <= 0
        ? 0.0f
        : (float)PlayerControlledAntimatter / TotalAntimatter;

    private float _timer;

    public override void _Ready()
    {
        AddToGroup("world_antimatter_tracker");
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
        NeutralLooseAntimatter = 0;
        NeutralCreatureAntimatter = 0;
        PlayerControlledAntimatter = 0;
        EnemyControlledAntimatter = 0;
        SpentAntimatter = 0;
        SiteGeneratedAntimatter = 0;
        foreach (Node node in GetTree().GetNodesInGroup("strategic_point"))
            if (node is StrategicPoint point && !point.IsQueuedForDeletion()) SiteGeneratedAntimatter += point.GeneratedAntimatter;
        RefundedAntimatter = InvestmentSpilledAntimatter = ActiveConstructionInvestment = DestroyedInvestmentLoss = 0;

        foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter"))
        {
            // Hidden, pending drop fragments reserve real antimatter too. Count
            // them immediately so staggered release never alters world totals.
            if (node is not AntimatterPickup pickup || pickup.IsQueuedForDeletion())
            {
                continue;
            }

            switch (pickup.Ownership)
            {
                case AntimatterOwnership.Neutral:
                    NeutralLooseAntimatter += pickup.AntimatterAmount;
                    break;
                case AntimatterOwnership.Player:
                    PlayerControlledAntimatter += pickup.AntimatterAmount;
                    break;
                case AntimatterOwnership.Enemy:
                    EnemyControlledAntimatter += pickup.AntimatterAmount;
                    break;
            }
        }

        foreach (Node node in GetTree().GetNodesInGroup("drone"))
        {
            if (node is not CollectorDrone drone || drone.IsQueuedForDeletion()) continue;
            if (drone.UsePlayerAntimatter) PlayerControlledAntimatter += drone.CarriedAntimatter;
            else EnemyControlledAntimatter += drone.CarriedAntimatter;
        }

        foreach (Node node in GetTree().GetNodesInGroup("player_base"))
        {
            if (node is FactionBase hub)
            {
                PlayerControlledAntimatter += hub.StoredAntimatter;
                SpentAntimatter += hub.SpentAntimatter;
                AddConstructionAccounting(hub);
            }
        }

        foreach (Node node in GetTree().GetNodesInGroup("enemy_base"))
        {
            if (node is FactionBase hub)
            {
                EnemyControlledAntimatter += hub.StoredAntimatter;
                SpentAntimatter += hub.SpentAntimatter;
                AddConstructionAccounting(hub);
            }
        }

        foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
        {
            if (node is EnemyEcosystemEnemy enemy)
            {
                if (enemy.Role == EnemyEcosystemEnemy.EnemyRole.RivalShip)
                    EnemyControlledAntimatter += enemy.AntimatterReserve;
                else
                    NeutralCreatureAntimatter += enemy.AntimatterReserve;
            }
        }

        foreach (Node node in GetTree().GetNodesInGroup("simple_monster"))
        {
            if (node is SimpleMonster monster)
            {
                NeutralCreatureAntimatter += monster.AntimatterReserve;
            }
        }
    }

    private void AddConstructionAccounting(FactionBase hub)
    {
        RefundedAntimatter += hub.RefundedAntimatter;
        InvestmentSpilledAntimatter += hub.InvestmentSpilledAntimatter;
        ActiveConstructionInvestment += hub.Construction.ActiveInvestment;
        DestroyedInvestmentLoss += hub.Construction.DestroyedInvestmentLoss;
    }
}

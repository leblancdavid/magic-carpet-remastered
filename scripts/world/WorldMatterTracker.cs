using Godot;

namespace MagicCarpetRemastered.Scripts.World;

public partial class WorldMatterTracker : Node
{
    public long PhysicalMatter { get; private set; }
    public long SpentMatter { get; private set; }
    public long RefundedMatter { get; private set; }
    public long InvestmentSpilledMatter { get; private set; }
    public long ActiveInvestment { get; private set; }
    public long DestroyedInvestmentLoss { get; private set; }
    public long SiteGeneratedMatter { get; private set; }
    public long NetCommittedMatter => SpentMatter - RefundedMatter - InvestmentSpilledMatter;
    private double _timer;
    public override void _Ready() { AddToGroup("world_matter_tracker"); Refresh(); }
    public override void _PhysicsProcess(double delta)
    {
        _timer += delta;
        if (_timer < 0.25) return;
        _timer = 0; Refresh();
    }
    public void Refresh()
    {
        PhysicalMatter = SpentMatter = RefundedMatter = InvestmentSpilledMatter = ActiveInvestment = DestroyedInvestmentLoss = SiteGeneratedMatter = 0;
        foreach (Node node in GetTree().GetNodesInGroup("matter_pickup"))
            if (node is MatterPickup pickup && !pickup.IsQueuedForDeletion()) PhysicalMatter += pickup.MatterAmount;
        foreach (string group in new[] { "player_base", "enemy_base" })
            foreach (Node node in GetTree().GetNodesInGroup(group))
                if (node is FactionBase hub && !hub.IsQueuedForDeletion())
                {
                    PhysicalMatter += hub.StoredMatter;
                    SpentMatter += hub.SpentMatter; RefundedMatter += hub.RefundedMatter;
                    InvestmentSpilledMatter += hub.InvestmentSpilledMatter;
                    ActiveInvestment += hub.Construction.ActiveMatterInvestment;
                    DestroyedInvestmentLoss += hub.Construction.DestroyedMatterLoss;
                }
        foreach (Node node in GetTree().GetNodesInGroup("strategic_point"))
            if (node is StrategicPoint point && !point.IsQueuedForDeletion()) SiteGeneratedMatter += point.GeneratedMatter;
    }
}

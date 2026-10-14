using System;
using System.Collections.Generic;

namespace MagicCarpetRemastered.Scripts.World;

// Owned by FactionBase and advanced by its physics tick. No automatic queues.
public sealed class BaseConstruction
{
    private readonly FactionBase _base;
    private readonly ConstructionProject?[] _slots = new ConstructionProject?[5];
    private readonly Dictionary<string, ConstructionDefinition> _catalog = new(StringComparer.Ordinal);
    private readonly HashSet<string> _completed = new(StringComparer.Ordinal);
    private long _nextInstanceId;

    public string LastStatus { get; private set; } = "Construction idle";
    public long ActiveInvestment
    {
        get
        {
            long total = 0;
            foreach (var project in _slots) total += project?.PaidAntimatter ?? 0;
            return total;
        }
    }
    public long CompletedInvestment { get; private set; }
    public long DestroyedInvestmentLoss { get; private set; }
    public int DestroyedProjectCount { get; private set; }

    internal BaseConstruction(FactionBase owner) => _base = owner;
    public ConstructionProject? GetActive(ConstructionSlot slot) => ValidSlot(slot) ? _slots[(int)slot] : null;
    public bool IsCompleted(string id) => _completed.Contains(id);
    public IEnumerable<ConstructionDefinition> Catalog => _catalog.Values;
    public int UpgradeRank(string id, bool active = false)
    {
        var item = UpgradeCatalog.Find(id);
        if (item == null || !item.Playable) return 0;
        if (active && (_base.IsDestroyed || (item.Slot != ConstructionSlot.Ship && _base.Tier < item.Tier))) return 0;
        int rank = id == "plasma-bolt" ? 1 : 0;
        for (int level = 1; level <= 3; level++) if (_completed.Contains(UpgradeCatalog.ProjectId(id, level))) rank = level;
        return rank;
    }

    public bool Register(ConstructionDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Id) || string.IsNullOrWhiteSpace(definition.Name)
            || !ValidSlot(definition.Slot) || definition.Slot == ConstructionSlot.Base
            || definition.Cost <= 0
            || !double.IsFinite(definition.DurationSeconds) || definition.DurationSeconds <= 0
            || definition.RequiredTier < 1 || definition.RequiredTier > FactionBase.MaximumTier) return false;
        return _catalog.TryAdd(definition.Id, definition);
    }

    public ConstructionDefinition? NextTierDefinition()
    {
        int target = _base.Tier + 1;
        if (_base.IsDestroyed || target > FactionBase.MaximumTier) return null;
        bool rebuild = target <= _base.HighestPurchasedTier;
        int cost = _base.UpgradeCostForTier(target);
        if (rebuild) cost = cost / 2 + cost % 2; // Round rebuild payment up.
        return new ConstructionDefinition($"base-tier-{target}", $"{(rebuild ? "Rebuild" : "Upgrade")} tier {target}",
            ConstructionSlot.Base, cost, _base.UpgradeSecondsForTier(target), _base.Tier);
    }

    public bool TryStartNextTier(out string reason)
    {
        var definition = NextTierDefinition();
        if (definition == null) { reason = "No next tier available"; return false; }
        return Start(definition, _base.Tier + 1, _base.Tier + 1 <= _base.HighestPurchasedTier, out reason);
    }

    public bool TryStart(string id, out string reason)
    {
        if (BaseOrders.IsOrder(id)) { reason = "Select a target using Launch / Deploy"; return false; }
        reason = GetStartReason(id);
        if (reason.Length > 0) return false;
        return Start(_catalog[id], 0, false, out reason);
    }

    public bool TryStartOrder(string id, Godot.Node3D? target, out string reason)
    {
        if (!BaseOrders.IsOrder(id)) { reason = "Not a deployment order"; return false; }
        reason = GetStartReason(id);
        if (reason.Length == 0) reason = BaseOrders.TargetReason(_base, id, target);
        if (reason.Length > 0) return false;
        return Start(_catalog[id], 0, false, out reason, target);
    }

    public string GetStartReason(string id)
    {
        if (!_catalog.TryGetValue(id, out var definition)) return "Unknown project";
        if (!definition.Repeatable && _completed.Contains(id)) return "Completed";
        return ValidateStart(definition);
    }

    public string GetNextTierStartReason() => NextTierDefinition() is { } definition
        ? ValidateStart(definition) : "Maximum tier / base destroyed";

    public bool IsEffectActive(string id) => _completed.Contains(id)
        && _catalog.TryGetValue(id, out var definition) && !_base.IsDestroyed
        && (definition.Slot == ConstructionSlot.Ship || _base.Tier >= definition.RequiredTier);

    private string ValidateStart(ConstructionDefinition definition)
    {
        if (_base.GetTree().Paused) return "Gameplay paused";
        if (_base.IsDestroyed) return "Base destroyed";
        if (_slots[(int)definition.Slot] != null) return "Slot occupied";
        if (_base.Tier < definition.RequiredTier) return $"Requires tier {definition.RequiredTier}";
        if (definition.RequiredProject != null && !_completed.Contains(definition.RequiredProject)) return $"Requires {definition.RequiredProject}";
        if (definition.Cost <= 0
            || !double.IsFinite(definition.DurationSeconds) || definition.DurationSeconds <= 0) return "Invalid tuning";
        if (_base.StoredAntimatter < definition.Cost) return $"Need {definition.Cost - _base.StoredAntimatter} more antimatter";
        return string.Empty;
    }

    private bool Start(ConstructionDefinition definition, int targetTier, bool rebuild, out string reason, Godot.Node3D? orderedTarget = null)
    {
        reason = ValidateStart(definition);
        if (reason.Length > 0) return false;
        if (!_base.TrySpendAntimatter(definition.Cost)) { reason = "Insufficient stored antimatter"; return false; }
        _slots[(int)definition.Slot] = new ConstructionProject(++_nextInstanceId, definition, targetTier, rebuild, orderedTarget);
        LastStatus = $"Started {definition.Name}";
        reason = string.Empty;
        return true;
    }

    // Instance identity prevents delayed UI cancellation from refunding a replacement.
    public bool Cancel(ConstructionSlot slot, long instanceId)
    {
        var project = GetActive(slot);
        if (project == null || project.InstanceId != instanceId || project.State != ConstructionState.Active) return false;
        _slots[(int)slot] = null;
        project.Finish(ConstructionState.Cancelled);
        _base.RefundConstruction(project.PaidAntimatter);
        LastStatus = $"Cancelled {project.Definition.Name}: full refund";
        return true;
    }

    internal void Advance(double delta)
    {
        if (_base.IsDestroyed || _base.GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        double projectDelta = delta * StrategicPoint.ProjectSpeed(_base);
        for (int i = 0; i < _slots.Length; i++)
        {
            var project = _slots[i];
            if (project == null) continue;
            project.Advance(projectDelta);
            if (project.Progress < 1) continue;
            if (BaseOrders.IsOrder(project.Definition.Id)
                && BaseOrders.TargetReason(_base, project.Definition.Id, project.OrderedTarget, false).Length > 0)
            { Cancel((ConstructionSlot)i, project.InstanceId); continue; }
            // Tier changes from damage destroy projects synchronously before this tick.
            if (project.TargetTier > 0 && !_base.TryCompleteTierUpgrade(project.TargetTier)) continue;
            _slots[i] = null;
            project.Finish(ConstructionState.Completed);
            CompletedInvestment += project.PaidAntimatter;
            if (project.TargetTier == 0 && !project.Definition.Repeatable) _completed.Add(project.Definition.Id);
            if (project.TargetTier == 0 && BaseOrders.IsOrder(project.Definition.Id)) BaseOrders.Complete(_base, project);
            LastStatus = $"Completed {project.Definition.Name}";
        }
    }

    internal void DestroyAll()
    {
        int count = 0;
        for (int i = 0; i < _slots.Length; i++)
        {
            var project = _slots[i];
            if (project == null) continue;
            _slots[i] = null;
            project.Finish(ConstructionState.Destroyed);
            int spill = project.PaidAntimatter / 2; // Odd unit is permanently lost.
            DestroyedInvestmentLoss += project.PaidAntimatter - spill;
            _base.SpillConstructionInvestment(spill);
            count++;
        }
        DestroyedProjectCount += count;
        if (count > 0) LastStatus = $"Siege destroyed {count} construction project(s)";
    }

    private static bool ValidSlot(ConstructionSlot slot) => (int)slot >= 0 && (int)slot < 5;
}

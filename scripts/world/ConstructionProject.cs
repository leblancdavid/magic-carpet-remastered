using System;

namespace MagicCarpetRemastered.Scripts.World;

public enum ConstructionSlot { Base, Defense, Resources, Offense, Ship }
public enum ConstructionState { Active, Completed, Cancelled, Destroyed }

// Immutable catalog data; content-specific benefits are integrated by later milestones.
public sealed record ConstructionDefinition(string Id, string Name, ConstructionSlot Slot,
    int Cost, double DurationSeconds, int RequiredTier = 1, string? RequiredProject = null,
    string Description = "Research project", bool Repeatable = false,
    string? UpgradeId = null, int UpgradeRank = 0);

public sealed class ConstructionProject
{
    public long InstanceId { get; }
    public ConstructionDefinition Definition { get; }
    public int PaidAntimatter { get; }
    public int TargetTier { get; }
    public bool IsRebuild { get; }
    public Godot.Node3D? OrderedTarget { get; }
    public double ElapsedSeconds { get; private set; }
    public double Progress => Math.Clamp(ElapsedSeconds / Definition.DurationSeconds, 0, 1);
    public ConstructionState State { get; private set; } = ConstructionState.Active;

    internal ConstructionProject(long instanceId, ConstructionDefinition definition, int targetTier = 0, bool rebuild = false,
        Godot.Node3D? orderedTarget = null)
    {
        InstanceId = instanceId;
        Definition = definition;
        PaidAntimatter = definition.Cost;
        TargetTier = targetTier;
        IsRebuild = rebuild;
        OrderedTarget = orderedTarget;
    }

    internal void Advance(double delta) => ElapsedSeconds = Math.Min(Definition.DurationSeconds, ElapsedSeconds + delta);
    internal void Finish(ConstructionState state) => State = state;
}

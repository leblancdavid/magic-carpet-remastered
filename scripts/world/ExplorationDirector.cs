using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MagicCarpetRemastered.Scripts.World;

public partial class ExplorationDirector : Node
{
    [Export] public int InitialCount { get; set; } = 12;
    [Export] public bool UseFixedSeed { get; set; }
    [Export] public int GenerationSeed { get; set; }
    [Export] public int MaximumActive { get; set; } = 20;
    [Export] public float BaseExclusionDistance { get; set; } = 180;
    [Export] public float MinimumSpacing { get; set; } = 100;
    [Export] public float ArrivalIntervalSeconds { get; set; } = 60;
    [Export] public int PlacementAttempts { get; set; } = 64;
    [Export] public int WreckHealth { get; set; } = 50;
    [Export] public int RewardSkipAntimatter { get; set; } = 30;
    [Export] public float ArrivalDescentSeconds { get; set; } = 3;
    public long SpawnedCount { get; private set; }
    private HeightmapArena _arena = null!;
    private readonly RandomNumberGenerator _random = new();
    private readonly Dictionary<ulong, int> _regionalCounts = new();
    private float _arrivalTimer;
    public override void _Ready()
    {
        AddToGroup("exploration_director");
        _arena = (HeightmapArena)GetTree().GetFirstNodeInGroup("heightmap_arena");
        if (UseFixedSeed) _random.Seed = unchecked((ulong)GenerationSeed); else _random.Randomize();
        if (!_arena.IsSpherical) { BaseExclusionDistance = 30; MinimumSpacing = 18; InitialCount = 5; MaximumActive = 8; }
        else
        {
            if (BaseExclusionDistance == 180) BaseExclusionDistance = Mathf.Clamp(_arena.PlanetRadius * 0.25f, 30, 80);
            if (MinimumSpacing == 100) MinimumSpacing = Mathf.Clamp(_arena.PlanetRadius * 0.2f, 20, 60);
        }
        for (int i = 0; i < InitialCount; i++) if (!TrySpawn(false)) break;
        _arrivalTimer = ArrivalIntervalSeconds;
    }
    public override void _PhysicsProcess(double delta)
    {
        _arrivalTimer -= (float)delta;
        if (_arrivalTimer > 0) return;
        bool spawned = TrySpawn(true);
        _arrivalTimer = spawned ? Mathf.Max(1, ArrivalIntervalSeconds) : 5;
    }
    private FactionBase[] Bases => GetTree().GetNodesInGroup("player_base").Concat(GetTree().GetNodesInGroup("enemy_base"))
        .OfType<FactionBase>().Where(b => !b.IsQueuedForDeletion()).ToArray();
    public bool IsValidPosition(Vector3 position)
    {
        if (!position.IsFinite() || !float.IsFinite(_arena.HeightAt(position))) return false;
        // Reject sharp ledges; water is a supported floating landing, not a submerged reward.
        Basis frame = PlanetGeometry.Frame(_arena.UpAt(position), Vector3.Forward);
        float height = _arena.HeightAt(position);
        foreach (Vector3 offset in new[] { frame.X * 2, -frame.X * 2, frame.Z * 2, -frame.Z * 2 })
            if (Mathf.Abs(_arena.HeightAt(position + offset) - height) > 4) return false;
        if (Bases.Any(b => _arena.SurfaceDistance(position, b.GlobalPosition) < BaseExclusionDistance)) return false;
        return !GetTree().GetNodesInGroup("exploration_wreck").OfType<ExplorationWreck>().Any(w => !w.IsCollected
            && !w.IsQueuedForDeletion() && _arena.SurfaceDistance(position, w.GlobalPosition) < MinimumSpacing);
    }
    public bool TrySpawn(bool orbital)
    {
        if (GetTree().GetNodesInGroup("exploration_wreck").OfType<ExplorationWreck>().Count(w => !w.IsCollected && !w.IsQueuedForDeletion()) >= MaximumActive) return false;
        Vector3? best = null; ulong region = 0; int bestCount = int.MaxValue;
        int bestPlanetCount = int.MaxValue;
        var planetaryCounts = new int[PlanetLayout.RegionCount];
        if (_arena.IsSpherical)
            foreach (Node node in GetTree().GetNodesInGroup("exploration_wreck"))
                if (node is ExplorationWreck existing && !existing.IsCollected && !existing.IsQueuedForDeletion())
                    planetaryCounts[PlanetLayout.RegionFor(_arena.UpAt(existing.GlobalPosition))]++;
        var bases = Bases;
        for (int i = 0; i < PlacementAttempts; i++)
        {
            Vector3 position;
            if (_arena.IsSpherical)
            {
                float y = _random.RandfRange(-1, 1), angle = _random.RandfRange(0, Mathf.Tau);
                float radius = Mathf.Sqrt(Mathf.Max(0, 1 - y * y));
                position = _arena.PlanetCenter + new Vector3(radius * Mathf.Cos(angle), y, radius * Mathf.Sin(angle)) * _arena.PlanetRadius;
            }
            else
            {
                float half = _arena.CellsPerSide * _arena.CellSize * 0.45f;
                position = new Vector3(_random.RandfRange(-half, half), 0, _random.RandfRange(-half, half));
            }
            position = _arena.OnSurface(position, 1);
            if (!IsValidPosition(position)) continue;
            ulong nearest = bases.OrderBy(b => _arena.SurfaceDistance(position, b.GlobalPosition)).FirstOrDefault()?.GetInstanceId() ?? 0;
            int count = _regionalCounts.GetValueOrDefault(nearest);
            int planetCount = _arena.IsSpherical ? planetaryCounts[PlanetLayout.RegionFor(_arena.UpAt(position))] : 0;
            // Fill sparsely occupied globe regions first; nearest-base cumulative
            // fairness breaks ties. Arrivals refill explored regions as well.
            if (planetCount > bestPlanetCount || (planetCount == bestPlanetCount && count >= bestCount)) continue;
            best = position; region = nearest; bestCount = count; bestPlanetCount = planetCount;
        }
        if (!best.HasValue) return false;
        var wreck = new ExplorationWreck { Name = "ExplorationWreck", Position = GetParent<Node3D>().ToLocal(best.Value),
            InitialHealth = WreckHealth, SkipAntimatter = Mathf.Max(0, RewardSkipAntimatter),
            ArrivalRemaining = orbital ? Mathf.Max(0, ArrivalDescentSeconds) : 0 };
        GetParent().AddChild(wreck);
        _regionalCounts[region] = bestCount + 1; SpawnedCount++;
        return true;
    }
    public override void _ExitTree() => _random.Dispose();
}

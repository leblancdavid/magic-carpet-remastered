using Godot;
using System.Collections.Generic;

namespace MagicCarpetRemastered.Scripts.World;

// Match-only fleet ownership keeps independent diagnostic/station drones intact.
public partial class HomeCollectorFleet : Node
{
    public FactionBase Home { get; set; } = null!;
    private readonly List<CollectorDrone> _drones = new();
    private float _replacementDelay;
    private HeightmapArena? _arena;

    public override void _Ready()
    {
        _arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
        AddCollector();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GetTree().Paused || !IsInstanceValid(Home) || Home.IsDestroyed) return;
        int lost = _drones.RemoveAll(drone => !IsInstanceValid(drone) || drone.IsQueuedForDeletion() || drone.Health <= 0);
        if (lost > 0) _replacementDelay = 15;
        for (int i = 0; i < _drones.Count; i++) _drones[i].Retiring = i >= Home.Tier;
        _replacementDelay = Mathf.Max(0, _replacementDelay - (float)delta);
        if (_drones.Count < Home.Tier && _replacementDelay <= 0)
        { AddCollector(); _replacementDelay = 1; }
    }

    private void AddCollector()
    {
        Vector3 start = Home.GlobalPosition + Home.GlobalBasis.X * (-10 + _drones.Count * 3);
        if (_arena != null) start = _arena.OnSurface(start, 5);
        var drone = new CollectorDrone
        {
            Name = _drones.Count == 0 ? Home.IsPlayerBase ? "PlayerDrone" : "EnemyDrone" : "HomeCollector",
            UsePlayerAntimatter = Home.IsPlayerBase,
            Position = GetParent<Node3D>().ToLocal(start),
            MaxSeekRadius = _arena?.IsSpherical == true ? Mathf.Pi * _arena.PlanetRadius : 46
        };
        GetParent().AddChild(drone); _drones.Add(drone);
    }
}

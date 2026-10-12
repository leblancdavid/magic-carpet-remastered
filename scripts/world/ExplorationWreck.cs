using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Enemies;

namespace MagicCarpetRemastered.Scripts.World;

public partial class ExplorationWreck : StaticBody3D
{
    [Export] public int InitialHealth { get; set; } = 50;
    [Export] public float CollectionRadius { get; set; } = 4;
    [Export] public int SkipAntimatter { get; set; } = 30;
    public int Health { get; private set; }
    public bool IsRevealed => Health == 0;
    public bool IsCollected { get; private set; }
    public bool IsCharted { get; set; }
    public float ArrivalRemaining { get; set; }
    private HeightmapArena _arena = null!;
    private MeshInstance3D _visual = null!;
    private Label3D _label = null!;
    private CollisionShape3D _collision = null!;
    private readonly RandomNumberGenerator _random = new();

    public override void _Ready()
    {
        AddToGroup("exploration_wreck");
        _arena = (HeightmapArena)GetTree().GetFirstNodeInGroup("heightmap_arena");
        _random.Randomize(); Health = Mathf.Max(1, InitialHealth);
        _visual = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(4, 2, 5) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.38f, 0.44f, 0.5f), Roughness = 0.85f } };
        AddChild(_visual);
        _collision = new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(4, 2, 5) }, Disabled = ArrivalRemaining > 0 };
        AddChild(_collision);
        _label = new Label3D { Text = $"WRECK · {Health} HP\nFire to reveal salvage", Position = Vector3.Up * 3,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, FontSize = 28, PixelSize = 0.012f,
            VisibilityRangeEnd = 55 };
        AddChild(_label);
        FollowSurface();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsCollected) return;
        float before = ArrivalRemaining;
        ArrivalRemaining = Mathf.Max(0, ArrivalRemaining - (float)delta);
        if (before > 0 && ArrivalRemaining == 0) _collision.SetDeferred(CollisionShape3D.PropertyName.Disabled, false);
        FollowSurface();
        if (!IsRevealed || ArrivalRemaining > 0) return;
        _visual.RotateY((float)delta);
        foreach (Node node in GetTree().GetNodesInGroup("pilots"))
            if (node is ShipFlightController ship && TryCollect(ship)) return;
        foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
            if (node is EnemyEcosystemEnemy { Role: EnemyEcosystemEnemy.EnemyRole.RivalShip, Health: > 0 } rival
                && InRange(rival))
            {
                // The current autonomous rival always takes the legal skip outcome.
                if (GetTree().GetFirstNodeInGroup("enemy_base") is not FactionBase home || home.IsDestroyed) continue;
                IsCollected = true; ExplorationBoons.AwardAntimatter(home, SkipAntimatter); Hide(); QueueFree(); return;
            }
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0 || IsRevealed || IsCollected || ArrivalRemaining > 0) return;
        Health = Mathf.Max(0, Health - amount);
        _label.Text = $"WRECK · {Health} HP\nFire to reveal salvage";
        if (!IsRevealed) return;
        _collision.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
        _visual.Mesh = new PrismMesh { Size = new Vector3(1.8f, 2.2f, 1.8f) };
        _visual.MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(1, 0.78f, 0.18f),
            EmissionEnabled = true, Emission = new Color(1, 0.6f, 0.1f) };
        _label.Text = "SALVAGE\nFly close to secure choices";
        FollowSurface();
    }

    public bool TryCollect(ShipFlightController ship)
    {
        if (!IsRevealed || IsCollected || IsQueuedForDeletion() || ship.Health <= 0 || !InRange(ship)
            || GetTree().Paused || ship.HomeBase is not { IsDestroyed: false }) return false;
        IsCollected = true;
        ship.Boons.SecureOffer(_random, SkipAntimatter);
        Hide(); QueueFree(); return true;
    }

    private bool InRange(Node3D actor) => _arena.SurfaceDistance(actor.GlobalPosition, GlobalPosition) <= CollectionRadius
        && Mathf.Abs(_arena.Elevation(actor.GlobalPosition) - _arena.Elevation(GlobalPosition)) <= 6;
    private void FollowSurface()
    {
        GlobalPosition = _arena.OnSurface(GlobalPosition, (IsRevealed ? 6 : 1) + ArrivalRemaining * 20);
        _arena.Orient(this);
    }
    public override void _ExitTree() => _random.Dispose();
}

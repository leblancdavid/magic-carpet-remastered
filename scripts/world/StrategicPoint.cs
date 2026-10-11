using System;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;

namespace MagicCarpetRemastered.Scripts.World;

public enum StrategicPointRole { Extractor, Research, Logistics, MatterExtractor }

public partial class StrategicPoint : Node3D
{
    [Export] public StrategicPointRole Role { get; set; }
    [Export] public float CaptureRadius { get; set; } = 18;
    [Export] public double CaptureSeconds { get; set; } = 10;
    [Export] public double IncomePerSecond { get; set; } = 2;
    [Export] public double ResearchBonus { get; set; } = 0.25;
    public AntimatterOwnership Ownership { get; private set; }
    public AntimatterOwnership Claimant { get; private set; }
    public double CaptureProgress { get; private set; }
    public bool IsContested { get; private set; }
    public long GeneratedAntimatter { get; private set; }
    public long GeneratedMatter { get; private set; }
    public int CaptureCount { get; private set; }
    public CollectorDrone? Drone { get; private set; }
    public string BenefitText => Role switch
    {
        StrategicPointRole.Extractor => $"+{IncomePerSecond:0.#} antimatter/s (room permitting)",
        StrategicPointRole.MatterExtractor => $"+{IncomePerSecond:0.#} matter/s (room permitting)",
        StrategicPointRole.Research => $"+{ResearchBonus * 100:0}% project speed",
        _ => Drone == null ? "Collector activates on first capture" : GodotObject.IsInstanceValid(Drone) && !Drone.IsQueuedForDeletion()
            ? $"Station drone · {Drone.CarriedCount} cargo objects ({Drone.CarriedMatterAmount} matter)" : "Station drone destroyed"
    };
    public string StatusText => $"{Role} · {Ownership} · {BenefitText}\n"
        + (IsContested ? $"CONTESTED · {Claimant} {CaptureProgress:P0}" : Claimant != AntimatterOwnership.Neutral
            ? $"{Claimant} capturing {CaptureProgress:P0}" : "Remain in ring to capture");
    private HeightmapArena _arena = null!;
    private Label3D _label = null!;
    private StandardMaterial3D _material = null!;
    private double _incomeFraction;
    private bool _droneActivated;

    public override void _Ready()
    {
        AddToGroup("strategic_point");
        _arena = (HeightmapArena)GetTree().GetFirstNodeInGroup("heightmap_arena");
        GlobalPosition = _arena.OnSurface(GlobalPosition);
        _arena.Orient(this);
        _material = new StandardMaterial3D { AlbedoColor = Colors.Gray, EmissionEnabled = true, EmissionEnergyMultiplier = 0.5f };
        AddChild(new MeshInstance3D { Position = Vector3.Up * 2, Mesh = new CylinderMesh
            { TopRadius = 1.5f, BottomRadius = 3, Height = 4 }, MaterialOverride = _material });
        AddChild(new MeshInstance3D { Position = Vector3.Up * 0.2f, Mesh = new TorusMesh
            { InnerRadius = CaptureRadius - 0.3f, OuterRadius = CaptureRadius }, MaterialOverride = _material });
        _label = new Label3D { Position = Vector3.Up * 7, FontSize = 40, PixelSize = 0.015f,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, NoDepthTest = false };
        AddChild(_label);
        RefreshVisuals();
    }

    public override void _PhysicsProcess(double delta)
    {
        bool player = false, enemy = false;
        foreach (Node node in GetTree().GetNodesInGroup("player"))
            if (node is ShipFlightController ship && ship.Health > 0 && InRange(ship)) player = true;
        foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
            if (node is EnemyEcosystemEnemy pilot && pilot.Role == EnemyEcosystemEnemy.EnemyRole.RivalShip
                && pilot.Health > 0 && InRange(pilot)) enemy = true;
        Advance(delta, player, enemy);
        _label.Visible = GetTree().GetFirstNodeInGroup("player") is ShipFlightController observer
            && _arena.SurfaceDistance(GlobalPosition, observer.GlobalPosition) <= 65;
        // Follow authoritative terrain, including live deformation.
        GlobalPosition = _arena.OnSurface(GlobalPosition);
    }

    private bool InRange(Node3D actor) => !actor.IsQueuedForDeletion()
        && _arena.SurfaceDistance(GlobalPosition, actor.GlobalPosition) <= CaptureRadius
        && Mathf.Abs(_arena.Clearance(actor.GlobalPosition)) <= _arena.MaxFlightClearance + 2;

    internal void Advance(double delta, bool playerPresent, bool enemyPresent)
    {
        if (GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        // Pay elapsed time to the existing owner; the new owner starts next tick.
        AdvanceIncome(delta);
        playerPresent &= BaseFor(AntimatterOwnership.Player) is { IsDestroyed: false };
        enemyPresent &= BaseFor(AntimatterOwnership.Enemy) is { IsDestroyed: false };
        IsContested = playerPresent && enemyPresent;
        if (!IsContested)
        {
            var present = playerPresent ? AntimatterOwnership.Player : enemyPresent ? AntimatterOwnership.Enemy : AntimatterOwnership.Neutral;
            if (present == AntimatterOwnership.Neutral || present == Ownership)
            {
                Claimant = AntimatterOwnership.Neutral; CaptureProgress = 0;
            }
            else
            {
                if (Claimant != present) { Claimant = present; CaptureProgress = 0; }
                CaptureProgress = Math.Min(1, CaptureProgress + delta / Math.Max(0.01, CaptureSeconds));
                if (CaptureProgress >= 1) CompleteCapture(present);
            }
        }
        RefreshVisuals();
    }

    private void AdvanceIncome(double delta)
    {
        if (Role is not (StrategicPointRole.Extractor or StrategicPointRole.MatterExtractor)
            || BaseFor(Ownership) is not { IsDestroyed: false } hub) { _incomeFraction = 0; return; }
        bool matter = Role == StrategicPointRole.MatterExtractor;
        int available = matter ? hub.AvailableMatterStorage : hub.AvailableStorage;
        if (available == 0) { _incomeFraction = 0; return; }
        double accrued = _incomeFraction + delta * Math.Max(0, IncomePerSecond);
        int request = (int)Math.Min(available, Math.Floor(accrued));
        if (matter) GeneratedMatter += hub.DepositMatter(request);
        else GeneratedAntimatter += hub.DepositAntimatter(request);
        _incomeFraction = (matter ? hub.AvailableMatterStorage : hub.AvailableStorage) == 0 ? 0 : accrued - Math.Floor(accrued);
    }

    private void CompleteCapture(AntimatterOwnership owner)
    {
        Ownership = owner; CaptureCount++; Claimant = AntimatterOwnership.Neutral; CaptureProgress = 0; _incomeFraction = 0;
        if (Role != StrategicPointRole.Logistics) return;
        if (!_droneActivated)
        {
            _droneActivated = true;
            Drone = new CollectorDrone { Name = "StationDrone", UsePlayerAntimatter = owner == AntimatterOwnership.Player,
                Position = GlobalPosition + _arena.UpAt(GlobalPosition) * 5,
                MaxSeekRadius = _arena.IsSpherical ? Mathf.Pi * _arena.PlanetRadius : 100 };
            GetParent().AddChild(Drone);
        }
        if (GodotObject.IsInstanceValid(Drone) && !Drone!.IsQueuedForDeletion() && BaseFor(owner) is { } hub)
            Drone.TransferTo(hub);
    }

    private FactionBase? BaseFor(AntimatterOwnership owner) => owner == AntimatterOwnership.Neutral ? null
        : GetTree().GetFirstNodeInGroup(owner == AntimatterOwnership.Player ? "player_base" : "enemy_base") as FactionBase;

    private void RefreshVisuals()
    {
        _material.AlbedoColor = Ownership switch { AntimatterOwnership.Player => new Color(0.15f, 0.85f, 1),
            AntimatterOwnership.Enemy => new Color(1, 0.25f, 0.3f), _ => new Color(0.65f, 0.7f, 0.75f) };
        _material.Emission = IsContested ? Colors.Orange : _material.AlbedoColor;
        _label.Text = StatusText;
    }

    public static double ProjectSpeed(FactionBase hub)
    {
        double speed = 1;
        foreach (Node node in hub.GetTree().GetNodesInGroup("strategic_point"))
            if (node is StrategicPoint point && !point.IsQueuedForDeletion() && point.Role == StrategicPointRole.Research
                && point.Ownership == (hub.IsPlayerBase ? AntimatterOwnership.Player : AntimatterOwnership.Enemy))
                speed += Math.Max(0, point.ResearchBonus);
        return speed;
    }

    public static string Summary(FactionBase hub)
    {
        int sites = 0, drones = 0; double income = 0, matter = 0;
        foreach (Node node in hub.GetTree().GetNodesInGroup("strategic_point"))
            if (node is StrategicPoint point && !point.IsQueuedForDeletion()
                && point.Ownership == (hub.IsPlayerBase ? AntimatterOwnership.Player : AntimatterOwnership.Enemy))
            {
                sites++; if (point.Role == StrategicPointRole.Extractor) income += point.IncomePerSecond;
                if (point.Role == StrategicPointRole.MatterExtractor) matter += point.IncomePerSecond;
                if (GodotObject.IsInstanceValid(point.Drone) && !point.Drone!.IsQueuedForDeletion()) drones++;
            }
        return $"Sites {sites} · income up to {income:0.#} antimatter/s + {matter:0.#} matter/s · project speed {ProjectSpeed(hub):0.##}× · station drones {drones}";
    }
}

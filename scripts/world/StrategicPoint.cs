using System;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;

namespace MagicCarpetRemastered.Scripts.World;

public enum StrategicPointRole { Extractor, Research, Logistics }

public partial class StrategicPoint : Node3D
{
    [Export] public StrategicPointRole Role { get; set; }
    [Export] public float CaptureRadius { get; set; } = 18;
    [Export] public double CaptureSeconds { get; set; } = 10;
    [Export] public double CaptureDecayMultiplier { get; set; } = 0.5;
    [Export] public double IncomePerSecond { get; set; } = 0.2;
    [Export] public double ResearchBonus { get; set; } = 0.25;
    public AntimatterOwnership Ownership { get; private set; }
    public AntimatterOwnership Claimant { get; private set; }
    public double CaptureProgress { get; private set; }
    public bool IsContested { get; private set; }
    public bool PlayerPresent { get; private set; }
    public string CaptureStage => IsContested ? "Contested" : Ownership != AntimatterOwnership.Neutral
        && Claimant != AntimatterOwnership.Neutral ? "Neutralizing" : "Capturing";
    public long GeneratedAntimatter { get; private set; }
    public int CaptureCount { get; private set; }
    public CollectorDrone? Drone { get; private set; }
    public string BenefitText => Role switch
    {
        StrategicPointRole.Extractor => $"+{IncomePerSecond:0.#} antimatter/s (room permitting)",
        StrategicPointRole.Research => $"+{ResearchBonus * 100:0}% project speed",
        _ => Drone == null ? "Collector activates on first capture" : GodotObject.IsInstanceValid(Drone) && !Drone.IsQueuedForDeletion()
            ? $"Station drone{(Drone.StationSuspended ? " · suspended" : "")} · {Drone.CarriedCount} cargo objects" : "Station drone destroyed"
    };
    public string StatusText => $"{Role} · {Ownership} · {BenefitText}\n"
        + (IsContested || Claimant != AntimatterOwnership.Neutral ? $"{CaptureStage} · {Claimant} {CaptureProgress:P0}" : "Remain in ring to capture");
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
        foreach (Node node in GetTree().GetNodesInGroup("pilots"))
            if (node is ShipFlightController ship && ship.Health > 0 && InRange(ship))
            { if (ship.IsPlayerControlled) player = true; else enemy = true; }
        foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
            if (node is EnemyEcosystemEnemy pilot && pilot.Role == EnemyEcosystemEnemy.EnemyRole.RivalShip
                && pilot.Health > 0 && InRange(pilot)) enemy = true;
        Advance(delta, player, enemy);
        _label.Visible = GetTree().GetFirstNodeInGroup("player") is ShipFlightController observer
            && observer.Health > 0 && _arena.SurfaceDistance(GlobalPosition, observer.GlobalPosition) <= CaptureRadius + 12;
        // Follow authoritative terrain, including live deformation.
        GlobalPosition = _arena.OnSurface(GlobalPosition);
    }

    private bool InRange(Node3D actor) => !actor.IsQueuedForDeletion()
        && _arena.SurfaceDistance(GlobalPosition, actor.GlobalPosition) <= CaptureRadius
        && Mathf.Abs(_arena.Clearance(actor.GlobalPosition)) <= _arena.MaxFlightClearance + 2;

    internal void Advance(double delta, bool playerPresent, bool enemyPresent)
    {
        if (GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        playerPresent &= BaseFor(AntimatterOwnership.Player) is { IsDestroyed: false };
        enemyPresent &= BaseFor(AntimatterOwnership.Enemy) is { IsDestroyed: false };
        PlayerPresent = playerPresent;
        IsContested = playerPresent && enemyPresent;
        // Income ends at neutralization even when one diagnostic/accelerated
        // tick spans both takeover stages. The new owner starts next tick.
        bool draining = !IsContested && (Ownership == AntimatterOwnership.Player && enemyPresent
            || Ownership == AntimatterOwnership.Enemy && playerPresent);
        AdvanceIncome(draining ? Math.Min(delta, (1 - CaptureProgress) * Math.Max(0.01, CaptureSeconds)) : delta);
        if (!IsContested)
        {
            var present = playerPresent ? AntimatterOwnership.Player : enemyPresent ? AntimatterOwnership.Enemy : AntimatterOwnership.Neutral;
            double work = delta / Math.Max(0.01, CaptureSeconds);
            if (present == AntimatterOwnership.Neutral || present == Ownership)
            {
                CaptureProgress = Math.Max(0, CaptureProgress - work * (present == Ownership && present != AntimatterOwnership.Neutral
                    ? 1 : Math.Max(0, CaptureDecayMultiplier)));
                if (CaptureProgress == 0) Claimant = AntimatterOwnership.Neutral;
            }
            else
            {
                if (Claimant != present && CaptureProgress > 0)
                {
                    double reversed = Math.Min(work, CaptureProgress);
                    CaptureProgress -= reversed; work -= reversed;
                }
                if (CaptureProgress == 0) Claimant = present;
                if (Claimant == present)
                {
                    double needed = 1 - CaptureProgress;
                    CaptureProgress = Math.Min(1, CaptureProgress + work);
                    if (CaptureProgress >= 1 && Ownership != AntimatterOwnership.Neutral)
                    {
                        Ownership = AntimatterOwnership.Neutral; _incomeFraction = 0;
                        if (GodotObject.IsInstanceValid(Drone) && !Drone!.IsQueuedForDeletion()) Drone.StationSuspended = true;
                        CaptureProgress = Math.Min(1, Math.Max(0, work - needed));
                    }
                    if (CaptureProgress >= 1) CompleteCapture(present);
                }
            }
        }
        RefreshVisuals();
    }

    private void AdvanceIncome(double delta)
    {
        if (Role != StrategicPointRole.Extractor
            || BaseFor(Ownership) is not { IsDestroyed: false } hub) { _incomeFraction = 0; return; }
        int available = hub.AvailableStorage;
        if (available == 0) { _incomeFraction = 0; return; }
        double accrued = _incomeFraction + delta * Math.Max(0, IncomePerSecond);
        int request = (int)Math.Min(available, Math.Floor(accrued));
        GeneratedAntimatter += hub.DepositAntimatter(request);
        _incomeFraction = hub.AvailableStorage == 0 ? 0 : accrued - Math.Floor(accrued);
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
        {
            Drone.StationSuspended = false;
            Drone.TransferTo(hub);
        }
    }

    private FactionBase? BaseFor(AntimatterOwnership owner) => owner == AntimatterOwnership.Neutral ? null
        : GetTree().GetFirstNodeInGroup(owner == AntimatterOwnership.Player ? "player_base" : "enemy_base") as FactionBase;

    private void RefreshVisuals()
    {
        _material.AlbedoColor = Ownership switch { AntimatterOwnership.Player => new Color(0.15f, 0.85f, 1),
            AntimatterOwnership.Enemy => new Color(1, 0.25f, 0.3f), _ => new Color(0.65f, 0.7f, 0.75f) };
        _material.Emission = IsContested ? Colors.Orange : _material.AlbedoColor;
        _label.Text = IsContested || Claimant != AntimatterOwnership.Neutral
            ? $"{Role} · {CaptureStage} {CaptureProgress:P0}" : Ownership == AntimatterOwnership.Player ? $"{Role} · OWNED"
            : $"{Role}\nHold in ring to capture";
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
        int sites = 0, drones = 0; double income = 0;
        foreach (Node node in hub.GetTree().GetNodesInGroup("strategic_point"))
            if (node is StrategicPoint point && !point.IsQueuedForDeletion()
                && point.Ownership == (hub.IsPlayerBase ? AntimatterOwnership.Player : AntimatterOwnership.Enemy))
            {
                sites++; if (point.Role == StrategicPointRole.Extractor) income += point.IncomePerSecond;
                if (GodotObject.IsInstanceValid(point.Drone) && !point.Drone!.IsQueuedForDeletion()) drones++;
            }
        return $"Sites {sites} · income up to {income:0.#} antimatter/s · project speed {ProjectSpeed(hub):0.##}× · station drones {drones}";
    }
}

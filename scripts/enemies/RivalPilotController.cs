using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Enemies;

// Decisions only: simulation, payments, equipment effects and respawn belong to
// the shared ship/base/world systems, just as they do for human input.
public partial class RivalPilotController : Node
{
    public enum PilotTask { Idle, Gather, Capture, Install, Explore, Attack, Defend, Recover, Respawning }
    [Export] public int StorageReserve { get; set; } = 12;
    [Export] public float AttackGraceSeconds { get; set; } = 300;
    [Export] public float AttackIntervalSeconds { get; set; } = 180;
    [Export] public float AttackDurationSeconds { get; set; } = 20;
    public PilotTask Task { get; private set; }
    public string StatusText => $"{Task} · {(_goal != null && IsInstanceValid(_goal) ? _goal.Name.ToString() : "no goal")} · {_ship.Energy}/{_ship.EnergyCapacity} energy · {OrdersPlaced} paid orders";
    public int OrdersPlaced { get; private set; }
    private ShipFlightController _ship = null!;
    private HeightmapArena _arena = null!;
    private Node3D? _goal;
    private float _age, _think, _researchTimer, _deadline, _stall, _lastDistance = float.PositiveInfinity;
    private float _nextAttack, _attackAtGoal = -1, _nextRecovery;
    private readonly Dictionary<ulong, float> _ignored = new();
    private readonly HashSet<ulong> _knownWrecks = new();
    private static readonly string[] PreferredItems = { "plasma-bolt", "energy-shield", "dash", "pulse-burst" };

    public override void _Ready()
    {
        _ship = GetParent<ShipFlightController>();
        _arena = (HeightmapArena)GetTree().GetFirstNodeInGroup("heightmap_arena");
        ProcessPhysicsPriority = -10;
        AddToGroup("rival_ai");
        _nextAttack = AttackGraceSeconds;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GetTree().Paused || !_ship.IsPhysicsProcessing() || !double.IsFinite(delta) || delta <= 0) return;
        float elapsed = (float)delta; _age += elapsed;
        if (_ship.Health <= 0) { Task = PilotTask.Respawning; _goal = null; Stop(); return; }
        if (_ship.HomeBase is not { IsDestroyed: false } home) { Task = PilotTask.Idle; _goal = null; Stop(); return; }
        _researchTimer -= elapsed;
        if (_researchTimer <= 0) { Research(home); _researchTimer = 5; }
        ResolveBoons(home);
        if (_ship.Health <= 35 && _age >= _nextRecovery && Task != PilotTask.Recover)
        { if (Task == PilotTask.Attack) EndAttack(); SetGoal(PilotTask.Recover, home); _nextRecovery = _age + 30; }
        _think -= elapsed;
        if (_think <= 0)
        {
            DiscoverWrecks();
            Node3D? threat = LocalThreat();
            if (threat != null && _ship.Health > 35 && _ship.Energy >= 5 && Task != PilotTask.Attack)
                SetGoal(PilotTask.Defend, threat);
            else if (!GoalValid(home) || _age >= _deadline) SelectGoal(home);
            _think = 0.5f;
        }
        if (!IsInstanceValid(_goal)) { Stop(); return; }
        if (!GoalValid(home)) { ClearGoal(); Stop(); return; }
        float distance = _arena.SurfaceDistance(_ship.GlobalPosition, _goal!.GlobalPosition);
        float arrive = Task switch { PilotTask.Gather => 28, PilotTask.Capture => ((StrategicPoint)_goal).CaptureRadius * 0.6f,
            PilotTask.Install or PilotTask.Recover => 12, PilotTask.Explore => ((ExplorationWreck)_goal).IsRevealed ? 2 : 28,
            PilotTask.Attack or PilotTask.Defend => 35, _ => 8 };
        Vector3 aim = _goal.GlobalPosition;
        if (_goal is FactionBase) aim += _arena.UpAt(aim) * 2;
        Navigate(aim, distance, arrive, elapsed);
        if (_goal == null) return;
        if (Task == PilotTask.Gather && distance <= 34) _ship.TryActivateClaim();
        if (Task == PilotTask.Explore && _goal is ExplorationWreck { IsRevealed: false } && distance <= 34 && Visible(_goal))
            _ship.TryActivateEquipmentSlot(0);
        if (Task is PilotTask.Attack or PilotTask.Defend)
        {
            if (_ship.Health <= 35 || _ship.Energy < 5)
            { EndAttack(); SetGoal(PilotTask.Recover, home); _nextRecovery = _age + 30; return; }
            if (distance <= 50 && Visible(_goal))
            {
                if (Task == PilotTask.Attack && _attackAtGoal < 0) _attackAtGoal = _age;
                _ship.TryActivateEquipmentSlot(0);
                if (_ship.IsEquipmentInstalled("energy-shield") && !_ship.IsShielded) _ship.TryActivateEquipmentSlot(1);
                if (_ship.IsEquipmentInstalled("pulse-burst") && distance < 35 && _ship.Energy > 30)
                {
                    _ship.SetAutonomousControls(Vector3.Zero, _arena.OnSurface(_goal.GlobalPosition) - _ship.AimOrigin);
                    _ship.TryActivateEquipmentSlot(3);
                }
            }
            if (_attackAtGoal >= 0 && _age - _attackAtGoal >= AttackDurationSeconds)
            { EndAttack(); SetGoal(PilotTask.Recover, home); _nextRecovery = _age + 30; }
        }
        if (Task == PilotTask.Install && distance < _ship.EquipmentInstallationRadius)
        {
            for (int slot = 0; slot < PreferredItems.Length; slot++)
                if (_ship.GetOwnedEquipmentRank(PreferredItems[slot]) > 0) _ship.TryInstallEquipment(PreferredItems[slot], slot, out _);
            ClearGoal();
        }
        if (Task == PilotTask.Recover && distance < 15 && _ship.Energy >= Math.Min(18, _ship.EnergyCapacity * 0.75f))
        { _nextRecovery = _age + 30; ClearGoal(); }
    }

    private void Stop() => _ship.SetAutonomousControls(Vector3.Zero, -_ship.GlobalBasis.Z);
    private void ClearGoal() { _goal = null; _think = 0; _stall = 0; }
    private bool Allowed(Node3D actor) => !actor.IsQueuedForDeletion() && (!_ignored.TryGetValue(actor.GetInstanceId(), out float until) || until <= _age);

    private void SetGoal(PilotTask task, Node3D goal)
    {
        if (Task == task && _goal == goal) return;
        Task = task; _goal = goal; _lastDistance = float.PositiveInfinity; _stall = 0;
        _deadline = _age + 25 + _arena.SurfaceDistance(_ship.GlobalPosition, goal.GlobalPosition) / Math.Max(1, _ship.MoveSpeed) * 2;
        _attackAtGoal = -1;
    }

    private bool GoalValid(FactionBase home)
    {
        if (!IsInstanceValid(_goal) || _goal!.IsQueuedForDeletion()) return false;
        return Task switch
        {
            PilotTask.Gather => _ship.EnergyCapacity >= _ship.ClaimEquipmentEnergyCost && (_goal is AntimatterPickup a
                ? a.IsAvailable && a.Ownership != _ship.Ownership && home.AvailableStorage > 0
                : _goal is MatterPickup m && m.IsAvailable && m.Ownership != _ship.Ownership && home.AvailableMatterStorage > 0),
            PilotTask.Capture => _goal is StrategicPoint site && site.Ownership != _ship.Ownership,
            PilotTask.Explore => _goal is ExplorationWreck { IsCollected: false },
            PilotTask.Defend => FactionCombat.CanDamage(_goal, _ship.Ownership) && _arena.SurfaceDistance(_ship.GlobalPosition, _goal.GlobalPosition) < 65,
            PilotTask.Attack => FactionCombat.CanDamage(_goal, _ship.Ownership),
            PilotTask.Install => PendingInstallation(), PilotTask.Recover => true, _ => false
        };
    }

    private void SelectGoal(FactionBase home)
    {
        if (Task == PilotTask.Attack) EndAttack();
        if (_ship.CanInstallEquipment)
            for (int i = 0; i < PreferredItems.Length; i++)
                if (_ship.GetOwnedEquipmentRank(PreferredItems[i]) > _ship.GetEquipmentRank(PreferredItems[i])) _ship.TryInstallEquipment(PreferredItems[i], i, out _);
        if (_age >= _nextRecovery && _ship.EnergyCapacity > 0
            && (_ship.Health <= 35 || _ship.Energy < Math.Min(8, _ship.EnergyCapacity * 0.25f)))
        { SetGoal(PilotTask.Recover, home); _nextRecovery = _age + 30; return; }
        if (PendingInstallation()) { SetGoal(PilotTask.Install, home); return; }
        Node3D? resource = ResourceGoal(home, 100);
        if (resource != null) { SetGoal(PilotTask.Gather, resource); return; }
        if (_age >= _nextAttack && home.Tier >= 2 && _ship.Health > 60 && _ship.Energy >= 20)
        {
            var enemyBase = GetTree().GetFirstNodeInGroup("player_base") as FactionBase;
            Node3D? opponent = enemyBase is { IsDestroyed: false } ? enemyBase : GetTree().GetFirstNodeInGroup("player") as ShipFlightController;
            if (opponent != null && FactionCombat.CanDamage(opponent, _ship.Ownership)) { SetGoal(PilotTask.Attack, opponent); return; }
        }
        StrategicPoint? best = null; float score = float.PositiveInfinity;
        foreach (Node node in GetTree().GetNodesInGroup("strategic_point"))
            if (node is StrategicPoint site && site.Ownership != _ship.Ownership && Allowed(site))
            {
                float fromHome = _arena.SurfaceDistance(home.GlobalPosition, site.GlobalPosition);
                if (_arena.IsSpherical && fromHome > _arena.PlanetRadius * 1.2f) continue;
                float priority = site.Role == StrategicPointRole.Extractor ? -80 : site.Role == StrategicPointRole.MatterExtractor && home.StoredMatter < 40 ? -60 : 0;
                float candidate = _arena.SurfaceDistance(_ship.GlobalPosition, site.GlobalPosition) + fromHome * 0.3f + priority;
                if (candidate < score) { score = candidate; best = site; }
            }
        if (best != null) { SetGoal(PilotTask.Capture, best); return; }
        foreach (Node node in GetTree().GetNodesInGroup("exploration_wreck"))
            if (node is ExplorationWreck wreck && _knownWrecks.Contains(wreck.GetInstanceId()) && !wreck.IsCollected && wreck.ArrivalRemaining <= 0 && Allowed(wreck))
            { SetGoal(PilotTask.Explore, wreck); return; }
        resource = ResourceGoal(home, _arena.IsSpherical ? _arena.PlanetRadius * 0.8f : 120);
        if (resource != null) { SetGoal(PilotTask.Gather, resource); return; }
        SetGoal(PilotTask.Recover, home); _nextRecovery = _age + 30;
    }

    private Node3D? ResourceGoal(FactionBase home, float range)
    {
        // Zero storage cannot recharge Claim. A no-cost capture goal is the
        // recovery path instead of waiting forever beside an unclaimable globe.
        if (_ship.EnergyCapacity < _ship.ClaimEquipmentEnergyCost) return null;
        Node3D? best = null; float score = Math.Min(range, 120);
        foreach (string group in new[] { "loose_antimatter", "matter_pickup" })
            foreach (Node node in GetTree().GetNodesInGroup(group))
            {
                if (node is not Node3D actor || !Allowed(actor)) continue;
                bool eligible = actor is AntimatterPickup a ? home.AvailableStorage > 0 && a.IsAvailable && a.Ownership != _ship.Ownership
                    : actor is MatterPickup m && home.AvailableMatterStorage > 0 && m.IsAvailable && m.Ownership != _ship.Ownership;
                if (!eligible) continue;
                float distance = _arena.SurfaceDistance(_ship.GlobalPosition, actor.GlobalPosition);
                if (distance < score) { score = distance; best = actor; }
            }
        return best;
    }

    private bool PendingInstallation() => PreferredItems.Any(id => _ship.GetOwnedEquipmentRank(id) > _ship.GetEquipmentRank(id));

    public bool TryOrderProject(string id)
    {
        if (_ship.Health <= 0 || GetTree().Paused || _ship.HomeBase is not { IsDestroyed: false } home) return false;
        var definition = home.Construction.Catalog.FirstOrDefault(p => p.Id == id);
        if (definition == null || home.StoredAntimatter - definition.Cost < StorageReserve || !home.Construction.TryStart(id, out _)) return false;
        OrdersPlaced++; return true;
    }

    private void Research(FactionBase home)
    {
        if (_age < 15) return;
        var tier = home.Construction.NextTierDefinition();
        if (tier != null && home.StoredAntimatter - tier.Cost >= StorageReserve && home.Construction.TryStartNextTier(out _)) OrdersPlaced++;
        string[] projects = { "collector-drive", "equipment-energy-shield-1", "equipment-dash-1", "energy-grid", "repair-matrix", "claim-range",
            "equipment-plasma-bolt-2", "equipment-pulse-burst-1", "equipment-energy-shield-2", "equipment-dash-2", "equipment-plasma-bolt-3", "equipment-pulse-burst-2" };
        foreach (string id in projects) if (TryOrderProject(id)) break;
    }

    private void ResolveBoons(FactionBase home)
    {
        foreach (var offer in _ship.Boons.Pending.ToArray())
        {
            if (home.StoredAntimatter < StorageReserve) { _ship.Boons.Skip(offer.Id, home); continue; }
            string[] preference = { "recharge", "energy-damage", "speed", "ship-hp", "fire-rate", "projectile-speed", "explosive-damage", "deployable-damage" };
            foreach (string id in preference) if (_ship.Boons.Choose(offer.Id, id)) break;
            if (_ship.Boons.Pending.Any(p => p.Id == offer.Id)) _ship.Boons.Skip(offer.Id, home);
        }
    }

    private void DiscoverWrecks()
    {
        var active = new HashSet<ulong>();
        foreach (Node node in GetTree().GetNodesInGroup("exploration_wreck"))
            if (node is ExplorationWreck wreck && !wreck.IsCollected && !wreck.IsQueuedForDeletion())
            { ulong id = wreck.GetInstanceId(); active.Add(id); if (_arena.SurfaceDistance(_ship.GlobalPosition, wreck.GlobalPosition) <= 120) _knownWrecks.Add(id); }
        _knownWrecks.IntersectWith(active);
        foreach (ulong id in _ignored.Where(p => p.Value <= _age).Select(p => p.Key).ToArray()) _ignored.Remove(id);
    }

    private Node3D? LocalThreat()
    {
        foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
            if (node is EnemyEcosystemEnemy mob && FactionCombat.CanDamage(mob, _ship.Ownership)
                && (mob.Territory?.Target == _ship || mob.RaidTarget == _ship.HomeBase)
                && _arena.SurfaceDistance(_ship.GlobalPosition, mob.GlobalPosition) < 55) return mob;
        if (GetTree().GetFirstNodeInGroup("player") is ShipFlightController player && player.Health > 0
            && _arena.SurfaceDistance(_ship.GlobalPosition, player.GlobalPosition) < 32) return player;
        return null;
    }

    private bool Visible(Node3D target)
    {
        using var ray = PhysicsRayQueryParameters3D.Create(_ship.AimOrigin, target.GlobalPosition + (target is FactionBase ? _arena.UpAt(target.GlobalPosition) * 2 : Vector3.Zero));
        ray.Exclude = new Godot.Collections.Array<Rid> { _ship.GetRid() };
        using var hit = _ship.GetWorld3D().DirectSpaceState.IntersectRay(ray);
        return hit.Count == 0 || hit["collider"].As<Node3D>() == target;
    }

    private void Navigate(Vector3 aim, float distance, float arrive, float elapsed)
    {
        Vector3 movement = Vector3.Zero;
        if (distance > arrive)
        {
            if (_lastDistance - distance > 0.75f) { _stall = 0; _lastDistance = distance; } else _stall += elapsed;
            Vector3 direction = _arena.DirectionTo(_ship.GlobalPosition, _goal!.GlobalPosition);
            if (_stall > 6) direction = (direction + direction.Cross(_arena.UpAt(_ship.GlobalPosition)) * 0.6f).Normalized();
            if (_stall > 12)
            { _ignored[_goal!.GetInstanceId()] = _age + 30; if (Task == PilotTask.Attack) EndAttack(); ClearGoal(); Stop(); return; }
            movement = direction * Mathf.Clamp((distance - arrive) / 35, 0, 1);
        }
        else _stall = 0;
        movement += _arena.UpAt(_ship.GlobalPosition) * Mathf.Clamp((6 - _arena.Clearance(_ship.GlobalPosition)) * 0.2f, -1, 1);
        _ship.SetAutonomousControls(movement, aim - _ship.AimOrigin);
        if (distance > 90 && _ship.Energy >= Math.Max(12, _ship.EnergyCapacity * 0.5f)) _ship.TryActivateBooster();
    }

    private void EndAttack() { _nextAttack = _age + AttackIntervalSeconds; _attackAtGoal = -1; }
}

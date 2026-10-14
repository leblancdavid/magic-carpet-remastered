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
    private float _routeProbe, _jumpRetry;
    private Vector3 _escapeHeading;
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
            if (threat != null && _ship.Health > 35 && _ship.Energy >= 5 && Task is not (PilotTask.Attack or PilotTask.Recover))
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
    private void ClearGoal() { _goal = null; _think = 0; _stall = 0; _escapeHeading = Vector3.Zero; }
    private bool Allowed(Node3D actor) => !actor.IsQueuedForDeletion() && (!_ignored.TryGetValue(actor.GetInstanceId(), out float until) || until <= _age);

    private void SetGoal(PilotTask task, Node3D goal)
    {
        if (Task == task && _goal == goal) return;
        Task = task; _goal = goal; _lastDistance = float.PositiveInfinity; _stall = 0;
        _escapeHeading = Vector3.Zero; _routeProbe = 0;
        _deadline = _age + 25 + _arena.SurfaceDistance(_ship.GlobalPosition, goal.GlobalPosition) / Math.Max(1, _ship.MoveSpeed) * 2;
        _attackAtGoal = -1;
    }

    private bool GoalValid(FactionBase home)
    {
        if (!IsInstanceValid(_goal) || _goal!.IsQueuedForDeletion()) return false;
        return Task switch
        {
            PilotTask.Gather => _ship.EnergyCapacity >= _ship.ClaimEquipmentEnergyCost && _goal is AntimatterPickup a
                && a.IsAvailable && a.Ownership != _ship.Ownership && home.AvailableStorage > 0,
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
                float priority = site.Role == StrategicPointRole.Extractor ? -80 : 0;
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
            foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter"))
            {
                if (node is not Node3D actor || !Allowed(actor)) continue;
                bool eligible = actor is AntimatterPickup a && home.AvailableStorage > 0 && a.IsAvailable && a.Ownership != _ship.Ownership;
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
        if (BaseOrders.IsOrder(id) && home.Construction.UpgradeRank(BaseOrders.Capability(id)) == 0)
            id = UpgradeCatalog.ProjectId(BaseOrders.Capability(id), 1);
        var definition = home.Construction.Catalog.FirstOrDefault(p => p.Id == id);
        if (definition == null || home.StoredAntimatter - definition.Cost < StorageReserve) return false;
        bool placed;
        if (BaseOrders.IsOrder(id))
        {
            var targets = BaseOrders.Targets(home, id).ToArray();
            placed = targets.Length > 0 && home.Construction.TryStartOrder(id, targets[0], out _);
        }
        else placed = home.Construction.TryStart(id, out _);
        if (!placed) return false;
        OrdersPlaced++; return true;
    }

    private void Research(FactionBase home)
    {
        if (_age < 15) return;
        var tier = home.Construction.NextTierDefinition();
        // A tier priced at the current storage ceiling must remain reachable;
        // reserve policy cannot demand resources the base cannot hold.
        if (tier != null && home.StoredAntimatter - tier.Cost >= Math.Min(StorageReserve,
            Math.Max(0, home.EffectiveStorageCapacity - tier.Cost)) && home.Construction.TryStartNextTier(out _)) OrdersPlaced++;
        string[] projects = { "collector-drive", "scout-drone", "equipment-energy-shield-1", "attack-drone", "harvester-drone", "equipment-pulse-burst-1", "equipment-dash-1", "defensive-drone", "energy-grid", "repair-matrix", "claim-range", "siphon-drone", "upgrade-strike-drone-1",
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
            _routeProbe -= elapsed; _jumpRetry -= elapsed;
            if (_routeProbe <= 0)
            {
                _escapeHeading = EscapeHeading(direction);
                _routeProbe = _stall > 1.5f ? 0.75f : 1;
            }
            if (_escapeHeading.LengthSquared() > 0.01f)
                direction = _arena.Tangent(_escapeHeading, _ship.GlobalPosition).Normalized();
            if (_stall > 2 && _jumpRetry <= 0)
            {
                _ship.TryActivateJump();
                _jumpRetry = 3;
            }
            if (_stall > 12)
            { _ignored[_goal!.GetInstanceId()] = _age + 30; if (Task == PilotTask.Attack) EndAttack(); ClearGoal(); Stop(); return; }
            movement = direction * Mathf.Clamp((distance - arrive) / 35, 0, 1);
        }
        else _stall = 0;
        movement += _arena.UpAt(_ship.GlobalPosition) * Mathf.Clamp((6 - _arena.Clearance(_ship.GlobalPosition)) * 0.2f, -1, 1);
        _ship.SetAutonomousControls(movement, aim - _ship.AimOrigin);
        if (distance > 90 && _stall < 1.5f && _ship.Energy >= Math.Max(12, _ship.EnergyCapacity * 0.5f)) _ship.TryActivateBooster();
    }

    private Vector3 EscapeHeading(Vector3 direct)
    {
        Vector3 start = _ship.GlobalPosition, up = _arena.UpAt(start);
        Vector3 best = direct;
        float bestScore = float.PositiveInfinity;
        // Bounded local lookahead, refreshed rather than probing terrain every frame.
        foreach (float angle in new[] { 0f, 30, -30, 60, -60, 90, -90, 120, -120, 180 })
        {
            Vector3 heading = direct.Rotated(up, Mathf.DegToRad(angle));
            float previous = _arena.SurfaceHeight(start), worst = 0;
            for (int step = 1; step <= 4; step++)
            {
                float height = _arena.SurfaceHeight(start + heading * (step * 2));
                worst = Mathf.Max(worst, (height - previous) / 2);
                previous = height;
            }
            if (worst > Mathf.Tan(Mathf.DegToRad(_ship.MaximumHoverSlopeDegrees - 3))) continue;
            Vector3 end = _arena.OnSurface(start + heading * 8, 2);
            using var ray = PhysicsRayQueryParameters3D.Create(start, end);
            ray.Exclude = new Godot.Collections.Array<Rid> { _ship.GetRid() };
            using var hit = _ship.GetWorld3D().DirectSpaceState.IntersectRay(ray);
            // The straight chord can intersect a convex slope that the sampled
            // hover route follows safely; only solid actors obstruct that route.
            if (hit.Count > 0 && !hit["collider"].As<Node3D>().IsInGroup("deformable_terrain")) continue;
            float score = _arena.SurfaceDistance(end, _goal!.GlobalPosition)
                + Mathf.Abs(angle) * 0.015f + worst * 2;
            // When blocked, prefer continuing the detour rather than alternating sides.
            if (_stall > 1.5f && _escapeHeading.LengthSquared() > 0.01f)
                score += (1 - heading.Dot(_escapeHeading.Normalized())) * 4;
            if (score < bestScore) { bestScore = score; best = heading; }
        }
        return best;
    }

    private void EndAttack() { _nextAttack = _age + AttackIntervalSeconds; _attackAtGoal = -1; }
}

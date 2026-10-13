using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Enemies;

// Local state/steering only. Damage, role attacks and movement remain on the body.
public sealed class NeutralTerritory
{
    public enum BehaviorState { Patrol, Alert, Pursuit, Returning }
    public BehaviorState State { get; private set; }
    public Node3D? Target { get; private set; }
    public Vector3 Home { get; }
    private readonly EnemyEcosystemEnemy _body;
    private readonly HeightmapArena? _arena;
    private readonly Vector3 _forward;
    private readonly bool _waterHabitat;
    private readonly System.Random _random;
    private Vector3 _wanderDestination;
    private float _wanderRemaining;
    private float _pauseRemaining;
    private float _wanderSpeed;
    private float _progressClock;
    private float _lastDistance;
    private float _alertRemaining;
    private float _reacquireDelay;
    private readonly System.Collections.Generic.List<Node3D> _pilots = new();
    private static readonly string[] PilotGroups = { "pilots", "legacy_rival_pilot" };
    private float _pilotRefresh;

    public NeutralTerritory(EnemyEcosystemEnemy body, HeightmapArena? arena, Vector3 home)
    {
        _body = body; _arena = arena; Home = home;
        _waterHabitat = arena != null && arena.HeightAt(home) < arena.WaterHeight;
        _random = new System.Random(body.WanderSeed != 0 ? body.WanderSeed
            : unchecked((int)(home.X * 997 + home.Y * 313 + home.Z * 101 + body.GlobalPosition.X * 71 + body.GlobalPosition.Z * 53)));
        _wanderDestination = body.GlobalPosition;
        Vector3 up = arena?.UpAt(home) ?? Vector3.Up;
        _forward = PlanetGeometry.Frame(up, Mathf.Abs(up.Z) < 0.9f ? Vector3.Forward : Vector3.Right).X;
    }

    public static bool IsLivingPilot(Node3D? actor) => GodotObject.IsInstanceValid(actor) && !actor!.IsQueuedForDeletion()
        && (actor is ShipFlightController { Health: > 0 }
            || actor is EnemyEcosystemEnemy { Role: EnemyEcosystemEnemy.EnemyRole.RivalShip, Health: > 0 });

    public float Distance(Vector3 from, Vector3 to) => _arena?.SurfaceDistance(from, to)
        ?? new Vector2(from.X - to.X, from.Z - to.Z).Length();

    private bool WithinHabitat(Node3D actor) => Distance(Home, actor.GlobalPosition) <= _body.HabitatLeashRadius;

    public void OnDamage(Node3D? attacker)
    {
        // Returning is a committed disengagement, not an immediately restartable chase.
        if (State == BehaviorState.Returning || _reacquireDelay > 0) return;
        if (IsLivingPilot(attacker) && WithinHabitat(attacker!)
            && Distance(Home, _body.GlobalPosition) <= _body.HabitatLeashRadius
            && Distance(_body.GlobalPosition, attacker!.GlobalPosition) <= _body.RetaliationRadius)
        {
            Target = attacker; State = BehaviorState.Pursuit;
        }
        else if (State != BehaviorState.Pursuit)
        {
            Target = null; State = BehaviorState.Alert; _alertRemaining = 3;
        }
    }

    public void Update(float delta)
    {
        _reacquireDelay = Mathf.Max(0, _reacquireDelay - delta);
        _pilotRefresh -= delta;
        if (State == BehaviorState.Pursuit)
        {
            if (!IsLivingPilot(Target) || !WithinHabitat(Target!)
                || Distance(Home, _body.GlobalPosition) > _body.HabitatLeashRadius
                || Distance(_body.GlobalPosition, Target!.GlobalPosition) > _body.DisengagementRadius)
                BeginReturn();
            return;
        }
        if (State == BehaviorState.Returning)
        {
            if (Distance(_body.GlobalPosition, Home) <= 4)
            { State = BehaviorState.Patrol; _reacquireDelay = 3; }
            return;
        }
        if (Distance(_body.GlobalPosition, Home) > _body.HabitatLeashRadius)
        { BeginReturn(); return; }
        if (_reacquireDelay <= 0)
        {
            Node3D? nearest = null;
            float best = _body.AwarenessRadius;
            // Cache membership, never positions or living state. Every physics
            // tick still checks proximity; roster refresh is staggered so dense
            // worlds do not allocate two native group arrays per idle mob/tick.
            if (_pilotRefresh <= 0)
            {
                _pilots.Clear();
                foreach (string group in PilotGroups)
                {
                    using var actors = (Godot.Collections.Array)_body.GetTree().GetNodesInGroup(group);
                    foreach (Variant actor in actors) if (actor.As<Node>() is Node3D pilot) _pilots.Add(pilot);
                }
                _pilotRefresh = 0.4f + (Mathf.Abs(_body.WanderSeed % 100) / 500f);
            }
            Vector3 bodyPosition = _body.GlobalPosition;
            foreach (Node3D pilot in _pilots) Consider(pilot);
            if (nearest != null) { Target = nearest; State = BehaviorState.Pursuit; return; }

            void Consider(Node node)
            {
                if (node is not Node3D pilot || !IsLivingPilot(pilot)) return;
                Vector3 position = pilot.GlobalPosition;
                if (Distance(Home, position) > _body.HabitatLeashRadius) return;
                float distance = Distance(bodyPosition, position);
                if (distance <= best) { best = distance; nearest = pilot; }
            }
        }
        if (State == BehaviorState.Alert)
        {
            _alertRemaining -= delta;
            if (_alertRemaining <= 0) State = BehaviorState.Patrol;
        }
        if (State == BehaviorState.Patrol) UpdateWander(delta);
    }

    private void BeginReturn() { Target = null; State = BehaviorState.Returning; }

    private float Range(float minimum, float maximum) => Mathf.Lerp(minimum, maximum, (float)_random.NextDouble());

    private void UpdateWander(float delta)
    {
        if (_pauseRemaining > 0) { _pauseRemaining = Mathf.Max(0, _pauseRemaining - delta); return; }
        float distance = Distance(_body.GlobalPosition, _wanderDestination);
        _wanderRemaining -= delta;
        _progressClock += delta;
        if (_progressClock >= 2)
        {
            if (_lastDistance - distance < 0.5f) _wanderRemaining = 0;
            _lastDistance = distance; _progressClock = 0;
        }
        if (_wanderRemaining > 0 && distance > 2) return;
        bool flying = _body.Role == EnemyEcosystemEnemy.EnemyRole.FlyingSwarm;
        _pauseRemaining = _wanderRemaining > 0 ? Range(flying ? 0.2f : 1, flying ? 1.5f : 4) : 0;
        _wanderRemaining = Range(7, 16);
        _wanderSpeed = Range(flying ? 0.4f : 0.25f, flying ? 0.7f : 0.5f);
        Vector3 up = _arena?.UpAt(Home) ?? Vector3.Up;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector3 tangent = _forward.Rotated(up, Range(0, Mathf.Tau));
            float radius = Range(0.2f, 1) * Mathf.Min(_body.PatrolRadius, _body.HabitatLeashRadius * 0.6f);
            Vector3 destination = _arena?.IsSpherical == true
                ? _arena.OnSurface(_arena.PlanetCenter + new Quaternion(up.Cross(tangent), radius / _arena.PlanetRadius) * up * _arena.PlanetRadius)
                : Home + tangent * radius;
            if (_arena != null && !flying)
            {
                if (!_waterHabitat && _arena.HeightAt(destination) < _arena.WaterHeight) continue;
                Vector3 current = _body.GlobalPosition;
                bool safe = true;
                for (int step = 1; step <= 4; step++)
                {
                    Vector3 next = _arena.StepToward(current, destination, Mathf.Min(4, Distance(current, destination)));
                    if (Mathf.Abs(_arena.SurfaceHeight(next) - _arena.SurfaceHeight(current)) > 3) { safe = false; break; }
                    current = next;
                }
                if (!safe) continue;
            }
            _wanderDestination = destination; _lastDistance = Distance(_body.GlobalPosition, destination); _progressClock = 0;
            return;
        }
        _wanderDestination = _body.GlobalPosition; _wanderRemaining = 1;
    }

    public Vector3 IdleMove()
    {
        Vector3 destination = Home;
        float speed = _body.MoveSpeed;
        if (State == BehaviorState.Patrol)
        {
            if (_pauseRemaining > 0) return Vector3.Zero;
            destination = _wanderDestination;
            speed *= _wanderSpeed;
        }
        else if (State == BehaviorState.Alert) return Vector3.Zero;
        if (Distance(_body.GlobalPosition, destination) < 1) return Vector3.Zero;
        return (_arena?.DirectionTo(_body.GlobalPosition, destination)
            ?? new Vector3(destination.X - _body.GlobalPosition.X, 0, destination.Z - _body.GlobalPosition.Z).Normalized()) * speed;
    }
}

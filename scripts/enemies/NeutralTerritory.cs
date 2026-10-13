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
    private float _patrolPhase;
    private float _alertRemaining;
    private float _reacquireDelay;

    public NeutralTerritory(EnemyEcosystemEnemy body, HeightmapArena? arena, Vector3 home)
    {
        _body = body; _arena = arena; Home = home;
        _patrolPhase = (body.GetInstanceId() % 17) * Mathf.Tau / 17;
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
        _patrolPhase += delta * 0.25f;
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
            using var players = (Godot.Collections.Array)_body.GetTree().GetNodesInGroup("pilots");
            // Legacy diagnostic pilots have their own small group. Scanning
            // every neutral for every neutral makes world density quadratic.
            using var enemies = (Godot.Collections.Array)_body.GetTree().GetNodesInGroup("legacy_rival_pilot");
            foreach (Variant node in players) Consider(node.As<Node>());
            foreach (Variant node in enemies) Consider(node.As<Node>());
            if (nearest != null) { Target = nearest; State = BehaviorState.Pursuit; return; }

            void Consider(Node node)
            {
                if (node is not Node3D pilot || !IsLivingPilot(pilot) || !WithinHabitat(pilot)) return;
                float distance = Distance(_body.GlobalPosition, pilot.GlobalPosition);
                if (distance <= best) { best = distance; nearest = pilot; }
            }
        }
        if (State == BehaviorState.Alert)
        {
            _alertRemaining -= delta;
            if (_alertRemaining <= 0) State = BehaviorState.Patrol;
        }
    }

    private void BeginReturn() { Target = null; State = BehaviorState.Returning; }

    public Vector3 IdleMove()
    {
        Vector3 destination = Home;
        float speed = _body.MoveSpeed;
        if (State == BehaviorState.Patrol)
        {
            Vector3 up = _arena?.UpAt(Home) ?? Vector3.Up;
            Vector3 tangent = _forward.Rotated(up, _patrolPhase);
            destination = _arena?.IsSpherical == true
                ? _arena.OnSurface(_arena.PlanetCenter + new Quaternion(up.Cross(tangent), _body.PatrolRadius / _arena.PlanetRadius) * up * _arena.PlanetRadius)
                : Home + tangent * _body.PatrolRadius;
            speed *= 0.25f;
        }
        else if (State == BehaviorState.Alert) return Vector3.Zero;
        if (Distance(_body.GlobalPosition, destination) < 1) return Vector3.Zero;
        return (_arena?.DirectionTo(_body.GlobalPosition, destination)
            ?? new Vector3(destination.X - _body.GlobalPosition.X, 0, destination.Z - _body.GlobalPosition.Z).Normalized()) * speed;
    }
}

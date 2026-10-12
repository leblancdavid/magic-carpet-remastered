using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

// Independent, terrain-only route oracle for verification, not rival navigation.
// Every graph link and endpoint connector is checked at one-metre intervals.
public partial class TerrainAccessChecks : Node3D
{
    private int _checks, _routes, _detours;
    private const int Rows = 96, Columns = 192;
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();
    private void Check(bool condition, string message)
    { _checks++; if (!condition) throw new Exception(message); }
    private async Task Frames(int count)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }

    private static Vector3 Point(HeightmapArena arena, Vector3 up) => arena.PlanetCenter + up * arena.PlanetRadius;
    private static float Distance(HeightmapArena arena, Vector3 a, Vector3 b)
        => arena.PlanetRadius * Mathf.Acos(Mathf.Clamp(a.Dot(b), -1, 1));

    private static float Slope(HeightmapArena arena, Vector3 up)
    {
        Vector3 point = Point(arena, up);
        Basis frame = PlanetGeometry.Frame(up, Mathf.Abs(up.Z) < 0.9f ? Vector3.Forward : Vector3.Right);
        float x = (arena.SurfaceHeight(point + frame.X) - arena.SurfaceHeight(point - frame.X)) / 2;
        float z = (arena.SurfaceHeight(point + frame.Z) - arena.SurfaceHeight(point - frame.Z)) / 2;
        return new Vector2(x, z).Length();
    }

    private static bool Gentle(HeightmapArena arena, Vector3 up) => Slope(arena, up) <= Mathf.Tan(Mathf.DegToRad(30));

    private static bool Link(HeightmapArena arena, Vector3 a, Vector3 b)
    {
        int steps = Mathf.Max(1, Mathf.CeilToInt(Distance(arena, a, b)));
        for (int i = 0; i <= steps; i++)
            if (!Gentle(arena, a.Slerp(b, (float)i / steps).Normalized())) return false;
        return true;
    }

    private static Vector3 GridUp(int id)
    {
        float latitude = -Mathf.Pi / 2 + (id / Columns + 0.5f) * Mathf.Pi / Rows;
        float longitude = -Mathf.Pi + (id % Columns + 0.5f) * Mathf.Tau / Columns;
        return new Vector3(Mathf.Cos(latitude) * Mathf.Cos(longitude), Mathf.Sin(latitude), Mathf.Cos(latitude) * Mathf.Sin(longitude));
    }

    private static int GridId(Vector3 up)
    {
        int y = Mathf.Clamp(Mathf.FloorToInt((Mathf.Asin(up.Y) + Mathf.Pi / 2) / Mathf.Pi * Rows), 0, Rows - 1);
        int x = Mathf.Clamp(Mathf.FloorToInt((Mathf.Atan2(up.Z, up.X) + Mathf.Pi) / Mathf.Tau * Columns), 0, Columns - 1);
        return y * Columns + x;
    }

    private static List<Vector3>? Route(HeightmapArena arena, Vector3 from, Vector3 to)
    {
        if (Link(arena, from, to)) return new List<Vector3> { from, to };
        int start = GridId(from);
        if (!Link(arena, from, GridUp(start))) return null;
        var queue = new PriorityQueue<int, float>();
        var cost = new Dictionary<int, float> { [start] = 0 };
        var parent = new Dictionary<int, int>();
        var visited = new HashSet<int>();
        queue.Enqueue(start, Distance(arena, from, to));
        while (queue.TryDequeue(out int current, out _))
        {
            if (!visited.Add(current)) continue;
            Vector3 up = GridUp(current);
            if (Distance(arena, up, to) <= arena.PlanetRadius * Mathf.Tau / Columns * 2 && Link(arena, up, to))
            {
                var result = new List<Vector3> { to, up };
                while (parent.TryGetValue(current, out int previous)) { current = previous; result.Add(GridUp(current)); }
                result.Add(from); result.Reverse(); return result;
            }
            int y = current / Columns, x = current % Columns;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            {
                if ((dx == 0 && dy == 0) || y + dy < 0 || y + dy >= Rows) continue;
                int next = (y + dy) * Columns + (x + dx + Columns) % Columns;
                if (visited.Contains(next)) continue;
                Vector3 target = GridUp(next);
                float candidate = cost[current] + Distance(arena, up, target);
                if (cost.TryGetValue(next, out float old) && old <= candidate) continue;
                if (!Link(arena, up, target)) continue;
                cost[next] = candidate; parent[next] = current;
                queue.Enqueue(next, candidate + Distance(arena, target, to));
            }
        }
        return null;
    }

    private List<Vector3> VerifyRoute(HeightmapArena arena, Vector3 from, Vector3 to, string description)
    {
        var route = Route(arena, from, to);
        Check(route != null, $"Normal-hover opening is connected: {description}; endpoint slopes {Mathf.RadToDeg(Mathf.Atan(Slope(arena, from))):0.0}/{Mathf.RadToDeg(Mathf.Atan(Slope(arena, to))):0.0} degrees");
        float length = 0;
        for (int i = 1; i < route!.Count; i++) length += Distance(arena, route[i - 1], route[i]);
        Check(length <= Distance(arena, from, to) * 2 + 20, $"Opening route has a practical detour budget: {description} ({length:0.0} m)");
        _routes++; if (route.Count > 2) _detours++;
        return route;
    }

    private async Task Drive(HeightmapArena arena, ShipFlightController ship, List<Vector3> route)
    {
        int waypoint = 1, energy = ship.Energy;
        for (int tick = 0; tick < 900 && waypoint < route.Count; tick++)
        {
            Vector3 target = arena.OnSurface(Point(arena, route[waypoint]), 2);
            float distance = arena.SurfaceDistance(ship.GlobalPosition, target);
            if (distance < 2.5f) { waypoint++; continue; }
            Vector3 heading = arena.DirectionTo(ship.GlobalPosition, target);
            ship.SetAutonomousControls(heading * Mathf.Clamp(distance / 10, 0, 1), target - ship.AimOrigin);
            await Frames(1);
            Check(ship.GlobalPosition.IsFinite() && ship.Health == 100 && arena.Clearance(ship.GlobalPosition) >= -0.4f,
                "Actual shared hover motion remains physically supported during ordinary travel");
        }
        Check(waypoint == route.Count && ship.Energy == energy, "Ship completes opening/return with normal hover and no jump, Dash or booster payment");
        ship.SetAutonomousControls(Vector3.Zero, -ship.GlobalBasis.Z);
    }

    private async Task Run()
    {
        try
        {
            foreach (int radius in PlanetSizes.Radii)
            {
                foreach (int seed in new[] { 7319, 17, 901, -42 })
                {
                    // A previous seed's flattened pads must not influence the
                    // next seed's start selection. Match fresh-game generation.
                    var room = new Node3D(); AddChild(room);
                    bool randomTerrain = Array.IndexOf(OS.GetCmdlineUserArgs(), "--random-worlds") >= 0;
                    var arena = new HeightmapArena { IsSpherical = true, PlanetRadius = radius,
                        GenerateSeededLayout = randomTerrain, WorldSeed = seed }; room.AddChild(arena);
                    var layout = randomTerrain ? arena.GeneratedLayout! : PlanetLayout.Create(arena, seed);
                    Check(layout != null, "Requested random world has an accepted layout");
                    if (!randomTerrain) arena.PreparePlanetLayout(layout!);
                    CheckLandBudget(arena, seed);
                    for (int side = 0; side < 2; side++)
                    {
                        Vector3 start = layout!.Starts[side];
                        var route = VerifyRoute(arena, start, layout.AntimatterFields[side], $"R{radius}, seed {seed}, faction {side}, antimatter");
                        VerifyRoute(arena, start, layout.MatterFields[side * 2], $"R{radius}, seed {seed}, faction {side}, matter");
                        for (int role = 0; role < 4; role++)
                            VerifyRoute(arena, start, layout.EconomicSites[side * 4 + role].Up,
                                $"R{radius}, seed {seed}, faction {side}, station {role}");
                        if (seed == 7319 && Array.IndexOf(OS.GetCmdlineUserArgs(), "--geometry-only") < 0)
                        {
                            var ship = new ShipFlightController { IsPlayerControlled = false, Position = arena.OnSurface(Point(arena, start), 2) };
                            room.AddChild(ship); ship.SetPhysicsProcess(false);
                            arena.RefreshPlanetDetail(); await Frames(12);
                            ship.SetPhysicsProcess(true);
                            await Drive(arena, ship, route);
                            route.Reverse(); await Drive(arena, ship, route);
                            ship.QueueFree(); await Frames(4);
                        }
                    }
                    room.QueueFree(); await Frames(4);
                }
            }
            GD.Print($"Terrain access checks passed: {_checks}; {_routes} connected opening routes, {_detours} localized detours; physical travel {(Array.IndexOf(OS.GetCmdlineUserArgs(), "--geometry-only") < 0 ? "verified for both factions on every size" : "skipped (geometry-only)")}");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    private void CheckLandBudget(HeightmapArena arena, int seed)
    {
        int land = 0, comfortable = 0, blocked = 0;
        // Different density and orientation from the generator's calibration.
        const int samples = 4096;
        for (int i = 0; i < samples; i++)
        {
            float y = 1 - 2 * (i + 0.5f) / samples, angle = i * 2.39996323f + 0.37f, r = Mathf.Sqrt(1 - y * y);
            Vector3 up = new(r * Mathf.Cos(angle), y, r * Mathf.Sin(angle));
            if (arena.HeightAt(Point(arena, up)) <= arena.WaterHeight) continue;
            land++;
            float slope = Slope(arena, up);
            if (slope <= Mathf.Tan(Mathf.DegToRad(20))) comfortable++;
            if (slope > Mathf.Tan(Mathf.DegToRad(40))) blocked++;
        }
        GD.Print($"Access profile {arena.PlanetRadius}, seed {seed}: gentle {100f * comfortable / land:0.0}%, steep {100f * blocked / land:0.0}%");
        Check(comfortable >= land * 0.85f && blocked >= land * 0.03f && blocked <= land * 0.05f,
            $"Prepared land stays predominantly gentle with 3–5% steep coverage on fresh radius {arena.PlanetRadius}, seed {seed}");
    }
}

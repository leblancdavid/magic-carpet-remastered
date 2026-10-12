using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Game;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class PlaytestMechanicsChecks : Node3D
{
    private int _checks;
    public override void _Ready() => Callable.From(() => { _ = RunChecks(); }).CallDeferred();

    private async Task RunChecks()
    {
        try
        {
            CheckNeutralDrops();
            await ClearActors();
            CheckRepair();
            await ClearActors();
            await CheckLiveRespawns();
            await ClearActors();
            await CheckScatterPhysics();
            await ClearActors();
            await CheckStaggeredDrops();
            await ClearActors();
            await CheckAltitude();
            GD.Print($"Playtest mechanics checks passed: {_checks}");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GetTree().Paused = false;
            Input.ActionRelease("ascend");
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }

    private T Add<T>(T actor, bool processing = false) where T : Node
    {
        AddChild(actor);
        actor.SetPhysicsProcess(processing);
        return actor;
    }

    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task ClearActors()
    {
        foreach (Node child in GetChildren()) child.QueueFree();
        await Frames(2);
    }

    private void CheckNeutralDrops()
    {
        var tracker = Add(new WorldAntimatterTracker());
        foreach (var role in new[] { EnemyEcosystemEnemy.EnemyRole.FlyingSwarm, EnemyEcosystemEnemy.EnemyRole.GroundBeast,
            EnemyEcosystemEnemy.EnemyRole.RangedCaster, EnemyEcosystemEnemy.EnemyRole.BaseAttacker })
        {
            var enemy = Add(new EnemyEcosystemEnemy { Role = role, NeutralAntimatterValue = 17 });
            tracker.Refresh();
            int total = tracker.TotalAntimatter;
            int loose = tracker.NeutralLooseAntimatter;
            Check(enemy.AntimatterReserve == 17 && tracker.NeutralCreatureAntimatter == 17, $"{role} carries counted neutral antimatter from spawn");
            enemy.ApplyDamage(1000);
            tracker.Refresh();
            Check(enemy.AntimatterReserve == 0 && tracker.TotalAntimatter == total && tracker.NeutralLooseAntimatter == loose + 17,
                $"{role} death converts its entire reserve to neutral globes");
            enemy.ApplyDamage(1000);
            tracker.Refresh();
            Check(tracker.TotalAntimatter == total, $"{role} repeated lethal hits cannot duplicate antimatter");
        }
        foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter"))
        {
            if (node is not AntimatterPickup orb || orb.IsQueuedForDeletion()) continue;
            Check(orb.AntimatterAmount >= 1 && orb.AntimatterAmount <= 9 && orb.LinearVelocity.IsZeroApprox(),
                $"Neutral death fragments retain randomized amounts and drop without launch impulse (amount {orb.AntimatterAmount}, velocity {orb.LinearVelocity})");
        }
        var rival = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.RivalShip });
        Check(rival.AntimatterReserve == 0, "Pilot respawn template has no minted reserve");
    }

    private void CheckRepair()
    {
        var hub = Add(new FactionBase { InitialStoredAntimatter = 120, MaxHealth = 100 });
        var player = Add(new ShipFlightController());
        var tracker = Add(new WorldAntimatterTracker());
        hub.ApplyDamage(50);
        hub._PhysicsProcess(4.9);
        Check(hub.Health == 50 && !hub.IsRepairing, "Base cannot repair during the five-second quiet delay");
        hub._PhysicsProcess(1.1);
        Check(hub.Health == 53 && hub.EffectiveStorageCapacity == 64 && hub.IsRepairing,
            "Repair starts after delay at three HP per second and reopens capacity");
        tracker.Refresh();
        Check(hub.StoredAntimatter == 60 && player.EnergyCapacity == 60 && tracker.TotalAntimatter == 120,
            "Repair creates capacity but never replaces spilled antimatter or power");
        hub.ApplyDamage(1);
        hub._PhysicsProcess(4);
        Check(hub.Health == 52 && !hub.IsRepairing, "A fresh attack resets the quiet timer");
        hub._PhysicsProcess(100);
        Check(hub.Health == 100 && hub.EffectiveStorageCapacity == 120, "Repair stops at maximum health");
        var reclaimed = Add(new AntimatterPickup { AntimatterAmount = 3, Ownership = AntimatterOwnership.Player });
        var drone = Add(new CollectorDrone());
        typeof(CollectorDrone).GetMethod("Scoop", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(drone, new object[] { reclaimed });
        typeof(CollectorDrone).GetMethod("DepositAll", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(drone, null);
        Check(hub.StoredAntimatter == 63 && player.EnergyCapacity == 63, "Repaired capacity accepts drone deliveries and raises energy limit");
        hub.ApplyDamage(1000);
        hub._PhysicsProcess(1000);
        Check(hub.IsDestroyed && hub.StoredAntimatter == 0 && hub.EffectiveStorageCapacity == 0, "Destroyed hub never auto-rebuilds");
    }

    private async Task CheckScatterPhysics()
    {
        var arena = Add(new HeightmapArena { CellsPerSide = 40, CellSize = 2, HeightScale = 0, WaterHeight = -20 });
        arena.FlattenTerrain(Vector3.Zero, 100000, 0);
        AntimatterScatter.Spawn(this, new Vector3(0, 4, 0), 61);
        var fragments = new List<AntimatterPickup>();
        var releasePositions = new Dictionary<AntimatterPickup, Vector3>();
        float maximumStartDistance = 0;
        foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter"))
        {
            if (node is not AntimatterPickup orb) continue;
            fragments.Add(orb);
            Check(orb.LinearVelocity.IsZeroApprox(), "Antimatter drops start without outward or upward launch velocity");
            releasePositions[orb] = orb.GlobalPosition;
            maximumStartDistance = Mathf.Max(maximumStartDistance, new Vector2(orb.GlobalPosition.X, orb.GlobalPosition.Z).Length());
        }
        Check(fragments.Count == 31, "61 antimatter fragments into 31 small spheres with exact remainders");
        var values = new HashSet<int>();
        int initialTotal = 0;
        foreach (AntimatterPickup orb in fragments)
        {
            values.Add(orb.AntimatterAmount);
            initialTotal += orb.AntimatterAmount;
            Check(orb.AntimatterAmount > 0, "Random fragments never contain zero antimatter");
        }
        Check(values.Count > 1 && initialTotal == 61, "Random-sized fragments vary while summing to the exact reserve");
        var tracker = Add(new WorldAntimatterTracker());
        await Frames(20);
        int airborne = 0;
        bool fallingStraight = true;
        foreach (var orb in fragments)
        {
            if (!IsInstanceValid(orb) || !orb.IsAvailable) continue;
            if (orb.GlobalPosition.Y > orb.Radius + 0.5f) airborne++;
            Vector3 departure = releasePositions[orb];
            fallingStraight &= new Vector2(orb.GlobalPosition.X - departure.X, orb.GlobalPosition.Z - departure.Z).Length() < 0.05f
                && orb.GlobalPosition.Y < departure.Y && orb.LinearVelocity.Y < 0;
        }
        tracker.Refresh();
        Check(airborne > 1 && airborne < fragments.Count && fallingStraight && maximumStartDistance < 3.5f,
            "Compact antimatter cluster releases gradually and falls straight down without outward scatter");
        Check(tracker.TotalAntimatter == 61, "Scatter conserves antimatter during flight and contacts");
        await Frames(220);
        tracker.Refresh();
        Check(tracker.TotalAntimatter == 61 && tracker.NeutralLooseAntimatter == 61, "Settling/merging conserves all scattered antimatter");
        await ClearActors();
        arena = Add(new HeightmapArena { CellsPerSide = 40, CellSize = 2, HeightScale = 0, WaterHeight = -20 });
        arena.FlattenTerrain(Vector3.Zero, 100000, 0);
        AntimatterScatter.Spawn(this, new Vector3(0, 4, 0), 513);
        fragments.Clear();
        bool separated = true;
        int total = 0;
        foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter"))
        {
            if (node is not AntimatterPickup orb || orb.IsQueuedForDeletion()) continue;
            foreach (AntimatterPickup previous in fragments)
                separated &= orb.GlobalPosition.DistanceTo(previous.GlobalPosition) >= orb.Radius + previous.Radius;
            fragments.Add(orb);
            total += orb.AntimatterAmount;
        }
        Check(fragments.Count == 128 && total == 513, "Large bursts retain body cap and exact remainders");
        Check(separated, "Enlarged dense fragments start without overlap-driven solver explosions");
        await ClearActors();
        foreach (int reserve in new[] { 1, 2, 3, 4, 10, 12, 17, 128, 257 })
        {
            AntimatterScatter.Spawn(this, Vector3.Zero, reserve);
            int sum = 0;
            int count = 0;
            values.Clear();
            foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter"))
            {
                if (node is not AntimatterPickup orb || orb.IsQueuedForDeletion()) continue;
                sum += orb.AntimatterAmount;
                count++;
                values.Add(orb.AntimatterAmount);
                Check(orb.AntimatterAmount > 0, "Partition boundary cases contain only positive antimatter");
            }
            Check(sum == reserve && count == Mathf.Min(128, (reserve - 1) / 2 + 1),
                $"Random partition preserves exact {reserve}-antimatter total and body cap");
            Check(reserve < 3 || values.Count > 1, $"{reserve}-antimatter burst has distinct fragment sizes when possible");
            await ClearActors();
        }
    }

    private async Task CheckStaggeredDrops()
    {
        AntimatterScatter.Spawn(this, new Vector3(0, 8, 0), 12, AntimatterOwnership.Enemy);
        var drops = new List<AntimatterPickup>();
        foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter"))
            if (node is AntimatterPickup orb) drops.Add(orb);
        var tracker = Add(new WorldAntimatterTracker());
        Check(drops.Count == 6 && drops.FindAll(orb => orb.IsAvailable).Count == 1,
            "Only the first antimatter ball releases immediately");
        Check(drops.FindAll(orb => orb.IsDropPending && !orb.Visible && orb.Freeze
            && orb.CollisionLayer == 0 && orb.CollisionMask == 0).Count == 5,
            "Pending balls are hidden, frozen, and noncolliding until release");
        tracker.Refresh();
        Check(tracker.TotalAntimatter == 12 && tracker.EnemyControlledAntimatter == 12,
            "Pending fragments count immediately with the correct ownership");
        var hauler = Add(new Node3D());
        AntimatterPickup pending = drops[1];
        pending.BeginCarry(hauler, Vector3.Zero);
        Check(!pending.IsCarried && !pending.IsAvailable && pending.TakeAntimatter(12) == 0,
            "Pending antimatter cannot be collected or transferred before release");
        // Exercise precise sub-interval timing independently of rendered-frame jitter.
        pending.SetPhysicsProcess(false);
        pending._PhysicsProcess(AntimatterScatter.DropIntervalSeconds * 0.4);
        Check(pending.IsDropPending, "A sub-interval tick does not release the next ball early");
        GetTree().Paused = true;
        await Frames(12);
        Check(drops.FindAll(orb => orb.IsDropPending).Count == 5,
            "Pausing gameplay also pauses staggered antimatter release");
        GetTree().Paused = false;
        pending._PhysicsProcess(AntimatterScatter.DropIntervalSeconds * 0.6);
        Check(!pending.IsDropPending && pending.Visible && !pending.Freeze
            && pending.CollisionLayer == 1 && pending.LinearVelocity.IsZeroApprox(),
            "The next ball becomes visible and physical at its interval without launch impulse");
        pending.SetPhysicsProcess(true);
        await Frames(5);
        int released = drops.FindAll(orb => !orb.IsDropPending).Count;
        tracker.Refresh();
        Check(released > 1 && released < 6 && tracker.TotalAntimatter == 12,
            "Partial release preserves exact antimatter instead of creating it over time");
        await Frames(20);
        tracker.Refresh();
        Check(drops.TrueForAll(orb => !orb.IsDropPending && orb.IsAvailable && orb.Visible)
            && tracker.TotalAntimatter == 12,
            "All six balls finish their short stagger with exact conserved totals");
        await ClearActors();
        var earlier = Add(new AntimatterPickup { AntimatterAmount = 20, Freeze = true });
        var later = Add(new AntimatterPickup { AntimatterAmount = 3 });
        later.ScheduleDrop(Vector3.Zero, AntimatterScatter.DropIntervalSeconds);
        later._PhysicsProcess(AntimatterScatter.DropIntervalSeconds);
        Check(later.IsAvailable && later.GlobalPosition.DistanceTo(earlier.GlobalPosition) > earlier.Radius + later.Radius
            && Mathf.Abs(later.GlobalPosition.X) < 0.001f && Mathf.Abs(later.GlobalPosition.Z) < 0.001f
            && later.LinearVelocity.IsZeroApprox(),
            "Late release lifts above a globe occupying its reserved spot without sideways scatter or collider overlap");
    }

    private async Task CheckLiveRespawns()
    {
        var arena = Add(new HeightmapArena { HeightScale = 0, WaterHeight = -20 });
        arena.FlattenTerrain(Vector3.Zero, 100000, 0);
        var home = Add(new FactionBase { InitialStoredAntimatter = 0, Position = new Vector3(-20, 2.5f, 0) });
        var hub = Add(new FactionBase { IsPlayerBase = false, InitialStoredAntimatter = 0, Position = new Vector3(20, 2.5f, 0) });
        var player = Add(new ShipFlightController { Position = new Vector3(-20, 6, 10) });
        var rival = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.RivalShip, Position = new Vector3(20, 6, 10) });
        rival.AbsorbAntimatter(17);
        var objective = Add(new LevelObjective(), true);
        objective.Initialize(player, home, hub, rival);
        player.ApplyDamage(1000);
        rival.ApplyDamage(1000);
        await Frames(30);
        Check(player.Health == 0 && objective.RivalPilotDefeated && objective.State == LevelObjective.LevelState.Playing,
            "Real-frame deaths retain the arena and start hub-backed countdowns");
        float remaining = objective.PlayerRespawnRemaining;
        GetTree().Paused = true;
        await Frames(3);
        Check(Mathf.IsEqualApprox(objective.PlayerRespawnRemaining, remaining), "Respawn countdown respects paused gameplay");
        GetTree().Paused = false;
        await Frames(585);
        var respawned = GetNode<EnemyEcosystemEnemy>("RivalPilot");
        respawned.SetPhysicsProcess(false);
        Check(player.Health == 100 && !objective.RivalPilotDefeated && respawned.Health > 0,
            "Actual ten-second gameplay ticks respawn both pilots without arena reload");
        var tracker = Add(new WorldAntimatterTracker());
        tracker.Refresh();
        Check(tracker.TotalAntimatter == 17, "Live respawn preserves the dropped reserve without resource creation");
        player._PhysicsProcess(2.1); // Expire spawn protection before the next lethal hit.
        player.SetPhysicsProcess(false);
        player.ApplyDamage(1000);
        await Frames(2);
        home.ApplyDamage(1000);
        await Frames(2);
        Check(objective.State == LevelObjective.LevelState.Defeat && player.Health == 0,
            "Losing home hub during countdown cancels recovery and ends the level");
    }

    private async Task CheckAltitude()
    {
        var arena = Add(new HeightmapArena { CellsPerSide = 40, CellSize = 2, HeightScale = 0, WaterHeight = -20 });
        arena.FlattenTerrain(Vector3.Zero, 100000, 0);
        var player = Add(new ShipFlightController { Position = new Vector3(0, 90, 0) }, true);
        player.CollisionLayer = player.CollisionMask = 0;
        Check(player.GlobalPosition.Y <= 12, "Player spawn clamps to the shared twelve-unit ceiling");
        Input.ActionPress("ascend");
        await Frames(180);
        Input.ActionRelease("ascend");
        Check(player.GlobalPosition.Y < 4 && Mathf.Abs(player.Velocity.Y) < 2,
            "Legacy ascend cannot control ship altitude; gravity returns it to low hovering");
        player.SetPhysicsProcess(false);
        var enemies = new List<EnemyEcosystemEnemy>();
        foreach (var role in new[] { EnemyEcosystemEnemy.EnemyRole.FlyingSwarm, EnemyEcosystemEnemy.EnemyRole.RangedCaster,
            EnemyEcosystemEnemy.EnemyRole.RivalShip })
        {
            var enemy = Add(new EnemyEcosystemEnemy { Role = role, Position = new Vector3(10, 80, 10) }, true);
            enemy.ContactDamage = enemy.RangedDamage = 0;
            enemies.Add(enemy);
        }
        bool bounded = true;
        for (int i = 0; i < 240; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            foreach (var enemy in enemies)
                bounded &= enemy.GlobalPosition.Y <= arena.SurfaceHeight(enemy.GlobalPosition.X, enemy.GlobalPosition.Z) + 12.01f;
        }
        Check(bounded, "Swarm, caster, and pilot stay below shared ceiling over sustained real flight");
        var simple = Add(new SimpleMonster { Position = new Vector3(-10, 70, 0) });
        var guardian = Add(new GuardianDrone { Position = new Vector3(-10, 70, -10) });
        var drone = Add(new CollectorDrone { Position = new Vector3(0, 70, -10) });
        Check(simple.GlobalPosition.Y <= 12 && guardian.GlobalPosition.Y <= 12 && drone.GlobalPosition.Y <= 12,
            "Legacy monsters, guardian, and drones obey the same spawn ceiling");
        var antimatter = Add(new AntimatterPickup { AntimatterAmount = 3 }, true);
        antimatter.LaunchScatter(new Vector3(0, 11, -15), Vector3.Up * 40);
        bool antimatterBounded = true;
        for (int i = 0; i < 60; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            antimatterBounded &= antimatter.GlobalPosition.Y <= 12.05f;
        }
        Check(antimatterBounded, "Physical antimatter bursts cannot exceed the shared ceiling");
        player.GlobalPosition = new Vector3(-15, 12, 15);
        foreach (float muzzleOffset in new[] { 0.0f, 1.2f })
        {
            Vector3 start = player.GlobalPosition + Vector3.Up * muzzleOffset;
            foreach (Vector3 direction in new[] { Vector3.Up, Vector3.Forward, new Vector3(1, -0.1f, 0).Normalized() })
            {
                var bolt = Add(new WeaponProjectile());
                bolt.GlobalPosition = start;
                bolt.Launch(direction, player);
                bolt._PhysicsProcess(0.1);
                Check(!bolt.IsQueuedForDeletion() && bolt.GlobalPosition.DistanceTo(start + direction * bolt.Speed * 0.1f) < 0.001f,
                    "PlasmaBolt preserves aim and speed when spawned at/above ceiling");
                bolt.Free();
                var claim = Add(new ClaimProjectile());
                claim.GlobalPosition = start;
                claim.Launch(direction);
                claim._PhysicsProcess(0.1);
                Check(!claim.IsQueuedForDeletion() && !claim.IsFollowingTerrain
                    && claim.GlobalPosition.DistanceTo(start + direction * claim.Speed * 0.1f) < 0.001f,
                    "Claim preserves airborne aim and speed when spawned at/above ceiling");
                claim.Free();
            }
            var enemyBolt = Add(new EnemyProjectile());
            enemyBolt.GlobalPosition = start;
            enemyBolt.Launch(Vector3.Up, enemies[0]);
            enemyBolt._PhysicsProcess(0.1);
            Check(!enemyBolt.IsQueuedForDeletion() && enemyBolt.GlobalPosition.DistanceTo(start + Vector3.Up * enemyBolt.Speed * 0.1f) < 0.001f,
                "Enemy projectiles are also exempt from the flight ceiling");
            enemyBolt.Free();
        }
        var downwardClaim = Add(new ClaimProjectile());
        downwardClaim.GlobalPosition = new Vector3(-15, 13.2f, 15);
        downwardClaim.Launch(new Vector3(0, -1, -1));
        downwardClaim._PhysicsProcess(0.5);
        float skimHeight = arena.SurfaceHeight(downwardClaim.GlobalPosition.X, downwardClaim.GlobalPosition.Z) + downwardClaim.SurfaceClearance;
        Check(!downwardClaim.IsQueuedForDeletion() && downwardClaim.IsFollowingTerrain
            && Mathf.Abs(downwardClaim.GlobalPosition.Y - skimHeight) < 0.001f,
            $"Claim fired above ceiling still transitions to ground skimming on surface contact (position {downwardClaim.GlobalPosition}, skim {skimHeight}, following {downwardClaim.IsFollowingTerrain}, queued {downwardClaim.IsQueuedForDeletion()})");
        downwardClaim.Free();
        arena.WaterHeight = 3;
        player.GlobalPosition = new Vector3(-15, 100, 15);
        arena.ClampFlightBody(player, player.MinimumAltitude);
        Check(Mathf.IsEqualApprox(player.GlobalPosition.Y, 15), "Ceiling follows water surface rather than submerged terrain");
    }

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }
}

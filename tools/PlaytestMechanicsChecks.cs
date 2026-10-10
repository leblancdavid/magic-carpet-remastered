using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Game;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Spells;
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
        var tracker = Add(new WorldManaTracker());
        foreach (var role in new[] { EnemyEcosystemEnemy.EnemyRole.FlyingSwarm, EnemyEcosystemEnemy.EnemyRole.GroundBeast,
            EnemyEcosystemEnemy.EnemyRole.RangedCaster, EnemyEcosystemEnemy.EnemyRole.CastleAttacker })
        {
            var enemy = Add(new EnemyEcosystemEnemy { Role = role, NeutralManaValue = 17 });
            tracker.Refresh();
            int total = tracker.TotalMana;
            int loose = tracker.NeutralLooseMana;
            Check(enemy.ManaReserve == 17 && tracker.NeutralCreatureMana == 17, $"{role} carries counted neutral mana from spawn");
            enemy.ApplyDamage(1000);
            tracker.Refresh();
            Check(enemy.ManaReserve == 0 && tracker.TotalMana == total && tracker.NeutralLooseMana == loose + 17,
                $"{role} death converts its entire reserve to neutral globes");
            enemy.ApplyDamage(1000);
            tracker.Refresh();
            Check(tracker.TotalMana == total, $"{role} repeated lethal hits cannot duplicate mana");
        }
        foreach (Node node in GetTree().GetNodesInGroup("loose_mana"))
        {
            if (node is not ManaPickup orb || !orb.IsAvailable) continue;
            Check(orb.ManaAmount >= 1 && orb.ManaAmount <= 9 && orb.LinearVelocity.IsZeroApprox(),
                $"Neutral death fragments retain randomized amounts and drop without launch impulse (amount {orb.ManaAmount}, velocity {orb.LinearVelocity})");
        }
        var rival = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.EnemyWizard });
        Check(rival.ManaReserve == 0, "Wizard respawn template has no minted reserve");
    }

    private void CheckRepair()
    {
        var castle = Add(new CastleKeep { InitialStoredMana = 120, MaxHealth = 100 });
        var player = Add(new CarpetFlightController());
        var tracker = Add(new WorldManaTracker());
        castle.ApplyDamage(50);
        castle._PhysicsProcess(4.9);
        Check(castle.Health == 50 && !castle.IsRepairing, "Castle cannot repair during the five-second quiet delay");
        castle._PhysicsProcess(1.1);
        Check(castle.Health == 53 && castle.EffectiveStorageCapacity == 64 && castle.IsRepairing,
            "Repair starts after delay at three HP per second and reopens capacity");
        tracker.Refresh();
        Check(castle.StoredMana == 60 && player.ClaimedMana == 140 && tracker.TotalMana == 120,
            "Repair creates capacity but never replaces spilled mana or power");
        castle.ApplyDamage(1);
        castle._PhysicsProcess(4);
        Check(castle.Health == 52 && !castle.IsRepairing, "A fresh attack resets the quiet timer");
        castle._PhysicsProcess(100);
        Check(castle.Health == 100 && castle.EffectiveStorageCapacity == 120, "Repair stops at maximum health");
        var reclaimed = Add(new ManaPickup { ManaAmount = 3, Ownership = ManaOwnership.Player });
        var balloon = Add(new ManaBalloon());
        typeof(ManaBalloon).GetMethod("Scoop", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(balloon, new object[] { reclaimed });
        typeof(ManaBalloon).GetMethod("DepositAll", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(balloon, null);
        Check(castle.StoredMana == 63 && player.ClaimedMana == 143, "Repaired capacity accepts balloon deliveries and restores power");
        castle.ApplyDamage(1000);
        castle._PhysicsProcess(1000);
        Check(castle.IsDestroyed && castle.StoredMana == 0 && castle.EffectiveStorageCapacity == 0, "Destroyed castle never auto-rebuilds");
    }

    private async Task CheckScatterPhysics()
    {
        var arena = Add(new HeightmapArena { CellsPerSide = 40, CellSize = 2, HeightScale = 0, WaterHeight = -20 });
        arena.FlattenTerrain(Vector3.Zero, 100000, 0);
        ManaScatter.Spawn(this, new Vector3(0, 4, 0), 61);
        var fragments = new List<ManaPickup>();
        var releasePositions = new Dictionary<ManaPickup, Vector3>();
        float maximumStartDistance = 0;
        foreach (Node node in GetTree().GetNodesInGroup("loose_mana"))
        {
            if (node is not ManaPickup orb) continue;
            fragments.Add(orb);
            Check(orb.LinearVelocity.IsZeroApprox(), "Mana drops start without outward or upward launch velocity");
            releasePositions[orb] = orb.GlobalPosition;
            maximumStartDistance = Mathf.Max(maximumStartDistance, new Vector2(orb.GlobalPosition.X, orb.GlobalPosition.Z).Length());
        }
        Check(fragments.Count == 31, "61 mana fragments into 31 small spheres with exact remainders");
        var values = new HashSet<int>();
        int initialTotal = 0;
        foreach (ManaPickup orb in fragments)
        {
            values.Add(orb.ManaAmount);
            initialTotal += orb.ManaAmount;
            Check(orb.ManaAmount > 0, "Random fragments never contain zero mana");
        }
        Check(values.Count > 1 && initialTotal == 61, "Random-sized fragments vary while summing to the exact reserve");
        var tracker = Add(new WorldManaTracker());
        await Frames(20);
        int airborne = 0;
        bool fallingStraight = true;
        foreach (var orb in fragments)
        {
            if (!IsInstanceValid(orb) || !orb.IsAvailable) continue;
            if (orb.GlobalPosition.Y > orb.Radius + 0.5f) airborne++;
            Vector3 departure = releasePositions[orb];
            fallingStraight &= new Vector2(orb.GlobalPosition.X - departure.X, orb.GlobalPosition.Z - departure.Z).Length() < 0.05f
                && orb.GlobalPosition.Y < departure.Y - 0.2f && orb.LinearVelocity.Y < -1;
        }
        tracker.Refresh();
        Check(airborne > 10 && fallingStraight && maximumStartDistance < 3.5f,
            "Compact mana cluster falls straight down under gravity without outward scatter");
        Check(tracker.TotalMana == 61, "Scatter conserves mana during flight and contacts");
        await Frames(220);
        tracker.Refresh();
        Check(tracker.TotalMana == 61 && tracker.NeutralLooseMana == 61, "Settling/merging conserves all scattered mana");
        await ClearActors();
        arena = Add(new HeightmapArena { CellsPerSide = 40, CellSize = 2, HeightScale = 0, WaterHeight = -20 });
        arena.FlattenTerrain(Vector3.Zero, 100000, 0);
        ManaScatter.Spawn(this, new Vector3(0, 4, 0), 513);
        fragments.Clear();
        bool separated = true;
        int total = 0;
        foreach (Node node in GetTree().GetNodesInGroup("loose_mana"))
        {
            if (node is not ManaPickup orb || !orb.IsAvailable) continue;
            foreach (ManaPickup previous in fragments)
                separated &= orb.GlobalPosition.DistanceTo(previous.GlobalPosition) >= orb.Radius + previous.Radius;
            fragments.Add(orb);
            total += orb.ManaAmount;
        }
        Check(fragments.Count == 128 && total == 513, "Large bursts retain body cap and exact remainders");
        Check(separated, "Enlarged dense fragments start without overlap-driven solver explosions");
        await ClearActors();
        foreach (int reserve in new[] { 1, 2, 3, 4, 10, 12, 17, 128, 257 })
        {
            ManaScatter.Spawn(this, Vector3.Zero, reserve);
            int sum = 0;
            int count = 0;
            values.Clear();
            foreach (Node node in GetTree().GetNodesInGroup("loose_mana"))
            {
                if (node is not ManaPickup orb || !orb.IsAvailable) continue;
                sum += orb.ManaAmount;
                count++;
                values.Add(orb.ManaAmount);
                Check(orb.ManaAmount > 0, "Partition boundary cases contain only positive mana");
            }
            Check(sum == reserve && count == Mathf.Min(128, (reserve - 1) / 2 + 1),
                $"Random partition preserves exact {reserve}-mana total and body cap");
            Check(reserve < 3 || values.Count > 1, $"{reserve}-mana burst has distinct fragment sizes when possible");
            await ClearActors();
        }
    }

    private async Task CheckLiveRespawns()
    {
        var arena = Add(new HeightmapArena { HeightScale = 0, WaterHeight = -20 });
        arena.FlattenTerrain(Vector3.Zero, 100000, 0);
        var home = Add(new CastleKeep { InitialStoredMana = 0, Position = new Vector3(-20, 2.5f, 0) });
        var castle = Add(new CastleKeep { IsPlayerCastle = false, InitialStoredMana = 0, Position = new Vector3(20, 2.5f, 0) });
        var player = Add(new CarpetFlightController { Position = new Vector3(-20, 6, 10) });
        var rival = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.EnemyWizard, Position = new Vector3(20, 6, 10) });
        rival.AbsorbMana(17);
        var objective = Add(new LevelObjective(), true);
        objective.Initialize(player, home, castle, rival);
        player.ApplyDamage(1000);
        rival.ApplyDamage(1000);
        await Frames(30);
        Check(player.Health == 0 && objective.RivalWizardDefeated && objective.State == LevelObjective.LevelState.Playing,
            "Real-frame deaths retain the arena and start castle-backed countdowns");
        float remaining = objective.PlayerRespawnRemaining;
        GetTree().Paused = true;
        await Frames(3);
        Check(Mathf.IsEqualApprox(objective.PlayerRespawnRemaining, remaining), "Respawn countdown respects paused gameplay");
        GetTree().Paused = false;
        await Frames(585);
        var respawned = GetNode<EnemyEcosystemEnemy>("RivalWizard");
        respawned.SetPhysicsProcess(false);
        Check(player.Health == 100 && !objective.RivalWizardDefeated && respawned.Health > 0,
            "Actual ten-second gameplay ticks respawn both wizards without arena reload");
        var tracker = Add(new WorldManaTracker());
        tracker.Refresh();
        Check(tracker.TotalMana == 17, "Live respawn preserves the dropped reserve without resource creation");
        player._PhysicsProcess(2.1); // Expire spawn protection before the next lethal hit.
        player.SetPhysicsProcess(false);
        player.ApplyDamage(1000);
        await Frames(2);
        home.ApplyDamage(1000);
        await Frames(2);
        Check(objective.State == LevelObjective.LevelState.Defeat && player.Health == 0,
            "Losing home castle during countdown cancels recovery and ends the level");
    }

    private async Task CheckAltitude()
    {
        var arena = Add(new HeightmapArena { CellsPerSide = 40, CellSize = 2, HeightScale = 0, WaterHeight = -20 });
        arena.FlattenTerrain(Vector3.Zero, 100000, 0);
        var player = Add(new CarpetFlightController { Position = new Vector3(0, 90, 0) }, true);
        player.CollisionLayer = player.CollisionMask = 0;
        Check(player.GlobalPosition.Y <= 12, "Player spawn clamps to the shared twelve-unit ceiling");
        Input.ActionPress("ascend");
        await Frames(60);
        Input.ActionRelease("ascend");
        Check(player.GlobalPosition.Y <= 12.01f && player.Velocity.Y <= 0.01f, "Holding ascend stops at ceiling without accumulating upward velocity");
        player.SetPhysicsProcess(false);
        var enemies = new List<EnemyEcosystemEnemy>();
        foreach (var role in new[] { EnemyEcosystemEnemy.EnemyRole.FlyingSwarm, EnemyEcosystemEnemy.EnemyRole.RangedCaster,
            EnemyEcosystemEnemy.EnemyRole.EnemyWizard })
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
        Check(bounded, "Swarm, caster, and wizard stay below shared ceiling over sustained real flight");
        var simple = Add(new SimpleMonster { Position = new Vector3(-10, 70, 0) });
        var guardian = Add(new SummonedGuardian { Position = new Vector3(-10, 70, -10) });
        var balloon = Add(new ManaBalloon { Position = new Vector3(0, 70, -10) });
        Check(simple.GlobalPosition.Y <= 12 && guardian.GlobalPosition.Y <= 12 && balloon.GlobalPosition.Y <= 12,
            "Legacy monsters, guardian, and balloons obey the same spawn ceiling");
        var mana = Add(new ManaPickup { ManaAmount = 3 }, true);
        mana.LaunchScatter(new Vector3(0, 11, -15), Vector3.Up * 40);
        bool manaBounded = true;
        for (int i = 0; i < 60; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            manaBounded &= mana.GlobalPosition.Y <= 12.05f;
        }
        Check(manaBounded, "Physical mana bursts cannot exceed the shared ceiling");
        player.GlobalPosition = new Vector3(-15, 12, 15);
        foreach (float muzzleOffset in new[] { 0.0f, 1.2f })
        {
            Vector3 start = player.GlobalPosition + Vector3.Up * muzzleOffset;
            foreach (Vector3 direction in new[] { Vector3.Up, Vector3.Forward, new Vector3(1, -0.1f, 0).Normalized() })
            {
                var bolt = Add(new SpellProjectile());
                bolt.GlobalPosition = start;
                bolt.Launch(direction, player);
                bolt._PhysicsProcess(0.1);
                Check(!bolt.IsQueuedForDeletion() && bolt.GlobalPosition.DistanceTo(start + direction * bolt.Speed * 0.1f) < 0.001f,
                    "Firebolt preserves aim and speed when spawned at/above ceiling");
                bolt.Free();
                var possess = Add(new PossessProjectile());
                possess.GlobalPosition = start;
                possess.Launch(direction);
                possess._PhysicsProcess(0.1);
                Check(!possess.IsQueuedForDeletion() && !possess.IsFollowingTerrain
                    && possess.GlobalPosition.DistanceTo(start + direction * possess.Speed * 0.1f) < 0.001f,
                    "Possess preserves airborne aim and speed when spawned at/above ceiling");
                possess.Free();
            }
            var enemyBolt = Add(new EnemyProjectile());
            enemyBolt.GlobalPosition = start;
            enemyBolt.Launch(Vector3.Up, enemies[0]);
            enemyBolt._PhysicsProcess(0.1);
            Check(!enemyBolt.IsQueuedForDeletion() && enemyBolt.GlobalPosition.DistanceTo(start + Vector3.Up * enemyBolt.Speed * 0.1f) < 0.001f,
                "Enemy projectiles are also exempt from the flight ceiling");
            enemyBolt.Free();
        }
        var downwardPossess = Add(new PossessProjectile());
        downwardPossess.GlobalPosition = new Vector3(-15, 13.2f, 15);
        downwardPossess.Launch(new Vector3(0, -1, -1));
        downwardPossess._PhysicsProcess(0.5);
        float skimHeight = arena.SurfaceHeight(downwardPossess.GlobalPosition.X, downwardPossess.GlobalPosition.Z) + downwardPossess.SurfaceClearance;
        Check(!downwardPossess.IsQueuedForDeletion() && downwardPossess.IsFollowingTerrain
            && Mathf.Abs(downwardPossess.GlobalPosition.Y - skimHeight) < 0.001f,
            $"Possess fired above ceiling still transitions to ground skimming on surface contact (position {downwardPossess.GlobalPosition}, skim {skimHeight}, following {downwardPossess.IsFollowingTerrain}, queued {downwardPossess.IsQueuedForDeletion()})");
        downwardPossess.Free();
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

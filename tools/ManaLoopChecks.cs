using System;
using System.Reflection;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Spells;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

// Run ManaLoopChecks.tscn headlessly to exercise real Godot nodes and transfers.
public partial class ManaLoopChecks : Node3D
{
    private int _checks;

    public override void _Ready()
    {
        Callable.From(RunChecks).CallDeferred();
    }

    private void RunChecks()
    {
        try
        {
            CheckPlayerCannotCollect();
            ClearActors();
            CheckBalloonDelivery();
            ClearActors();
            CheckBalloonArrival();
            ClearActors();
            CheckBalloonPop();
            ClearActors();
            CheckEnemyTransfers();
            ClearActors();
            CheckLegacyMonster();
            ClearActors();
            CheckPossession();
            GD.Print($"Mana loop checks passed: {_checks}");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }

    private T AddActor<T>(T actor) where T : Node
    {
        AddChild(actor);
        actor.SetPhysicsProcess(false);
        actor.SetProcess(false);
        return actor;
    }

    private void ClearActors()
    {
        foreach (Node child in GetChildren())
        {
            child.Free();
        }
    }

    private void CheckPlayerCannotCollect()
    {
        var player = AddActor(new CarpetFlightController());
        player.GlobalPosition = new Vector3(0, 20, 0);
        var tracker = AddActor(new WorldManaTracker());
        int claimed = player.ClaimedMana;
        int mana = player.Mana;
        foreach (ManaOwnership owner in new[] { ManaOwnership.Neutral, ManaOwnership.Player, ManaOwnership.Enemy })
        {
            var orb = AddActor(new ManaPickup { Ownership = owner, ManaAmount = 10 });
            orb.GlobalPosition = player.GlobalPosition + new Vector3(6, 0, 0);
            Vector3 position = orb.GlobalPosition;
            orb._PhysicsProcess(1.0 / 60);
            Check(orb.GlobalPosition.IsEqualApprox(position) && orb.LinearVelocity.IsZeroApprox(), $"{owner} mana is not attracted to player");
            orb.GlobalPosition = player.GlobalPosition;
            orb._PhysicsProcess(1.0 / 60);
            Invoke(orb, "OnBodyEntered", player);
            Check(orb.IsAvailable && orb.ManaAmount == 10, $"Player proximity/contact cannot absorb {owner} mana");
            Check(player.ClaimedMana == claimed && player.Mana == mana, "Player contact grants no spell power or refill");
        }
        tracker.Refresh();
        int total = tracker.TotalMana;
        Check(total == 30 && tracker.PlayerControlledMana == 10, "Uncollected owned mana stays counted in world exactly once");
        player.SpendMana(5);
        Invoke(player, "RegenerateMana", 1.0f);
        tracker.Refresh();
        Check(tracker.TotalMana == total, "Spell spending and regeneration do not change world mana");

        var consumed = AddActor(new ManaPickup { ManaAmount = 9 });
        Check(consumed.TakeMana(9) == 9 && !consumed.IsAvailable && consumed.TakeMana(9) == 0, "Consumed orb cannot transfer twice");
    }

    private void CheckBalloonDelivery()
    {
        var player = AddActor(new CarpetFlightController());
        player.GlobalPosition = new Vector3(100, 20, 100);
        var castle = AddActor(new CastleKeep { InitialStoredMana = 0 });
        var balloon = AddActor(new ManaBalloon { CarryCapacity = 2 });
        balloon.GlobalPosition = new Vector3(8, 4.5f, 0);
        var first = AddActor(new ManaPickup { Ownership = ManaOwnership.Player, ManaAmount = 8 });
        first.GlobalPosition = new Vector3(8, 0, 0);
        var second = AddActor(new ManaPickup { Ownership = ManaOwnership.Player, ManaAmount = 11 });
        second.GlobalPosition = new Vector3(9, 0, 0);
        var tracker = AddActor(new WorldManaTracker());
        int total = tracker.TotalMana;
        int claimed = player.ClaimedMana;
        for (int i = 0; i < 240 && castle.StoredMana < 19; i++)
        {
            balloon._PhysicsProcess(1.0 / 60);
            first._PhysicsProcess(1.0 / 60);
            second._PhysicsProcess(1.0 / 60);
            tracker.Refresh();
            Check(tracker.TotalMana == total, "Scoop/transit/deposit conserves mana");
        }
        Check(castle.StoredMana == 19 && balloon.CarriedCount == 0, "Multiple orbs scoop, dock, and deposit without enumeration errors");
        Check(player.ClaimedMana == claimed + 19, "Castle delivery grows power without double counting world mana");

        castle.DepositMana(castle.ManaStorageCapacity - castle.StoredMana - 4);
        var partial = AddActor(new ManaPickup { Ownership = ManaOwnership.Player, ManaAmount = 10 });
        partial.GlobalPosition = balloon.GlobalPosition - Vector3.Up * balloon.HoverHeight;
        Invoke(balloon, "Scoop", partial);
        tracker.Refresh();
        total = tracker.TotalMana;
        Invoke(balloon, "DepositAll");
        tracker.Refresh();
        Check(castle.StoredMana == castle.ManaStorageCapacity && partial.ManaAmount == 6, "Partial castle deposit removes accepted amount");
        Check(!partial.IsCarried && partial.Ownership == ManaOwnership.Neutral && balloon.CarriedCount == 0, "Overflow spills neutral and clears cargo");
        Check(tracker.TotalMana == total, "Partial castle deposit conserves mana");

        var distant = AddActor(new ManaPickup { Ownership = ManaOwnership.Player });
        distant.GlobalPosition = balloon.GlobalPosition + new Vector3(balloon.MaxSeekRadius + 1, 0, 0);
        Invoke(balloon, "HandleSeeking", 0.1f);
        Check(!distant.IsCarried, "Seek radius excludes distant mana");
    }

    private void CheckBalloonPop()
    {
        AddActor(new CastleKeep { InitialStoredMana = 0, IsPlayerCastle = false });
        var balloon = AddActor(new ManaBalloon { UsePlayerMana = false });
        balloon.GlobalPosition = new Vector3(5, 10, 5);
        var orb = AddActor(new ManaPickup { Ownership = ManaOwnership.Enemy, ManaAmount = 17 });
        Invoke(balloon, "Scoop", orb);
        var tracker = AddActor(new WorldManaTracker());
        int total = tracker.TotalMana;
        orb._PhysicsProcess(1.0 / 60);
        Check(orb.GlobalPosition.Y < balloon.GlobalPosition.Y, "Cargo sits below balloon mesh");
        balloon.ApplyDamage(balloon.MaxHealth);
        tracker.Refresh();
        Check(!orb.IsCarried && orb.Ownership == ManaOwnership.Neutral && orb.ManaAmount == 17, "Pop releases intact neutral cargo");
        Check(tracker.TotalMana == total && tracker.NeutralLooseMana == 17, "Pop changes ownership without creating mana");
    }

    private void CheckEnemyTransfers()
    {
        var castle = AddActor(new CastleKeep { IsPlayerCastle = false, InitialStoredMana = 0 });
        var wizard = AddActor(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.EnemyWizard });
        Check(wizard.ManaReserve == 0, "Wizard spawning does not mint mana");
        var orb = AddActor(new ManaPickup { ManaAmount = 19 });
        var tracker = AddActor(new WorldManaTracker());
        int total = tracker.TotalMana;
        wizard._PhysicsProcess(0.01);
        tracker.Refresh();
        Check(orb.ManaAmount == 0 && wizard.ManaReserve + castle.StoredMana == 19, "Wizard acquires and absorbs neutral orb");
        Check(tracker.TotalMana == total, "Wizard absorption/deposit conserves mana");
        wizard._PhysicsProcess(wizard.AttackCooldownSeconds + 0.01);
        Check(wizard.ManaReserve == 0 && castle.StoredMana == 19, "Wizard docks after collection cooldown");

        var thief = AddActor(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.ManaThief });
        var owned = AddActor(new ManaPickup { Ownership = ManaOwnership.Player, ManaAmount = 15 });
        thief._PhysicsProcess(0.01);
        thief._PhysicsProcess(thief.AttackCooldownSeconds + 0.01);
        Check(owned.ManaAmount == 0 && castle.StoredMana == 34, "Thief collects and deposits mana");

        wizard.AbsorbMana(19);
        tracker.Refresh();
        total = tracker.TotalMana;
        wizard.ApplyDamage(1000);
        tracker.Refresh();
        Check(wizard.ManaReserve == 0 && tracker.TotalMana == total, "Death drops preserve remainder and clear reserve");
        Check(tracker.EnemyControlledMana == total, "Wizard drops retain rival ownership");
        wizard.ApplyDamage(1000);
        tracker.Refresh();
        Check(tracker.TotalMana == total, "Repeated death damage cannot duplicate drops");
    }

    private void CheckLegacyMonster()
    {
        var monster = AddActor(new SimpleMonster { ManaDroppedOnDeath = 17 });
        var orb = AddActor(new ManaPickup { ManaAmount = 8 });
        var tracker = AddActor(new WorldManaTracker());
        int total = tracker.TotalMana;
        monster.AbsorbMana(orb.TakeMana(8));
        tracker.Refresh();
        Check(tracker.TotalMana == total, "Legacy monster absorption stays counted");
        monster.ApplyDamage(1000);
        tracker.Refresh();
        Check(monster.ManaReserve == 0 && tracker.TotalMana == total && tracker.NeutralLooseMana == total, "Legacy monster death returns initial and absorbed mana exactly");
    }

    private void CheckBalloonArrival()
    {
        var balloon = AddActor(new ManaBalloon());
        balloon.GlobalPosition = new Vector3(10, 6, 10);
        Vector3 target = balloon.GlobalPosition + new Vector3(0.02f, 0, 0);
        Invoke(balloon, "MoveToward", target, 1.0f);
        Check(balloon.GlobalPosition.IsEqualApprox(target), "Balloon capped step arrives exactly at target");
        Check((-balloon.GlobalBasis.Z).Dot(Vector3.Right) > 0.99f, "Balloon turns before exact arrival without a coincident LookAt");
        Basis facing = balloon.GlobalBasis;
        Invoke(balloon, "MoveToward", target, 1.0f);
        Check(balloon.GlobalBasis.IsEqualApprox(facing), "Balloon already at target keeps its facing");
        target += Vector3.Up;
        Invoke(balloon, "MoveToward", target, 1.0f);
        Check(balloon.GlobalPosition.IsEqualApprox(target) && balloon.GlobalBasis.IsEqualApprox(facing), "Vertical-only balloon movement preserves facing without invalid up-vector LookAt");
    }

    private void CheckPossession()
    {
        var arena = AddActor(new HeightmapArena { CellsPerSide = 24, CellSize = 2, HeightScale = 0, WaterHeight = -20 });
        arena.FlattenTerrain(Vector3.Zero, 100000, 0);
        arena.RaiseTerrain(new Vector3(8, 0, 0), 4, 5);
        var bolt = AddActor(new PossessProjectile());
        bolt.GlobalPosition = new Vector3(-8, 4, 0);
        bolt.Launch(new Vector3(1, -1, 0));
        bolt._PhysicsProcess(0.2);
        Check(bolt.IsFollowingTerrain, "Downward possession bolt lands and starts following terrain");
        float x = bolt.GlobalPosition.X;
        bolt._PhysicsProcess(0.2);
        Check(bolt.GlobalPosition.X > x && Mathf.Abs(bolt.GlobalPosition.Z) < 0.001f, "Terrain-following bolt keeps straight horizontal heading");
        Check(Mathf.Abs(bolt.GlobalPosition.Y - arena.HeightAt(bolt.GlobalPosition.X, 0) - bolt.SurfaceClearance) < 0.001f, "Bolt climbs raised terrain at fixed clearance");
        arena.ApplyCrater(bolt.GlobalPosition + new Vector3(2, 0, 0), 4, 6);
        bolt._PhysicsProcess(0.05);
        Check(Mathf.Abs(bolt.GlobalPosition.Y - arena.HeightAt(bolt.GlobalPosition.X, 0) - bolt.SurfaceClearance) < 0.001f, "Bolt samples deformed terrain before collision rebuild");
        bolt.Free();

        arena.FlattenTerrain(Vector3.Zero, 100000, 0);
        // Add farther orb first to catch group-order selection, then skip owned,
        // carried, spent, and off-path orbs before hitting the nearer rival.
        var farther = AddActor(new ManaPickup { ManaAmount = 9 });
        farther.GlobalPosition = new Vector3(9, 12, 0);
        farther._PhysicsProcess(0);
        Check(Mathf.Abs(farther.GlobalPosition.Y - farther.Radius) < 0.001f, "Unclaimed mana touches terrain for possession");
        var owned = AddActor(new ManaPickup { Ownership = ManaOwnership.Player });
        owned.GlobalPosition = new Vector3(1, 0.9f, 0);
        var carried = AddActor(new ManaPickup());
        carried.GlobalPosition = new Vector3(2, 0.9f, 0);
        carried.BeginCarry(this, Vector3.Zero);
        var spent = AddActor(new ManaPickup());
        spent.GlobalPosition = new Vector3(3, 0.9f, 0);
        spent.TakeMana(spent.ManaAmount);
        var offPath = AddActor(new ManaPickup());
        offPath.GlobalPosition = new Vector3(4, 0.9f, 4);
        var near = AddActor(new ManaPickup { Ownership = ManaOwnership.Enemy, ManaAmount = 17 });
        near.GlobalPosition = new Vector3(6, 1.3f, 0);
        var tracker = AddActor(new WorldManaTracker());
        int total = tracker.TotalMana;
        int playerTotal = tracker.PlayerControlledMana;
        bolt = AddActor(new PossessProjectile());
        bolt.GlobalPosition = new Vector3(0, 0.9f, 0);
        bolt.Launch(Vector3.Right);
        int claimed = 0;
        bolt.ManaPossessed += amount => claimed += amount;
        bolt._PhysicsProcess(0.3);
        tracker.Refresh();
        Check(near.Ownership == ManaOwnership.Player && near.ManaAmount == 17 && claimed == 17, "Swept hit claims first rival globe without consuming its mana");
        Check(farther.Ownership == ManaOwnership.Neutral && offPath.Ownership == ManaOwnership.Neutral && carried.Ownership == ManaOwnership.Neutral, "Bolt ignores off-path/carried globes and stops at first hit");
        Check(bolt.IsQueuedForDeletion() && tracker.TotalMana == total && tracker.PlayerControlledMana == playerTotal + 17, "Claim conserves world mana and terminates bolt");
        bolt._PhysicsProcess(0.3);
        Check(claimed == 17, "Claim cannot repeat after impact");

        bolt = AddActor(new PossessProjectile());
        bolt.GlobalPosition = new Vector3(7, 0.9f, 0);
        bolt.Launch(Vector3.Right);
        bolt._PhysicsProcess(0.1);
        Check(farther.Ownership == ManaOwnership.Player && bolt.IsQueuedForDeletion(), "Neutral globe is also claimable by bolt impact");

        bolt = AddActor(new PossessProjectile { MaxRange = 3 });
        bolt.GlobalPosition = new Vector3(0, 10, -10);
        bolt.Launch(Vector3.Right);
        bolt._PhysicsProcess(1);
        Check(bolt.IsQueuedForDeletion() && Mathf.Abs(bolt.GlobalPosition.X - 3) < 0.001f, "Miss expires at bounded range");
        bolt = AddActor(new PossessProjectile { LifetimeSeconds = 0.1f });
        bolt.GlobalPosition = new Vector3(0, 10, -10);
        bolt.Launch(Vector3.Up);
        bolt._PhysicsProcess(1);
        Check(bolt.IsQueuedForDeletion() && !bolt.IsFollowingTerrain, "Airborne vertical miss expires safely by lifetime");

        arena.WaterHeight = 2;
        bolt = AddActor(new PossessProjectile());
        bolt.GlobalPosition = new Vector3(-10, 0, -10);
        bolt.Launch(Vector3.Right);
        bolt._PhysicsProcess(0.02);
        Check(Mathf.Abs(bolt.GlobalPosition.Y - 2 - bolt.SurfaceClearance) < 0.001f, "Bolt skims water rather than submerged terrain");

        var player = AddActor(new CarpetFlightController());
        Check(InputMap.ActionHasEvent("cast_possess", new InputEventMouseButton { ButtonIndex = MouseButton.Right }), "RMB binds possession");
        Check(!InputMap.HasAction("cast_terrain") && !InputMap.HasAction("terrain_raise"), "Terrain tool input is hidden");
        for (int i = 0; i < 5; i++) Invoke(player, "CycleQuickSpell");
        Check(player.PrimarySpellName == "Firebolt", "Tab wraps through five combat/utility spells");
        int mana = player.Mana;
        Invoke(player, "CastPossessSpell");
        Check(player.Mana == mana - player.PossessSpellManaCost, "Casting a miss still spends possession mana");
        foreach (Node child in GetChildren())
        {
            if (child is PossessProjectile projectile) projectile.SetPhysicsProcess(false);
        }
    }

    private static void Invoke(object target, string method, params object[] arguments)
    {
        MethodInfo info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing method {method}");
        info.Invoke(target, arguments);
    }

    private void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
        _checks++;
    }
}

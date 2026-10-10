using System;
using System.Reflection;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
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
            CheckCollection();
            ClearActors();
            CheckBalloonDelivery();
            ClearActors();
            CheckBalloonPop();
            ClearActors();
            CheckEnemyTransfers();
            ClearActors();
            CheckLegacyMonster();
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

    private void CheckCollection()
    {
        var player = AddActor(new CarpetFlightController());
        player.GlobalPosition = new Vector3(0, 20, 0);
        var orb = AddActor(new ManaPickup { Ownership = ManaOwnership.Player, ManaAmount = 10 });
        orb.GlobalPosition = player.GlobalPosition + new Vector3(6, 0, 0);
        var tracker = AddActor(new WorldManaTracker());
        int total = tracker.TotalMana;
        int claimed = player.ClaimedMana;
        orb._PhysicsProcess(1.0 / 60);
        float firstDistance = orb.GlobalPosition.DistanceTo(player.GlobalPosition);
        orb._PhysicsProcess(1.0 / 60);
        Check(orb.GlobalPosition.DistanceTo(player.GlobalPosition) < firstDistance, "Orb homing accumulates");
        for (int i = 0; i < 120 && orb.IsAvailable; i++)
        {
            orb._PhysicsProcess(1.0 / 60);
        }
        tracker.Refresh();
        Check(player.CollectedWorldMana == 10 && player.ClaimedMana == claimed + 10, "Pickup transfers to held mana and power");
        Check(tracker.TotalMana == total && tracker.PlayerControlledMana == 10, "Collected orb stays counted exactly once");
        Check(orb.ManaAmount == 0 && !orb.IsAvailable && orb.TakeMana(10) == 0, "Consumed orb cannot transfer twice");
        player.SpendMana(5);
        Invoke(player, "RegenerateMana", 1.0f);
        tracker.Refresh();
        Check(tracker.TotalMana == total, "Spell spending and regeneration do not change world mana");

        var capped = AddActor(new ManaPickup { Ownership = ManaOwnership.Player, ManaAmount = 9 });
        capped.GlobalPosition = player.GlobalPosition;
        player.ClaimMana(player.MaxClaimedMana - player.ClaimedMana - 3);
        tracker.Refresh();
        total = tracker.TotalMana;
        capped._PhysicsProcess(1.0 / 60);
        tracker.Refresh();
        Check(capped.ManaAmount == 6 && player.CollectedWorldMana == 13, "Player capacity leaves unaccepted orb mana");
        Check(tracker.TotalMana == total, "Partial player collection conserves mana");
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
        Check(player.ClaimedMana == claimed + 19 && player.CollectedWorldMana == 0, "Castle delivery grows power without held-mana double counting");

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

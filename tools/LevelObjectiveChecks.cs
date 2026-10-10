using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Game;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Spells;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class LevelObjectiveChecks : Node3D
{
    private int _checks;

    public override void _Ready() => Callable.From(RunChecks).CallDeferred();

    private async void RunChecks()
    {
        try
        {
            CheckVictory(castleFirst: true);
            ClearActors();
            CheckVictory(castleFirst: false);
            ClearActors();
            CheckDefeat();
            ClearActors();
            CheckRespawns();
            ClearActors();
            CheckArenaIntegration();
            GetTree().Paused = false;
            ClearActors();
            await CheckRestart();
            GD.Print($"Level objective checks passed: {_checks}");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GetTree().Paused = false;
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }

    private (LevelObjective Objective, CarpetFlightController Player, CastleKeep Home,
        CastleKeep RivalCastle, EnemyEcosystemEnemy Rival) CreateActors()
    {
        var player = AddActor(new CarpetFlightController());
        var home = AddActor(new CastleKeep { InitialStoredMana = 120 });
        var castle = AddActor(new CastleKeep { IsPlayerCastle = false, InitialStoredMana = 0 });
        var rival = AddActor(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.EnemyWizard });
        var objective = AddActor(new LevelObjective());
        objective.Initialize(player, home, castle, rival);
        return (objective, player, home, castle, rival);
    }

    private void CheckVictory(bool castleFirst)
    {
        var actors = CreateActors();
        var tracker = AddActor(new WorldManaTracker());
        tracker.Refresh();
        Check(tracker.EquilibriumMet, "Fixture exceeds mana quota");
        actors.Objective.Evaluate();
        Check(actors.Objective.State == LevelObjective.LevelState.Playing, "Mana quota cannot win level");
        int finishes = 0;
        actors.Objective.Finished += () => finishes++;
        if (castleFirst)
        {
            HitCastleWithFirebolts(actors.RivalCastle);
        }
        else
        {
            actors.Rival.ApplyDamage(1000);
        }
        actors.Objective.Evaluate();
        Check(actors.Objective.State == LevelObjective.LevelState.Playing, "One defeated target cannot win");
        if (castleFirst)
        {
            actors.Rival.ApplyDamage(1000);
        }
        else
        {
            // Freeing the dead wizard must preserve the recorded kill.
            actors.Rival.Free();
            HitCastleWithFirebolts(actors.RivalCastle);
        }
        actors.Objective.Evaluate();
        Check(actors.Objective.State == LevelObjective.LevelState.Victory, "Both defeated targets win in either order");
        actors.Home.ApplyDamage(1000);
        actors.Objective.Evaluate();
        Check(actors.Objective.State == LevelObjective.LevelState.Victory && finishes == 1, "Victory is terminal and emitted once");
    }

    private void CheckDefeat()
    {
        var actors = CreateActors();
        actors.Rival.ApplyDamage(1000);
        actors.RivalCastle.ApplyDamage(1000);
        actors.Home.ApplyDamage(1000);
        actors.Player.ApplyDamage(1000);
        actors.Objective.Evaluate();
        Check(actors.Objective.State == LevelObjective.LevelState.Defeat, "Home castle loss takes precedence over simultaneous victory");
        Check(!string.IsNullOrEmpty(actors.Objective.ResultMessage), "Defeat explains its cause");
    }

    private void CheckRespawns()
    {
        var actors = CreateActors();
        actors.Home.GlobalPosition = new Vector3(-20, 0, 0);
        actors.RivalCastle.GlobalPosition = new Vector3(20, 0, 0);
        actors.Rival.AbsorbMana(17);
        var tracker = AddActor(new WorldManaTracker());
        int total = tracker.TotalMana;
        actors.Player.ApplyDamage(1000);
        actors.Rival.ApplyDamage(1000);
        actors.Objective.Evaluate();
        Check(actors.Objective.State == LevelObjective.LevelState.Playing && actors.Objective.PlayerRespawnRemaining == 10,
            "Wizard death starts respawn rather than ending a level with a living castle");
        Vector3 position = actors.Player.GlobalPosition;
        actors.Player._PhysicsProcess(1);
        Check(actors.Player.Health == 0 && actors.Player.GlobalPosition == position && actors.Player.CollisionLayer == 0,
            "Dead player cannot move or collide during countdown");
        actors.Objective._PhysicsProcess(9.9);
        Check(actors.Player.Health == 0 && actors.Objective.RivalWizardDefeated, "Both wizards stay dead before ten seconds");
        actors.Rival.Free();
        actors.Objective._PhysicsProcess(0.2);
        var rival = GetNode<EnemyEcosystemEnemy>("RivalWizard");
        rival.SetPhysicsProcess(false);
        Check(actors.Player.Health == 100 && actors.Player.Mana == actors.Player.ManaCapacity && actors.Player.IsInvulnerable,
            "Player respawns with restored health/mana and brief protection");
        Check(actors.Player.GlobalPosition.DistanceTo(actors.Home.GlobalPosition) < 12 && actors.Player.CollisionLayer != 0,
            "Player returns beside its home castle with collision restored");
        Check(rival.Health > 0 && rival.GlobalPosition.DistanceTo(actors.RivalCastle.GlobalPosition) < 12
            && !actors.Objective.RivalWizardDefeated, "Rival respawns at its own castle and clears the old defeat");
        tracker.Refresh();
        Check(tracker.TotalMana == total && rival.ManaReserve == 0, "Respawns do not mint physical wizard mana");
        rival.ApplyDamage(1000);
        actors.Objective._PhysicsProcess(10);
        var second = GetNode<EnemyEcosystemEnemy>("RivalWizard");
        second.SetPhysicsProcess(false);
        Check(second != rival && second.Health > 0, "Repeated rival deaths rebind the respawn lifecycle");
        actors.RivalCastle.ApplyDamage(1000);
        actors.Objective._PhysicsProcess(20);
        Check(!actors.Objective.RivalWizardDefeated && second.Health > 0, "Castle destruction alone leaves the current wizard alive");
        second.ApplyDamage(1000);
        actors.Objective._PhysicsProcess(20);
        Check(actors.Objective.State == LevelObjective.LevelState.Victory, "Destroyed castle prevents respawn and final wizard death wins");
    }

    private void HitCastleWithFirebolts(CastleKeep castle)
    {
        int before = castle.Health;
        var bolt = AddActor(new SpellProjectile());
        typeof(SpellProjectile).GetMethod("OnBodyEntered", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(bolt, new object[] { castle });
        Check(castle.Health == before - bolt.Damage, "Firebolt damages rival castle");
        typeof(SpellProjectile).GetMethod("OnBodyEntered", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(bolt, new object[] { castle });
        Check(castle.Health == before - bolt.Damage, "Consumed firebolt cannot deal duplicate damage");
        castle.ApplyDamage(1000);
    }

    private void CheckArenaIntegration()
    {
        var game = AddActor(new MagicCarpetRemastered.Scripts.Game.Game());
        var objective = game.GetNode<LevelObjective>("LevelObjective");
        var director = game.GetNode<EnemyEcosystemDirector>("EnemyEcosystemDirector");
        Check(!director.SpawnEnemyWizards, "Director cannot replace the designated rival");
        var rival = game.GetNode<EnemyEcosystemEnemy>("RivalWizard");
        Check(rival.Health > 0 && !objective.RivalWizardDefeated, "Arena begins with living rival");
        game.GetNode<CastleKeep>("EnemyCastle").ApplyDamage(1000);
        rival.ApplyDamage(1000);
        objective.Evaluate();
        Check(GetTree().Paused && Input.MouseMode == Input.MouseModeEnum.Visible, "Result freezes simulation and releases mouse");
        var hud = game.GetNode<MagicCarpetRemastered.Scripts.Ui.Hud>("Hud");
        Check(hud.ProcessMode == ProcessModeEnum.Always && game.ProcessMode == ProcessModeEnum.Pausable,
            "Result HUD runs while gameplay and new spawned children pause");
        hud._Process(1);
        Check(hud.GetNode<PanelContainer>("HudRoot/LevelResult").Visible, "Victory result panel appears");
    }

    private T AddActor<T>(T actor) where T : Node
    {
        AddChild(actor);
        actor.SetPhysicsProcess(false);
        actor.SetProcess(false);
        return actor;
    }

    private async Task CheckRestart()
    {
        // Keep this harness outside CurrentScene so it survives the actual scene reload.
        var game = GD.Load<PackedScene>("res://scenes/Game.tscn")
            .Instantiate<MagicCarpetRemastered.Scripts.Game.Game>();
        GetTree().Root.AddChild(game);
        GetTree().CurrentScene = game;
        game.GetNode<CastleKeep>("EnemyCastle").ApplyDamage(1000);
        game.GetNode<EnemyEcosystemEnemy>("RivalWizard").ApplyDamage(1000);
        game.GetNode<LevelObjective>("LevelObjective").Evaluate();
        var spawned = new EnemyEcosystemEnemy();
        game.AddChild(spawned);
        Check(!spawned.CanProcess(), "Dynamically added enemies remain frozen on result");
        var hud = game.GetNode<MagicCarpetRemastered.Scripts.Ui.Hud>("Hud");
        hud._UnhandledInput(new InputEventAction { Action = "restart_arena", Pressed = true });
        await ToSignal(GetTree(), SceneTree.SignalName.SceneChanged);
        var restarted = GetTree().CurrentScene as MagicCarpetRemastered.Scripts.Game.Game;
        Check(restarted != null && restarted != game && !GetTree().Paused, "R reloads the actual arena and unpauses");
        Check(restarted!.GetNode<LevelObjective>("LevelObjective").State == LevelObjective.LevelState.Playing,
            "Restart resets terminal objective state");
        Check(restarted.GetNode<EnemyEcosystemEnemy>("RivalWizard").Health > 0
            && !restarted.GetNode<CastleKeep>("EnemyCastle").IsDestroyed,
            "Restart restores rival wizard and castle");
        if (DisplayServer.GetName() != "headless")
        {
            Check(Input.MouseMode == Input.MouseModeEnum.Captured, "Restart recaptures flight mouse input");
        }
    }

    private void ClearActors()
    {
        foreach (Node child in GetChildren()) child.Free();
    }

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }
}

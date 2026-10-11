using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Spells;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class SpellProgressionChecks : Node3D
{
    private int _checks;
    public override void _Ready() => Callable.From(() => { _ = RunChecks(); }).CallDeferred();

    private async Task RunChecks()
    {
        try
        {
            var arena = new HeightmapArena();
            AddChild(arena);
            var player = new CarpetFlightController { Position = new Vector3(0, 20, 0) };
            AddChild(player);
            player.SetPhysicsProcess(false);
            var castle = new CastleKeep { InitialStoredMana = 30, Position = new Vector3(-20, 20, 0) };
            AddChild(castle);
            int mana = player.Mana;
            int power = player.ClaimedMana;
            Check(player.FireboltDamage == 25 && player.EffectiveAreaDamage == 32 && player.EffectiveAreaRadius == 8,
                "Base offensive spells retain original strength");
            Check(player.EffectiveShieldDuration == 4 && player.EffectiveDashSpeed == 46,
                "Base utility spells retain original strength");
            Check(!player.DiscoverSpellUpgrade("possess") && !player.DiscoverSpellUpgrade("invalid"),
                "Fixed economy binding and unknown spells are not upgradable");
            string[] ids = { "firebolt", "arcane-burst", "mana-shield", "wind-dash", "guardian" };
            for (int i = 0; i < ids.Length; i++)
            {
                var discovery = new SpellDiscovery { SpellId = ids[i], Position = new Vector3(10 + i * 6, 0, 10) };
                AddChild(discovery);
                player.GlobalPosition = discovery.GlobalPosition + new Vector3(6, 0, 0);
                await Frames(3);
                Check(!discovery.IsCollected && player.GetSpellRank(ids[i]) == 1, "Distant player cannot collect a discovery");
                player.GlobalPosition = discovery.GlobalPosition + Vector3.Up * 7;
                Check(!discovery.TryCollect(player), "Player outside the flight column cannot collect");
                player.GlobalPosition = discovery.GlobalPosition + Vector3.Up * (i % 2 == 0 ? 5 : -4);
                await Frames(3);
                Check(player.GetSpellRank(ids[i]) == 2 && !IsInstanceValid(discovery),
                    "Real physics frames collect a nearby beacon at low/high flight clearance and remove it");
                Check(!player.DiscoverSpellUpgrade(ids[i]), "Repeated discoveries cannot stack rank or effects");
            }
            Check(player.Mana == mana && player.ClaimedMana == power && castle.StoredMana == 30,
                "Spell discoveries do not change spell mana, power, or physical castle reserves");
            Check(player.PrimarySpellName == "Firebolt II" && player.SpellProgressionText.Contains("Guardian II"),
                "Selected spell and progression text expose rank II");
            Check(player.DiscoveryFeedbackMessage.Contains("II"), "Discovery has dedicated persistent feedback");
            Check(player.FireboltDamage == 35 && player.EffectiveAreaDamage == 40 && player.EffectiveAreaRadius == 10,
                "Offensive discoveries improve damage and burst radius");
            Check(player.EffectiveShieldDuration == 6 && Mathf.IsEqualApprox(player.EffectiveDashSpeed, 55.2f),
                "Utility discoveries improve shield duration and dash speed");

            player.RespawnAt(new Vector3(0, 20, 0));
            Invoke(player, "CastPrimarySpell");
            var shot = GetChildren().OfType<SpellProjectile>().Single();
            Check(shot.Damage == 35 && player.Mana == player.ManaCapacity - player.PrimarySpellManaCost,
                "Actual upgraded firebolt receives damage and retains its mana cost");
            shot.QueueFree();
            Invoke(player, "CastSummonSpell");
            var guardian = GetChildren().OfType<SummonedGuardian>().Single();
            guardian.SetPhysicsProcess(false);
            Check(guardian.Damage == 16 && guardian.LifetimeSeconds == 18,
                "Actual upgraded summon receives stronger attacks and lifetime before Ready");
            Invoke(player, "CastShieldSpell");
            Check(player.IsShielded && player.ShieldStatusText.Contains("6.0"), "Actual upgraded shield lasts six seconds");
            // Clear respawn protection before exercising upgraded shield mitigation.
            typeof(CarpetFlightController).GetField("_damageInvulnerabilityTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, 0f);
            player.ApplyDamage(20);
            Check(player.Health == 93, "Shield still mitigates real incoming damage");
            Invoke(player, "CastMobilitySpell");
            Check(new Vector2(player.Velocity.X, player.Velocity.Z).Length() > 50,
                "Actual upgraded dash launches faster than base speed");
            Invoke(player, "CastAreaSpell");
            var areaBurst = GetChildren().OfType<SpellImpactBurst>().Last(burst => burst.Name == "AreaSpellBurst");
            var burstTarget = new EnemyEcosystemEnemy
            {
                Role = EnemyEcosystemEnemy.EnemyRole.GroundBeast,
                Position = areaBurst.GlobalPosition + Vector3.Right * 9
            };
            AddChild(burstTarget);
            burstTarget.SetPhysicsProcess(false);
            // Ground beasts initialize on terrain in Ready. Explicitly position
            // this 3D radius probe afterward so first-person/chase aim defaults
            // do not silently move it out of the intended nine-unit test range.
            burstTarget.GlobalPosition = areaBurst.GlobalPosition + Vector3.Right * 9;
            Invoke(player, "CastAreaSpell");
            Check(burstTarget.Health == 2 && areaBurst.EndScale == 10,
                $"Actual upgraded Burst deals 40 damage beyond the base eight-unit radius and enlarges feedback (health {burstTarget.Health}, radius {areaBurst.EndScale}, status {player.StatusMessage})");

            typeof(CarpetFlightController).GetField("_damageInvulnerabilityTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, 0f);
            player.ApplyDamage(1000);
            var duplicate = new SpellDiscovery { SpellId = "firebolt", Position = player.GlobalPosition };
            AddChild(duplicate);
            player.GlobalPosition = duplicate.GlobalPosition;
            Check(!duplicate.TryCollect(player) && !duplicate.IsCollected, "Dead wizards cannot consume discoveries");
            player.RespawnAt(duplicate.GlobalPosition);
            Check(ids.All(id => player.GetSpellRank(id) == 2), "Wizard recovery preserves every discovered upgrade");
            Check(!duplicate.TryCollect(player) && !duplicate.IsCollected, "Max-rank discovery remains unconsumed");
            var fresh = new CarpetFlightController { Position = new Vector3(-60, 20, -60) };
            AddChild(fresh);
            fresh.SetPhysicsProcess(false);
            Check(ids.All(id => fresh.GetSpellRank(id) == 1), "New arena player has fresh progression, no static carryover");

            foreach (Node child in GetChildren()) child.Free();
            var game = GD.Load<PackedScene>("res://scenes/Game.tscn")
                .Instantiate<MagicCarpetRemastered.Scripts.Game.Game>();
            GetTree().Root.AddChild(game);
            GetTree().CurrentScene = game;
            var arenaPlayer = game.GetNode<CarpetFlightController>("PlayerCarpet");
            arenaPlayer.SetPhysicsProcess(false);
            var beacons = game.GetChildren().OfType<SpellDiscovery>().ToArray();
            Check(beacons.Length == 5 && beacons.Select(beacon => beacon.SpellId).Distinct().Count() == 5,
                "Actual arena seeds one exploration upgrade for each combat/utility spell");
            var beacon = beacons[0];
            var liveArena = game.GetNode<HeightmapArena>("HeightmapArena");
            float previousHeight = beacon.GlobalPosition.Y;
            liveArena.RaiseTerrain(beacon.GlobalPosition, 8, 5);
            await Frames(3);
            Check(beacon.GlobalPosition.Y > previousHeight + 1,
                "Discovery collection column and landmark follow live terrain deformation");
            arenaPlayer.GlobalPosition = beacon.GlobalPosition;
            GetTree().Paused = true;
            ProcessMode = ProcessModeEnum.Always;
            await Frames(3);
            Check(!beacon.IsCollected && arenaPlayer.GetSpellRank(beacon.SpellId) == 1,
                "Paused arena cannot grant discovery progression");
            GetTree().Paused = false;
            await Frames(3);
            Check(arenaPlayer.GetSpellRank("firebolt") == 2 && game.GetChildren().OfType<SpellDiscovery>().Count() == 4,
                "Resumed arena discovers and consumes a beacon");
            Callable.From(() => game._UnhandledInput(new InputEventAction { Action = "restart_arena", Pressed = true })).CallDeferred();
            await ToSignal(GetTree(), SceneTree.SignalName.SceneChanged);
            var restarted = (MagicCarpetRemastered.Scripts.Game.Game)GetTree().CurrentScene;
            Check(restarted.GetChildren().OfType<SpellDiscovery>().Count() == 5
                && ids.All(id => restarted.GetNode<CarpetFlightController>("PlayerCarpet").GetSpellRank(id) == 1),
                "Actual R restart restores all beacons and resets spell progression");

            GD.Print($"Spell progression checks passed: {_checks}");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }

    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private static void Invoke(CarpetFlightController player, string method) =>
        typeof(CarpetFlightController).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(player, null);

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }
}

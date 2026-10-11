using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class EquipmentProgressionChecks : Node3D
{
    private int _checks;
    public override void _Ready() => Callable.From(() => { _ = RunChecks(); }).CallDeferred();

    private async Task RunChecks()
    {
        try
        {
            var arena = new HeightmapArena();
            AddChild(arena);
            var player = new ShipFlightController { Position = new Vector3(0, 20, 0) };
            AddChild(player);
            player.SetPhysicsProcess(false);
            var hub = new FactionBase { InitialStoredAntimatter = 30, Position = new Vector3(-20, 20, 0) };
            AddChild(hub);
            int antimatter = player.Energy;
            int power = player.ClaimedAntimatter;
            Check(player.PlasmaBoltDamage == 25 && player.EffectiveAreaDamage == 32 && player.EffectiveAreaRadius == 8,
                "Base offensive equipment retain original strength");
            Check(player.EffectiveShieldDuration == 4 && player.EffectiveDashSpeed == 46,
                "Base utility equipment retain original strength");
            Check(!player.DiscoverEquipmentUpgrade("claim") && !player.DiscoverEquipmentUpgrade("invalid"),
                "Fixed economy binding and unknown equipment are not upgradable");
            string[] ids = { "plasma-bolt", "pulse-burst", "energy-shield", "dash", "guardian-drone" };
            for (int i = 0; i < ids.Length; i++)
            {
                var discovery = new EquipmentDiscovery { EquipmentId = ids[i], Position = new Vector3(10 + i * 6, 0, 10) };
                AddChild(discovery);
                player.GlobalPosition = discovery.GlobalPosition + new Vector3(6, 0, 0);
                await Frames(3);
                Check(!discovery.IsCollected && player.GetEquipmentRank(ids[i]) == 1, "Distant player cannot collect a discovery");
                player.GlobalPosition = discovery.GlobalPosition + Vector3.Up * 7;
                Check(!discovery.TryCollect(player), "Player outside the flight column cannot collect");
                player.GlobalPosition = discovery.GlobalPosition + Vector3.Up * (i % 2 == 0 ? 5 : -4);
                await Frames(3);
                Check(player.GetEquipmentRank(ids[i]) == 2 && !IsInstanceValid(discovery),
                    "Real physics frames collect a nearby beacon at low/high flight clearance and remove it");
                Check(!player.DiscoverEquipmentUpgrade(ids[i]), "Repeated discoveries cannot stack rank or effects");
            }
            Check(player.Energy == antimatter && player.ClaimedAntimatter == power && hub.StoredAntimatter == 30,
                "Equipment discoveries do not change equipment antimatter, power, or physical hub reserves");
            Check(player.PrimaryEquipmentName == "Plasma Bolt II" && player.EquipmentProgressionText.Contains("Guardian Drone II"),
                "Selected equipment and progression text expose rank II");
            Check(player.DiscoveryFeedbackMessage.Contains("II"), "Discovery has dedicated persistent feedback");
            Check(player.PlasmaBoltDamage == 35 && player.EffectiveAreaDamage == 40 && player.EffectiveAreaRadius == 10,
                "Offensive discoveries improve damage and burst radius");
            Check(player.EffectiveShieldDuration == 6 && Mathf.IsEqualApprox(player.EffectiveDashSpeed, 55.2f),
                "Utility discoveries improve shield duration and dash speed");

            player.RespawnAt(new Vector3(0, 20, 0));
            Invoke(player, "CastPrimaryEquipment");
            var shot = GetChildren().OfType<WeaponProjectile>().Single();
            Check(shot.Damage == 35 && player.Energy == player.EnergyCapacity - player.PrimaryEquipmentEnergyCost,
                "Actual upgraded plasmaBolt receives damage and retains its antimatter cost");
            shot.QueueFree();
            Invoke(player, "CastSummonEquipment");
            var guardian = GetChildren().OfType<GuardianDrone>().Single();
            guardian.SetPhysicsProcess(false);
            Check(guardian.Damage == 16 && guardian.LifetimeSeconds == 18,
                "Actual upgraded summon receives stronger attacks and lifetime before Ready");
            Invoke(player, "CastShieldEquipment");
            Check(player.IsShielded && player.ShieldStatusText.Contains("6.0"), "Actual upgraded shield lasts six seconds");
            // Clear respawn protection before exercising upgraded shield mitigation.
            typeof(ShipFlightController).GetField("_damageInvulnerabilityTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, 0f);
            player.ApplyDamage(20);
            Check(player.Health == 93, "Shield still mitigates real incoming damage");
            Invoke(player, "CastMobilityEquipment");
            Check(new Vector2(player.Velocity.X, player.Velocity.Z).Length() > 50,
                "Actual upgraded dash launches faster than base speed");
            Invoke(player, "CastAreaEquipment");
            var areaBurst = GetChildren().OfType<EquipmentImpactBurst>().Last(burst => burst.Name == "AreaEquipmentBurst");
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
            Invoke(player, "CastAreaEquipment");
            Check(burstTarget.Health == 2 && areaBurst.EndScale == 10,
                $"Actual upgraded Burst deals 40 damage beyond the base eight-unit radius and enlarges feedback (health {burstTarget.Health}, radius {areaBurst.EndScale}, status {player.StatusMessage})");

            typeof(ShipFlightController).GetField("_damageInvulnerabilityTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, 0f);
            player.ApplyDamage(1000);
            var duplicate = new EquipmentDiscovery { EquipmentId = "plasma-bolt", Position = player.GlobalPosition };
            AddChild(duplicate);
            player.GlobalPosition = duplicate.GlobalPosition;
            Check(!duplicate.TryCollect(player) && !duplicate.IsCollected, "Dead pilots cannot consume discoveries");
            player.RespawnAt(duplicate.GlobalPosition);
            Check(ids.All(id => player.GetEquipmentRank(id) == 2), "Pilot recovery preserves every discovered upgrade");
            Check(!duplicate.TryCollect(player) && !duplicate.IsCollected, "Max-rank discovery remains unconsumed");
            var fresh = new ShipFlightController { Position = new Vector3(-60, 20, -60) };
            AddChild(fresh);
            fresh.SetPhysicsProcess(false);
            Check(ids.All(id => fresh.GetEquipmentRank(id) == 1), "New arena player has fresh progression, no static carryover");

            foreach (Node child in GetChildren()) child.Free();
            var game = GD.Load<PackedScene>("res://scenes/Game.tscn")
                .Instantiate<MagicCarpetRemastered.Scripts.Game.Game>();
            GetTree().Root.AddChild(game);
            GetTree().CurrentScene = game;
            var arenaPlayer = game.GetNode<ShipFlightController>("PlayerShip");
            arenaPlayer.SetPhysicsProcess(false);
            var beacons = game.GetChildren().OfType<EquipmentDiscovery>().ToArray();
            Check(beacons.Length == 5 && beacons.Select(beacon => beacon.EquipmentId).Distinct().Count() == 5,
                "Actual arena seeds one exploration upgrade for each combat/utility equipment");
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
            Check(!beacon.IsCollected && arenaPlayer.GetEquipmentRank(beacon.EquipmentId) == 1,
                "Paused arena cannot grant discovery progression");
            GetTree().Paused = false;
            await Frames(3);
            Check(arenaPlayer.GetEquipmentRank("plasma-bolt") == 2 && game.GetChildren().OfType<EquipmentDiscovery>().Count() == 4,
                "Resumed arena discovers and consumes a beacon");
            Callable.From(() => game._UnhandledInput(new InputEventAction { Action = "restart_arena", Pressed = true })).CallDeferred();
            await ToSignal(GetTree(), SceneTree.SignalName.SceneChanged);
            var restarted = (MagicCarpetRemastered.Scripts.Game.Game)GetTree().CurrentScene;
            Check(restarted.GetChildren().OfType<EquipmentDiscovery>().Count() == 5
                && ids.All(id => restarted.GetNode<ShipFlightController>("PlayerShip").GetEquipmentRank(id) == 1),
                "Actual R restart restores all beacons and resets equipment progression");

            GD.Print($"Equipment progression checks passed: {_checks}");
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

    private static void Invoke(ShipFlightController player, string method) =>
        typeof(ShipFlightController).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(player, null);

    private void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }
}

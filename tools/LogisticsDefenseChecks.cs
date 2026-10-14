using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class LogisticsDefenseChecks : Node3D
{
    private int _checks;
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();
    private void Check(bool value, string message)
    { _checks++; if (!value) throw new InvalidOperationException(message); }
    private async Task Frames(int count)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    private static void Scoop(CollectorDrone drone, AntimatterPickup pickup) => typeof(CollectorDrone)
        .GetMethod("Scoop", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(drone, new object[] { pickup });

    private async Task Run()
    {
        try
        {
            var room = new Node3D(); AddChild(room);
            var home = new FactionBase { InitialStoredAntimatter = 0 }; room.AddChild(home); home.SetPhysicsProcess(false);
            home.GetNode<BaseDefense>("BaseDefense").SetPhysicsProcess(false);
            var drone = new CollectorDrone { CarryCapacity = 30 }; room.AddChild(drone); drone.SetPhysicsProcess(false);
            var tracker = new WorldAntimatterTracker(); room.AddChild(tracker); tracker.SetPhysicsProcess(false);
            for (int i = 0; i < 12; i++)
            {
                var ball = new AntimatterPickup { AntimatterAmount = 1, Ownership = AntimatterOwnership.Player };
                room.AddChild(ball); Scoop(drone, ball);
            }
            Check(drone.CarriedAntimatter == 12, "Collector takes more than three balls without returning at an object cap");
            var large = new AntimatterPickup { AntimatterAmount = 50, Ownership = AntimatterOwnership.Player };
            room.AddChild(large); large.SetPhysicsProcess(false); Scoop(drone, large);
            Check(drone.CarriedAntimatter == 30 && large.AntimatterAmount == 32 && large.IsAvailable,
                "Partial scoop fills exact AM capacity and leaves a claimable remainder");
            Scoop(drone, large); tracker.Refresh();
            Check(drone.CarriedAntimatter == 30 && tracker.TotalAntimatter == 62, "Full collector rejects repeat pickup and internal cargo is counted exactly once");
            drone.ApplyDamage(1000); tracker.Refresh();
            Check(drone.CarriedAntimatter == 0 && tracker.TotalAntimatter == 62 && tracker.NeutralLooseAntimatter == 30,
                "Destroyed internal cargo spills exactly and becomes neutral");
            room.QueueFree(); await Frames(3);

            room = new Node3D(); AddChild(room);
            home = new FactionBase { InitialStoredAntimatter = 0 }; room.AddChild(home); home.SetPhysicsProcess(false);
            var fleet = new HomeCollectorFleet { Home = home }; room.AddChild(fleet); fleet.SetPhysicsProcess(false);
            foreach (var collector in room.GetChildren().OfType<CollectorDrone>()) collector.SetPhysicsProcess(false);
            Check(room.GetChildren().OfType<CollectorDrone>().Count() == 1, "Tier I creates one home collector");
            for (int tier = 2; tier <= 5; tier++)
            {
                Check(home.TryCompleteTierUpgrade(tier), "Fleet fixture advances a real base tier");
                fleet._PhysicsProcess(1);
                foreach (var collector in room.GetChildren().OfType<CollectorDrone>()) collector.SetPhysicsProcess(false);
                Check(room.GetChildren().OfType<CollectorDrone>().Count() == tier, $"Tier {tier} adds exactly one collector");
            }
            var collectors = room.GetChildren().OfType<CollectorDrone>().ToArray();
            collectors[0].ApplyDamage(1000); await Frames(2); fleet._PhysicsProcess(1);
            Check(room.GetChildren().OfType<CollectorDrone>().Count() == 4, "Destroyed home collector waits before replacement");
            fleet._PhysicsProcess(14);
            foreach (var collector in room.GetChildren().OfType<CollectorDrone>()) collector.SetPhysicsProcess(false);
            Check(room.GetChildren().OfType<CollectorDrone>().Count() == 5, "Destroyed home collector is replaced after fifteen seconds");
            var retiring = room.GetChildren().OfType<CollectorDrone>().Last();
            var retirementCargo = new AntimatterPickup { AntimatterAmount = 7, Ownership = AntimatterOwnership.Player };
            room.AddChild(retirementCargo); Scoop(retiring, retirementCargo);
            retiring.GlobalPosition = new Vector3(20, 5, 0);
            home.ApplyDamage(home.Health - 400); fleet._PhysicsProcess(1);
            foreach (var collector in room.GetChildren().OfType<CollectorDrone>()) collector._PhysicsProcess(0);
            await Frames(2); fleet._PhysicsProcess(0);
            Check(room.GetChildren().OfType<CollectorDrone>().Count() == 2 && retiring.CarriedAntimatter == 7,
                "Tier loss retires empty extras but preserves a loaded collector");
            for (int i = 0; i < 300 && !retiring.IsQueuedForDeletion(); i++) retiring._PhysicsProcess(1f / 60);
            await Frames(2);
            Check(home.StoredAntimatter == 7 && room.GetChildren().OfType<CollectorDrone>().Count() == 1,
                "Excess collector delivers its exact cargo before retiring");
            room.QueueFree(); await Frames(3);

            room = new Node3D(); AddChild(room);
            home = new FactionBase(); room.AddChild(home); home.SetPhysicsProcess(false);
            var defense = home.GetNode<BaseDefense>("BaseDefense"); defense.SetPhysicsProcess(false);
            var enemy = new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.GroundBeast, Position = new Vector3(6, 1, 0) };
            room.AddChild(enemy); enemy.SetPhysicsProcess(false);
            await Frames(2);
            int hp = enemy.Health; defense._PhysicsProcess(1);
            Check(enemy.Health == hp, "Basic base defense does not hunt peaceful neutrals");
            enemy._PhysicsProcess(0);
            Check(home.Health == home.MaxHealth - enemy.ContactDamage, "Ordinary beast damages a base on contact");
            int storage = home.StoredAntimatter;
            defense._PhysicsProcess(0);
            Check(enemy.Health == hp - defense.Damage && home.StoredAntimatter == storage,
                "Basic pulse defense retaliates without spending stored AM");
            defense._PhysicsProcess(0);
            Check(enemy.Health == hp - defense.Damage, "Pulse cooldown prevents repeated zero-time damage");
            enemy.GlobalPosition = new Vector3(40, 1, 0); defense._PhysicsProcess(2);
            Check(enemy.Health == hp - defense.Damage, "Defense stops firing outside its perimeter");
            var rival = new ShipFlightController { IsPlayerControlled = false, Position = new Vector3(10, 2, 0), DamageInvulnerabilitySeconds = 0 };
            room.AddChild(rival); rival.SetPhysicsProcess(false); await Frames(2);
            int rivalHp = rival.Health; home.ApplyDamage(1, rival); defense._PhysicsProcess(2);
            Check(rival.Health == rivalHp - defense.Damage, "Basic weapon retaliates against rival ships too");
            var wall = new StaticBody3D { Position = new Vector3(5, 3, 0) };
            wall.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(2, 8, 6) } });
            room.AddChild(wall); await Frames(2);
            home.ApplyDamage(1, rival); defense._PhysicsProcess(2);
            Check(rival.Health == rivalHp - defense.Damage, "Base pulse cannot fire through an obstruction");
            wall.QueueFree(); await Frames(2);
            GetTree().Paused = true; home.ApplyDamage(1, rival); defense._PhysicsProcess(10);
            Check(rival.Health == rivalHp - defense.Damage, "Pause freezes base weapons"); GetTree().Paused = false;
            home.ApplyDamage(10000); defense._PhysicsProcess(10);
            Check(rival.Health == rivalHp - defense.Damage, "Destroyed bases cannot fire");
            room.QueueFree(); await Frames(3);
            GD.Print($"Logistics/defense checks passed: {_checks}"); GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}

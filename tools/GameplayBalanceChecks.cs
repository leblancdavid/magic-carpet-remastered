using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class GameplayBalanceChecks : Node3D
{
    private int _checks;
    private Node3D _room = null!;
    private FactionBase _home = null!;
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();
    private void Check(bool pass, string message) { _checks++; if (!pass) throw new InvalidOperationException(message); }
    private async Task Frames(int n = 2) { for (int i = 0; i < n; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    private T Add<T>(T node) where T : Node { _room.AddChild(node); node.SetPhysicsProcess(false); return node; }
    private void Research(string id, int rank = 1)
    {
        _home.DepositAntimatter(10000);
        Check(_home.Construction.TryStart(UpgradeCatalog.ProjectId(id, rank), out string reason), $"Research {id}/{rank}: {reason}");
        _home.Construction.Advance(500);
    }
    private async Task Reset()
    {
        if (IsInstanceValid(_room)) { _room.QueueFree(); await Frames(3); }
        _room = new Node3D(); AddChild(_room);
        var floor = new StaticBody3D { Position = new Vector3(0, -1, 0) };
        floor.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(400, 2, 400) } }); Add(floor);
        _home = Add(new FactionBase { IsPlayerBase = false, InitialStoredAntimatter = 0 });
        _home.Fleet.SetPhysicsProcess(false); _home.GetNode<BaseDefense>("BaseDefense").SetPhysicsProcess(false);
    }
    private async Task Run()
    {
        try
        {
            await Reset();
            Check(_home.UpgradeCostForTier(2) == 150, "First advancement is half nominal capacity");
            _home.Tier1StorageCapacity = 301;
            Check(_home.UpgradeCostForTier(2) == 151, "Odd custom capacities round advancement up");
            _home.ApplyDamage(100);
            Check(_home.UpgradeCostForTier(2) == 151, "Damage cannot discount advancement");
            await Reset(); _home.TryCompleteTierUpgrade(2);
            Research("harvester-drone");
            _home.WithdrawAntimatter(_home.StoredAntimatter);
            long spent = _home.SpentAntimatter;
            var prey = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.GroundBeast, IsHeavy = true,
                Position = new Vector3(24, 1.3f, 0), HabitatId = "wild-check", NeutralAntimatterValue = 72 });
            var second = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.FlyingSwarm,
                Position = new Vector3(-24, 5, 0), HabitatId = "wild-second", NeutralAntimatterValue = 6 });
            var guards = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.FlyingSwarm,
                Position = new Vector3(10, 5, 0), HabitatId = "route-check", NeutralAntimatterValue = 0 });
            _home.Fleet._PhysicsProcess(9.9);
            Check(!_room.GetChildren().OfType<BaseTacticalDrone>().Any(), "Harvester waits for ordinary free production clock");
            _home.Fleet._PhysicsProcess(0.11);
            var harvester = _room.GetChildren().OfType<BaseTacticalDrone>().Single(); harvester.SetPhysicsProcess(false);
            Check(harvester.Role == BaseTacticalDrone.DroneRole.Harvester && harvester.Target != guards && harvester.Target != null
                && _home.SpentAntimatter == spent, "Harvester automatically chooses a wild mob without another AM payment");
            Check(!BaseTacticalDrone.IsHarvestable(guards), "Captured-route guards are excluded from farming");
            harvester.Target = prey; harvester.GlobalPosition = new Vector3(24, 5, 0);
            await Frames();
            for (int i = 0; i < 22; i++) harvester._PhysicsProcess(1);
            Check(prey.Health == 0 && harvester.AwaitingHarvest, "Bronze harvester kills a real heavy and waits for staggered salvage");
            _home.Fleet._PhysicsProcess(1);
            Check(harvester.Target == prey, "Fleet preserves the cleanup assignment after its target dies");
            var tracker = Add(new WorldAntimatterTracker()); tracker.Refresh();
            Check(tracker.TotalAntimatter == 78, "Wild deaths preserve intrinsic AM including pending drops");
            for (int i = 0; i < 420; i++) { await Frames(1); harvester._PhysicsProcess(1.0 / 60); }
            var drops = _room.GetChildren().OfType<AntimatterPickup>().Where(d => !d.IsQueuedForDeletion()).ToArray();
            Check(drops.Sum(d => d.AntimatterAmount) == 72 && drops.All(d => d.Ownership == AntimatterOwnership.Enemy),
                "Harvester claims every real heavy-death fragment without inventing income");
            var collector = Add(new CollectorDrone { UsePlayerAntimatter = false, Position = new Vector3(24, 5, 0), MaxSeekRadius = 150 });
            Check(collector.EffectiveCarryCapacity == 80, "Basic collector can carry a whole 72-A heavy");
            // Let ordinary search/scoop/return/docking run, with physical spheres active.
            for (int i = 0; i < 1500 && _home.StoredAntimatter < 72; i++)
            { collector._PhysicsProcess(1.0 / 60); await Frames(1); }
            tracker.Refresh();
            Check(_home.StoredAntimatter == 72 && collector.CarriedAntimatter == 0 && tracker.TotalAntimatter == 78,
                "Actual harvester kill → claim → collector haul → bank conserves all AM");
            for (int rank = 1; rank <= 3; rank++)
            { Research("cargo-compression", rank); Check(collector.EffectiveCarryCapacity == 80 + rank * 40, "Cargo stars add useful pack capacity"); }
            for (int rank = 2; rank <= 3; rank++) Research("harvester-drone", rank);
            ResearchExpansion();
            Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.FlyingSwarm,
                Position = new Vector3(24, 5, 24), HabitatId = "wild-third", NeutralAntimatterValue = 0 });
            harvester._PhysicsProcess(2); // Finish delayed-drop cleanup before assigning the next habitat.
            _home.Fleet._PhysicsProcess(10);
            foreach (var unit in _room.GetChildren().OfType<BaseTacticalDrone>()) unit.SetPhysicsProcess(false);
            var harvesters = _room.GetChildren().OfType<BaseTacticalDrone>().ToArray();
            Check(harvesters.Length == 2 && harvesters.All(h => h.Health == 125), "Gold updates surviving and replacement harvesters");
            Check(harvesters.Select(h => ((EnemyEcosystemEnemy)h.Target!).HabitatId).Distinct().Count() == 2,
                "Additional Harvesters reserve distinct habitats rather than different mobs in one pack");
            _home.ApplyDamage(400); int before = second.Health;
            harvester.Target = second; harvester.GlobalPosition = second.GlobalPosition; harvester._PhysicsProcess(10);
            Check(second.Health == before, "Harvester suspends below tier II");

            await Reset();
            var combatPrey = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.GroundBeast,
                Position = new Vector3(12, 5, 0), NeutralAntimatterValue = 0 });
            var attacker = Add(new BaseTacticalDrone { Home = _home, Role = BaseTacticalDrone.DroneRole.Attacker,
                Automatic = true, Target = combatPrey, Position = new Vector3(0, 5, 0) }); await Frames();
            Check(attacker.Health == 75, "Bronze attacker has enough baseline durability");
            attacker._PhysicsProcess(0);
            Check(combatPrey.Health == 40, "Bronze attacker fires ten actual damage");
            attacker._PhysicsProcess(0.5);
            Check(combatPrey.Health == 40, "Attack cooldown prevents extra half-second shots");
            attacker._PhysicsProcess(0.5);
            Check(combatPrey.Health == 30, "Attack drone delivers ten damage per second");
            attacker.ApplyResearchRank(3); attacker._PhysicsProcess(1);
            Check(attacker.Health == 125 && combatPrey.Health == 12, "Gold increases surviving attacker durability and firing damage");
            var siegeTarget = Add(new FactionBase { Position = new Vector3(0, 0, 60) });
            siegeTarget.Fleet.SetPhysicsProcess(false); siegeTarget.GetNode<BaseDefense>("BaseDefense").SetPhysicsProcess(false);
            attacker.Target = siegeTarget; attacker.GlobalPosition = new Vector3(0, 5, 52); await Frames();
            attacker._PhysicsProcess(1);
            Check(siegeTarget.Health == 397, "Gold attacker applies three structural damage with line of sight above the ground");

            await Reset(); _home.TryCompleteTierUpgrade(2);
            var ship = Add(new ShipFlightController { IsPlayerControlled = false, Position = new Vector3(0, 10, 0) });
            Research("scatter-cannon"); Research("seeker-missile"); Research("pulse-burst");
            _home.DepositAntimatter(10000); ship.RespawnAt(new Vector3(0, 10, 0));
            Check(ship.TryInstallEquipment("scatter-cannon", 1, out _) && ship.TryInstallEquipment("seeker-missile", 2, out _)
                && ship.TryInstallEquipment("pulse-burst", 3, out _), "New weapons research and install through ordinary slots");
            ship.SetAutonomousControls(Vector3.Zero, Vector3.Forward);
            var close = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.GroundBeast, IsHeavy = true,
                Position = ship.AimOrigin + Vector3.Forward * 6, NeutralAntimatterValue = 0 });
            await Frames(); int hp = close.Health, energy = ship.Energy;
            Check(ship.TryActivateEquipmentSlot(1) && ship.Energy == energy - 5, "Scatter spends five current energy");
            Check(!ship.TryActivateEquipmentSlot(1) && ship.Energy == energy - 5, "Scatter cooldown rejects duplicate activation");
            await Frames(15);
            Check(close.Health <= hp - 48, "Close scatter connects most pellets against a heavy");
            close.QueueFree(); foreach (var pellet in _room.GetChildren().OfType<WeaponProjectile>()) pellet.QueueFree(); await Frames();
            var target = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.GroundBeast, IsHeavy = true,
                Position = ship.AimOrigin + Vector3.Forward * 25, NeutralAntimatterValue = 0 }); await Frames();
            hp = target.Health; energy = ship.Energy;
            Check(ship.TryActivateEquipmentSlot(2) && ship.Energy == energy - 7, "Seeker spends seven energy");
            var missile = _room.GetChildren().OfType<SeekerMissile>().Single();
            Check(missile.Target == target, "Seeker locks the visible aimed mobile enemy");
            target.GlobalPosition += Vector3.Right * 2; await Frames(60);
            Check(target.Health == hp - 42, "Guided missile turns to hit a displaced target with actual damage");
            ship._PhysicsProcess(2); ship.GlobalPosition = new Vector3(0, 10, 0); ship.SetAutonomousControls(Vector3.Zero, Vector3.Forward);
            Check(ship.TryActivateEquipmentSlot(2), "Seeker becomes available after cooldown");
            missile = _room.GetChildren().OfType<SeekerMissile>().Single(m => !m.IsQueuedForDeletion()); missile.SetPhysicsProcess(false);
            Check(!FactionCombat.CanDamage(missile, ship.Ownership) && FactionCombat.CanDamage(missile, AntimatterOwnership.Player), "Missiles obey faction interception rules");
            FactionCombat.Damage(missile, 10, null);
            Check(missile.Health == 0 && missile.IsQueuedForDeletion(), "An intercepted missile dies before delivering damage"); await Frames();
            var wall = Add(new StaticBody3D { Position = ship.AimOrigin + Vector3.Forward * 10 });
            wall.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(12, 12, 2) } }); await Frames();
            var blocked = Add(new SeekerMissile { OwnerShip = ship, Ownership = ship.Ownership }); blocked.GlobalPosition = ship.AimOrigin;
            blocked.Launch(Vector3.Forward);
            Check(blocked.Target == null, "Seeker cannot acquire a target through a wall"); blocked.QueueFree(); wall.QueueFree(); await Frames();
            target.GlobalPosition = ship.AimOrigin + Vector3.Forward * 20;
            var packmate = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.GroundBeast,
                Position = target.GlobalPosition + Vector3.Right * 4, NeutralAntimatterValue = 0 }); await Frames();
            hp = target.Health; int otherHp = packmate.Health; energy = ship.Energy;
            Check(ship.TryActivateEquipmentSlot(3) && ship.Energy == energy - 5, "Burst has an affordable five-energy pack-clearing cost");
            Check(target.Health == hp - 32 && packmate.Health == otherHp - 32, "Aiming Burst at a mob hits its airborne cluster instead of only terrain");
            GetTree().Paused = true; energy = ship.Energy;
            Check(!ship.TryActivateEquipmentSlot(1) && !ship.TryActivateEquipmentSlot(2) && ship.Energy == energy, "Pause blocks new weapon activation");
            GetTree().Paused = false;
            _room.QueueFree(); await Frames(3);
            GD.Print($"Gameplay balance checks passed: {_checks}"); GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    private void ResearchExpansion()
    {
        _home.DepositAntimatter(10000);
        Check(_home.Construction.TryStart(AutomaticDroneFleet.ExpansionId("harvester-drone", 2), out _), "Gold unlocks paid Harvester expansion");
        _home.Construction.Advance(500);
    }
}

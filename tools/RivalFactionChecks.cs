using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.Game;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Ui;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class RivalFactionChecks : Node3D
{
    private int _checks;
    private void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private async Task Frames(int count) { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    private static object? Invoke(object obj, string name, params object[] args)
        => obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(obj, args);
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();

    private async Task Run()
    {
        double originalScale = Engine.TimeScale;
        try
        {
            foreach (bool spherical in new[] { false, true })
            {
                var room = new Node3D(); AddChild(room);
                var arena = new HeightmapArena { IsSpherical = spherical, PlanetRadius = 500 }; room.AddChild(arena);
                Vector3 origin = spherical ? arena.PlanetCenter + Vector3.Down * 500 : Vector3.Zero;
                Vector3 Point(float x, float h = 6) => arena.OnSurface(origin + Vector3.Right * x, h);
                var home = new FactionBase { Position = Point(-80, 2) }; room.AddChild(home); home.SetPhysicsProcess(false);
                var enemy = new FactionBase { IsPlayerBase = false, Position = Point(0, 2) }; room.AddChild(enemy); enemy.SetPhysicsProcess(false);
                var player = new ShipFlightController { Position = Point(-70), DamageInvulnerabilitySeconds = 0 }; room.AddChild(player); player.SetPhysicsProcess(false);
                Camera3D? camera = GetViewport().GetCamera3D(); var mouse = Input.MouseMode;
                var rival = new ShipFlightController { IsPlayerControlled = false, Position = Point(10), DamageInvulnerabilitySeconds = 0 };
                room.AddChild(rival); rival.SetPhysicsProcess(false);
                var ai = new RivalPilotController(); rival.AddChild(ai); ai.SetPhysicsProcess(false);
                Check(GetViewport().GetCamera3D() == camera && Input.MouseMode == mouse && rival.GetChildren().OfType<Camera3D>().Count() == 0
                    && !rival.HasNode("CameraPivot"), "AI creation cannot own camera or cursor");
                Check(GetTree().GetNodesInGroup("player").Count == 1 && GetTree().GetNodesInGroup("pilots").Count == 2,
                    "Human UI group stays exclusive while both ships are pilots");
                Check(rival.Health == player.Health && rival.MoveSpeed == player.MoveSpeed && rival.Energy == 30
                    && rival.EnergyCapacity == 30 && rival.EnergyRechargePerSecond == player.EnergyRechargePerSecond,
                    "Starting health, flight and storage-backed energy match the player");
                Check(rival.EquipmentSlots[0] == "plasma-bolt" && rival.EquipmentSlots.Skip(1).All(id => id == null), "AI starts with the same one-item loadout");
                enemy.DepositAntimatter(20);
                Check(rival.EnergyCapacity == 50 && rival.Energy == 30 && player.EnergyCapacity == 30, "Faction deliveries update only their energy limit without refill");
                Input.ActionPress("move_forward"); Input.ActionPress("activate_primary"); Input.ActionPress("activate_claim");
                rival.SetAutonomousControls(Vector3.Zero, -rival.GlobalBasis.Z); rival._PhysicsProcess(0);
                rival._UnhandledInput(new InputEventKey { PhysicalKeycode = Key.Shift, Pressed = true });
                Check(rival.Speed < 0.01f && !rival.IsBoosting && !room.GetChildren().OfType<WeaponProjectile>().Any(), "AI ignores human polling and key events");
                Input.ActionRelease("move_forward"); Input.ActionRelease("activate_primary"); Input.ActionRelease("activate_claim");
                int energy = rival.Energy;
                rival.SetAutonomousControls(Vector3.Zero, player.GlobalPosition - rival.AimOrigin);
                Check(rival.TryActivateEquipmentSlot(0) && rival.Energy == energy - player.PrimaryEquipmentEnergyCost, "AI weapon pays the same current-energy cost");
                Check(!rival.TryActivateEquipmentSlot(0) && rival.Energy == energy - 5, "AI cannot bypass primary cooldown");
                var shot = room.GetChildren().OfType<WeaponProjectile>().Single(); shot.SetPhysicsProcess(false);
                int enemyHp = enemy.Health; Invoke(shot, "OnBodyEntered", enemy);
                Check(enemy.Health == enemyHp, "AI projectile cannot damage its own base"); shot.Free();
                rival._PhysicsProcess(1); energy = rival.Energy;
                Check(rival.TryActivateEquipmentSlot(0), "Cooldown expiry permits the next paid shot");
                shot = room.GetChildren().OfType<WeaponProjectile>().Single(); shot.SetPhysicsProcess(false); int hp = player.Health;
                Invoke(shot, "OnBodyEntered", player);
                Check(player.Health == hp - rival.PlasmaBoltDamage && rival.Energy == energy - 5, "Shared rival plasma damages the human ship"); shot.Free();
                GetTree().Paused = true; energy = rival.Energy;
                Check(!rival.TryActivateEquipmentSlot(0) && !rival.TryActivateClaim() && !rival.TryActivateBooster() && rival.Energy == energy, "Pause rejects all AI activations without payment");
                GetTree().Paused = false;
                rival.GameplayInputBlocked = true;
                Check(!rival.TryActivateClaim() && !rival.TryActivateBooster(), "The shared input gate also applies to autonomous requests"); rival.GameplayInputBlocked = false;
                rival._PhysicsProcess(100);
                var tracker = new WorldAntimatterTracker(); room.AddChild(tracker); tracker.SetProcess(false); tracker.Refresh(); int total = tracker.TotalAntimatter;
                rival.TryActivateBooster(); tracker.Refresh();
                Check(tracker.TotalAntimatter == total && rival.Energy == rival.EnergyCapacity - rival.BoosterEnergyCost,
                    "Booster spends energy, not physical stored antimatter");
                rival._PhysicsProcess(3);

                // Real mixed Claim and collector delivery; no direct absorption.
                rival.GlobalPosition = Point(10); rival.Velocity = Vector3.Zero;
                var orb = new AntimatterPickup { Ownership = AntimatterOwnership.Player, AntimatterAmount = 9, Position = Point(30, 1) };
                room.AddChild(orb); orb.Freeze = true; orb.SetPhysicsProcess(false);
                rival.SetAutonomousControls(Vector3.Zero, orb.GlobalPosition - rival.AimOrigin);
                Check(rival.TryActivateClaim(), "Shared AI Claim is a paid bolt");
                await Frames(45);
                Check(orb.Ownership == AntimatterOwnership.Enemy && orb.AntimatterAmount == 9 && orb.IsAvailable,
                    "Actual Claim steals ownership but cannot absorb antimatter into the ship");
                var matter = new MatterPickup { Ownership = AntimatterOwnership.Player, MatterAmount = 10, Position = Point(25, 0) };
                room.AddChild(matter); matter.SetPhysicsProcess(false);
                rival._PhysicsProcess(1); rival.GlobalPosition = Point(10); rival.Velocity = Vector3.Zero;
                rival.SetAutonomousControls(Vector3.Zero, matter.GlobalPosition - rival.AimOrigin); rival.TryActivateClaim();
                await Frames(45);
                Check(matter.Ownership == AntimatterOwnership.Enemy && matter.MatterAmount == 10, "Same Claim bolt handles rival material crates");
                int stored = enemy.StoredAntimatter, storedMatter = enemy.StoredMatter;
                var drone = new CollectorDrone { UsePlayerAntimatter = false, Position = Point(25, 5), MaxSeekRadius = 100 };
                room.AddChild(drone);
                await Frames(480); drone.SetPhysicsProcess(false);
                Check(enemy.StoredAntimatter == stored + 9 && enemy.StoredMatter == storedMatter + 10,
                    "Actual shared collector hauls and deposits both AI-owned resources");

                enemy.DepositAntimatter(1000); enemy.DepositMatter(1000);
                Check(enemy.Construction.TryStartNextTier(out _), "Rival tier requires a paid project"); enemy.Construction.Advance(30);
                enemy.DepositAntimatter(1000);
                int beforeOrder = enemy.StoredAntimatter;
                Check(ai.TryOrderProject("equipment-energy-shield-1") && enemy.StoredAntimatter == beforeOrder - 20,
                    "AI orders use ordinary research payment and prerequisites");
                enemy.Construction.Advance(20);
                Check(rival.GetOwnedEquipmentRank("energy-shield") == 1 && rival.GetEquipmentRank("energy-shield") == 0,
                    "AI research does not install remotely");
                rival.GlobalPosition = Point(40);
                Check(!rival.TryInstallEquipment("energy-shield", 1, out _), "AI cannot install away from home"); rival.GlobalPosition = Point(10);
                Check(rival.TryInstallEquipment("energy-shield", 1, out _), "AI can explicitly install at home");
                rival._PhysicsProcess(100); energy = rival.Energy;
                Check(rival.TryActivateEquipmentSlot(1) && rival.IsShielded && rival.Energy == energy - player.ShieldEquipmentEnergyCost,
                    "Installed AI shield has shared effect, cost and duration");
                int rivalHp = rival.Health; rival.ApplyDamage(20, player);
                Check(rival.Health == rivalHp - 7, "Rival shield applies the same damage reduction");
                enemy.WithdrawAntimatter(1000); enemy.DepositAntimatter(20);
                Check(!ai.TryOrderProject("collector-drive") && enemy.StoredAntimatter == 20, "AI research policy retains its minimum energy storage reserve");
                enemy.DepositAntimatter(1000);
                Check(ai.TryOrderProject("equipment-guardian-drone-1"), "Rival can research the same guardian equipment"); enemy.Construction.Advance(30);
                rival.GlobalPosition = Point(10); rival.TryInstallEquipment("guardian-drone", 3, out _); rival._PhysicsProcess(100);
                player.GlobalPosition = Point(25); rival.SetAutonomousControls(Vector3.Zero, player.GlobalPosition - rival.AimOrigin);
                rival.TryActivateEquipmentSlot(3); var guardian = room.GetChildren().OfType<GuardianDrone>().Single(); guardian.SetPhysicsProcess(false);
                Check(Invoke(guardian, "FindNearestTarget") == player, "Enemy guardian targets the opponent, not its owner"); guardian.Free();

                Check(ai.TryOrderProject("equipment-plasma-bolt-2"), "Sequential rival ranks use the same tier-gated paid catalog");
                enemy.Construction.Advance(30); rival.TryInstallEquipment("plasma-bolt", 0, out _);
                enemy.ApplyDamage(enemy.Health - 140);
                Check(enemy.Tier == 1 && rival.GetOwnedEquipmentRank("plasma-bolt") == 2 && rival.GetEquipmentRank("plasma-bolt") == 2,
                    "Siege tier loss preserves rival owned and installed higher-tier equipment");
                var rebuild = enemy.Construction.NextTierDefinition()!;
                int rebuildBefore = enemy.StoredAntimatter;
                Check(rebuild.Cost == 50 && enemy.Construction.TryStartNextTier(out _) && enemy.StoredAntimatter == rebuildBefore - 50,
                    "Rival recovery uses normal sequential half-price rebuilding"); enemy.Construction.Advance(30);
                Check(enemy.Tier == 2 && rival.GetEquipmentRank("plasma-bolt") == 2, "Rebuilding keeps the existing applied loadout");

                // Exercise committed combat with earned equipment, without waiting
                // for the normal five-minute strategic attack grace in this fixture.
                rival.GlobalPosition = arena.OnSurface(home.GlobalPosition + home.GlobalBasis.X * 40, 6);
                player.GlobalPosition = arena.OnSurface(home.GlobalPosition - home.GlobalBasis.X * 60, 6);
                rival._PhysicsProcess(100); int homeHp = home.Health;
                Invoke(ai, "SetGoal", RivalPilotController.PilotTask.Attack, home);
                rival.SetPhysicsProcess(true); ai.SetPhysicsProcess(true); await Frames(72);
                ai.SetPhysicsProcess(false); rival.SetPhysicsProcess(false);
                Check(home.Health < homeHp, "Committed rival attack fires real paid projectiles at the opposing base");
                rival.SpendEnergy(rival.Energy); rival.SetPhysicsProcess(true); ai._PhysicsProcess(0.01); rival.SetPhysicsProcess(false);
                Check(ai.Task == RivalPilotController.PilotTask.Recover, "An energy-starved attack retreats instead of using a free weapon");
                rival.GlobalPosition = Point(10); player.GlobalPosition = Point(-70);

                var site = new StrategicPoint { Position = Point(10), CaptureSeconds = 1 }; room.AddChild(site); site.SetPhysicsProcess(false);
                int recoveryStorage = enemy.StoredAntimatter; enemy.WithdrawAntimatter(1000);
                Invoke(ai, "SelectGoal", enemy);
                Check(ai.Task == RivalPilotController.PilotTask.Capture && rival.EnergyCapacity == 0,
                    "Zero storage selects no-cost income capture instead of an impossible Claim loop");
                enemy.DepositAntimatter(recoveryStorage);
                player.GlobalPosition = Point(-70); site._PhysicsProcess(1);
                Check(site.Ownership == AntimatterOwnership.Enemy, "Actual modern rival presence captures stations");
                player.GlobalPosition = rival.GlobalPosition; site.Advance(0.5, true, false); double progress = site.CaptureProgress; site._PhysicsProcess(1);
                Check(site.IsContested && site.CaptureProgress == progress, "Both modern pilots contest identically");
                player.GlobalPosition = Point(-70);
                var wreck = new ExplorationWreck { Position = Point(10), CollectionRadius = 8 }; room.AddChild(wreck); wreck.ApplyDamage(1000);
                rival.GlobalPosition = arena.OnSurface(wreck.GlobalPosition, 6);
                Check(wreck.TryCollect(rival) && rival.Boons.Pending.Count == 1, "Rival secures the same immutable three-choice offer");
                var offer = rival.Boons.Pending[0]; Invoke(ai, "ResolveBoons", enemy);
                var boon = offer.Choices.Single(b => rival.Boons.Stacks(b.Id) == 1);
                Check(rival.Boons.Pending.Count == 0 && rival.Boons.Stacks(boon.Id) == 1, "AI policy chooses one of the same legal persistent boons");
                using var random = new RandomNumberGenerator { Seed = 17 }; rival.Boons.SecureOffer(random, 30);
                enemy.WithdrawAntimatter(1000); enemy.DepositAntimatter(5);
                int rewardEnergy = rival.Energy; Invoke(ai, "ResolveBoons", enemy);
                Check(enemy.StoredAntimatter == 35 && rival.Energy == rewardEnergy && rival.Boons.Pending.Count == 0,
                    "Low-storage AI uses the legal skip deposit without refilling current energy");
                rival.Boons.SecureOffer(random, 30);
                int pending = rival.Boons.Pending.Count;
                var objective = new LevelObjective { PilotRespawnDelaySeconds = 0.1f }; objective.Initialize(player, home, enemy, rival); room.AddChild(objective); objective.SetPhysicsProcess(false);
                rival._PhysicsProcess(5); rival.ApplyDamage(1000, player);
                Check(rival.Health == 0 && objective.RivalPilotDefeated && !rival.TryActivateClaim(), "Shared death is tracked and prevents activation");
                objective._PhysicsProcess(0.2);
                Check(rival.Health == rival.MaximumHealth && !objective.RivalPilotDefeated && rival.GetEquipmentRank("energy-shield") == 1 && rival.GetEquipmentRank("plasma-bolt") == 2
                    && rival.Boons.Stacks(boon.Id) == 1 && rival.Boons.Pending.Count == pending,
                    "Same-object respawn retains ownership/loadout/boons/pending choices");
                Check(rival.Energy == enemy.StoredAntimatter && !rival.HasNode("CameraPivot"), "AI respawn shares storage-backed refill without stealing camera ownership");
                enemy.ApplyDamage(10000); rival._PhysicsProcess(3); rival.ApplyDamage(1000); objective.Evaluate();
                Check(objective.State == LevelObjective.LevelState.Victory, "Modern rival base-plus-pilot elimination preserves victory rules");
                room.QueueFree(); await Frames(10);
            }

            // Normal opening, accelerated simulation time; no AI funding or free completions.
            PlanetSizes.SelectedIndex = 1;
            var game = GD.Load<PackedScene>("res://scenes/PlanetGame.tscn").Instantiate<MagicCarpetRemastered.Scripts.Game.Game>(); AddChild(game);
            var hero = game.GetNode<ShipFlightController>("PlayerShip"); hero.SetPhysicsProcess(false);
            var npc = game.GetNode<ShipFlightController>("RivalPilot"); var controller = npc.GetNode<RivalPilotController>("RivalAI");
            var hub = game.GetNode<FactionBase>("EnemyBase");
            Check(hub.StoredAntimatter == 30 && hub.StoredMatter == 30 && npc.Health == 100, "Real match starts with equal unassisted rival resources");
            Engine.TimeScale = 8;
            await Frames(180);
            var management = game.GetNode<BaseManagement>("BaseManagement"); management.SetOpen(true);
            int orders = controller.OrdersPlaced;
            await Frames(480); Engine.TimeScale = originalScale;
            Check(!npc.GameplayInputBlocked && hero.GameplayInputBlocked && !GetTree().Paused, "Human live management cannot block rival input or simulation"); management.SetOpen(false);
            Check(controller.OrdersPlaced > orders, "Rival continues paid development while human management is open");
            Check(controller.OrdersPlaced > 0 && hub.SpentAntimatter > 0 && hub.SpentMatter > 0, "Integrated opening funds real paid development from the normal economy");
            Check(GetTree().GetNodesInGroup("strategic_point").OfType<StrategicPoint>().Any(p => p.Ownership == AntimatterOwnership.Enemy),
                "Integrated economy-first rival actually captures an income site");
            Check(npc.GetOwnedEquipmentRank("energy-shield") > 0 && npc.GetEquipmentRank("energy-shield") > 0,
                "Integrated research returns home and installs its researched equipment");
            Check(npc.Health > 0 && npc.EnergyCapacity == hub.StoredAntimatter && npc.Energy <= npc.EnergyCapacity,
                "Integrated AI remains alive with strictly storage-backed energy");
            GD.Print($"Rival opening: {controller.StatusText}; tier {hub.Tier}, {hub.StoredAntimatter} A / {hub.StoredMatter} M");
            if (DisplayServer.GetName() != "headless")
            {
                npc.SetPhysicsProcess(false); var terrain = game.GetNode<HeightmapArena>("HeightmapArena");
                hero.GlobalPosition = terrain.OnSurface(npc.GlobalPosition + npc.GlobalBasis.Z * 18, 6);
                terrain.Orient(hero, terrain.DirectionTo(hero.GlobalPosition, npc.GlobalPosition)); hero._PhysicsProcess(0); terrain.RefreshPlanetDetail();
                await Frames(30); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Check(GetViewport().GetTexture().GetImage().SavePng("C:/Users/lblan/AppData/Local/Temp/opencode/RivalFactionGameplay.png") == Error.Ok, "Rendered rival gameplay capture saves");
                var hud = game.GetNode<Hud>("Hud");
                hud._Input(new InputEventKey { Keycode = Key.F3, PhysicalKeycode = Key.F3, Pressed = true });
                hud._Process(1);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var details = hud.GetNode<RichTextLabel>("HudRoot/DebugPanel/DebugContent/DebugDetails");
                Check(hud.GetNode<PanelContainer>("HudRoot/DebugPanel").Visible, "Actual F3 event opens debug diagnostics");
                Check(details.Text.Contains("Rival AI:") && details.Text.Contains(controller.Task.ToString()), "F3 exposes the rival's current task");
                int line = Array.FindIndex(details.Text.Split('\n'), text => text.Contains("Rival AI:"));
                details.ScrollToParagraph(Math.Max(0, line - 1));
                await Frames(3); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Check(GetViewport().GetTexture().GetImage().SavePng("C:/Users/lblan/AppData/Local/Temp/opencode/RivalFactionDebug.png") == Error.Ok, "Rendered rival task diagnostics capture saves");
            }
            game.QueueFree(); await Frames(10); GD.Print($"Rival faction checks passed: {_checks}"); GetTree().Quit();
        }
        catch (Exception error) { Engine.TimeScale = originalScale; GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Ui;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class ExplorationBoonChecks : Node3D
{
    private int _checks;
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();
    private void Check(bool condition, string message) { _checks++; if (!condition) throw new Exception(message); }
    private async Task Frames(int count) { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    private static void Cast(ShipFlightController ship, string method) => typeof(ShipFlightController)
        .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(ship, null);
    private BoonOffer OfferWith(ExplorationBoons boons, RandomNumberGenerator random, string id)
    {
        for (int i = 0; i < 200; i++) { var offer = boons.SecureOffer(random, 30); if (offer.Choices.Any(b => b.Id == id)) return offer; }
        throw new Exception("Seeded offer generation failed to include requested family");
    }
    private async Task Run()
    {
        try
        {
            using var random = new RandomNumberGenerator { Seed = 7821 };
            foreach (int radius in PlanetSizes.Radii)
            {
                var room = new Node3D(); AddChild(room);
                var arena = new HeightmapArena { IsSpherical = true, PlanetRadius = radius }; room.AddChild(arena);
                var home = new FactionBase { Position = arena.OnSurface(arena.PlanetCenter + Vector3.Up * radius) };
                var rival = new FactionBase { IsPlayerBase = false, Position = arena.OnSurface(arena.PlanetCenter + Vector3.Down * radius) };
                room.AddChild(home); room.AddChild(rival); home.SetPhysicsProcess(false); rival.SetPhysicsProcess(false);
                var director = new ExplorationDirector(); room.AddChild(director); director.SetPhysicsProcess(false);
                var wrecks = room.GetChildren().OfType<ExplorationWreck>().ToArray();
                Check(wrecks.Length == 12, $"{radius}: all initial wrecks placed");
                foreach (var wreck in wrecks)
                {
                    wreck.SetPhysicsProcess(false);
                    Check(arena.SurfaceDistance(wreck.GlobalPosition, home.GlobalPosition) >= 180
                        && arena.SurfaceDistance(wreck.GlobalPosition, rival.GlobalPosition) >= 180, "Both pole bases excluded");
                    Check(Mathf.Abs(arena.Clearance(wreck.GlobalPosition) - 1) < 0.01f
                        && wreck.GlobalBasis.Y.Dot(arena.UpAt(wreck.GlobalPosition)) > 0.999f, "Reachable surface support / upright wreck");
                }
                Check(wrecks.SelectMany((w, i) => wrecks.Skip(i + 1).Select(other => arena.SurfaceDistance(w.GlobalPosition, other.GlobalPosition))).All(d => d >= 99.99f), "Initial spacing respected");
                int nearHome = wrecks.Count(w => arena.SurfaceDistance(w.GlobalPosition, home.GlobalPosition) < arena.SurfaceDistance(w.GlobalPosition, rival.GlobalPosition));
                Check(Math.Abs(nearHome - 6) <= 1, "Nearest-faction regions receive balanced initial placement");
                director._PhysicsProcess(60);
                Check(director.SpawnedCount == 13, "Real-time interval triggers orbital arrival");
                for (int i = 0; i < 7; i++) Check(director.TrySpawn(true), "Orbital arrival placed");
                Check(!director.TrySpawn(true) && director.SpawnedCount == 20, "Active wreck/reward bound rejects without spawning");
                foreach (var wreck in room.GetChildren().OfType<ExplorationWreck>())
                    Check(arena.SurfaceDistance(wreck.GlobalPosition, home.GlobalPosition) >= 180
                        && arena.SurfaceDistance(wreck.GlobalPosition, rival.GlobalPosition) >= 180, "Arrivals retain every-base exclusion");
                var arrival = room.GetChildren().OfType<ExplorationWreck>().Last(); arrival.SetPhysicsProcess(false);
                arrival.ApplyDamage(1000); Check(!arrival.IsRevealed && arrival.ArrivalRemaining == 3, "Harmless descent cannot be destroyed before landing");
                arrival._PhysicsProcess(4); Check(arrival.ArrivalRemaining == 0 && Mathf.Abs(arena.Clearance(arrival.GlobalPosition) - 1) < 0.01f, "Arrival lands on surface without impact damage");
                foreach (var wreck in room.GetChildren().OfType<ExplorationWreck>().ToArray()) wreck.Free();
                home.GlobalPosition = arena.OnSurface(arena.PlanetCenter + new Vector3(-1, 0, 0.001f).Normalized() * radius);
                Vector3 acrossSeam = arena.PlanetCenter + new Vector3(-1, 0, -0.001f).Normalized() * radius;
                Check(!director.IsValidPosition(acrossSeam), "Every-base exclusion crosses the longitude seam");
                director.BaseExclusionDistance = Mathf.Pi * radius + 1;
                long spawned = director.SpawnedCount;
                Check(!director.TrySpawn(true) && director.SpawnedCount == spawned && !room.GetChildren().OfType<ExplorationWreck>().Any(), "Impossible placement defers without actors/unfair fallback");
                room.Free(); await Frames(2);
            }

            var testRoom = new Node3D(); AddChild(testRoom);
            var terrain = new HeightmapArena { IsSpherical = true, PlanetRadius = 800 }; testRoom.AddChild(terrain);
            Vector3 underside = terrain.PlanetCenter + Vector3.Down * 800;
            var hub = new FactionBase { Position = terrain.OnSurface(underside), InitialStoredAntimatter = 110 }; testRoom.AddChild(hub); hub.SetPhysicsProcess(false);
            var enemyHub = new FactionBase { IsPlayerBase = false, Position = terrain.OnSurface(terrain.PlanetCenter + Vector3.Up * 800) };
            testRoom.AddChild(enemyHub); enemyHub.SetPhysicsProcess(false);
            var ship = new ShipFlightController { Position = terrain.OnSurface(underside, 6), EnergyRegenFractionPerSecond = 0.04f }; testRoom.AddChild(ship); ship.SetPhysicsProcess(false);
            var other = new ShipFlightController { Position = ship.GlobalPosition }; testRoom.AddChild(other); other.SetPhysicsProcess(false);
            var wreckTarget = new ExplorationWreck { Position = terrain.OnSurface(underside + terrain.UpAt(underside).Cross(Vector3.Right) * 16, 1) };
            testRoom.AddChild(wreckTarget); wreckTarget.SetPhysicsProcess(false);
            Check(!wreckTarget.TryCollect(ship), "Intact wreck rejects collection");
            var bolt = new WeaponProjectile { Damage = 25 }; AddChild(bolt);
            typeof(WeaponProjectile).GetMethod("OnBodyEntered", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(bolt, new object[] { wreckTarget });
            Check(wreckTarget.Health == 25 && ship.Boons.Pending.Count == 0, "Actual Plasma Bolt hit damages wreck without awarding attacker");
            await Frames(2);
            var sweptBolt = new WeaponProjectile { Damage = 25 }; AddChild(sweptBolt);
            sweptBolt.GlobalPosition = wreckTarget.GlobalPosition + wreckTarget.GlobalBasis.Z * 8 + wreckTarget.GlobalBasis.Y * 0.8f;
            sweptBolt.Launch(-wreckTarget.GlobalBasis.Z, ship);
            sweptBolt._PhysicsProcess(0.3);
            Check(wreckTarget.IsRevealed, "Actual swept projectile ray destroys radial wreck collider");
            wreckTarget.ApplyDamage(500);
            Check(wreckTarget.IsRevealed && ship.Boons.Pending.Count == 0, "Destruction reveals one reward and gives attacker no exclusive ownership");
            ship.GlobalPosition = terrain.OnSurface(wreckTarget.GlobalPosition, 6); other.GlobalPosition = ship.GlobalPosition;
            Check(wreckTarget.TryCollect(other) && !wreckTarget.TryCollect(ship), "First collector secures reward; second simultaneous call rejects");
            Check(other.Boons.Pending.Count == 1 && ship.Boons.Pending.Count == 0, "Offer belongs only to collector");
            var secured = other.Boons.Pending[0];
            Check(secured.Choices.Count == 3 && secured.Choices.Select(b => b.Id).Distinct().Count() == 3, "Three distinct stable options");
            Check(!other.Boons.Choose(secured.Id, "invalid") && other.Boons.Pending[0] == secured, "Invalid selection does not consume/reroll");
            other.ApplyDamage(1000); other.RespawnAt(other.GlobalPosition);
            Check(other.Boons.Pending[0] == secured, "Pending choice survives death / respawn unchanged");
            Check(other.Boons.Choose(secured.Id, secured.Choices[0].Id) && !other.Boons.Choose(secured.Id, secured.Choices[1].Id)
                && !other.Boons.Skip(secured.Id, hub), "Choose once excludes later choice and skip");
            var rivalPilot = new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.RivalShip, Position = terrain.OnSurface(enemyHub.GlobalPosition, 6) };
            testRoom.AddChild(rivalPilot); rivalPilot.SetPhysicsProcess(false);
            var rivalWreck = new ExplorationWreck { Position = terrain.OnSurface(enemyHub.GlobalPosition, 1) }; testRoom.AddChild(rivalWreck); rivalWreck.SetPhysicsProcess(false);
            int rivalStored = enemyHub.StoredAntimatter;
            rivalWreck.ApplyDamage(50); rivalWreck._PhysicsProcess(0);
            Check(rivalWreck.IsCollected && enemyHub.StoredAntimatter == rivalStored + 30 && !rivalWreck.TryCollect(ship), "Rival first pickup secures legal skip reward and excludes human collector");
            rivalPilot.Free();
            var tracker = new WorldAntimatterTracker(); testRoom.AddChild(tracker); tracker.SetPhysicsProcess(false); tracker.Refresh();
            int beforeTotal = tracker.TotalAntimatter, energy = ship.Energy;
            var skip = ship.Boons.SecureOffer(random, 30);
            Check(ship.Boons.Skip(skip.Id, hub), "Skip deposits without a collector drone"); tracker.Refresh();
            Check(hub.StoredAntimatter == 120 && tracker.TotalAntimatter == beforeTotal + 30 && tracker.PlayerControlledAntimatter == 140,
                "Partial capacity bonus: 10 deposit + 20 owned overflow exactly");
            Check(ship.Energy == energy && ship.EnergyCapacity == 120 && hub.SpentAntimatter == 0, "Bonus raises energy limit without refill/spending");
            skip = ship.Boons.SecureOffer(random, 30); beforeTotal = tracker.TotalAntimatter;
            Check(ship.Boons.Skip(skip.Id, hub) && !ship.Boons.Skip(skip.Id, hub) && !ship.Boons.Choose(skip.Id, skip.Choices[0].Id), "Skip exactly once excludes selection");
            tracker.Refresh(); Check(tracker.TotalAntimatter == beforeTotal + 30, "Full-capacity skip preserves all bonus as owned overflow");

            foreach (var boon in ExplorationBoons.Catalog)
            {
                int health = ship.Health;
                var offer = OfferWith(ship.Boons, random, boon.Id);
                Check(ship.Boons.Choose(offer.Id, boon.Id) && ship.Boons.Stacks(boon.Id) == 1, "Every boon family applies");
                Check(ship.GetOwnedEquipmentRank("plasma-bolt") == 1 && ship.GetOwnedEquipmentRank("dash") == 0 && hub.Tier == 1, "Boons cannot research equipment / tiers");
                if (boon.Id == "ship-hp") Check(ship.Health == health && ship.MaximumHealth == 120, "Hull increases maximum HP without acquisition healing");
            }
            Check(Mathf.IsEqualApprox(ship.EffectiveMoveSpeed, 26.4f) && Mathf.IsEqualApprox(ship.EffectiveBoosterSpeed, 39.6f)
                && Mathf.IsEqualApprox(ship.EffectiveDashSpeed, 50.6f), "Mobility affects normal / built-in / slotted speed");
            Check(Mathf.IsEqualApprox(ship.EnergyRechargePerSecond, 5.28f) && Mathf.IsEqualApprox(ship.EffectiveFireCooldown, 0.25f / 1.1f), "Recharge and primary fire-rate formulas");
            Check(ship.PlasmaBoltDamage == Mathf.RoundToInt(27.5f) && ship.EffectiveAreaDamage == 35 && ship.EffectiveGuardianDamage == 15,
                "Rank-derived energy/explosive damage and overlapping deployable tag multiply consistently");
            ship.GlobalPosition = terrain.OnSurface(underside, 6); ship.RespawnAt(ship.GlobalPosition);
            Cast(ship, "CastPrimaryEquipment");
            var actual = GetChildren().OfType<WeaponProjectile>().Last();
            Check(actual.Damage == ship.PlasmaBoltDamage && Mathf.IsEqualApprox(actual.Speed, 46.2f), "Actual projectile uses boon damage and speed");
            Check(ship.Health == 120 && ship.Boons.Stacks("ship-hp") == 1, "Respawn restores boon max HP and retains stacks");
            hub.TryCompleteTierUpgrade(2);
            Check(hub.Construction.TryStart("equipment-plasma-bolt-2", out _), "Research rank independently from boons");
            hub.Construction.Advance(100);
            Check(ship.TryInstallEquipment("plasma-bolt", 0, out _) && ship.PlasmaBoltDamage == Mathf.RoundToInt(35 * 1.1f), "Rank effect precedes additive boon multiplier");
            hub.ApplyDamage(140);
            Check(hub.Tier == 1 && ship.Boons.Stacks("energy-damage") == 1 && ship.GetEquipmentRank("plasma-bolt") == 2,
                "Tier loss retains both installed rank and match boons");
            for (int i = 1; i < 10; i++) { var offer = OfferWith(ship.Boons, random, "speed"); Check(ship.Boons.Choose(offer.Id, "speed"), "Repeated mobility stacks"); }
            var capped = OfferWith(ship.Boons, random, "speed");
            Check(!ship.Boons.Choose(capped.Id, "speed") && Mathf.IsEqualApprox(ship.EffectiveMoveSpeed, 48), "Ten-stack additive cap rejects without consuming reward");
            // Remove the helper's unused offers so the UI exercises a short realistic queue.
            foreach (var offer in ship.Boons.Pending.ToArray()) ship.Boons.Skip(offer.Id, hub);
            var offerA = ship.Boons.SecureOffer(random, 30); var offerB = ship.Boons.SecureOffer(random, 30);
            var management = new BaseManagement(); testRoom.AddChild(management);
            var ui = new BoonChoices(); testRoom.AddChild(ui);
            Check(ui.SetOpen(true) && ship.GameplayInputBlocked && !GetTree().Paused, "Choice UI owns input while simulation stays live");
            int storage = hub.StoredAntimatter; ship._PhysicsProcess(0.2);
            Check(!ship.TryActivateBooster() && hub.StoredAntimatter == storage, "Live choice UI blocks combat/booster payment");
            var project = hub.Construction.Catalog.First(d => d.Id == "repair-matrix");
            hub.WithdrawAntimatter(20); hub.DepositAntimatter(20);
            Check(hub.Construction.TryStart(project.Id, out _), "Research can start alongside pending reward");
            hub.SetPhysicsProcess(true); double elapsed = hub.Construction.GetActive(ConstructionSlot.Defense)!.ElapsedSeconds;
            await Frames(4); hub.SetPhysicsProcess(false);
            Check(hub.Construction.GetActive(ConstructionSlot.Defense)!.ElapsedSeconds > elapsed && !GetTree().Paused, "Research advances real frames with choice UI open");
            Check(ui.SetOpen(false) && !ship.GameplayInputBlocked && ship.Boons.Pending[0] == offerA && ship.Boons.Pending[1] == offerB, "Deferring preserves separate secured offers");
            management.SetOpen(true); ui.SetOpen(true);
            Check(ui.IsOpen && !management.IsOpen && ship.GameplayInputBlocked, "Management-to-boons transfers input ownership");
            management.SetOpen(true); Check(!ui.IsOpen && management.IsOpen && ship.GameplayInputBlocked, "Boons-to-management transfers input ownership");
            management.SetOpen(false); ui.SetOpen(true);
            // Route a real viewport click through GUI hit testing.
            var button = FindButton(ui, "BoonChoice_0");
            if (button.Disabled) button = FindButton(ui, "SkipBoon");
            await Frames(2);
            Vector2 click = button.GetGlobalRect().GetCenter();
            int projectileCount = GetChildren().OfType<WeaponProjectile>().Count();
            GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = click }, true);
            GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = click }, true);
            await Frames(2);
            Check(ship.Boons.Pending.Count == 1, "GUI selection consumes exactly one displayed reward");
            Check(GetChildren().OfType<WeaponProjectile>().Count() <= projectileCount, "Choice click cannot also cast Plasma Bolt");
            string? capture = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?.Substring(10);
            if (capture != null)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage(); Check(image.SavePng(capture) == Error.Ok, "Rendered boon UI capture saves");
                await Frames(2);
            }
            ui.SetOpen(false); GetViewport().PushInput(new InputEventKey { Keycode = Key.H, PhysicalKeycode = Key.H, Pressed = true }, true);
            Check(ui.IsOpen && ship.GameplayInputBlocked, "Physical H opens secured choices");
            GetViewport().PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = true }, true);
            Check(!ui.IsOpen && !ship.GameplayInputBlocked, "Escape defers and releases input");
            ui.SetOpen(true); hub.ApplyDamage(1000);
            Check(!ui.Skip() && !ui.Choose(0) && ship.Boons.Pending.Count == 1, "Terminal base destruction rejects unresolved choice/bonus without consuming it");
            ui._Process(0);
            Check(!ui.IsOpen, "Terminal state closes boon panel");
            Check(new ExplorationBoons().Pending.Count == 0 && new ExplorationBoons().Stacks("speed") == 0, "New match state starts empty");
            testRoom.Free(); await Frames(2);
            GD.Print($"Exploration boon checks passed: {_checks}"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
    private static Button FindButton(Node node, string name)
    {
        if (node is Button button && button.Name == name) return button;
        foreach (Node child in node.GetChildren()) { var found = FindButtonOrNull(child, name); if (found != null) return found; }
        throw new Exception($"Missing button {name}");
    }
    private static Button? FindButtonOrNull(Node node, string name)
    {
        if (node is Button button && button.Name == name) return button;
        foreach (Node child in node.GetChildren()) { var found = FindButtonOrNull(child, name); if (found != null) return found; }
        return null;
    }
}

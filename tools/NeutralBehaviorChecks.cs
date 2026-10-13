using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.Game;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class NeutralBehaviorChecks : Node3D
{
    private int _checks;
    private Node3D _room = null!;
    private HeightmapArena _arena = null!;
    private Vector3 _origin;
    private Vector3 _tangent;
    private ShipFlightController _ship = null!;
    private EnemyEcosystemEnemy _pilot = null!;

    private void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private async Task Frames(int count)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    private static object? Invoke(object subject, string method, params object[] args)
        => subject.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(subject, args);
    private static float Windup(EnemyEcosystemEnemy mob) => (float)typeof(EnemyEcosystemEnemy)
        .GetField("_rangedWindup", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mob)!;
    private Vector3 Point(float distance, float clearance = 10)
    {
        Vector3 up = _arena.UpAt(_origin);
        return _arena.OnSurface(_arena.IsSpherical
            ? _arena.PlanetCenter + new Quaternion(up.Cross(_tangent), distance / _arena.PlanetRadius) * up * _arena.PlanetRadius
            : _origin + _tangent * distance, clearance);
    }
    private EnemyEcosystemEnemy Mob(EnemyEcosystemEnemy.EnemyRole role = EnemyEcosystemEnemy.EnemyRole.GroundBeast)
    {
        var mob = new EnemyEcosystemEnemy { Role = role, Position = Point(0), PilotHoverClearance = 10 };
        _room.AddChild(mob); mob.SetPhysicsProcess(false); return mob;
    }
    private void FarPilots() { _ship.GlobalPosition = Point(220); _pilot.GlobalPosition = Point(-220); }

    public override void _Ready() => Callable.From(() => { _ = RunChecks(); }).CallDeferred();
    private async Task RunChecks()
    {
        WorldSeeds.UseSeed(7319);
        try
        {
            foreach (Vector3 up in new[] { Vector3.Zero, Vector3.Down, new Vector3(1, -1, 1).Normalized() })
            {
                bool spherical = up != Vector3.Zero;
                _room = new Node3D(); AddChild(_room);
                _arena = new HeightmapArena { IsSpherical = spherical, PlanetRadius = 800 }; _room.AddChild(_arena);
                _origin = spherical ? _arena.PlanetCenter + up * 800 : Vector3.Zero;
                _tangent = PlanetGeometry.Frame(spherical ? up : Vector3.Up, Vector3.Forward).X;
                _ship = new ShipFlightController { Position = Point(220), DamageInvulnerabilitySeconds = 0 };
                _room.AddChild(_ship); _ship.SetPhysicsProcess(false);
                _pilot = new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.RivalShip, Position = Point(-220) };
                _room.AddChild(_pilot); _pilot.SetPhysicsProcess(false);
                await Frames(6);

                foreach (var role in new[] { EnemyEcosystemEnemy.EnemyRole.FlyingSwarm, EnemyEcosystemEnemy.EnemyRole.GroundBeast,
                    EnemyEcosystemEnemy.EnemyRole.RangedCaster, EnemyEcosystemEnemy.EnemyRole.BaseAttacker, EnemyEcosystemEnemy.EnemyRole.AntimatterThief })
                {
                    FarPilots(); var mob = Mob(role);
                    mob._PhysicsProcess(0);
                    Check(mob.Territory!.State == NeutralTerritory.BehaviorState.Patrol && mob.Territory.Target == null,
                        $"{spherical}/{role}: distant pilots do not attract ordinary mobs");
                    _ship.GlobalPosition = Point(mob.AwarenessRadius + 1); mob._PhysicsProcess(0);
                    Check(mob.Territory.Target == null, "Proximity range does not silently extend to ranged weapon range");
                    _ship.GlobalPosition = Point(mob.AwarenessRadius - 1); mob._PhysicsProcess(0);
                    Check(mob.Territory.Target == _ship, "Entering proximity aggro targets the human pilot");
                    mob.GlobalPosition = Point(mob.HabitatLeashRadius * 0.4f);
                    _ship.GlobalPosition = Point(mob.HabitatLeashRadius * 0.4f + mob.DisengagementRadius + 1); mob._PhysicsProcess(0);
                    Check(mob.Territory.State == NeutralTerritory.BehaviorState.Returning && mob.Territory.Target == null,
                        "Escaping pursuit commits a return-home state");
                    _ship.GlobalPosition = mob.GlobalPosition; int hp = _ship.Health;
                    mob.ApplyDamage(1, _ship); mob._PhysicsProcess(0);
                    Check(mob.Territory.State == NeutralTerritory.BehaviorState.Returning && _ship.Health == hp,
                        "Return cannot immediately re-aggro or deal incidental contact damage");
                    mob.GlobalPosition = Point(0); _ship.GlobalPosition = Point(1); mob._PhysicsProcess(0);
                    Check(mob.Territory.State == NeutralTerritory.BehaviorState.Patrol && mob.Territory.Target == null,
                        "Home arrival pacifies the mob before reacquisition");
                    mob._PhysicsProcess(0); Check(mob.Territory.Target == null, "Return grace prevents a same-frame aggro loop");
                    FarPilots(); _pilot.GlobalPosition = Point(mob.AwarenessRadius - 1); mob._PhysicsProcess(3.1);
                    Check(mob.Territory.Target == _pilot, "Rival proximity uses the same neutral aggression rules");
                    _pilot.Health = 0; mob._PhysicsProcess(0);
                    Check(mob.Territory.Target == null && mob.Territory.State == NeutralTerritory.BehaviorState.Returning,
                        "Dead rival targets are discarded safely");
                    _pilot.Health = 36;
                    mob.Free();
                }

                FarPilots(); var retaliation = Mob();
                _ship.GlobalPosition = Point(retaliation.RetaliationRadius - 1); retaliation._PhysicsProcess(0);
                Check(retaliation.Territory!.Target == null, "Outer retaliation band is passive before damage");
                retaliation.ApplyDamage(1, _ship);
                Check(retaliation.Territory.Target == _ship, "Damage inside the larger range identifies and aggroes the attacker");
                _ship.GlobalPosition = Point(retaliation.HabitatLeashRadius + 1); retaliation._PhysicsProcess(0);
                Check(retaliation.Territory.State == NeutralTerritory.BehaviorState.Returning, "Target crossing habitat leash ends pursuit");
                retaliation.Free();

                FarPilots(); var packVictim = Mob(); var packMate = Mob(); var unrelated = Mob();
                packVictim.HabitatId = packMate.HabitatId = "test-pack"; unrelated.HabitatId = "other-pack";
                _ship.GlobalPosition = Point(packVictim.RetaliationRadius - 2);
                packVictim.ApplyDamage(1000, _ship);
                Check(packMate.Territory!.Target == _ship && unrelated.Territory!.Target == null,
                    "Lethal hit rallies the same habitat but never unrelated packs");
                _ship.GlobalPosition = Point(packMate.HabitatLeashRadius + 1); packMate._PhysicsProcess(0);
                Check(packMate.Territory.State == NeutralTerritory.BehaviorState.Returning, "Rallied pack obeys ordinary escape/leash rules");
                _ship.GlobalPosition = Point(5); packMate.ApplyDamage(1, _ship);
                Check(packMate.Territory.Target == null, "Damage cannot interrupt committed pack return");
                packMate.GlobalPosition = Point(0); packMate._PhysicsProcess(0); packMate._PhysicsProcess(3.1);
                FarPilots(); _pilot.GlobalPosition = Point(packMate.RetaliationRadius - 2);
                packMate.HabitatId = unrelated.HabitatId = "test-rival-pack"; packMate.ApplyDamage(1, _pilot);
                Check(unrelated.Territory!.Target == _pilot, "Pack retaliation also identifies the rival attacker");
                packMate.Free(); unrelated.Free(); await Frames(3);

                FarPilots(); var alert = Mob();
                _ship.GlobalPosition = Point(alert.RetaliationRadius + 1); alert.ApplyDamage(1, _ship);
                Check(alert.Territory!.State == NeutralTerritory.BehaviorState.Alert && alert.Territory.Target == null,
                    "Distant shots cause local alert, not global attacker tracking");
                FarPilots(); alert._PhysicsProcess(3.1);
                Check(alert.Territory.State == NeutralTerritory.BehaviorState.Patrol, "Local search times out normally");
                _pilot.GlobalPosition = Point(alert.RetaliationRadius - 1); alert.ApplyDamage(1, _pilot);
                Check(alert.Territory.Target == _pilot, "Rival attacks use identical retaliation range");
                alert.GlobalPosition = Point(alert.HabitatLeashRadius + 1); _pilot.GlobalPosition = Point(alert.HabitatLeashRadius - 1); alert._PhysicsProcess(0);
                Check(alert.Territory.State == NeutralTerritory.BehaviorState.Returning, "The mob's own leash also ends pursuit");
                alert.Free();

                FarPilots(); var orphaned = Mob();
                var temporaryPilot = new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.RivalShip, Position = Point(5) };
                _room.AddChild(temporaryPilot); temporaryPilot.SetPhysicsProcess(false);
                orphaned.ApplyDamage(1, temporaryPilot); temporaryPilot.Free(); orphaned._PhysicsProcess(0);
                Check(orphaned.Territory!.Target == null && orphaned.Territory.State == NeutralTerritory.BehaviorState.Returning,
                    "Actually freed attacker wrappers safely end pursuit"); orphaned.Free();

                FarPilots(); var contact = Mob(); contact.GlobalPosition = Point(0, contact.GroundClearance); _pilot.GlobalPosition = Point(1, 2);
                int contactHp = _pilot.Health; contact._PhysicsProcess(0);
                Check(_pilot.Health == contactHp - contact.ContactDamage, "Neutral melee contact damages its rival target");
                _pilot.Health = 36; contact.Free();

                FarPilots(); var caster = Mob(EnemyEcosystemEnemy.EnemyRole.RangedCaster);
                _ship.GlobalPosition = Point(10); caster._PhysicsProcess(1);
                Check(Windup(caster) > 0, "Aggroed visible caster begins a real windup");
                _ship.GlobalPosition = Point(220); caster._PhysicsProcess(0.2);
                Check(Windup(caster) == 0 && !_room.GetChildren().OfType<EnemyProjectile>().Any(), "Escape cancels a queued shot");
                caster.Free(); FarPilots(); caster = Mob(EnemyEcosystemEnemy.EnemyRole.RangedCaster);
                _pilot.GlobalPosition = Point(10); caster._PhysicsProcess(1); caster._PhysicsProcess(0.2);
                var shot = _room.GetChildren().OfType<EnemyProjectile>().Single(); shot.SetPhysicsProcess(false);
                Check(caster.Territory!.Target == _pilot, "Caster aims its actual shot at the rival rather than the distant player");
                int rivalHp = _pilot.Health; Invoke(shot, "OnBodyEntered", _pilot);
                Check(_pilot.Health == rivalHp - shot.Damage, "Neutral projectile damages the rival pilot");
                caster.Free(); shot.Free(); _pilot.Health = 36;

                FarPilots(); caster = Mob(EnemyEcosystemEnemy.EnemyRole.RangedCaster); _pilot.GlobalPosition = Point(10);
                var wall = new StaticBody3D { Position = (caster.GlobalPosition + _pilot.GlobalPosition) * 0.5f };
                wall.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(4, 8, 4) } });
                _room.AddChild(wall); await Frames(3); caster._PhysicsProcess(1);
                Check(Windup(caster) == 0, "An occluded pilot cannot be shot through a solid obstacle");
                wall.Free(); await Frames(2); caster._PhysicsProcess(0);
                Check(Windup(caster) > 0, "Removing the obstruction restores a legal attack without reacquiring globally"); caster.Free();

                FarPilots(); var victim = Mob(); _ship.GlobalPosition = Point(victim.RetaliationRadius - 1);
                var bolt = new WeaponProjectile { Damage = 1 }; _room.AddChild(bolt); bolt.SetPhysicsProcess(false); bolt.Launch(_tangent, _ship);
                Invoke(bolt, "OnBodyEntered", victim);
                Check(victim.Territory!.Target == _ship, "Plasma projectile delivers attacker identity to retaliation");
                victim.Free(); bolt.Free(); victim = Mob();
                Invoke(_ship, "DamageEnemiesInRadius", victim.GlobalPosition, 3f, 1);
                Check(victim.Territory!.Target == _ship, "Area damage delivers attacker identity");
                victim.Free(); victim = Mob();
                var guardian = new GuardianDrone { OwnerPilot = _ship, Damage = 1, Position = victim.GlobalPosition };
                _room.AddChild(guardian); guardian.SetPhysicsProcess(false); guardian.GlobalPosition = victim.GlobalPosition;
                guardian._PhysicsProcess(0);
                Check(victim.Territory!.Target == _ship, "Guardian damage attributes retaliation to its pilot");
                guardian.Free(); victim.Free();

                FarPilots(); victim = Mob(); _pilot.GlobalPosition = Point(victim.RetaliationRadius - 1);
                var rivalShot = new EnemyProjectile { Damage = 1 }; _room.AddChild(rivalShot); rivalShot.SetPhysicsProcess(false); rivalShot.Launch(_tangent, _pilot);
                Invoke(rivalShot, "OnBodyEntered", victim);
                Check(victim.Territory!.Target == _pilot, "Actual rival projectile delivers attacker identity to a neutral");
                rivalShot.Free(); victim.Free();

                FarPilots();
                foreach (var owner in new[] { AntimatterOwnership.Player, AntimatterOwnership.Enemy, AntimatterOwnership.Neutral })
                {
                    var scavenger = Mob(EnemyEcosystemEnemy.EnemyRole.AntimatterThief);
                    var resource = new AntimatterPickup { Ownership = owner, AntimatterAmount = 9, Position = Point(0, 1) };
                    _room.AddChild(resource); resource.SetPhysicsProcess(false); resource.Freeze = true;
                    var ledger = new WorldAntimatterTracker(); _room.AddChild(ledger); ledger.SetProcess(false); ledger.Refresh(); int supplied = ledger.TotalAntimatter;
                    scavenger._PhysicsProcess(0); ledger.Refresh();
                    Check(resource.AntimatterAmount == 0 && scavenger.AntimatterReserve == 9 && ledger.TotalAntimatter == supplied,
                        $"Local scavenger steals {owner} resource with exact conservation");
                    Check(ledger.NeutralCreatureAntimatter == 9, "Scavenging cannot subsidize either faction");
                    scavenger.ApplyDamage(1000); ledger.Refresh();
                    Check(ledger.TotalAntimatter == supplied && ledger.NeutralCreatureAntimatter == 0, "Scavenger death transfers reserve to neutral drops once");
                    scavenger.Free(); resource.Free(); ledger.Free();
                    foreach (var fragment in _room.GetChildren().OfType<AntimatterPickup>().ToArray()) fragment.Free();
                }

                FarPilots(); var walker = Mob(); _ship.GlobalPosition = Point(walker.AwarenessRadius - 1);
                float before = _arena.SurfaceDistance(walker.GlobalPosition, _ship.GlobalPosition);
                walker.SetPhysicsProcess(true); await Frames(45); walker.SetPhysicsProcess(false);
                Check(_arena.SurfaceDistance(walker.GlobalPosition, _ship.GlobalPosition) < before - 0.5f, "Real solver movement pursues a local target");
                walker.GlobalPosition = Point(14); FarPilots(); walker.SetPhysicsProcess(true); await Frames(180); walker.SetPhysicsProcess(false);
                Check(walker.Territory!.Target == null && _arena.SurfaceDistance(walker.GlobalPosition, walker.Territory.Home) < 10,
                    "Real movement returns home after escape instead of continuing toward a base");
                Check(_arena.Clearance(walker.GlobalPosition) >= walker.GroundClearance - 0.02f
                    && walker.GlobalBasis.Y.Dot(_arena.UpAt(walker.GlobalPosition)) > 0.99f, "Returning movement retains radial grounding/orientation");
                walker.Free();

                FarPilots(); var swarm = Mob(EnemyEcosystemEnemy.EnemyRole.FlyingSwarm);
                _ship.GlobalPosition = Point(5, 2); int shipHp = _ship.Health;
                swarm.SetPhysicsProcess(true); await Frames(180); swarm.SetPhysicsProcess(false);
                Check(_ship.Health < shipHp, "Real swarm descends to a low-flying pilot and lands contact hits before breaking away");
                swarm.Free();

                _room.QueueFree(); await Frames(10);
            }

            await CheckSpawningAndRaids();
            GD.Print($"Neutral behavior checks passed: {_checks}"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }

    private async Task CheckSpawningAndRaids()
    {
        foreach (int radius in DisplayServer.GetName() == "headless" ? PlanetSizes.Radii : new[] { 200 })
        {
            PlanetSizes.SelectedIndex = Array.IndexOf(PlanetSizes.Radii, radius);
            var game = GD.Load<PackedScene>("res://scenes/PlanetGame.tscn").Instantiate<MagicCarpetRemastered.Scripts.Game.Game>(); AddChild(game);
            var arena = game.GetNode<HeightmapArena>("HeightmapArena");
            var director = game.GetNode<EnemyEcosystemDirector>("EnemyEcosystemDirector"); director.SetPhysicsProcess(false);
            var home = game.GetNode<FactionBase>("PlayerBase"); home.SetPhysicsProcess(false);
            var rival = game.GetNode<FactionBase>("EnemyBase"); rival.SetPhysicsProcess(false);
            var player = game.GetNode<ShipFlightController>("PlayerShip"); player.SetPhysicsProcess(false);
            game.GetNode<ShipFlightController>("RivalPilot").SetPhysicsProcess(false);
            game.GetNode<LevelObjective>("LevelObjective").SetPhysicsProcess(false);
            foreach (var e in game.GetChildren().OfType<EnemyEcosystemEnemy>()) e.SetPhysicsProcess(false);
            foreach (var d in game.GetChildren().OfType<CollectorDrone>()) d.SetPhysicsProcess(false);
            var initial = game.GetChildren().OfType<EnemyEcosystemEnemy>().Where(e => e.IsTerritorial).ToArray();
            Check(director.InitialWaveCount == new[] { 64, 120, 200, 300 }[Array.IndexOf(PlanetSizes.Radii, radius)],
                "Named-size population targets scale 64/120/200/300 across the globe");
            GD.Print($"Encounters R{radius}: {initial.Length}/{director.InitialWaveCount}, habitats {director.HabitatCount}, wild {initial.Count(e => e.HabitatId.StartsWith("wild-"))}, regions {initial.Select(e => PlanetLayout.RegionFor(arena.UpAt(e.GlobalPosition))).Distinct().Count()}");
            Check(initial.All(e => Mathf.IsEqualApprox(e.AwarenessRadius, 20 + radius * 0.05f)
                && e.RetaliationRadius > e.AwarenessRadius && e.DisengagementRadius >= e.RetaliationRadius
                && e.HabitatLeashRadius >= e.DisengagementRadius), "All sizes use wider coherent aggro/retaliation/escape/leash ranges");
            Check(initial.Length >= director.InitialWaveCount * 0.85 && initial.Length <= director.InitialWaveCount
                && initial.GroupBy(e => e.HabitatId).Count(g => g.Count() >= 2) >= 4,
                $"{radius}: increased initial population contains multiple stable groups ({initial.Length}/{director.InitialWaveCount})");
            Check(initial.Select(e => PlanetLayout.RegionFor(arena.UpAt(e.GlobalPosition))).Distinct().Count() >= 10
                && initial.Count(e => e.HabitatId.StartsWith("wild-")) > initial.Length / 2,
                "Most encounters are random wild groups distributed over at least ten regions");
            Check(initial.Any(e => e.IsHeavy && e.Health == 150) && initial.Where(e => e.Role == EnemyEcosystemEnemy.EnemyRole.FlyingSwarm)
                .All(e => e.Health == 16 && e.AntimatterReserve == 3), "Heavy beasts and easy low-reward swarm members are distinct");
            Check(initial.Where(e => e.Role == EnemyEcosystemEnemy.EnemyRole.FlyingSwarm).GroupBy(e => e.HabitatId).Any(g => g.Count() >= 6),
                "Each size contains full easy swarms of at least six rather than thinning every group to pairs");
            Check(initial.All(e => !e.IsRaider && arena.SurfaceDistance(e.GlobalPosition, home.GlobalPosition) >= director.EffectiveBaseExclusion
                && arena.SurfaceDistance(e.GlobalPosition, rival.GlobalPosition) >= director.EffectiveBaseExclusion), "All-size initial populations exclude every base");
            Check(!director.IsSpawnPositionValid(home.GlobalPosition) && !director.IsSpawnPositionValid(rival.GlobalPosition), "Placement rejects both faction bases");
            player.GlobalPosition = PlanetLayout.Position(arena, game.Layout!.Landmarks[4].Up, 8);
            bool spawned = director.TrySpawnNeutral();
            Check(spawned || initial.Length == director.MaxActiveEnemies, "Travelling pilot does not alter habitat placement or bypass population cap");
            var balanced = game.GetChildren().OfType<EnemyEcosystemEnemy>().Where(e => e.IsTerritorial).ToArray();
            foreach (var e in balanced) e.SetPhysicsProcess(false);
            Check(balanced.All(e => e.HabitatId.Length > 0) && balanced.GroupBy(e => e.HabitatId).All(g => g.Select(e => e.Role).Distinct().Count() == 1),
                "Groups retain stable habitat identity and a consistent role");
            for (int i = 0; i < 30; i++) director.TrySpawnNeutral();
            var all = game.GetChildren().OfType<EnemyEcosystemEnemy>().ToArray();
            foreach (var e in all) e.SetPhysicsProcess(false);
            Check(all.Count(e => e.IsTerritorial) <= director.MaxActiveEnemies
                && all.Where(e => e.IsTerritorial).GroupBy(e => e.HabitatId).All(g => g.Count() <= director.MaximumPerHabitat), "Global and per-habitat caps cannot overspawn");
            Check(all.Count(e => e.Role == EnemyEcosystemEnemy.EnemyRole.FlyingSwarm) <= director.MaxFlyingSwarms
                && all.Count(e => e.Role == EnemyEcosystemEnemy.EnemyRole.GroundBeast) <= director.MaxGroundBeasts
                && all.Count(e => e.Role == EnemyEcosystemEnemy.EnemyRole.RangedCaster) <= director.MaxRangedCasters
                && all.Count(e => e.Role == EnemyEcosystemEnemy.EnemyRole.AntimatterThief) <= director.MaxAntimatterThieves,
                "Fallback selections enforce every ordinary role cap");
            Check(GetTree().GetNodesInGroup("rival_ship").Count == 1 && !all.Any(e => e.IsRaider), "Ordinary spawning never creates rival ships or raiders");
            var depletedGroup = all.Where(e => e.IsTerritorial).GroupBy(e => e.HabitatId).Last(g => g.Count() >= 2).ToArray();
            var victim = depletedGroup[0]; string depleted = victim.HabitatId;
            player.GlobalPosition = home.GlobalPosition;
            game.GetNode<ShipFlightController>("RivalPilot").GlobalPosition = rival.GlobalPosition;
            int populationBefore = all.Count(e => e.IsTerritorial && e.HabitatId == depleted);
            victim.ApplyDamage(1000); depletedGroup[1].ApplyDamage(1000); await Frames(3);
            director._PhysicsProcess(19);
            Check(game.GetChildren().OfType<EnemyEcosystemEnemy>().Count(e => e.Health > 0 && e.HabitatId == depleted) == populationBefore - 2,
                "Depleted habitat does not replenish before its 20-second recovery delay");
            director._PhysicsProcess(1);
            Check(game.GetChildren().OfType<EnemyEcosystemEnemy>().Count(e => e.Health > 0 && e.HabitatId == depleted) == populationBefore - 1,
                "Depleted habitat replenishes one member independently after recovery delay");
            director._PhysicsProcess(4);
            Check(game.GetChildren().OfType<EnemyEcosystemEnemy>().Count(e => e.Health > 0 && e.HabitatId == depleted) == populationBefore - 1,
                "Group does not respawn all remaining members at once");
            director._PhysicsProcess(1);
            Check(game.GetChildren().OfType<EnemyEcosystemEnemy>().Count(e => e.Health > 0 && e.HabitatId == depleted) == populationBefore,
                "Remaining member returns five seconds after the first");
            foreach (var e in game.GetChildren().OfType<EnemyEcosystemEnemy>()) e.SetPhysicsProcess(false);
            // Guard replacement follows either faction's ownership, not player
            // proximity or the visible capture ring. Living guards remain.
            director.RaidsEnabled = false;
            foreach (var site in game.GetChildren().OfType<StrategicPoint>()) site.SetPhysicsProcess(false);
            var guarded = game.GetChildren().OfType<StrategicPoint>().First(site => game.GetChildren().OfType<EnemyEcosystemEnemy>()
                .Count(e => e.Health > 0 && e.HabitatId == $"route-{site.Name}") >= 2);
            string guardId = $"route-{guarded.Name}";
            var guards = game.GetChildren().OfType<EnemyEcosystemEnemy>().Where(e => e.Health > 0 && e.HabitatId == guardId).ToArray();
            guards[0].ApplyDamage(1000); await Frames(3);
            guarded.Advance(10, true, false); director._PhysicsProcess(60);
            Check(game.GetChildren().OfType<EnemyEcosystemEnemy>().Count(e => e.Health > 0 && e.HabitatId == guardId) == guards.Length - 1,
                "Player ownership suppresses replacements and leaves surviving guards intact");
            guarded.Advance(20, false, true); director._PhysicsProcess(60);
            Check(game.GetChildren().OfType<EnemyEcosystemEnemy>().Count(e => e.Health > 0 && e.HabitatId == guardId) == guards.Length - 1,
                "Rival ownership also suppresses guard replacements");
            guarded.Advance(10, true, false); director._PhysicsProcess(0.5);
            player.GlobalPosition = home.GlobalPosition;
            game.GetNode<ShipFlightController>("RivalPilot").GlobalPosition = rival.GlobalPosition;
            director._PhysicsProcess(19);
            Check(game.GetChildren().OfType<EnemyEcosystemEnemy>().Count(e => e.Health > 0 && e.HabitatId == guardId) == guards.Length - 1,
                "Neutralization restarts a full guard recovery delay");
            director._PhysicsProcess(1);
            Check(game.GetChildren().OfType<EnemyEcosystemEnemy>().Count(e => e.Health > 0 && e.HabitatId == guardId) == guards.Length,
                "Neutral point resumes its defeated guard replacements after recovery");
            director.RaidsEnabled = true;
            GetTree().Paused = true; float remaining = director.RaidRemaining;
            director._PhysicsProcess(200); Check(director.RaidRemaining == remaining && !director.TrySpawnNeutral(), "Pause stops raid clocks and population orders");
            GetTree().Paused = false;
            director._PhysicsProcess(director.RaidRemaining - 1);
            Check(!game.GetChildren().OfType<EnemyEcosystemEnemy>().Any(e => e.IsRaider), "Opening grace prevents initial base raids");
            director._PhysicsProcess(1.1);
            var raider = game.GetChildren().OfType<EnemyEcosystemEnemy>().Single(e => e.IsRaider); raider.SetPhysicsProcess(false);
            Check(raider.RaidTarget == home && director.SpawnedRaids == 1 && director.RaidFeedbackMessage.Contains("INBOUND"), "First rare raid has an explicit target and inbound feedback");
            Check(arena.SurfaceDistance(raider.GlobalPosition, home.GlobalPosition) >= director.EffectiveBaseExclusion,
                "Raider approaches from outside the spawn exclusion instead of appearing on the base");
            float raidDistance = arena.SurfaceDistance(raider.GlobalPosition, home.GlobalPosition);
            raider.SetPhysicsProcess(true); await Frames(45); raider.SetPhysicsProcess(false);
            Check(arena.SurfaceDistance(raider.GlobalPosition, home.GlobalPosition) < raidDistance - 0.5f,
                "Actual raider movement approaches its assigned base along the globe");
            director._PhysicsProcess(180);
            Check(game.GetChildren().OfType<EnemyEcosystemEnemy>().Count(e => e.IsRaider) == 1, "Active-raider cap blocks overlapping raids");
            int homeHp = home.Health, rivalHp = rival.Health;
            player.GlobalPosition = PlanetLayout.Position(arena, game.Layout.Landmarks[4].Up, 8);
            raider.GlobalPosition = arena.OnSurface(home.GlobalPosition + home.GlobalBasis.X * 6, 1.1f); raider._PhysicsProcess(0);
            Check(home.Health == homeHp - raider.ContactDamage && rival.Health == rivalHp, "Raider damages only its assigned base once per cooldown");
            raider._PhysicsProcess(0); Check(home.Health == homeHp - raider.ContactDamage, "Repeated attack ticks do not bypass raid cooldown");
            var tracker = game.GetNode<WorldAntimatterTracker>("WorldAntimatterTracker"); tracker.Refresh(); int total = tracker.TotalAntimatter;
            raider._PhysicsProcess(91); tracker.Refresh();
            Check(raider.IsQueuedForDeletion() && tracker.TotalAntimatter == total, "Raid expiry removes prolonged pressure and conserves its reserve");
            await Frames(3); director._PhysicsProcess(10.1);
            var second = game.GetChildren().OfType<EnemyEcosystemEnemy>().Single(e => e.IsRaider); second.SetPhysicsProcess(false);
            Check(second.RaidTarget == rival && director.SpawnedRaids == 2, "Successful raids alternate between living factions");
            // Keep pilots away so an ordinary sieger cannot accidentally aggro a nearby ship.
            var ordinary = new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.BaseAttacker,
                Position = arena.OnSurface(rival.GlobalPosition + rival.GlobalBasis.X * 6, 1.1f) };
            game.AddChild(ordinary); ordinary.SetPhysicsProcess(false);
            game.GetNode<ShipFlightController>("RivalPilot").GlobalPosition = PlanetLayout.Position(arena, game.Layout.Landmarks[0].Up, 8);
            rivalHp = rival.Health; ordinary._PhysicsProcess(2);
            Check(rival.Health == rivalHp, "An ordinary sieger does not automatically ram nearby bases");
            ordinary.Free();
            var stray = new EnemyProjectile { Damage = 10 }; game.AddChild(stray); stray.SetPhysicsProcess(false);
            stray.Launch(Vector3.Forward, initial[0]); homeHp = home.Health; Invoke(stray, "OnBodyEntered", home);
            Check(home.Health == homeHp, "Ordinary neutral projectile collateral cannot siege a base"); stray.Free();
            if (DisplayServer.GetName() != "headless")
            {
                player.GlobalPosition = arena.OnSurface(player.GlobalPosition + arena.Tangent(Vector3.Right, player.GlobalPosition).Normalized() * 30, 8);
                arena.Orient(player); player._PhysicsProcess(0);
                second.GlobalPosition = arena.OnSurface(player.GlobalPosition - player.GlobalBasis.Z * 16, 2);
                arena.Orient(second); arena.RefreshPlanetDetail();
                await Frames(30); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Check(GetViewport().GetTexture().GetImage().SavePng("C:/Users/lblan/AppData/Local/Temp/opencode/NeutralRaidGameplay.png") == Error.Ok,
                    "Rendered raider label and inbound HUD capture saves");
            }
            // Block every candidate without a nearby fallback; preserve roles/population.
            second.GlobalPosition = arena.OnSurface(rival.GlobalPosition + rival.GlobalBasis.X * 6, 1.1f);
            for (int i = 0; i < 31 && second.Health > 0; i++) second._PhysicsProcess(3);
            Check(!rival.IsDestroyed && rival.Health < rivalHp && second.IsQueuedForDeletion(),
                "A default unopposed raid expires before it can destroy an intact tier-one base");
            second.ApplyDamage(1000); await Frames(3);
            foreach (var mob in game.GetChildren().OfType<EnemyEcosystemEnemy>().Where(e => e.IsTerritorial).ToArray()) mob.ApplyDamage(1000);
            await Frames(3);
            director.BaseExclusionDistance = Mathf.Tau * radius;
            int count = game.GetChildren().OfType<EnemyEcosystemEnemy>().Count();
            Check(!director.TrySpawnNeutral(), "Fully excluded habitats do not use an unsafe fallback");
            director._PhysicsProcess(180);
            Check(game.GetChildren().OfType<EnemyEcosystemEnemy>().Count() == count && director.SpawnedRaids == 2,
                "Blocked raid placement allocates no actor and retries later");
            game.QueueFree(); await Frames(10);
        }
        PlanetSizes.SelectedIndex = 1;
    }
}

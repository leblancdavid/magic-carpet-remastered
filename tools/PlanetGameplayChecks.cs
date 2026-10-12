using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Game;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class PlanetGameplayChecks : Node3D
{
    private int _assertions;
    private Node3D _room = null!;
    private HeightmapArena _arena = null!;
    private ShipFlightController _player = null!;
    private void Check(bool condition, string message)
    {
        _assertions++;
        if (!condition) throw new Exception(message);
    }
    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }
    private T Add<T>(T node, Vector3 up, float clearance) where T : Node3D
    {
        node.Position = _arena.OnSurface(_arena.PlanetCenter + up.Normalized() * _arena.PlanetRadius, clearance);
        _room.AddChild(node);
        return node;
    }
    private static Vector3 Offset(Vector3 up, float distance, float radius, Vector3 tangent)
        => PlanetGeometry.TravelRotation(up, tangent, radius, distance) * up;
    private int LooseTotal()
    {
        int amount = 0;
        foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter"))
            if (node is AntimatterPickup orb && !orb.IsQueuedForDeletion()) amount += orb.AntimatterAmount;
        return amount;
    }
    private static void Cast(ShipFlightController player, string method)
        => typeof(ShipFlightController).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(player, null);

    private async Task CheckProjectileOwnerLifetime(Vector3 up)
    {
        EnemyEcosystemEnemy shooter = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.RivalShip }, up, 6);
        shooter.SetPhysicsProcess(false);
        EnemyProjectile flight = Add(new EnemyProjectile(), up, 30);
        flight.SetPhysicsProcess(false);
        flight.Launch(_arena.UpAt(flight.GlobalPosition), shooter);
        EnemyProjectile damaging = Add(new EnemyProjectile { Damage = 10 }, _arena.UpAt(_player.GlobalPosition), 10);
        damaging.SetPhysicsProcess(false);
        damaging.GlobalPosition = _player.GlobalPosition + _arena.UpAt(_player.GlobalPosition) * 4;
        damaging.Launch(-_arena.UpAt(_player.GlobalPosition), shooter);
        shooter.ApplyDamage(1000);
        await Frames(2);
        Check(!IsInstanceValid(shooter), "Regression shooter must actually be freed before projectile advances");
        Vector3 previous = flight.GlobalPosition;
        flight._PhysicsProcess(0.1);
        Check(!flight.IsQueuedForDeletion() && Mathf.Abs(flight.GlobalPosition.DistanceTo(previous) - 2.8f) < 0.001f,
            "Enemy projectile keeps moving after its shooter is disposed");
        int health = _player.Health;
        damaging._PhysicsProcess(0.2);
        Check(_player.Health == health - 10 && damaging.IsQueuedForDeletion(),
            "Ownerless enemy projectile still hits and damages its target");
        flight._PhysicsProcess(4);
        Check(flight.IsQueuedForDeletion(), "Ownerless enemy projectile retains lifetime expiry");

        ShipFlightController equipmentOwner = Add(new ShipFlightController(), Vector3.Right, 6);
        equipmentOwner.SetPhysicsProcess(false);
        WeaponProjectile plasmaBolt = Add(new WeaponProjectile(), Vector3.Right, 30);
        plasmaBolt.SetPhysicsProcess(false);
        plasmaBolt.Launch(_arena.UpAt(plasmaBolt.GlobalPosition), equipmentOwner);
        equipmentOwner.QueueFree();
        await Frames(2);
        Check(!IsInstanceValid(equipmentOwner), "Regression plasmaBolt owner must actually be freed");
        previous = plasmaBolt.GlobalPosition;
        plasmaBolt._PhysicsProcess(0.1);
        Check(!plasmaBolt.IsQueuedForDeletion() && Mathf.Abs(plasmaBolt.GlobalPosition.DistanceTo(previous) - 4.2f) < 0.001f,
            "Player plasmaBolt also survives a disposed caster");
        plasmaBolt._PhysicsProcess(2);
        Check(plasmaBolt.IsQueuedForDeletion(), "Ownerless plasmaBolt retains lifetime expiry");
        await Frames(2);
    }

    public override async void _Ready()
    {
        WorldSeeds.UseSeed(7319);
        try
        {
            Check(PlanetSizes.Radii.Length == 4 && PlanetSizes.Radii[0] == 100 && PlanetSizes.Radii[1] == 200
                && PlanetSizes.Radii[2] == 300 && PlanetSizes.Radii[3] == 400, "Approved compact size presets must match");
            foreach (int radius in PlanetSizes.Radii)
            {
                _room = new Node3D(); AddChild(_room);
                _arena = new HeightmapArena { IsSpherical = true, PlanetRadius = radius };
                _room.AddChild(_arena);
                foreach (Vector3 up in new[] { Vector3.Up, Vector3.Down, Vector3.Right, Vector3.Forward, new Vector3(1, 1, 0).Normalized() })
                {
                    Vector3 point = _arena.PlanetCenter + up * radius;
                    Vector3 surface = _arena.OnSurface(point, 6);
                    Check(Mathf.Abs(_arena.Clearance(surface) - 6) < 0.001f, "Surface-relative placement on all hemispheres");
                    Vector3 clamped = _arena.ClampFlightPosition(_arena.OnSurface(point, 50), 2);
                    Check(Mathf.Abs(_arena.Clearance(clamped) - 12) < 0.001f, "Radial flight ceiling");
                    clamped = _arena.ClampFlightPosition(_arena.OnSurface(point, -10), 2);
                    Check(Mathf.Abs(_arena.Clearance(clamped) - 2) < 0.001f, "Radial minimum clearance");
                    float before = _arena.HeightAt(point);
                    _arena.ApplyCrater(surface, 8, 2);
                    Check(Mathf.Abs(_arena.HeightAt(point) - (before - 2)) < 0.001f || before < 1, "Crater lowers radial terrain height");
                    _arena.RaiseTerrain(point, 8, 1);
                    Check(Mathf.Abs(_arena.HeightAt(point) - (Mathf.Max(_arena.WaterHeight - 2, before - 2) + 1)) < 0.001f, "Raise radial terrain");
                    _arena.FlattenTerrain(point, 8, 7);
                    Check(Mathf.Abs(_arena.HeightAt(point) - 7) < 0.001f, "Flatten is a radial elevation");
                    Check(_arena.TrySurfaceHit(_arena.OnSurface(point, 10), _arena.OnSurface(point, -4), out Vector3 hit)
                        && Mathf.Abs(_arena.Clearance(hit)) < 0.001f, "Surface ray fallback works on every hemisphere");
                }
                Vector3 seam = new Vector3(1, 1, 0).Normalized();
                Vector3 seamA = _arena.PlanetCenter + (seam + Vector3.Right * 0.000001f).Normalized() * radius;
                Vector3 seamB = _arena.PlanetCenter + (seam + Vector3.Up * 0.000001f).Normalized() * radius;
                Check(Mathf.Abs(_arena.HeightAt(seamA) - _arena.HeightAt(seamB)) < 0.001f, "Edits remain continuous at cube-face seams");
                await Frames(2);
                Check(_arena.LastRebuiltChunkCount > 0, "Deferred deformation rebuild must run");
                _room.QueueFree(); await Frames(2);
            }

            _room = new Node3D(); AddChild(_room);
            _arena = new HeightmapArena { IsSpherical = true, PlanetRadius = 800 };
            _room.AddChild(_arena);
            // Use the underside for all economic/combat tests: world-Y assumptions fail here.
            Vector3 underside = Vector3.Down;
            _arena.FlattenTerrain(_arena.PlanetCenter + underside * 800, 80, 7);
            FactionBase home = Add(new FactionBase { InitialStoredAntimatter = 30 }, underside, 2);
            Vector3 rivalUp = Offset(underside, 45, 800, Vector3.Right);
            FactionBase rival = Add(new FactionBase { IsPlayerBase = false, InitialStoredAntimatter = 48 }, rivalUp, 2);
            _player = Add(new ShipFlightController(), Offset(underside, 12, 800, Vector3.Forward), 6);
            _player.SetPhysicsProcess(false);
            Check(home.GlobalBasis.Y.Dot(underside) > 0.9999f && rival.GlobalBasis.Y.Dot(rivalUp) > 0.9999f, "Bases upright on underside");
            Check(_player.EnergyCapacity == 30, "Energy limit equals home storage on underside");
            await CheckProjectileOwnerLifetime(underside);

            AntimatterPickup falling = Add(new AntimatterPickup { AntimatterAmount = 10 }, Offset(underside, 20, 800, Vector3.Right), 6);
            falling.SpillAt(falling.GlobalPosition);
            _arena.RefreshPlanetDetail();
            await Frames(20);
            Check(falling.LinearVelocity.Dot(_arena.UpAt(falling.GlobalPosition)) < -1, "Antimatter accelerates inward on underside");
            await Frames(160);
            Check(Mathf.Abs(_arena.Clearance(falling.GlobalPosition) - falling.Radius) < 0.25f, "Antimatter settles on underside collision surface");
            Check(falling.GravityScale == 0 && falling.AntimatterAmount == 10, "Disable global gravity without losing antimatter");
            int power = _player.EnergyCapacity;
            _player.GlobalPosition = falling.GlobalPosition;
            await Frames(15);
            Check(falling.AntimatterAmount == 10 && _player.EnergyCapacity == power, "Player contact never collects physical antimatter");
            _player.GlobalPosition = _arena.OnSurface(home.GlobalPosition + home.GlobalBasis.X * 12, 6);

            AntimatterPickup mergeA = Add(new AntimatterPickup { AntimatterAmount = 5, Ownership = AntimatterOwnership.Player }, Offset(underside, 30, 800, Vector3.Forward), 1);
            AntimatterPickup mergeB = Add(new AntimatterPickup { AntimatterAmount = 7, Ownership = AntimatterOwnership.Player }, Offset(underside, 36, 800, Vector3.Forward), 1);
            float clusterDistance = mergeA.GlobalPosition.DistanceTo(mergeB.GlobalPosition);
            await Frames(60);
            Check(mergeA.GlobalPosition.DistanceTo(mergeB.GlobalPosition) < clusterDistance - 0.5f,
                "Radial surface magnetism visibly closes a gap on the underside");
            await Frames(30);
            Check(IsInstanceValid(mergeA) && IsInstanceValid(mergeB) && mergeA.IsAvailable && mergeB.IsAvailable,
                "Underside antimatter waits for sustained settled contact");
            await Frames(600);
            Check(LooseTotal() == 22, "Real radial-physics merging conserves antimatter");
            Check(!IsInstanceValid(mergeA) || !IsInstanceValid(mergeB) || mergeA.IsQueuedForDeletion() || mergeB.IsQueuedForDeletion(), "Same-owner orbs merge on underside");

            Vector3 seamUp = new Vector3(1, -1, 0).Normalized();
            Vector3 seamForward = new Vector3(1, 1, 0).Normalized();
            _arena.FlattenTerrain(_arena.PlanetCenter + seamUp * 800, 50, 7);
            Vector3 boltStartUp = Offset(seamUp, -12, 800, seamForward);
            AntimatterPickup target = Add(new AntimatterPickup { AntimatterAmount = 9 }, Offset(seamUp, 10, 800, seamForward), 0.5f);
            target.SetPhysicsProcess(false); target.Freeze = true;
            ClaimProjectile bolt = Add(new ClaimProjectile(), boltStartUp, 0.9f);
            bolt.SetPhysicsProcess(false);
            bolt.Launch(_arena.Tangent(seamForward, bolt.GlobalPosition).Normalized());
            bolt._PhysicsProcess(0.6);
            Check(target.Ownership == AntimatterOwnership.Player && target.AntimatterAmount == 9, "Claim traverses cube seam and preserves amount");
            target.QueueFree();

            int beforeDrop = LooseTotal();
            var existingAntimatter = GetTree().GetNodesInGroup("loose_antimatter").OfType<AntimatterPickup>().ToHashSet();
            Vector3 dropOrigin = _arena.OnSurface(_arena.PlanetCenter
                + Offset(underside, -35, 800, Vector3.Forward) * 800, 6);
            AntimatterScatter.Spawn(_room, dropOrigin, 12, name: "UndersideDropCheck");
            var deathDrops = GetTree().GetNodesInGroup("loose_antimatter").OfType<AntimatterPickup>()
                .Where(orb => !existingAntimatter.Contains(orb)).ToArray();
            Check(deathDrops.Length == 6 && deathDrops.All(orb => orb.LinearVelocity.IsZeroApprox()),
                "Underside death drop starts as six randomized fragments without launch impulses");
            Check(deathDrops.Count(orb => orb.IsDropPending) == 5,
                "Underside antimatter releases its first fragment immediately and stages the other five");
            var departures = deathDrops.ToDictionary(orb => orb, orb => orb.GlobalPosition);
            await Frames(30);
            Check(deathDrops.All(orb => orb.LinearVelocity.Dot(_arena.UpAt(orb.GlobalPosition)) < -1
                && orb.LinearVelocity.Slide(_arena.UpAt(orb.GlobalPosition)).Length() < 0.1f
                && _arena.Clearance(orb.GlobalPosition) < _arena.Clearance(departures[orb]) - 0.1f),
                "Death antimatter falls radially inward without tangential scatter on the globe underside");
            Check(LooseTotal() == beforeDrop + 12, "Radial straight-down death drop preserves the exact antimatter total");

            foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter")) if (node is AntimatterPickup orb) orb.QueueFree();
            await Frames(2);
            AntimatterPickup cargo = Add(new AntimatterPickup { AntimatterAmount = 10, Ownership = AntimatterOwnership.Player }, Offset(underside, 15, 800, Vector3.Right), 0.5f);
            CollectorDrone drone = Add(new CollectorDrone { CarryCapacity = 1, MoveSpeed = 20, MaxSeekRadius = 80 }, Offset(underside, 12, 800, Vector3.Right), 5);
            _arena.RefreshPlanetDetail();
            int storedBefore = home.StoredAntimatter;
            await Frames(180);
            Check(home.StoredAntimatter == storedBefore + 10, $"Drone scoop/carry/delivery on underside: stored {home.StoredAntimatter}");
            Check(_player.EnergyCapacity == 40, "Delivery raises energy limit to retained home storage");
            Check(drone.GlobalBasis.Y.Dot(_arena.UpAt(drone.GlobalPosition)) > 0.999f, "Drone stays upright while hauling");

            EquipmentDiscovery discovery = Add(new EquipmentDiscovery { EquipmentId = "plasma-bolt", EquipmentName = "Plasma Bolt" }, Offset(underside, 22, 800, Vector3.Back), 6);
            _player.GlobalPosition = _arena.OnSurface(discovery.GlobalPosition, 11);
            Check(!discovery.TryCollect(_player), "Legacy signals cannot bypass research on the globe");
            _player.GlobalPosition = _arena.OnSurface(home.GlobalPosition, 6);
            home.TryCompleteTierUpgrade(2);
            foreach (string id in new[] { "plasma-bolt", "energy-shield", "dash", "guardian-drone", "pulse-burst" })
            {
                home.DepositAntimatter(200);
                int rank = id == "plasma-bolt" ? 2 : 1;
                Check(home.Construction.TryStart(MagicCarpetRemastered.Scripts.Equipment.ShipEquipmentCatalog.ProjectId(id, rank), out _), "Research spherical equipment");
                home.Construction.Advance(100);
            }
            foreach (var item in new[] { ("plasma-bolt", 0), ("energy-shield", 1), ("dash", 2), ("guardian-drone", 3) })
                Check(_player.TryInstallEquipment(item.Item1, item.Item2, out _), "Install equipment in radial home column");
            home.WithdrawAntimatter(home.StoredAntimatter);
            home.ApplyDamage(140);
            home._PhysicsProcess(1000);
            home.DepositAntimatter(40);
            await Frames(2);
            Vector3 shotUp = Offset(underside, 35, 800, Vector3.Back);
            _arena.RefreshPlanetDetail();
            WeaponProjectile shot = Add(new WeaponProjectile { Damage = 35 }, shotUp, 7);
            shot.Launch(-_arena.UpAt(shot.GlobalPosition), _player);
            float oldHeight = _arena.HeightAt(shot.GlobalPosition);
            await Frames(30);
            Check(_arena.HeightAt(_arena.PlanetCenter + shotUp * 800) < oldHeight - 0.5f, "PlasmaBolt impact deforms spherical terrain");

            _player.RespawnAt(_arena.OnSurface(home.GlobalPosition + home.GlobalBasis.X * 12, 6));
            Cast(_player, "CastShieldEquipment");
            Check(_player.IsShielded && _player.Energy == _player.EnergyCapacity - 14, "Actual shield cast retains cost and duration");
            typeof(ShipFlightController).GetField("_damageInvulnerabilityTimer", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_player, 0f);
            _player.ApplyDamage(20);
            Check(_player.Health == 93, "Shield mitigation retained on underside");
            Cast(_player, "CastMobilityEquipment");
            Check(_arena.Tangent(_player.Velocity, _player.GlobalPosition).Length() > 30
                && _player.Velocity.Dot(_arena.UpAt(_player.GlobalPosition)) > 0, "Dash uses tangent speed plus radial lift");
            _player.RespawnAt(_player.GlobalPosition);
            Cast(_player, "CastSummonEquipment");
            GuardianDrone summoned = _room.GetChildren().OfType<GuardianDrone>().Last();
            summoned.SetPhysicsProcess(false);
            Check(_arena.Clearance(summoned.GlobalPosition) >= 1 && summoned.GlobalBasis.Y.Dot(_arena.UpAt(summoned.GlobalPosition)) > 0.999f, "Actual guardian spawn is upright and above radial surface");
            Vector3 guardianUp = _arena.UpAt(summoned.GlobalPosition);
            EnemyEcosystemEnemy guardianTarget = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.GroundBeast }, guardianUp, _arena.Clearance(summoned.GlobalPosition));
            guardianTarget.SetPhysicsProcess(false);
            int guardianTargetHealth = guardianTarget.Health;
            summoned._PhysicsProcess(0.1);
            Check(guardianTarget.Health == guardianTargetHealth - 12, "Guardian attacks still damage enemies on underside");
            summoned.QueueFree(); guardianTarget.QueueFree(); await Frames(2);

            Camera3D camera = _player.GetNode<Camera3D>("CameraPivot/Camera3D");
            Vector3 burstUp = Offset(underside, 28, 800, Vector3.Back);
            Vector3 burstPoint = _arena.OnSurface(_arena.PlanetCenter + burstUp * 800);
            camera.LookAt(burstPoint, _player.GlobalBasis.X);
            EnemyEcosystemEnemy burstTarget = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.GroundBeast }, burstUp, 1);
            burstTarget.SetPhysicsProcess(false);
            int burstHealth = burstTarget.Health;
            _player.RespawnAt(_player.GlobalPosition);
            Check(_player.TryInstallEquipment("pulse-burst", 3, out _), "Replace guardian with researched Burst at home");
            Cast(_player, "CastAreaEquipment");
            Check(burstTarget.Health == burstHealth - 32, "Actual Pulse Burst targets spherical ground and damages enemies");
            burstTarget.QueueFree(); await Frames(2);
            _player.Velocity = Vector3.Zero;

            foreach (Vector3 hemisphere in new[] { Vector3.Right, Vector3.Forward, Vector3.Down })
            {
                Vector3 flightUp = Offset(hemisphere, 35, 800,
                    Mathf.Abs(hemisphere.Dot(Vector3.Right)) < 0.9f ? Vector3.Right : Vector3.Forward);
                Vector3 flightStart = _arena.OnSurface(_arena.PlanetCenter + flightUp * 800);
                // This fixture verifies radial movement, not arbitrary terrain
                // climbability after earlier deformation probes.
                _arena.FlattenTerrain(flightStart, 50, _arena.SurfaceHeight(flightStart));
                _player.RespawnAt(_arena.OnSurface(flightStart, 2));
                Vector3 departure = _player.GlobalPosition;
                _arena.RefreshPlanetDetail();
                _player.SetPhysicsProcess(true);
                Input.ActionPress("move_forward");
                await Frames(60);
                Input.ActionRelease("move_forward");
                _player.SetPhysicsProcess(false);
                Check(_arena.SurfaceDistance(departure, _player.GlobalPosition) > 8, "Actual gameplay ship advances around each hemisphere");
                Check(_player.GlobalBasis.Y.Dot(_arena.UpAt(_player.GlobalPosition)) > 0.9999f
                    && _arena.Clearance(_player.GlobalPosition) >= 1.7f && _arena.Clearance(_player.GlobalPosition) <= 3,
                    $"Real flight maintains upright frame and clearance across faces ({hemisphere}, up {_player.GlobalBasis.Y.Dot(_arena.UpAt(_player.GlobalPosition))}, clearance {_arena.Clearance(_player.GlobalPosition)})");
                Check(_arena.Clearance(camera.GlobalPosition) >= 0.49f, "Gameplay chase camera remains outside planet terrain");
            }
            _player.Velocity = Vector3.Zero;

            EnemyEcosystemEnemy flyingPilot = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.RivalShip }, Vector3.Right, 6);
            _arena.RefreshPlanetDetail();
            Vector3 pilotDeparture = flyingPilot.GlobalPosition;
            await Frames(120);
            Check(_arena.SurfaceDistance(pilotDeparture, flyingPilot.GlobalPosition) > 10
                && flyingPilot.GlobalBasis.Y.Dot(_arena.UpAt(flyingPilot.GlobalPosition)) > 0.9999f,
                "Real pilot flight follows tangent route with radial upright orientation");
            Check(_arena.Clearance(flyingPilot.GlobalPosition) > 1 && _arena.Clearance(flyingPilot.GlobalPosition) <= 12.01f,
                "Real pilot altitude stays surface relative on distant hemisphere");
            flyingPilot.QueueFree(); await Frames(2);

            home.DepositAntimatter(50);
            int economicTotal = home.StoredAntimatter + LooseTotal();
            home.ApplyDamage(70);
            Check(home.StoredAntimatter == 60 && home.EffectiveStorageCapacity == 60, "Base damage reduces retained storage and spills excess");
            Check(home.StoredAntimatter + LooseTotal() == economicTotal && _player.EnergyCapacity == 60, "Radial hub spill conserves resources and reduces energy limit");
            foreach (Node node in GetTree().GetNodesInGroup("loose_antimatter"))
                if (node is AntimatterPickup orb && !orb.IsQueuedForDeletion())
                    Check(orb.LinearVelocity.IsZeroApprox()
                        && _arena.Clearance(orb.GlobalPosition) >= orb.Radius,
                        "Base fragments drop without launch impulse and retain underside surface clearance");
            home._PhysicsProcess(6);
            Check(home.Health == 73 && home.StoredAntimatter == 60, "Quiet hub repairs health without recreating spilled antimatter");

            EnemyEcosystemEnemy pilot = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.RivalShip }, rivalUp, 6);
            pilot.SetPhysicsProcess(false);
            LevelObjective objective = new() { PilotRespawnDelaySeconds = 0.05f };
            objective.Initialize(_player, home, rival, pilot);
            _room.AddChild(objective);
            objective.SetPhysicsProcess(false);
            _player.DamageInvulnerabilitySeconds = 0;
            typeof(ShipFlightController).GetField("_damageInvulnerabilityTimer", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_player, 0f);
            _player.ApplyDamage(1000);
            objective._PhysicsProcess(0.1);
            Check(_player.Health == 100 && _arena.Clearance(_player.GlobalPosition) >= 2, "Base-backed player respawn on underside");
            Check(_player.GlobalBasis.Y.Dot(_arena.UpAt(_player.GlobalPosition)) > 0.9999f, "Respawn restores radial upright frame");
            Check(_player.GetEquipmentRank("plasma-bolt") == 2, "Upgrade survives spherical respawn");
            pilot.ApplyDamage(1000);
            objective._PhysicsProcess(0.1);
            Check(!objective.RivalPilotDefeated, "Surviving rival hub respawns its pilot");
            EnemyEcosystemEnemy replacement = null!;
            foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
                if (node is EnemyEcosystemEnemy enemy && enemy.Health > 0) replacement = enemy;
            Check(replacement != null && _arena.Clearance(replacement.GlobalPosition) >= 1, "Rival replacement is placed against radial surface");
            replacement!.SetPhysicsProcess(false);
            rival.ApplyDamage(1000);
            replacement.ApplyDamage(1000);
            objective.Evaluate();
            Check(objective.State == LevelObjective.LevelState.Victory, "Base plus rival elimination wins on spherical world");
            LevelObjective defeat = new();
            EnemyEcosystemEnemy anotherRival = Add(new EnemyEcosystemEnemy { Role = EnemyEcosystemEnemy.EnemyRole.RivalShip }, rivalUp, 6);
            anotherRival.SetPhysicsProcess(false);
            defeat.Initialize(_player, home, rival, anotherRival);
            _room.AddChild(defeat); defeat.SetPhysicsProcess(false);
            home.ApplyDamage(1000);
            defeat.Evaluate();
            Check(defeat.State == LevelObjective.LevelState.Defeat, "Home hub destruction still causes defeat");
            _room.QueueFree(); await Frames(2);
            foreach (Node child in GetChildren()) child.QueueFree();
            await Frames(2);

            // Finally load the actual default gameplay scene and inspect its systems.
            PlanetSizes.SelectedIndex = 1;
            var game = GD.Load<PackedScene>("res://scenes/PlanetGame.tscn").Instantiate<MagicCarpetRemastered.Scripts.Game.Game>();
            AddChild(game);
            Check(game.UseSphericalWorld, "Default playable scene must enable spherical terrain");
            var arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
            Check(arena != null && arena.IsSpherical && arena.PlanetRadius == 200, "Compact Medium world is the default");
            Check(GetTree().GetNodesInGroup("equipment_discovery").Count == 0
                && GetTree().GetNodesInGroup("exploration_wreck").Count == 12, "Random wrecks replace legacy discovery beacons");
            Check(GetTree().GetNodesInGroup("drone").Count == 2, "Both hub drones present");
            Check(GetTree().GetNodesInGroup("ecosystem_enemy").Count >= 16 && GetTree().GetNodesInGroup("rival_ship").Count == 1,
                "Grouped initial creatures and one shared-mechanics rival ship present");
            Check(GetTree().GetNodesInGroup("level_objective").Count == 1 && GetTree().GetNodesInGroup("world_antimatter_tracker").Count == 1, "Objective and accounting present");
            Check(LooseTotal() > 0, "Initial neutral world antimatter present");
            await Frames(60);
            foreach (Node node in GetTree().GetNodesInGroup("ecosystem_enemy"))
                if (node is EnemyEcosystemEnemy enemy)
                    Check(arena!.Clearance(enemy.GlobalPosition) >= enemy.GroundClearance - 0.01f && arena.Clearance(enemy.GlobalPosition) <= 12.01f, "Ecosystem retains radial clearance bounds");
            var liveRival = game.GetNode<ShipFlightController>("RivalPilot");
            Check(arena!.Clearance(liveRival.GlobalPosition) >= 0.25f && liveRival.GlobalPosition.IsFinite()
                && liveRival.GlobalBasis.Y.Dot(arena.UpAt(liveRival.GlobalPosition)) > 0.999f,
                $"Autonomous shared hover ship preserves physical surface clearance and orientation (clearance {arena.Clearance(liveRival.GlobalPosition)})");
            foreach (Node child in game.GetChildren())
                if (child is PlanetSettings settings)
                {
                    settings._Input(new InputEventKey { Keycode = Key.F2, Pressed = true });
                    Check(GetTree().Paused, "Size menu pauses gameplay");
                    settings._Input(new InputEventKey { Keycode = Key.Escape, Pressed = true });
                    Check(!GetTree().Paused, "Closing menu restores gameplay");
                }
            foreach (int preset in new[] { 0, 2, 3 })
            {
                game.QueueFree();
                foreach (Node child in GetChildren()) child.QueueFree();
                await Frames(2);
                PlanetSizes.SelectedIndex = preset;
                game = GD.Load<PackedScene>("res://scenes/PlanetGame.tscn").Instantiate<MagicCarpetRemastered.Scripts.Game.Game>();
                AddChild(game);
                arena = GetTree().GetFirstNodeInGroup("heightmap_arena") as HeightmapArena;
                Check(arena?.PlanetRadius == PlanetSizes.Radii[preset], "Full gameplay scene respects selected size");
                Check(GetTree().GetNodesInGroup("player_base").Count == 1 && GetTree().GetNodesInGroup("enemy_base").Count == 1
                    && GetTree().GetNodesInGroup("drone").Count == 2 && GetTree().GetNodesInGroup("exploration_wreck").Count == 12,
                    "Every planet preset includes bases, drones, and upgrades");
                await Frames(2);
            }
            game.QueueFree(); foreach (Node child in GetChildren()) child.QueueFree(); await Frames(2);
            PlanetSizes.SelectedIndex = 1;
            game = GD.Load<PackedScene>("res://scenes/PlanetGame.tscn").Instantiate<MagicCarpetRemastered.Scripts.Game.Game>();
            AddChild(game);
            await Frames(60);
            foreach (string argument in OS.GetCmdlineUserArgs())
            {
                if (!argument.StartsWith("--capture=")) continue;
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Check(GetViewport().GetTexture().GetImage().SavePng(argument.Substring(10)) == Error.Ok, "Save rendered planet gameplay capture");
                PlanetSettings settings = game.GetChildren().OfType<PlanetSettings>().Single();
                settings._Input(new InputEventKey { Keycode = Key.F2, Pressed = true });
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Check(GetViewport().GetTexture().GetImage().SavePng(argument.Substring(10).Replace(".png", "-settings.png")) == Error.Ok,
                    "Save rendered size-menu capture");
                settings._Input(new InputEventKey { Keycode = Key.Escape, Pressed = true });
            }
            GD.Print($"Planet gameplay checks passed: {_assertions} assertions.");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Paused = false;
            GetTree().Quit(1);
        }
    }
}

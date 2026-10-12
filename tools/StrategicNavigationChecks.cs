using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Enemies;
using MagicCarpetRemastered.Scripts.Game;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Ui;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class StrategicNavigationChecks : Node
{
    private int _checks;
    private void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Callable.From(() => { _ = RunChecks(); }).CallDeferred();
    }

    private async Task RunChecks()
    {
        try
        {
            foreach (Vector3 up in new[] { Vector3.Up, Vector3.Down, Vector3.Right, Vector3.Left, Vector3.Forward, Vector3.Back,
                new Vector3(1, -1, 1).Normalized(), new Vector3(0.001f, 0.001f, 1).Normalized() })
                Check(MapProjection.Unproject(MapProjection.Project(up)).DistanceTo(up) < 0.0001f,
                    "World projection round-trips both hemispheres, seams and poles");
            Check(MapProjection.WrappedDifference(new Vector2(0.99f, 0.5f), new Vector2(0.01f, 0.5f)).DistanceTo(new Vector2(-0.02f, 0)) < 0.00001f,
                "Heading projection takes the short direction across the map seam");
            foreach (Vector3 up in new[] { Vector3.Up, Vector3.Down, Vector3.Right })
            {
                Vector3 forward = up.Cross(Vector3.Forward).Normalized();
                Basis frame = PlanetGeometry.Frame(up, forward);
                Vector3 next = PlanetGeometry.TravelRotation(up, forward, 800, 100) * up;
                Vector2 local = MapProjection.LocalOffset(next, frame, 800);
                Check(local.DistanceTo(Vector2.Up * 100) < 0.001f, "Local radar preserves great-circle forward direction and meters on every hemisphere");
            }
            foreach (int radius in PlanetSizes.Radii)
            {
                var geometry = new HeightmapArena { IsSpherical = true, PlanetRadius = radius };
                var sites = PlanetLayout.Sites(geometry);
                Check(sites.Length == 5 && sites.Select(site => site.EquipmentId).Distinct().Count() == 5,
                    "Every approved radius has five distinct landmark identities");
                Check(sites.All(site => Mathf.Abs(site.Up.Length() - 1) < 0.00001f), "Site directions remain unit-length");
                Check(Mathf.Acos(sites[^1].Up.Dot(Vector3.Up)) > 2, "Far-side upgrade is genuinely distributed beyond the starting hemisphere");
                geometry.Free();
            }

            PlanetSizes.SelectedIndex = 0;
            var game = GD.Load<PackedScene>("res://scenes/PlanetGame.tscn").Instantiate<MagicCarpetRemastered.Scripts.Game.Game>();
            GetTree().Root.AddChild(game);
            GetTree().CurrentScene = game;
            var arena = game.GetNode<HeightmapArena>("HeightmapArena");
            var player = game.GetNode<ShipFlightController>("PlayerShip");
            var director = game.GetNode<EnemyEcosystemDirector>("EnemyEcosystemDirector");
            var navigation = game.GetNode<WorldNavigation>("WorldNavigation");
            var map = game.GetNode<TacticalMap>("TacticalMap");
            var canvas = map.GetNode<TacticalMapCanvas>("MapCanvas");
            player.SetPhysicsProcess(false);
            director.SetPhysicsProcess(false);
            var landmarks = game.GetChildren().OfType<WorldLandmark>().ToArray();
            var wrecks = game.GetChildren().OfType<ExplorationWreck>().ToArray();
            Check(landmarks.Length == 5 && wrecks.Length == 12, "Live spherical scene creates landmarks and random exploration wrecks");
            Check(game.GetChildren().OfType<AntimatterPickup>().Count() == 18
                && game.GetChildren().OfType<AntimatterPickup>().Sum(orb => orb.AntimatterAmount) == 204,
                "Three four-globe resource fields plus the six-globe well conserve their authored 204 antimatter");
            foreach (var landmark in landmarks)
                Check(Mathf.Abs(arena.Clearance(landmark.GlobalPosition)) < 0.001f
                    && landmark.GlobalBasis.Y.Dot(arena.UpAt(landmark.GlobalPosition)) > 0.999f,
                    "Landmark is terrain/water grounded and radially upright");
            foreach (var drone in game.GetChildren().OfType<CollectorDrone>())
                Check(drone.MaxSeekRadius >= Mathf.Pi * arena.PlanetRadius, "Claimed far-side drops remain reachable by haulers");
            Check(navigation.Markers.Count(marker => marker.Kind == MapMarkerKind.Home) == 1
                && navigation.Markers.Count(marker => marker.Kind == MapMarkerKind.RivalBase) == 1, "Both bases are known from the start");
            Check(navigation.Markers.Count(marker => marker.Kind == MapMarkerKind.Landmark && !marker.Charted) == 5,
                "Unknown sites are chart hints without revealed names");
            navigation.SetWaypoint(-arena.UpAt(player.GlobalPosition), "Far side");
            Check(Mathf.Abs(navigation.TargetDistance - Mathf.Pi * arena.PlanetRadius) < 1
                && float.IsFinite(navigation.TargetDistance), "Antipodal waypoint distance equals half the circumference rather than world X/Z");
            navigation.SetWaypoint(Vector3.Zero);
            Check(navigation.WaypointName == "Far side", "Invalid waypoint direction cannot corrupt navigation");
            navigation.ClearWaypoint();
            Check(!navigation.WaypointUp.HasValue && navigation.TargetName == "Home base", "Clearing a waypoint restores home guidance");

            var remoteSite = landmarks.Single(landmark => landmark.SiteId == "observatory");
            AntimatterPickup AddOrb(Vector3 position, int amount, AntimatterOwnership ownership)
            {
                var orb = new AntimatterPickup { Position = arena.OnSurface(position, 1), AntimatterAmount = amount, Ownership = ownership };
                game.AddChild(orb);
                orb.SetPhysicsProcess(false); orb.Freeze = true;
                return orb;
            }
            var nearby = AddOrb(player.GlobalPosition, 77, AntimatterOwnership.Neutral);
            var hidden = AddOrb(remoteSite.GlobalPosition, 98, AntimatterOwnership.Neutral);
            var owned = AddOrb(remoteSite.GlobalPosition + remoteSite.GlobalBasis.X * 8, 123, AntimatterOwnership.Player);
            navigation.Refresh();
            Check(navigation.Markers.Any(marker => marker.Title == "Neutral antimatter · 77"), "Nearby neutral antimatter is revealed");
            Check(!navigation.Markers.Any(marker => marker.Title == "Neutral antimatter · 98"), "Remote neutral antimatter is not globally revealed");
            Check(navigation.Markers.Any(marker => marker.Title == "Player antimatter · 123"), "Owned loose antimatter remains trackable on the far side");
            MapMarker liveMarker = navigation.Markers.Single(marker => marker.Title == "Neutral antimatter · 77");
            Vector3 snapshotUp = liveMarker.Up;
            nearby.GlobalPosition = arena.OnSurface(player.GlobalPosition + player.GlobalBasis.X * 12, 1);
            Check(liveMarker.TryGetLiveUp(arena, out Vector3 liveUp)
                && liveUp.DistanceTo(snapshotUp) > 0.01f
                && liveUp.DistanceTo(arena.UpAt(nearby.GlobalPosition)) < 0.00001f,
                "Radar follows moving actors between visibility snapshots without a group refresh");
            Check(Mathf.Abs(navigation.TargetDistance - arena.SurfaceDistance(player.GlobalPosition, navigation.WorldPosition(navigation.TargetUp))) < 0.01f,
                "Terrain-free guidance retains accurate great-circle target distance");
            var disposable = AddOrb(player.GlobalPosition, 7, AntimatterOwnership.Player);
            navigation.Refresh();
            MapMarker disposedMarker = navigation.Markers.Single(marker => marker.Actor == disposable);
            disposable.Free();
            Check(!disposedMarker.TryGetLiveUp(arena, out _), "Radar safely rejects disposed actor wrappers between snapshots");
            var hauler = game.GetNode<CollectorDrone>("PlayerDrone");
            hauler.SetPhysicsProcess(false);
            float distanceBefore = arena.SurfaceDistance(hauler.GlobalPosition, owned.GlobalPosition);
            hauler._PhysicsProcess(0.5);
            Check(distanceBefore > arena.PlanetRadius * 0.8f && arena.SurfaceDistance(hauler.GlobalPosition, owned.GlobalPosition) < distanceBefore,
                "Actual hauler starts a curved route to claimed far-side antimatter rather than stranding it");
            nearby.ReassignOwnership(AntimatterOwnership.Enemy);
            navigation.Refresh();
            Check(navigation.Markers.Any(marker => marker.Title == "Enemy antimatter · 77"), "Map ownership changes on claim/contesting");
            nearby.ScheduleDrop(nearby.GlobalPosition, 1);
            Check(!liveMarker.TryGetLiveUp(arena, out _), "Pending drops disappear from live radar before the next visibility scan");
            navigation.Refresh();
            Check(!navigation.Markers.Any(marker => marker.Title.EndsWith("· 77")), "Hidden pending fragments do not appear as available resources");
            owned.BeginCarry(game.GetNode<CollectorDrone>("PlayerDrone"), Vector3.Zero);
            Check(!navigation.Markers.Single(marker => marker.Actor == owned).TryGetLiveUp(arena, out _),
                "Carried antimatter immediately stops drawing between visibility scans");
            navigation.Refresh();
            Check(!navigation.Markers.Any(marker => marker.Title == "Player antimatter · 123"), "Carried antimatter is represented by drone cargo rather than duplicate loose markers");

            Vector3 start = player.GlobalPosition;
            player.GlobalPosition = arena.OnSurface(remoteSite.GlobalPosition, 8);
            remoteSite._PhysicsProcess(1);
            navigation.Refresh();
            Check(remoteSite.IsDiscovered && navigation.Markers.Any(marker => marker.Title.StartsWith("Starfall Observatory")),
                "Visiting a landmark reveals its name and habitat");
            var chartedWreck = wrecks[0];
            chartedWreck.GlobalPosition = arena.OnSurface(remoteSite.GlobalPosition + remoteSite.GlobalBasis.X * 20, 1);
            navigation.Refresh();
            Check(navigation.Markers.Any(marker => marker.Actor == chartedWreck && marker.Kind == MapMarkerKind.Wreck), "Nearby wreck becomes charted");
            Check(navigation.Markers.Any(marker => marker.Title == "Neutral antimatter · 98"), "Remote resources reveal when approached on the other hemisphere");
            Check(director.TrySpawnNeutral(), "Stable habitat populations can fill an empty region while the player explores");
            var mobs = game.GetChildren().OfType<EnemyEcosystemEnemy>().Where(e => e.IsTerritorial).ToArray();
            Check(mobs.Select(e => e.HabitatId).Distinct().Count() == 5, "Initial replacements balance stable habitats rather than following the player");
            Check(mobs.All(e => e.Territory != null && arena.SurfaceDistance(e.Territory.Home, e.GlobalPosition) < 41),
                "Habitat spawning uses local radial coordinates on all hemispheres");
            player.GlobalPosition = start;
            navigation.Refresh();
            Check(navigation.Markers.Any(marker => marker.Actor == chartedWreck), "Discovered wreck remains charted after leaving the site");
            chartedWreck.ApplyDamage(50); navigation.Refresh();
            Check(navigation.Markers.Any(marker => marker.Actor == chartedWreck && marker.Kind == MapMarkerKind.Reward),
                "Charted intact wreck changes to a distinct revealed salvage marker");

            var previousMouse = Input.MouseMode;
            Check(map.SetExpanded(true) && !GetTree().Paused && player.GameplayInputBlocked && Input.MouseMode == Input.MouseModeEnum.Visible,
                "Expanded world map is real time and owns ship input/cursor");
            Check(!canvas.SelectDestination(Vector2.Zero), "Clicks outside the survey cannot create invalid waypoints");
            Vector2 click = canvas.SurveyRect.Position + MapProjection.Project(arena.UpAt(remoteSite.GlobalPosition)) * canvas.SurveyRect.Size;
            Check(canvas.SelectDestination(click) && navigation.WaypointName == "Starfall Observatory", "Clicking a charted landmark selects its waypoint");
            float raidTime = director.RaidRemaining;
            director.SetPhysicsProcess(true);
            player.SetPhysicsProcess(true);
            await Frames(4);
            Check(director.RaidRemaining < raidTime && arena.Tangent(player.Velocity, player.GlobalPosition).Length() < 0.1f,
                "Real-time survey advances raid pacing while steering input is blocked and hover/gravity remain live");
            director.SetPhysicsProcess(false);
            player.SetPhysicsProcess(false);

            string? capture = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--capture="))?.Substring(10);
            if (capture != null)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Check(image.SavePng(capture) == Error.Ok, "Rendered world-survey capture saves");
            }
            Check(map.SetExpanded(false) && !GetTree().Paused && Input.MouseMode == previousMouse,
                "Closing map restores the previous mouse mode without changing pause");
            GetTree().Paused = true;
            Check(!map.SetExpanded(true) && GetTree().Paused, "Map cannot steal pause from another menu or terminal result");
            GetTree().Paused = false;
            if (capture != null)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                string radar = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(capture)!, System.IO.Path.GetFileNameWithoutExtension(capture) + "_radar.png");
                Check(image.SavePng(radar) == Error.Ok, "Rendered local-radar capture saves");
                // Do not synthesize modal input/reload reentrantly from a
                // FramePostDraw callback while render dirty lists are flushing.
                await Frames(2);
            }
            GetViewport().PushInput(new InputEventKey { Keycode = Key.M, Pressed = true });
            await Frames(2);
            Check(map.IsExpanded && !GetTree().Paused && player.GameplayInputBlocked, "Actual M input opens the real-time modal world map");
            GetViewport().PushInput(new InputEventKey { Keycode = Key.M, Pressed = false });
            GetViewport().PushInput(new InputEventKey { Keycode = Key.F2, Pressed = true });
            await Frames(2);
            Check(map.IsExpanded && !GetTree().Paused, "Map consumes size-menu input while it owns interface input");
            GetViewport().PushInput(new InputEventKey { Keycode = Key.F2, Pressed = false });
            GetViewport().PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = true });
            await Frames(2);
            Check(!map.IsExpanded && !GetTree().Paused, "Actual Escape input closes only the map and resumes the match");
            GetViewport().PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = false });
            foreach (var orb in new[] { nearby, hidden, owned }) orb.QueueFree();
            Check(!chartedWreck.TryCollect(player), "Remote revealed salvage requires actual proximity");
            Check(player.GetEquipmentRank("guardian-drone") == 0, "Exploration does not install equipment");
            navigation.SetWaypoint(Vector3.Down);
            using var boonRandom = new RandomNumberGenerator { Seed = 37 };
            var firstOffer = player.Boons.SecureOffer(boonRandom, 30);
            player.Boons.Choose(firstOffer.Id, firstOffer.Choices[0].Id);
            player.Boons.SecureOffer(boonRandom, 30);
            game.GetNode<BoonChoices>("BoonChoices").SetProcess(false);
            map.SetExpanded(true);
            // Scene restart from an open map must not leave the next match paused.
            map._Input(new InputEventKey { Keycode = Key.R, Pressed = true });
            await ToSignal(GetTree(), SceneTree.SignalName.SceneChanged);
            var restarted = (MagicCarpetRemastered.Scripts.Game.Game)GetTree().CurrentScene;
            var newNavigation = restarted.GetNode<WorldNavigation>("WorldNavigation");
            Check(!GetTree().Paused && !newNavigation.WaypointUp.HasValue
                && restarted.GetChildren().OfType<WorldLandmark>().All(landmark => !landmark.IsDiscovered), "Restart clears waypoint/chart knowledge and restores gameplay");
            Check(restarted.GetNode<ShipFlightController>("PlayerShip").GetEquipmentRank("guardian-drone") == 0,
                "Restart resets upgrades alongside the strategic layout");
            Check(restarted.GetNode<ShipFlightController>("PlayerShip").Boons.Pending.Count == 0
                && restarted.GetNode<ShipFlightController>("PlayerShip").Boons.Summary.Length == 0,
                "Actual scene restart clears pending offers and acquired boon stacks");
            Check(!restarted.GetNode<BaseManagement>("BaseManagement").IsOpen
                && !restarted.GetNode<ShipFlightController>("PlayerShip").GameplayInputBlocked
                && restarted.GetNode<FactionBase>("PlayerBase").Construction.ActiveInvestment == 0,
                "Restart clears management state and restores ship input");
            GD.Print($"Strategic navigation checks passed: {_checks} assertions.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Paused = false;
            GetTree().Quit(1);
        }
    }
}

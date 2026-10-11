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
                Check(sites.Length == 5 && sites.Select(site => site.SpellId).Distinct().Count() == 5,
                    "Every approved radius has five distinct sites and spell upgrades");
                Check(sites.All(site => Mathf.Abs(site.Up.Length() - 1) < 0.00001f), "Site directions remain unit-length");
                Check(Mathf.Acos(sites[^1].Up.Dot(Vector3.Up)) > 2, "Far-side upgrade is genuinely distributed beyond the starting hemisphere");
                geometry.Free();
            }

            PlanetSizes.SelectedIndex = 0;
            var game = GD.Load<PackedScene>("res://scenes/PlanetGame.tscn").Instantiate<MagicCarpetRemastered.Scripts.Game.Game>();
            GetTree().Root.AddChild(game);
            GetTree().CurrentScene = game;
            var arena = game.GetNode<HeightmapArena>("HeightmapArena");
            var player = game.GetNode<CarpetFlightController>("PlayerCarpet");
            var director = game.GetNode<EnemyEcosystemDirector>("EnemyEcosystemDirector");
            var navigation = game.GetNode<WorldNavigation>("WorldNavigation");
            var map = game.GetNode<TacticalMap>("TacticalMap");
            var canvas = map.GetNode<TacticalMapCanvas>("MapCanvas");
            player.SetPhysicsProcess(false);
            director.SetPhysicsProcess(false);
            var landmarks = game.GetChildren().OfType<WorldLandmark>().ToArray();
            var discoveries = game.GetChildren().OfType<SpellDiscovery>().ToArray();
            Check(landmarks.Length == 5 && discoveries.Length == 5, "Live spherical scene creates all landmarks and one upgrade at each");
            Check(game.GetChildren().OfType<ManaPickup>().Count() == 18
                && game.GetChildren().OfType<ManaPickup>().Sum(orb => orb.ManaAmount) == 204,
                "Three four-globe resource fields plus the six-globe well conserve their authored 204 mana");
            foreach (var landmark in landmarks)
                Check(Mathf.Abs(arena.Clearance(landmark.GlobalPosition)) < 0.001f
                    && landmark.GlobalBasis.Y.Dot(arena.UpAt(landmark.GlobalPosition)) > 0.999f,
                    "Landmark is terrain/water grounded and radially upright");
            foreach (var balloon in game.GetChildren().OfType<ManaBalloon>())
                Check(balloon.MaxSeekRadius >= Mathf.Pi * arena.PlanetRadius, "Claimed far-side drops remain reachable by haulers");
            Check(navigation.Markers.Count(marker => marker.Kind == MapMarkerKind.Home) == 1
                && navigation.Markers.Count(marker => marker.Kind == MapMarkerKind.RivalCastle) == 1, "Both castles are known from the start");
            Check(navigation.Markers.Count(marker => marker.Kind == MapMarkerKind.Landmark && !marker.Charted) == 5,
                "Unknown sites are chart hints without revealed names");
            navigation.SetWaypoint(Vector3.Down, "Far side");
            Check(navigation.TargetDistance > 1200 && float.IsFinite(navigation.TargetDistance), "Waypoint distance uses the spherical surface rather than world X/Z");
            navigation.SetWaypoint(Vector3.Zero);
            Check(navigation.WaypointName == "Far side", "Invalid waypoint direction cannot corrupt navigation");
            navigation.ClearWaypoint();
            Check(!navigation.WaypointUp.HasValue && navigation.TargetName == "Home castle", "Clearing a waypoint restores home guidance");

            var remoteSite = landmarks.Single(landmark => landmark.SiteId == "observatory");
            ManaPickup AddOrb(Vector3 position, int amount, ManaOwnership ownership)
            {
                var orb = new ManaPickup { Position = arena.OnSurface(position, 1), ManaAmount = amount, Ownership = ownership };
                game.AddChild(orb);
                orb.SetPhysicsProcess(false); orb.Freeze = true;
                return orb;
            }
            var nearby = AddOrb(player.GlobalPosition, 77, ManaOwnership.Neutral);
            var hidden = AddOrb(remoteSite.GlobalPosition, 98, ManaOwnership.Neutral);
            var owned = AddOrb(remoteSite.GlobalPosition + remoteSite.GlobalBasis.X * 8, 123, ManaOwnership.Player);
            navigation.Refresh();
            Check(navigation.Markers.Any(marker => marker.Title == "Neutral mana · 77"), "Nearby neutral mana is revealed");
            Check(!navigation.Markers.Any(marker => marker.Title == "Neutral mana · 98"), "Remote neutral mana is not globally revealed");
            Check(navigation.Markers.Any(marker => marker.Title == "Player mana · 123"), "Owned loose mana remains trackable on the far side");
            MapMarker liveMarker = navigation.Markers.Single(marker => marker.Title == "Neutral mana · 77");
            Vector3 snapshotUp = liveMarker.Up;
            nearby.GlobalPosition = arena.OnSurface(player.GlobalPosition + player.GlobalBasis.X * 12, 1);
            Check(liveMarker.TryGetLiveUp(arena, out Vector3 liveUp)
                && liveUp.DistanceTo(snapshotUp) > 0.01f
                && liveUp.DistanceTo(arena.UpAt(nearby.GlobalPosition)) < 0.00001f,
                "Radar follows moving actors between visibility snapshots without a group refresh");
            Check(Mathf.Abs(navigation.TargetDistance - arena.SurfaceDistance(player.GlobalPosition, navigation.WorldPosition(navigation.TargetUp))) < 0.01f,
                "Terrain-free guidance retains accurate great-circle target distance");
            var disposable = AddOrb(player.GlobalPosition, 7, ManaOwnership.Player);
            navigation.Refresh();
            MapMarker disposedMarker = navigation.Markers.Single(marker => marker.Actor == disposable);
            disposable.Free();
            Check(!disposedMarker.TryGetLiveUp(arena, out _), "Radar safely rejects disposed actor wrappers between snapshots");
            var hauler = game.GetNode<ManaBalloon>("PlayerBalloon");
            hauler.SetPhysicsProcess(false);
            float distanceBefore = arena.SurfaceDistance(hauler.GlobalPosition, owned.GlobalPosition);
            hauler._PhysicsProcess(0.5);
            Check(distanceBefore > 350 && arena.SurfaceDistance(hauler.GlobalPosition, owned.GlobalPosition) < distanceBefore,
                "Actual hauler starts a curved route to possessed far-side mana rather than stranding it");
            nearby.ReassignOwnership(ManaOwnership.Enemy);
            navigation.Refresh();
            Check(navigation.Markers.Any(marker => marker.Title == "Enemy mana · 77"), "Map ownership changes on possession/contesting");
            nearby.ScheduleDrop(nearby.GlobalPosition, 1);
            Check(!liveMarker.TryGetLiveUp(arena, out _), "Pending drops disappear from live radar before the next visibility scan");
            navigation.Refresh();
            Check(!navigation.Markers.Any(marker => marker.Title.EndsWith("· 77")), "Hidden pending fragments do not appear as available resources");
            owned.BeginCarry(game.GetNode<ManaBalloon>("PlayerBalloon"), Vector3.Zero);
            Check(!navigation.Markers.Single(marker => marker.Actor == owned).TryGetLiveUp(arena, out _),
                "Carried mana immediately stops drawing between visibility scans");
            navigation.Refresh();
            Check(!navigation.Markers.Any(marker => marker.Title == "Player mana · 123"), "Carried mana is represented by balloon cargo rather than duplicate loose markers");

            Vector3 start = player.GlobalPosition;
            player.GlobalPosition = arena.OnSurface(remoteSite.GlobalPosition, 8);
            remoteSite._PhysicsProcess(1);
            navigation.Refresh();
            Check(remoteSite.IsDiscovered && navigation.Markers.Any(marker => marker.Title.StartsWith("Starfall Observatory")),
                "Visiting a landmark reveals its name and habitat");
            Check(navigation.Markers.Any(marker => marker.Title == "Guardian II"), "Discovered landmark exposes its uncollected upgrade");
            Check(navigation.Markers.Any(marker => marker.Title == "Neutral mana · 98"), "Remote resources reveal when approached on the other hemisphere");
            object zone = typeof(EnemyEcosystemDirector).GetMethod("SelectSpawnZone", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(director, null)!;
            var habitat = (WorldLandmark?)typeof(EnemyEcosystemDirector).GetField("_habitat", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(director);
            Check(habitat == remoteSite, "Ecosystem selects the nearest visited habitat");
            object siegeZone = Activator.CreateInstance(zone.GetType(), new object[] { "Castle Ruins", Vector3.Zero, 0.85f })!;
            var roleMethod = typeof(EnemyEcosystemDirector).GetMethod("SelectRole", BindingFlags.NonPublic | BindingFlags.Instance)!;
            GD.Seed(901);
            int favored = 0;
            for (int i = 0; i < 100; i++)
                if ((EnemyEcosystemEnemy.EnemyRole)roleMethod.Invoke(director, new object[] { siegeZone, false, 0 })! == remoteSite.Habitat) favored++;
            Check(favored >= 45 && favored <= 85, "Habitat bias applies even to high-pressure zones while preserving encounter variation");
            Vector3 spawn = (Vector3)typeof(EnemyEcosystemDirector).GetMethod("SpawnPosition", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(director, new object[] { zone, EnemyEcosystemEnemy.EnemyRole.RangedCaster })!;
            Check(arena.SurfaceDistance(remoteSite.GlobalPosition, spawn) > 23 && arena.SurfaceDistance(remoteSite.GlobalPosition, spawn) < 41
                && Mathf.Abs(arena.Clearance(spawn) - 5.5f) < 0.001f, "Habitat spawning uses its radial local frame on the far side");
            player.GlobalPosition = start;
            navigation.Refresh();
            Check(navigation.Markers.Any(marker => marker.Title == "Guardian II"), "Discovered upgrade remains charted after leaving the site");

            var previousMouse = Input.MouseMode;
            Check(map.SetExpanded(true) && GetTree().Paused && Input.MouseMode == Input.MouseModeEnum.Visible,
                "Expanded world map pauses combat and releases the cursor");
            Check(!canvas.SelectDestination(Vector2.Zero), "Clicks outside the survey cannot create invalid waypoints");
            Vector2 click = canvas.SurveyRect.Position + MapProjection.Project(arena.UpAt(remoteSite.GlobalPosition)) * canvas.SurveyRect.Size;
            Check(canvas.SelectDestination(click) && navigation.WaypointName == "Starfall Observatory", "Clicking a charted landmark selects its waypoint");
            float intensity = director.Intensity;
            director.SetPhysicsProcess(true);
            player.SetPhysicsProcess(true);
            await Frames(4);
            Check(director.Intensity == intensity && player.GlobalPosition == start, "Paused navigation leaves gameplay frozen");
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
                "Closing map resumes combat and restores the previous mouse mode");
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
            Check(map.IsExpanded && GetTree().Paused, "Actual M input opens the modal world map");
            GetViewport().PushInput(new InputEventKey { Keycode = Key.M, Pressed = false });
            GetViewport().PushInput(new InputEventKey { Keycode = Key.F2, Pressed = true });
            await Frames(2);
            Check(map.IsExpanded && GetTree().Paused, "Map consumes size-menu input while it owns pause");
            GetViewport().PushInput(new InputEventKey { Keycode = Key.F2, Pressed = false });
            GetViewport().PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = true });
            await Frames(2);
            Check(!map.IsExpanded && !GetTree().Paused, "Actual Escape input closes only the map and resumes the match");
            GetViewport().PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = false });
            foreach (var orb in new[] { nearby, hidden, owned }) orb.QueueFree();
            Check(discoveries.Single(discovery => discovery.SpellId == "guardian").TryCollect(TeleportForCollection()),
                "Far-side upgrade remains physically collectible");
            CarpetFlightController TeleportForCollection()
            {
                player.GlobalPosition = discoveries.Single(discovery => discovery.SpellId == "guardian").GlobalPosition;
                return player;
            }
            Check(player.GetSpellRank("guardian") == 2, "Far-side collection grants the existing rank-II effect");
            navigation.SetWaypoint(Vector3.Down);
            map.SetExpanded(true);
            // Scene restart from an open map must not leave the next match paused.
            map._Input(new InputEventKey { Keycode = Key.R, Pressed = true });
            await ToSignal(GetTree(), SceneTree.SignalName.SceneChanged);
            var restarted = (MagicCarpetRemastered.Scripts.Game.Game)GetTree().CurrentScene;
            var newNavigation = restarted.GetNode<WorldNavigation>("WorldNavigation");
            Check(!GetTree().Paused && !newNavigation.WaypointUp.HasValue
                && restarted.GetChildren().OfType<WorldLandmark>().All(landmark => !landmark.IsDiscovered), "Restart clears waypoint/chart knowledge and restores gameplay");
            Check(restarted.GetNode<CarpetFlightController>("PlayerCarpet").GetSpellRank("guardian") == 1,
                "Restart resets upgrades alongside the strategic layout");
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

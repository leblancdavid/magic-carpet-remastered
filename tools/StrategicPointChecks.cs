using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Ui;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class StrategicPointChecks : Node3D
{
    private int _checks;
    private void Trace(string stage) { if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--trace") >= 0) GD.Print($"Capture checks: {stage} ({_checks})"); }
    public override void _Ready() => Callable.From(() => { _ = RunChecks(); }).CallDeferred();
    private void Check(bool result, string message) { _checks++; if (!result) throw new Exception(message); }
    private async Task Frames(int count)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    private static void Invoke(object actor, string method, params object[] args)
        => actor.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(actor, args);

    private async Task RunChecks()
    {
        try
        {
            foreach (bool spherical in new[] { false, true })
            {
                Trace($"fixture {spherical}");
                var room = new Node3D(); AddChild(room);
                var arena = new HeightmapArena { IsSpherical = spherical, PlanetRadius = 500 }; room.AddChild(arena);
                Vector3 origin = spherical ? arena.PlanetCenter + Vector3.Down * 500 : new Vector3(70, 0, 70);
                var home = new FactionBase { Position = arena.OnSurface(origin), InitialStoredAntimatter = 80 }; room.AddChild(home); home.SetPhysicsProcess(false);
                var rival = new FactionBase { IsPlayerBase = false, Position = arena.OnSurface(origin + Vector3.Right * 100), InitialStoredAntimatter = 80 };
                room.AddChild(rival); rival.SetPhysicsProcess(false);
                var player = new ShipFlightController { Position = arena.OnSurface(origin, 8), EnergyRegenFractionPerSecond = 0 };
                room.AddChild(player); player.SetPhysicsProcess(false);
                int currentEnergy = player.Energy;
                var extractor = new StrategicPoint { Position = arena.OnSurface(origin), CaptureSeconds = 10 };
                room.AddChild(extractor); extractor.SetPhysicsProcess(false);
                extractor.Advance(5, true, false);
                Check(extractor.Ownership == AntimatterOwnership.Neutral && extractor.CaptureProgress == 0.5 && home.StoredAntimatter == 80,
                    "Incomplete neutral capture provides no income");
                extractor.Advance(20, true, true);
                Check(extractor.IsContested && extractor.CaptureProgress == 0.5 && extractor.CaptureCount == 0, "Opposing presence freezes progress");
                extractor.Advance(1, false, false);
                Check(extractor.CaptureProgress == 0 && extractor.Claimant == AntimatterOwnership.Neutral, "Departure resets progress");
                extractor.Advance(10, true, false);
                Check(extractor.Ownership == AntimatterOwnership.Player && extractor.CaptureCount == 1, "Full dwell captures exactly once");
                extractor.Advance(0.25, true, false); extractor.Advance(0.25, true, false);
                player._PhysicsProcess(0);
                Check(home.StoredAntimatter == 81 && extractor.GeneratedAntimatter == 1 && player.Energy == currentEnergy && player.EnergyCapacity == 81,
                    "Fractional income creates only accepted supply and raises limit without refilling current energy");
                extractor.Advance(5, false, true);
                Check(extractor.Ownership == AntimatterOwnership.Player && home.StoredAntimatter == 91 && rival.StoredAntimatter == 80,
                    "Previous owner retains income during takeover");
                extractor.Advance(5, false, true);
                Check(extractor.Ownership == AntimatterOwnership.Enemy && home.StoredAntimatter == 101, "Income ownership changes only on completion");
                extractor.Advance(1, false, true);
                Check(rival.StoredAntimatter == 82 && home.StoredAntimatter == 101 && extractor.CaptureCount == 2, "New owner alone receives income without repeated capture");
                rival.DepositAntimatter(1000); long supplied = extractor.GeneratedAntimatter;
                extractor.Advance(1000, false, false);
                Check(extractor.GeneratedAntimatter == supplied && rival.AvailableStorage == 0, "Full storage produces no backlog or supply");
                rival.WithdrawAntimatter(1); extractor.Advance(0.25, false, false);
                Check(rival.AvailableStorage == 1, "Opening storage does not pay accumulated income");
                extractor.Advance(0.25, false, false);
                Check(rival.AvailableStorage == 0 && extractor.GeneratedAntimatter == supplied + 1, "Partial room accepts exact amount");
                var research = new StrategicPoint { Role = StrategicPointRole.Research, Position = extractor.GlobalPosition }; room.AddChild(research); research.SetPhysicsProcess(false);
                Trace("research");
                home.Construction.TryStart("repair-matrix", out _);
                var project = home.Construction.GetActive(ConstructionSlot.Defense)!;
                Check(project != null, "Research test starts a paid legal project");
                research.Advance(10, true, false);
                home.Construction.Advance(2);
                Check(Math.Abs(project!.Progress - 2.5 / project.Definition.DurationSeconds) < 0.0001 && StrategicPoint.ProjectSpeed(home) == 1.25,
                    "Research modifies active elapsed work without changing cost or prerequisites");
                research.Advance(5, false, true);
                Check(StrategicPoint.ProjectSpeed(home) == 1.25 && StrategicPoint.ProjectSpeed(rival) == 1, "Old research benefit survives incomplete takeover");
                research.Advance(5, false, true);
                Check(StrategicPoint.ProjectSpeed(home) == 1 && StrategicPoint.ProjectSpeed(rival) == 1.25, "Research benefit transfers atomically");
                var extraResearch = new StrategicPoint { Role = StrategicPointRole.Research, Position = extractor.GlobalPosition };
                room.AddChild(extraResearch); extraResearch.SetPhysicsProcess(false); extraResearch.Advance(10, false, true);
                Check(StrategicPoint.ProjectSpeed(rival) == 1.5, "Multiple station bonuses add without compounding");
                extraResearch.Free();
                home.Construction.Advance(100);
                long completed = home.Construction.CompletedInvestment; home.Construction.Advance(100);
                Check(home.Construction.IsCompleted("repair-matrix") && home.Construction.CompletedInvestment == completed,
                    "Modified active project completes once with unchanged investment");
                var logistics = new StrategicPoint { Role = StrategicPointRole.Logistics, Position = extractor.GlobalPosition }; room.AddChild(logistics); logistics.SetPhysicsProcess(false);
                Trace("logistics");
                Check(logistics.Drone == null, "Neutral logistics has no active collector");
                logistics.Advance(10, true, false);
                var drone = logistics.Drone!; drone.SetPhysicsProcess(false);
                var cargo = new AntimatterPickup { AntimatterAmount = 12, Ownership = AntimatterOwnership.Player,
                    Position = arena.OnSurface(origin, 1) }; room.AddChild(cargo); cargo.Freeze = true;
                Invoke(drone, "Scoop", cargo);
                Check(drone.CarriedCount == 1 && !cargo.IsAvailable, "Station drone carries real attached cargo");
                var tracker = new WorldAntimatterTracker(); room.AddChild(tracker); tracker.Refresh(); int total = tracker.TotalAntimatter;
                Check(tracker.SiteGeneratedAntimatter == extractor.GeneratedAntimatter, "World accounting reports accepted extractor supply separately from transfers");
                logistics.Advance(10, false, true);
                tracker.Refresh();
                Check(ReferenceEquals(logistics.Drone, drone) && !drone.UsePlayerAntimatter && drone.CarriedCount == 1
                    && cargo.Ownership == AntimatterOwnership.Enemy && cargo.AntimatterAmount == 12 && tracker.TotalAntimatter == total,
                    "Capture steals same drone/all cargo without resource creation or detachment");
                Check(drone.IsInGroup("enemy_drone") && !drone.IsInGroup("player_drone"), "Drone ownership groups update on transfer");
                var stalePickup = new AntimatterPickup { Ownership = AntimatterOwnership.Player, Position = arena.OnSurface(origin, 1) };
                room.AddChild(stalePickup); stalePickup.Freeze = true; Invoke(drone, "Scoop", stalePickup);
                Check(drone.CarriedCount == 1 && stalePickup.IsAvailable, "Stale previous-owner pickup orders cannot scoop after transfer");
                stalePickup.Free();
                drone.GlobalPosition = home.GlobalPosition + arena.UpAt(home.GlobalPosition) * drone.HoverHeight;
                Invoke(drone, "HandleReturning", 0f);
                Check(cargo.AntimatterAmount == 12, "Returning at old home after transfer cannot dock/deposit there");
                int oldStorage = home.StoredAntimatter;
                Invoke(drone, "DepositAll");
                Check(cargo.AntimatterAmount == 12 && home.StoredAntimatter == oldStorage, "Full new base retains stolen cargo and prevents old-base deposit");
                rival.WithdrawAntimatter(5); Invoke(drone, "DepositAll");
                Check(cargo.AntimatterAmount == 7 && drone.CarriedCount == 1 && rival.AvailableStorage == 0, "Partial deposit retains exact remainder aboard");
                logistics.Advance(10, true, false);
                Check(ReferenceEquals(logistics.Drone, drone) && cargo.Ownership == AntimatterOwnership.Player && cargo.AntimatterAmount == 7,
                    "Repeated recapture preserves drone and remaining cargo");
                int roomBefore = home.AvailableStorage; Invoke(drone, "DepositAll");
                Check(home.AvailableStorage == roomBefore - 7 && drone.CarriedCount == 0, "Recaptured cargo deposits only into current home");
                drone.ApplyDamage(1000); await Frames(2);
                logistics.Advance(10, false, true);
                Check(!GodotObject.IsInstanceValid(logistics.Drone) || logistics.Drone!.IsQueuedForDeletion(), "Destroyed station drones do not duplicate on recapture");
                GetTree().Paused = true; supplied = extractor.GeneratedAntimatter; extractor.Advance(100, true, false);
                Check(extractor.GeneratedAntimatter == supplied && extractor.Ownership == AntimatterOwnership.Enemy, "Pause rejects capture and income advancement");
                GetTree().Paused = false;
                research.Advance(10, true, false);
                player.GlobalPosition = research.GlobalPosition + arena.UpAt(research.GlobalPosition) * 8;
                research.Advance(10, false, true); // Return it to rival, then test actual presence sampling.
                research.SetPhysicsProcess(true); await Frames(8); research.SetPhysicsProcess(false);
                Check(research.Claimant == AntimatterOwnership.Player && research.CaptureProgress > 0, "Real physics samples eligible ship on flat/underside capture ring");
                var pilot = new MagicCarpetRemastered.Scripts.Enemies.EnemyEcosystemEnemy
                { Role = MagicCarpetRemastered.Scripts.Enemies.EnemyEcosystemEnemy.EnemyRole.RivalShip,
                    Position = player.GlobalPosition };
                room.AddChild(pilot); pilot.SetPhysicsProcess(false); pilot.GlobalPosition = player.GlobalPosition;
                double progress = research.CaptureProgress; research._PhysicsProcess(1);
                Check(research.IsContested && research.CaptureProgress == progress, "Actual living rival pilot contests ship presence");
                pilot.Health = 0; research._PhysicsProcess(1);
                Check(!research.IsContested && research.CaptureProgress > progress, "Defeated pilot no longer contests capture");
                pilot.Free();
                player.GlobalPosition = arena.OnSurface(origin + Vector3.Right * 150, 8);
                research._PhysicsProcess(0.1);
                Check(research.CaptureProgress == 0, "Real out-of-range ship cannot continue capture");
                var ui = new BaseManagement(); room.AddChild(ui); ui.SetOpen(true);
                Check(ui.SummaryText.Contains("Sites") && !GetTree().Paused, "Management summarizes economic sites in real time");
                ui.SetOpen(false);
                if (spherical)
                {
                    var navigation = new WorldNavigation(); room.AddChild(navigation);
                    Check(System.Linq.Enumerable.Count(navigation.Markers, m => m.Kind == MapMarkerKind.StrategicPoint) == 3,
                        "All economic roles expose global ownership/benefit navigation markers");
                }
                rival.ApplyDamage(10000); supplied = extractor.GeneratedAntimatter;
                extractor.Advance(100, false, true);
                Check(extractor.GeneratedAntimatter == supplied, "Destroyed owner's base cannot receive generated supply");
                room.Free(); await Frames(2);
                Trace("fixture cleared");
            }
            // Large repeated render worlds retain expensive GPU retirement work at
            // shutdown; headless checks cover all layouts, rendering covers Medium.
            foreach (int radius in DisplayServer.GetName() == "headless" ? PlanetSizes.Radii : new[] { 200 })
            {
                Trace($"world {radius}");
                PlanetSizes.SelectedIndex = Array.IndexOf(PlanetSizes.Radii, radius);
                var game = GD.Load<PackedScene>("res://scenes/PlanetGame.tscn").Instantiate<Node3D>(); AddChild(game);
                var arena = (HeightmapArena)GetTree().GetFirstNodeInGroup("heightmap_arena");
                var home = (FactionBase)GetTree().GetFirstNodeInGroup("player_base");
                var rival = (FactionBase)GetTree().GetFirstNodeInGroup("enemy_base");
                var points = GetTree().GetNodesInGroup("strategic_point");
                Check(points.Count == 8, $"{radius}: match creates eight fresh economic sites");
                for (int role = 0; role < 4; role++)
                {
                    StrategicPoint? left = null, right = null;
                    foreach (Node node in points)
                        if (node is StrategicPoint point && (int)point.Role == role)
                        { if (left == null) left = point; else right = point; }
                    Check(left != null && right != null && left.Ownership == AntimatterOwnership.Neutral && right.Ownership == AntimatterOwnership.Neutral,
                        $"{radius}: two neutral sites of each role reset without previous benefits");
                    float a = arena.SurfaceDistance(home.GlobalPosition, left!.GlobalPosition);
                    float b = arena.SurfaceDistance(rival.GlobalPosition, right!.GlobalPosition);
                    Check(Mathf.Abs(a - b) < 1 && a > 30 && b > 30, $"{radius}: mirrored opening distances are comparable and outside bases");
                }
                if (radius == 200 && DisplayServer.GetName() != "headless")
                {
                    var ship = (ShipFlightController)GetTree().GetFirstNodeInGroup("player");
                    var site = (StrategicPoint)points[0];
                    ship.GlobalPosition = arena.OnSurface(site.GlobalPosition + site.GlobalBasis.Z * 28, 8);
                    arena.Orient(ship, -site.GlobalBasis.Z);
                    await Frames(12);
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    GetViewport().GetTexture().GetImage().SavePng("C:/Users/lblan/AppData/Local/Temp/opencode/StrategicPointGameplay.png");
                    var management = (BaseManagement)GetTree().GetFirstNodeInGroup("base_management");
                    management.SetOpen(true); await Frames(4);
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    GetViewport().GetTexture().GetImage().SavePng("C:/Users/lblan/AppData/Local/Temp/opencode/StrategicPointManagement.png");
                    management.SetOpen(false);
                }
                game.QueueFree(); await Frames(10);
                Trace("world cleared");
            }
            PlanetSizes.SelectedIndex = 1;
            // Allow queued native teardown to drain normally. Explicit forced
            // GC/finalizer waits here can crash after all world assertions pass.
            await Frames(10);
            GD.Print($"Strategic point checks passed: {_checks}"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}

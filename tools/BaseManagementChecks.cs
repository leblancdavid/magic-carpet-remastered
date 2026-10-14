using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Game;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Ui;
using MagicCarpetRemastered.Scripts.World;
using MagicCarpetRemastered.Scripts.Equipment;

namespace MagicCarpetRemastered.Tools;

public partial class BaseManagementChecks : Node3D
{
    private int _checks;
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();
    private void Check(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); _checks++; }
    private async Task Frames(int count)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    private void Key(Key key) => GetViewport().PushInput(new InputEventKey { Keycode = key, Pressed = true });
    private async Task Click(Button button)
    {
        Vector2 point = button.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Position = point, GlobalPosition = point, Pressed = true }, true);
        GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Position = point, GlobalPosition = point, Pressed = false }, true);
        await Frames(2);
    }
    private async Task Run()
    {
        try
        {
            var game = new MagicCarpetRemastered.Scripts.Game.Game { UseSphericalWorld = true };
            AddChild(game);
            foreach (Node child in game.GetChildren())
                if (child is not CanvasLayer) { child.SetPhysicsProcess(false); child.SetProcess(false); }
            var hub = game.GetNode<FactionBase>("PlayerBase");
            var rival = game.GetNode<FactionBase>("EnemyBase");
            hub.Fleet.SetPhysicsProcess(false); rival.Fleet.SetPhysicsProcess(false);
            var player = game.GetNode<ShipFlightController>("PlayerShip");
            var panel = game.GetNode<BaseManagement>("BaseManagement");
            var map = game.GetNode<TacticalMap>("TacticalMap");
            await Frames(3);
            Check(!panel.IsOpen && hub.Construction.Catalog.Count() == 104, "Fresh match has playable star research and fleet expansions with closed management");
            hub.DepositAntimatter(90);
            player.RespawnAt(player.GlobalPosition);
            Key(Godot.Key.B); await Frames(2);
            Check(panel.IsOpen && !GetTree().Paused && player.GameplayInputBlocked, "Actual B opens real-time management and owns input");
            Check(panel.SummaryText.Contains("120/300") && panel.SummaryText.Contains("BASE  I"), "Panel shows antimatter and surviving tier");
            var tabs = (TabContainer)panel.FindChild("ProjectTabs", true, false);
            tabs.CurrentTab = 1;
            await Frames(3);
            await Click((Button)panel.FindChild("Order_repair-matrix", true, false));
            var repair = hub.Construction.GetActive(ConstructionSlot.Defense);
            Check(repair != null && hub.StoredAntimatter == 80 && player.EnergyCapacity == 80, "Actual GUI click starts bronze research and lowers limit");
            hub.SetPhysicsProcess(true);
            await Frames(8);
            hub.SetPhysicsProcess(false);
            Check(repair!.ElapsedSeconds > 0 && panel.IsOpen && !GetTree().Paused, "Construction advances during open management");
            player.SetPhysicsProcess(true);
            int energy = player.Energy;
            Input.ActionPress("activate_primary"); Input.ActionPress("activate_claim"); Input.ActionPress("activate_shield"); Input.ActionPress("move_forward");
            await Frames(4);
            Check(player.Energy >= energy && !player.IsShielded && !game.GetChildren().OfType<WeaponProjectile>().Any()
                && !game.GetChildren().OfType<ClaimProjectile>().Any(),
                "Polling combat input does not fire or spend while UI owns input");
            Key(Godot.Key.Escape); await Frames(2);
            Check(!panel.IsOpen && !player.GameplayInputBlocked && !GetTree().Paused, "Escape restores gameplay without pausing");
            energy = player.Energy;
            await Frames(2);
            Check(player.Energy >= energy && !player.IsShielded, "Held interface clicks/keys do not activate equipment immediately on close");
            foreach (string action in new[] { "activate_primary", "activate_claim", "activate_shield", "move_forward" }) Input.ActionRelease(action);
            await Frames(2); player.SetPhysicsProcess(false);
            panel.SetOpen(true);
            hub.DepositAntimatter(1000);
            await Click(((UiTile)panel.FindChild("Order_repair-matrix", true, false)).CancelButton);
            Check(repair.State == ConstructionState.Cancelled && hub.StoredAntimatter == 300,
                "GUI cancellation returns payment when storage has refilled");
            Check(GetTree().GetNodesInGroup("loose_antimatter").OfType<AntimatterPickup>()
                .Any(p => p.Name.ToString().Contains("ConstructionRefundSpill") && p.Ownership == AntimatterOwnership.Player),
                "Cancellation preserves owned overflow in the world");
            Check(panel.OrderProject("repair-matrix"), "Restart cancelled research");
            hub._PhysicsProcess(60);
            Check(hub.EffectiveRepairHealthPerSecond == 3.75f && hub.Construction.IsCompleted("repair-matrix"), "Completed bronze repair matrix has a live repair effect");
            hub.ApplyDamage(20); hub._PhysicsProcess(6);
            Check(hub.Health == 383 && hub.Tier == 1, "Actual quiet repair uses researched rate without adding tiers");
            hub.DepositAntimatter(1000);
            Check(panel.OrderProject("collector-drive") && panel.OrderProject("claim-range"), "Resources and ship categories can run concurrently");
            hub._PhysicsProcess(60);
            Check(hub.CollectorSpeedMultiplier == 1.15f && player.EffectiveClaimRange == 120 && hub.Construction.UpgradeRank("claim-range") == 1, "Bronze collector speed and magnetism research apply without changing Claim range");
            var collector = game.GetNode<CollectorDrone>("PlayerDrone");
            Vector3 departure = collector.GlobalPosition;
            var arena = game.GetNode<HeightmapArena>("HeightmapArena");
            Vector3 destination = arena.StepToward(departure, rival.GlobalPosition, 100);
            typeof(CollectorDrone).GetMethod("MoveToward", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(collector, new object[] { destination, 1f });
            Check(arena.SurfaceDistance(departure, collector.GlobalPosition) > 11, "Real collector movement uses researched speed");
            hub.DepositAntimatter(1000);
            Check(panel.OrderNextTier(), "Panel orders tier advancement");
            hub._PhysicsProcess(60);
            Check(hub.Tier == 2, "Timed tier advancement completes through panel order");
            hub.DepositAntimatter(200);
            Check(panel.OrderProject("energy-grid"), "Tier two unlocks energy grid");
            hub._PhysicsProcess(90);
            Check(hub.EnergyRechargeMultiplier == 1.15f && player.EnergyRechargePerSecond == player.EnergyCapacity * player.EnergyRegenFractionPerSecond * 1.15f,
                "Completed energy grid changes actual ship recharge");
            player.SpendEnergy(player.Energy);
            typeof(ShipFlightController).GetMethod("RegenerateEnergy", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(player, new object[] { 1f });
            Check(player.Energy == Mathf.FloorToInt(player.EnergyRechargePerSecond), "Actual energy regeneration uses researched multiplier");
            Check(!player.TryActivateClaim(), "Management owns built-in Claim input");
            panel.SetOpen(false); player._PhysicsProcess(0);
            Check(player.TryActivateClaim(), "Closed management allows the ordinary Claim action");
            var claim = game.GetChildren().OfType<ClaimProjectile>().Single();
            claim.SetPhysicsProcess(false);
            Check(claim.MaxRange == 120, "Actual built-in Claim retains baseline range with magnetism research");
            claim.QueueFree();
            panel.SetOpen(true);
            hub.DepositAntimatter(1000);
            Check(panel.OrderProject(UpgradeCatalog.ProjectId("strike-drone", 1)), "Panel researches the bronze offensive capability");
            hub._PhysicsProcess(40); hub.DepositAntimatter(1000);
            Vector3 previousPosition = player.GlobalPosition;
            player.GlobalPosition = arena.OnSurface(rival.GlobalPosition, 6);
            game.GetNode<WorldNavigation>("WorldNavigation").Refresh(); player.GlobalPosition = previousPosition;
            Check(!panel.OpenTargetOrder("strike-drone"), "Automatic strike fleets do not expose paid-target controls");
            hub.Fleet._PhysicsProcess(20);
            var strike = GetTree().GetNodesInGroup("base_strike_drone").OfType<BaseStrikeDrone>().Single();
            strike.SetPhysicsProcess(false);
            int rivalHealth = rival.Health;
            int routeTicks = (int)Math.Ceiling(strike.LifetimeSeconds / 0.1) + 1;
            for (int i = 0; i < routeTicks && !strike.IsQueuedForDeletion(); i++) strike._PhysicsProcess(0.1);
            Check(rival.Health == rivalHealth - 120 && strike.IsQueuedForDeletion(), "Offensive drone travels the globe and hits rival base once");
            strike._PhysicsProcess(1);
            Check(rival.Health == rivalHealth - 120 && !hub.Construction.IsCompleted("strike-drone"), "Repeatable unit cannot hit twice or become one-time research");
            hub.ApplyDamage(400); panel.Refresh();
            Check(hub.Tier == 1 && hub.EnergyRechargeMultiplier == 1.15f && hub.Construction.UpgradeRank("strike-drone", true) == 0 && panel.AlertText.Contains("SIEGE"),
                "Lost tier suspends base capability effects but retains ship passive research");
            hub.DepositAntimatter(120);
            Check(panel.OrderNextTier(), "UI exposes rebuilding after siege");
            hub._PhysicsProcess(60);
            Check(hub.Tier == 2 && hub.Construction.UpgradeRank("strike-drone", true) == 1, "Rebuilding restores retained base capability effect");
            Key(Godot.Key.M); await Frames(2);
            Check(map.IsExpanded && !panel.IsOpen && player.GameplayInputBlocked && !GetTree().Paused,
                "M transfers input to real-time survey without simultaneous panels");
            Key(Godot.Key.B); await Frames(2);
            Check(panel.IsOpen && !map.IsExpanded && !GetTree().Paused, "B transfers input from survey to management");
            string? capture = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?.Substring(10);
            if (capture != null)
            {
                hub.DepositAntimatter(200);
                Check(panel.OrderNextTier(), "Capture has active base construction");
                hub.DepositAntimatter(200);
                Check(panel.OrderProject("attack-drone"), "Capture has concurrent automatic-unit research");
                hub._PhysicsProcess(5);
                tabs.CurrentTab = 1;
                panel.Refresh();
                await Frames(3);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Check(image.SavePng(capture) == Error.Ok, "Save management layout capture");
                await Frames(2);
            }
            Key(Godot.Key.F2); await Frames(2);
            Check(!panel.IsOpen && GetTree().Paused, "Size setup closes real-time management and retains its own pause");
            Check(!panel.SetOpen(true) && !map.SetExpanded(true), "Live interfaces cannot open over setup/result pause");
            Key(Godot.Key.F2); await Frames(2);
            Check(!GetTree().Paused && !player.GameplayInputBlocked, "Closing setup restores gameplay input");
            panel.SetOpen(true);
            hub.ApplyDamage(int.MaxValue); await Frames(2);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(!panel.IsOpen && !player.GameplayInputBlocked, "Destroyed home base closes management");
            GD.Print($"Base management checks passed: {_checks} assertions.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GetTree().Paused = false;
            foreach (string action in new[] { "activate_primary", "activate_claim", "activate_shield", "move_forward" }) Input.ActionRelease(action);
            GD.PushError(error.ToString()); GetTree().Quit(1);
        }
    }
}

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
            var player = game.GetNode<ShipFlightController>("PlayerShip");
            var panel = game.GetNode<BaseManagement>("BaseManagement");
            var map = game.GetNode<TacticalMap>("TacticalMap");
            await Frames(3);
            Check(!panel.IsOpen && hub.Construction.Catalog.Count() == 19, "Fresh match has starter/equipment catalog and closed management");
            hub.DepositAntimatter(90);
            hub.DepositMatter(90); // Fund the scripted infrastructure sequence independently of hauling.
            player.RespawnAt(player.GlobalPosition);
            Key(Godot.Key.B); await Frames(2);
            Check(panel.IsOpen && !GetTree().Paused && player.GameplayInputBlocked, "Actual B opens real-time management and owns input");
            Check(panel.SummaryText.Contains("120/120") && panel.SummaryText.Contains("tier 1"), "Panel shows storage, energy and surviving tier");
            var tabs = (TabContainer)panel.FindChild("ProjectTabs", true, false);
            tabs.CurrentTab = 1;
            await Frames(3);
            await Click((Button)panel.FindChild("Order_repair-matrix", true, false));
            var repair = hub.Construction.GetActive(ConstructionSlot.Defense);
            Check(repair != null && hub.StoredAntimatter == 100 && player.EnergyCapacity == 100, "Actual GUI click starts research and lowers limit");
            hub.SetPhysicsProcess(true);
            await Frames(8);
            hub.SetPhysicsProcess(false);
            Check(repair!.ElapsedSeconds > 0 && panel.IsOpen && !GetTree().Paused, "Construction advances during open management");
            player.SetPhysicsProcess(true);
            int energy = player.Energy;
            Input.ActionPress("activate_primary"); Input.ActionPress("activate_claim"); Input.ActionPress("activate_shield"); Input.ActionPress("move_forward");
            await Frames(4);
            Check(player.Energy >= energy && !player.IsShielded && !GetChildren().OfType<WeaponProjectile>().Any()
                && !GetChildren().OfType<ClaimProjectile>().Any(),
                "Polling combat input does not fire or spend while UI owns input");
            Key(Godot.Key.Escape); await Frames(2);
            Check(!panel.IsOpen && !player.GameplayInputBlocked && !GetTree().Paused, "Escape restores gameplay without pausing");
            energy = player.Energy;
            await Frames(2);
            Check(player.Energy >= energy && !player.IsShielded, "Held interface clicks/keys do not activate equipment immediately on close");
            foreach (string action in new[] { "activate_primary", "activate_claim", "activate_shield", "move_forward" }) Input.ActionRelease(action);
            await Frames(2); player.SetPhysicsProcess(false);
            panel.SetOpen(true);
            hub.DepositAntimatter(20);
            await Click((Button)panel.FindChild("Cancel_Defense", true, false));
            Check(repair.State == ConstructionState.Cancelled && hub.StoredAntimatter == 120,
                "GUI cancellation returns payment when storage has refilled");
            Check(GetTree().GetNodesInGroup("loose_antimatter").OfType<AntimatterPickup>()
                .Any(p => p.Name.ToString().Contains("ConstructionRefundSpill") && p.Ownership == AntimatterOwnership.Player),
                "Cancellation preserves owned overflow in the world");
            Check(panel.OrderProject("repair-matrix"), "Restart cancelled research");
            hub._PhysicsProcess(8);
            Check(hub.EffectiveRepairHealthPerSecond == 4.5f && hub.Construction.IsCompleted("repair-matrix"), "Completed repair matrix has a live repair effect");
            hub.ApplyDamage(20); hub._PhysicsProcess(6);
            Check(hub.Health == 124 && hub.Tier == 1, "Actual quiet repair uses researched rate without adding tiers");
            Check(panel.OrderProject("collector-drive") && panel.OrderProject("claim-range"), "Resources and ship categories can run concurrently");
            hub._PhysicsProcess(10);
            Check(hub.CollectorSpeedMultiplier == 1.25f && player.EffectiveClaimRange == 150, "Collector speed and built-in Claim range effects apply");
            var collector = game.GetNode<CollectorDrone>("PlayerDrone");
            Vector3 departure = collector.GlobalPosition;
            var arena = game.GetNode<HeightmapArena>("HeightmapArena");
            Vector3 destination = arena.StepToward(departure, rival.GlobalPosition, 100);
            typeof(CollectorDrone).GetMethod("MoveToward", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(collector, new object[] { destination, 1f });
            Check(arena.SurfaceDistance(departure, collector.GlobalPosition) > 12, "Real collector movement uses researched speed");
            hub.DepositAntimatter(120);
            Check(panel.OrderNextTier(), "Panel orders tier advancement");
            hub._PhysicsProcess(20);
            Check(hub.Tier == 2, "Timed tier advancement completes through panel order");
            hub.DepositAntimatter(200);
            Check(panel.OrderProject("energy-grid"), "Tier two unlocks energy grid");
            hub._PhysicsProcess(12);
            Check(hub.EnergyRechargeMultiplier == 1.25f && player.EnergyRechargePerSecond == player.EnergyCapacity * player.EnergyRegenFractionPerSecond * 1.25f,
                "Completed energy grid changes actual ship recharge");
            player.SpendEnergy(player.Energy);
            typeof(ShipFlightController).GetMethod("RegenerateEnergy", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(player, new object[] { 1f });
            Check(player.Energy == Mathf.FloorToInt(player.EnergyRechargePerSecond), "Actual energy regeneration uses researched multiplier");
            typeof(ShipFlightController).GetMethod("CastClaimEquipment", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(player, null);
            var claim = GetChildren().OfType<ClaimProjectile>().Single();
            claim.SetPhysicsProcess(false);
            Check(claim.MaxRange == 150, "Actual built-in Claim cast receives researched range");
            claim.QueueFree();
            Check(panel.OrderProject("strike-drone"), "Panel orders offensive unit");
            hub._PhysicsProcess(15);
            var strike = GetTree().GetNodesInGroup("base_strike_drone").OfType<BaseStrikeDrone>().Single();
            strike.SetPhysicsProcess(false);
            int rivalHealth = rival.Health;
            for (int i = 0; i < 600 && !strike.IsQueuedForDeletion(); i++) strike._PhysicsProcess(0.1);
            Check(rival.Health == rivalHealth - 45 && strike.IsQueuedForDeletion(), "Offensive drone travels the globe and hits rival base once");
            strike._PhysicsProcess(1);
            Check(rival.Health == rivalHealth - 45 && !hub.Construction.IsCompleted("strike-drone"), "Repeatable unit cannot hit twice or become one-time research");
            hub.ApplyDamage(140); panel.Refresh();
            Check(hub.Tier == 1 && hub.EnergyRechargeMultiplier == 1 && panel.AlertText.Contains("SIEGE"),
                "Lost tier suspends higher-tier effects and shows siege alert");
            hub.DepositAntimatter(120);
            Check(panel.OrderNextTier(), "UI exposes rebuilding after siege");
            hub._PhysicsProcess(20);
            Check(hub.Tier == 2 && hub.EnergyRechargeMultiplier == 1.25f, "Rebuilding restores retained research effect");
            Key(Godot.Key.M); await Frames(2);
            Check(map.IsExpanded && !panel.IsOpen && player.GameplayInputBlocked && !GetTree().Paused,
                "M transfers input to real-time survey without simultaneous panels");
            Key(Godot.Key.B); await Frames(2);
            Check(panel.IsOpen && !map.IsExpanded && !GetTree().Paused, "B transfers input from survey to management");
            string? capture = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture="))?.Substring(10);
            if (capture != null)
            {
                hub.DepositAntimatter(200);
                hub.DepositMatter(200); // Refill both resources for the concurrent-project layout capture.
                Check(panel.OrderNextTier(), "Capture has active base construction");
                hub.DepositAntimatter(200);
                Check(panel.OrderProject("strike-drone"), "Capture has concurrent offense construction");
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

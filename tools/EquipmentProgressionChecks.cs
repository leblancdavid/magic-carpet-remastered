using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Ui;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class EquipmentProgressionChecks : Node3D
{
    private int _checks;
    public override void _Ready() => Callable.From(() => { _ = RunChecks(); }).CallDeferred();

    private async Task RunChecks()
    {
        try
        {
            AddChild(new HeightmapArena());
            var home = new FactionBase { InitialStoredAntimatter = 120, Position = new Vector3(0, 20, 0) };
            AddChild(home); home.SetPhysicsProcess(false);
            var player = new ShipFlightController(); AddChild(player); player.SetPhysicsProcess(false);
            player.GlobalPosition = home.GlobalPosition + Vector3.Up * 6;
            var ui = new BaseManagement(); AddChild(ui);
            Check(player.EquipmentSlots.Count(id => id != null) == 1 && player.GetOwnedEquipmentRank("plasma-bolt") == 1,
                "Fresh match owns/installs only starter Plasma Bolt");
            Check(ShipEquipmentCatalog.ItemIds.Skip(1).All(id => player.GetOwnedEquipmentRank(id) == 0), "Other items require research");
            for (int slot = 0; slot < 4; slot++)
            {
                int energy = player.Energy;
                await SelectKey(slot);
                Check(player.SelectedEquipmentSlot == slot && player.Energy == energy && !GetChildren().OfType<WeaponProjectile>().Any(),
                    "Viewport-routed 1–4 selects the corresponding slot without activating it");
            }
            Input.ActionPress("activate_primary"); player._PhysicsProcess(0.001); Input.ActionRelease("activate_primary");
            Check(player.PrimaryEquipmentName == "Empty" && !GetChildren().OfType<WeaponProjectile>().Any(), "LMB on selected empty slot does nothing");
            await SelectKey(0);
            Check(!home.Construction.TryStart(ShipEquipmentCatalog.ProjectId("pulse-burst", 2), out _), "Ranks cannot skip ownership research");
            Check(!player.DiscoverEquipmentUpgrade("plasma-bolt"), "Legacy beacons cannot purchase ranks");
            int before = player.Energy;
            Cast(player, "CastShieldEquipment");
            Check(!player.IsShielded && player.Energy == before, "Uninstalled direct hotkeys reject without spending");

            ui.SetOpen(true);
            await SelectKey(2);
            Check(player.SelectedEquipmentSlot == 0, "Management consumes number keys without changing selection");
            home.TryCompleteTierUpgrade(2); home.TryCompleteTierUpgrade(3); home.TryCompleteTierUpgrade(4);
            foreach (string id in ShipEquipmentCatalog.ItemIds)
            {
                int rank = id == "plasma-bolt" ? 2 : 1;
                home.DepositAntimatter(400);
                int storage = home.StoredAntimatter;
                string project = ShipEquipmentCatalog.ProjectId(id, rank);
                Check(ui.OrderProject(project), "Remote equipment order uses Ship construction slot");
                var active = home.Construction.GetActive(ConstructionSlot.Ship)!;
                Check(home.StoredAntimatter == storage - active.PaidAntimatter, "Research spends physical storage immediately");
                Check(!ui.OrderProject(project), "Occupied research rejects duplicate payment");
                player.GlobalPosition = home.GlobalPosition + Vector3.Right * 80;
                home.Construction.Advance(100);
                Check(player.GetOwnedEquipmentRank(id) == rank, "Completion makes item/rank permanently owned");
                Check(player.GetEquipmentRank(id) == (id == "plasma-bolt" ? 1 : 0), "Remote completion never applies item/rank");
                Check(!ui.InstallEquipment(id, 0), "Remote UI cannot install");
                Check(!home.Construction.TryStart(project, out _), "Completed ranks cannot be repurchased");
            }
            player.GlobalPosition = home.GlobalPosition + Vector3.Up * 6;
            // Entering the home area applies the pending researched level without a GUI action.
            ui.Refresh();
            Find<TabContainer>(ui, "ProjectTabs").CurrentTab = 3;
            for (int i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var shipScroll = (ScrollContainer)Find<TabContainer>(ui, "ProjectTabs").GetChild(3);
            player._PhysicsProcess(0);
            Check(player.GetEquipmentRank("plasma-bolt") == 2 && player.PlasmaBoltDamage == 18 && Find<Button>(ui, "ApplyRank_plasma-bolt") == null,
                "Home automatically applies pending rank with no Apply button");
            Check(ui.InstallEquipment("pulse-burst", 1) && ui.InstallEquipment("energy-shield", 2) && ui.InstallEquipment("dash", 3), "Four mixed items install");
            int paid = home.StoredAntimatter;
            Check(ui.InstallEquipment("guardian-drone", 1) && player.EquipmentSlots.Count(id => id != null) == 4,
                "Fifth item replaces selected slot rather than exceeding limit");
            Check(player.GetOwnedEquipmentRank("pulse-burst") == 1 && !player.IsEquipmentInstalled("pulse-burst"), "Replacement retains ownership");
            Check(ui.InstallEquipment("guardian-drone", 3) && player.EquipmentSlots.Count(id => id == "guardian-drone") == 1,
                "Moving an item never duplicates it");
            Check(!player.TryInstallEquipment("claim", 1, out _) && !player.TryInstallEquipment("dash", 4, out _), "Claim and invalid slot reject");
            Check(ui.InstallEquipment("pulse-burst", 1) && home.StoredAntimatter == paid, "Re-equipping and applying ranks are free");
            home.DepositAntimatter(400);
            Check(ui.OrderProject(ShipEquipmentCatalog.ProjectId("plasma-bolt", 3)), "Next sequential upgrade starts");
            var cancelled = home.Construction.GetActive(ConstructionSlot.Ship)!;
            Check(ui.CancelProject(ConstructionSlot.Ship, cancelled.InstanceId) && player.GetOwnedEquipmentRank("plasma-bolt") == 2,
                "Cancelled upgrade never grants ownership");
            Check(ui.OrderProject(ShipEquipmentCatalog.ProjectId("plasma-bolt", 3)), "Cancelled rank can be ordered again");
            home.Construction.Advance(120);
            Check(player.GetOwnedEquipmentRank("plasma-bolt") == 3 && player.GetEquipmentRank("plasma-bolt") == 2, "Rank III waits for home application");
            Check(ui.InstallEquipment("plasma-bolt", 0) && player.PlasmaBoltDamage == 23, "Sequential third rank applies exactly once");
            foreach (string id in new[] { "pulse-burst", "energy-shield", "dash" })
            {
                for (int rank = 2; rank <= 3; rank++)
                {
                    home.DepositAntimatter(400);
                    Check(ui.OrderProject(ShipEquipmentCatalog.ProjectId(id, rank)), "Utility/offense ranks research sequentially");
                    home.Construction.Advance(120);
                }
                Check(player.GetOwnedEquipmentRank(id) == 3 && player.GetEquipmentRank(id) < 3, "Purchased utility ranks await installation");
            }
            Check(ui.InstallEquipment("energy-shield", 2) && ui.InstallEquipment("pulse-burst", 1), "Apply third-rank defense and area research");
            Check(player.EffectiveShieldDuration == 8 && player.EffectiveAreaDamage == 48 && player.EffectiveAreaRadius == 12,
                "Sequential utility/offense stats scale without double application");
            if (DisplayServer.GetName() != "headless")
            {
                Find<TabContainer>(ui, "ProjectTabs").CurrentTab = 3;
                shipScroll.ScrollVertical = 0;
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                Check(image.SavePng("user://equipment-management.png") == Error.Ok, "Save rendered owned/rank/loadout UI");
            }
            home.ApplyDamage(1200); // Tier four to one with live 400-HP layers.
            Check(home.Tier == 1 && player.GetEquipmentRank("plasma-bolt") == 3 && player.GetOwnedEquipmentRank("guardian-drone") == 1,
                "Tier loss preserves owned and installed items/ranks");
            Check(!home.Construction.TryStart(ShipEquipmentCatalog.ProjectId("guardian-drone", 2), out _), "Tier loss still blocks new research prerequisites");
            Check(ui.InstallEquipment("guardian-drone", 3), "Previously researched higher-tier equipment installs after tier loss");
            home.WithdrawAntimatter(home.StoredAntimatter); home.DepositAntimatter(10);
            ui.SetOpen(false); player.RespawnAt(player.GlobalPosition);
            Cast(player, "CastSummonEquipment");
            Check(!GetChildren().OfType<GuardianDrone>().Any() && player.Energy == 10, "Cost above energy limit rejects installed equipment");
            home.DepositAntimatter(110); player.RespawnAt(player.GlobalPosition);
            Cast(player, "CastSummonEquipment");
            Check(GetChildren().OfType<GuardianDrone>().Single().Damage == 12 && player.Energy == 96, "Actual researched Guardian casts with expected cost/effect");
            int beforeSelection = player.Energy;
            await SelectKey(2);
            Check(player.PrimaryEquipmentName == "Energy Shield III" && !player.IsShielded && player.Energy == beforeSelection,
                "Number key selects installed utility without casting or spending");
            player._PhysicsProcess(0.001); // Release suppression after closing management.
            Input.ActionPress("activate_primary"); player._PhysicsProcess(0.001);
            Check(player.IsShielded && player.ShieldStatusText.Contains("8.0") && player.Energy == beforeSelection - 14,
                "LMB activates number-selected rank-III Shield");
            int afterActivation = player.Energy;
            player._PhysicsProcess(0.001); Input.ActionRelease("activate_primary");
            Check(player.Energy == afterActivation, "LMB respects the selected item's existing cooldown");
            Check(player.TryInstallEquipment("dash", 1, out _), "Install owned Dash III for activation");
            Cast(player, "CastMobilityEquipment");
            Check(new Vector2(player.Velocity.X, player.Velocity.Z).Length() > 60, "Actual rank-III Dash uses stronger speed");
            Check(player.TryInstallEquipment("pulse-burst", 1, out _), "Restore owned Burst III without rebuilding");
            Cast(player, "CastAreaEquipment");
            Check(GetChildren().OfType<EquipmentImpactBurst>().Any(burst => burst.Name == "AreaEquipmentBurst" && burst.EndScale == 12),
                "Actual rank-III Burst uses researched radius");
            player.GameplayInputBlocked = true; before = player.Energy; Cast(player, "CastPrimaryEquipment");
            Check(player.Energy == before, "Management ownership blocks direct activation");
            player.GameplayInputBlocked = false;
            typeof(ShipFlightController).GetField("_damageInvulnerabilityTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, 0f);
            player.ApplyDamage(1000);
            Check(!player.TryInstallEquipment("dash", 1, out _), "Dead pilot cannot install");
            player.RespawnAt(home.GlobalPosition + Vector3.Up * 6);
            Check(player.GetOwnedEquipmentRank("plasma-bolt") == 3 && player.GetEquipmentRank("plasma-bolt") == 3 && player.EquipmentSlots[3] == "guardian-drone",
                "Death/respawn retains owned ranks and loadout");
            Cast(player, "CastClaimEquipment");
            Check(player.Energy == player.EnergyCapacity - 1 && GetChildren().OfType<ClaimProjectile>().Any(), "Built-in Claim costs one energy outside four slots");
            Check(player.TryRemoveEquipment(0), "Home removal works");
            Cast(player, "CycleQuickEquipment");
            Check(player.PrimaryEquipmentName != "Plasma Bolt III", "Tab skips removed equipment");
            GetTree().Paused = true;
            Check(!player.TryInstallEquipment("plasma-bolt", 0, out _), "Paused installation rejects"); GetTree().Paused = false;

            // Actual match restart resets inventory, research and installed slots.
            foreach (Node child in GetChildren()) child.QueueFree();
            for (int i = 0; i < 10; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            var game = GD.Load<PackedScene>("res://scenes/Game.tscn").Instantiate();
            GetTree().Root.AddChild(game); GetTree().CurrentScene = game;
            var fresh = game.GetNode<ShipFlightController>("PlayerShip");
            Check(fresh.GetOwnedEquipmentRank("plasma-bolt") == 1 && fresh.EquipmentSlots.Count(id => id != null) == 1, "New match resets equipment state");
            game.GetNode<BaseManagement>("BaseManagement").SetOpen(true);
            game.GetNode<BaseManagement>("BaseManagement")._Input(new InputEventKey { Keycode = Key.F4, Pressed = true });
            await ToSignal(GetTree(), SceneTree.SignalName.SceneChanged);
            fresh = GetTree().CurrentScene.GetNode<ShipFlightController>("PlayerShip");
            Check(fresh.GetOwnedEquipmentRank("guardian-drone") == 0 && fresh.GetEquipmentRank("plasma-bolt") == 1, "F4 creates a fresh starter loadout");
            Node restartedWorld = GetTree().CurrentScene;
            GetTree().CurrentScene = null;
            restartedWorld.QueueFree();
            for (int i = 0; i < 10; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            GD.Print($"Equipment progression checks passed: {_checks}"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    private static T Find<T>(Node root, string name) where T : Node
    {
        if (root.Name == name && root is T match) return match;
        foreach (Node child in root.GetChildren()) { var found = Find<T>(child, name); if (found != null) return found; }
        return null!;
    }
    private async Task SelectKey(int slot)
    {
        var key = (Key)(49 + slot);
        GetViewport().PushInput(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true });
        GetViewport().PushInput(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false });
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private static void Cast(ShipFlightController player, string method) => typeof(ShipFlightController)
        .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(player, null);
    private void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); _checks++; }
}

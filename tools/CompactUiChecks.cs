using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Equipment;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Ui;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class CompactUiChecks : Node3D
{
    private int _checks;
    private void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    private void Mouse(Button button, bool pressed)
    {
        Vector2 point = button.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = pressed }, true);
    }
    private async Task Click(Button button)
    {
        for (Node? parent = button.GetParent(); parent != null; parent = parent.GetParent())
            if (parent is ScrollContainer scroll) { scroll.EnsureControlVisible(button); await Frames(2); break; }
        Mouse(button, true); Mouse(button, false); await Frames(2);
    }
    private static T Find<T>(Node node, string name) where T : Node => (T)node.FindChild(name, true, false);
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();

    private async Task Run()
    {
        WorldSeeds.UseSeed(7319);
        try
        {
            PlanetSizes.SelectedIndex = 1;
            var game = GD.Load<PackedScene>("res://scenes/PlanetGame.tscn").Instantiate<MagicCarpetRemastered.Scripts.Game.Game>(); AddChild(game);
            foreach (Node node in game.GetChildren()) if (node is not CanvasLayer) { node.SetPhysicsProcess(false); node.SetProcess(false); }
            var ship = game.GetNode<ShipFlightController>("PlayerShip");
            var home = game.GetNode<FactionBase>("PlayerBase");
            var hud = game.GetNode<Hud>("Hud"); var ui = game.GetNode<BaseManagement>("BaseManagement");
            var bar = Find<EquipmentBar>(hud, "EquipmentBar");
            await Frames(4); hud._Process(1); bar.Refresh();
            Check(hud.FindChild("Vitals", true, false) == null && hud.FindChild("Objective", true, false) == null,
                "Persistent corner vitals and objective panels are removed");
            var vitals = Find<HBoxContainer>(hud, "VitalBars");
            Check(vitals.GetChildCount() == 3 && Find<ProgressBar>(hud, "EnergyGauge").Value == ship.Energy
                && Find<ProgressBar>(hud, "AntimatterGauge").MaxValue == home.EffectiveStorageCapacity,
                "Three vertical gauges show actual health, rechargeable energy and base storage");
            Check(vitals.GetChildren().OfType<VBoxContainer>().Select(c => c.GetChild<Label>(2).Text).SequenceEqual(new[] { "H", "E", "A" })
                && vitals.GetChildren().OfType<VBoxContainer>().All(c => !c.GetChild<Label>(0).Text.Contains('/')),
                "Compact gauges use letter icons and current values only");
            Check(!Find<PanelContainer>(hud, "Feedback").Visible, "Routine gameplay feedback is not displayed as a toast");
            var cards = Find<HBoxContainer>(bar, "EquipmentSlots").GetChildren().OfType<UiTile>().ToArray();
            Check(cards.Length == 4 && cards.Select(c => c.Name.ToString()).SequenceEqual(new[] { "EquipmentSlot_1", "EquipmentSlot_2", "EquipmentSlot_3", "EquipmentSlot_4" }),
                "HUD is exactly four numbered equipment cards in one horizontal row");
            Check(cards[0].Title.Text.Contains("1") && cards[0].Title.Text.Contains("Plasma") && cards[1].State.Text == "Empty", "Starter item and empty slots are explicit");
            Check(((StyleBoxFlat)cards[0].GetThemeStylebox("normal")).BorderWidthLeft == 2 && ((StyleBoxFlat)cards[1].GetThemeStylebox("normal")).BorderWidthLeft == 1,
                "Selected item has a distinct outline");
            Check(cards.All(c => c.TooltipText.Length > 20), "Occupied and empty HUD slots have useful tooltips");
            Check(!hud.HasNode("HudRoot/Loadout") && !game.GetNode<BoonChoices>("BoonChoices").FindChildren("*", "Button", true, false).OfType<Button>().Any(b => b.Text.StartsWith("H  ")),
                "Old verbose loadout and separate boon hint are removed");
            Check(Find<HBoxContainer>(hud, "InterfaceActions").GetChildren().OfType<Button>().Count() == 3, "B/M/H share one entry row");
            var site = game.GetChildren().OfType<StrategicPoint>().First();
            site.Advance(4, true, false); hud._Process(1);
            var capturePanel = Find<PanelContainer>(hud, "CapturePanel");
            Check(capturePanel.Visible && Math.Abs(Find<ProgressBar>(hud, "CaptureProgress").Value - 0.4) < 0.0001,
                "Screen-space capture bar displays actual site progress without camera aiming");
            site.Advance(1, false, false); hud._Process(1);
            Check(!capturePanel.Visible && Math.Abs(site.CaptureProgress - 0.35) < 0.0001,
                "Leaving hides HUD bar while gameplay progress drains slowly");
            site.Advance(10, true, false); site.Advance(4, false, true); site.Advance(1, true, false); hud._Process(1);
            Check(capturePanel.Visible && capturePanel.GetChild<VBoxContainer>(0).GetChild<Label>(0).Text.Contains("Restoring"),
                "Returning defender sees restoration stage in the screen HUD");
            site.Advance(1, true, true); hud._Process(1);
            Check(capturePanel.GetChild<VBoxContainer>(0).GetChild<Label>(0).Text.Contains("Contested")
                && Math.Abs(Find<ProgressBar>(hud, "CaptureProgress").Value - 0.3) < 0.0001, "Contested HUD shows frozen stage progress");
            site.Advance(10, true, false); site.Advance(0.01, false, false); hud._Process(1);
            Input.MouseMode = Input.MouseModeEnum.Visible;
            int energy = ship.Energy;
            Mouse(cards[2], true); Input.ActionPress("activate_primary"); ship._PhysicsProcess(0.01); bar.Refresh();
            Check(ship.SelectedEquipmentSlot == 2 && ship.Energy == energy && !game.GetChildren().OfType<WeaponProjectile>().Any(),
                "Cursor slot selection suppresses held LMB without casting");
            Mouse(cards[2], false); Input.ActionRelease("activate_primary"); ship._PhysicsProcess(0); bar.Refresh();
            await Click(Find<Button>(hud, "BaseAction")); hud._Process(1); bar.Refresh();
            Check(ui.IsOpen && ship.GameplayInputBlocked && !GetTree().Paused && !bar.Visible && !Find<HBoxContainer>(hud, "InterfaceActions").Visible,
                "Entry click opens live management and hides redundant flight controls");
            var tabs = Find<TabContainer>(ui, "ProjectTabs"); tabs.CurrentTab = 1; await Frames(3);
            Check(tabs.GetTabCount() == 4 && ui.FindChild("ActiveProjects", true, false) == null
                && Find<UiTile>(ui, "Order_Tier").GetGlobalRect().End.Y < tabs.GetGlobalRect().Position.Y,
                "Four project tabs sit under header upgrade button without a separate active-project strip");
            var repair = Find<UiTile>(ui, "Order_repair-matrix"); var gridUpgrade = Find<UiTile>(ui, "Order_base-armor");
            Check(repair.Glyph.IconId == "repair-matrix" && repair.Title.Text.Contains("Repair") && repair.Detail.Text.Contains("40 A") && !repair.Detail.Text.Contains(" M"),
                "Upgrade tile shows original glyph and antimatter-only cost");
            Check(gridUpgrade.Disabled && gridUpgrade.TooltipText.Contains("Requires tier 2") && gridUpgrade.TooltipText.Contains("70 antimatter"),
                "Unavailable tiles preserve exact tier/cost reasons in tooltip");
            Check(gridUpgrade.TierBadge.Text == "II" && gridUpgrade.SelfModulate.A < 1,
                "Unavailable upgrades are faded and show the Roman required-tier badge");
            var tooltip = repair._MakeCustomTooltip(repair.TooltipText);
            Check(tooltip.GetChild<Label>(0).CustomMinimumSize.X == 310 && tooltip.GetChild<Label>(0).Text.Contains("30s"), "Custom tooltips bound width and include duration"); tooltip.Free();
            home.DepositAntimatter(1000); ui.Refresh();
            int before = home.StoredAntimatter;
            await Click(repair);
            var project = home.Construction.GetActive(ConstructionSlot.Defense)!;
            Check(project != null && home.StoredAntimatter == before - 40 && repair.Disabled,
                "Actual icon tile click starts one paid project and disables duplicate order");
            ui.Refresh(); Check(repair.State.Text.Contains("Building") && repair.BuildProgress.Visible, "Active tile has overlaid progress state");
            home.SetPhysicsProcess(true); double elapsed = project!.ElapsedSeconds; await Frames(8); home.SetPhysicsProcess(false);
            Check(project.ElapsedSeconds > elapsed && ui.IsOpen && !GetTree().Paused, "Construction remains live with compact UI open");
            home.Construction.Advance(20); ui.Refresh();
            Check(repair.BuildProgress.Value > 0.3 && repair.ProjectInstanceId == project.InstanceId,
                "Button overlay fills with actual elapsed work and keeps its own cancellation identity");
            if (DisplayServer.GetName() != "headless") await Capture("CompactActiveBuild.png");
            await Click(repair.CancelButton);
            Check(project.State == ConstructionState.Cancelled && home.StoredAntimatter == before, "Compact cancellation preserves exact antimatter refund");
            for (int rank = 1; rank <= 3; rank++)
            {
                home.DepositAntimatter(1000); ui.Refresh(); await Click(repair); home.Construction.Advance(100); ui.Refresh();
                Check(repair.Stars.Text == new string('★', rank), "One tile advances sequential bronze/silver/gold stars");
                Check(rank == 1 ? repair.Stars.Modulate.R > repair.Stars.Modulate.G
                    : rank == 2 ? repair.Stars.Modulate.B >= repair.Stars.Modulate.R : repair.Stars.Modulate.R > repair.Stars.Modulate.B,
                    "Completed stars have bronze/silver/gold colors");
            }
            // Purchase and install a complete four-slot loadout through ordinary APIs.
            home.DepositAntimatter(1000);
            Check(ui.OrderNextTier() && Find<UiTile>(ui, "Order_Tier").BuildProgress.Visible,
                "Header upgrade button displays its paid construction overlay");
            home.Construction.Advance(60);
            home.TryCompleteTierUpgrade(3); home.TryCompleteTierUpgrade(4);
            foreach (string id in new[] { "energy-shield", "dash", "pulse-burst" })
            {
                home.DepositAntimatter(1000); Check(ui.OrderProject(ShipEquipmentCatalog.ProjectId(id, 1)), "Tile catalog retains shared equipment research"); home.Construction.Advance(60);
            }
            tabs.CurrentTab = 3; ui.Refresh(); await Frames(4);
            Check(ui.FindChild("ShipSections", true, false) == null, "Ship equipment uses one integrated view");
            var install = Find<Button>(ui, "Equip_energy-shield");
            Check(!install.Disabled && install.TooltipText.Contains("row order"), "Equipment button explains automatic key assignment");
            await Click(install); Check(ship.EquipmentSlots[1] == "energy-shield" && install.ButtonPressed, "Viewport click equips automatically at home");
            Check(ui.ToggleEquipment("dash") && ui.ToggleEquipment("pulse-burst"), "Mixed equipment still occupies only four shared slots");
            Check(ship.EquipmentSlots.SequenceEqual(new[] { "plasma-bolt", "pulse-burst", "energy-shield", "dash" }), "Automatic slots follow displayed row order");
            home.DepositAntimatter(1000); Check(ui.OrderProject(ShipEquipmentCatalog.ProjectId("guardian-drone", 1)), "Fifth item can be researched at tier IV"); home.Construction.Advance(100); ui.Refresh();
            Check(Find<Button>(ui, "Equip_guardian-drone").Disabled && !ui.ToggleEquipment("guardian-drone"), "Fifth toggle cannot replace an item silently");
            ui.SetOpen(false); ship.TrySelectEquipmentFromInterface(2); ui.SetOpen(true);
            Check(ui.ToggleEquipment("pulse-burst") && ship.SelectedEquipmentSlot == 1 && ship.EquipmentSlots[1] == "energy-shield", "Compaction preserves selected item identity");
            Check(ui.ToggleEquipment("pulse-burst") && ship.SelectedEquipmentSlot == 2, "Reinsertion preserves selected item identity");
            home.DepositAntimatter(1000); Check(ui.OrderProject(ShipEquipmentCatalog.ProjectId("energy-shield", 2)), "Equipped ranks can be researched"); home.Construction.Advance(90); ui.Refresh();
            Check(ui.FindChild("ApplyRank_energy-shield", true, false) == null && ship.HasPendingLoadout, "Completed rank awaits automatic home synchronization with no Apply button");
            ship._PhysicsProcess(0); ui.Refresh(); Check(ship.GetEquipmentRank("energy-shield") == 2, "Home tick automatically installs completed rank without changing loadout");
            Vector3 homePosition = ship.GlobalPosition; ship.GlobalPosition += ship.GlobalBasis.X * 80; ui.Refresh();
            Check(!install.Disabled && install.TooltipText.Contains("return home"), "Remote equipment planning stays available with installation explanation");
            ship.GlobalPosition = homePosition; ui.Refresh();
            if (DisplayServer.GetName() != "headless")
            {
                await Capture("CompactResearch.png");
                var locked = Find<UiTile>(ui, "Order_phase-dash");
                ((ScrollContainer)tabs.GetChild(3)).EnsureControlVisible(locked); await Frames(3);
                Vector2 hover = locked.GetGlobalRect().GetCenter();
                GetViewport().PushInput(new InputEventMouseMotion { Position = hover, GlobalPosition = hover }, true);
                await ToSignal(GetTree().CreateTimer(0.9), SceneTreeTimer.SignalName.Timeout); await Capture("CompactTooltip.png");
            }
            ui.SetOpen(false); ship.RespawnAt(ship.GlobalPosition); ship._PhysicsProcess(0); bar.Refresh(); hud._Process(1);
            Check(bar.Visible && cards[2].Title.Text.Contains("Shield") && cards[1].Title.Text.Contains("Burst"), "Flight HUD follows the actual installed loadout");
            Input.MouseMode = Input.MouseModeEnum.Visible;
            energy = ship.Energy; Mouse(cards[2], true); Input.ActionPress("activate_primary"); ship._PhysicsProcess(0.01);
            Check(ship.SelectedEquipmentSlot == 2 && !ship.IsShielded && ship.Energy == energy, "Clicking an installed Shield selects it without spending or activating");
            Mouse(cards[2], false); Input.ActionRelease("activate_primary"); ship._PhysicsProcess(0); bar.Refresh();
            Input.ActionPress("activate_primary"); ship._PhysicsProcess(0.01); Input.ActionRelease("activate_primary"); bar.Refresh();
            Check(ship.IsShielded && cards[2].State.Text.EndsWith("s") && Find<ColorRect>(cards[2], "CooldownOverlay").Visible,
                "Selected LMB activation shows real cooldown overlay/countdown");
            ship.SpendEnergy(ship.Energy); bar.Refresh();
            Check(cards[3].State.Text.Contains("Need") && !cards[3].Disabled, "Unaffordable equipment stays selectable with distinct energy state");
            if (DisplayServer.GetName() != "headless")
            {
                var terrain = game.GetNode<HeightmapArena>("HeightmapArena");
                ship.GlobalPosition = terrain.OnSurface(home.GlobalPosition + home.GlobalBasis.X * 28, 6);
                terrain.Orient(ship); ship._PhysicsProcess(0); terrain.RefreshPlanetDetail();
                await Capture("CompactHud.png");
            }
            foreach (Vector2I size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
            {
                GetTree().Root.ContentScaleSize = size;
                await Frames(5); hud._Process(1); bar.Refresh();
                Check(cards.All(c => c.GetGlobalRect().Position.Y == cards[0].GetGlobalRect().Position.Y), "Equipment remains a single row at supported viewport sizes");
                var actions = Find<HBoxContainer>(hud, "InterfaceActions");
                Check(!actions.GetGlobalRect().Intersects(bar.GetGlobalRect()), "Consolidated hints do not overlap the equipment bar");
                Check(vitals.GetGlobalRect().End.X < bar.GetGlobalRect().Position.X
                    && Find<PanelContainer>(hud, "BaseHealthPanel").GetGlobalRect().Position.X > bar.GetGlobalRect().End.X,
                    "Vitals are left of weapons and home-base health is beneath the right-side map");
                var basePanel = Find<PanelContainer>(hud, "BaseHealthPanel");
                Check(Mathf.Abs((vitals.GetGlobalRect().Position.X + basePanel.GetGlobalRect().End.X) * 0.5f
                    - GetViewport().GetVisibleRect().Size.X * 0.5f) < 1, "Complete HUD including radar/base HP is bottom centered");
                Check(FlightHudLayout.RadarOrigin(GetViewport().GetVisibleRect().Size).X == basePanel.GetGlobalRect().Position.X,
                    "Radar and base HP share the same centered layout");
                site.Advance(0.01, true, false); hud._Process(1); await Frames(2);
                Check(capturePanel.Visible && !capturePanel.GetGlobalRect().Intersects(bar.GetGlobalRect())
                    && !capturePanel.GetGlobalRect().Intersects(actions.GetGlobalRect()), $"Capture HUD clears equipment and interface dock at {size}: visible {capturePanel.Visible}, capture {capturePanel.GetGlobalRect()}, bar {bar.GetGlobalRect()}, dock {actions.GetGlobalRect()}");
                if (DisplayServer.GetName() != "headless") await Capture($"CaptureHud{size.X}.png");
                site.Advance(0.01, false, false);
                ui.SetOpen(true); await Frames(4);
                var panel = Find<PanelContainer>(ui, "ManagementPanel");
                Check(panel.GetGlobalRect().Size.X <= GetViewport().GetVisibleRect().Size.X + 1
                    && panel.GetGlobalRect().End.Y <= GetViewport().GetVisibleRect().Size.Y + 1, "Management bounds stay within viewport");
                var shipTab = tabs.GetChild(3); var researchTable = Find<GridContainer>(shipTab, "TierGrid_1");
                Check(researchTable.Columns == 4 && researchTable.GetChildCount() == 4, "Ship rows contain four upgrade types instead of rank columns");
                for (int tab = 0; tab < 4; tab++)
                {
                    tabs.CurrentTab = tab; await Frames(2);
                    for (int tier = 1; tier <= 5; tier++)
                    {
                        var row = Find<GridContainer>(tabs.GetChild(tab), $"TierGrid_{tier}");
                        var cells = row.GetChildren().OfType<VBoxContainer>().Select(cell => cell.GetChild<UiTile>(0)).ToArray();
                        Check(cells.All(tile => Mathf.Abs(tile.GetGlobalRect().Position.Y - cells[0].GetGlobalRect().Position.Y) < 1)
                            && cells.Last().GetGlobalRect().End.X <= panel.GetGlobalRect().End.X,
                            "Tier row columns remain aligned inside management bounds");
                    }
                }
                tabs.CurrentTab = 3; ((ScrollContainer)tabs.GetChild(3)).ScrollVertical = 0; await Frames(2);
                Check(Find<UiTile>(ui, "Order_particle-beam").Disabled && Find<UiTile>(ui, "Order_particle-beam").TooltipText.Contains("not yet playable"),
                    "Future content is visibly pending and cannot take payment");
                if (DisplayServer.GetName() != "headless") await Capture($"CompactUi{size.X}.png");
                ui.SetOpen(false);
            }
            GetTree().Root.ContentScaleSize = new Vector2I(1280, 720);
            GetTree().Paused = true; hud._Process(1); bar.Refresh();
            Check(!bar.Visible && !Find<HBoxContainer>(hud, "InterfaceActions").Visible, "Paused setup/results cannot expose interactive flight HUD");
            GetTree().Paused = false;
            game.QueueFree();
            for (int i = 0; i < 10; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            await Frames(3);
            GD.Print($"Compact UI checks passed: {_checks}"); GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    private async Task Capture(string filename)
    {
        await Frames(3); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Check(GetViewport().GetTexture().GetImage().SavePng($"C:/Users/lblan/AppData/Local/Temp/opencode/{filename}") == Error.Ok, "Rendered compact UI capture saves");
    }
}

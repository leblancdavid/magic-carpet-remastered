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
    private async Task Click(Button button) { Mouse(button, true); Mouse(button, false); await Frames(2); }
    private static T Find<T>(Node node, string name) where T : Node => (T)node.FindChild(name, true, false);
    public override void _Ready() => Callable.From(() => { _ = Run(); }).CallDeferred();

    private async Task Run()
    {
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
            var repair = Find<UiTile>(ui, "Order_repair-matrix"); var gridUpgrade = Find<UiTile>(ui, "Order_energy-grid");
            Check(repair.Glyph.IconId == "repair-matrix" && repair.Title.Text.Contains("Repair") && repair.Detail.Text.Contains("20 A") && repair.Detail.Text.Contains("5 M"),
                "Upgrade tile shows original glyph, short name and independent costs");
            Check(gridUpgrade.Disabled && gridUpgrade.TooltipText.Contains("Requires tier 2") && gridUpgrade.TooltipText.Contains("40 antimatter"),
                "Unavailable tiles preserve exact tier/cost reasons in tooltip");
            var tooltip = repair._MakeCustomTooltip(repair.TooltipText);
            Check(tooltip.GetChild<Label>(0).CustomMinimumSize.X == 310 && tooltip.GetChild<Label>(0).Text.Contains("8s"), "Custom tooltips bound width and include duration"); tooltip.Free();
            home.DepositAntimatter(1000); home.DepositMatter(1000); ui.Refresh();
            int before = home.StoredAntimatter, beforeM = home.StoredMatter;
            await Click(repair);
            var project = home.Construction.GetActive(ConstructionSlot.Defense)!;
            Check(project != null && home.StoredAntimatter == before - 20 && home.StoredMatter == beforeM - 5 && repair.Disabled,
                "Actual icon tile click starts one paid project and disables duplicate order");
            ui.Refresh(); Check(repair.State.Text.Contains("Researching"), "Active tile has progress state");
            home.SetPhysicsProcess(true); double elapsed = project!.ElapsedSeconds; await Frames(8); home.SetPhysicsProcess(false);
            Check(project.ElapsedSeconds > elapsed && ui.IsOpen && !GetTree().Paused, "Construction remains live with compact UI open");
            await Click(Find<Button>(ui, "Cancel_Defense"));
            Check(project.State == ConstructionState.Cancelled && home.StoredAntimatter == before && home.StoredMatter == beforeM, "Compact cancellation preserves both exact refunds");
            // Purchase and install a complete four-slot loadout through ordinary APIs.
            ui.OrderNextTier(); home.Construction.Advance(30);
            foreach (string id in new[] { "energy-shield", "dash", "pulse-burst" })
            {
                home.DepositAntimatter(1000); Check(ui.OrderProject(ShipEquipmentCatalog.ProjectId(id, 1)), "Tile catalog retains shared equipment research"); home.Construction.Advance(30);
            }
            tabs.CurrentTab = 4; var sections = Find<TabContainer>(ui, "ShipSections"); sections.CurrentTab = 1; ui.Refresh(); await Frames(4);
            Check(sections.GetTabCount() == 2 && sections.GetTabTitle(0) == "Research" && sections.GetTabTitle(1) == "Loadout", "Ship research and installation are separate views");
            var selector = Find<OptionButton>(ui, "InstallationSlot"); selector.Select(1); ui.Refresh();
            var install = Find<UiTile>(ui, "Install_energy-shield");
            Check(!install.Disabled && install.TooltipText.Contains("replacing Empty") && install.State.Text.Contains("2"), "Installation tile explains its exact destination/replacement");
            await Click(install); Check(ship.EquipmentSlots[1] == "energy-shield", "Viewport click explicitly installs into selected slot");
            Check(ui.InstallEquipment("dash", 2) && ui.InstallEquipment("pulse-burst", 3), "Mixed equipment still occupies only four shared slots");
            Vector3 homePosition = ship.GlobalPosition; ship.GlobalPosition += ship.GlobalBasis.X * 80; ui.Refresh();
            Check(install.Disabled && install.TooltipText.Contains("Return home"), "Remote install stays visibly unavailable with explanation");
            ship.GlobalPosition = homePosition; ui.Refresh();
            if (DisplayServer.GetName() != "headless")
            {
                await Capture("CompactLoadout.png"); sections.CurrentTab = 0; await Frames(3); await Capture("CompactResearch.png");
                var locked = Find<UiTile>(ui, "Order_equipment-plasma-bolt-3");
                Vector2 hover = locked.GetGlobalRect().GetCenter();
                GetViewport().PushInput(new InputEventMouseMotion { Position = hover, GlobalPosition = hover }, true);
                await ToSignal(GetTree().CreateTimer(0.9), SceneTreeTimer.SignalName.Timeout); await Capture("CompactTooltip.png");
            }
            ui.SetOpen(false); ship.RespawnAt(ship.GlobalPosition); ship._PhysicsProcess(0); bar.Refresh(); hud._Process(1);
            Check(bar.Visible && cards[1].Title.Text.Contains("Shield") && cards[3].Title.Text.Contains("Burst"), "Flight HUD follows the actual installed loadout");
            Input.MouseMode = Input.MouseModeEnum.Visible;
            energy = ship.Energy; Mouse(cards[1], true); Input.ActionPress("activate_primary"); ship._PhysicsProcess(0.01);
            Check(ship.SelectedEquipmentSlot == 1 && !ship.IsShielded && ship.Energy == energy, "Clicking an installed Shield selects it without spending or activating");
            Mouse(cards[1], false); Input.ActionRelease("activate_primary"); ship._PhysicsProcess(0); bar.Refresh();
            Input.ActionPress("activate_primary"); ship._PhysicsProcess(0.01); Input.ActionRelease("activate_primary"); bar.Refresh();
            Check(ship.IsShielded && cards[1].State.Text.EndsWith("s") && Find<ColorRect>(cards[1], "CooldownOverlay").Visible,
                "Selected LMB activation shows real cooldown overlay/countdown");
            ship.SpendEnergy(ship.Energy); bar.Refresh();
            Check(cards[2].State.Text.Contains("Need") && !cards[2].Disabled, "Unaffordable equipment stays selectable with distinct energy state");
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
                ui.SetOpen(true); sections.CurrentTab = 0; await Frames(4);
                var panel = Find<PanelContainer>(ui, "ManagementPanel");
                Check(panel.GetGlobalRect().Size.X <= GetViewport().GetVisibleRect().Size.X + 1
                    && panel.GetGlobalRect().End.Y <= GetViewport().GetVisibleRect().Size.Y + 1, "Management bounds stay within viewport");
                Check(Find<UiTile>(ui, "Order_equipment-plasma-bolt-2").GetParent<GridContainer>().Columns >= 2, "Research grid adapts column count without rebuilding controls");
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

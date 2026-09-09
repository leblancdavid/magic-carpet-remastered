using Godot;
using MagicCarpetRemastered.Scripts.Player;

namespace MagicCarpetRemastered.Scripts.Ui;

public partial class Hud : CanvasLayer
{
    private Label _label = null!;
    private CarpetFlightController? _player;

    public override void _Ready()
    {
        _label = new Label
        {
            Name = "StatusLabel",
            Text = "Finding carpet...",
            Position = new Vector2(24.0f, 20.0f)
        };
        AddChild(_label);
    }

    public override void _Process(double delta)
    {
        _player ??= GetTree().GetFirstNodeInGroup("player") as CarpetFlightController;
        if (_player == null)
        {
            return;
        }

        string damageState = _player.IsInvulnerable ? "   Invulnerable" : string.Empty;
        _label.Text = $"Health: {_player.Health}   Mana: {_player.Mana}   Camera: {_player.CameraMode}{damageState}\nSpeed: {_player.Speed:0.0}   Clearance: {_player.AltitudeAboveTerrain:0.0}\nTerrain mode: {_player.TerrainSpellModeText} ({_player.TerrainSpellManaCostForCurrentMode} mana)\n{_player.TerrainDebugText}\n{_player.StatusMessage}\nDestroy monsters to recover mana. WASD fly  Space/C ascend/descend  LMB fire ({_player.PrimarySpellManaCost} mana)  RMB cast terrain  {_player.TerrainSpellHelpText}  V camera  R restart  Esc mouse";
    }
}

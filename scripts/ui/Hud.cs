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

        _label.Text = $"Health: {_player.Health}   Mana: {_player.Mana}\nWASD fly  Space/C descend/ascend  Mouse look  LMB fire  Esc mouse";
    }
}

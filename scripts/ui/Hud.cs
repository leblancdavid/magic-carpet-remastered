using Godot;
using MagicCarpetRemastered.Scripts.Game;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Scripts.Ui;

public partial class Hud : CanvasLayer
{
    private Label _label = null!;
    private CarpetFlightController? _player;
    private ManaWell? _well;
    private CastleKeep? _playerCastle;
    private CastleKeep? _enemyCastle;
    private EnemyEcosystemDirector? _enemyEcosystem;

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
        _well ??= GetTree().GetFirstNodeInGroup("mana_well") as ManaWell;
        _playerCastle ??= GetTree().GetFirstNodeInGroup("player_castle") as CastleKeep;
        _enemyCastle ??= GetTree().GetFirstNodeInGroup("enemy_castle") as CastleKeep;
        _enemyEcosystem ??= GetTree().GetFirstNodeInGroup("enemy_ecosystem_director") as EnemyEcosystemDirector;
        if (_player == null)
        {
            return;
        }

        string damageState = _player.IsInvulnerable ? "   Invulnerable" : string.Empty;
        string wellText = _well == null ? "Well: unavailable" : $"Well: {_well.StoredMana}/{_well.StorageCapacity}";
        string playerCastleText = _playerCastle == null ? "Player castle: unavailable" : _playerCastle.GetStatusText();
        string enemyCastleText = _enemyCastle == null ? "Enemy castle: unavailable" : _enemyCastle.GetStatusText();
        string ecosystemText = _enemyEcosystem == null ? "Ecosystem: unavailable" : _enemyEcosystem.StatusText;
        _label.Text = $"Health: {_player.Health}   Mana: {_player.Mana}/{_player.ManaCapacity}   Camera: {_player.CameraMode}{damageState}\nSpeed: {_player.Speed:0.0}   Clearance: {_player.AltitudeAboveTerrain:0.0}   {wellText}\n{playerCastleText}\n{enemyCastleText}\n{ecosystemText}\nTerrain mode: {_player.TerrainSpellModeText} ({_player.TerrainSpellManaCostForCurrentMode} mana)\n{_player.TerrainDebugText}\n{_player.StatusMessage}\nColor guide: orange swarm, green beast, blue caster, red sieger, yellow thief, purple wizard\nBank excess mana at the well. Route mana to your castle. Clear swarms, thieves, beasts, and wizards. WASD fly  Space/C ascend/descend  LMB fire ({_player.PrimarySpellManaCost} mana)  RMB cast terrain  {_player.TerrainSpellHelpText}  V camera  R restart  Esc mouse";
    }
}

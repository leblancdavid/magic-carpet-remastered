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
    private WorldManaTracker? _manaTracker;

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
        _manaTracker ??= GetTree().GetFirstNodeInGroup("world_mana_tracker") as WorldManaTracker;
        if (_player == null)
        {
            return;
        }

        string damageState = _player.IsInvulnerable ? "   Invulnerable" : string.Empty;
        string wellText = _well == null ? "Well: landmark (fertile source drained)" : $"Well: {_well.StoredMana}/{_well.StorageCapacity}";
        string trackerText = _manaTracker == null
            ? "Mana world: unavailable"
            : $"Mana world: {_manaTracker.TotalMana} total   Neutral {_manaTracker.NeutralLooseMana}   Player {_manaTracker.PlayerControlledMana}   Enemy {_manaTracker.EnemyControlledMana}   Player share {_manaTracker.PlayerShare01 * 100.0f:0}% (quota {_manaTracker.EquilibriumFraction * 100.0f:0}%)";
        string playerCastleText = _playerCastle == null ? "Player castle: unavailable" : _playerCastle.GetStatusText();
        string enemyCastleText = _enemyCastle == null ? "Enemy castle: unavailable" : _enemyCastle.GetStatusText();
string ecosystemText = _enemyEcosystem == null ? "Ecosystem: unavailable" : _enemyEcosystem.StatusText;
        _label.Text = $"Health: {_player.Health}   Mana: {_player.Mana}/{_player.ManaCapacity}   Claimed: {_player.ClaimedMana}   Camera: {_player.CameraMode}{damageState}   {_player.ShieldStatusText}\nSpeed: {_player.Speed:0.0}   Clearance: {_player.AltitudeAboveTerrain:0.0}   {wellText}\n{trackerText}\n{playerCastleText}\n{enemyCastleText}\n{ecosystemText}\n{_player.SpellLoadoutText}\nTerrain mode: {_player.TerrainSpellModeText} ({_player.TerrainSpellManaCostForCurrentMode} mana)\n{_player.TerrainDebugText}\n{_player.StatusMessage}\nMana guide: gray loose neutral, cyan yours, red rival   Possess (Tab) claims aim-aimed loose mana   Balloons haul for their castle; pop enemy balloons to spill mana\nColor guide: orange swarm, green beast, blue caster, red sieger, yellow thief, purple wizard\nClaim mana to grow your pool; current mana regenerates from claimed mana.\nClear swarms, thieves, beasts, and wizards. WASD fly  Space/C ascend/descend  LMB {_player.PrimarySpellName}  Tab quick spell  Q {_player.AreaSpellName}  E {_player.ShieldSpellName}  F {_player.MobilitySpellName}  G {_player.SummonSpellName}  RMB {_player.TerrainSpellName}  {_player.TerrainSpellHelpText}  V camera  R restart  Esc mouse";
    }
}

using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Equipment;

namespace MagicCarpetRemastered.Scripts.Ui;

public partial class EquipmentBar : VBoxContainer
{
    private readonly UiTile[] _slots = new UiTile[4];
    private readonly ColorRect[] _cooldown = new ColorRect[4];
    private readonly bool[] _selected = new bool[4];
    private readonly bool[] _styled = new bool[4];
    private Label _builtIns = null!;
    private ShipFlightController? _ship;
    private StyleBoxFlat _normal = null!, _selection = null!;
    public override void _Ready()
    {
        Name = "EquipmentBar"; MouseFilter = MouseFilterEnum.Ignore;
        AddThemeConstantOverride("separation", 4);
        _normal = UiTile.Frame(new Color(0.04f, 0.075f, 0.11f, 0.94f), new Color(0.2f, 0.36f, 0.43f));
        _selection = UiTile.Frame(new Color(0.07f, 0.17f, 0.22f, 0.97f), new Color(0.5f, 0.94f, 1), 2);
        var row = new HBoxContainer { Name = "EquipmentSlots", MouseFilter = MouseFilterEnum.Ignore }; row.AddThemeConstantOverride("separation", 6); AddChild(row);
        for (int i = 0; i < 4; i++)
        {
            int slot = i;
            _slots[i] = new UiTile { Name = $"EquipmentSlot_{i + 1}", CustomMinimumSize = new Vector2(88, 98) };
            row.AddChild(_slots[i]);
            _slots[i].Title.AddThemeFontSizeOverride("font_size", 12);
            _slots[i].ButtonDown += () => _ship?.TrySelectEquipmentFromInterface(slot);
            _cooldown[i] = new ColorRect { Name = "CooldownOverlay", Color = new Color(0.02f, 0.04f, 0.07f, 0.85f), MouseFilter = MouseFilterEnum.Ignore, Visible = false };
            _slots[i].AddChild(_cooldown[i]);
        }
        _builtIns = new Label { Name = "BuiltInIndicators", HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        _builtIns.AddThemeFontSizeOverride("font_size", 12); AddChild(_builtIns); Refresh();
    }
    public override void _Process(double delta) => Refresh();
    public override void _ExitTree() { _normal?.Dispose(); _selection?.Dispose(); }
    public void Refresh()
    {
        _ship ??= GetTree().GetFirstNodeInGroup("player") as ShipFlightController;
        if (_ship == null) return;
        Visible = !GetTree().Paused && !_ship.GameplayInputBlocked;
        for (int i = 0; i < 4; i++)
        {
            string? id = _ship.EquipmentSlots[i]; EquipmentDefinition? item = id == null ? null : _ship.FindEquipment(id);
            float cooldown = id == null ? 0 : _ship.EquipmentCooldownRemaining(id);
            bool unavailable = item != null && _ship.Energy < item.EnergyCost;
            string state = _ship.Health <= 0 ? "Down" : item == null ? "Empty" : cooldown > 0 ? $"{cooldown:0.0}s" : unavailable ? $"Need {item!.EnergyCost} E" : "Ready";
            _slots[i].Glyph.IconId = id ?? "empty";
            _slots[i].Display($"{i + 1}  {(id == null ? "Empty" : UiIcon.ShortName(id))}",
                item == null ? "—" : $"{BaseManagement.Rank(item.Rank)} · {item.EnergyCost} E", state,
                item == null ? $"Slot {i + 1} · Empty\nResearch equipment with B, then install at home in Ship → Loadout."
                    : $"Slot {i + 1} · {item.RankedName}\n{item.Description}\n{item.EnergyCost} energy · {item.CooldownSeconds:0.##}s base cooldown\n"
                        + $"{state}\n{i + 1} selects · LMB activates · Tab cycles. Cursor click selects only.", _ship.Health <= 0);
            _slots[i].Glyph.Modulate = unavailable ? new Color(0.85f, 0.5f, 0.3f) : Colors.White;
            if (unavailable && cooldown <= 0) _slots[i].State.Modulate = new Color(1, 0.65f, 0.32f);
            bool selected = _ship.SelectedEquipmentSlot == i;
            if (!_styled[i] || selected != _selected[i])
            { _styled[i] = true; _selected[i] = selected; _slots[i].AddThemeStyleboxOverride("normal", selected ? _selection : _normal); }
            _cooldown[i].Visible = cooldown > 0;
            float full = id == "plasma-bolt" ? _ship.EffectiveFireCooldown : item?.CooldownSeconds ?? 1;
            float height = 32 * Mathf.Clamp(cooldown / Mathf.Max(0.01f, full), 0, 1);
            _cooldown[i].Position = new Vector2(7, 7 + 32 - height); _cooldown[i].Size = new Vector2(Mathf.Max(0, _slots[i].Size.X - 14), height);
        }
        _builtIns.Text = $"RMB Claim · {_ship.ClaimEquipmentEnergyCost} E     Shift · {_ship.BoosterStatusText.Replace("Boost ", "")}     Space · {_ship.JumpEnergyCost} E";
    }
}

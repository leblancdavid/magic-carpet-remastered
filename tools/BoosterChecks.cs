using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MagicCarpetRemastered.Scripts.Player;
using MagicCarpetRemastered.Scripts.Ui;
using MagicCarpetRemastered.Scripts.World;

namespace MagicCarpetRemastered.Tools;

public partial class BoosterChecks : Node3D
{
    private int _checks;
    public override void _Ready() => Callable.From(() => { _ = RunChecks(); }).CallDeferred();
    private void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new Exception(message);
    }
    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }
    private void Shift(bool pressed, bool echo = false) => GetViewport().PushInput(
        new InputEventKey { PhysicalKeycode = Key.Shift, Keycode = Key.Shift, Pressed = pressed, Echo = echo }, true);

    private async Task RunChecks()
    {
        try
        {
            foreach (bool spherical in new[] { false, true })
            {
                var room = new Node3D(); AddChild(room);
                var arena = new HeightmapArena { IsSpherical = spherical, PlanetRadius = 800 }; room.AddChild(arena);
                Vector3 origin = spherical ? arena.PlanetCenter + Vector3.Down * 800 : new Vector3(40, 0, 40);
                var home = new FactionBase { InitialStoredAntimatter = 120, Position = arena.OnSurface(origin) };
                room.AddChild(home); home.SetPhysicsProcess(false);
                Vector3 spawn = arena.OnSurface(origin, 8);
                var player = new ShipFlightController { Position = spawn, EnergyRegenFractionPerSecond = 0 };
                room.AddChild(player); player.SetPhysicsProcess(false);
                var ui = new BaseManagement(); room.AddChild(ui);
                int energy = player.Energy, storage = home.StoredAntimatter;
                Shift(true);
                Check(player.IsBoosting && player.Energy == energy - 4, "Physical Shift activates the built-in booster at its exact energy cost");
                Check(home.StoredAntimatter == storage && player.EnergyCapacity == storage && home.SpentAntimatter == 0,
                    "Boost does not spend storage, lower the energy limit or count as construction");
                Check(player.EquipmentSlots.Count(id => id != null) == 1 && !player.IsEquipmentInstalled("dash"),
                    "Starter-only loadout boosts without Dash research or a slot");
                int spent = player.Energy;
                Check(!player.TryActivateBooster() && player.Energy == spent, "Active/cooldown duplicate rejects without payment");
                player.SetPhysicsProcess(true); await Frames(16); player.SetPhysicsProcess(false);
                Check(arena.SurfaceDistance(spawn, player.GlobalPosition) > 6, "Real solver frames move the boosted ship over flat/globe surfaces");
                Check(arena.Tangent(player.Velocity, player.GlobalPosition).Length() > 35 && arena.Clearance(player.GlobalPosition) <= 12.01f,
                    "Boost supplies surface-relative speed and respects flight clearance, including the globe underside");
                player._PhysicsProcess(0.7);
                Check(!player.IsBoosting && player.BoosterCooldownRemaining > 0 && player.Energy == spent, "Short thrust ends before cooldown without ongoing payment");
                player._PhysicsProcess(2);
                Shift(true, true);
                Check(!player.IsBoosting && player.Energy == spent, "Held Shift/key echo cannot auto-retrigger after cooldown");
                Shift(false); Shift(true);
                Check(player.IsBoosting && player.Energy == spent - 4, "A fresh Shift press can retrigger after cooldown");
                ui.SetOpen(true);
                Check(!player.IsBoosting, "Opening live management ends active thrust");
                player._PhysicsProcess(3); energy = player.Energy;
                Shift(false); Shift(true);
                Check(!player.IsBoosting && player.Energy == energy && !player.TryActivateBooster(), "Management owns Shift and rejects direct boost calls");
                ui.SetOpen(false); Shift(true, true);
                Check(!player.IsBoosting, "Closing UI while Shift is held does not boost");
                Shift(false);
                GetTree().Paused = true;
                Check(!player.TryActivateBooster(), "Paused boost rejects without payment");
                GetTree().Paused = false;
                player.SpendEnergy(player.Energy - 3); energy = player.Energy;
                Check(!player.TryActivateBooster() && player.Energy == energy && player.BoosterCooldownRemaining == 0,
                    "Insufficient energy rejects atomically without starting cooldown");
                home.WithdrawAntimatter(storage - 2);
                Check(!player.TryActivateBooster() && player.EnergyCapacity == 2, "Storage-backed low energy limit blocks boost");
                home.DepositAntimatter(118); player.RespawnAt(spawn);
                Check(player.TryActivateBooster(), "Respawn restores built-in booster independently of inventory");
                GetTree().Paused = true; await Frames(3);
                Check(player.IsBoosting && player.BoosterCooldownRemaining == player.BoosterCooldownSeconds, "Pause freezes active duration and cooldown");
                GetTree().Paused = false;
                home.Construction.TryStart("equipment-dash-1", out _); home.Construction.Advance(100);
                Check(player.TryInstallEquipment("dash", 1, out _), "Dash research still requires explicit home installation");
                energy = player.Energy;
                typeof(ShipFlightController).GetMethod("CastMobilityEquipment", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(player, null);
                Check(!player.IsBoosting && player.Energy == energy - 10 && player.Velocity.Length() > player.BoosterSpeed,
                    "Aimed Dash costs more, is faster and supersedes boost instead of stacking");
                Check(player.EffectiveDashSpeed > player.BoosterSpeed, "Rank-I Dash already exceeds booster speed");
                player.RespawnAt(spawn); player.TryActivateBooster();
                typeof(ShipFlightController).GetField("_damageInvulnerabilityTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, 0f);
                player.ApplyDamage(1000); energy = player.Energy;
                Check(!player.IsBoosting && !player.TryActivateBooster() && player.Energy == energy, "Death ends boost and blocks payment/activation");
                player.RespawnAt(spawn);
                Check(player.BoosterCooldownRemaining == 0 && !player.IsBoosting && player.IsEquipmentInstalled("dash"), "Respawn resets mobility timers while retaining installed Dash");
                Shift(false); room.Free(); await Frames(2);
            }
            GD.Print($"Booster checks passed: {_checks}"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
}

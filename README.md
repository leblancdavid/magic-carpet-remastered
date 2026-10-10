# Magic Carpet Remastered

A Godot C# playable prototype for a modern spiritual remaster inspired by the classic DOS game Magic Carpet.

## Current Status

The prototype includes carpet flight, combat, deformable island terrain, mana ownership/possession, balloon hauling, castles, rival enemies, a first-pass spell set, and level victory/defeat flow. Win by destroying the rival castle and defeating its wizard, in either order. Mana ownership is a resource system and does not trigger victory. Visuals and audio remain placeholders.

See `docs/BUILD_ROADMAP.md` and `docs/PROGRESS_LOG.md` for priorities and verification history. The original DOS package at `D:\Games\MagicCarpet` is reference material; proprietary files are not copied into this repo.

## Requirements

- Godot 4.6.3 with .NET/C# support (matches the project SDK)
- .NET SDK

Godot is available locally at `D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe`; it is not on `PATH`.

## Structure

- `docs/` - discovery, design, and implementation plans
- `scenes/` - Godot scenes
- `scripts/` - C# gameplay code
- `assets/` - original/remastered assets
- `tools/` - local research/import helpers

## Run And Verify

Open `project.godot` in Godot .NET, build the C# project, and run `scenes/Game.tscn`. Current controls are listed in `docs/PROGRESS_LOG.md`.

The compact HUD shows health/mana, rival castle/wizard objective status, respawn countdowns, and selected spells. Victory requires the rival castle destroyed and its wizard defeated. Both wizards respawn after **10 seconds** while their respective castle survives; a respawn clears the rival's earlier defeat. Destroying the player castle causes defeat. Results freeze gameplay and release the mouse; press **R** to restart. One designated rival is tracked through its respawns; the ecosystem director does not spawn extra wizards. Use **Firebolt** against the rival castle. Press **F3** to show/hide the debug and controls panel (off by default); **PgUp / PgDn** scroll its grouped diagnostics.

Look for the purple-robed **RIVAL WIZARD** riding a magenta carpet near the red castle. Its flight follows terrain/water at a controlled height, approaches mana closely enough to collect it, and caps combat altitude so it cannot drift into the sky.

**Right-click casts Possess** (2 mana, 0.4-second cooldown). Aim at a mana globe or the ground before it: the cyan bolt flies along the aim direction, then skims terrain/water along a fixed horizontal heading and claims the first neutral/rival loose globe it hits. Misses expire after 120 units or 4 seconds. Claimed globes turn cyan and remain in the world for your balloon to collect and deliver to the castle. The player cannot directly collect mana or attract globes; castle delivery increases spell capacity/regeneration. **Tab** cycles the five LMB combat/utility spells. Direct terrain-shaping controls are hidden for now; firebolt impact craters still work.

Loose mana spheres touch the ground/water, roll downhill, and collide with terrain, castles, and solid objects. Their volume scales with mana amount (10 mana has a 0.45-unit radius; 80 mana has a 0.9-unit radius). Same-color spheres merge on physical contact and retain all their mana; different colors stay separate. Carried spheres suspend physics until released.

Castle storage capacity scales with remaining health (rounded up). Damage spills storage above that limit into neutral physical globes around the keep; destruction spills everything and leaves a reduced ruin silhouette. Surviving castles repair **3 HP/second after 5 seconds without damage**. Repair restores storage capacity, but mana must still be recovered. Possess spilled globes before your balloon can haul them home. Spell capacity and regeneration use the starting-power allowance plus mana currently retained in your castle, up to the power cap. Initial castle reserves count toward the bonus; withdrawal or damage reduces it immediately, and repeated deliveries cannot stack permanent power. Current spell mana is clamped when capacity falls. **F3** shows effective/intact storage limits, repair state, and the castle-power bonus. Destroyed castles do not rebuild.

Neutral creatures now drop their intrinsic mana on death (12 mana each by default). Deaths, castle spills, and balloon pops send many small 1–2-mana spheres through outward ballistic arcs; a brief merge delay lets them spread before same-color globes recombine. Resource-bearing neutral spawns add their reserve to the world tally; subsequent deaths/transfers conserve it. The shared flight ceiling is **12 units above terrain/water**, including swarms and ranged casters. See `docs/PLAYTEST_MECHANICS.md` for defaults, respawn/objective behavior, and a hands-on test sequence.

Mana-loop checks also cover damage/destruction spillage, exact integer remainders, possession/balloon recovery, spawn-order-independent initial bonuses, capped power recalculation, withdrawal, and re-delivery. Physics checks verify castle spills settle on the heightmap outside the keep collider.

From the repository root in PowerShell:

```powershell
dotnet build MagicCarpetRemastered.sln
$godot = 'D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe'
& $godot --headless --path . --editor --import --quit
& $godot --headless --path . 'res://tools/ManaLoopChecks.tscn'
& $godot --headless --path . 'res://tools/ManaPhysicsChecks.tscn'
& $godot --headless --path . 'res://tools/LevelObjectiveChecks.tscn'
& $godot --headless --path . 'res://tools/WizardFlightChecks.tscn'
& $godot --headless --path . 'res://tools/PlaytestMechanicsChecks.tscn'
& $godot --headless --path . --quit-after 600
```

The mana-loop regression scene checks mana conservation, player contact/proximity rejection, balloon delivery/overflow/pop, rival collection, exact death drops, possession bolt movement/hits/expiry, and possession controls. The physics scene advances real solver frames to check ground contact, size changes, rolling, obstacle collisions, same-color/chained merges, cargo release, balloon-only player collection, live terrain deformation, and water support. The objective scene checks both kill orders, one-target rejection, mana-quota independence, defeat precedence, firebolt castle damage, result pausing/UI, and actual arena restart. Run that scene without `--headless` to also check mouse recapture. Wizard flight checks advance real physics frames to cover stable altitude, globe acquisition/deposit, bounded combat height, castle-loss movement, and raised-terrain clearance. Regression scenes exit nonzero on failure. The final command is an arena startup/processing smoke run. Manual playtesting is still needed for control feel, readability, and balance.

# Magic Carpet Remastered

A Godot C# playable prototype for a modern spiritual remaster inspired by the classic DOS game Magic Carpet.

## Current Status

The prototype includes carpet flight, combat, deformable island terrain, mana ownership/possession, balloon hauling, castles, rival enemies, and a first-pass spell set. The current milestone is stabilizing and playtesting the mana loop before adding a complete level's win/loss flow. Visuals and audio remain placeholders.

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

The compact HUD shows health/mana, mana-control progress, and selected spells. Press **F3** to show/hide the debug and controls panel (off by default); **PgUp / PgDn** scroll its grouped diagnostics.

**Right-click casts Possess** (2 mana, 0.4-second cooldown). Aim at a mana globe or the ground before it: the cyan bolt flies along the aim direction, then skims terrain/water along a fixed horizontal heading and claims the first neutral/rival loose globe it hits. Misses expire after 120 units or 4 seconds. Unclaimed globes sit near the surface; claimed globes turn cyan and can home to you or be hauled by your balloon. **Tab** cycles the five LMB combat/utility spells. Direct terrain-shaping controls are hidden for now; firebolt impact craters still work.

From the repository root in PowerShell:

```powershell
dotnet build MagicCarpetRemastered.sln
$godot = 'D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe'
& $godot --headless --path . --editor --import --quit
& $godot --headless --path . 'res://tools/ManaLoopChecks.tscn'
& $godot --headless --path . --quit-after 600
```

The regression scene checks mana conservation, orb homing, balloon delivery/overflow/pop, rival collection, exact death drops, possession bolt movement/hits/expiry, and possession controls; it exits nonzero on failure. The final command is an arena startup/processing smoke run. Manual playtesting is still needed for control feel, readability, and balance.

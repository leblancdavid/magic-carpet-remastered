# Magic Carpet Remastered Discovery

## Current Repository

- `D:\Dev\Games\magic-carpet-remastered` was empty at discovery time.
- Git is initialized, with no commits yet on `main`; the configured upstream branch is gone.
- The project is now initialized as a minimal Godot C# shell.

## Local Tooling

- .NET SDK is available: `9.0.307`.
- `godot` is not on `PATH`, so Godot import/run verification needs a Godot executable path or PATH setup.

## Reference Godot Projects

`D:\Dev\Games\rustgrindgalaxy`:

- Godot C# project using `Godot.NET.Sdk/4.6.3` and `net8.0`.
- Uses `project.godot`, `scenes/`, `scripts/`, `assets/`, and `docs/`.
- Good reference for action-game structure, player/world separation, effects, procedural level pieces, and PixelLab art documentation.

`D:\Dev\Games\kbtv`:

- Godot C# project using `Godot.NET.Sdk/4.6.3` and `net8.0`/Android `net9.0`.
- Good reference for larger docs organization, systems docs, tooling docs, tests, and generated asset workflows.

## Original DOS Game Inventory

Original game folder: `D:\Games\MagicCarpet`.

- Package appears to be a GamesNostalgia DOSBox wrapper.
- `MagicCarpet.bat` launches `DOSBOX\DOSBox.exe` with `DOSBOX\dosbox.conf` and `DOSBOX\dosbox_single.conf`.
- `dosbox_single.conf` mounts `.\magic` as `C:` and launches `carpet`.
- Key executables found: `magic\carpet.exe`, `magic\data\main.exe`, `magic\dos4gw.exe`, `magic\maphack.exe`, `DOSBOX\DOSBox.exe`.
- Resource/data inventory: 348 files, about 24.9 MB total.
- Most common extensions: 176 `.DAT`, 73 `.INF`, 68 `.TAB`, 8 `.pal`, 5 `.exe`.
- Level files exist as `magic\levels\lev00000.dat` through `lev00069.dat`, paired with `.inf` files, plus `levels.dat`, `levels.tab`, and `all.inf`.
- Sample `.inf` files are binary, not immediately human-readable text.

## Legal/Safety Boundary

The safest remaster path is not to import the original data into the repo. Use the original game for:

- observational notes
- screenshots/video captures for personal reference
- control/feel measurements
- level-design analysis
- high-level system decomposition

Avoid committing:

- original binaries
- original `.dat`, `.inf`, `.tab`, `.pal`, audio, image, or movie files
- direct extracted assets unless there is explicit permission/license clarity

## GPT-6 Astra / AI-Assisted Exploration

No local GPT-6 Astra tool is available in this environment. If access exists elsewhere, the useful role would be research acceleration, not direct blind conversion:

- summarize captured gameplay videos into mechanics notes
- compare screenshots to infer UI/weapon/enemy states
- help document binary file layouts from legal local experiments
- generate original concept art prompts and asset specs
- create test cases from observed mechanics

The actual game should still be built from clean, original Godot systems.

## Original Gameplay Research Notes

Public references and the local DOSBox package inventory support these high-level mechanics for a legally safe remaster target:

- See `docs/ORIGINAL_MECHANICS_REFERENCE.md` for the condensed design reference and implementation priority order.
- Each world has a fixed total mana supply and the objective is to restore equilibrium by securing enough of that mana.
- Mana is not just ammunition. It is a strategic resource that must be possessed/claimed, collected, stored, and defended.
- Mana appears freely in the world and is also produced by destroying monsters or enemy wizard infrastructure.
- Claimed mana is hauled by balloons to the player's castle; unclaimed or enemy-claimed mana should not simply become player power.
- Castle storage and growth are central to progression. A larger/stronger castle supports more resource collection, defense, recovery, and spell use.
- The player's castle is also a home base: it supports health/mana recovery and prevents a death from immediately ending the level while it remains intact.
- Enemy wizards compete over the same mana economy by claiming mana and attacking/leveling castles.
- The player has a small equipped spell set rather than a giant always-active bar; quick access and meaningful tradeoffs matter.
- Carpet flight should remain forgiving: free 3D movement with obstacle/terrain avoidance instead of crash simulation.

Implication for this prototype: the current pickup-refill mana model should be revised into a claimed-mana pool with passive regeneration before detailed spell/enemy tuning. Pickups should eventually contribute to long-term owned mana through possession/castle routing, not only refill current mana.

## 2026-09-09 Gameplay Reference Attempt

Reference link provided by user: `https://www.youtube.com/watch?v=eFMQYfudF_o`.

- Page metadata was reachable and identifies the video as `Magic Carpet (PC) Gameplay`.
- Automated transcript access was blocked/unavailable through the available tools.
- `yt-dlp` was installed locally and used to download a low-resolution reference copy to temp storage only: `C:\Users\lblan\AppData\Local\Temp\opencode\magic-carpet-gameplay-240p.mp4`.
- `ffmpeg` was used to sample frames into temp storage for observation. No video frames or downloaded media were copied into the repo.
- Exact timings still need manual review at higher resolution for HUD numbers, spell list order, castle growth thresholds, balloon timing, and mana regeneration rates.

Observed from sampled frames/contact sheet:

- The HUD is dominated by a top strip: circular minimap at upper-left, player status/spell/resource elements across the top, and spell icons toward the right.
- Mana/resource state appears visually persistent in the HUD rather than being treated as incidental pickup ammo.
- White balloon-like collectors are prominent and visibly travel through the world carrying/collecting mana.
- Castles are large, bright, tiered structures placed in the world/water area, not small token bases; multiple visible structures imply expansion/growth or stronghold complexity.
- Golden mana pearls/orbs are large, readable world objects and often appear around combat/water/shoreline activity.
- Combat creates large bright yellow/orange fireballs and explosions with high screen readability.
- Enemy/creature silhouettes are often black/dark and airborne or near water/shoreline; the player fights while moving quickly rather than stopping for arena duels.
- The minimap appears important for locating mana/castles/entities and should not be treated as optional polish.
- Several frames show the player near castles/balloons/mana in the same moment, reinforcing that flight combat and economy are intertwined.

Design takeaways for the prototype:

- Mana objects should be large and readable in the 3D world.
- Collector balloons/spirits should be central to the mana loop and visible from a distance.
- Castle growth should become visually substantial, not just a small scale/color change.
- The HUD should expose owned/claimed mana, current mana, castle storage/growth, and active spells in a compact top-band style.
- The minimap/radar should eventually show castles, mana, and threats.
- Spell VFX should remain large/readable, with fireball and explosion silhouettes that can be understood at speed.

## Current Prototype Mana Scan

The current Godot prototype still diverges from the original mana loop in these concrete ways:

- `CarpetFlightController` has fixed `ManaCapacity` and `Mana`; pickups call `AddMana()` to refill current mana only.
- There is no `ClaimedMana`, owned mana pool, or passive player mana regeneration from owned/castle mana.
- `ManaPickup` homes directly to the player if the player is not full, otherwise to `ManaWell`; this skips original-style possession/claiming and balloon hauling.
- `ManaWell` acts as a neutral storage/spawner with ambient regen, not as claimed mana in the world.
- `ManaCollectorSpirit` shuttles from `ManaWell` to castles, but this is source-to-castle routing rather than claiming loose mana pearls and returning them to storage.
- `CastleKeep` stores mana and grows by stored mana, but player current mana is not regenerated from castle/claimed storage.
- Enemy thieves and wizards steal/deposit from well/castle storage, but they do not yet contest ownership of world mana.

Recommended implementation delta:

- Add a player-owned claimed mana pool separate from current mana.
- Regenerate current mana up to a cap derived from claimed/castle mana.
- Convert collected/returned mana into claimed mana instead of only current mana.
- Reframe the well as a temporary mana source until world mana possession/balloons exist.
- Make collector spirits/balloons collect claimed loose mana rather than only withdrawing from the well.
- Make enemy thieves/wizards contest claimed/stored mana so resource control becomes the strategic conflict.

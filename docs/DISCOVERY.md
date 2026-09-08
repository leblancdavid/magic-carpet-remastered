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

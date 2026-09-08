# Remaster Plan

## Direction

Create a modern spiritual remaster of Magic Carpet in Godot C#, focused first on the core feel rather than a complete recreation.

## Core Pillars

- fast free-flight over terrain
- readable first-person or close third-person spell combat
- mana economy built around collecting and contesting resources
- dynamic terrain and structures as a tactical layer
- monster/wizard encounters that create emergent pressure
- modern controls, HUD, and accessibility without losing the original fantasy

## Recommended Technical Approach

Use Godot 4.x C# as in `rustgrindgalaxy` and `kbtv`.

Start with a 3D prototype rather than attempting direct data conversion:

- `scenes/game/Game.tscn` as the root gameplay scene
- `scenes/player/PlayerCarpet.tscn` with a C# flight controller
- `scenes/world/Arena.tscn` with simple test terrain
- `scripts/player/CarpetFlightController.cs`
- `scripts/spells/SpellProjectile.cs`
- `scripts/world/ManaPickup.cs`
- `scripts/enemies/SimpleMonster.cs`
- `scripts/ui/Hud.cs`

## First Playable Slice

Build one small arena with:

- mouse-look or gamepad-look flight
- thrust/strafe/ascend/descend controls
- a height floor above terrain
- one fireball-like spell
- one mana pickup/resource counter
- one enemy that chases and damages the player
- one restart/death loop

This validates whether Godot's 3D feel is good enough before investing in terrain deformation, data extraction, or complex AI.

## Original Game Research Track

Use `D:\Games\MagicCarpet` as a reference install only.

Useful discovery tasks:

- run the game through DOSBox and capture short videos of flight, combat, HUD, mana collection, castle building, and terrain deformation
- map controls and camera behavior
- document spells, enemy types, mana balloon behavior, and level goals
- inspect file structure with read-only tools
- optionally create local-only extraction experiments under ignored folders, if legally acceptable

## Asset Track

Prefer original remastered assets over extracted assets:

- AI-generated or hand-authored spell icons, HUD frames, mana crystals, monster concepts, carpet models, skyboxes, terrain textures
- screenshots from the original can guide mood, palette, and shape language, but should not become source assets
- keep generated candidates under `_staging/` until curated

## Initialization Notes

The repo now contains a minimal Godot C# setup matching the prior projects:

- `project.godot`
- `MagicCarpetRemastered.csproj`
- `.gitignore`
- `README.md`
- `AGENTS.md`
- `docs/`

Remaining setup:

- install or locate Godot .NET and put it on `PATH`, or provide its executable path
- open/import `project.godot` once so Godot generates `.godot/` and C# metadata
- create the first `Game.tscn` and controller scripts

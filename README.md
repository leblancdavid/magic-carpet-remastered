# Magic Carpet Remastered

A Godot C# project shell for a modern remaster inspired by the classic DOS game Magic Carpet.

## Current Status

Discovery and initialization are in progress. The original DOS package is available locally at `D:\Games\MagicCarpet` for reference, but original proprietary files are not copied into this repo.

## Requirements

- Godot 4.x with .NET/C# support, matching the existing local projects where practical
- .NET SDK

Current discovery found .NET `9.0.307` available, but `godot` is not on `PATH`.

## Structure

- `docs/` - discovery, design, and implementation plans
- `scenes/` - Godot scenes
- `scripts/` - C# gameplay code
- `assets/` - original/remastered assets
- `tools/` - local research/import helpers

## First Milestone

Build a small first playable slice:

- fly a carpet over simple 3D terrain
- cast one projectile spell
- collect one mana pickup type
- fight one simple enemy
- show a minimal HUD
- restart the arena quickly

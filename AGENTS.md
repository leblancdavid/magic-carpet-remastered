# AGENTS.md - Magic Carpet Remastered Project Guidelines

## Project Context

- Engine target: Godot 4.x with C#
- Current state: accepted spherical prototype with economy, construction/siege/rebuilding, live management, equipment, booster, wrecks/boons, sites and matter logistics complete for their first pass. All four readiness phases are implemented and verified. Shared hover/paid Space jump and compact 100/200/300/400 m planets are implemented/verified. Terrain accessibility correction makes 94–98% of prepared land gentle, with three localized mountain regions; independent checks verify openings and normal-hover resource round trips. Next: runtime rival slope-aware routing/recovery, then approved Easy/Normal/Hard combat and grouped habitats. See `docs/TERRAIN_HOVER_PLAN.md` for live stage status and `docs/COMPACT_UI.md` for UI.
- Reference projects: `D:\Dev\Games\rustgrindgalaxy` and `D:\Dev\Games\kbtv`
- Local original game reference: `D:\Games\MagicCarpet`

## Product Direction

Build an original futuristic planetary faction-conflict game using the accepted spherical prototype: ship combat, terrain deformation, antimatter logistics, RTS-style home-base development and researched equipment. Magic Carpet is a historical mechanics reference, not the new setting. Follow `docs/BASE_DEVELOPMENT.md`, `docs/SHIP_EQUIPMENT.md` and `docs/EXPLORATION_BOONS.md` for the approved direction. Active code uses the migrated vocabulary; `docs/TERMINOLOGY_MIGRATION.md` records paths, identifiers and compatibility. Explicit tiers, storage-backed energy, five-slot lifecycle/refunds, health-layer siege/rebuilding and B-key management with starter repair/recharge/collector/strike/Claim projects are implemented. B management, M survey and H boon choices run in real time and own ship input. Equipment ranks I–III are researched remotely and installed explicitly into four slots at home; new matches start with Plasma Bolt only. Claim is built in. Random destructible wrecks replace legacy world signals; secured three-choice/skip offers and eight capped stacking boons survive death/tier loss and reset per match. Arrivals are bounded and exclude every base. Storage damage scales the top layer's incremental capacity over intact lower layers; completed higher-tier non-ship effects provisionally suspend below prerequisites, but owned/installed equipment survives tier loss and death.

Do not copy proprietary game code, data, art, audio, level files, or binary-derived assets into this repository unless the user explicitly confirms they own and can use them. Treat the DOS install as a local reference for observation, behavior study, and personal reverse-engineering experiments only.

Matter is an independent reserve with Claim-marked surface crates and mixed drone cargo; it never contributes to energy. Construction pays both resources atomically, snapshots both costs, and applies exact refunds/siege splits/half-price rebuilds independently. Eight economic sites include two matter extractors; income stops at applicable capacity and logistics capture steals both cargo kinds. See `docs/BASE_DEVELOPMENT.md` for provisional material defaults.

Spherical matches use one seeded `PlanetLayout` with antipodal starts and configurable 120° minimum separation, comparable opening fields/site routes and globe-wide content. Both factions start with 30/30 reserves. Neutrals use stable habitats, 25/65/85/120 aggression/leash ranges and committed return. Scavengers steal locally and retain/drop neutral reserves. One-at-a-time raids alternate living bases after 180s grace/interval, deal 4 HP per 3s and expire after 90s (`NEUTRAL_BEHAVIOR.md`). Live rivals use `ShipFlightController(IsPlayerControlled=false)` plus decision-only `RivalPilotController`: same flight, Claim/hauling, energy, paid research, home installation, combat, boons and persistent respawn. Human-only `player` group owns input/camera/UI; both ships use `pilots`. Legacy rival archetypes are diagnostic fixtures only. See `RIVAL_AI.md` for policy and limitations. Spherical strike lifetimes cover their actual route plus 15s while preserving speed/damage.

## Repository Expectations

UI uses original procedural line glyphs, compact icon tiles/tooltips and separate Ship Research/Loadout views. Five construction categories, full-refund instance-safe cancel and home-only installation remain live. Bottom equipment HUD is four numbered cards with selected/cooldown/energy states; mouse-down selects through `TrySelectEquipmentFromInterface` and latches global combat until release. B/M/H share a dock. Width-adaptive grids/scrolling and scoped/reused StyleBoxes are verified on 1280×720 and 960×540 canvases.

Target structure:

- `scenes/` for Godot scenes
- `scripts/` for C# gameplay code
- `assets/` for original/remastered game-ready assets
- `docs/` for design, discovery, and implementation notes
- `tools/` for extraction/research utilities that do not vendor original copyrighted data

## Coding Guidelines

- Prefer small, direct systems over abstract frameworks.
- Use `PascalCase` for C# types, methods, properties, scene names, and filenames.
- Keep one primary gameplay concept per script unless the file is very small.
- Prefer scene and script pairs where practical.
- Avoid premature architecture before the first playable flight/combat prototype exists.

## Godot Guidelines

- Keep Godot-generated cache files out of source control.
- Preserve pixel/retro presentation defaults where useful, but prioritize a modern 3D flight feel.
- Do not require proprietary DOS assets for the game to run.
- If adding imported assets, run Godot import before relying on `res://` runtime loads.

## Discovery Guidelines

- Separate three tracks: playable prototype, original-game research, and remastered asset creation.
- Record findings in `docs/` before encoding them as systems.
- For original game behavior, prefer notes, screenshots, measurements, and manually authored equivalents over direct binary/data conversion.

## Progress Tracking

- Before starting non-trivial work, read `docs/BUILD_ROADMAP.md` and `docs/PROGRESS_LOG.md` to confirm current status and remaining work.
- During work, keep changes aligned with the active roadmap phase unless the user explicitly redirects scope.
- After completing work, update `docs/PROGRESS_LOG.md` with what changed, what was verified, new known gaps, and next recommended work.
- After completing work that changes scope, phase status, or priorities, update `docs/BUILD_ROADMAP.md` as well.
- When implementation, playtesting, research, or debugging reveals a reusable lesson, update or create the appropriate document in `docs/` so the understanding is preserved. Use `docs/LEARNINGS.md` for durable project lessons, `docs/DISCOVERY.md` for original-game observations, and the roadmap/progress docs for status and priorities.
- Do not leave roadmap/progress docs stale after gameplay, architecture, tooling, or discovery changes.

# AGENTS.md - Magic Carpet Remastered Project Guidelines

## Project Context

- Engine target: Godot 4.x with C#
- Current state: accepted spherical prototype with an antimatter-only economy, construction/siege/rebuilding, live management, equipment, booster, wrecks/boons and eight sites. Matter is removed. Live radii are 200/300/400/500 m (Medium default). Session seeds replay with R; F2 blank starts fresh, entered seed replays. Independent streams seed terrain/layout/content; bounded validation enforces 3–5% steep land above 40°, ≥85% gentle below 20° and protected openings. Dense random habitats target 64/120/200/300 mobs with 20-second/five-second recovery; owned-site guards do not replenish. Bottom gauges/weapons/radar and four-tab management replace corner panels/five-tab UI. First-pass prices/income/siege target Medium tier V in 10–15 minutes, pending human acceptance. Next: hands-on density/economy/UI/XL performance, runtime rival routing/recovery/resilience, then difficulty. See `docs/PLAYTEST_SIMPLIFICATION.md` and progress log.
- Reference projects: `D:\Dev\Games\rustgrindgalaxy` and `D:\Dev\Games\kbtv`
- Local original game reference: `D:\Games\MagicCarpet`

## Product Direction

Latest playtest follow-up (2026-10-13) supersedes the older population/HUD tuning above: **96/180/300/450** mobs with irregular clusters and seeded destination/pause wandering. Shared Plasma is **2 E / 13–18–23 damage**; both ships recover **2 HP/s after five seconds without dealing/taking combat damage**. Human health hits briefly shake/darken the view. `FlightHudLayout` centers the entire HUD including radar/base HP, with compact H/E/A current-only gauges. H uses select/Reject/Confirm-and-close, with no second/empty summary UI; F3 retains collected bonuses. Routine toasts are removed. Next remains hands-on combat/density/economy/UI/XL performance, then rival routing/recovery and difficulty.

Build an original futuristic planetary faction-conflict game using the accepted spherical prototype: ship combat, terrain deformation, antimatter logistics, RTS-style home-base development and researched equipment. Magic Carpet is a historical mechanics reference, not the new setting. Follow `docs/BASE_DEVELOPMENT.md`, `docs/SHIP_EQUIPMENT.md` and `docs/EXPLORATION_BOONS.md` for the approved direction. Active code uses the migrated vocabulary; `docs/TERMINOLOGY_MIGRATION.md` records paths, identifiers and compatibility. Explicit tiers, storage-backed energy, five-slot lifecycle/refunds, health-layer siege/rebuilding and B-key management with starter repair/recharge/collector/strike/Claim projects are implemented. B management, M survey and H boon choices run in real time and own ship input. Equipment ranks I–III are researched remotely and installed explicitly into four slots at home; new matches start with Plasma Bolt only. Claim is built in. Random destructible wrecks replace legacy world signals; secured three-choice/skip offers and eight capped stacking boons survive death/tier loss and reset per match. Arrivals are bounded and exclude every base. Storage damage scales the top layer's incremental capacity over intact lower layers; completed higher-tier non-ship effects provisionally suspend below prerequisites, but owned/installed equipment survives tier loss and death.

Do not copy proprietary game code, data, art, audio, level files, or binary-derived assets into this repository unless the user explicitly confirms they own and can use them. Treat the DOS install as a local reference for observation, behavior study, and personal reverse-engineering experiments only.

Antimatter is the sole physical construction resource; current combat energy is a separate rechargeable quantity backed by base storage. Capacities are 200/350/550/800/1100; tier II–V prices 150/275/450/650 A. Health layers are 400 HP; Plasma deals 20% damage to bases, retaining normal mob/ship damage. Eight sites include four 0.2-A/s extractors, two research and two logistics. Exact paid snapshots/refunds/siege splits/half-price rebuilding remain. Do not reintroduce matter or dual-resource logistics from historical foundation notes.

Spherical matches use one seeded layout with antipodal starts, configurable 120° minimum separation and comparable opening/site routes. Both factions start with 30 antimatter. Region-spread random wild habitats include easy swarms, packs and larger 150-HP heavy beasts; site-linked guards stop replacing members under either owner and restart recovery when neutral. Aggro/retaliation/leash scale by size; committed return remains. Scavengers retain/drop stolen reserves. Raids alternate bases after 180s grace/interval, deal 4 HP per 3s and expire after 90s. Rivals use shared ships plus decision-only `RivalPilotController`, ordinary economy/research/installation/boons/respawn. Human-only `player` owns input/camera/UI; modern ships use `pilots`, legacy diagnostic pilots also use `legacy_rival_pilot` to avoid quadratic neutral acquisition scans. Strike drones deal 120 damage for 100 A/20s; globe lifetime covers the route plus 15s. See `NEUTRAL_BEHAVIOR.md` / `RIVAL_AI.md`.

## Repository Expectations

UI uses original procedural glyphs/tiles/tooltips. B header shows antimatter/tier and upgrade/rebuild above Offense/Defense/Resources/Ship; five backend slots remain concurrent. Progress/cancel overlay their buttons; unavailable projects fade and show Roman required tiers. Owned equipment tiles toggle at home (four-item cap, row-order keys, preserved selection); pending ranks use Apply. Bottom HUD has three vertical HP/energy/storage gauges left of four weapons, bottom-right radar and base HP beneath, controls/B/M/H below. Capture/important alerts are contextual; F3 is on demand. Card mouse-down selects through `TrySelectEquipmentFromInterface` and latches firing until release. Maps retain 220 m sensing, global home/owned sites and remembered rival-base locations; neutral mobs/antimatter are black/orange, claimed resources faction-colored. Responsive scrolling/layout is verified on 1280×720 and 960×540.

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

# AGENTS.md - Magic Carpet Remastered Project Guidelines

## Project Context

- Engine target: Godot 4.x with C#
- Current state: accepted spherical prototype, antimatter-only construction/siege/rebuilding, live tier-row management, planned/home-auto-applied equipment, booster, wrecks/boons, eight sites and paid scout/attack/defensive/strike deployment. Matter is removed. Radii 200/300/400/500m, Medium default. R retries the seed; F2 blank starts fresh/entered seed replays. Independent streams validate 3–5% steep land above 40°, ≥85% gentle below 20° and protected openings. Dense habitats target 96/180/300/450 with 35s/8s recovery; owned guards do not replenish. The approved 65-type catalog has 22 playable types; remaining types cannot take payment. Next: remaining tier-II content then III–V, hands-on economy/UI/orders and XL physics, then broader rival routing/difficulty. Medium tier-V 10–15-minute pacing remains provisional. See the upgrade plan and progress log.
- Reference projects: `D:\Dev\Games\rustgrindgalaxy` and `D:\Dev\Games\kbtv`
- Local original game reference: `D:\Games\MagicCarpet`

## Product Direction

Latest upgrade-tree pass (2026-10-13): `docs/UPGRADE_TREE_PLAN.md` records the 65-type tree and **22 functional types** (all tier I, eight tier II, Guardian IV), shared metal-star tier grids, remote planned/home-auto-applied loadouts/HUD indicators, paid target-dropdown orders and scout/attack/defensive/interceptable strike units. Pending types say Coming later and are absent from payable definitions. Scouts target nearest non-faction-owned points, half-rate/nonstacking capture; defenders retreat and protect home after site loss. Stars unlock at their type's tier; non-ship effects suspend below it, ship ownership/passives persist. First gameplay improvements remain live: internal 30-A cargo, 1-E penetrating Claim, gradual banking, 35s/8s neutral recovery, broader roaming/base contact attacks, basic defenses and tier fleets. **Next:** remaining tier-II Hunter/Rocket/Flak/Scatter/Seeker, then III–V. This scope precedes routing/difficulty; human acceptance and XL native-physics performance remain pending.

Latest flight/salvage follow-up (2026-10-13): shared moderate lateral drift, 24° turn/slip ship bank with ~10° camera roll, original triangular hovercraft hulls, and 65° uphill traversal with progressive slowdown. Rival bounded local probes/paid-jump recovery pass a real crater escape. Live mob rewards are **6/36/72/20 A**, with more 90-ms staggered/outward-scattered balls; scavengers carry 20 A plus stolen reserves. Seeded startup warms actual actor terrain; distant actors use smaller/coarser support requests. Generation retains the **3–5% above 40°** rough-land classification. See `docs/PLAYTEST_FLIGHT_DROPS.md`. Next: hands-on flight/salvage/economy/startup, remaining XL native-physics/first-use spikes, then broader rival routing and difficulty.

Latest playtest follow-up (2026-10-13) supersedes the older population/HUD tuning above: **96/180/300/450** mobs with irregular clusters and seeded destination/pause wandering. Shared Plasma is **2 E / 13–18–23 damage**; both ships recover **2 HP/s after five seconds without dealing/taking combat damage**. Human health hits briefly shake/darken the view. `FlightHudLayout` centers the entire HUD including radar/base HP, with compact H/E/A current-only gauges. H uses select/Reject/Confirm-and-close, with no second/empty summary UI; F3 retains collected bonuses. Routine toasts are removed. Next remains hands-on combat/density/economy/UI/XL performance, then rival routing/recovery and difficulty.

Build an original futuristic planetary faction-conflict game using the accepted spherical prototype: ship combat, terrain deformation, antimatter logistics, RTS-style home development and researched equipment. Magic Carpet is a mechanics reference, not the setting. Follow the current base/equipment/boon docs and upgrade plan. Active vocabulary migration is recorded in `TERMINOLOGY_MIGRATION.md`. Explicit tiers, storage-backed energy, five-slot exact refunds/siege/rebuilding and live B/M/H input ownership remain. Equipment stars are researched/planned remotely and automatically installed into four slots at home; start with Plasma bronze only, Claim built in. Wrecks secure three-choice/Reject boons, survive death/tier loss and reset per match. Arrivals exclude bases. Damaged top-layer storage scales incrementally; non-ship effects suspend below prerequisites, while ship ownership/passives persist.

Do not copy proprietary game code, data, art, audio, level files, or binary-derived assets into this repository unless the user explicitly confirms they own and can use them. Treat the DOS install as a local reference for observation, behavior study, and personal reverse-engineering experiments only.

Antimatter is the sole physical construction resource; current combat energy is a separate rechargeable quantity backed by base storage. Capacities are 200/350/550/800/1100; tier II–V prices 150/275/450/650 A. Health layers are 400 HP; Plasma deals 20% damage to bases, retaining normal mob/ship damage. Eight sites include four 0.2-A/s extractors, two research and two logistics. Exact paid snapshots/refunds/siege splits/half-price rebuilding remain. Do not reintroduce matter or dual-resource logistics from historical foundation notes.

Spherical matches use one seeded layout with antipodal starts, configurable 120° minimum separation and comparable opening/site routes. Both factions start with 30 antimatter. Region-spread random wild habitats include easy swarms, packs and larger 150-HP heavy beasts; site-linked guards stop replacing members under either owner and restart recovery when neutral. Aggro/retaliation/leash scale by size; committed return remains. Scavengers retain/drop stolen reserves. Raids alternate bases after 180s grace/interval, deal 4 HP per 3s and expire after 90s. Rivals use shared ships plus decision-only `RivalPilotController`, ordinary economy/research/installation/boons/respawn. Human-only `player` owns input/camera/UI; modern ships use `pilots`, legacy diagnostic pilots also use `legacy_rival_pilot` to avoid quadratic neutral acquisition scans. Strike drones deal 120 damage for 100 A/20s; globe lifetime covers the route plus 15s. See `NEUTRAL_BEHAVIOR.md` / `RIVAL_AI.md`.

## Repository Expectations

UI uses original procedural glyphs/tiles/tooltips. B header shows AM/tier/upgrade-rebuild above four tier-row tabs; each tile buys its next metal star and overlays progress/instance-safe cancel. Base columns three, Ship four; unavailable content fades and cannot take payment. Equip/Unequip underneath selects a remote four-item plan, passives say Passive; home auto-installs with preserved selection and pending HUD glyphs. Separate paid deployment dropdowns own input over management. Five backend slots remain concurrent. Centered bottom H/E/A gauges, four weapons, radar/base HP and B/M/H remain; F3 is on demand. Card mouse-down selects through `TrySelectEquipmentFromInterface` and latches firing until release. Live map data uses 220m ship sensing, owned sites/home global, remembered rival/scout-charted point locations without distant live details. Layout is verified at 1280×720 and 960×540.

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

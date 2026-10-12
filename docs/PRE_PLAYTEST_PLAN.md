# Pre-Playtest Readiness Plan

**2026-10-12 follow-up:** readiness phases below are completed history. Active user-approved redesign is `TERRAIN_HOVER_PLAN.md`: compact planets, dramatic route-shaping terrain, shared hover/paid jump physics, rival difficulty and grouped encounters. Implement and verify movement first.

## Approved scope (2026-10-11)

Before meaningful economy/balance playtesting, fix clustered world content, incessant neutral pressure, the legacy rival's unequal mechanics, and cluttered interaction UI. Work in independently verified phases; this milestone takes priority over balance/campaign expansion. Implementation status belongs in `PROGRESS_LOG.md` and `BUILD_ROADMAP.md`.

## Phase 1 — Coordinated planet layout

**Implemented and regression-verified.** See `STRATEGIC_NAVIGATION.md` for defaults and `PROGRESS_LOG.md` for checks. All four readiness phases are complete; hands-on balance is next.

- Generate one seeded layout before actors/content. Use spherical directions and surface distances, not small projected X/Z coordinates.
- Two faction starts aim for opposite sides, with a configurable minimum separation of **120 degrees** (surface distance = radius × 2π/3). Validate every pair; never silently accept an invalid nearby start. Prefer suitable terrain with bounded candidate selection.
- Spawn ships and home collectors relative to their actual base. Both factions start with the same reserves.
- Spread five named landmarks/habitats, scenery clusters, eight economic stations and remote resources across planetary regions, including both hemispheres. Nearby opening antimatter/matter and role-matched capture opportunities must have comparable surface distances for both starts.
- Retain manageable actor counts and resource totals. Regional scenery should provide destinations, not blanket the globe with collision bodies.
- Improve wreck coverage using planetary regions as well as nearest-base fairness, preserving bounded placement, all-base exclusion, spacing and arrival/population rules.
- Verify all four sizes, multiple seeds, minimum separation, coverage, terrain/water orientation, opening fairness, resource totals, map navigation and fresh-match startup. Inspect rendered survey/gameplay.

## Phase 2 — Territorial neutrals and occasional raids

**Implemented and regression-verified.** See `NEUTRAL_BEHAVIOR.md` for actual tuning/policies and `PROGRESS_LOG.md` for results. All four readiness phases are complete; hands-on balance is next.

- Explicit patrol/idle, aggro, pursuit and return-home states. Ordinary neutrals inhabit stable regions, rather than spawning around and streaming toward a travelling player.
- Provisional globe ranges: **25 proximity / 65 retaliation / 85 disengagement / 120 habitat leash** surface units; export tuning and smaller flat-reference defaults. Both faction pilots provoke the same behaviour.
- Hits identify the attacker, including projectiles, area attacks and deployables. Nearby attacks trigger retaliation within its larger range; farther hits cause local alert/search. Pursuit ends on escape, invalid/dead target or habitat leash; return does not immediately reacquire distant targets.
- Ordinary mobs do not choose bases as global objectives. Spawn exclusions apply to every base, with bounded attempts and no unsafe fallback.
- Keep occasional clearly identified siege raiders separately paced: opening grace, long raid interval, low active cap, either faction as target. Avoid uninterrupted base harassment.
- Verify real movement/attack gates, both factions, retaliation boundaries, disengagement/leash/return, spawn exclusions, independent raid pacing and radial geometry.

## Phase 3 — Shared-mechanics economy-first rival

**Implemented and regression-verified.** See `RIVAL_AI.md` for policy/defaults and AI limitations. Compact UI is also complete; hands-on balance is next.

- Make ship simulation/actions faction-aware and shared. Human input and a small AI controller drive the same flight, health, energy/recharge, equipment, Claim, booster and cooldown rules.
- Rival starts with Plasma Bolt I and equal reserves. It claims both resources for collectors; no direct absorption, free weapons, free research, remote installation or injected income.
- Minimal decision sequence: gather/Claim, capture useful nearby sites, research, return home/install, explore wrecks, occasionally attack, retreat/recover. Retain an energy reserve when ordering projects. Use commitment/retry timers to avoid thrashing/stalls.
- Share five-slot construction, prerequisites, tier loss/rebuilding, four-slot home installation, boons and progression retention across respawn. Use the same capture/contest and wreck-choice mechanics with a simple legal AI choice policy.
- Update faction targeting/friendly fire, guardians, navigation, accounting, objectives and respawn. F3 exposes the rival's current task.
- Verify actual Claim/mixed hauling, costs/cooldowns, paid research/installation, capture, rewards, combat/retreat and death retention. Confirm human input/camera/UI remain correctly owned.

## Phase 4 — Compact and readable UI

**Implemented and regression-verified.** See `COMPACT_UI.md` for presentation/controls and `PROGRESS_LOG.md` for verification. Next is the meaningful hands-on balance pass.

- Bottom-centred **4×1 equipment bar**, labelled 1–4, icons/ranks, strong selected highlight, empty slots, cooldown overlays and unaffordable state. 1–4 selects; LMB activates; Tab cycles. Claim/Shift indicators remain outside four slots.
- Compact health/energy/resources and consolidated B/M/H entry hints. Detailed explanations move to F3/tooltips; world interaction prompts appear contextually.
- Replace verbose research rows with distinct icon tiles, short name/rank, compact costs and visible availability/progress/completion states. Tooltips explain effects, time, prerequisites and exact disabled reasons.
- Visually separate research from owned inventory/home installation. Keep five concurrent categories and make current-project progress/cancellation compact and clear.
- Consistent actionable/hover/selected/disabled styling. Ensure tooltips work for unavailable tiles and clicks cannot leak into ship combat.
- Verify rendered layouts, normal/small viewport readability, tooltip content, viewport-routed actions, equipment selection and real-time panel ownership.

## Completion and next work

Each phase requires clean build/import, relevant gameplay regressions and documentation updates. New ranges, layouts, raid rates and AI priorities are provisional until hands-on play. After all four phases, run a 15–20-minute Medium match to tune economy, travel, upgrades, raids, wrecks and mobility; then deepen faction strategy/campaign/content.

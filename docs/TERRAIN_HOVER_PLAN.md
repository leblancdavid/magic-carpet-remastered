# Compact Planets, Terrain And Hover Movement

## Approved direction — 2026-10-12

Hands-on feedback supersedes the former large-world travel baseline. Keep genuine spherical curvature first; evaluate it in motion before considering rendering distortion. Use **Small 100 / Medium 200 / Large 300 / XL 400 m radii**, Medium default. Hover freely over oceans. Ship movement becomes ground-hugging suspension with momentum, radial gravity and paid jumping, rather than manual altitude control. Dramatic terrain must influence routes. Rival combat gets **Easy / Normal / Hard**, Normal default. Increase meaningful encounter density using habitats and role-specific groups.

**Approved accessibility correction, 2026-10-12:** most land must be easy to traverse, with mountains/roughness restricted to occasional regions. Targets: at least **85% of land below 20°**, at most **5% above the 40° hover limit**, and connected practical opening routes through ordinary hovering. Oceans are excluded from those land-percentage denominators. Fix terrain/approaches before implementing rival pathfinding.

## Execution order and acceptance

1. **Shared hover/jump movement — implemented and regression-verified 2026-10-12.** Human/rival use the same suspension, limited climbing, airborne gravity and collision. WASD steers; mouse pitch aims; Space requests a fresh jump. Provisional **2 current energy / about 4 m level-ground jump**, no storage payment, no double jump, short input buffering/edge grace, no fall damage. Natural crest/drop launches cost nothing. Booster preserves vertical momentum; Dash lift integrates with gravity. Airborne ships have no ground-relative ceiling clamp and steep faces cannot teleport them upward. Verified ordinary slopes, walls, crests, drops, water, radial underside/seams, landing, pause/UI/death/respawn and exact jump payments; see progress log for counts. Hands-on feel remains pending.
2. **Compact planets/terrain — implemented; accessibility correction verified 2026-10-12.** Live presets remain 100/200/300/400 m. Gentle continental slopes/coastlines/lowlands are the default; three separated land-biased mountain regions replace the globe-spanning belts. Destination heights, broader/adaptive ramps and lowland-preferring starts prevent raised platforms. Bases retain level dry footprints, wet pads remain below the ocean mesh, and deformation remains live. All-size slope budgets, layout/content checks and rendered Small/Medium inspection pass. Hands-on feel remains iterative.
3. **Traversable layouts and routing — opening validation implemented; runtime rival routing pending.** Independent terrain-only checks establish 192 practical routes from both starts to opening antimatter/matter and role-matched stations, across four sizes/four seeds. Actual shared ships complete both-faction resource out-and-back trips on every size without paid mobility. Next: runtime slope-aware rival routes around the remaining barriers, deformation recovery, live-obstacle handling and collector/strike review. Preserve bounded placement and no unsafe fallback.
4. **Rival difficulty — pending.** F2 setup selection; reaction delay, capped tracking speed, smoothly persistent aim error and burst/rest timing. Shared HP/damage/payments/research remain unchanged. Keep navigation/Claim independent of combat inaccuracy. Interrupt combat tracking/fire on lost visibility and verify difficulty ordering, acquisition, legal firing and unchanged accounting.
5. **Encounter density — pending.** More stable habitats along useful routes. Swarms 3–5, beasts singles/pairs with occasional packs, casters mostly solitary/occasionally paired, scavengers singles/pairs. Individual territorial aggression initially; no automatic whole-pack aggro. Size-aware population/role caps and gradual bounded replenishment. Target useful encounters/destinations roughly every 10–20 seconds along routes, with quiet stretches. Recheck extra antimatter supply and faster hauling/progression.
6. **Integrated verification/playtest — pending.** Clean build/import, relevant gameplay regressions and rendered checks. Record actual results in progress log. Hands-on curvature, hover feel, route readability, duels and economy acceptance remain iterative; provisional values are not final balance.

## Implementation notes

- Stage 1 replaced ship live ground/ceiling clamps and one-sided lift; other actors retain their altitude policy.
- The former 180 m exclusion from both opposite bases is impossible on radius 100 (maximum nearest-base distance is πR/2 ≈157 m). Stage 2 introduced coordinated size-aware exclusions, spacing and capture/territory settings.
- Stage 2 removed pole-centred smoothing unrelated to faction starts. Local landing pads are prepared before actors and invalidate visible fine and dormant coarse geometry, so later LOD swaps cannot resurrect the unprepared landscape.
- Commit stages only after their acceptance checks; preserve existing uncommitted project work.

## Stage 1 verification

`tools/HoverMovementChecks.tscn` passes **35 headless / 35 rendered** checks with real solver frames and controlled terrain fixtures. It covers jump payments/rejection/held input/buffering/edge grace, suspension, climb barriers, ramp momentum, drops, oceans, booster/Dash integration, death/respawn and radius-100 underside/seam jumps. Existing booster, playtest mechanics, rival faction, equipment, spherical gameplay and compact UI checks pass. UI captures show the Space cost in the built-in equipment indicators. The old flight lab remains diagnostic, not the gameplay movement acceptance test.

## Current terrain tuning and verification

- Live radius presets: **100/200/300/400 m**, Medium default. Approximate uninterrupted opposite-base cruise: **13/26/39/52 s**; this is geometry, not actual route timing.
- Three non-overlapping, land-biased mountain caps extend **0.32×radius** around their centres, approximately **7.7% of the whole sphere** before destination shaping (roughly 10–15% of land). Each has gentle foothills, a localized ridge and a low notch. Main mountain amplitude is **16–32 m**; ordinary hills use much smaller, lower-frequency variation. Raw peaks are approximately **19/26/37/39 m**, raw ocean coverage **44–50%**. Generation remains a deterministic fixed-noise baseline; layout placement varies with `LayoutSeed`.
- Base pads: **20 m inner / at least 45 m outer**, elevation constrained to **3.5–5 m above sea**. Station/resource/landmark pads cap dry elevation at 5 m above sea; submerged centres use sea minus 0.5 m. Default outer radii are **24 m stations / 32 m antimatter / 28 m matter / 24 m landmarks**, expanding when surrounding height differences require longer ramps. Height snapshots use the unprepared profile, and bases apply last to protect their level footprint. Approaches blend against the water support level rather than deep seabed. Combat edits still apply afterward.
- Wreck all-base exclusions **30/50/75/80 m**, spacing **20/40/60/60 m**. Twelve initial/twenty active remain. Ordinary-mob base exclusions **30/60/80/80 m**; raid launch distances **36/70/100/100 m** plus bounded search ring. Capture radii **8/12/18/18 m**. Explicit exported overrides survive default substitution.
- Neutral proximity **16/16/24/25**, retaliation **30/30/45/55**, disengagement **40/40/60/70**, habitat leash **60/60/90/100**, patrol **6/8/12/12 m**. Population/group rules remain the previous baseline until stage 5.
- `CompactTerrainChecks` passes **16,482 headless / 16,484 rendered**, adding raw/prepared land slope budgets and retained mountain peaks to finite heights, oceans, seams, pad physics and deformation. `PlanetLayoutChecks` passes **1,939 headless / 1,853 rendered**, including four seeds and rendered Small/Medium camera modes/survey. `TerrainAccessChecks` passes **3,288 headless**, with **192 connected openings / four localized detours** and actual both-faction normal-hover out-and-back resource travel on every size. Every seed uses a fresh arena and checks its prepared land budget. This is an independent terrain verifier, not the runtime rival navigator; scenery/combat/deformation routing remains next.

Measured prepared land (4,096 uniform-area samples per size, default layout seed):

| Size | Land below 20° | Land above 40° | Remaining sampled peak |
| --- | ---: | ---: | ---: |
| Small | 97.0% | 0.8% | 16.2 m |
| Medium | 94.1% | 1.0% | 26.3 m |
| Large | 95.4% | 0.9% | 37.4 m |
| XL | 97.9% | 0.6% | 39.3 m |

**Next implementation stage:** runtime rival navigation around the remaining localized barriers, including deformation recovery, live-obstacle handling and collector/strike policy review. Terrain opening connectivity now has independent verification for the tested sizes/seeds. Radar/reveal ranges still use the prior baseline; revisit compact-map readability during routing. Difficulty and grouped populations remain pending. Terrain/curvature/performance acceptance needs hands-on sessions, not only these checks.

# Compact Planets, Terrain And Hover Movement

## Approved direction — 2026-10-12

Hands-on feedback supersedes the former large-world travel baseline. Keep genuine spherical curvature first; evaluate it in motion before considering rendering distortion. Follow-up playtesting rejected radius 100: use **Small 200 / Medium 300 / Large 400 / XL 500 m radii**, Medium default. Hover freely over oceans. Ship movement uses ground-hugging suspension with momentum, radial gravity and paid jumping. Dramatic terrain must influence routes. Rival combat gets **Easy / Normal / Hard**, Normal default. Increase meaningful encounter density using habitats and role-specific groups.

**Approved accessibility correction, 2026-10-12:** most land must be easy to traverse, with mountains/roughness restricted to occasional regions. Targets: at least **85% of land below 20°**, at most **5% above the 40° hover limit**, and connected practical opening routes through ordinary hovering. Oceans are excluded from those land-percentage denominators. Fix terrain/approaches before implementing rival pathfinding.

**Approved steepness follow-up:** increase prepared land above the **40°** climbing limit to **3–5%**, retaining the 85% gentle-land minimum and protected opening access. Initial generation targets approximately 4%; combat deformation remains free to change the distribution afterward.

## Execution order and acceptance

**Random-world follow-up implemented:** initial application startup/F2 fresh-world creation use a new session seed; **R restarts the current seed and size**. F2 displays the seed and accepts an optional replay seed (blank = random); F3 reports root/accepted terrain seed and attempts. Independent terrain/layout/content streams make actual geography and initial placement reproducible. Generation validates up to sixteen candidates before allocating terrain meshes/actors; failed candidates do not silently bypass access/slope requirements. Tests now vary actual terrain seeds rather than only layout rotation over fixed geography.

1. **Shared hover/jump movement — implemented and regression-verified 2026-10-12.** Human/rival use the same suspension, limited climbing, airborne gravity and collision. WASD steers; mouse pitch aims; Space requests a fresh jump. Provisional **2 current energy / about 4 m level-ground jump**, no storage payment, no double jump, short input buffering/edge grace, no fall damage. Natural crest/drop launches cost nothing. Booster preserves vertical momentum; Dash lift integrates with gravity. Airborne ships have no ground-relative ceiling clamp and steep faces cannot teleport them upward. Verified ordinary slopes, walls, crests, drops, water, radial underside/seams, landing, pause/UI/death/respawn and exact jump payments; see progress log for counts. Hands-on feel remains pending.
2. **Compact planets/terrain — implemented; accessibility correction verified 2026-10-12.** Live presets now use 200/300/400/500 m. Gentle continental slopes/coastlines/lowlands are the default; three separated land-biased mountain regions replace the globe-spanning belts. Destination heights, broader/adaptive ramps and lowland-preferring starts prevent raised platforms. Bases retain level dry footprints, wet pads remain below the ocean mesh, and deformation remains live. All-size slope budgets, layout/content checks and rendered Small/Medium inspection pass. Hands-on feel remains iterative.
3. **Traversable layouts and routing — opening validation implemented; runtime rival routing pending.** After matter removal, checks establish **160** practical antimatter/station opening routes across four sizes/four seeds, with real both-faction ordinary-hover resource round trips on every size. Earlier 192-route results include removed material fields. Runtime slope-aware routing/deformation/live-obstacle recovery remains next.
4. **Rival difficulty — pending.** F2 setup selection; reaction delay, capped tracking speed, smoothly persistent aim error and burst/rest timing. Shared HP/damage/payments/research remain unchanged. Keep navigation/Claim independent of combat inaccuracy. Interrupt combat tracking/fire on lost visibility and verify difficulty ordering, acquisition, legal firing and unchanged accounting.
5. **Encounter density — user-directed pass implemented ahead of routing/difficulty.** Current targets **64/120/200/300** combine random region-spread wild habitats, full easy swarms, larger heavy beasts and ownership-aware site guards. Recovery remains 20 seconds/five-second replacements, suppressed at owned points. Proximity/pack retaliation remains; differentiated rewards/prices/slower extractor income are tuned together. See `PLAYTEST_SIMPLIFICATION.md` and `NEUTRAL_BEHAVIOR.md` for current values; previous density notes are historical.
6. **Integrated verification/playtest — pending.** Clean build/import, relevant gameplay regressions and rendered checks. Record actual results in progress log. Hands-on curvature, hover feel, route readability, duels and economy acceptance remain iterative; provisional values are not final balance.

## Implementation notes

- Stage 1 replaced ship live ground/ceiling clamps and one-sided lift; other actors retain their altitude policy.
- The former 180 m exclusion from both opposite bases is impossible on radius 100 (maximum nearest-base distance is πR/2 ≈157 m). Stage 2 introduced coordinated size-aware exclusions, spacing and capture/territory settings.
- Stage 2 removed pole-centred smoothing unrelated to faction starts. Local landing pads are prepared before actors and invalidate visible fine and dormant coarse geometry, so later LOD swaps cannot resurrect the unprepared landscape.
- Commit stages only after their acceptance checks; preserve existing uncommitted project work.

## Stage 1 verification

`tools/HoverMovementChecks.tscn` passes **35 headless / 35 rendered** checks with real solver frames and controlled terrain fixtures. It covers jump payments/rejection/held input/buffering/edge grace, suspension, climb barriers, ramp momentum, drops, oceans, booster/Dash integration, death/respawn and radius-100 underside/seam jumps. Existing booster, playtest mechanics, rival faction, equipment, spherical gameplay and compact UI checks pass. UI captures show the Space cost in the built-in equipment indicators. The old flight lab remains diagnostic, not the gameplay movement acceptance test.

## Current terrain tuning and verification

- Live radius presets: **200/300/400/500 m**, Medium default. Approximate uninterrupted opposite-base cruise: **26/39/52/65 s**; this is geometry, not actual route timing.
- Three separated land-biased mountain regions extend **0.38×radius** (0.44×radius on Small). Main relief remains **10–40 m**; folds fade around protected destinations and the **ten** retained home-to-antimatter/station approaches. Matter pads/approaches are removed. Ordinary hills/coastlines remain gentle. Historical measurements below precede this removal; current seed/access verification is in the progress log.
- Startup-only fold tuning measures exposed land at **2,048** uniform-area points, searches amplitude **0–3 m** for at most eight attempts, and targets ~4% above 40°. This accounts for different layouts' protective pads without spreading roughness globally. Independent verification uses **4,096** samples, including a rotated pattern across four fresh seeds per size. No continuous in-match terrain rebalancing occurs.
- Base pads remain **20 m inner / at least 45 m outer**, elevation **3.5–5 m above sea**. Station/resource/landmark pads cap dry elevation at 5 m; submerged centres use sea minus 0.5 m. Outer radii are **24 m stations / 32 m antimatter / 24 m landmarks**, expanding around larger height differences. Bases apply last; approaches blend against water support rather than deep seabed. Combat edits still apply afterward.
- Wreck all-base exclusions **50/75/80/80 m**, spacing **40/60/60/60 m**. Twelve initial/twenty active remain. Ordinary-mob base exclusions **60/80/80/80 m**; raid launch distances **70/100/100/100 m** plus bounded search ring. Capture radii **12/18/18/18 m**. Explicit exported overrides survive default substitution.
- Neutral proximity **30/35/40/45**, retaliation **65/65/80/100**, disengagement **85/85/100/125**, habitat leash **100/100/120/150**, patrol **8/12/12/12 m**. Population/group rules and bounded pack retaliation are implemented; see `NEUTRAL_BEHAVIOR.md`.
- Current steepness follow-up verification: `CompactTerrainChecks` **16,483 headless / 16,485 rendered**; `PlanetLayoutChecks` **1,942 headless**; `TerrainAccessChecks` **3,287 headless**, with **192 connected openings / nine localized detours** and actual both-faction normal-hover out-and-back resource travel on every size. Across sixteen fresh worlds, independently sampled steep coverage is **3.3–4.3%**, with at least **89.3%** gentle land. Submerged coastal transition checks require genuine mesh separation rather than assuming every blended seabed stays exactly 0.5 m below sea. This is an independent terrain verifier, not the runtime rival navigator; scenery/combat/deformation routing remains next.

Historical fixed-noise prepared land (4,096 samples, layout seed 7319 / terrain seed 27031; retained diagnostic fixtures, not the default random world):

| Size | Land below 20° | Land above 40° | Remaining sampled peak |
| --- | ---: | ---: | ---: |
| Small | 91.7% | 3.8% | 14.5 m |
| Medium | 90.9% | 3.8% | 25.5 m |
| Large | 91.8% | 3.6% | 36.8 m |
| XL | 93.4% | 4.1% | 46.8 m |

**Next implementation stage:** runtime rival navigation around the remaining localized barriers, including deformation recovery, live-obstacle handling and collector/strike policy review. Terrain opening connectivity now has independent verification for the tested sizes/seeds. Radar/survey now share 220 m ship-only sensing, owned-site visibility and discovered-base memory. Difficulty remains pending; grouped populations, pack retaliation and integrated equip/research rows are implemented. Terrain/curvature/performance and economy acceptance need hands-on sessions, not only these checks.

## Random generation / replay contract

- World seeds are signed 32-bit integers, including zero and negatives, retained for the application session. The same seed, size and generator version reproduce geography/layout/initial wreck and neutral placement. Changing seed changes real continental/detail noise and mountain selection. Later simulation/boon choices remain ordinary gameplay randomness.
- Terrain and layout streams include a deterministic candidate index. Failed slope/land/opening candidates are retried up to **16**; the accepted candidate is reconstructible without saving hidden RNG state. Generation failure presents setup for choosing another seed, with no unsafe match actors.
- Acceptance samples 4,096 terrain points for finite values, **25–85% land**, **≥85% of land below 20° / 3–5% above 40°**, plus one-metre direct opening-link checks under 30°. Oceans do not count toward gentle-land percentages. This conservative opening policy may reject a usable detour world; runtime AI pathfinding remains a separate next stage.
- New seed tests cover four world seeds on every radius, fixed-seed replays and actual R/F2 scene transitions. Independent 3,072-point sampling gives **88.8–94.7% gentle / 3.3–4.8% steep** across the sixteen-world bank. Random-world route verification passes 192 openings and actual both-faction resource out-and-back travel (`TerrainAccessChecks --random-worlds`). Fixed-noise terrain fixtures remain for historical regression comparison.

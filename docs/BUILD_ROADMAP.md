# Build Roadmap

## Direction

Build an original Godot C# futuristic planetary combat game: factions pilot ships, compete for antimatter, develop home bases, research equipment and fight over deformable spherical terrain. Matter was removed by the 2026-10-12 playtest simplification. Magic Carpet remains a historical mechanics reference, not the setting or identity. Do not copy proprietary game data or assets.

Use `docs/BASE_DEVELOPMENT.md`, `docs/SHIP_EQUIPMENT.md`, `docs/EXPLORATION_BOONS.md` and `docs/STRATEGIC_POINTS.md` for the approved new gameplay direction. `docs/ORIGINAL_MECHANICS_REFERENCE.md` remains the historical reference for inherited mechanics. New design decisions supersede original-style priorities where they differ.

## Active Milestone: Science-Fiction Identity And RTS-Style Base Development (2026-10-11)

User acceptance: the basic spherical mechanics are in place and feel smooth enough to serve as the playtest baseline. Integrated hands-on baseline acceptance is complete; this is not final balance, AI, progression, content or performance signoff. Revisit measured performance issues when reproduced rather than requiring further profiling before gameplay development.

Approved design:

- Bases are remotely managed, real-time RTS-style home hubs. Explicit tiers 1–5 increase antimatter storage capacity and research access; stored resources no longer automatically purchase tiers.
- Stored antimatter supports the player's energy limit. Construction consumes storage immediately, lowering that limit. Current combat energy regenerates within it; collecting more raises it. The earlier reservation/limit-recovery proposal is superseded.
- Five independent construction slots: base tier/rebuild, defense, resources, offense and ship; one active project per slot, concurrent categories allowed.
- Cancellation fully refunds paid antimatter; capacity overflow spills as owned resources. Losing a tier destroys every in-progress project: half its paid investment spills for rivals to claim, half is permanently lost, separately from ordinary storage spills.
- Lost tiers reduce capacity/research access and must be rebuilt sequentially at half each tier's normal upgrade cost. Passive HP repair cannot restore lost tiers for free.
- Ships equip up to four owned equipment items, including offensive, defensive and strategic abilities. Built items/ranks and loadouts survive death; installed equipment survives base-tier loss, although energy costs can prevent use. Replaced equipment remains owned and can be re-equipped at home for free.
- Research is remote; installation/loadout changes require returning home. Items have two or three sequential upgrade steps, tunable per item.
- RMB Claim is built in, outside the four slots, with optional research improvements. Shift booster is a separate built-in short speed burst costing current energy; slotted Dash remains stronger mobility.
- Claim works at full storage; collectors stop gathering the saturated resource independently. Matter uses separate storage and claimed surface crates, with shared mixed-resource cargo and atomic material-dependent construction.
- Random wrecks hide permanent, stacking match-level boons, not equipment/rank unlocks. First collector secures three choices or a skip-for-antimatter bonus; selection is real time and deferrable. Skip deposits directly, with owned overflow. Initial wrecks and ongoing orbital arrivals exclude every faction's base.
- Economic sites use timed capture, halted by opposing player presence. Owned sites take neutralization then capture; prior benefits stop at neutralization. Departure slowly drains unfinished progress. Extractor income deposits directly into base storage and stops at capacity; research stations improve speed; logistics stations suspend while neutral and transfer their drone/cargo on final capture.
- Boons survive death/reset with the match; site benefits follow ownership. Damage categories, bonus formulas, rates, capture timing and layout are tunable. New management/reward/capture UI runs in real time; reconcile the legacy paused survey during UI work.

## Active Playtest Redesign — 2026-10-12

**Control follow-up — 2026-10-14:** restart moved from R to **F4**, including live management/map/boon interfaces, terminal results and flight-lab reset. Same seed/size retry semantics remain. Older R references below record historical controls; active gameplay/balance priorities are unchanged.

**Latest economy/combat pass — 2026-10-14:** `ECONOMY_COMBAT_PLAYTEST.md` implements larger **300/500/750/1000/1500** storage, next-tier prices at half current nominal capacity (**150/250/375/500 A**), **0.35-A/s** extractors/+20–40–60% optimization and **80-A** collectors/120–160–200 cargo stars. Tier-II Resources Harvester fleets kill wild mobs and claim physical drops for collectors. Attackers/mob threats are stronger; **25s/6s** recovery starts at the first wild vacancy without repeated-kill resets. Scatter/Seeker are playable; Pulse gains affordable body-aimed pack clearing. Catalog **68 types / 27 playable**. **Next:** hands-on Medium income, tier/equipment pacing, drone/mob threat and weapon roles, then remaining tier-II Hunter/Rocket/Flak before III–V. Earlier entries below are implementation history. Broader rival routing/difficulty and XL physics remain pending; verification is in the progress log.

**Latest implemented pass — 2026-10-14:** `AUTOMATIC_FLEETS_PLAYTEST.md` supersedes manual paid drone orders and Claim range research below. Base research grants automatic free-replacement Scout/Attack/Defensive/Strike/Siphon fleets with distinct automatic assignments and post-gold paid extra slots (provisional five/type). Siphons raid actual opposing base storage and physically return cargo. Claim Calibration improves owned-ball magnetism. Tier-II built-in researched **T Teleport Home** uses a 3s interruptible channel/20 E/60–45–30s cooldown. Targets fall to **72/135/225/338**; both maps gain one-pixel mob/AM dots, role-specific drone glyphs and line-of-sight-gated rival pilots. Catalog is **67 types / 24 playable**. **Next:** hands-on fleet pressure/theft/teleport/magnetism/density and Medium economic pacing; finalize fleet limit/prices and free-strike timing; remaining Hunter/Rocket/Flak/Scatter/Seeker, then III–V. XL physics/performance and broader rival routing/difficulty remain pending. Verification is in the progress log.

**Approved next pass — 2026-10-13:** amount-based internal collector cargo, penetrating 1-E Claim, smoother bank, slower neutral recovery/broader roaming, neutral/base combat and tier-scaled collectors; then four tier-row/star grids, remotely planned/automatically home-installed loadouts, manual base attack orders and scout/defensive drones. The approved 65-type catalog (three base upgrades/category/tier and four ship upgrades/tier) and staged implementation/verification are in `UPGRADE_TREE_PLAN.md`. Tier I–II precede III–V. This user-directed work takes priority over routing/difficulty; existing performance follow-up remains. Content approval is not implementation status.

**First stage implemented/verified:** collectors carry 30 A internally, partial-scoop and preserve overflow/theft/death totals; home fleets provide 1–5 collectors with 15s loss replacement and cargo-safe retirement on tier loss. Claim penetrates all swept AM for 1 E. Bank uses smoothed input and exponential easing. Habitats recover after 35s, with 8s replacements; roaming has wider bounded destinations/shorter pauses. Ordinary neutrals damage bases on contact; basic defenses retaliate at 30m/6 damage/1.2s, line-of-sight bounded. **Next implementation:** tier/star grids and planned/home-auto-applied ship loadouts, then paid base targeting/scout/defensive deployment. Hands-on feel/economy and XL performance remain acceptance work.

**Tree/interface/order stage implemented — 2026-10-13:** all four tabs have tier rows and sequential bronze/silver/gold stars; remotely planned loadouts automatically synchronize at home with pending HUD glyphs and no Apply click. The approved 65-type catalog has **22 functional types**, including all tier I, eight tier II and Guardian at IV; other types are disabled Coming later and cannot take payment. Scout/attack/defensive/strike research is separate from paid target-dropdown production. Scouts chart/capture nearest non-owned points at half speed without stacking, including enemy takeovers; defenders retreat and protect home after site loss; deployed units are damageable/interceptable. **Next:** finish tier-II Hunter/Rocket/Flak/Scatter/Seeker, then III–V content, alongside hands-on grid/loadout/orders/economy acceptance. Fresh dense XL is still expensive with and without additional deployments; no performance signoff is claimed. See the progress log for actual checks/profiles.

**Latest flight/salvage/startup follow-up — 2026-10-13:** implemented moderate lateral drift, turn/slip ship banking with camera roll, original delta hovercraft hulls, 65° shared slope traversal with uphill slowdown and bounded rival crater recovery. Live mob rewards double to **6/36/72/20 A**, with more balls, 90-ms release and varied outward scatter; scavengers also retain stolen reserves. Seeded games warm the actual actor terrain layout, while distant actors request smaller/lower-resolution support rather than global fine-detail bubbles. Medium first-minute p95 is ~19–20 ms; XL's persistent build backlog is removed, but early/dense physics spikes remain. See `PLAYTEST_FLIGHT_DROPS.md` and progress log. **Next:** hands-on drift/banking/crater/salvage/economy/startup acceptance, dense XL/first-use physics profiling, then broader rival routing/recovery and difficulty. The 40° terrain-generation roughness classification remains unchanged; it is no longer the ship's traversal cap.

**Latest follow-up — 2026-10-13:** centered the complete HUD (including radar/base HP), narrowed H/E/A gauges with current-only values, replaced boon immediate selection/summary with select/Reject/Confirm-and-close, and removed routine toasts. Shared Plasma uses **2 E / 13–18–23 damage**; both ships recover **2 HP/s after five combat-free seconds**. Player hits have brief camera shake/world darkening. Encounter targets are now **96/180/300/450**, with irregular clusters/pack footprints and seeded destination/pause wandering. The earlier pass's population/HUD/Plasma values below are history superseded by this follow-up. Verification and XL workload observations are in the progress log. **Next:** hands-on hunting/energy/healing/UI/economy acceptance, sustained XL performance, then rival routing/recovery/resilience and difficulty.

**Latest approved pass: dense encounters / single-resource economy / bottom HUD.** See `PLAYTEST_SIMPLIFICATION.md`. Implemented wild region-spread habitats with **64/120/200/300** targets, easy swarms and heavy beasts, differentiated rewards and ownership-aware site-guard replacement. Matter is removed; former material sites become antimatter extractors, making four extractors at **0.2 A/s**. Tier capacities **200/350/550/800/1100**, advancement prices **150/275/450/650 A**, 400-HP layers and 20% Plasma base damage replace previous tuning. Four management tabs/header upgrade and button-overlaid progress replace five-tab/active-strip UI; bottom gauges/weapons/radar/base HP replace persistent corner panels. The goal is economy-focused **Medium tier V in 10–15 minutes**, requiring human playtest acceptance. Earlier matter/density/capacity/UI entries below are implementation history superseded by this pass. **Next:** hands-on economy/density/UI/siege and sustained XL performance, then rival routing/recovery/resilience and difficulty. Actual verification is recorded in the progress log.

**Retained prior follow-up:** radius 100 rejected; live presets are **200/300/400/500 m**, Medium default. Home installation, four-item cap/row-order keys/pending-rank application, 220 m ship-only sensing, global home/owned sites and rival-base discovery memory remain. Proximity aggro **30/35/40/45 m** and bounded habitat retaliation remain. The newer pass above supersedes prior 16/24/32/40 populations, matter and toggle/corner UI.

User approved compact **100/200/300/400 m** planets with genuine curvature, dramatic islands/oceans/mountain ranges/ridges/passes, ground-hugging hover physics and **Space jump costing current energy** (provisional 2 energy, ~4 m level-ground height), free water traversal and no fall damage. Add Easy/Normal/Hard rival combat (Normal default) and role-specific encounter groups. Execute shared hover/jump, compact terrain/placement, traversable layout/routing, difficulty, density and integrated checks. See `TERRAIN_HOVER_PLAN.md` for acceptance and live stage status. This redesign is active ahead of further balance-only sessions; compact radius presets and layered terrain are now live.

**Stage 1 implemented/verified:** shared suspension/gravity, 2-energy Space jump, free ramp/drop launches, steep-face blocking, water hovering, no fall damage, boost momentum and bounded Dash lift. Hover checks pass 35 headless/35 rendered; relevant existing regressions and rendered compact UI pass. Hands-on hover/curvature/balance acceptance remains pending.

**Stage 2 implemented/verified:** 100/200/300/400 m presets, layered oceans/islands/lowlands/mountain belts/saddles, actual seeded-start landing pads, size-aware placement/capture/territory distances and retained deformation. Terrain checks pass 16,462 headless/16,464 rendered; all-size/four-seed layout passes 1,934 headless/1,848 rendered with Small/Medium camera/survey inspection. **Next: stage 3 traversable opening validation and rival routing**, then difficulty/grouped density. Equal geographic opportunities/flat destinations do not yet establish full route accessibility or hands-on balance.

**Accessibility correction implemented/verified after user playtest:** the initial belts were too pervasive. Most land is now gentle, with three localized mountain regions, lowland-preferring starts and longer adaptive destination/coastal ramps. Prepared land below 20° is **97.0/94.1/95.4/97.9%**; above the 40° hover limit is **0.8/1.0/0.9/0.6%**. Terrain checks pass 16,482 headless/16,484 rendered; layout 1,939 headless/1,853 rendered. Independent fresh-world access checks pass **3,288**, with 192 practical opening routes across four sizes/four seeds, only four local detours, and both-faction actual normal-hover resource out-and-back travel on every size. **Next: runtime rival slope-aware routing/deformation/live-obstacle recovery**, then difficulty and grouped density. The initial stage-2 tuning above is historical evidence superseded by this correction; hands-on feel/balance remains iterative.

**Steepness follow-up implemented/verified:** user increased the target to **3–5% of prepared land above 40°**, while retaining mostly gentle terrain/protected access. Localized shoulders and layout-calibrated folds give default steep coverage **3.8/3.8/3.6/4.1%**, gentle coverage **91.7/90.9/91.8/93.4%**. Independent sixteen-world sampling measures 3.3–4.3% steep; all 192 openings and ordinary-hover round trips remain verified (nine detours). Terrain checks pass 16,483 headless/16,485 rendered, access 3,287, layout 1,942. These measurements supersede the lower steepness figures above. Runtime rival routing/recovery remains next.

**Random-world generation implemented/verified:** new applications and F2 blank-seed starts choose fresh world seeds; **R retries the current seed/size**. F2 supports entered-seed replay and shows the current seed; F3 exposes accepted terrain seed/attempt diagnostics. Independent streams seed actual geography, layout, initial wrecks and neutral placement. Up to sixteen deterministic candidates validate land/slope budgets and twelve normal-hover opening approaches before mesh/actor creation. Sixteen tested terrain/size cases accept within 1–3 attempts, replay identically, preserve 3–5% steep/≥85% gentle land, and pass 192 opening routes plus real resource round trips. Fixed-noise measurements above are historical diagnostics, not a default random world. Runtime rival routing/recovery remains next.

Next work, in order (completed foundation history):

**Playtest improvements implemented 2026-10-12:** grouped role-specific neutral habitats and 16/24/32/40 population budgets, per-habitat 20-second recovery/five-second replacements, dearer permanent upgrades with 60–120-second base durations, two-stage owned-site takeover/half-speed departure decay, screen capture HUD and item-row/rank-column equipment research. This user-directed pass moved encounter density ahead of routing/difficulty. Values remain provisional; next is hands-on economy/capture/readability tuning alongside runtime rival routing/recovery, then difficulty. See current neutral/base/equipment/site/UI design docs and progress log for verification.

1. **Completed 2026-10-11:** behavior-preserving terminology conversion to bases, ships/pilots, equipment, antimatter, collector drones and Claim. Files/types/references, identifiers, controls, UI and verification tools are migrated; build/import, gameplay regressions and rendered UI checks pass. Historical research and working project identity remain. See `docs/TERMINOLOGY_MIGRATION.md` for mappings and profiler compatibility.
2. **Foundation completed 2026-10-11:** explicit tiers 1–5 with guarded sequential completion, configurable tier capacities, storage-backed energy without starting-power allowance/cap, atomic spending diagnostics and full-storage/cargo-aware collectors. Build/import, accounting/gameplay checks and rendered HUD verification pass. Player-facing upgrades arrive with construction; intermediate tier destruction/rebuilding remains item 3.
3. **Backend completed 2026-10-11:** five-slot concurrent project lifecycle with payment/timing/prerequisites, exact cancellation/owned overflow, surviving/purchased tier history, health-layer siege/project destruction and sequential half-price rebuilding. Gross/refund/spill/permanent-loss ledgers and headless/rendered regressions pass. Non-base content effects and player-facing orders are integrated by subsequent UI/equipment milestones.
4. **Completed 2026-10-11:** B-key real-time management with five slots, project costs/prerequisites/unavailable reasons, ordering/cancellation, live HP/storage/energy and siege feedback. Starter repair/recharge/collector/Claim research and repeatable one-shot strike drones have gameplay effects. M survey now runs in real time with shared ship-input ownership; setup/results retain pause. GUI/input/effects/restart checks and rendered layouts pass.
5. **Completed 2026-10-11:** owned equipment inventory, four-slot Ship UI, explicit home-base installation/replacement and sequential ranks I–III. 1–4 selects a slot, LMB activates, Tab cycles occupied slots. Starter Plasma Bolt only; Claim stays built in. Purchased/installed state survives death and tier loss. Legacy beacons are non-reward signals pending boon conversion.
6. **Implemented 2026-10-11:** built-in Shift booster (4 current energy, 36 speed, 0.65s thrust, 2s cooldown, fresh press), distinct stronger aimed/lifting slotted Dash and sequential speed ranks. HUD/Ship hints and headless/rendered mobility regressions pass; hands-on balance remains iterative.
7. **Implemented 2026-10-11:** random destructible wrecks, stable three-choice/skip offers, H-key real-time selection/deferral/summary, eight capped stacking match boons, exact direct-deposit/owned-overflow bonuses, charted wreck/salvage navigation and bounded all-base-excluding region-balanced orbital arrivals. Headless/rendered regressions pass; hands-on travel/pacing/balance remains iterative. Base-HP boons and deeper rival boon AI are deferred.
8. **Implemented 2026-10-11:** six neutral economic capture points with timed/contested pilot presence, direct capacity-limited antimatter income and supply ledger, ownership-derived additive project-speed modifiers, same-drone/attached-cargo theft and rerouting, global navigation/local capture feedback and management summaries. Departure resets progress; neutral logistics activates once and destroyed drones do not respawn. Headless/rendered regressions pass; deliberate rival capture routing and hands-on economic/layout tuning remain iterative.
9. **Implemented 2026-10-11:** independent matter reserves/capacities, Claim-marked surface crates, nearest-eligible mixed collector cargo, station material theft, atomic dual-resource construction, snapshotted material refund/siege/rebuild accounting, four material fields and two timed matter extractors. HUD/management/navigation/diagnostics expose material. Hands-on economic/hauling/cost balance remains iterative.
10. **Pre-playtest readiness implemented:** all four phases—globe layout/separated starts, territorial neutrals/raids, shared-mechanics economy-first rival and compact icon management/bottom-centred 4×1 equipment HUD—are regression-verified. **Next: hands-on Medium-match economic/combat/travel/readability balance.** See `PRE_PLAYTEST_PLAN.md`, `NEUTRAL_BEHAVIOR.md`, `RIVAL_AI.md` and `COMPACT_UI.md` for policies/controls. Deeper strategy/campaign/content follows feedback.

Items 1–9 and all four readiness phases are implemented for their first pass. **Active next work: runtime rival slope-aware routing/recovery in `TERRAIN_HOVER_PLAN.md`, after implemented hover/jump, compact terrain and verified mostly-accessible openings.** Rival difficulty follows; grouped density is now implemented from playtest feedback. Hands-on economic/combat/readability balance continues throughout. Explicit health layers govern tier loss/repair/storage; real-time management exposes paid icon orders, home installation and site summaries, and H exposes secured rewards/boons. Wrecks replace legacy beacons; boons cannot grant ranks. Policies/tuning remain provisional. Historical phases below describe implementation history/backlogs; the active milestone takes priority.

Current victory direction (user update, 2026-10-10): destroy the rival player's castle AND defeat its wizard, in either order. Mana possession/storage supports combat power and logistics; it is not a victory condition. This supersedes the earlier equilibrium-quota objective plan.

Playtest mechanics update (2026-10-10): wizards respawn after ten seconds while their respective castle survives; a rival respawn clears its previous defeat. Killing the wizard first can win only if the castle falls before it returns. Player castle destruction causes defeat. Surviving castles slowly repair after a quiet delay, neutral creatures carry/drop intrinsic mana, destruction spills fragment into ballistic small spheres, and shared flight clearance is capped at 12 units above terrain/water. See `docs/PLAYTEST_MECHANICS.md` for current defaults and validation steps.

## Planned Balance Tooling — AI-Versus-AI Simulation (2026-10-12)

User approved recording this future milestone; implementation is deferred until reached. Establish a lightweight harness after runtime rival routing/recovery is reliable, alongside upcoming combat/difficulty and balance work. Routing/recovery remains the immediate priority; the harness does not block the approved difficulty/grouped-habitat work.

- Run two AI-controlled factions through the actual shared gameplay systems in headless matches, with recorded seeds/settings, swapped starting positions, a match time limit and unattended result collection.
- Begin with economy-first, aggressive and defensive strategy profiles as rival equipment/tactical competence develops. Keep strategy priorities configurable independently of shared gameplay rules.
- Export CSV/JSON results: winner/duration, tier and research timings, resource income/spending, combat/deaths/base damage, and stalled navigation/stalemates. Distinguish execution failures from strategy outcomes.
- Measure wall-clock match throughput before choosing acceleration or parallel batch sizes; faster simulation must preserve meaningful physics/combat outcomes. Recorded seeds/configurations should support visible reproduction of interesting runs; exact replay determinism must be verified rather than assumed.
- Expand later into multi-seed round-robin tournaments and paired single-parameter comparisons for upgrade costs/effects. Use early runs for structural problems and broad trends; fine balance follows representative systems and watched/human playtests.
- A simplified high-throughput economic simulator is a possible later extension, not part of the initial milestone. Automated balance conclusions remain limited by bot competence.

## Development Tracks

### Active Scope Addition: Configurable Spherical Worlds (2026-10-10)

User-approved direction: spherical planets with radial gravity and surface-relative upright flight. The user playtested and accepted 800 radius, then selected **Small 500 / Medium 800 / Large 1,000 / XL 1,200** and requested the existing gameplay features on the globe. Medium is the default. Larger worlds for more participants remains a future direction; human networking scope is undecided.

Playable conversion implemented: default `scenes/PlanetGame.tscn` includes patch-based procedural spherical terrain/oceans/deformation/collision, upright carpet flight/cameras, radial physical mana/merging/spills, curved possession and balloon routes, castles/storage/power/repair, all spells/discoveries, enemy roles/rival wizard, respawns, objectives, HUD/VFX/audio, and F2 size selection. `PlanetFlightTest.tscn` remains a diagnostic lab with the same presets. Geometry and integrated gameplay regressions pass; hands-on tuning of the converted full loop remains pending. See `docs/SPHERICAL_WORLD.md`.

Active next priorities: hands-on dual-resource/capture-site, wreck/boon, rival-combat, mobility and UI-readability balance. All four readiness phases (`PRE_PLAYTEST_PLAN.md`) are implemented and verified, including compact interaction UI. Economy, tier construction/siege/rebuilding, starter content, equipment/home installation, booster, boons and matter logistics are complete for their first pass. Historical phases below retain earlier backlogs; shared systems run on the globe. Basic smoothness is accepted; per-size/balance acceptance remains iterative.

Strategic navigation milestone (2026-10-10): implemented five named resource/exploration landmarks, regional encounter bias, three resource fields, and relocated rank-II upgrades including distant/far-side rewards. Added All-Seeing Eye local radar, paused M-key whole-world survey, exploration/ownership visibility, static waypoints with surface-distance/bearing guidance, and globe-wide balloon reach for remote claimed drops. One rival and existing elimination/respawn rules remain. Implementation is regression-verified; hands-on pacing/readability acceptance is pending. See `docs/STRATEGIC_NAVIGATION.md`.

Combat hitch follow-up (2026-10-10): pooling/shared resources/spatial queries are followed by smaller atomic terrain mesh/collision tiles, retaining fine vertex spacing and safe LOD replacement. Rendered XL repeated-impact maxima improved from 89.65 to 18–20 ms; terrain batches from 87.10 to about three ms. Dense-mana isolation then identified repeated managed ground-support queries; same-tick support caching and filtering irrelevant deferred contact callbacks reduced combined-workload dense-creation maxima from 190.60/161.92 to 30.05/25.53 ms (Medium/XL). Dense-active maxima are 19.52/20.31 ms. Regression checks pass; Medium mixed stress still reaches 41.65 ms and one isolated first-full-use run had an 84.93-ms outlier. Next priority is longer-distance LOD/draw-count and mixed-stress profiling, alongside hands-on combat acceptance. See `docs/COMBAT_PERFORMANCE.md`.

Startup: F5 launches full spherical gameplay. In-game F6 switches to the flight lab and back to a fresh spherical match. The flat island remains `Game.tscn` for regression/reference; it is not the default world.

Long-distance profiling milestone (2026-10-10): added a repeatable two-lap traversal harness with all-size stress and dash-reference-speed routes, real collision probes, settled LOD working-set guards, draw/node/static-memory and frame-time reports. Corrected a reproduced render-to-physics LOD collision handoff gap by delaying old-collider retirement through a physics step. Four-size stress (23,434 samples) and fresh XL at 46 units/second (20,030 samples) pass collision/retirement checks. Return-home terrain/node counts stabilize, but XL still has 64–128-ms default-speed worst frames and up to 1,184 scene draw calls; attribution and hands-on acceptance remain next. See `docs/TRAVERSAL_PERFORMANCE.md`.

Hitch attribution follow-up (2026-10-10): traversal reports now include managed GC/allocation and viewport CPU/GPU timing with adjacent outlier samples. Paced and corrected unpaced XL stress each pass 7,900 samples/132 collision probes and stable return counts. Long 348–377-ms intervals persist with VSync disabled, without matching measured GC pauses or terrain/viewport work. Next isolate main-thread/native cleanup and presentation/scheduling waits; these runs do not establish a cause or hitch fix. See `docs/TRAVERSAL_PERFORMANCE.md`.

Terrain resource-lifetime follow-up (2026-10-10): phase/OS CPU probes located reproduced 121/52-ms spikes before ProcessFrame, with main-thread CPU work immediately after full GC. Terrain build arrays, meshes and collision-shape temporary owners now release deterministically after native-node assignment. Corrected XL stress maxima are 28.60/29.79 ms; fresh 46-unit/second laps max 22.00/27.74 ms across 20,030 samples, with stable working sets and no >33-ms frames. Checks pass. Next validate hands-on traversal/combat and mixed workloads; remaining allocation volume, draw counts and other machines/longer sessions still need acceptance. Earlier 350-ms outliers are not symbol-trace-attributed.

Mixed-load follow-up (2026-10-10): post-release stationary combat passes 4,620 samples/14 possessions with no >33-ms frames. New mixed traversal passes 13,468 samples/226 terrain probes/210 craters, conserving 798 mana per world and stable returned terrain counts. Fixed a reproduced live-edit collider publication gap by delaying old-shape retirement through physics, with no accumulating collision children. Mixed maxima are 37.88/40.26 ms Medium/XL; no >50-ms frames. Combat checks now pass 171 assertions headless/rendered. Next hands-on flight/combat/logistics acceptance and remaining renderer/draw/allocation tuning; see performance docs.

Renderer follow-up (2026-10-11): combined terrain ground/skirts into one surface and reused copied native-input scratch buffers. Fresh XL stress peak draws fall 1,024→595 (~42%) and lap allocations ~302/291→182/176 MiB (~40%), with stable terrain sets/collision. Final mixed run passes 13,468 samples/226 probes/210 craters and exact resource conservation; rendered geometry/gameplay checks pass. Sporadic presentation and first-use dense-body setup outliers remain (one 75-ms creation event, fresh repeat 22 ms). Next hands-on integrated acceptance and targeted setup/presentation tracing, then route/logistics or remaining spell work from feedback. See `docs/TRAVERSAL_PERFORMANCE.md`.

Keep work separated into three tracks:

- playable prototype: clean Godot systems and original content
- original-game research: observations, screenshots, measurements, and notes from `D:\Games\MagicCarpet`
- remastered assets: original art, audio, VFX, UI, and prompts

## Phase 1: Flight And Combat Feel

Goal: prove the game is fun at the smallest scale.

Status: implementation-complete for the initial prototype, pending runtime playtest tuning. A first procedural 3D arena exists with tuned carpet flight, camera toggle, mana-gated firebolt, pickups, chasing monsters with contact and ranged attacks, enemy mana drops, HUD, restart, terrain clearance, speed/altitude debug readout, player damage invulnerability, first-pass combat VFX, and placeholder audio.

Core work:

- tuned carpet flight with acceleration, drag, banking, altitude floor, and collision
- shared terrain/water-relative maximum flight clearance (12 units), also used by creatures, balloons, guardians, and physical mana; projectiles ignore this flight ceiling
- mouse-look, first-person camera, chase camera, and camera toggle
- one projectile spell with mana cost, cooldown, and hit feedback
- mana pickups and a visible mana/health HUD
- one simple chasing enemy with contact damage
- restartable debug arena

Deliverable: a replayable arena where flying, casting, collecting mana, and surviving already feels promising.

Remaining Phase 1 work before signoff:

- runtime playtest flight acceleration, drag, banking, camera smoothing, and altitude feel; adjust values from feedback if needed

## Phase 2: Terrain System

Goal: make terrain a tactical system, not just scenery.

Status: implementation-complete pending runtime tuning and signoff. The prototype now has a procedural heightmap island, water plane, chunked/generated mesh-collision terrain, height-band colors, direct height clearance, a stored height grid, bounded radial deformation APIs, deferred/coalesced chunk rebuilds, firebolt terrain craters, player-facing crater/raise/lower/flatten terrain modes, HUD edit/rebuild diagnostics, and a heightmap fallback for terrain targeting. The remaining work is runtime tuning and playtest validation.

Core work:

- heightmap terrain grid
- procedural island/arena generation
- generated terrain mesh and collision
- water plane and altitude safety
- deformation API for craters, raising, lowering, and flattening
- efficient mesh/collision rebuild path
- player-facing spell modes for crater, raise, lower, and flatten
- reliable terrain targeting path for deformation

Deliverable: generated terrain can be flown over and modified by spells.

Remaining Phase 2 work before signoff:

- runtime playtest combat impact craters in Godot
- direct shaping-mode playtesting/tuning is deferred: the user requested hiding crater/raise/lower/flatten controls for now (2026-10-10); internal tools remain implemented
- tune chunk size if terrain edits are too expensive or too slow

## Phase 3: Mana Economy

Goal: make mana control the strategic loop.

Status: first-pass implementation has verified mana-transfer correctness, grounded amount-scaled physics spheres, same-owner merging, and projectile-based possession. Mana carries explicit `Neutral`/`Player`/`Enemy` ownership with distinct colors; the well seeds a finite neutral orb field; and RMB `Possess` launches a terrain/water-skimming bolt that converts the first neutral/rival loose globe hit into player-owned mana. Per user direction (2026-10-10), the player cannot absorb or attract mana: possessed spheres remain in the world for balloons to scoop and deliver to castles, where deliveries grow spell capacity/regeneration. Player contact/proximity rejection, balloon delivery/overflow/pop, rival transfers, exact death drops, possession, and sphere physics are covered by headless Godot regression scenes. World-mana accounting includes loose/carried spheres, castle storage, and enemy reserves once each; initial spell capacity and castle-derived capacity bonuses are not additional world mana. Mana totals remain resource diagnostics; victory now requires rival castle destruction and wizard defeat. Remaining work is hands-on playtest tuning.

Core work:

- mana orb drops from enemies
- intrinsic neutral-creature reserves and compact randomly sized destruction drops that release one at a time at 50-ms intervals and fall straight down with exact-total accounting (hidden pending fragments reserve mana immediately); mana is counted from spawn and conserved through transfers/deaths, while recurring neutral spawns introduce new supply
- dramatically value-scaled physics spheres at one-third the accepted overall size, with downhill rolling, solid-object collision, visible six-unit same-owner attraction, and 1.25-second supported-contact merging followed by 0.6-second smooth mesh/collider growth; shared rolling/solver jitter tolerated and drops remain non-overlapping (scale/size ratios/animation/random fragments/faster merging/gravity-only drops accepted; staggered release awaits hands-on acceptance)
- mana wells or ambient sources
- possession marks mana for balloon-only player collection; player contact/proximity never collects it
- spell costs and storage limits
- mana routing to player castle/storage
- enemy contesting behavior
- claimed/possessed mana that increases the player's long-term mana pool
- passive mana regeneration derived from claimed mana and castle/storage state

Deliverable: fights happen because mana is valuable and contested.

Remaining Phase 3 work before signoff:

- runtime playtest grounded globe sizes, rolling/friction, merge readability, large-globe balloon cargo, and balloon-only collection pacing; automated real-physics and mana-conservation checks pass
- runtime playtest possession bolt aiming, surface clearance, hit tolerance, speed/range, cost/cooldown, and ownership colors in Godot
- runtime playtest balloon scoop/return/dock pacing and pop-spill behavior
- tune enemy thief/wizard orb acquisition, absorb amounts, and retreat thresholds around the claimed-mana loop
- validate world-mana totals during combat and hauling; quota state does not gate victory
- decide director pressure around castle assaults and resource control
- defer detailed capacity, regen, pickup rate, and contest-drain tuning until the claimed-mana loop is playtested
- drive the well forward from its finite neutral landmark role (visual landmark + initial field) as balloon/possession behavior matures

## Phase 4: Castle And Territory

Goal: add base building, storage, and strategic conflict.

Status: implementation in progress. The prototype has player/enemy castles, health/storage/growth, and `ManaBalloon` haulers. Damage reduces effective storage capacity (rounded up), spilling excess as neutral ballistic fragments; destruction releases the full reserve exactly once and leaves stage-zero ruins. Surviving castles repair 3 HP/second after five seconds without damage, reopening capacity without recreating mana. Both wizards respawn after ten seconds beside surviving castles. Growth and spell-power bonuses follow retained storage. Transfer, recovery, power, repair, respawn, and real-physics spill checks pass. Remaining work is runtime tuning, deeper sanctuary behavior, destroyed-castle rebuilding decisions, and signoff.

Core work:

- player castle site and growth stages
- mana storage and capacity
- damageable castle structure
- collector balloon or spirit equivalent (now owned `ManaBalloon` haulers)
- enemy castles
- castle attack/defense objectives

Deliverable: one arena has a player castle, enemy castle, and contested mana flow.

Remaining Phase 4 work before signoff:

- runtime playtest the castle scale, storage, and routing loop in Godot
- runtime playtest firebolt castle damage and the combined rival castle/wizard elimination objective
- add clearer territorial feedback if the current castle silhouettes are too subtle
- runtime playtest castle-backed ten-second wizard respawns; deeper sanctuary/respawn-cost behavior remains deferred
- runtime playtest castle storage regression, neutral spill visibility/recovery, and retained-mana spell power under siege; automated conservation and terrain-grounded spill checks pass
- runtime playtest surviving-castle auto-repair delay/rate and recovery logistics; decide whether destroyed castles can ever be rebuilt

## Phase 5: Enemy Ecosystem

Goal: create systemic pressure rather than only scripted waves.

Status: functionally complete for the current prototype. Role-based enemies include swarms, ground beasts, ranged casters, castle attackers, mana thieves, and a wizard archetype. Neutral creatures now carry/drop intrinsic mana and swarms/casters use bounded flight rather than upward drift. The arena starts with one designated rival; the objective replaces it after a ten-second respawn delay while its castle survives, and ecosystem wizard spawning stays disabled to avoid duplicate rivals. Thieves/wizards have retreat/deposit loops; fireballs damage enemies and the rival castle; attack checks use horizontal distance; and castles are separated for readable siege pressure. Fine tuning remains pending.

Core work:

- flying swarm enemy
- ground beast
- ranged caster
- castle attacker
- mana thief
- spawn rules by biome and intensity
- local steering and target priority
- enemy wizard with flight, spells, mana goals, retreat, and castle interaction

Deliverable: monsters and enemy wizards interact around mana, castles, and player pressure.

Deferred Phase 5 tuning:

- runtime playtest the enemy mix, spawn pacing, and role behavior in Godot
- tune intensity ramp, max active enemies, spawn interval, and role health/damage values
- decide whether enemy wizard retreat and castle interaction need deeper behavior after the spell/campaign loops exist
- deeper rival wizard AI is explicitly deferred by the user (2026-10-10); current visibility, defeatability, and castle-destruction objective behavior are accepted for this pass
- confirm red castle attackers and purple wizards appear at readable pressure levels
- rival visibility and defeatability are confirmed by user playtest; further robe/carpet/nameplate polish and flight/combat tuning are deferred
- confirm ranged enemy shots and castle rams are now obvious and reliable during playtest

## Phase 6: Spell System

Goal: make spells the player's primary expression and progression axis.

Progression update (2026-10-10): five exploration beacons now grant one-time rank-II upgrades for Firebolt, Burst, Shield, Dash, and Guardian. All existing spells remain available at rank I; stronger effects preserve current costs/cooldowns and are independent of castle mana power. Discoveries survive wizard respawn and reset with R. Actual collection, casting, terrain-following landmarks, pause behavior, and arena restart are regression-checked. See `docs/SPELL_PROGRESSION.md`. Locked-spell unlocks, campaign persistence, general hand reassignment, and homing remain pending; next validate upgrade visibility/pacing and tune the spell tradeoffs before broadening progression.

Status: first-pass spell variety is implemented. The abilities have a small spell definition model and visible HUD loadout text. `Arcane Burst` adds area damage on `Q`, `Mana Shield` adds a defensive spell on `E`, `Wind Dash` adds mobility utility on `F`, and `Guardian` adds a temporary summoned ally on `G`. `Possess` is the first `Economy`-kind spell, dedicated to RMB and launching a terrain-following claiming bolt. `Tab` cycles the five combat/utility quick spells for `LMB`, while direct spell hotkeys remain available. Per user direction, the direct crater/raise/lower/flatten tool and its 1–4 bindings are hidden; its internal code remains available, and firebolt impact craters still work. The original-style claimed/regenerating mana base now exists, so further spell tuning, unlock/upgrade rules, and runtime validation can resume once the claimed-mana loop is playtested.

Core work:

- spell data model
- quick slots and loadout
- two active hand/button bindings with fast reassignment
- mana costs, cooldowns, upgrades, and unlocks
- spell pickups or unlock discoveries tied to exploration
- light auto-targeting/homing rules for appropriate offensive spells
- projectile damage spell
- area damage spell
- terrain deformation through combat impacts (direct shaping tool deferred/hidden)
- summoning spell
- shield or defense spell
- mobility or utility spell
- economy/trap spells such as possession, mana stealing, and fake mana

Deliverable: a small spellbook with meaningful tactical tradeoffs.

## Phase 7: Campaign Structure

Goal: turn arenas into a full game loop.

Status: the initial rival/castle elimination pass was accepted by the user; the subsequent requested playtest mechanics are implemented pending hands-on tuning. `LevelObjective` requires rival castle destruction and designated wizard defeat, tracking that wizard through ten-second castle-backed respawns. Player death starts recovery while its castle survives; home castle destruction causes defeat. Rival respawn clears the earlier kill, making wizard-first victory time-sensitive. Terminal results pause gameplay, release the mouse, and show an R-to-restart overlay. Headless, real-frame, and rendered regressions verify respawns, objective transitions, and arena reload. Deeper rival AI is deferred; broader balance, multiple realms, and progression remain pending.

Core work:

- island/realm level model
- objective variants
- tracked world mana as a strategic resource (intrinsic neutral-creature spawns introduce supply); rival castle AND wizard elimination win condition with castle-backed respawns
- biome themes
- escalating enemy wizard setups
- persistent unlocks and spell progression
- win/loss flow

Deliverable: several levels connected by progression and escalating challenge.

## Phase 8: Presentation And Content

Goal: establish the remastered identity.

Status: presentation remains placeholder-based. A compact edge HUD shows gameplay essentials, with diagnostics/control help in a formatted `F3` debug panel. Spherical gameplay now has heading-relative radar and a paused M-key world survey with ownership/exploration markers and waypoint guidance. Rendered captures and navigation regressions have been checked; hands-on readability/travel tuning, richer map features, original assets/audio, and polish remain.

Core work:

- original stylized fantasy terrain, skies, castles, monsters, and carpets
- readable spell VFX and combat hit feedback
- HUD, spell icons, menus, and accessibility options
- All-Seeing Eye/minimap and full-world map for mana, castles, balloons, threats, and ownership state
- original audio for spells, wind, monsters, castle activity, and music
- polish passes for performance, controls, and onboarding

Deliverable: a coherent original game that evokes the classic fantasy without relying on copied assets.

## Original Game Research Backlog

Use the DOS install as observation material only.

Research tasks:

- capture short videos of flight, combat, HUD, mana collection, castle building, and terrain deformation
- document controls and camera behavior
- document spell list, costs, pacing, and effects
- document mana balloon behavior and castle growth
- document enemy and wizard behavior
- document level objectives and progression rhythm
- inspect file layout with read-only/local-only tools where legally acceptable

Do not commit original binaries, `.dat`, `.inf`, `.tab`, `.pal`, audio, images, movies, or direct extracted assets unless license/ownership is explicitly cleared.

## Near-Term Milestones

The active 2026-10-11 base/equipment sequence above supersedes the order below, which is retained as the historical prototype progression.

1. Flight prototype: tuned movement, camera modes, spell firing, one enemy, mana pickups.
2. Terrain prototype: generated heightmap arena and collision.
3. Mana loop: collect, spend, store, and contest mana.
4. Castle loop: build, defend, attack, and route mana.
5. Enemy wizard duel: rival AI with flight, spells, and mana goals.
6. Vertical slice: one complete level with win/loss, HUD, audio, and polish.
7. Campaign alpha: multiple levels, several spells, progression, and biome variety.

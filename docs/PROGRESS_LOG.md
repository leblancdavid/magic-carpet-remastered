# Progress Log

## 2026-10-10

### Completed

- User accepted faster merging and gravity-only drops, then requested one-at-a-time release. Shared creature/castle/balloon fragments now release at 50-ms intervals with the first ball immediate (six-ball creature drop completes in 0.25 seconds). Pending balls remain hidden/frozen/noncolliding and unavailable to possession/collection/merging, while their mana is reserved/countable immediately for exact conservation. Physics countdowns pause with gameplay and remain on the world parent after source death; merge grace starts on release. Later releases lift above active globes that invade their reserved spot to avoid overlap-driven scatter. Added stagger timing, pending-state/transfer gates, pause/resume, per-ownership accounting, release-clearance, and radial-drop checks. Corrected castle-spill solver verification to measure the actual box footprint after attraction. Updated current tuning, roadmap, and lessons. Next hands-on check: judge the 50-ms cadence and larger reserve spill duration.

- Applied further user feedback after acceptance of smaller animated/randomized mana: reduced contact dwell from 3 to 1.25 seconds and smooth growth from 0.9 to 0.6 seconds. Shared creature-death/balloon-pop/castle-spill drops now start with zero velocity, falling under gravity instead of launching outward/upward. Tightened default placement from a 1.5-unit ring to radius-only compact packing with 0.15-unit offsets; non-overlap checks remain and castle spills still start outside their collider. Random sizes, exact totals, ownership, and magnetism remain intact. Updated real-physics checks for faster merging and true straight-down trajectories, including globe-underside drops. Updated tuning/roadmap/lessons. Next hands-on check: merge pacing and compact drop visibility on flat ground, slopes, and water.

- User accepted the mana size ratios and clustering fix, then requested one-third overall ball size, animated merge growth, and randomly sized explosion drops. Reduced radius coefficient to `0.35 / 3` while preserving the 0.65 exponent. Merged mana commits immediately and the survivor smoothly grows its mesh/collider over 0.9 seconds, gradually lifting to keep ground contact; chained merges wait for growth to finish. Random positive integer partitions now replace equal-valued death/castle/balloon fragments, preserving exact totals, the existing fragment-count formula, and the 128-body cap; variation is guaranteed when mathematically possible. Added intermediate-size/collider/grounding/accounting animation checks and randomized partition boundary cases. Updated current tuning, roadmap, and lessons. Next hands-on check: judge the smaller scale, growth speed, and varied destruction drops.

- Fixed the follow-up mana size/merge/magnetism playtest report. Radius now uses `0.35 * pow(amount, 0.65)` (1/2/10/80 mana radii approximately 0.35/0.55/1.56/6.04), giving strong fragment-versus-reserve silhouettes. Same-owner grounded balls select their nearest neighbor within a six-unit surface gap, with up to 3 units/second² tangential acceleration and a 1.5-unit/second approach-speed limit. Rolling no longer disables attraction or resets contact dwell; merging keeps the three-second delay but checks relative pair speed instead of absolute speed/spin. Widened contact skin to 0.12 and added support-ray fallback for missing solver floor manifolds, reproducing/fixing a case where visually touching spheres never completed their timer. Huge spheres prioritize radius clearance over the flight ceiling. Added actual separated-to-contact-to-merge checks on flat ground and the globe underside, shared-motion merging, ownership isolation, wall blocking, airborne rejection, and huge-globe water support. Updated tuning, roadmap, and durable lessons.

- Set `project.godot` startup window mode to fullscreen (Godot mode 3), applying to gameplay and the flight lab.

- Applied user playtest feedback to mana physics: reduced destruction/spill launch speeds to 2–3.2 outward / 2.5–3.3 upward; enlarged small fragments approximately twofold and increased value-based radius scaling to `1.1 * pow(amount / 10, 0.45)`. Added initial fragment-clearance checks so dense enlarged bursts cannot explosively separate overlapping colliders. Nearby same-owner settled spheres now attract gently along the surface (two-unit gap, capped acceleration/speed) and merge only after three continuous seconds of slow grounded contact. Separation/motion/ownership/carry reset progress, and each survivor growth restarts contact dwell. Preserved exact resource accounting, solid collisions, and balloon-only player collection. Updated real-physics/spherical/spill regressions and current tuning/lesson/roadmap documentation.

- Made first person the startup camera in full gameplay and the spherical flight lab. Gameplay initializes directly at the first-person camera pose; the carpet is hidden in first person and restored immediately when toggling to chase view. Player firebolts and Arcane Burst now skip player-owned balloons. Verified the existing home-castle protection against player firebolts/Burst, while enemy attacks still damage it and player attacks still damage rival structures. Added `tools/CameraFriendlyFireChecks.cs` / `.tscn`. Next hands-on check: toggle both views and fire both offensive spells near the home balloon/castle.

- Fixed the reported `ObjectDisposedException` in spherical `EnemyProjectile` collision sweeps: a projectile retained its freed shooter's C# wrapper and called `GetRid` after the enemy died. Both enemy projectiles and player firebolts now require a valid native caster before excluding its collider or comparing owner collisions. Shots continue after caster disposal and retain normal damage/expiry. Added seven owner-lifetime assertions to `PlanetGameplayChecks`, covering actual deferred enemy/player deletion, continued motion, enemy-shot damage, and both projectile lifetimes. Next hands-on check: kill a caster while its purple shots are airborne and continue the duel.

- Integrated the user-requested full spherical gameplay prototype and approved **Small 500 / Medium 800 / Large 1,000 / XL 1,200** presets. `PlanetGame.tscn` is now the main scene with Medium as the default. F2 pauses for size selection and starts a fresh match; R retains the selected session size. F6 opens the diagnostic lab and returns to a fresh spherical match. This supersedes the earlier lab-only startup entries.
- Added `PlanetTerrain`: six cube-sphere faces split into patches, procedural land/oceans, approximately two-unit nearby detail/collision, coarser distant patches, visual LOD skirts, shared direction-based deformation heights, and deferred affected-patch rebuilds. `HeightmapArena` exposes surface elevation/clearance/up/distance/steering and collision-body heading transport while retaining its flat regression mode.
- Converted the existing gameplay systems to the globe: collision-based carpet flight/cameras and dash/summon/terrain aiming; radial-gravity physical mana, same-owner merging and local ballistic spills; terrain-skimming possession; upright castles, repairs, storage/power and radial respawns; curved balloon routing/local cargo; all enemy roles/rival flight/aim/visibility and distant-player ecosystem pressure; radial discovery columns and existing spell upgrades; unchanged elimination rules, HUD/VFX/audio and restart flow. Castles are about 280–290 surface units apart; spherical balloon search covers 200 units. Preserved the flat suites and added `PlanetGameplayChecks` for actual gameplay on different hemispheres, seams, and every preset. Updated roadmap, README, spherical design notes, and durable lessons.

- Corrected startup after the user reported the world still looked unchanged: `project.godot` now launches `PlanetFlightTest.tscn` by default. Updated launch instructions to use editor F5 and keep explicit island-arena smoke commands. In-game F6 switches between the scenes.

- Added the user-approved configurable spherical flight milestone: `PlanetFlightTest.tscn`, `PlanetFlightTest`, `PlanetFlightController`, and `PlanetGeometry`. Includes radius presets/custom settings, speed/clearance controls, constant inward gravity with carpet lift, pole-safe great-circle travel/heading transport, first-person/chase cameras, camera surface protection, distance markers, two upright castle landmarks, and live flight diagnostics. F6 switches between the arena and flight lab. Recorded tuning rules, controls, acceptance steps, and conversion gaps in `docs/SPHERICAL_WORLD.md`; spherical flight playtesting is the active next milestone.

- Continued Phase 6 with exploration spell upgrades: five labeled gold rune beacons around the outskirts grant rank II once per combat/utility spell. Firebolt deals 35 damage; Burst gains 25% damage/radius; Shield gains two seconds; Dash gains 20% speed; Guardian gains 16-damage attacks and an 18-second lifetime. Existing costs/cooldowns and starting access remain; progression stays separate from mana ownership/castle power.
- Added `SpellDiscovery` with living-player-only flight-column collection, immediate consume guards, terrain/water-following landmarks, and pause inheritance. Upgrades survive castle-backed player recovery and reset on arena restart. Added ranked selected-spell text, dedicated four-second discovery feedback, a compact beacon hint, and F3 progression details. Documented rules/playtest steps in `docs/SPELL_PROGRESSION.md`; added `tools/SpellProgressionChecks.cs` / `.tscn`.
- Fixed ceiling-height casting per user playtest feedback: removed ceiling-based clamping/expiry from fireballs, possession bolts, and enemy projectiles. Shots preserve launch direction above the actor flight ceiling; normal impacts, lifetime/range expiry, and possession's terrain/water-skimming transition remain. Updated regression checks for ceiling/above-ceiling muzzle origins and upward/horizontal/downward aim. This supersedes the earlier projectile-ceiling behavior below.
- Added the user-requested playtest mechanics: neutral ecosystem creatures carry/drop an intrinsic 12-mana reserve; enemy deaths, castle spills, and balloon pops now share many-small-sphere ballistic fragmentation (normally 1–2 mana, 128-body cap, exact remainder accounting, 0.8-second merge grace). The tracker separately counts neutral-creature reserves. Resource-bearing neutral spawns introduce supply; transfers/deaths conserve it. Rival respawns have no starting physical reserve.
- Added castle auto-repair at 3 HP/second after five damage-free seconds. Hits reset the timer; repair restores capacity without recreating spilled mana. Destroyed keeps remain ruins.
- Added ten-second player/rival respawns beside their surviving castles. Removed the legacy deferred arena reload on player death; dead players stop flight/casting/collision. Player recovery restores health/current spell mana and two-second protection. The objective tracks replacement rivals, clears old defeat state on respawn, and exposes countdowns to the HUD. Player castle destruction remains defeat; rival castle destruction prevents further respawns. Killing the rival first remains valid only until it respawns.
- Added a shared 12-unit terrain/water-relative flight ceiling for players, ecosystem/legacy enemies, guardians, balloons, and physical mana; upward projectiles expire at the ceiling. Swarms and ranged casters now share bounded horizontal/vertical steering instead of normalizing tiny upward biases into indefinite ascent.
- Added `tools/PlaytestMechanicsChecks.cs` / `.tscn` and updated existing objective/mana regressions. Documented current tuning and the hands-on test sequence in `docs/PLAYTEST_MECHANICS.md`. These entries supersede the earlier no-repair/no-respawn and terminal-player-death behavior below.
- Implemented castle economic damage: effective storage capacity is intact capacity times remaining health, rounded up; storage exceeding it spills as bounded, exact-remainder neutral spheres outside the keep collider, placed against terrain/water. Destroying either castle releases all remaining storage exactly once and shows stage-zero ruins. Growth regresses as stored mana falls.
- Replaced cumulative delivery-derived power with starting allowance plus retained player-castle storage, capped by maximum power. Initial reserves contribute in either spawn order; deposits, withdrawals, and damage immediately recompute capacity/regeneration power and clamp current spell mana. Re-delivery cannot permanently stack bonuses. Added power gain/loss feedback and effective/intact storage diagnostics in F3.
- Expanded mana-loop and real-physics checks for castle damage/destruction, exact spill remainders, power caps and reductions, withdrawals, possession and balloon recovery, and terrain-grounded spill placement. Castle repair/rebuilding remains unimplemented; surviving damaged castles accept recovered mana only up to their reduced capacity.
- User signed off the current rival visibility/combat and castle-destruction objective pass after hands-on playtesting. Deeper rival wizard AI is explicitly deferred to a later pass.
- Fixed the "rival wizard isn't showing up" report: real-frame reproduction showed its tiny upward movement being normalized into full-speed vertical flight, reaching roughly 91 units above terrain after ten seconds. Wizard flight now uses separate horizontal steering and bounded vertical correction, follows terrain/water, and tracks combat height without climbing indefinitely.
- Fixed wizard mana approach stopping eight units away despite a 2.7-unit absorption radius. It now reaches collection range and banks mana. Destroyed rival castles no longer trigger the wizard's reserve-based retreat behavior.
- Replaced the wizard's generic sphere with original primitive robe/head/hat/carpet geometry, a matching capsule collider, and a billboard RIVAL WIZARD label. Added `tools/WizardFlightChecks.cs` / `.tscn` for real-frame flight/resource regressions.
- Implemented the user-corrected level objective: victory requires rival castle destruction AND designated rival wizard defeat, in either order; mana possession/storage never triggers victory. This supersedes the earlier quota-based victory plan. Player death or home castle destruction causes prototype defeat, with defeat taking precedence if both sides lose in the same evaluation.
- Added one named rival wizard at arena start and disabled repeatable ecosystem wizard spawning in the arena. Recorded its death before node removal so killing it first still counts later.
- Enabled firebolt damage against the rival castle and guarded consumed projectiles against repeat collision damage. Added rival-target HUD status and a centered victory/defeat result; gameplay pauses, mouse releases, and R reloads/unpauses the arena.
- Added `tools/LevelObjectiveChecks.cs` / `.tscn` covering objective transitions, castle damage, result UI/pause, dynamically spawned enemy pause inheritance, and actual arena restart.
- Removed player mana pickup/homing per user direction: possession marks world mana for balloon collection, and proximity or physical player contact never absorbs any color or grants spell power/refill. All loose ownership colors retain gravity/collisions; balloon castle delivery remains the player-side power-growth route. This supersedes the earlier player-homing/absorption entries below.
- Removed the unused player pickup APIs/settings, absorbed-world-mana reserve/accounting, and HUD absorbed-mana readout. Updated regression checks and current README/roadmap/learnings for balloon-only collection.
- Converted loose mana from bobbing `Area3D` pickups into `RigidBody3D` spheres. Initial loose placement uses terrain/water height + sphere radius, removing the hover gap. Gravity, friction, angular rotation, and continuous collision detection let mana roll downhill and collide with castles/world objects; water retains surface support.
- Made sphere volume and physical mass proportional to mana amount: radius is `0.45 * cbrt(amount / 10)`. Visual mesh and collider resize together after partial transfers and merges.
- Added deferred same-ownership contact merging with a stable survivor, exact amount conservation, weighted linear momentum, and immediate consumption of the donor. Different colors remain solid separate spheres; carried/consumed orbs cannot merge. Sustained contacts also allow merging after an ownership change.
- Changed owned-orb homing to physics velocity so solid obstacles block it. Balloon cargo freezes/disables collisions while carried, resumes gravity on release, and castle overflow spills outside the keep collider. Possession sweeps include each globe's current radius.
- Corrected heightmap triangle winding to Godot's clockwise upward-facing convention. The formerly downward-facing concave collision surface prevented actual rigid-body slope contacts despite the player's height-clearance fallback making terrain appear functional.
- Added `tools/ManaPhysicsChecks.cs` / `.tscn` for real-solver regression checks; adapted the existing manual mana-loop checks for velocity-driven homing and grounded radius clearance. Documented the new verification command in README.
- Fixed balloon arrival `LookAt` errors reported during playtesting: orient from the departure position before capped movement reaches the target. Already-arrived and vertical-only moves preserve facing.
- Replaced instant aim-weighted possession with `PossessProjectile`: a visible cyan bolt flies along the camera aim, then follows terrain/water at fixed clearance and a straight horizontal heading. Short heightmap substeps and swept sphere hits claim the first eligible neutral/rival globe, preserving its mana amount; carried, consumed, and already-owned globes are skipped.
- Dedicated RMB to Possess (2 mana, 0.4-second cooldown); removed Possess from the five-slot LMB/Tab cycle. Disabled direct terrain casting and 1–4 bindings, hid its gameplay/help hints, and retained the internal shaping tools and firebolt craters.
- Grounded unclaimed neutral/rival loose globes at terrain/water height + 1.3 units so possession bolts can reach seeded mana, death drops, and balloon spills. Carried/player-owned homing globes retain their existing movement.
- Added cyan impact feedback and delayed possession status on actual globe hits. Initial projectile tuning is 48 units/second, 120-unit range, 4-second lifetime, 0.9-unit surface clearance, and 1.3-unit hit radius; misses spend mana and expire without claiming.
- Completed the next mana-loop correctness pass before expanding level objectives.
- Fixed player-owned orb homing so movement accumulates instead of resetting to the original anchor every physics tick.
- Added a shared `IsAvailable`/`TakeMana` path: consumed or queued-for-deletion orbs cannot be collected twice, and partially accepted transfers preserve the remainder.
- Fixed balloon seek-radius units, hover-aware scoop/dock checks, capped movement, idle return position, and safe multi-orb deposits. Cargo now sits spaced below the balloon rather than inside its mesh.
- Fixed partial castle delivery: subtract the accepted amount, spill only the neutral remainder, and clear the cargo list after enumeration.
- Added `CollectedWorldMana` for physically absorbed player orbs. The world tally includes this separately from castle storage; starting spell capacity and castle-derived capacity growth are not extra physical mana.
- Enabled wizard orb absorption and thief castle deposits, revalidated target availability/ownership at absorption, and removed automatic 12-mana reserves from repeatedly spawned wizards.
- Preserved division remainders in ecosystem death drops. Legacy monsters now expose/count their initial reserve, return absorbed mana on death, and reject repeated death damage.
- Added `tools/ManaLoopChecks.cs` and `tools/ManaLoopChecks.tscn` for focused headless checks against actual Godot nodes. Updated README with current status and reproducible commands.
- Found Godot .NET 4.6.3 at `D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe`; CLI runtime verification is now available using its full path.
- Replaced the wall-of-text HUD with compact edge panels for health/mana, quota progress, and selected mouse spells. Feedback now appears only during its active timer (or defeat), rather than retaining stale status text.
- Added `F3` debug/controls toggle, hidden by default. The right-side panel uses readable padded backgrounds, section headings, wrapped text, and `PgUp`/`PgDn` scrolling; live refresh preserves the scroll position. HUD controls ignore mouse events so flight/casting stays usable.

### Verified

- Staggered mana drops: `dotnet build MagicCarpetRemastered.sln` passed with zero warnings/errors. Headless Godot suites passed: playtest mechanics **397**, spherical gameplay **238**, mana loop **173**, and mana physics **94**. Verified first-ball-immediate/sub-interval timing, hidden pending fragments, unavailable collection/transfers, pause/resume, partial/final conserved ownership totals, overlap-safe late release, and radial falling. A 600-frame default gameplay smoke exited cleanly and `git diff --check` passed. The spherical suite retained its known exit-time ObjectDB warning.

- Faster merging/gravity-only drops: `dotnet build MagicCarpetRemastered.sln` passed with zero warnings/errors. Headless Godot suites passed: mana physics **94**, playtest mechanics **387**, spherical gameplay **237**, and mana loop **173**. Coverage confirms supported touching balls begin merging in under two seconds, compact fragments fall without horizontal launch drift, underside fragments accelerate radially inward without tangential scatter, and randomized totals/hauling/ownership remain conserved. A 600-frame default gameplay smoke had no gameplay errors; the spherical suite and smoke retained the known exit-time ObjectDB warning. `git diff --check` passed.

- Smaller/animated/randomized mana: `dotnet build MagicCarpetRemastered.sln` passed with zero warnings/errors. Headless Godot suites passed: mana physics **93**, playtest mechanics **387**, spherical gameplay **234**, and mana loop **173**. Tests verify exact one-third sizing, intermediate growth and final radius, synchronized collider/mesh, grounded growth, conserved animated merges, positive variable-sized partitions including tiny drops/body-cap cases, and existing attraction/hauling/ownership behavior. A 600-frame default gameplay smoke exited cleanly and `git diff --check` passed. The spherical suite retained its known exit-time ObjectDB warning.

- Follow-up mana fixes: `dotnet build MagicCarpetRemastered.sln` passed with zero warnings/errors. Headless Godot suites passed: mana physics **86**, spherical gameplay **234**, playtest mechanics **119**, and mana loop **173**. A 600-frame default spherical gameplay smoke completed without errors. The spherical suite retained its known exit-time ObjectDB warning; the physics suite exited cleanly after disposing temporary support-query resources. Hands-on acceptance of dramatic sizing and visible clustering remains the next check.

- Fullscreen startup: rendered default-game launch (`--path . --quit-after 120`) completed without errors; `git diff --check` passed.

- Mana cluster tuning: `dotnet build MagicCarpetRemastered.sln` passed with zero warnings/errors. Headless Godot checks passed: mana physics **80**, playtest mechanics **119**, spherical gameplay **233**, and mana loop **173**. New coverage includes attraction, delayed merge/recontact resets, four seconds of moving touching spheres refusing to merge, enlarged radius/collider scaling, capped/non-overlapping 128-fragment large spills, compact real ballistic arcs, and delayed underside merging. A 600-frame default spherical startup smoke completed without gameplay errors. The previously known exit-time ObjectDB warning appeared in the spherical suite. Hands-on size/scatter/magnetic clustering feel still needs acceptance.

- Camera/friendly-fire fixes: `dotnet build MagicCarpetRemastered.sln` passed with zero warnings/errors; Godot headless editor import passed; dedicated camera/friendly-fire checks passed **15 assertions**, and integrated spherical gameplay checks passed **233 assertions**. The spherical suite reported the previously known exit-time ObjectDB warning. `git diff --check` passed. Camera transition feel remains a hands-on check.

- Disposed-projectile-owner fix: the new regression reproduced the exact `EnemyProjectile.cs:56` exception before the guard was added. After the fix, `dotnet build MagicCarpetRemastered.sln` passed with zero warnings/errors; spherical gameplay checks passed **232 assertions**, playtest mechanics **86**, and mana loop **173**. `git diff --check` passed. The known exit-time ObjectDB warning appeared in the spherical check; no gameplay exceptions occurred.

- Full spherical conversion: clean `dotnet build MagicCarpetRemastered.sln` (zero warnings/errors) and Godot headless editor import. `PlanetGameplayChecks` passed **225 headless / 227 rendered assertions**, including all four real gameplay scenes, surface placement/ceilings, crater/raise/flatten/seam continuity, real underside mana gravity/contact merging, seam-crossing possession, balloon delivery/castle power, all spell types, real carpet/wizard movement on other hemispheres, camera clearance, castle spill/repair conservation, radial player/rival respawns, upgrades, victory/defeat, and menu pause/resume. Inspected rendered gameplay and F2-menu screenshots for visible terrain/castle/balloon/beacons/HUD and readable settings. Existing suites passed: mana loop 173, mana physics 70, objectives 35, playtest mechanics 86, progression 43, wizard flight six, and flight-lab geometry 467. A **1,200-frame default spherical startup smoke run** completed without gameplay errors; the existing exit-time ObjectDB warning appeared in some runs. `git diff --check` passed.

- Spherical startup correction: default project launch (`--headless --path . --quit-after 120`, without an explicit scene) completed without errors; `git diff --check` passed.

- Spherical flight milestone: `dotnet build MagicCarpetRemastered.sln` passed with zero warnings/errors; Godot editor import passed. `PlanetFlightChecks` passed 467 headless assertions and 468 rendered assertions including screenshot saving. Verified multiple radii, full-lap pole crossings with stable frames/heading, nonzero-center inward gravity, antipodal surface distances, actual controller circumnavigation/arc odometer, radial altitude convergence, and radius reset. Rendered capture confirmed the lab settings/readouts, carpet, spherical horizon, and castle landmark are visible. A 300-frame main-arena smoke run completed without gameplay errors (existing exit-time ObjectDB warning); `git diff --check` passed. Comfortable radius/speed selection still needs user playtesting; spherical terrain/gameplay conversion and multiplayer scope remain pending.

- Spell progression pass: `dotnet build MagicCarpetRemastered.sln` passed with zero warnings/errors; headless Godot editor import passed. New progression checks passed 43 assertions covering actual low/high flight collection, distance/height rejection, all five upgrades and duplicate guards, economy isolation, actual upgraded casts (including Burst hits outside its base radius), shield mitigation, dead-player rejection, respawn retention, terrain-following beacons, paused collection, five arena placements, and actual R reset. Mana-loop checks passed 173 assertions, objective checks passed 35, and playtest mechanics checks passed 86. A 600-frame arena smoke run completed without gameplay errors. The known exit-time ObjectDB warning appeared in objective and progression integration checks. Visibility/HUD layout and upgrade balance still need hands-on review.
- Projectile ceiling fix: `dotnet build MagicCarpetRemastered.sln` passed with zero warnings/errors; playtest mechanics checks passed 86 assertions and mana-loop checks passed 173. Verified fireballs/Possess maintain aim and speed from ceiling and above-ceiling muzzle positions, enemy projectiles cross the ceiling, and downward Possess still reaches terrain/water skimming. Existing possession hit/range/lifetime checks pass. `git diff --check` passed.
- Playtest mechanics pass: clean C# build (zero warnings/errors) and Godot headless editor import. New checks passed 73 assertions, including real ten-second respawns, paused countdowns, interrupted recovery, neutral drops, repair delays, actual scatter arcs/conservation, sustained low-altitude enemy flight, ceiling limits, and projectile expiry. Mana-loop checks passed 173, real-physics checks passed 70, objective checks passed 35 headlessly / 36 rendered (including actual restart/mouse capture), and wizard flight checks passed six. A 1,200-frame arena smoke run completed without gameplay errors; the known exit-time ObjectDB warning appeared at arena shutdown.
- Castle economy pass: `dotnet build MagicCarpetRemastered.sln` passed with zero warnings/errors; Godot headless editor import passed. Mana-loop checks passed 131 assertions, real-physics checks passed 40, objective checks passed 27, and wizard flight checks passed six. A 600-frame arena smoke run completed without gameplay errors. The known exit-time ObjectDB warning appeared in objective checks and arena shutdown. `git diff --check` passed.
- User playtest confirmed the rival wizard now appears and can be defeated, and reported that the castle destruction objective works. The user accepted this implementation for the current phase; this does not imply broader campaign or AI balance signoff.
- Rival visibility fix: clean C# build (zero warnings/errors), editor import, six real-frame wizard checks, 93 mana-loop checks, and 27 headless objective checks passed. Rendered default-camera and close-up screenshots confirmed the wizard/carpet/nameplate visible in the arena. A 1,200-frame arena smoke run completed without gameplay errors; the known exit-time ObjectDB warning appeared.
- Elimination objective: `dotnet build MagicCarpetRemastered.sln` passed with zero warnings/errors; Godot headless editor import passed. Objective regressions passed 27 headless assertions and 28 rendered assertions (including mouse recapture after actual reload). Mana-loop regressions still pass all 93 assertions. A 600-frame arena smoke run completed without gameplay/shutdown errors. Headless objective checks reported the previously known exit-time ObjectDB leak warning; the rendered checks did not.
- Balloon-only collection pass: `dotnet build MagicCarpetRemastered.sln` passed with zero warnings/errors; 93 mana-loop assertions and 31 real-physics assertions passed. Checks cover all three colors ignoring player proximity/contact, unchanged power/refill, possessed mana falling rather than attracting, and actual balloon pickup/castle delivery preserving physical mana while increasing spell power. A 600-frame arena smoke run completed without gameplay errors; the previously known exit-time ObjectDB leak warning appeared.
- Mana physics pass: clean `dotnet build MagicCarpetRemastered.sln` (zero warnings/errors), Godot headless editor import, 88 passing mana-loop assertions, and 28 passing real-physics assertions. Covered ground rest/no bobbing, volume/collider scaling and shrinking, physical rotation/rolling on slopes, castle/object collision, neutral/player/enemy merges, chained merges and conservation, ownership-change contact merging, carry/release, actual homing collection and wall blocking, raised terrain, water support, heightmap hills, and crater falls.
- A 2,400-frame headless main-arena smoke run completed without gameplay or shutdown errors. `git diff --check` passed. Hands-on rolling/size/merge readability and pacing remain to be tuned.
- Balloon arrival fix: clean `dotnet build MagicCarpetRemastered.sln` (zero warnings/errors) and 88 passing headless mana-loop assertions, including four new exact-arrival/facing/idle/vertical-motion checks. A 2,400-frame headless arena smoke run completed without `LookAt` or gameplay errors; the previously known exit-time ObjectDB leak warning remains.
- Possession pass: `dotnet build MagicCarpetRemastered.sln` passed with zero warnings/errors; Godot headless editor import passed; `res://tools/ManaLoopChecks.tscn` passed 84 assertions. New checks cover airborne-to-ground transition, straight heading, hill/crater following against the live height grid, neutral/rival first hits, ignored carried/consumed/owned/off-path globes, no repeat claims, range/lifetime expiry, water skim, RMB binding, hidden terrain bindings, five-slot Tab wrap, and cast cost.
- Possession arena smoke run (`--headless --path . --quit-after 600`) completed without gameplay errors. Visual bolt readability and hands-on aiming still need playtesting.
- `dotnet build MagicCarpetRemastered.sln`: passed, zero warnings/errors.
- Godot 4.6.3 headless editor import: passed.
- Headless `res://tools/ManaLoopChecks.tscn`: passed 67 assertions, including per-step balloon conservation, accumulated homing, partial player/castle collection, no double transfers, spell spend/regen isolation, rival collection/deposit, pop-spill ownership, and exact death drops.
- Main arena headless smoke run with `--quit-after 600`: completed without gameplay exceptions. A verbose repeat identified an exit-time leak warning for `AudioStreamGeneratorPlayback`; audio cleanup remains a separate known gap.
- These checks validate transfer correctness and scene execution, not visual readability, control feel, or balanced pacing.
- HUD build passed with zero warnings/errors. Temporary headless and rendered Godot smoke checks verified initial hidden state, actual F3 input binding, repeat guard, section population, keyboard scrolling, scroll retention, and idle feedback hiding. Reviewed 1280×720 rendered screenshots for compact/debug layouts; the center stays clear with debug hidden.

### Current Gaps And Next Recommended Work

- Mana size ratios/one-third scale/animated growth/randomized fragments/faster merging/gravity-only drops are accepted by user playtest. Next judge one-at-a-time 50-ms release on land/water/slopes, including larger reserve spill duration. See `docs/PLAYTEST_MECHANICS.md` for current defaults. Profile large loose-mana populations alongside the existing terrain profiling backlog; nearby cluster candidates currently scan the loose-mana group.

- Playtest the integrated spherical loop using `docs/SPHERICAL_WORLD.md`, especially Small/XL curvature, horizon-based visibility, longer castle/balloon routes, terrain edits at seams, and globe traversal in both cameras. The user accepted the 800-radius lab feel; full spherical gameplay balance still needs hands-on acceptance.
- Profile longer sessions with many terrain edits and mana bodies. Current spherical patches retain coarse/fine colliders and scan nearby actors; deformation stamps remain a linear history. More aggressive streaming/collision budgeting and edit-history baking/indexing are follow-ups. Matches still have one designated rival; larger participant setups and networked human multiplayer require their own scope decisions.

- Playtest the new gold discovery beacons using `docs/SPELL_PROGRESSION.md`: landmark/label visibility, low/high flight collection, travel/reward pacing, and rank-II spell feel. All spells currently start available; locked unlocks, campaign persistence, and reassignable hands remain pending Phase 6 work. Upgrades survive wizard recovery, while R resets them.
- Hands-on playtest the new mechanics using `docs/PLAYTEST_MECHANICS.md`: neutral loot and ballistic spill visibility, balloon pickup pacing with many small fragments, five-second repair grace/three-HP repair rate, ten-second wizard recovery and castle-first assault pacing, and the 12-unit flight ceiling. Damage spills only mana above the reduced storage limit. Destroyed-castle rebuilding and balloon replacement remain future decisions.
- Playtest grounded mana sizes, downhill rolling/friction, cluster merging, balloon hauling of large merged spheres, and possession tolerance. The player now only possesses mana; balloons handle collection and castle delivery.
- Hands-on Godot playtest: RMB possession bolt aiming (aim down to land before distant globes), speed/range/hit tolerance, balloon-only collection pacing, cargo readability, scoop/return/dock pacing, and rival mana pressure. Horizontal/upward shots remain airborne until they meet a surface or expire; the bolt does not auto-target or steer toward globes.
- Playtest HUD legibility during flight/combat and at other window sizes. Use `F3` for diagnostics/control help and `PgUp`/`PgDn` to browse the debug panel.
- Current rival/castle elimination pass is accepted by the user. Defer deeper wizard AI (combat decisions, maneuvering, target priorities, retreat/defense) to a later pass. Mana logistics, broader balance, and presentation polish remain future work. The earlier 50% quota win condition is cancelled; tracker quota helpers remain unused resource diagnostics.
- Initial spell capacity is a prototype starting-power allowance, not physical world mana. The castle bonus now reflects retained storage and falls on damage/spillage or withdrawal; it includes initial stored reserves. All physical mana remains counted once in storage, loose/carried spheres, or enemy reserves.
- Full enemy castles can leave reserve-carrying rivals in retreat; a full-storage fallback and balloon replacement after popping remain future behavior decisions.
- Wizard respawns and surviving-castle repair are implemented pending hands-on tuning. Deeper castle sanctuary behavior, destroyed-castle rebuilding, general reassignable two-hand loadouts, and minimap/radar remain pending. RMB has fixed Possess; direct terrain shaping remains hidden per user direction.
- Investigate placeholder audio playback disposal on scene shutdown (headless arena exit warning).

## 2026-09-09

### Completed

- Condensed the user's mechanics research into `docs/ORIGINAL_MECHANICS_REFERENCE.md` as a clean system-design reference.
- Updated `docs/BUILD_ROADMAP.md` so Phase 3 through Phase 8 explicitly track possession, owned mana, balloon logistics, castle regression, equilibrium quotas, two-hand spell loadouts, spell discovery, auto-targeting, and map/radar needs.
- Linked `docs/DISCOVERY.md` to the new mechanics reference and added an original-spirit lesson in `docs/LEARNINGS.md`.
- Researched the original mana economy at a high level from the local DOSBox package inventory and public references.
- Updated the plan so original-style claimed mana and passive regeneration are the next active priority before further spell/enemy tuning.
- Scanned the current mana/castle/collector code and confirmed the prototype still uses fixed player mana, direct pickup refills, neutral well storage, and source-to-castle collector routing rather than original-style claimed mana ownership.
- Tried to inspect the provided YouTube gameplay reference, but automated transcript/frame access was blocked and `yt-dlp` is not installed in this environment.
- Installed `yt-dlp`, downloaded a low-resolution copy of the provided gameplay reference into temp storage only, sampled frames with `ffmpeg`, and documented observed HUD/mana/castle/balloon takeaways in `docs/DISCOVERY.md`.
- Began the original-style claimed mana implementation: added player `ClaimedMana`, passive regeneration from claimed mana, claimed-mana growth from pickups, and HUD display for claimed mana.
- Changed player castle deposits to add to the player's claimed mana pool, so collector deliveries now increase long-term player mana power.
- Stopped auto-banking current player mana into the neutral well because current mana now regenerates from claimed mana instead of acting as overflow cargo.
- Verified the claimed-mana pass with a temporary-output `dotnet build` path.
- Rebased the progress log into per-day sections so the 2026-09-08 implementation passes and the 2026-09-09 research/claimed-mana/doc work are recorded separately.
- Implemented a full loose-mana ownership model on `ManaPickup` with `Neutral`/`Player`/`Enemy` states, per-owner colors (gray/cyan/red), the `loose_mana` group, carry semantics for haulers, and ownership-gated homing so only player-owned orbs fly to the player.
- Converted `ManaWell` into a finite neutral landmark: it seeds a small neutral orb field on `_Ready`, holds no stored mana, and no longer infinitely regenerates or spawns pickups; the old ambient loop is removed.
- Converted `ManaCollectorSpirit` into `ManaBalloon`: an owned flying hauler that seeks its side's loose orbs (player or enemy), scoops up to `CarryCapacity`, returns to its castle, deposits (player deposits still grow claimed mana), and pops on damage to spill carried orbs back into the world as neutral.
- Added a `Possess` economy spell on the reworked quick-slot enum: `Tab` now cycles Firebolt/Burst/Shield/Dash/Guardian/Possess, and `LMB` claims aim-weighted neutral-or-enemy loose mana as player-owned at a cost of 2 mana with a reusable cooldown.
- Wired balloon damage into both `SpellProjectile` (firebolt proximity hit) and the player's `DamageEnemiesInRadius` (Arcane Burst), so enemy balloons can be popped to recapture their haul.
- Rewired enemy `ManaThief` and `EnemyWizard` to acquire and absorb loose mana orbs instead of stealing from the well: thieves prefer player-owned orbs, wizards prefer neutral, both retreat/deposit at the enemy castle, and thieves/wizards now drop enemy-owned orbs on death so the player must possess them.
- Added `WorldManaTracker`, a first-pass finite-world-mana tracker: it tallies neutral/player/enemy loose orbs, castle storage, and ecosystem transit (`ManaReserve`), exposes a `PlayerShare01` and a 50% equilibrium quota, and the HUD now shows a quota line plus an ownership legend and updated well/loadout text.
- Seeded `Game.cs` with four neutral loose-mana clusters plus a larger neutral beacon orb and one balloon per side, replacing the old ambient well + collector setup.
- Verified the full mana-ownership pass with `dotnet build MagicCarpetRemastered.sln` (clean build, 0 warnings).

### Current Prototype Controls

- `WASD`: fly horizontally
- `Space`: ascend
- `C`: descend
- Mouse: look
- Left mouse: cast selected quick spell
- `Q`: cast Arcane Burst area damage
- `E`: cast Mana Shield defense
- `F`: cast Wind Dash mobility
- `G`: cast Guardian summon
- `Tab`: cycle LMB quick spell
- Right mouse: fire terrain-following Possess bolt to claim neutral/rival loose mana
- Direct terrain shaping and `1`–`4` mode bindings are hidden for now
- `V`: toggle chase/first-person camera
- `R`: restart arena
- `Esc`: capture/release mouse
- `F3`: show/hide debug UI and controls (hidden by default)
- `PgUp` / `PgDn`: scroll the visible debug panel

### Known Gaps

- Godot runtime is tested manually by the user; CLI runtime verification is blocked because `godot` is not on `PATH`.
- Flight feel has a first tuning pass and needs runtime playtest validation.
- Terrain deformation modes are working, but still need runtime tuning and feel validation; chunk size is still a tuning tradeoff.
- Enemy steering remains simple and will need a later polish pass.
- Mana ownership is visually distinct now, but the `/loose mana/` loop is first-pass and needs runtime validation: possession aim selection, balloon scoop/return pacing, and ownership color readability all need playtest feedback.
- Collector spirits were replaced by true balloons that visibly haul owned loose mana; balloon movement, scoop radius, capacity, and pop spill behavior still need runtime tuning.
- Finite world mana and the 50% equilibrium quota are tracked and displayed, but the win/loss objective still needs runtime validation and a director-driven response to quota state.
- Spell system now has a basic data model plus firebolt, Arcane Burst, Mana Shield, Wind Dash, Guardian, terrain shaping modes, and first-pass quick-slot cycling, but still lacks unlocks, upgrades, two-hand loadout behavior, and runtime validation.
- No minimap/radar or full-world map yet; ownership and threats are not shown on a tactical display.
- Exact original timings for mana claiming, balloon hauling, castle growth, and regeneration still need higher-resolution/manual video observation, but first-pass visual references confirm balloons, large mana pearls, castle scale, minimap importance, and top-strip HUD priority.
- Phase 3 is only a first-pass mana loop; it still needs runtime tuning and playtest validation.
- Phase 4 is only a first-pass castle layer; it still needs runtime tuning, sanctuary/respawn behavior, and damage regression with mana spillage.
- Phase 5 enemy ecosystem is functionally complete for now; detailed tuning is intentionally deferred.
- Phase 6 is a first-pass spell set and needs the broader loadout/discovery systems before it can be signed off.
- No original art/audio pass beyond simple procedural meshes/materials and synthesized placeholder tones.

### Next Recommended Work

- Playtest claimed mana regeneration, possession, and balloon hauling in Godot and capture tuning feedback.
- Tune possession radius/cost/cooldown, player-owned homing collection radius, and balloon scoop/return/dock pacing around playtest feel.
- Tune enemy thief/wizard orb acquisition intervals, steal radius, absorb amounts, and retreat thresholds so resource control creates strategic pressure without trivializing mana.
- Validate finite-world-mana totals and the equilibrium quota HUD; decide how the director and win/loss respond to quota state.
- Fix the residual visual gap: balloons carrying orbs do not yet show the cargo visually, and carried-orb spill on pop still needs an ownership/position sanity check.
- Add castle sanctuary/respawn behavior and damage regression with stored-mana spillage.
- Add two active hand/button loadout bindings with fast reassignment.
- Add a minimap/radar showing mana, castles, balloons, threats, and ownership colors.
- Keep Phase 2 and Phase 1 on the playtest backlog for later signoff if further feel issues appear.
- Defer detailed Phase 5 enemy tuning and Phase 6 spell tuning until the claimed-mana loop is playtested.

## 2026-09-08

### Completed

- Initialized the repository as a Godot C# project shell.
- Added `project.godot`, `MagicCarpetRemastered.csproj`, and `MagicCarpetRemastered.sln`.
- Added project folders: `scenes/`, `scripts/`, `assets/`, `tools/`, and `docs/`.
- Added project guidance in `AGENTS.md` and startup notes in `README.md`.
- Documented original DOS package discovery in `docs/DISCOVERY.md`.
- Documented the full build roadmap in `docs/BUILD_ROADMAP.md`.
- Created the first 3D gameplay scene at `scenes/Game.tscn`.
- Added procedural scene construction in `scripts/game/Game.cs`.
- Added carpet flight controller in `scripts/player/CarpetFlightController.cs`.
- Added chase and first-person camera modes with `V` camera toggle.
- Added mouse-look, WASD movement, ascend/descend, drag, banking, and terrain clearance.
- Added primary spell projectile in `scripts/spells/SpellProjectile.cs`.
- Added mana-gated casting, cooldown, and spell cost display.
- Added projectile impact burst in `scripts/spells/SpellImpactBurst.cs`.
- Added mana pickups in `scripts/world/ManaPickup.cs`.
- Added simple chasing monster in `scripts/enemies/SimpleMonster.cs`.
- Added enemy contact damage, hit flash, death, and mana drops.
- Added HUD in `scripts/ui/Hud.cs` showing health, mana, camera mode, controls, and status text.
- Added restart input with `R`.
- Replaced the flat arena with a procedural heightmap island in `scripts/world/HeightmapArena.cs`.
- Added water plane and height-band terrain colors.
- Fixed player spawning inside terrain by placing gameplay objects relative to terrain height.
- Fixed terrain trapping by clamping flight clearance directly against the heightmap surface.
- Fixed impact burst and mana drop placement warnings by setting `GlobalPosition` after nodes enter the tree.
- Fixed terrain visibility by removing transparent water overdraw and using vertex-colored terrain.
- Added speed and terrain-clearance debug readout to the HUD.
- Added player damage invulnerability after hits, including HUD state and carpet flash feedback.
- Added a ranged monster attack with windup telegraph and enemy projectile damage.
- Improved combat VFX with configurable impact burst colors, purple enemy projectile impacts, and a larger enemy death burst.
- Added code-generated placeholder audio for flight hum, spell casting, mana pickup, and hit events.
- Tuned first-pass flight feel with higher top speed, capped acceleration/deceleration, velocity-based banking, speed-responsive chase camera distance/FOV, and slightly higher terrain clearance.
- Added `docs/LEARNINGS.md` to preserve durable implementation, playtest, and research lessons.
- Updated `AGENTS.md` to require documenting reusable lessons in the appropriate `docs/` file.
- Began Phase 2 terrain deformation by changing `HeightmapArena` from formula-only height queries to a stored vertex height grid with bilinear `HeightAt` sampling.
- Added terrain deformation APIs for craters, raising, lowering, and flattening, with full terrain mesh/collision rebuilds after edits.
- Marked terrain collision as deformable and wired firebolt impacts into terrain crater creation.
- Added right mouse crater casting for direct terrain deformation testing, including mana cost, cooldown, HUD help text, and impact feedback.
- Optimized terrain deformation edits to scan only the affected height-grid bounds instead of every vertex in the arena.
- Added HUD terrain diagnostics showing the most recent edited vertex count and terrain rebuild time.
- Changed terrain edits to update the height grid immediately while deferring and coalescing mesh/collision rebuilds to one rebuild per frame.
- Split the terrain mesh/collision into reusable chunks and now rebuild only the chunks touched by deformation edits.
- Expanded HUD terrain diagnostics to include the number of rebuilt chunks.
- Verified `dotnet build MagicCarpetRemastered.sln` after each implementation pass.
- Added terrain spell mode switching for crater, raise, lower, and flatten using `1` through `4`.
- Routed right mouse terrain casting through the selected terrain mode instead of crater-only behavior.
- Added terrain mode and control hints to the HUD.
- Set terrain casting mana cost to zero so terrain mode testing is not blocked by mana.
- Added a terrain-height fallback for RMB casting so deformation works even when physics ray hits are flaky.
- Increased default terrain edit radius, crater depth, and raise/lower height to make deformation easier to see.
- Verified `dotnet build MagicCarpetRemastered.sln` after the terrain mode implementation.
- Began Phase 3 mana economy work by adding a player mana capacity, spend helper, and excess-mana routing to a mana well.
- Added `scripts/world/ManaWell.cs` as a storage source with ambient regeneration, pickup spawning, and mana deposit/steal APIs.
- Updated `ManaPickup` to home toward the player or well and to let monsters absorb mana.
- Updated `SimpleMonster` to contest mana near the well, steal mana from storage, and visibly gain mana reserve.
- Spawned a mana well in the main game scene and surfaced well storage in the HUD.
- Verified `dotnet build MagicCarpetRemastered.sln` after the Phase 3 implementation pass.
- Began Phase 4 castle and territory work by adding `CastleKeep` with storage, health, and growth stages.
- Added `ManaCollectorSpirit` to route mana between the well and castles, creating a visible territory loop.
- Spawned player and enemy castles in the main scene and surfaced their status in the HUD.
- Verified `dotnet build MagicCarpetRemastered.sln` after the Phase 4 implementation pass.
- Began Phase 5 enemy ecosystem work by adding role-based enemy archetypes for flying swarms, ground beasts, ranged casters, castle attackers, mana thieves, and an enemy wizard.
- Added `EnemyEcosystemDirector` to control biome/intensity-based spawning and initial wave setup.
- Routed enemy projectiles into castle damage so siege units can pressure territory as well as the player.
- Expanded the HUD with ecosystem pressure and role population readout.
- Verified `dotnet build MagicCarpetRemastered.sln` after the Phase 5 implementation pass.
- Tuned the phase 5 ecosystem by adding per-role population caps, a gentler intensity ramp, and better spawn fallback logic.
- Added retreat-and-deposit behavior for mana thieves and enemy wizards so they can carry pressure back to the enemy castle.
- Verified `dotnet build MagicCarpetRemastered.sln` after the phase 5 tuning pass.
- Fixed player fireballs so they damage the new phase 5 ecosystem enemies as well as the earlier `SimpleMonster`.
- Adjusted castle-attacker selection so red siege units can actually spawn from the castle ruins zone during playtest.
- Added an explicit HUD color/role legend to make the phase 5 enemy roles easier to identify in-game.
- Verified `dotnet build MagicCarpetRemastered.sln` after the phase 5 visibility and combat routing fix.
- Fixed phase 5 enemy attack checks to use horizontal distance so ranged attacks, thief steals, wizard deposits, and castle rams can trigger reliably in the flying arena.
- Increased enemy projectile visibility and lifetime so ranged attacks are easier to notice during playtest.
- Verified `dotnet build MagicCarpetRemastered.sln` after the phase 5 interaction fix.
- Moved the player and enemy castles to opposite sides of the island so siege pressure has room to play out.
- Widened ranged enemy firing windows and projectile travel so projectiles are easier to observe in playtest.
- Closed Phase 5 as functionally complete for the current prototype and deferred detailed enemy tuning until the broader game loop is more complete.
- Began Phase 6 by adding `SpellDefinition` as a small spell data model for id, display name, kind, mana cost, cooldown, and description.
- Routed the current firebolt and terrain shaping abilities through spell definitions while preserving the existing mouse controls.
- Added HUD spell loadout text so the active LMB/RMB spell bindings are visible during playtest.
- Added `Arcane Burst`, a `Q`-cast area-damage spell that detonates at the aimed point, damages nearby enemies, and shows a larger purple burst VFX.
- Tagged the legacy `SimpleMonster` enemy group so area damage can affect both old and phase-5 enemy types.
- Verified `dotnet build MagicCarpetRemastered.sln` after adding the first Phase 6 area spell.
- Added `Mana Shield`, an `E`-cast defensive spell that spends mana, runs on its own cooldown, briefly reduces incoming damage, and shows cyan shield feedback on the carpet/HUD.
- Verified the shield spell with a temporary-output `dotnet build` because the normal Godot build DLL was locked by a running `.NET Host` process.
- Added `Wind Dash`, an `F`-cast mobility spell that spends mana, runs on its own cooldown, boosts the carpet in the aimed direction, and shows a teal movement burst.
- Verified `Wind Dash` with the same temporary-output `dotnet build` path.
- Added `Guardian`, a `G`-cast summoning spell that creates a temporary allied orb which seeks and damages nearby ecosystem enemies.
- Added `SummonedGuardian` as the first friendly summoned unit behavior.
- Verified `Guardian` with the same temporary-output `dotnet build` path.
- Added quick-slot behavior: `Tab` cycles the selected quick spell and `LMB` casts the selected spell while direct `Q`/`E`/`F`/`G` hotkeys remain available.
- Verified quick-slot behavior with the same temporary-output `dotnet build` path.

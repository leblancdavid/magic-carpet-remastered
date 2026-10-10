# Build Roadmap

## Direction

Build a modern Godot C# spiritual remaster inspired by Magic Carpet. The project should capture the fantasy of fast carpet flight, spell duels, mana control, terrain deformation, monsters, and castle conflict without copying original proprietary game data or assets.

Use `docs/ORIGINAL_MECHANICS_REFERENCE.md` as the condensed system-design reference for original-style mechanics and priorities.

Current victory direction (user update, 2026-10-10): destroy the rival player's castle AND defeat its wizard, in either order. Mana possession/storage supports combat power and logistics; it is not a victory condition. This supersedes the earlier equilibrium-quota objective plan.

Playtest mechanics update (2026-10-10): wizards respawn after ten seconds while their respective castle survives; a rival respawn clears its previous defeat. Killing the wizard first can win only if the castle falls before it returns. Player castle destruction causes defeat. Surviving castles slowly repair after a quiet delay, neutral creatures carry/drop intrinsic mana, destruction spills fragment into ballistic small spheres, and shared flight clearance is capped at 12 units above terrain/water. See `docs/PLAYTEST_MECHANICS.md` for current defaults and validation steps.

## Development Tracks

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
- intrinsic neutral-creature reserves and outward many-small-sphere destruction bursts; mana is counted from spawn and conserved through transfers/deaths, while recurring neutral spawns introduce new supply
- grounded, amount-scaled physics spheres with downhill rolling, solid-object collision, and same-ownership contact merging (implemented; runtime tuning pending)
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

Status: presentation remains placeholder-based. A compact edge HUD now shows gameplay essentials, with diagnostics and control help in a formatted `F3` debug panel hidden by default. Rendered layout and toggle/scroll behavior have been checked; further readability playtesting, tactical map, original assets/audio, and polish remain.

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

1. Flight prototype: tuned movement, camera modes, spell firing, one enemy, mana pickups.
2. Terrain prototype: generated heightmap arena and collision.
3. Mana loop: collect, spend, store, and contest mana.
4. Castle loop: build, defend, attack, and route mana.
5. Enemy wizard duel: rival AI with flight, spells, and mana goals.
6. Vertical slice: one complete level with win/loss, HUD, audio, and polish.
7. Campaign alpha: multiple levels, several spells, progression, and biome variety.

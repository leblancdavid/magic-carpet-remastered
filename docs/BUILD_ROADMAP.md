# Build Roadmap

## Direction

Build a modern Godot C# spiritual remaster inspired by Magic Carpet. The project should capture the fantasy of fast carpet flight, spell duels, mana control, terrain deformation, monsters, and castle conflict without copying original proprietary game data or assets.

Use `docs/ORIGINAL_MECHANICS_REFERENCE.md` as the condensed system-design reference for original-style mechanics and priorities.

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

- runtime playtest terrain modes in Godot
- tune crater radius/depth, raise/lower height, flatten behavior, mana cost, and cooldown
- tune chunk size if terrain edits are too expensive or too slow

## Phase 3: Mana Economy

Goal: make mana control the strategic loop.

Status: implementation substantially advanced with the original-style direction in place. Mana pickups now carry explicit `Neutral`/`Player`/`Enemy` ownership with distinct colors; claimed/possessed mana grows the player's long-term pool; the well is a finite neutral landmark that seeds a claimable orb field instead of regenerating; a `Possess` economy spell (sixth `Tab` slot) converts aim-weighted neutral/rival loose mana into player-owned orbs; and player-owned orbs home naturally to the player. `ManaBalloon` replaces the old collector spirit and visibly hauls owned loose mana back to castle storage, popping on damage to spill neutral orbs; enemy thieves/wizards acquire and absorb loose orbs and drop rival-owned orbs on death. A world-mana tracker sums loose + castle + transit mana and exposes a 50% equilibrium quota on the HUD. Remaining work is runtime playtest tuning and gating the win/loss objective on quota state.

Core work:

- mana orb drops from enemies
- mana wells or ambient sources
- pickup attraction and collection rules
- spell costs and storage limits
- mana routing to player castle/storage
- enemy contesting behavior
- claimed/possessed mana that increases the player's long-term mana pool
- passive mana regeneration derived from claimed mana and castle/storage state

Deliverable: fights happen because mana is valuable and contested.

Remaining Phase 3 work before signoff:

- runtime playtest possession aim selection, cost/cooldown, and ownership colors in Godot
- runtime playtest balloon scoop/return/dock pacing and pop-spill behavior
- tune enemy thief/wizard orb acquisition, absorb amounts, and retreat thresholds around the claimed-mana loop
- validate world-mana totals and equilibrium-quota progress, then gate the win/loss objective on quota state
- decide director response to quota state (spawn pressure shifts as the player approaches equilibrium)
- defer detailed capacity, regen, pickup rate, and contest-drain tuning until the claimed-mana loop is playtested
- drive the well forward from its finite neutral landmark role (visual landmark + initial field) as balloon/possession behavior matures

## Phase 4: Castle And Territory

Goal: add base building, storage, and strategic conflict.

Status: implementation in progress. The prototype now has a player castle, enemy castle, castle health/storage/growth, and `ManaBalloon` haulers that visibly carry owned loose mana between the field and castles. Remaining work is runtime tuning and signoff.

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
- decide whether castle damage and raiding should be handled by existing combat or a dedicated assault objective
- add clearer territorial feedback if the current castle silhouettes are too subtle
- add castle sanctuary/respawn behavior after the mana loop is stable
- add castle damage regression and stored-mana spillage once castle combat is readable

## Phase 5: Enemy Ecosystem

Goal: create systemic pressure rather than only scripted waves.

Status: functionally complete for the current prototype. The prototype now has role-based enemy spawning with swarms, ground beasts, ranged casters, castle attackers, mana thieves, and an enemy wizard archetype. The director caps each role; mana thief and wizard roles have retreat/deposit loops; player fireballs damage ecosystem enemies; enemy attack checks use horizontal distance so arena altitude does not suppress behavior; and the castles are far enough apart for readable siege pressure. Fine tuning is deferred until the game loop is more complete.

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
- confirm red castle attackers and purple wizards appear at readable pressure levels
- confirm ranged enemy shots and castle rams are now obvious and reliable during playtest

## Phase 6: Spell System

Goal: make spells the player's primary expression and progression axis.

Status: first-pass spell variety is implemented. The current firebolt and terrain shaping abilities now have a small spell definition model and visible HUD loadout text. `Arcane Burst` adds area damage on `Q`, `Mana Shield` adds a defensive spell on `E`, `Wind Dash` adds mobility utility on `F`, and `Guardian` adds a temporary summoned ally on `G`. `Possess` is the first `Economy`-kind spell, cycling as the sixth `Tab` quick slot to claim loose neutral/rival mana as player-owned. `Tab` now cycles the active quick spell for `LMB`, while direct spell hotkeys remain available. The original-style claimed/regenerating mana base now exists, so further spell tuning, unlock/upgrade rules, and runtime validation can resume once the claimed-mana loop is playtested.

Core work:

- spell data model
- quick slots and loadout
- two active hand/button bindings with fast reassignment
- mana costs, cooldowns, upgrades, and unlocks
- spell pickups or unlock discoveries tied to exploration
- light auto-targeting/homing rules for appropriate offensive spells
- projectile damage spell
- area damage spell
- terrain deformation spell
- summoning spell
- shield or defense spell
- mobility or utility spell
- economy/trap spells such as possession, mana stealing, and fake mana

Deliverable: a small spellbook with meaningful tactical tradeoffs.

## Phase 7: Campaign Structure

Goal: turn arenas into a full game loop.

Core work:

- island/realm level model
- objective variants
- finite world mana and equilibrium quota win condition
- biome themes
- escalating enemy wizard setups
- persistent unlocks and spell progression
- win/loss flow

Deliverable: several levels connected by progression and escalating challenge.

## Phase 8: Presentation And Content

Goal: establish the remastered identity.

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

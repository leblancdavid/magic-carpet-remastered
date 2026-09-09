# Original Mechanics Reference

Condensed design reference for a legally safe spiritual remaster. This document describes observed and publicly documented mechanics at the system level only; do not copy proprietary code, data, art, audio, level files, or extracted assets.

## Core Game Shape

Magic Carpet is best understood as first-person aerial spell combat layered over a real-time resource-control economy.

The primary loop:

1. Explore the world from a flying carpet.
2. Find spells, monsters, mana, structures, and rival wizards.
3. Kill creatures or assault infrastructure to release mana.
4. Possess neutral or contested mana so it becomes yours.
5. Use a castle and balloons to haul owned mana into storage.
6. Grow the castle to increase capacity, defense, recovery, and collection throughput.
7. Contest rival wizards over a finite global mana pool.
8. Store enough mana to meet the world's equilibrium quota and advance.

The important design lesson is that combat serves the economy. The player fights because mana is finite, claimed, stealable, transportable, and required for victory.

## Flight And Combat Feel

The player is always mounted on a flying carpet, not walking through an FPS level.

Key feel targets:

- full 3D movement with forward/backward thrust, turning, ascending, descending, hovering, skimming, and high-altitude travel
- acceleration, drag, and momentum that make piloting part of the skill ceiling
- forgiving terrain and obstacle clearance; the carpet should usually rise over terrain rather than crash
- altitude as a tactical variable for evasion, downward attacks, castle approaches, ocean crossings, and terrain traversal
- movement magic that changes actual carpet motion, including acceleration, escape, pursuit, and rapid repositioning

Combat should be less about pixel-perfect shooter aim and more about piloting, spell choice, altitude, and target priority. Offensive spells may use light auto-targeting or homing when a valid hostile target is near the screen center.

## Mana Economy

Mana has several roles at once:

- casting energy and long-term power source
- finite level resource
- contested ownership token
- castle storage cargo
- visible territorial/economic state
- victory condition resource

Mana should exist as world objects with ownership state:

- neutral mana is unclaimed and should not instantly become player power
- player-owned mana uses the player's color
- rival-owned mana uses that wizard's color
- fake or trapped mana can exploit the same visual language

Harvesting is a two-step process: create or find mana, then claim it. Claimed mana should still need to be transported to storage before it contributes fully to level progress.

## Possession

Possession is a defining economy spell, not a minor pickup action.

Primary uses:

- claim neutral loose mana
- contest or steal mana associated with rivals
- take control of certain structures or buildings
- claim value from defeated wizard remains

Design implication: possession should be a tactical spell competing for an active hand/loadout slot. Harvesting under pressure should require player attention and risk, especially when rival wizards or balloons are nearby.

## Castles And Balloons

The castle is the player's base, storage, logistics center, defensive anchor, recovery zone, and respawn/survival anchor.

Castle systems to preserve:

- placement matters because it changes terrain access, defense, collection distance, and exposure
- repeated castle investment grows through multiple stages, with roughly seven original-style development tiers as a useful reference target
- growth increases storage capacity, defensive strength, and collection throughput
- larger castles can support multiple balloons, with up to three as a reference target
- being near or inside the castle should provide strong recovery/safety benefits
- dying with a viable castle should allow recovery; dying before establishing a castle should be much harsher

Balloons are the worker/logistics layer. They should visibly travel from castle to owned mana, collect it, and return it to storage. Because transport is physical, distance and interception matter. Balloons should be valid strategic targets for disrupting an opponent's economy.

Castle damage should have economic consequences. A heavily damaged castle can regress to an earlier stage and spill excess stored mana back into the world, turning base assaults into resource races rather than simple structure deletion.

## Rival Wizards

Rival wizards should participate in the same economy as the player where practical.

Rival behavior priorities:

- fly through the world
- cast offensive, defensive, movement, and economy spells
- hunt creatures for mana
- possess loose mana
- build and grow castles
- launch balloons or collector equivalents
- attack the player's wizard, balloons, castle, and claimed resources
- retreat to recover or deposit value

The finite mana pool should naturally force conflict. If a rival controls too much of the world's mana, the player must steal, kill, intercept, or destroy infrastructure rather than farming endlessly.

## Monsters And Ecology

Monsters are not just blockers. They are threats, mana generators, tactical distractions, and world inhabitants.

Useful creature roles:

- flying attackers that pressure aerial combat
- ground beasts that make low-altitude travel dangerous
- ranged casters or spitters that force movement
- large creatures that release high-value mana
- castle attackers that pressure infrastructure
- opportunists that complicate player/rival engagements

Monster death should create mana pearls of varying values, making hunting a deliberate economic action.

## Terrain And Environment

Terrain manipulation is both weapon and strategy.

System goals:

- raise, lower, crater, flatten, split, or otherwise reshape terrain
- make terrain edits affect navigation, line of sight, castle placement, and combat
- support high-impact late spells such as earthquake and volcano as area weapons and terrain events
- allow environmental propagation where feasible, such as fire igniting trees or scenery

Terrain should not be cosmetic. It should change the battlefield, threaten infrastructure, and create emergent outcomes when combined with mana, castles, and rival AI.

## Spell System

The original-style spellbook is broad because nearly every interaction is expressed as magic.

Spell categories to support over time:

- direct offense: fireballs, lightning, stronger attacks
- area attack: bursts, storms, meteors
- terrain: crater, raise/lower, earthquake, volcano
- economy: possession, mana stealing, fake mana
- construction: castle build/grow/repair
- defense: shield, rebound, protection
- recovery: healing
- movement: acceleration, teleport or rapid repositioning
- summoning: autonomous attackers such as skeleton armies
- reconnaissance: reveal information, map/radar enhancement
- traps: Fool's Mana-style bait or punishment

Loadout constraint matters. The player can know many spells, but should have two active hand/button bindings at once with fast reassignment. This keeps combat decisions immediate: what two spells do I need right now?

Spell acquisition should be part of exploration. Spells can exist as world pickups, urn rewards, hidden reveals, or level-progress unlocks. Discovery should create a loop of exploration leading to new tactical capability.

## UI And Readability

The UI must support both first-person action and strategic resource awareness.

Important elements:

- compact top-band status presentation for health, current mana, owned/claimed mana, castle storage, and active spells
- All-Seeing Eye/minimap or radar showing nearby mana, castles, enemies, balloons, and ownership colors
- full-world map for strategic planning across large arenas
- readable ownership colors for mana, flags, structures, and rival assets
- large, visible mana pearls and collector balloons that can be understood while moving quickly

The minimap and ownership colors are not optional polish. They are how the player understands the economy during high-speed combat.

## Progression And Victory

Within a world, the player rebuilds power by claiming mana, growing a castle, and contesting rivals. Across worlds, the player should gain access to more spells, harder monsters, stronger rival wizards, and more complex terrain/economy setups.

Primary victory reference: each world has finite mana and an equilibrium quota. Completing a level should usually mean storing the required share of total mana, not simply killing every enemy.

This creates a near zero-sum economy. If rivals claim too much, the player must fight the rival economy directly.

## Multiplayer Reference

Original multiplayer reinforces the core design: multiple wizards compete in the same world over kills, mana power, castles, and resource control. This project does not need multiplayer early, but the single-player systems should avoid assumptions that prevent multiple wizard economies from coexisting later.

## Prototype Priority Order

For this project, the highest-value mechanics to encode next are:

1. Loose mana with neutral/player/enemy ownership state.
2. Possession as the way to claim mana.
3. Balloon/collector units that haul owned loose mana to castles.
4. Castle storage capacity, growth stages, and equilibrium quota progress.
5. Rival wizard economy behavior that claims, hauls, stores, steals, retreats, and attacks infrastructure.
6. Two active spell hands with fast reassignment.
7. Minimap/radar for mana, enemies, castles, balloons, and ownership.
8. Castle damage regression and mana spillage.
9. Spell pickups/unlocks and broader spell categories.
10. Larger terrain/environment effects such as earthquake, volcano, fire propagation, and structure damage.

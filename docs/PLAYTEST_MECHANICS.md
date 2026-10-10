# Playtest Mechanics

Current defaults for the playable arena (2026-10-10).

## Loot And Burst Spillage

- Swarms, ground beasts, ranged casters, and castle attackers carry an intrinsic neutral mana value, currently 12 each. Killing one releases that reserve as neutral globes. Legacy monsters retain their configured starting reserve. Thieves and rival wizards release only the mana they actually acquired, with rival ownership.
- Enemy deaths, castle spills, and balloon destruction share a physical scatter burst. Normally each fragment contains 1–2 mana; count grows with total amount. The burst is capped at 128 bodies for unusually large reserves, distributing every integer remainder without losing or creating mana.
- Fragments launch in varied horizontal directions at 6–9.9 units/second, with 5–7 units/second upward velocity. They preserve airborne release height, fall naturally, and ignore same-owner merging for 0.8 seconds so the burst spreads before recombining.
- Castle fragments start beyond the keep collider. Balloon pops debit/consume carried globes and fragment their total into new neutral spheres exactly once. Ordinary partial-delivery overflow still releases its remaining cargo intact.
- RMB possession and balloon-only collection remain the recovery path.
- The tracker distinguishes neutral loose mana from mana still inside neutral creatures. Recurring neutral spawns introduce their configured reserve into the world; conservation applies to subsequent transfers/deaths, not across resource-bearing spawns. Wizard respawns do not introduce physical mana.

## Castle Auto-Repair

- After **5 seconds without damage**, surviving castles repair **3 HP/second**, up to their maximum health. Every new hit resets the delay and fractional repair progress.
- Repair reopens storage capacity. It does not recreate spilled mana, automatically reclaim globes, or increase spell power without retained storage.
- Destroyed castles remain ruins and do not repair themselves.
- F3 shows health, effective/intact storage limits, growth stage, and repair state.

## Wizard Respawns And Objectives

- A dead player or rival wizard returns after **10 seconds** beside its respective surviving castle. Simulation continues during the countdown; pausing gameplay pauses respawn time.
- The player cannot move, cast, or collide while dead. Respawn restores 100 health, full current spell mana, and 2 seconds of damage protection.
- Rival replacement is managed by the level objective, with one designated rival at a time. Its death record is cleared when it respawns, and the replacement is tracked through subsequent deaths.
- Destroying a castle prevents its wizard's future respawns. Destroying the player castle ends the prototype in defeat, including during a player respawn countdown.
- Victory still requires a destroyed rival castle and a defeated rival wizard. Killing the wizard first works if the castle falls before the wizard returns; otherwise kill the respawned wizard after destroying its castle.
- The HUD shows both respawn countdowns. R remains a manual full-arena restart.

## Flight Ceiling

- `HeightmapArena.MaxFlightClearance` defaults to **12 units above terrain or water**, whichever is higher. The ceiling follows hills and deformation instead of imposing a fixed world-space height that can bury actors.
- The player, ecosystem enemies, legacy monsters, guardians, balloons, and loose physical mana use the same ceiling. Projectiles are exempt: fireballs, possession bolts, and enemy shots continue along their launch direction even at or above the flight ceiling. Their normal hit/range/lifetime rules still apply; possession starts ground skimming only after reaching terrain/water.
- Swarms and ranged casters now use separate horizontal steering and bounded altitude correction, removing the tiny-upward-vector normalization that caused perpetual ascent.
- The ceiling clamps upward velocity as well as position for moving bodies. Ground clearance takes priority if a very large sphere requires more space.

## Suggested Hands-On Pass

1. Kill a neutral creature and watch the small globes burst outward, fall, and settle. Possess a few and follow your balloon's collection.
2. Attack the rival castle enough to spill its reserve. Stop attacking for five seconds and watch its health recover. Claim the spill while it repairs.
3. Kill the rival wizard while its castle is intact. Confirm the countdown and its return at that castle after ten seconds.
4. Allow your wizard to die with your castle intact. Confirm automatic recovery without resetting the arena or mana economy.
5. Hold Space and try an upward Wind Dash. Confirm the lower flight ceiling, and check that swarms/casters remain accessible.
6. Destroy the rival castle and then its wizard. Confirm the final result still pauses gameplay and R restarts.

Automated coverage: `tools/PlaytestMechanicsChecks.tscn`, `tools/LevelObjectiveChecks.tscn`, and the existing mana/physics/wizard checks. These verify behavior and conservation; burst readability, hauling speed with many small spheres, and combat/repair balance still need hands-on feedback.

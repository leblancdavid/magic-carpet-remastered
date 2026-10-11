# Playtest Mechanics

Current defaults for the playable arena (2026-10-10).

## Loot And Burst Spillage

- Swarms, ground beasts, ranged casters, and castle attackers carry an intrinsic neutral mana value, currently 12 each. Killing one releases that reserve as neutral globes. Legacy monsters retain their configured starting reserve. Thieves and rival wizards release only the mana they actually acquired, with rival ownership.
- Enemy deaths, castle spills, and balloon destruction share a physical scatter burst. Fragment count remains `ceil(total / 2)`, capped at 128 bodies. Random integer cuts partition the reserve into positive, varied amounts that sum exactly to the original total. Equal partitions are adjusted by transferring one mana between two fragments when variation is possible. One/two-mana drops necessarily produce one ball; larger drops mix small and larger fragments.
- Fragments start with **zero launch velocity** and fall straight down under gravity (radially inward on the globe). Compact golden-angle placement uses only sphere radius plus small packing offsets, expanding just enough to avoid overlaps and solver-driven explosions. Airborne release height is preserved; castle spills retain their explicit six-unit minimum distance outside the keep collider. Natural downhill rolling/ground collisions remain. The initial 0.8-second merge grace remains, followed by the supported-contact rules below.
- Drops release **one ball every 0.05 seconds (50 ms)**, with the first ball immediate. A normal 12-mana/six-ball creature drop completes release in 0.25 seconds. Waiting fragments are hidden, frozen, noncolliding, and unavailable to possession/collection/merging; their reserved mana is included in world totals immediately. Release runs on pause-aware physics ticks, independently of the dead source. Each ball starts its own merge grace on release. If an earlier ball rolls/grows into a reserved drop spot, the next ball is lifted above it before release to prevent collision-driven scatter.
- Castle fragments start beyond the keep collider. Balloon pops debit/consume carried globes and fragment their total into new neutral spheres exactly once. Ordinary partial-delivery overflow still releases its remaining cargo intact.
- RMB possession and balloon-only collection remain the recovery path.
- The tracker distinguishes neutral loose mana from mana still inside neutral creatures. Recurring neutral spawns introduce their configured reserve into the world; conservation applies to subsequent transfers/deaths, not across resource-bearing spawns. Wizard respawns do not introduce physical mana.

## Mana Size, Magnetism, And Merging

- Target sphere radius is **`(0.35 / 3) * pow(amount, 0.65)`**: 1 mana = 0.117, 2 = 0.183, 10 = 0.521, 80 = 2.014. All radii/diameters are one-third the previously accepted size, with the same dramatic ratios: eight times the mana gives about **3.86 times the radius**. Mesh and collider scale together; physical mass still follows mana amount.
- Nearby **same-owner** grounded spheres attract across surface gaps of up to **6 units**. Acceleration is capped at **3 units/second²**, falling to 25% at the edge of range; added speed toward the neighbor is limited to **1.5 units/second**. Each ball selects its nearest eligible neighbor so opposing pulls do not cancel in dense fields. Attraction is tangent to the surface, preserves gravity/solid collisions, and works while rolling.
- Merging requires **1.25 continuous seconds** touching while both spheres are supported and their **relative linear speed is at most 0.8 units/second**. Shared rolling and spin do not reset progress. A **0.12-unit contact skin** and terrain-height/support-ray fallback tolerate solver contact jitter. Separation, fast relative impacts/falling, carry, or mismatched ownership resets progress; a growing survivor starts fresh timers. Airborne scatter retains its initial grace period.
- Merges conserve exact mana, ownership, and amount-weighted linear momentum. Different ownership colors do not attract or combine; player proximity still has no attraction/collection effect.
- On merge, the survivor commits the combined mana immediately but grows from its current radius to its new target over **0.6 seconds**, using smooth easing. The mesh/collider resize together and the center lifts gradually to retain ground contact. Further merges wait until growth finishes, then begin a fresh contact delay. Growth continues while hauled, pauses with gameplay, and direct amount changes/partial transfers immediately resize to the remaining value.

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

1. Kill a neutral creature and watch differently sized small fragments fall out one at a time at 50-ms intervals, straight down in a compact cluster beneath the death location. Watch same-color spheres visibly pull together and combine after 1.25 seconds of sustained contact, including when rolling together; the survivor should smoothly grow over 0.6 seconds rather than snap. Compare the smaller overall scale and growing reserves against fresh fragments, then possess a few and follow your balloon's collection.
2. Attack the rival castle enough to spill its reserve. Stop attacking for five seconds and watch its health recover. Claim the spill while it repairs.
3. Kill the rival wizard while its castle is intact. Confirm the countdown and its return at that castle after ten seconds.
4. Allow your wizard to die with your castle intact. Confirm automatic recovery without resetting the arena or mana economy.
5. Hold Space and try an upward Wind Dash. Confirm the lower flight ceiling, and check that swarms/casters remain accessible.
6. Destroy the rival castle and then its wizard. Confirm the final result still pauses gameplay and R restarts.

Automated coverage: `tools/PlaytestMechanicsChecks.tscn`, `tools/LevelObjectiveChecks.tscn`, and the existing mana/physics/wizard checks. These verify behavior and conservation; burst readability, hauling speed with many small spheres, and combat/repair balance still need hands-on feedback.

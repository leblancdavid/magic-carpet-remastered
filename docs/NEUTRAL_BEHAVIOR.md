# Territorial Neutrals And Occasional Raids

## Status

Pre-playtest phase 2 implemented, 2026-10-11. Ordinary enemies occupy stable habitats and fight locally. Phase 3 now supplies a shared-mechanics faction ship (`RIVAL_AI.md`); local targeting treats both faction ships as pilots. Ranges, population and raid pacing remain provisional pending hands-on play.

## Local aggression

`NeutralTerritory` owns patrol, alert, pursuit and committed return-home states; the enemy body retains role attacks, damage, visuals and actual movement.

| Setting | Globe | Flat reference |
| --- | ---: | ---: |
| Proximity aggro (`AwarenessRadius`) | 25 | 12 |
| Retaliation after damage | 65 | 30 |
| Escape/disengagement distance | 85 | 40 |
| Habitat leash | 120 | 50 |
| Patrol radius | 12 | 6 |

**Compact-world update (2026-10-12):** the Globe column above is retained for diagnostic radii above 400. Live Small/Medium/Large/XL use proximity **16/16/24/25**, retaliation **30/30/45/55**, disengagement **40/40/60/70**, leash **60/60/90/100**, patrol **6/8/12/12 m**. Explicit non-default exported values are preserved. Grouped populations are now implemented from playtest feedback.

- Distances use spherical surface arcs, or flat X/Z distance. Both living faction pilots are eligible; proximity chooses the nearest, and damage explicitly identifies the attacker. Targets and the mob must remain inside the habitat leash.
- Plasma Bolt, Pulse Burst, Guardian Drone and rival projectiles provide attacker identity. Guardian hits identify its pilot. An unknown/disposed attacker or a hit outside retaliation range causes a **three-second local alert/search**, not global tracking. Existing valid pursuit is not cancelled by unrelated distant hits.
- Escaping, dying, being freed or crossing either side of the habitat leash ends pursuit. Returning ignores new proximity/retaliation until within four surface units of home, then waits **three seconds** before reacquisition. Returning mobs do not heal for free and can still be damaged/killed.
- Ordinary swarms, beasts, casters, siegers and scavengers do not choose bases as global destinations. Patrol moves at quarter speed; returning/chasing uses normal role speed. Home and steering follow live terrain/water and radial orientation.
- Attacks require a live aggro target. Contact also checks relative height; beasts cannot hit pilots far overhead merely because X/Z overlaps. Swarms descend to low-flying targets and break away after contact, rather than hovering too high or backing off before striking.
- Casters check actual line of sight on flat and spherical worlds. Windup advances once per tick and holds its chosen target; escape/death/occlusion cancels the queued shot. Neutral shots damage either pilot, do not hurt other neutrals, and do not damage bases as collateral.
- Navigation hover details show local state. The existing F3 role guide and director counts distinguish local scavengers, raiders and the rival.

## Population and safe placement

- Globe populations use five authored landmarks plus eight economic-route habitats; Large/XL also have two outer extractor-route swarm habitats. Corresponding faction routes are interleaved when seeding the population budget. Flat reference uses its site habitats/fixed-corner fallback. No spawn is relative to a travelling pilot.
- Initial/maximum ordinary populations are **16/24/32/40** on Small/Medium/Large/XL, subject to bounded placement. Opening swarms start with three, beasts with two, casters/scavengers with one; further rounds enlarge groups. Per-habitat caps are **five swarms / three beasts / two casters / two scavengers**. Global swarm cap is half the population budget; each other role is capped at one third. Explicit exported overrides survive size substitution. Aggression remains individual, not automatic whole-pack aggro.
- Each habitat remembers its seeded population target, so another habitat cannot consume its replacement slot. Death starts a **20-second recovery delay**, then replacements arrive individually every **five seconds**. Pause freezes both population and raid clocks; live interfaces do not. A blocked placement retries safely rather than allocating an actor.
- Ordinary spawning cannot create rival ships or base raiders. The designated rival is created/respawned only by the match/objective loop.
- Every placement gets at most **64 attempts**. It excludes **30/60/80/80 m around every compact globe base** (30 flat), avoids sharp support transitions, other mobs and scenery, and stays inside the flat arena. Failure allocates nothing and never uses an unsafe nearby fallback.
- Mobs start in a **14–28-unit habitat ring** (8–14 flat); the stable habitat centre is their home. Long-lived populations remain bounded rather than following the player across the planet.

## Neutral scavengers

The historical `AntimatterThief` role is now displayed as Scavenger. It searches locally within the smaller of **42 units or half its habitat leash**, preferring the nearest faction-owned loose antimatter over neutral globes. Both factions can be robbed; matter/cargo is unaffected. It stops scavenging after reaching 12 carried antimatter (the last whole globe can exceed that amount), and during alert, pursuit or return.

Stolen antimatter stays in the neutral's reserve, not a faction base. World accounting classifies it as neutral-creature supply; death drops the exact remainder as neutral salvage. It still starts with no minted reserve. Other neutral roles retain their intrinsic 12-antimatter drop. Live faction ships use Claim and collector hauling without a physical ship reserve; legacy rival-archetype collection/deposit remains only in diagnostic fixtures.

## Separately paced raids

- A **180-second opening grace** precedes the first raid. Successful launches reset a **180-second interval**; at most **one raider** is alive. No intensity ramp changes these clocks. Failed/capped attempts retry after ten seconds. Pause stops timers; live B/M/H panels do not.
- Successful raids alternate between living faction bases, starting with the player. If one is absent/destroyed, only a surviving target is eligible. A raid never changes its assigned base to the other faction midway.
- A red, labelled **34-HP** sieger approaches from a size-aware baseline **36/70/100/100 m**, searching between baseline + 1 and baseline + 40 m (46–53 flat), subject to the same all-base exclusions/support/spacing checks. It can locally fight nearby pilots, using a broader approach leash of spawn distance + 80 units around its assigned base.
- Base attacks deal **4 HP every 3 seconds**, with a **90-second lifetime**. An intact 140-HP tier-one base survives even an immediate, unopposed default raid; actual approach travel further reduces attack time. Other simultaneous damage and future tuning are separate balance considerations.
- Kill, target loss or expiry clears the raider and releases its exact reserve as neutral salvage. An ordinary sieger without an explicit raid assignment cannot ram a base.
- An **eight-second inbound notice** uses the existing HUD feedback panel. The world label identifies its target; local map markers are orange-red with raid details. F3 shows counts, launch total and time to the next raid check.

## Verification

`tools/NeutralBehaviorChecks.tscn` covers flat, globe underside and face-seam local behavior; both pilots; proximity/retaliation boundaries; dead/freed attackers; leash, return and grace; real pursuit/return/swarm-contact movement; melee/ranged damage to rivals; line-of-sight/windup cancellation; projectile/area/guardian attribution; three-ownership scavenging/conservation; all-size safe placement/habitat/role caps; pause/grace/interval/cap/alternation; actual raid approach, attack cooldown, collateral rejection, expiry/accounting, intact-base survival and blocked placement with no fallback.

Run after building/importing:

```powershell
& $godot --headless --path . 'res://tools/NeutralBehaviorChecks.tscn'
& $godot --path . 'res://tools/NeutralBehaviorChecks.tscn'
```

Rendered verification covers Medium live placement and saves `NeutralRaidGameplay.png` to the approved temporary directory. The harness reorients scripted teleports and refreshes the camera/terrain before taking captures. Existing economy, equipment, exploration, layout, navigation, objectives and flight/clearance checks remain part of regression verification.

Next: meaningful balance playtesting; shared-mechanics rival and compact icon management/four-slot HUD are complete for their first pass. Later work includes richer patrol/search animations, ground-route obstacle avoidance, more habitat content and raid tuning.

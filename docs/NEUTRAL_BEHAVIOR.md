# Territorial Neutrals And Occasional Raids

## Status

Territorial behavior implemented 2026-10-11; globe-wide random habitats and ownership-aware guard recovery implemented 2026-10-12. Ordinary enemies fight locally; both factions share ship mechanics. See `PLAYTEST_SIMPLIFICATION.md` for current density, rewards, UI and economy. Tuning remains provisional.

## Local aggression

`NeutralTerritory` owns patrol, alert, pursuit and committed return-home states; the enemy body retains role attacks, damage, visuals and actual movement.

| Setting | Globe | Flat reference |
| --- | ---: | ---: |
| Proximity aggro (`AwarenessRadius`) | 25 | 12 |
| Retaliation after damage | 65 | 30 |
| Escape/disengagement distance | 85 | 40 |
| Habitat leash | 120 | 50 |
| Patrol radius | 12 | 6 |

**Playtest follow-up (2026-10-13):** the Globe column above is retained for diagnostic radii above 500. Live 200/300/400/500 m Small/Medium/Large/XL use proximity **30/35/40/45**, retaliation **65/65/80/100**, disengagement **85/85/100/125**, leash **100/100/120/150**, wander footprints **18/27/36/36 m**. Explicit non-default exported values are preserved.

Damage, including a lethal hit, rallies living members of the same nonempty habitat ID against the responsible pilot. Each member independently checks retaliation distance, habitat leash, return state and grace; no recursive propagation or cross-habitat alert. Human/rival and Guardian-attributed attacks use the same path. Fleeing still commits the pack to return; no free healing or global pursuit.

- Distances use spherical surface arcs, or flat X/Z distance. Both living faction pilots are eligible; proximity chooses the nearest, and damage explicitly identifies the attacker. Targets and the mob must remain inside the habitat leash.
- Plasma Bolt, Pulse Burst, Guardian Drone and rival projectiles provide attacker identity. Guardian hits identify its pilot. An unknown/disposed attacker or a hit outside retaliation range causes a **three-second local alert/search**, not global tracking. Existing valid pursuit is not cancelled by unrelated distant hits.
- Escaping, dying, being freed or crossing either side of the habitat leash ends pursuit. Returning ignores new proximity/retaliation until within four surface units of home, then waits **three seconds** before reacquisition. Returning mobs do not heal for free and can still be damaged/killed.
- Idle patrol uses independently seeded random destinations, pauses and variable speeds: swarms 40–70% role speed, others 25–50%. Destinations last 7–16s; arrival pauses swarms 0.2–1.5s and others 1–4s. Progress checks repick stalled destinations; dry-home ground roles reject water and ground roles reject rough initial approaches. Water-supported station habitats permit water wandering. This is local wandering, not full obstacle navigation. Returning/chasing uses normal role speed and existing combat rules.
- Cache pilot group membership for staggered 0.4–0.6s intervals, while checking current position/liveness/proximity every physics tick. Newly created pilots enter the roster on refresh; damage attribution remains immediate. This avoids per-idle-mob per-tick native array allocations without caching stale positions.
- Attacks require a live aggro target. Contact also checks relative height; beasts cannot hit pilots far overhead merely because X/Z overlaps. Swarms descend to low-flying targets and break away after contact, rather than hovering too high or backing off before striking.
- Casters check actual line of sight on flat and spherical worlds. Windup advances once per tick and holds its chosen target; escape/death/occlusion cancels the queued shot. Neutral shots damage either pilot, do not hurt other neutrals, and do not damage bases as collateral.
- Navigation hover details show local state. The existing F3 role guide and director counts distinguish local scavengers, raiders and the rival.

## Population and safe placement

- Globe populations combine five landmarks, eight site guard habitats and many **seeded random wild habitats** across twelve regions. Spacing varies **22–65 m**; later habitats have a 65% chance of clustering around an existing same-region wild habitat. Land roles prefer dry locations. Flat reference retains site/corner habitats. Spawn locations do not follow a travelling pilot.
- Initial/maximum ordinary targets are **96/180/300/450** on Small/Medium/Large/XL. Seed all habitats then finish packs under budget. Swarms permit **6–10 members**, ordinary beasts 2–5, heavy beasts 1–3, casters 2–4, scavengers 1–3; initial populations can be smaller than per-habitat capacity. Swarm global cap is three quarters of budget; other roles one third. Exported overrides remain respected.
- Heavy beasts have **150 HP / 18 contact damage / 1.7× body size**, versus ordinary beasts' 42 HP/10 damage. Swarms remain 16 HP/5 damage. Live intrinsic rewards are **3/18/36/10 A** for swarms/beasts/heavies/casters; scavengers only drop stolen reserves. Random heavy selection makes tougher solo/pair encounters alongside easy swarms.
- Habitats remember their seeded target. Death starts a **20-second delay**, followed by one replacement every **five seconds**. Either faction owning a guard's linked site suppresses replacements without removing living guards. Becoming neutral restarts the full delay. Wild recovery is independent. Replacements wait within 64 m of a living pilot, keeping the 32 m placement ring away from immediate pop-in. A 0.5-second census limits polling; raids keep full-frequency clocks. Pause freezes both; live interfaces do not.
- Ordinary spawning cannot create rival ships or base raiders. The designated rival is created/respawned only by the match/objective loop.
- Every placement gets at most **64 attempts**. It excludes **60/80/80/80 m around every live globe base** (30 flat), avoids sharp support transitions, other mobs and scenery, and stays inside the flat arena. Failure allocates nothing and never uses an unsafe nearby fallback.
- Mobs start throughout the habitat, from **2 m** out to a varied **12–32 m** footprint (16–32 for wild swarms, 24 for fixed spherical habitats, 14 flat). Safety spacing between bodies remains 6 m. The stable habitat centre is home; populations remain bounded rather than following the player.

## Neutral scavengers

The historical `AntimatterThief` role is now displayed as Scavenger. It searches locally within the smaller of **42 units or half its habitat leash**, preferring the nearest faction-owned loose antimatter over neutral globes. Both factions can be robbed; matter/cargo is unaffected. It stops scavenging after reaching 12 carried antimatter (the last whole globe can exceed that amount), and during alert, pursuit or return.

Stolen antimatter stays in the scavenger, not a faction base, and drops exactly on death. It starts without minted reserve. Other live roles use the differentiated rewards above; standalone diagnostic fixtures retain their explicit/default reserves. Live ships use Claim/collectors without a physical ship reserve.

## Separately paced raids

- A **180-second opening grace** precedes the first raid. Successful launches reset a **180-second interval**; at most **one raider** is alive. No intensity ramp changes these clocks. Failed/capped attempts retry after ten seconds. Pause stops timers; live B/M/H panels do not.
- Successful raids alternate between living faction bases, starting with the player. If one is absent/destroyed, only a surviving target is eligible. A raid never changes its assigned base to the other faction midway.
- A red, labelled **34-HP** sieger approaches from a size-aware baseline **70/100/100/100 m**, searching between baseline + 1 and baseline + 40 m (46–53 flat), subject to the same all-base exclusions/support/spacing checks. It can locally fight nearby pilots, using a broader approach leash of spawn distance + 80 units around its assigned base.
- Base attacks deal **4 HP every 3 seconds**, with a **90-second lifetime**. An intact **400-HP** tier-one base survives an immediate unopposed raid; approach travel reduces attack time. Raid pressure needs hands-on tuning against stronger bases.
- Kill, target loss or expiry clears the raider and releases its exact reserve as neutral salvage. An ordinary sieger without an explicit raid assignment cannot ram a base.
- Toast notices are removed. The world label identifies the raid target; local map markers are orange-red with raid details. F3 shows counts, launch total and time to the next raid check.

## Verification

`tools/NeutralBehaviorChecks.tscn` covers flat, globe underside and face-seam local behavior; both pilots; proximity/retaliation boundaries; dead/freed attackers; leash, return and grace; real pursuit/return/swarm-contact movement; melee/ranged damage to rivals; line-of-sight/windup cancellation; projectile/area/guardian attribution; three-ownership scavenging/conservation; all-size safe placement/habitat/role caps; pause/grace/interval/cap/alternation; actual raid approach, attack cooldown, collateral rejection, expiry/accounting, intact-base survival and blocked placement with no fallback.

Run after building/importing:

```powershell
& $godot --headless --path . 'res://tools/NeutralBehaviorChecks.tscn'
& $godot --path . 'res://tools/NeutralBehaviorChecks.tscn'
```

Rendered verification covers Medium live placement and saves `NeutralRaidGameplay.png` to the approved temporary directory. The harness reorients scripted teleports and refreshes the camera/terrain before taking captures. Existing economy, equipment, exploration, layout, navigation, objectives and flight/clearance checks remain part of regression verification.

Next: meaningful balance playtesting; shared-mechanics rival and compact icon management/four-slot HUD are complete for their first pass. Later work includes richer patrol/search animations, ground-route obstacle avoidance, more habitat content and raid tuning.

# Shared-Mechanics Rival AI

## Stronger-pack playtest follow-up (2026-10-12)

Live Medium is now radius 300 with broader neutral aggro and same-habitat damage retaliation. Recovery goals are committed against local threat reacquisition until the existing home recharge threshold; no free healing/funding is added. The unassisted opening verifier waits for both Shield installation and an owned site within its existing bounded budget. Final headless run passes **97** checks (including **90** controlled mechanics checks), with real paid orders, installed Shield and owned-site capture. Earlier accelerated runs missed the owned-site deadline even while researching/installing and surviving; repeated defense/salvage/recovery pressure and frame-based accelerated observation remain reliability/balance follow-ups alongside slope/obstacle routing. Do not treat a passing opening or terrain-only access success as full-match rival acceptance.

## Status (2026-10-11)

Phase 3 is implemented and regression-verified. Both live Game/PlanetGame rivals use the shared player ship implementation. Phase 4 compact UI is also complete (`COMPACT_UI.md`); next is hands-on balance. Strategy and longer-session performance remain provisional.

## Shared mechanics

Use `ShipFlightController` for both faction ships. Separate human input/camera/cursor ownership from simulation and expose guarded movement/selection/activation requests to a small `RivalPilotController`. The runtime rival must no longer use the ecosystem creature's direct absorption, different health/speed, free weapon or disposable respawn template.

- Both factions start with Plasma Bolt I and equal reserves. Home antimatter determines energy; flight, regeneration, costs, cooldowns, invulnerability, shield, booster, Dash, ranks and four-slot installation rules are shared.
- Claim bolts mark both resources for collector hauling. Research/rebuild orders use ordinary payment, slots and prerequisites; the AI retains an energy storage reserve.
- Capture samples ships identically. Wreck collection secures the same offers; the AI chooses a legal boon/skip. Research, loadout, boons and pending offers stay on the same ship across normal home-backed respawn.
- Basic tasks: gather, capture, research, return home/install, explore locally discovered wrecks, occasionally attack, defend locally and retreat/recharge. Since 2026-10-13 both ships recover **2 HP/s after five seconds without dealing/taking combat damage**, anywhere; home provides no extra healing. Plasma uses the shared **2 E / 13–18–23 damage** tuning.
- Goal commitment, timeouts and retry suppression bound stalled routes. Existing tangent movement/clearance drives travel; obstacle avoidance remains basic.
- Faction combat rejects friendly ships/bases/collectors/guardians. Projectile ownership survives shooter deletion. Neutrals still target both pilots.
- AI must not poll human actions, own the mouse, create a camera or open UI. Live panels block only the human ship.

The old `EnemyRole.RivalShip` remains for historical diagnostic fixtures and their legacy objective overload. Live matches create `ShipFlightController(IsPlayerControlled=false)` plus a decision-only child controller. Runtime respawn retains the same ship/inventory/bonuses and never swaps it for a creature template.

`pilots` includes both ships; `player` stays human-only and `rival_ship` identifies the AI. Base spending/delivery updates matching ships immediately. Neutrals, terrain detail, capture, wreck collection, navigation and projectile/area/guardian rules recognise both. Projectiles retain their launch faction after shooter deletion. Rival Claim uses red visuals. Neutral shots still cannot collateral-siege bases.

Autonomous movement/aim uses shared heading, acceleration/drag, banking, terrain follow, ceiling and energy simulation. Aim pitch uses human look limits. Guarded APIs pay the same costs and check installation, health, pause, input ownership, cooldown and energy; unaffordable requests do not start a cooldown. AI creates no camera and ignores human polling. It cannot own human flight audio; remote casts are excluded from the non-spatial placeholder cast sound.

## Basic policy and defaults

- Think every **0.5 seconds**. Gather visible resources first; only resources within **120-unit local reveal range** qualify. Claim marks ownership; the ship cannot absorb either resource. Full storage gates the two resources independently; mixed collectors deliver normally.
- Capture known stations near home, preferring antimatter extractors. Matter is removed. Hold inside the normal ring and obey contested capture; benefits remain ownership/capacity-dependent.
- Development starts after **15 seconds**, every **five seconds**, retaining **12 A** after research. Tier/rebuild reserve policy is bounded by possible storage after payment. Tiers/rebuilds use the shared current prices/capacities; projects use ordinary slots/payment. No free completion, funding or remote installation exists. Dense encounters permit normal combat deaths/ten-second objective-owned respawns; opening checks validate recovery rather than requiring immortality at one arbitrary frame.
- Default preferred loadout: **Plasma Bolt / Shield / Dash / Pulse Burst**, slots 1–4. Completed ranks trigger home return and explicit installation. All five equipment families, including Guardian, are available through shared APIs; this minimal strategy does not research/use every item or optimise its loadout.
- Wrecks are discovered only within **120 units**, without changing human chart flags. After economic expansion, approach known wrecks, pay ordinary shots to reveal them, and collect the same three-choice offer. Choose legal uncapped recharge/damage/mobility/hull priorities; low storage or fully capped options use the legal skip. No boon/rank injection occurs.
- Defend locally against pursuing neutrals, nearby home raiders, or an opposing pilot within 32 units. Use normal weapons, installed Shield/Pulse Burst, and the ordinary booster when affordable. Owning Dash does not imply optimal use in this first policy.
- Attacks become eligible after **300 seconds**, with tier 2+, health above 60 and current energy at least 20; finish the current committed economic goal first. Approach the rival base/pilot, fire within range/line of sight, limit engagement to **20 seconds after reaching firing range**, then wait **180 seconds after the attack ends**.
- Low health/energy causes retreat/recharge. Zero storage cannot fund Claim, so gathering is invalidated and no-cost income-site capture becomes the recovery path. Wounded ships can recover under the shared quiet-combat rule; home grants no special refill. Goal deadline: 25 seconds plus twice estimated unboosted travel time. A stalled route tries lateral steering after six seconds, aborts after twelve, and suppresses the target for thirty. This is basic steering, not navigation-mesh pathfinding.

F3 BASES/ENEMIES shows task, goal, energy and paid-order count. Rivals start with 100 HP, 24 cruise speed, one-second hit invulnerability, 30/30 energy and Plasma Bolt I, matching human defaults. Flat-reference factions now also have equal 30/30 reserves.

## Long-haul strike follow-up

Antipodal bases exposed the old strike drone's 60-second expiry at 18 units/s. Spherical lifetime is now **max(configured 60 seconds, launch surface distance / speed + 15 seconds)**. Flat lifetime, speed, cost and one-shot 45 damage are unchanged; target loss still expires the unit without refund. This is a shared route-lifetime correction, not an AI-only benefit.

## Verification and next work

`RivalFactionChecks.tscn` covers flat/underside parity, input/camera isolation, paid projectiles/shield/booster, friendly fire, actual Claim and mixed hauling, paid research/remote rejection/home installation, Guardian targeting, tier-loss/rebuild retention, capture/contest, AI boon choice/skip, persistent respawn and victory. A committed-combat fixture verifies paid siege shots and retreat without waiting for the five-minute strategic grace.

The original faster-tuning baseline advanced **88 simulated seconds at 8× time scale**. Following the 2026-10-12 grouped encounters and dearer 60–120-second research pass, the integrated opening allows a longer bounded window for ordinary income, paid orders, site capture and researched/home-installed Shield. Shared faction checks still pass **95 headless**; development continues while B blocks only human input. The current window is approximately 208 seconds plus up to 320 seconds waiting for installation. This is correctness evidence, not normal-session pacing, a frame-time benchmark or full-match balance acceptance. Rendered gameplay/F3 inspection remains recorded in the earlier progress entry.

```powershell
& $godot --headless --path . 'res://tools/RivalFactionChecks.tscn'
& $godot --path . 'res://tools/RivalFactionChecks.tscn'
```

Next: hands-on economic/combat/raid/travel/readability tuning; compact management/four-slot HUD is implemented. Richer tactics, obstacle routing, exploration prioritisation and wider catalog use remain future AI work.

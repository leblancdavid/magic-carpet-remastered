# Economy, Automated Hunting And Combat — 2026-10-14

User-approved follow-up to `AUTOMATIC_FLEETS_PLAYTEST.md`, superseding earlier economy/cargo/combat/recovery values. Provisional pending human Medium-match acceptance. Catalog: **68 types / 27 playable**, seven equipable ship items. Remaining tier-II content: Hunter Drone, Rocket Battery, Flak Battery; then III–V.

## Economy

| Tier | Intact AM capacity | Next-tier price |
| --- | ---: | ---: |
| I | 300 | 150 |
| II | 500 | 250 |
| III | 750 | 375 |
| IV | 1,000 | 500 |
| V | 1,500 | — |

- Advancement costs half the **current tier's nominal capacity**, rounded up. Damage cannot discount it. Positive inspector cost overrides remain for diagnostics; zero selects the formula. Total advancement: **1,275 A**. Durations remain 60/80/100/120s; rebuilding is rounded-up half normal price. Exact project payment/refund/siege snapshots remain.
- Start with **30 A**. Combat energy remains separate, limited by actual retained AM, with 4%/s recharge. Empty storage grants no energy.
- Four extractors: **0.35 A/s / 21 A/min** each. Optimization adds **20/40/60%** (25.2/29.4/33.6 A/min). Fractional accounting, capacity clipping/discarded backlog, ownership transfer and neutralization cutoff remain.
- Collector capacity **80 A**, Cargo Compression **120/160/200 A**. One basic load holds a heavy's 72 A. Partial scoops, retained overflow, death spills and station cargo theft remain physical/exact. One home collector per tier, 15s replacement, cargo-safe retirement on tier loss.

## Harvester Drone — Resources Tier II

- Stars cost **70/140/245 A**, take **40/60/80s**. Bronze grants one automatic slot; free 10s production/replacement, ordinary tier-loss suspension. Gold permits paid expansion under the provisional five/type cap.
- Target ordinary **non-site habitats**, including wild/landmark habitats, preferring uncovered habitat IDs before distance. Exclude route guards, rivals and raiders. Commit to living prey; after death spend up to eight seconds at its last position to catch staggered fragments before reassignment.
- **75/100/125 HP**, **8/12/16 damage/s**, 16m firing reach, 12m/s travel before relay. Actual line of sight gates damage. Retaliation identifies the drone as attacker. No capture/siege.
- Every 0.35s claim available **neutral-only** AM within **12/16/20m**, with line of sight and same-match checks. Opposing-owned, pending and carried drops are excluded. Collectors must physically scoop/haul/deposit; no direct or synthetic income. Original hull/UI/map glyphs, claim flashes and firing tracers identify the role.
- Both factions use the same catalog/fleet; rival research includes Harvesters.

## Combat And Recovery

- Attack drones: **75/100/125 HP**, **10/14/18 damage/s**, **2/2/3 structure damage** after retained one-fifth siege modifier. Defenders remain separately tuned at 45/60/75 HP, 6/8/10 damage per 1.2s. Both combat roles and harvesters show brief faction tracers.
- Swarms **16 HP / 8 contact**; beasts **50 HP / 15 contact**; heavies **175 HP / 26 contact**; casters **32 HP / 14 ranged / 1.2s interval / 6 contact**. Rewards remain **6/36/72/20 A**; populations remain **72/135/225/338**. Separate raid tuning stays unchanged.
- Recovery **25s delay / 6s stagger**. Wild first vacancy starts the delay; further kills while under target do not restart it. Site loss/neutralization keeps its explicit reset; either owner suppresses guard replacement. Ships still block spawning within 64m; harvesters do not. Placement/caps/pause remain bounded. F3 reports recovering, owned/suppressed and ship-blocked habitats.

## Ship Weapon Incentives

Plasma remains unchanged: 2 E / 13–18–23 damage / 0.25s. Shield retains normal mitigation; stronger incoming pressure should give it practical value.

- **Pulse Burst:** **5 E** (was 18), 2.5s, 32/40/48 damage, 8/10/12m area. Aim rays resolve bodies before terrain fallback, allowing airborne packs to be targeted. Friendly rejection remains; no dedicated siege damage.
- **Scatter Cannon, II:** seven pellets, **8/11/14 damage each**, **5 E / 0.8s**, 60m/s / 0.6s lifetime (36m reach), fixed cone slope 0.11. Close heavies/ships take most pellets; distant/dispersed targets do not. No pellet craters; one-fifth structural damage. Energy-damage boon.
- **Seeker Missile, II:** **42/56/70 damage**, **7 E / 1.8s**, visible mobile-enemy acquisition within **90m**, crosshair dot ≥0.9. Speed 40m/s, lifetime 4s, turning bounded to 2.2rad/s. No hidden initial locks or remote reacquisition; unguided launch is possible. Ray/terrain collision, **10 HP**, interceptable by opposing projectiles/area/Guardian/tactical units. One-fifth structural damage, no splash; explosive-damage boon.
- Both weapons use paid sequential research, remote plans/home installation, independent cooldowns, four-slot limits, ownership persistence and input/energy/pause gates. Tooltips explain costs/effects and original glyphs distinguish them.

## Checks And Next Human Playtest

`GameplayBalanceChecks.tscn` exercises actual heavy kill → delayed claim → ordinary collector haul/bank with exact AM totals; cargo stars; free production, gold/expansion and suspension; odd/damaged capacity prices; actual Scatter pellet hits/cost/cooldown; Seeker turning/impact/interception/faction/occlusion; body-aimed Pulse pack damage. Existing construction/fleet/habitat/capture/equipment/UI checks cover surrounding invariants. Completed runs/results are in the progress log.

Next Medium match: record time to tiers II/V, manual/automated/extractor income, hauling delays and storage-full time; whether bronze attackers clear light points and gold survive tougher opposition; damage/deaths and Shield usefulness; compare Burst on a pack, Scatter on a close heavy, Seeker on moving targets and Plasma as generalist. Larger storage also supports more energy, and free replacement/passive income can snowball. Accept pacing through playtesting before final tuning. XL physics and broader rival routing/difficulty remain pending.

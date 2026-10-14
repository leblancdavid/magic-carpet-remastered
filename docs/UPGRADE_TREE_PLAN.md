# Approved Upgrade Tree And Playtest Pass — 2026-10-13

## Status

Approved for implementation. This plan supersedes older catalog category/rank UI and explicit home Apply rules. Initial numbers are provisional playtest tuning. Implementation is staged; approval does not mean the content below is already playable.

**Implemented:** first gameplay stage plus shared tier/star catalog and all four tier-row grids, remotely planned/home-auto-synchronized loadouts/HUD markers, paid target orders, scout/attack/defensive deployment and interceptable strike drones. **22 of 65 types are playable** (all thirteen tier-I types, eight tier-II types and the existing Guardian moved to IV). The other 43 are visible as disabled Coming later tiles, absent from the payable research catalog. Verification is in the progress log. **Next content:** remaining tier-II Hunter Drone/Rocket Battery/Flak Battery/Scatter Cannon/Seeker Missile, then III–V. Hands-on economy/UI and dense XL performance remain pending.

## Playtest Mechanics

- Collectors carry internal AM amounts, not three visible objects. Baseline capacity 30 A; partial pickup leaves the remainder. Return full, when no eligible resources remain, or storage fills. Retain undeposited cargo; death spills exactly. Tier I–V entitlement is 1/2/3/4/5 home collectors, with delayed replacement; logistics collectors are additional. Excess collectors retire after delivering on tier loss.
- Claim costs 1 E and claims every eligible AM sphere along its swept terrain-following path, without ending on a hit.
- Smooth turn/slip inputs and bank/camera roll, retaining maximum bank and gradual reversals.
- Habitat recovery 35s; replacements 8s apart. Broader role-specific wandering with shorter pauses. Combat-capable neutrals attack bases encountered locally.
- Every base has a modest automatic retaliatory pulse weapon, line-of-sight/range bounded, without storage or ship-energy expenditure.

## Progression And Interface

- Four tabs: Offense (base attacks/units), Defense (base/site protection), Resources (economy), Ship (equipment/passives).
- Each tab uses tier I–V rows with 3–4 upgrade tiles per row. Main tile buys the next sequential star. No stars unbuilt, bronze one, silver two, gold three. Progress/cancel overlay the tile. Header retains tier advancement/rebuilding.
- Stars indicate researched level. All three levels unlock at the type's base tier. Existing category construction slots, payments, exact refunds and siege accounting remain.
- Under ship equipment: Equip/Unequip selects a planned four-item loadout remotely. Passives read Passive. Active equipment stays unchanged until automatically synchronized inside the living home installation area, including newest owned ranks. No Apply click. Home respawn also synchronizes. Do not reset cooldowns or refill energy during synchronization.
- Active four-cell HUD marks pending loadout/rank changes with amber indicators and Return home guidance. Selection identity, plans and ownership survive death; removing pending changes clears markers.
- Base capabilities have separate paid Launch/Fire/Deploy buttons opening eligible-known-target dropdowns and confirmation. Research is permanent; production/firing is repeatable, separately costed. Hidden targets must not leak through orders.
- Ship passives apply upon research completion; no equipment slots or home installation.

## Capture And Defense Drones

- Scout drones seek nearest non-faction-owned point, including enemy ownership. Half ship capture speed, no scout speed stacking; hostile ships/capture drones contest. Preserve neutralization/capture/departure/ownership benefits.
- Defensive drones deploy to home or owned points, defend locally and retreat home when assignment ownership is lost. Do not transfer to captors.
- Local particle cannon requires range and line of sight. Planet-wide attacks use strike/torpedo units or later orbital capabilities.

## Approved Catalog

| Tier | Offense | Defense | Resources | Ship |
| --- | --- | --- | --- | --- |
| I | Scout Drone; Attack Drone; Drone Command Relay | Repair Matrix; Pulse Turret; Defensive Drone | Cargo Compression; Collector Drive; Scoop Array | Plasma Bolt; Pulse Burst; Energy Shield; Claim Calibration (passive) |
| II | Strike Drone; Hunter Drone; Rocket Battery | Base Armor; Flak Battery; Site Fortification | Collector Armor; Dock Automation; Extractor Optimization | Scatter Cannon; Seeker Missile; Dash; Energy Grid (passive) |
| III | Particle Cannon; Capture Squadron; Disruptor Drone | Shield Pylon; Interceptor Array; Emergency Bulkheads | Collector Escort; Research Coordination; Storage Reinforcement | Rail Lance; Arc Emitter; Repair Projector; Hull Reinforcement (passive) |
| IV | Siege Torpedo; Assault Squadron; Siege Coordination | Heavy Defense Beam; Repair Drones; Suppression Battery | Collector Overdrive; Remote Depot; Salvage Reclaimer | Siege Launcher; Guardian Drone; Decoy Projector; Vector Thrusters (passive) |
| V | Siege Carrier; Orbital Lance; Electronic Warfare Array | Aegis Network; Emergency Barrier; Defense Coordination | Quantum Cargo; Extraction Network; Construction Efficiency | Particle Beam; Disruptor Cannon; Phase Dash; Energy Recycling (passive) |

Equipment roles: Plasma efficient hunting/weak siege; Pulse clustered neutrals; Scatter close heavies/ships; Seeker moving aerial targets; Rail precise armored targets; Arc packs/drones; Siege Launcher structures; Guardian mobile combat/site holding; Decoy guidance countermeasure; Particle Beam sustained aimed pressure; Disruptor barriers/recovery; Dash mobility; Phase Dash evasive immunity. Stars preserve weaknesses. Target classes, energy/cooldown, range and interception distinguish roles. Tooltips describe strengths/weaknesses.

## Provisional Research Costs / Times

Each payment is incremental. Plasma I remains free. Per-item premiums and production costs need actual playtest balance.

| Unlock tier | Bronze | Silver | Gold |
| --- | --- | --- | --- |
| I | 40 A / 30s | 80 A / 45s | 140 A / 60s |
| II | 70 A / 40s | 140 A / 60s | 245 A / 80s |
| III | 110 A / 50s | 220 A / 75s | 385 A / 100s |
| IV | 165 A / 60s | 330 A / 90s | 575 A / 120s |
| V | 240 A / 75s | 480 A / 110s | 840 A / 150s |

Initial production suggestions: Scout 25 A, Attack 40 A, Strike 100 A. Cargo Compression 40/50/60 A cargo; Collector Drive +15/30/45%; Repair Matrix +25/50/75%; Extractor Optimization +10/20/30%. Choose upgrades rather than complete the tree during the 10–15 minute tier-V progression target.

### Live Production And Early Effects

- Scout **25 A / 10s**, Attack **40 A / 10s**, Defender **40 A / 10s**, Strike **100 A / 20s**. Research and production share their category slot, with other slots independent. A cancelled/invalidated-target production project refunds its exact payment, preserving overflow; launched units retain expended investment and do not drop free AM.
- Explicit target orders validate knowledge, ownership, liveness and match membership before payment and revalidate eligibility at completion. Automatic scouts may discover unknown points; their dropdown does not enumerate hidden ones. There is no implicit rival-base attack when research completes.
- Scouts have **30/40/50 HP** and **14/16/18m/s** travel; combat/defensive units **45/60/75 HP**, **6/8/10 damage / 1.2s**, 16m firing range and local target/line-of-sight bounds. Attack damage to bases is one fifth. Finite lifetime starts at 180s, extended for long initial routes; scouts retarget after capture. Defenders that lose a site retreat and continue defending home. Units snapshot production stars; relay/fortification passives apply live.
- Drone Command Relay adds **10/20/30%** speed and **5/10/15m** local acquisition range. Site Fortification gives stationed defenders **10/20/30%** resistance. Base Armor gives **10/15/20%** resistance; Pulse Turret becomes **8/10/12 damage / 1.1/1.0/0.9s**. Collector Armor gives **15/30/45%** resistance; Scoop Array **25/50/75%** radius; Dock Automation **20/40/60%** shorter docking. Ship Energy Grid gives **15/30/45%** recharge and persists through tier loss. All numbers remain provisional.
- Scout sensing charts point locations (not distant live owner/benefits). Friendly/enemy ships or capture drones contest; same-side scout presence stays half-rate and a ship raises that side to normal rate. Local combat neutrals and player weapons can damage deployed units. Guardian support is now destructible too.
- Claim Calibration grants **150/180/210m** range and **1.6/1.9/2.2m** hit radius; projectile lifetime permits the full gold range. Player equipment plans/ranks synchronize on a living home-area physics tick, including while a live interface owns input. Synchronization never refills energy or resets cooldowns; normal respawn retains its existing resets.

## Execution And Verification

1. Collection/Claim/banking, then neutral/base combat/tier collectors.
2. Shared star definitions and tier-row grids; planned/active loadout synchronization.
3. Separate base research/paid orders and scout/defensive capture behavior.
4. Tier I–II content followed by III–V playable passes.
5. Resource conservation/partial pickup/spill/theft; multi-hit Claim; research/refunds/siege; loadout synchronization/cooldowns/death; visible targeting; capture/contest/retreat and both-faction behavior. Rendered grids at 1280×720 and 960×540. Profile new actor/projectile load. Update status and durable lessons after verification.

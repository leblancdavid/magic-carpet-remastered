# Ship Equipment

## Current Stage — 2026-10-14

Three sequential bronze/silver/gold stars, tier-row research, remote plans and automatic home installation are implemented. **27 of 68 types** are playable across all categories. Seven equipable ship items include new tier-II **Scatter Cannon / Seeker Missile**. `ECONOMY_COMBAT_PLAYTEST.md` records the latest economy/combat changes; `AUTOMATIC_FLEETS_PLAYTEST.md` covers built-in **T Teleport Home** and owned-AM magnetism. Guardian stays manual; remaining future weapons are disabled and cannot take payment.

## Four-Slot Planned And Active Loadouts

- A fresh ship owns/installs Plasma Bolt bronze only. RMB Claim, Shift booster and Space jump are built in, outside the four slots.
- Weapons, Shield, Dash and Guardian share four equipment slots. Passives occupy no slots.
- B → Ship uses tier I–V rows and one tile per type. Clicking researches its next sequential star. Stars show researched level; tooltips distinguish installation from ownership.
- Equip/Unequip beneath researched equipment changes a **planned** loadout anywhere while alive. A fifth planned item rejects until another is removed. Keys compact in catalog order (Plasma, Burst, Shield, Scatter, Seeker, Dash, Guardian). Unequipped equipment stays owned; changes are free.
- The **active** loadout and installed levels do not change remotely. A living ship within **18 surface metres / 16 elevation metres** of its living home automatically installs the whole plan and newest researched levels on its physics tick. Live B/M/H input ownership does not prevent synchronization. There is no Apply button. Research finished while already home synchronizes on the next ship tick.
- Synchronization preserves selected item identity where possible, current energy, cooldowns, velocity and combat state. It latches held combat until release; installation cannot be used for instant recharge/cooldown reset. Normal respawn still performs its existing health/energy/cooldown resets and also applies a home-area plan.
- The active four-cell HUD stays usable during remote planning. Amber update glyphs mark changed slots/ranks; a Return home hint appears underneath. Undoing changes clears markers. Empty planned slots can replace occupied active slots only at home.
- Direct home-only installation APIs remain for diagnostic/rival slot placement; the player UI uses planned choices and automatic installation.

## Ownership, Prerequisites And Death

Purchased types/stars, active and planned loadouts survive death and base-tier loss. Tier loss blocks new research below its required tier, not previously installed/owned equipment. Previously owned equipment can still be planned and installed at a surviving lower-tier home. Ship passives retain their researched effect through tier loss; destroyed-home energy/storage rules still apply. Non-ship effects suspend below their prerequisites. Active construction is destroyed/refunded under the normal siege/cancellation rules.

Research pays stored AM immediately and lowers the storage-backed energy limit. Activation spends only current energy. Insufficient current energy rejects atomically; construction/delivery/installation never grant a free refill. Restart creates a new match with Plasma bronze only and fresh research/plans/boons.

## Live Ship Catalog

All stars unlock at the type's base tier and require the preceding star. Costs/times are per step, before station bonuses.

| Type | Unlock tier | Bronze | Silver | Gold |
| --- | --- | --- | --- | --- |
| Plasma Bolt | I | Free starter | 80 A / 45s | 140 A / 60s |
| Pulse Burst | I | 40 A / 30s | 80 A / 45s | 140 A / 60s |
| Energy Shield | I | 40 A / 30s | 80 A / 45s | 140 A / 60s |
| Claim Calibration (passive) | I | 40 A / 30s | 80 A / 45s | 140 A / 60s |
| Dash | II | 70 A / 40s | 140 A / 60s | 245 A / 80s |
| Scatter Cannon | II | 70 A / 40s | 140 A / 60s | 245 A / 80s |
| Seeker Missile | II | 70 A / 40s | 140 A / 60s | 245 A / 80s |
| Energy Grid (passive) | II | 70 A / 40s | 140 A / 60s | 245 A / 80s |
| Teleport Home (built-in T) | II | 70 A / 40s | 140 A / 60s | 245 A / 80s |
| Guardian Drone | IV | 165 A / 60s | 330 A / 90s | 575 A / 120s |

- Plasma: **2 E / 0.25s**, **13/18/23 damage**, only **20% against bases** (3/4/5 without boons). Efficient general hunting/skirmishing, weak siege.
- Pulse: **5 E / 2.5s**, **32/40/48 damage**, **8/10/12m** area. Body-aimed or terrain-targeted cluster clearing. Friendly units are rejected; no dedicated siege effect.
- Scatter: **5 E / 0.8s**, seven **8/11/14-damage** pellets, 36m reach, wide cone. Close heavies/ships; weak at range and against structures. No pellet craters.
- Seeker: **7 E / 1.8s**, **42/56/70 damage**, 90m visible mobile-target lock near crosshair, limited turning, 4s lifetime, 10 HP/interceptable. No remote reacquisition or splash; weak siege. Explosive-damage boon applies.
- Shield: **14 E / 6s**, **65%** mitigation for **4/6/8s**. Exposed approaches and concentrated fire.
- Dash: **10 E / 3s**, **46/55.2/64.4m/s** aimed tangent impulse, minimum 5m/s lift capped by ordinary jump launch budget; preserves stronger ramp momentum and ends booster thrust. Escape/chase/traversal.
- Guardian: **24 E / 8s**, **12/16/20 damage**, **12/18/24s** lifetime, **30 HP**, destructible support that fights nearby neutrals/opposing pilots/deployed units. Personal deployable damage remains pilot-attributed for boons/combat recovery. It does not capture sites.
- Energy Grid: **+15/30/45%** recharge, effective remotely on completion without a loadout slot.

Future equipment roles/tiers are displayed in the grid and recorded in the plan. Weapon stars strengthen a role while preserving target/range/energy weaknesses; future content must have actual gameplay effects before its payable definitions are registered.

## Built-In Claim

RMB fires a **1-E / 0.4s** aimed bolt that transitions to terrain/water skimming. Every eligible neutral/rival loose sphere intersecting a short swept segment changes ownership; hits do not terminate it. Owned/carried/pending/off-path spheres do not block/repeat credit. Spheres remain for collector delivery, not ship absorption. Full storage does not block Claim.

Range stays **120m**, hit radius **1.3m**, at every star. Calibration instead gives owned AM **8/10/12m** attraction gaps, **+25/50/75%** attraction strength and **1/0.8/0.6s** merge contact, versus 6m/normal strength/1.25s baseline. Neutral balls receive no bonus; changed ownership uses the new owner's research. Claim never consumes an equipment slot or requires home installation.

## Built-In Teleport Home

Tier-II research unlocks **T** without a loadout slot or installation. Channel **3s**, pay **20 current E on successful arrival**, cooldown **60/45/30s**. Movement, firing/energy actions, incoming damage and live interfaces interrupt. Arrival validates the actual hull at home-adjacent hover positions, resets movement and synchronizes the normal home plan; no healing/refill/unrelated cooldown resets. A blocked arrival spends nothing. Teleport cooldown survives death. See `AUTOMATIC_FLEETS_PLAYTEST.md`.

## Booster, Hover And Recovery

Shift is a fresh-press built-in boost: **4 E**, **36m/s**, **0.65s**, **2s cooldown**. It follows surface-relative forward heading, without special lift/diagonal stacking; normal momentum/gravity follow. UI/death ends thrust and held Shift cannot reactivate on close. Pause freezes timers.

Space costs **2 E**, with **12m/s** jump launch under **18m/s²** radial gravity, about 4m above 2m hovering. Natural ramp/drop launches and water traversal are free; no fall damage. Shared ships support 65° climbs with uphill slowdown, moderate drift and gradual visual bank/camera roll. F6 lab controls remain separate.

Both ships heal **2 HP/s after five seconds without dealing/taking accepted combat health damage**. Successful weapon/deployable damage resets the delay; misses, wreck damage and invulnerable hits do not. Recovery works in live interfaces and pauses with the match. Player health hits produce **0.28s** fading camera offsets/world darkening, without changing aim/flight direction.

## Boons And Verification

Boons remain match-level capped modifiers, independent of ownership/stars/plans; H handles secured choices, F3 shows collected/effective stats. Existing energy/explosive/deployable and movement/recharge/fire-rate/projectile-speed formulas combine once with installed equipment. Wrecks cannot buy equipment or stars.

`UpgradeTreeChecks`, `EquipmentProgressionChecks` and `CompactUiChecks` verify remote planning, cap/undo/selection, latest-rank home synchronization, unchanged energy/cooldowns, sequential payments/cancellation, death/tier loss/restart, actual activation and pending UI. Grids are checked/rendered at **1280×720 and 960×540**. Hands-on pacing/comfort and dense XL physics remain acceptance work; completed source checks are in the progress log.

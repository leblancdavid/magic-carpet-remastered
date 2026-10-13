# Strategic Economic Points

## Status And Purpose

First playable implementation, 2026-10-11; single-resource redesign, 2026-10-12. Capturable sites create economic objectives and contested routes. Their benefits follow current ownership, unlike permanent exploration boons. Matter has been removed; see `PLAYTEST_SIMPLIFICATION.md`.

## Implemented Defaults And Policies

- Eight neutral sites: **four antimatter extractors, two research, two logistics**. Each faction has two extractors at 0.38 radians, logistics at 0.68, research at 0.92 on different bearings. Corresponding distances are equal across all sizes; seed/access checks cover terrain routes. Flat sites retain x = ±48, z = -40/0/40/-65.
- A living pilot with a surviving base captures automatically within **12/18/18/18 surface metres** on Small/Medium/Large/XL (12 flat). Neutral capture takes **10 seconds**. Owned takeover takes **10 seconds neutralization + 10 seconds capture**; benefits stop at neutralization. Both factions present freezes progress. Departure drains unfinished work at half capture speed; defenders restore at full speed. A different claimant reverses retained neutral progress first. Neutralization persists. No input/payment is required.
- Extractors generate **0.2 antimatter/second**, one unit per five seconds, accumulating only a fractional remainder. Deposits respect partial room; full/missing/destroyed storage discards remainder without backlog/overflow. Ownership change clears fractional work. Prior-owner income ends exactly at neutralization; new-owner income begins next tick. `GeneratedAntimatter` / tracker `SiteGeneratedAntimatter` report accepted supply. Delivery raises the energy limit without refilling current energy.
- Each research station adds **25%** progress speed; bonuses add (two stations = 1.5×) across all five construction categories, including upgrades/rebuilds/repeatables and active projects. Payments, stored base durations, prerequisites, completed records and slots remain unchanged. Losing ownership immediately removes the rate bonus.
- Each site's guard habitat stops replenishing while either faction owns the site. Living guards are not removed on capture. Neutralization restarts the full 20-second recovery delay; replacements resume only while neutral and clear of nearby pilots. Wild habitats are independent.
- Neutral logistics stations activate exactly one drone on first capture. Neutralization suspends station-drone movement/pickup/deposit, preserving its old faction and attached cargo. Final capture resumes and transfers that same living drone, ownership groups/visuals, all attached cargo and destination; resets former seek/return/dock state; and applies the new base's collector upgrade. Cargo is retained at full storage and partly deposited when room exists. Destroyed drones are **not replaced** by recapture. Sites remain indestructible. Ownership persists when a base falls, but generation/capture for that base is disabled.
- Sites use ring/cross markers colored gray/cyan/red, orange contested. Player-owned sites are global; others require 220 m sensing. Hover gives role/owner/benefit/progress; local feedback appears within capture radius + 12. Rings follow terrain/water. B resource tooltip/F3 summarize sites, income, modifiers and drones; F3 reports supply. R resets ownership/drone state.
- A screen-space HUD bar above equipment shows role, current stage and percentage while the player is inside the zone, regardless of camera aim. Contested/restoration/owned states are explicit; live panels, pause, death and results hide it. Leaving hides the bar without erasing gameplay progress.

### Validation And Playtest

`StrategicPointChecks` covers flat/underside capture, contest/decay, prior-owner benefits, slow fractional income/capacity accounting, research, cargo theft, destruction/pause and all-size layouts. `NeutralBehaviorChecks` covers guard suppression for either owner and neutral recovery. `RivalFactionChecks` verifies actual autonomous capture/research/home installation and ordinary respawn. Hands-on balance remains pending.

See `BASE_DEVELOPMENT.md` for storage/construction, `EXPLORATION_BOONS.md` for wreck rewards and `STRATEGIC_NAVIGATION.md` for current navigation and planned markers.

## Initial Site Roles

| Site | Benefit while owned |
| --- | --- |
| Antimatter extractor | Recurring antimatter income deposited directly into base storage. |
| Research station | Increased research/construction speed. |
| Logistics station | Control of an associated collection drone. |

Prioritize economic benefits. Exact site counts, income rates, research modifiers and geographic distribution are tunable; additional site types can be added later.

## Timed Capture And Contest

- Players capture a point by remaining in its capture area for a duration. RMB Claim remains the loose-resource tool, not the site-capture action.
- Capture takes place in real time.
- An opposing player in the area halts capture progress.
- The existing owner retains benefits only until the neutralization stage completes; a neutral point grants no owner benefit.
- Completion changes ownership and associated benefits together. Remove the old owner's benefits before enabling the new owner's benefits so there is no double income or duplicated drone.
- Current radius, stage duration, decay and pilot-only participation follow the implemented policies above. Broader multiplayer/unit participation remains later work.

Progress feedback should identify the owner, claimant, capture progress and contested state. Completion cannot trigger repeatedly for the same ownership transition.

## Direct Resource Income

- Extractors deposit into the current owner's base storage, without physical output at the site or collector transport.
- Respect the applicable resource storage capacity. Stop deposits while that storage is full; do not spawn continuous overflow spheres or accumulate a hidden backlog to pay out later.
- Partial remaining room accepts only the amount that fits; use the implemented fractional accounting above without allowing over-capacity storage.
- Antimatter deposits raise stored antimatter and the player energy limit through normal accounting; they do not instantly refill current combat energy.
- After spending, withdrawals or damage free room, subsequent income can resume.
- Do not award new storage income to an absent/destroyed base; exact post-defeat site handling is a future match-lifecycle decision.

Site generation is new antimatter supply, distinct from transfers, ownership changes and construction expenditure. Track it explicitly in resource-accounting checks.

## Research Benefit

Research speed follows current site ownership; losing the station removes that benefit and capturing it grants the benefit to the new owner. It does not grant equipment, bypass tier prerequisites, change project payments or create an extra construction slot.

All five categories currently receive additive station bonuses as described above. Apply bonuses to progress rates consistently rather than granting free completion on a capture event.

## Logistics Drone And Cargo Theft

- The logistics station has an associated drone. Neutralization suspends it; final ownership capture transfers it.
- On capture, transfer the existing drone to the new owner, wherever it currently is. Do not despawn it, retire it after delivery or replace it with a duplicate as the normal transfer behavior.
- All antimatter aboard changes ownership immediately, stealing that cargo from the previous owner.
- Keep cargo amounts, cargo attachment and physical state intact. Ownership transfer is not resource creation, expenditure or an extra loose-resource copy.
- Abandon the former owner's pickup/deposit orders and reroute carried cargo to the new owner's base. An empty drone subsequently serves the new owner's collection needs.
- Transfer must remain correct while seeking, scooping, returning or docking. Revalidate ownership and destination immediately before any pickup or deposit to prevent stale callbacks delivering stolen cargo to the former owner.
- Existing resources already deposited at the previous base do not transfer retroactively.
- If the new base is full, retain capacity-aware cargo handling; do not overfill storage or delete cargo. Current collectors wait/retry and retain partial-deposit remainders aboard.

Current neutral activation, no replacement, new-owner upgrades and indestructible-station policies are provisional as recorded above. The approved rule is control and cargo transfer on capture, not automatic respawning.

## Layout And Navigation

- Distribute sites to support contested economic routes rather than concentrating advantages beside one faction's starting base.
- Evaluate spherical surface travel distances, terrain accessibility and comparable opportunities for each faction across all size presets.
- Fixed strategic sites and random arriving wrecks are separate placement systems. Do not apply a wreck's base-exclusion rule to stations without a layout decision.
- Radar/world-map markers show site role, ownership and benefit information; locally visible capture feedback shows progress and contest.
- Current eight-site layout and ship-only sensing remain tunable.
- Base UI should summarize owned sites, income, research modifiers and station-controlled drones separately from purchased drones.

## Verification Requirements

- Timed capture works only with eligible presence; opposing presence halts progress.
- Neutralization stops prior benefits and suspends logistics; final capture grants benefits/transfers cargo once. Decay/restoration never duplicates a capture.
- Resource income reaches only the current owner, respects partial/full capacity and resumes when room returns without stockpiled overflow.
- Research modifiers transfer without bypassing prerequisites or duplicating completion.
- Drone ownership and every cargo resource transfer together, with the exact total conserved.
- Capture during scoop/return/dock cannot deposit into the former owner's base or create duplicate cargo/drone bodies.
- Repeated recapture, drone destruction and restart do not retain stale owners/routes/benefits.
- Capture feedback and management do not pause gameplay or leak UI clicks into combat input.

## Later Decisions

- Tune provisional capture/layout/income/research defaults from playtests; add deliberate rival capture routing with deeper AI work.
- Tune slower passive income against hunting, hauling and construction demand.
- Decide later drone replacement, station damage and richer complete-base-loss policies.
- Consider broader participants and discovery rules as faction/match scope expands.

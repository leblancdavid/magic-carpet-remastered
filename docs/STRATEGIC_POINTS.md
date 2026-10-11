# Strategic Economic Points

## Status And Purpose

Approved design, 2026-10-11; implementation pending. Capturable sites create economic objectives and contested routes around the planet. Their benefits follow current ownership, unlike permanent exploration boons.

See `BASE_DEVELOPMENT.md` for storage/construction, `EXPLORATION_BOONS.md` for wreck rewards and `STRATEGIC_NAVIGATION.md` for current navigation and planned markers.

## Initial Site Roles

| Site | Benefit while owned |
| --- | --- |
| Antimatter extractor | Recurring antimatter income deposited directly into base storage. |
| Matter extractor | Recurring matter income deposited directly into the base's material storage once that resource is implemented. |
| Research station | Increased research/construction speed. |
| Logistics station | Control of an associated collection drone. |

Prioritize economic benefits. Exact site counts, income rates, research modifiers and geographic distribution are tunable; additional site types can be added later.

## Timed Capture And Contest

- Players capture a point by remaining in its capture area for a duration. RMB Claim remains the loose-resource tool, not the site-capture action.
- Capture takes place in real time.
- An opposing player in the area halts capture progress.
- The existing owner retains its benefits until capture completes; a neutral point grants no owner benefit before completion.
- Completion changes ownership and associated benefits together. Remove the old owner's benefits before enabling the new owner's benefits so there is no double income or duplicated drone.
- Radius, duration, progress retention/decay on departure, multi-player participation and whether AI/unit occupants also contest are not yet specified.

Progress feedback should identify the owner, claimant, capture progress and contested state. Completion cannot trigger repeatedly for the same ownership transition.

## Direct Resource Income

- Extractors deposit into the current owner's base storage, without physical output at the site or collector transport.
- Respect the applicable resource storage capacity. Stop deposits while that storage is full; do not spawn continuous overflow spheres or accumulate a hidden backlog to pay out later.
- Partial remaining room may accept only the amount that fits. Rate/tick/fractional accounting must be specified without allowing over-capacity storage.
- Antimatter deposits raise stored antimatter and the player energy limit through normal accounting; they do not instantly refill current combat energy.
- After spending, withdrawals or damage free room, subsequent income can resume.
- Do not award new storage income to an absent/destroyed base; exact post-defeat site handling is a future match-lifecycle decision.

Site generation is new resource supply, distinct from transfers, cargo ownership changes and construction expenditure. Track it explicitly in resource-accounting checks. Matter capacity and its wider logistics remain a dependency of matter extractors.

## Research Benefit

Research speed follows current site ownership; losing the station removes that benefit and capturing it grants the benefit to the new owner. It does not grant equipment, bypass tier prerequisites, change project payments or create an extra construction slot.

Exact eligible construction categories, stacking across multiple stations and treatment of active-project remaining time are implementation decisions. Apply bonuses to progress rates consistently rather than granting free completion on a capture event.

## Logistics Drone And Cargo Theft

- The logistics station has an associated drone. Ownership of that drone follows ownership of the station.
- On capture, transfer the existing drone to the new owner, wherever it currently is. Do not despawn it, retire it after delivery or replace it with a duplicate as the normal transfer behavior.
- All antimatter or matter currently aboard changes ownership immediately, stealing those resources from the previous owner.
- Keep cargo amounts, cargo attachment and physical state intact. Ownership transfer is not resource creation, expenditure or an extra loose-resource copy.
- Abandon the former owner's pickup/deposit orders and reroute carried cargo to the new owner's base. An empty drone subsequently serves the new owner's collection needs.
- Transfer must remain correct while seeking, scooping, returning or docking. Revalidate ownership and destination immediately before any pickup or deposit to prevent stale callbacks delivering stolen cargo to the former owner.
- Existing resources already deposited at the previous base do not transfer retroactively.
- If the new base is full, retain capacity-aware cargo handling; do not overfill storage or delete cargo. Exact waiting/partial-deposit/overflow policy remains to be decided with general hauling rules.

Neutral-station drone behavior, replacement after destruction, interaction with purchased drone upgrades and station destruction/recapture policies are undecided. The approved rule is control and cargo transfer on capture, not automatic respawning.

## Layout And Navigation

- Distribute sites to support contested economic routes rather than concentrating advantages beside one faction's starting base.
- Evaluate spherical surface travel distances, terrain accessibility and comparable opportunities for each faction across all size presets.
- Fixed strategic sites and random arriving wrecks are separate placement systems. Do not apply a wreck's base-exclusion rule to stations without a layout decision.
- Radar/world-map markers show site role, ownership and benefit information; locally visible capture feedback shows progress and contest.
- Exact discovery/global-visibility policy, site counts and layout generation remain tunable.
- Base UI should summarize owned sites, income, research modifiers and station-controlled drones separately from purchased drones.

## Verification Requirements

- Timed capture works only with eligible presence; opposing presence halts progress.
- The prior owner retains all benefits until completion, and each completion transfers them once.
- Resource income reaches only the current owner, respects partial/full capacity and resumes when room returns without stockpiled overflow.
- Research modifiers transfer without bypassing prerequisites or duplicating completion.
- Drone ownership and every cargo resource transfer together, with the exact total conserved.
- Capture during scoop/return/dock cannot deposit into the former owner's base or create duplicate cargo/drone bodies.
- Repeated recapture, drone destruction and restart do not retain stale owners/routes/benefits.
- Capture feedback and management do not pause gameplay or leak UI clicks into combat input.

## Remaining Decisions

- Capture radius/duration, departure behavior and AI/unit contest rules.
- Site distribution/counts, income rates and research-speed stacking/categories.
- Material capacity and cargo-at-capacity behavior.
- Neutral logistics stations, destroyed-drone replacement and transfer of drone upgrades.
- Complete-base-loss, site destruction and visibility policies.

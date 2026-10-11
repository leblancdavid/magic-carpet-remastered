# Exploration Boons

## Status And Purpose

Approved design, 2026-10-11; implementation pending. Exploration rewards are permanent, stacking boons that influence play style. They do not grant weapons, equipment or equipment ranks; those come from base research. This supersedes the current exploration rank-II beacon reward model, which remains implemented until the boon milestone replaces it.

See `SHIP_EQUIPMENT.md` for researched progression, `BASE_DEVELOPMENT.md` for resource accounting and `STRATEGIC_POINTS.md` for ownership-dependent economic sites.

## Wrecks And Orbital Arrivals

- Initially distribute hidden boon rewards in randomly placed wrecked craft around the planet.
- Players must destroy a wreck to reveal its collectible reward.
- More wrecks fall out of orbit at random locations throughout the match.
- Wreck destruction does not award the boon to its attacker. The first player to collect the revealed reward secures the choice, even if another player destroyed the wreck.
- Once collected, the reward cannot be taken by anyone else, even while its owner defers choosing.

### Placement Fairness

- Exclude locations within a configurable spherical surface distance of every faction's base, not only the local player's base. Apply this to initial wrecks and ongoing arrivals.
- Validate reachable landing/reward positions, surface support and minimum spacing from other wrecks.
- Exclusion distance, population, arrival intervals and spatial distribution are tunable. Avoid repeatedly favoring a faction through clustering even outside the exclusion radius.
- Use bounded placement attempts and active-wreck/reward counts; if no valid location exists, defer the arrival instead of accepting an unfair fallback.
- On all four planet sizes, distance checks must work across seams, poles and the far hemisphere.

Wreck visuals, entry effects, hitpoints, fall behavior and collision/impact damage are not yet specified. Do not assume orbital arrivals are damaging bombardments.

## Reward Choice

Each collected reward presents three boon options plus a skip option:

1. Select one of the three boons and apply it once.
2. Alternatively, skip the boons for an antimatter bonus.
3. Either outcome consumes that reward; it cannot produce both a boon and a bonus.

Selection runs in real time. The player may defer choosing after collection; the reward and its choice remain secured to that player. Use UI input ownership so selecting or dismissing a panel does not also fire equipment or Claim. No pause or slow-motion advantage is granted by the choice panel.

Represent multiple pending choices independently. Offer generation, duplicate options within an offer, rarity and eligibility rules remain implementation decisions; deferring/reopening must not inadvertently reroll an offer.

### Skip Bonus

- Deposit the bonus directly into the collector's base antimatter storage; no delivery drone is required.
- Respect the current effective capacity, update the player's energy limit through the normal storage path, and do not automatically refill current combat energy.
- Spill any excess outside the base as player-owned antimatter, preserving the full bonus amount.
- Bonus amount is tunable. Handling an unresolved reward after complete base destruction remains to be specified alongside match termination.

## Persistence And Stacking

- Boons last for the match, survive death/respawn and stack when collected repeatedly.
- Starting a new match clears boons and pending choices.
- Keep boon modifiers distinct from equipment ownership, purchased ranks, base tiers and stored antimatter.
- Expose collected boons, stack counts and effective bonuses so players can make informed equipment choices.
- Exact percentage values, additive versus multiplicative stacking, caps and interactions with researched upgrades remain tunable. Define each explicitly before implementing formulas.
- HP boons must distinguish maximum HP from current HP; whether acquisition also heals is undecided. Base HP boons must not silently rebuild a destroyed tier.

## Initial Boon Families

These are candidate effects, not a final balanced catalog:

| Family | Example modifier |
| --- | --- |
| Mobility | Increased ship speed. |
| Energy | Increased antimatter-energy recharge rate, not new stored antimatter income. |
| Weapon handling | Increased fire rate or projectile speed. |
| Damage specialization | Increased physical/kinetic, explosive, laser/energy or deployable output. |
| Ship survivability | Increased ship maximum HP. |
| Base survivability | Increased base maximum HP. |

Damage taxonomy is not approved yet. A proposed starting point is kinetic, explosive and energy damage types, with deployable as an equipment/source tag rather than an exclusive damage type. Final categories, overlapping bonuses and damage attribution must be defined consistently with the equipment catalog.

## Navigation And UI

- Distinguish intact wrecks, revealed rewards and secured pending choices.
- Radar/map discovery rules and markers should support exploration without exposing every hidden reward globally by default; exact reveal rules remain to be designed.
- Show three choices, skip bonus, current stacks/effects and an option to defer.
- Show a real-time pending-choice indicator and a match-level boon summary.

## Verification Requirements

- Destroying a wreck reveals one reward once; repeated damage cannot duplicate it.
- First collector wins simultaneous pickup attempts; the attacker receives no exclusive right.
- Choosing and skipping are mutually exclusive and apply exactly once.
- Deferred choices remain secured and survive respawn; restart clears them and boon stacks.
- Repeated boons stack according to explicit formulas without changing equipment rank/ownership.
- Skip bonus deposits plus owned overflow equal the awarded amount, including at full storage.
- Initial and ongoing placement excludes all faction bases across every planet size and hemisphere.
- Placement failure and population bounds do not leak actors or bypass exclusion zones.
- Selection leaves simulation running and blocks accidental combat input.

## Remaining Decisions

- Boon values, stacking formulas/caps, offer selection/rarity and damage taxonomy.
- Wreck durability, arrival pacing, landing effects and uncollected reward lifetime.
- Reveal distances, map information and placement fairness metrics beyond minimum distance.
- HP acquisition/healing behavior and effects of base damage on boon-derived stats.

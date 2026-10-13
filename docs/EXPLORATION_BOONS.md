# Exploration Boons

## Status And Purpose

Implemented first pass, 2026-10-11. Exploration rewards are permanent, stacking match boons, separate from base-researched equipment/ranks. Random destructible wrecks replace the legacy world signals in both gameplay scenes. Hands-on pacing, fairness and balance remain provisional.

## Implemented Rules And Provisional Tuning

- Spherical worlds start with **12 wrecks**, bounded to **20 active intact/revealed actors**. Arrivals attempt every **60 seconds**; failed/full attempts retry after **5 seconds**. Collected pending choices are secured inventory, not world actors, and do not expire. Intact/uncollected actors persist until collected/new match.
- Uniform sphere candidates exclude **30/50/75/80 m from every base** on compact Small/Medium/Large/XL, including ruins, and **20/40/60/60 m from every active wreck/reward**, with **64 bounded attempts**. These defaults replace the old 180/100 m guards that cannot populate Small. Explicit non-default exported overrides remain. Fill the least occupied of twelve planetary regions, then break ties by cumulative nearest-base fairness. This improves coverage without guaranteeing equal travel costs. Sharp ledges (>4 height units over 2 surface units) reject; water supports floating rewards. Flat defaults: five initial/eight active, 30 base exclusion, 18 spacing.
- Wrecks have **50 HP**. Plasma Bolt, Pulse Burst and enemy projectiles can destroy them. Destruction reveals gold salvage without rewarding the attacker. Collect within **4 surface / 6 elevation units**; first living valid collector wins. Modern rival ships secure the same immutable three-choice offers and choose a legal boon/skip through their policy (`RIVAL_AI.md`). The legacy fixture archetype still takes skip; richer exploration/selection strategy remains future work.
- Orbital arrivals descend radially for **3 seconds** from 60 units above the landing height, with no collision/damage while descending; they become solid/damageable at landing. Placeholder procedural hulls and gold salvage are original geometry.
- Offers store **three distinct equal-weight catalog options**, generated once at collection. **H** opens pending rewards/summary; auto-opens on a new reward if another interface is not active. Escape/H defers. Multiple rewards remain independent and can be cycled; no reroll. H/B/M transfer interface ownership. Setup/results still pause, choices never do; death closes the panel and retains offers. Complete base loss ends the match, leaving unresolved offers unusable until reset.
- Skip awards **30 antimatter** directly through base deposit, with owned overflow outside the base. It increases capacity through stored antimatter without healing/refilling current energy. Choose/skip consume the offer exactly once. Capped choices remain displayed but disabled; another choice or skip remains available.
- Each family stacks additively up to **10**. Percentage factor is `1 + 0.1 × stacks`; independent families multiply. Integer damage rounds once after all factors. Rank effects are calculated first; no equipment/rank/base tier mutations.

| Boon | Implemented effect per stack |
| --- | --- |
| Vector drive | +10% normal surface flight, built-in booster and ranked Dash speed; no vertical/control/ceiling change |
| Flux recovery | +10% energy recharge, multiplied by active Energy grid; no stored resource income |
| Rapid cycling | +10% Plasma Bolt fire rate (`base cooldown / factor`); other abilities unchanged |
| Accelerated plasma | +10% Plasma Bolt speed, starting at 42; existing lifetime remains 2 seconds |
| Energy focus | +10% Plasma Bolt / summoned Guardian energy damage |
| Blast focus | +10% Pulse Burst explosive damage |
| Drone coordination | +10% summoned Guardian output as a deployable source tag, multiplying Energy focus; base strike research unchanged |
| Reinforced hull | +20 maximum ship HP; acquisition never heals current HP; respawn restores the enlarged maximum |

Damage attribution is scoped to this catalog: Plasma Bolt/Guardian are energy, Pulse Burst is explosive; deployable is an overlapping source tag. No kinetic weapon is currently offered. Base maximum-HP boons remain deferred so acquisition cannot silently restore lost layers; base HP/layer formulas are unchanged. H shows stack counts and live effective stats. Radar/survey show wrecks only within 220 m ship sensing, even after charting; intact wrecks and gold revealed salvage have distinct markers. Chart memory does not globally expose rewards.

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

The first-pass visuals, durability and harmless arrival policy are specified above; richer entry effects remain presentation work.

## Reward Choice

Each collected reward presents three boon options plus a skip option:

1. Select one of the three boons and apply it once.
2. Alternatively, skip the boons for an antimatter bonus.
3. Either outcome consumes that reward; it cannot produce both a boon and a bonus.

Selection runs in real time. The player may defer choosing after collection; the reward and its choice remain secured to that player. Use UI input ownership so selecting or dismissing a panel does not also fire equipment or Claim. No pause or slow-motion advantage is granted by the choice panel.

Pending choices are independent immutable offers: three distinct equal-weight options, fixed at collection. Deferring/reopening never rerolls. Capped options are disabled and skip remains available.

### Skip Bonus

- Deposit the bonus directly into the collector's base antimatter storage; no delivery drone is required.
- Respect the current effective capacity, update the player's energy limit through the normal storage path, and do not automatically refill current combat energy.
- Spill any excess outside the base as player-owned antimatter, preserving the full bonus amount.
- Bonus amount is tunable. Complete base destruction ends the match and blocks unresolved choices/bonuses; reset clears them.

## Persistence And Stacking

- Boons last for the match, survive death/respawn and stack when collected repeatedly.
- Starting a new match clears boons and pending choices.
- Keep boon modifiers distinct from equipment ownership, purchased ranks, base tiers and stored antimatter.
- Expose collected boons, stack counts and effective bonuses so players can make informed equipment choices.
- Percentage values, additive stacking/caps and independent multiplicative interactions are explicit above and remain playtest tuning.
- Ship hull acquisition increases only maximum HP; respawn heals to it. Base HP boons remain deferred until their tier-layer policy is explicit.

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

The first-pass attribution/formulas above use energy/explosive damage and an overlapping deployable source tag. Kinetic content and broader taxonomy remain future equipment work.

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

- Hands-on boon values/caps, equal-weight offer selection, wreck durability and arrival/travel pacing.
- Richer wreck/landing art and effects; current uncollected rewards have no expiry.
- Travel-cost fairness beyond nearest-base regional count balancing, and radar/map readability.
- Base-HP boon tier-layer/healing policy and deeper rival exploration/boon selection AI.

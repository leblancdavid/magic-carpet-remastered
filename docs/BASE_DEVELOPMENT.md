# Base Development

## Status And Direction

Approved design, 2026-10-11. This document specifies future implementation; the current prototype still uses the legacy castle/mana systems. The existing spherical mechanics are accepted as a smooth playtest baseline. Costs, timing, balance, AI, equipment progression and base development remain iterative work.

The game is moving to futuristic faction conflict over planetary resources. A base is the player's home and an RTS-style development hub. Players choose investments rather than obtaining base tiers automatically from stored resources.

See `SHIP_EQUIPMENT.md` for equipment ownership, installation and the four-slot loadout, `EXPLORATION_BOONS.md` for wreck rewards, `STRATEGIC_POINTS.md` for economic site benefits, and `TERMINOLOGY_MIGRATION.md` for the first implementation milestone. Historical original-game observations remain in the research documents and do not override this design.

## Resource Model

Keep these quantities separate in gameplay code and UI:

| Quantity | Meaning |
| --- | --- |
| Base storage capacity | Maximum stored antimatter allowed by the base's surviving tier and damage rules. |
| Stored antimatter | Collected antimatter currently retained at the base, available for construction expenditure and supporting the player's energy limit. |
| Player energy limit | Maximum rechargeable antimatter energy available for equipment use; equals stored antimatter in the new design. |
| Current energy | Rechargeable combat/movement energy, bounded by the player energy limit. |
| Matter | Planned material resource for selected building projects; acquisition, storage and costs are not yet specified. |

Equipment use and boosting spend current energy, not stored antimatter. Construction spends stored antimatter permanently, immediately lowering the player energy limit and clamping current energy if necessary. Current energy still regenerates within that limit. Collecting and delivering new antimatter increases stored antimatter and the energy limit; it does not grant an automatic full energy refill.

There is no construction reservation or post-construction limit-recovery mechanic. The earlier reservation proposal is superseded by permanent expenditure.

### Example

1. The base has capacity 200 and stores 200 antimatter; the player energy limit is 200.
2. A tier upgrade costs 100. Starting it leaves 100 stored, an energy limit of 100, and current energy no higher than 100.
3. Completion raises storage capacity to 300. Stored antimatter and the player limit remain 100.
4. Delivering another 100 raises storage and the player limit to 200. Another 100 can raise them to 300.

The numbers illustrate the rules, not an approved balance table.

### Collection At Capacity

Claim remains usable on loose antimatter even when the base is full. Collector drones stop gathering when storage is full; owned resources can remain in the world until room becomes available. Capacity-aware handling of already-carried cargo and partial deposits must be specified during implementation, without deleting resources or allowing over-capacity storage.

Matter is a later milestone. Do not invent material costs or collection behavior during the terminology-only conversion.

### Economic Sites And Reward Bonuses

- Captured antimatter/matter extractors deposit directly into the owner's applicable base storage without drone hauling. Continuous income stops while storage is full, without world overflow or a deferred backlog.
- Skipping a secured wreck reward deposits a one-time antimatter bonus directly into storage. Unlike continuous income, this preserves excess as player-owned antimatter spilled outside the base.
- Antimatter deposits use the normal storage/energy-limit accounting and do not directly refill current energy.
- Captured research stations improve research/construction speed while owned; scope and stacking are tunable. They do not alter payments, prerequisites or the five-slot limit.
- Capturing a logistics station transfers its associated drone and all carried antimatter/matter to the captor, rerouting delivery to the new base. This steals cargo but does not create new resources.
- Boons may improve energy recharge or base maximum HP independently of tiers/research. Base HP modifiers must not automatically restore destroyed tiers; formulas/healing behavior remain to be specified.

Site income and skip rewards introduce new supply. Distinguish them from transfers, refunds and intentional construction expenditure in world-resource accounting.

## Tiers And Research Access

- Start at tier 1; tier 5 is the provisional maximum.
- Purchase tier upgrades explicitly. Higher surviving tiers increase storage capacity and unlock higher-tier research.
- Separate purchased/rebuildable progression from the currently surviving tier. Spending resources must not itself demote the base.
- Damage can destroy tiers, reducing capacity and research access immediately.
- Research requiring a lost tier is unavailable until that tier is rebuilt.
- Installed ship equipment remains owned and usable subject to energy requirements, regardless of lost base tiers.
- Ordinary passive HP repair must not restore destroyed tiers for free.

Exact HP-to-tier thresholds, per-tier HP, capacities, costs and construction times are tunable implementation decisions. The legacy system uses HP-scaled storage and stored-resource-driven visual stages; that is not an implementation of explicit purchased tiers.

## Construction Slots

Five independent slots each permit one active project:

| Slot | Intended projects |
| --- | --- |
| Base | Tier advancement and sequential rebuilding of destroyed tiers. |
| Defense | Base defenses, energy regeneration, repair and HP improvements. |
| Resources | Additional collectors, speed, toughness and collector weaponry. |
| Offense | Units sent against nearby rivals and upgrades to those units. |
| Ship | New equipment, equipment ranks and built-in Claim improvements. |

Multiple categories can run concurrently, with up to five active projects. Each pays its full cost when started. A second project in the same slot cannot start until the active one completes, is cancelled or is destroyed. An automatic queue is not part of the initial design.

Remote ordering is allowed. Research and construction run in real time, including while the management UI is open. Ship equipment installation/loadout changes require returning home.

## Cancellation

- Cancelling an active project refunds its full paid antimatter investment, without a fee.
- Refund only what that project actually paid; a rebuild refunds its reduced rebuild price, not the normal tier price.
- Deposit the refund up to currently available storage space.
- Spill the remainder outside the base as player-owned antimatter, available to collectors.
- Completing, cancelling and destroying a project are mutually exclusive outcomes; repeated callbacks cannot refund or spill it twice.

Construction frees storage space at purchase time. Collectors may refill that space before cancellation, making refund overflow a normal case to support.

## Siege And Project Destruction

When the base loses a tier, all projects currently under construction are destroyed, including projects in other categories and the base slot. Individual project structures/HP are not required for this initial rule.

For each destroyed project's paid antimatter:

- 50% spills outside the base as unclaimed/neutral antimatter, allowing rivals to claim it.
- The other 50% is permanently lost; it is not refunded to storage.
- A project is destroyed once even if one attack removes multiple tiers.

For integer resource accounting, the rounding convention for odd costs remains an implementation decision; record it explicitly and ensure spilled plus permanently lost equals the paid amount.

Base storage damage spills are separate from construction-investment spills. Reducing capacity spills any excess stored antimatter under the retained neutral-spill behavior. Do not count construction investment as stored antimatter or spill the same amount twice.

### Siege Example

A tier-4 base has active projects paid at 100 and 60 antimatter. An attack drops it to tier 3:

- Capacity and research access drop to tier 3.
- Stored antimatter exceeding the surviving capacity spills separately.
- Both active projects are destroyed: 50 + 30 antimatter spills, and 50 + 30 is permanently lost.
- Restoring tier 4 requires a new rebuild project at half its normal upgrade price.

## Rebuilding

- Rebuilding a previously purchased, destroyed tier costs 50% of its normal upgrade cost.
- Rebuild one tier at a time using the base construction slot.
- A tier-4 base reduced to tier 2 must rebuild tier 3, then tier 4, paying the respective reduced prices.
- Buying a tier never previously obtained remains a full-price advancement.
- Rebuild completion restores that tier's capacity and research access, not previously spent or spilled antimatter.
- Rebuild projects follow the same cancellation and destruction rules, using their actual paid cost.

The current base-destruction defeat rule is the proposed baseline for complete base loss; the user has not separately confirmed a tier-0 recovery mechanic. Do not introduce one without a decision.

## Management UI

The base panel should show:

- Surviving tier, next tier and destroyed tiers eligible for rebuilding.
- HP/repair state, storage used/capacity, player energy limit and current energy.
- Five category slots with active project, progress, paid cost and cancellation.
- Available projects, prerequisites, costs, duration and why unavailable.
- Research access lost to damage, attack alerts and destroyed-project feedback.
- Completed ship research awaiting installation, with a route/home prompt.
- Owned strategic sites, income/storage saturation, research modifiers and station-controlled drones.
- Relevant boon modifiers, with base HP/recharge bonuses separate from purchased upgrades.

The panel does not pause the match. Input ownership must prevent UI clicks from firing equipment or Claim and restore flight/mouse handling when closed. Preserve existing survey/size-menu/result behavior during the terminology-only conversion; reconcile the paused survey with the approved real-time gameplay interfaces during UI work. Terminal results and fresh-match setup are separate from active-match management.

## Implementation Order

1. Convert terminology without changing existing mechanics.
2. Introduce explicit tiers and the stored-antimatter/energy-limit model.
3. Implement the five-slot construction lifecycle, expenditure and cancellation.
4. Implement tier destruction, investment spills and sequential rebuilding.
5. Add real-time management UI and initial upgrade definitions.
6. Integrate ship inventory/installation and equipment ranks.
7. Add matter collection and material-dependent projects after defining logistics.
8. Integrate boon bonuses and economic capture points; matter extractors depend on defined material storage. See the roadmap for their sequence after the base/equipment foundation.

## Verification Requirements

- Storage, player limit and current energy remain distinct and synchronized correctly.
- Starting multiple category projects spends once; occupied categories reject another project.
- Completion increases the purchased benefit without restoring spent resources.
- Cancellation returns the exact payment, preserving owned overflow.
- Single- and multi-tier hits destroy all active projects once, with separate storage spills.
- Sequential rebuilds use the correct half-price cost and cannot skip tiers.
- Passive HP repair does not restore destroyed tiers.
- Installed equipment survives lost prerequisites and death.
- Full storage stops new collection without preventing Claim.
- Real-time management input does not pause simulation or leak combat clicks.
- Accounting records expenditure/destruction sinks explicitly; current conservation-only checks must evolve to distinguish transfers, new supply and intentional consumption.
- Continuous site income respects capacity without backlog/overflow; one-time boon skip rewards preserve owned overflow.
- Station drone/cargo transfers conserve amounts and cannot deliver through a stale former-owner route.
- Ownership-dependent research modifiers and permanent boon modifiers remain separate from purchased tiers/upgrades.

## Remaining Design Decisions

- Completed base/collector/unit-upgrade behavior when its prerequisite tier is lost: disable temporarily, retain unchanged, or another policy. Ship equipment retention is already decided.
- Tier damage thresholds, rebuild duration, repair interaction and complete-base-loss policy.
- Odd-cost rounding for 50% spills/rebuilds.
- Pending completed ship research if the base is damaged or the player dies before installation.
- Matter acquisition/storage/refunds and overflow handling.
- Rival research behavior, faction differences and eventual participant/networking scope.

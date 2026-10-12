# Base Development

## Status And Direction

Approved design, 2026-10-11. Terminology, explicit-tier/storage-backed-energy accounting, five-slot construction, cancellation/refunds, tier destruction/rebuilding, real-time management, starter projects, owned equipment/four-slot home installation, Shift booster, boons, economic sites and matter logistics/material costs are implemented for their first pass. The existing spherical mechanics are accepted as a smooth playtest baseline. Costs, timing, balance, AI, equipment progression and base development remain iterative work.

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
| Matter | Independent construction material; never contributes to combat energy. |

Equipment use and boosting spend current energy, not stored antimatter. Construction spends stored antimatter permanently, immediately lowering the player energy limit and clamping current energy if necessary. Current energy still regenerates within that limit. Collecting and delivering new antimatter increases stored antimatter and the energy limit; it does not grant an automatic full energy refill.

There is no construction reservation or post-construction limit-recovery mechanic. The earlier reservation proposal is superseded by permanent expenditure.

### Example

1. The base has capacity 200 and stores 200 antimatter; the player energy limit is 200.
2. A tier upgrade costs 100. Starting it leaves 100 stored, an energy limit of 100, and current energy no higher than 100.
3. Completion raises storage capacity to 300. Stored antimatter and the player limit remain 100.
4. Delivering another 100 raises storage and the player limit to 200. Another 100 can raise them to 300.

The numbers illustrate the rules, not an approved balance table.

### Implemented Foundation And Provisional Tuning (2026-10-11)

- Fresh bases start at tier 1; internal sequential `TryCompleteTierUpgrade(targetTier)` supports tiers 2–5 and rejects skipped/duplicate completions. `Construction.TryStartNextTier` owns payment/timing, choosing advancement or rebuilding from surviving tier and purchase history. The B-key management panel exposes ordering and cancellation remotely.
- Configurable intact capacities default to **120 / 200 / 300 / 400 / 500**. Capacity at each tier is clamped nonnegative and at least the previous tier's capacity.
- Starting storage remains **30 antimatter**. `InitialEnergy` defaults to 40, capped by starting storage (therefore **30/30 energy** in a fresh match). Initialization occurs once when the home base becomes available, in either spawn order. An initially empty base initializes zero energy; later deliveries do not refill it.
- Energy limit equals home-base storage exactly, with no starting-power allowance or separate 400-power cap. Recharge defaults to **4% of the current limit per second** (1.2 energy/s at 30 storage); fractional recharge accumulates, and full energy discards unused recharge credit. Respawn retains the existing full refill within the current storage-backed limit, without changing physical resources.
- `TrySpendAntimatter(amount)` is an all-or-nothing positive payment; unaffordable/nonpositive payments change nothing. Successful payments immediately lower storage/limit and clamp current energy. `WithdrawAntimatter` remains a partial physical transfer API. Cumulative spending, refunds and investment spills are recorded separately from physical totals; see the construction accounting below.
- Spending or delivery never changes tier. Damage removes health layers/tiers, preserves `HighestPurchasedTier`, and destroys all active projects once. Complete base destruction sets tier 0 and energy limit 0, preserving the defeat rule; no tier-zero recovery is introduced.
- **Effective storage follows intact lower layers plus the rounded-up fraction of the damaged top layer's incremental capacity.** Tier 1 retains the previous HP-scaled rule. This is continuous at tier boundaries; quiet repair reopens surviving-layer capacity without restoring antimatter or lost tiers.
- The compact HUD shows base tier/storage and ship energy/limit; F3 adds purchased tier, five slot statuses/progress/payments, recharge, refunds and siege-investment diagnostics. B opens live management with those values, project costs/prerequisites, unavailable reasons, progress, cancellation and siege feedback.

These are provisional implementation defaults selected with user approval to proceed; playtesting determines balance.

### Collection At Capacity

Claim remains usable on loose antimatter even when the base is full, subject to its normal current-energy cost. Collector drones stop gathering when storage is full; owned resources remain in the world until room becomes available. Implemented policy: an empty drone waits at its home anchor; a loaded drone returns home, deposits only available room and retains undeposited owned cargo. Dock-delay retries resume delivery when room opens. Partial deposits never neutralize or delete cargo; destroying a loaded drone still releases its exact remaining cargo through the normal neutral-drop path.

### Matter Foundation — Provisional Defaults (2026-10-11)

- Matter is one generic material, stored separately from antimatter, starting at **30**. Intact capacities are **120/200/300/400/500**, independently configurable, using the same incremental health-layer damage rule. Repair reopens room without recreating material.
- Loose matter uses orange material crates, grounded/floating on authoritative terrain (not rolling antimatter spheres). RMB Claim marks neutral/rival crates; ships never absorb them. Existing collectors haul both resources, sharing their three-object cargo limit and choosing the nearest eligible resource with room at home. Full storage for one resource does not block gathering the other. Partial deliveries retain cargo; station capture transfers both cargo kinds; drone destruction spills exact remaining neutral material.
- Each base starts with a nearby **60-matter neutral field**; two additional mirrored **60-matter fields** support exploration. Two neutral matter extractors use existing timed/contested capture and deposit **2 matter/s**, stopping at capacity without backlog or overflow.
- Tier 2–5 material costs are **20/30/40/50**. Repair matrix, energy grid, collector drive and strike drone cost **5/10/5/15 matter** respectively. Ship/Claim research remains antimatter-only for this pass. Costs are snapshotted and both resources pay atomically; insufficiency in either changes neither reserve.
- Cancellation fully refunds both actual payments with owned overflow crates/spheres. Tier loss destroys all active projects: independently for each resource, floor-half spills neutral and the remainder is lost. Rebuilds round up half of each normal resource cost. Ordinary capacity spills remain separate. No tier-zero recovery is added.
- Separate material supply/physical/investment ledgers reconcile loose plus carried plus stored matter plus net committed matter against initial and extractor supply. Current energy is unaffected by material transfers, payments or refunds. These defaults require hands-on balance tuning.

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

Each tier adds one health layer, using the base's initial exported `MaxHealth` (**140 by default**) as per-tier HP. Tier completion adds a fresh layer, retaining pre-existing damage in lower layers. A tier is lost when health falls to or below the previous tier's maximum: default thresholds are 560/420/280/140 HP for tiers 5/4/3/2. Surviving `MaxHealth` becomes `Tier * per-tier HP`, so passive repair cannot restore destroyed layers. `HighestPurchasedTier` remains unchanged by damage.

Effective capacity is `capacity(Tier-1) + ceil((capacity(Tier)-capacity(Tier-1)) * top-layer HP / per-tier HP)`. Scaling the whole surviving tier by its own HP fraction would cause capacity to jump upward at some loss boundaries; incremental layers avoid that. A non-tier-losing hit preserves active projects. Single/multi-tier losses reduce access and destroy all projects once. Rebuilding adds one fresh layer with no antimatter refill. HP, capacities, prices and times remain provisional balance values.

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

### Implemented Lifecycle And Integration Surface

- Every base owns `BaseConstruction`, advanced by its physics tick. Immutable catalog definitions specify ID/name, slot, nonnegative antimatter/material costs (at least one positive), positive duration, required surviving tier and optional completed-project prerequisite. Unknown, unaffordable, occupied, duplicate, completed or prerequisite-blocked requests reject without either payment. Existing global pause freezes timers; active gameplay advances them in real time.
- Base projects use `TryStartNextTier`; provisional tier 2/3/4/5 prices are **100/150/200/250 antimatter + 20/30/40/50 matter** and times **20/30/40/50 seconds**, independently configurable. Rebuilds use the same duration and half-price rounding independently for each resource.
- Defense/resources/offense/ship slots accept registered content definitions. Non-repeatable completion records finished research once; repeatable projects can be ordered again after completion. The starter catalog below applies live repair/recharge/collector/Claim effects and launches offensive units. The Ship catalog also researches owned equipment and sequential ranks I–III; completion awaits explicit home installation. Completed research survives tier loss; starter non-ship effects suspend below prerequisites, while equipment ownership and installed ranks persist independently.
- Projects expose immutable payment/definition snapshots, progress, target tier/rebuild flag, and a unique instance ID. `Cancel(slot, instanceId)` cannot accidentally cancel a replacement project from a stale UI action. Active/completed/cancelled/destroyed outcomes are exclusive; terminal projects clear their slots, with no queue or replayed completion/refund.
- Cancellation deposits the exact payment and spills owned overflow through the existing grounded/radial fragment path. Tier loss destroys all categories synchronously before later timer completion, including the base/rebuild slot; investment neutral drops are separate from storage drops.
- Player-facing remote orders/cancellation are available through B. The basic economy-first rival now places paid orders through the same catalog/economy lifecycle, retaining a provisional 12-antimatter reserve. See `RIVAL_AI.md`; deeper strategic ordering remains iterative.

### Starter Catalog And Effects (2026-10-11)

| Project | Slot / required tier | Cost / time | Implemented effect |
| --- | --- | --- | --- |
| Repair matrix | Defense / 1 | 20 antimatter + 5 matter / 8s | Passive base repair ×1.5; never restores lost tiers. |
| Energy grid | Defense / 2 | 40 antimatter + 10 matter / 12s | Ship recharge ×1.25; completed research suspends below tier 2. |
| Collector drive | Resources / 1 | 25 antimatter + 5 matter / 10s | Home-base collector movement speed ×1.25, including existing collectors. |
| Strike drone | Offense / 2 | 60 antimatter + 15 matter / 15s | Repeatable one-shot unit flies from home to the opposing base, dealing 45 damage once. |
| Claim range calibration | Ship / 1 | 15 antimatter / 8s | Built-in RMB Claim range 120→150; no equipment slot or installation. |

Provisional definitions live in `BaseProjectCatalog.cs`. Effects are derived from completed research and surviving prerequisites rather than repeatedly mutating exported tuning; no double application occurs. Ship energy, collector movement, base repair and newly cast Claim projectiles use these live modifiers. The energy-grid suspension policy is provisional for this initial non-ship catalog; installed equipment retention remains governed separately by `SHIP_EQUIPMENT.md`.

Strike drones use original primitive visuals and spherical/flat routes at speed 18. Flat lifetime remains 60 seconds; spherical launch lifetime is max(60 seconds, surface route distance / speed + 15 seconds), so antipodal targets are reachable. They attach to the owning world, expire on target loss and hit once; completed expenditure is not refunded. Research requires a surviving rival base. Interception HP/collision combat and alternate targeting remain later content.

### Construction Accounting

`SpentAntimatter` is cumulative gross payment, **not** an additional physical reserve. `RefundedAntimatter` includes both refund deposits and owned overflow; `InvestmentSpilledAntimatter` records the investment fraction returned as neutral physical drops. `NetCommittedAntimatter = spent - refunded - investment spilled` includes active investment, completed expenditure and permanent losses. `WorldAntimatterTracker` aggregates these, active investment and `DestroyedInvestmentLoss` separately from loose/carried/storage/creature resources.

With no new supply or withdrawals into untracked destinations, `physical world total + net committed = supplied antimatter`. Transfers and ordinary storage spills leave that sum unchanged. Refunds and investment spills return only the actual payment fractions to physical supply. `WorldMatterTracker` independently applies the same identity to material crates (including attached cargo) and base matter; its accepted extractor supply, active investment and destroyed loss are separate diagnostics. Material has no creature reserves or energy-limit contribution.

Remote ordering is allowed. Research and construction run in real time, including while the management UI is open. Ship equipment installation/loadout changes require returning home.

## Cancellation

- Cancelling an active project refunds its full paid antimatter and matter investment, without a fee.
- Refund only what that project actually paid; a rebuild refunds its reduced rebuild price, not the normal tier price.
- Deposit the refund up to currently available storage space.
- Spill the remainder outside the base as faction-owned antimatter spheres and matter crates, available to collectors.
- Completing, cancelling and destroying a project are mutually exclusive outcomes; repeated callbacks cannot refund or spill it twice.

Construction frees storage space at purchase time. Collectors may refill that space before cancellation, making refund overflow a normal case to support.

## Siege And Project Destruction

When the base loses a tier, all projects currently under construction are destroyed, including projects in other categories and the base slot. Individual project structures/HP are not required for this initial rule.

For each destroyed project's paid resource, independently for antimatter and matter:

- 50% spills outside the base as unclaimed/neutral spheres or material crates, allowing rivals to claim it.
- The other 50% is permanently lost; it is not refunded to storage.
- A project is destroyed once even if one attack removes multiple tiers.

Implemented integer rounding: neutral investment spill is `floor(paid / 2)`; permanent loss is the exact remainder (`ceil(paid / 2)`). Thus a 51-antimatter payment spills 25 and loses 26. A one-antimatter payment spills zero and loses one. This is applied per project, not to an aggregated payment.

Base storage damage spills are separate from construction-investment spills. Reducing capacity spills any excess stored antimatter under the retained neutral-spill behavior. Do not count construction investment as stored antimatter or spill the same amount twice.

### Siege Example

A tier-4 base has active projects paid at 100 and 60 antimatter. An attack drops it to tier 3:

- Capacity and research access drop to tier 3.
- Stored antimatter exceeding the surviving capacity spills separately.
- Both active projects are destroyed: 50 + 30 antimatter spills, and 50 + 30 is permanently lost.
- Restoring tier 4 requires a new rebuild project at half its normal upgrade price.

## Rebuilding

- Rebuilding a previously purchased, destroyed tier costs 50% of its normal upgrade cost.
- Integer rebuild prices round **up**, so a normal 101 price rebuilds for 51. Cancellation/destruction use that snapshotted 51 payment, never 101. Rebuild duration currently equals the configured normal upgrade duration.
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

Implemented controls: **B / Esc** manages/closes the live panel. Five category tabs use icon tiles with name/rank, A/M/time and availability/progress states; hover gives effects, requirements and exact reasons, including disabled tiles. Active summaries show instance-safe full-refund cancel only while a project exists. Tier tiles switch to sequential rebuilds. HP/storage/energy/tier/site summaries update live, with damage/siege alerts and a consolidated dock badge. Ship separates **Research** from **Loadout**: four occupants/removal, owned/applied ranks, explicit destination/replacement selector and home-gated install tiles. Tooltips preserve full retention/suspension/energy warnings. See `COMPACT_UI.md`.

Management and the **M world survey both run in real time**. Only one owns ship input at a time; B/M transfer between them. Cursor and prior mouse mode restore on close. Look, movement and equipment input are blocked while open, but timers/recharge/AI/damage continue; drift decelerates normally. Held combat buttons must be released before firing resumes, preventing a closing click from activating equipment. F2 from management closes it and opens the existing paused setup menu; setup/terminal results remain pause owners, and live interfaces cannot open over them. R resets from either live interface; F6 from management returns to the lab. Survey consumes F2/F3/F6 until closed. Automatic closure on base loss/result preserves the terminal pause.

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

- Starter non-ship research effects use provisional suspension below prerequisites; future completed unit/defense upgrades need their own effect-retention policies. Installed ship equipment retention is already decided.
- Balance tuning of the implemented health-layer thresholds, rebuild times and repair rates; complete loss remains terminal.
- Pending completed ship research if the base is damaged or the player dies before installation.
- Matter costs, capacity, crate readability and nearest-resource/shared-cargo logistics balance from playtesting; richer material types/mining are later content.
- Rival research behavior, faction differences and eventual participant/networking scope.

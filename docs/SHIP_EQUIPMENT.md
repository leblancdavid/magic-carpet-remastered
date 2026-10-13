# Ship Equipment

## Status

Implemented 2026-10-11: owned equipment research, sequential ranks I–III, four-slot management UI, home installation, built-in Shift booster and independent exploration boon modifiers. A new match owns/installs Plasma Bolt I only. Claim and booster remain built in. See `BASE_DEVELOPMENT.md` for construction and `EXPLORATION_BOONS.md` for match bonuses; economic capture points are next.

## Four-Slot Loadout

- Each player starts with the basic projectile weapon (legacy Firebolt); its new display name is selected in the terminology pass.
- The ship can equip up to four items, not four weapons plus additional slotted utilities.
- Offensive weapons, defensive equipment such as shield/heal, and strategic equipment such as Guardian/Dash all compete for those four slots.
- The ship UI lists owned equipment and lets the player choose the equipped set.
- When all four slots are occupied, turn off one row toggle before enabling another.
- Unequipped items remain owned for the match and can be re-equipped at the base without paying to build them again.
- Installation and loadout changes require returning to the player's home base; remote research ordering is allowed.

**1–4 selects the corresponding equipped slot; LMB activates the selected item.** Selection never casts or spends energy. Tab cycles occupied slots in slot order, skipping empties. Selecting an empty slot displays Empty and LMB does nothing. Q/E/F/G no longer directly activate gameplay equipment (the separate flight lab retains F dash). Ship upgrade rows have home-only equip toggles; enabled items are automatically assigned contiguous keys 1–4 in displayed row order (Plasma, Pulse, Shield, Dash, Guardian). Compaction preserves the selected item's identity, or selects the first remaining item if it was removed. There is no separate Loadout tab or replacement selector. Explicit slot APIs remain for rival policy/diagnostic fixtures.

## Ownership And Death

Keep researched/built ownership separate from the equipped loadout and base research prerequisites.

- Built equipment and its purchased ranks remain owned through player death and respawn.
- Respawn preserves the equipped loadout; dying does not require rebuilding equipment.
- Base-tier loss does not remove or disable installed equipment, even if its research requires a higher tier.
- Equipment still requires sufficient current energy to activate.
- A firing cost above the player's current energy limit makes an installed item unusable until more antimatter is delivered. This is the intended economic consequence of base damage or construction spending.
- Ownership is match-level for now. Campaign persistence is a separate milestone; restart begins a fresh match.

## Research And Sequential Ranks

- Higher base tiers unlock research for new equipment.
- Each item can have two or three sequential upgrade steps; the exact number per item is tunable.
- Players can improve an existing lower-tier item instead of purchasing a higher-tier alternative.
- Initial upgrades are sequential ranks, not branching selections among damage/range/efficiency options.
- Equipment tier and equipment upgrade rank are distinct concepts: a rank upgrade need not turn one item into a different tier's weapon.
- Research pays stored antimatter through the ship construction slot, reducing the player's energy limit.
- Completion makes the equipment/upgrade ready for home-base collection/installation; it does not auto-install remotely.

Equipment unlocks and ranks come exclusively from base research. Random destructible wrecks now replace legacy gold beacons, granting secured permanent stacking match boons; they cannot unlock items or purchase ranks. See `EXPLORATION_BOONS.md`.

### Provisional Catalog And Installation Rules

All items have ranks I–III. Plasma Bolt I is free starter equipment; every later project uses the Ship slot, requires the previous researched rank and is non-repeatable. Definition tuning lives in `scripts/equipment/ShipEquipmentCatalog.cs`.

| Item | Rank-I cost / tier | Rank-II cost / tier | Rank-III cost / tier |
| --- | --- | --- | --- |
| Plasma Bolt | Starter / 1 | 60 / 2 | 90 / 3 |
| Pulse Burst | 50 / 1 | 100 / 2 | 150 / 3 |
| Energy Shield | 40 / 1 | 80 / 2 | 120 / 3 |
| Dash | 40 / 1 | 80 / 2 | 120 / 3 |
| Guardian Drone | 80 / 2 | 160 / 3 | 240 / 4 |

Research durations are **60 / 90 / 120 seconds** for ranks I / II / III before research-station bonuses (2026-10-12 playtest tuning). The Ship view has one row per item with equip toggle/assigned key and fixed I/II/III columns; Plasma I is explicitly Starting/Owned. Equipped items with unapplied research show a home-only **Apply rank** button in their row. Enabling an unequipped item applies its latest owned rank. Activation costs/cooldowns retain the baseline: Plasma 5/0.25s, Burst 18/2.5s, Shield 14/6s, Dash 10/3s, Guardian 24/8s. Rank effects: Plasma damage 25/35/45; Burst damage 32/40/48 and radius 8/10/12; Shield duration 4/6/8s; Dash speed 46/55.2/64.4; Guardian damage 12/16/20 and lifetime 12/18/24s. No damage-specialization taxonomy is introduced yet.

Installation is allowed while alive, in unpaused live gameplay, within **18 surface units** of the living home base and **16 elevation units** of its origin. Spherical distance/elevation use the arena's radial helpers; flat gameplay uses horizontal distance/height. Distance is exported on `ShipFlightController`. No automatic installation occurs on completion, return, death or respawn. Completed research permanently records ownership on the home construction state, even through tier loss; the ship stores installed ranks separately. Applying a rank uses the latest researched rank. Previously researched items can be installed after losing their prerequisite tier. Completed uninstalled items/ranks survive death and tier loss; active projects still obey siege destruction/refund rules.

## Boon Modifiers

Boons can influence equipment selection through speed, recharge, fire rate, projectile speed, specialized damage and HP modifiers. They stack, persist through death and last for the match, independently of the owned equipment inventory and four-slot loadout. Unequipping equipment does not consume its applicable boons. Display purchased rank and boon-derived effective modifiers separately.

Initial boon attribution: Plasma Bolt and summoned Guardian use energy damage, Pulse Burst uses explosive damage, and Guardian additionally has a deployable source tag. Ten capped additive +10% stacks per family multiply independent factors after researched rank, rounding integer damage once. Mobility affects normal/boost/ranked Dash speeds; recharge multiplies Energy grid; fire-rate/projectile-speed apply to Plasma Bolt. H exposes current stacks/effective stats. Broader kinetic/new-item taxonomy and base-HP boons remain deferred.

## Built-In Claim

- Everyone has Claim on RMB by default.
- Claim never occupies one of the four equipment slots and cannot be displaced by a loadout choice.
- Optional research improvements can enhance it through ship development.
- Continue using the existing aimed, surface-following resource-claim projectile in the terminology-only pass.
- Claim stays available when base storage is full; collectors wait for storage space.

The live-management starter catalog includes **Claim range calibration**: 30 antimatter, 60 seconds, tier 1; new Claim projectiles gain 25% range (120→150). Its separate built-in research row does not create an owned equipment item, occupy a slot or require loadout installation. Additional Claim upgrades/ranks remain undecided.

## Built-In Booster

- A fresh Shift press activates a temporary short burst of forward, surface-relative thrust; holding Shift/key echo cannot repeat it automatically.
- The booster is built into every ship and does not occupy an equipment slot.
- It costs a small amount of current antimatter energy, competing with attacks and other equipment use.
- Boosting does not consume stored construction resources or permanently lower the energy limit.
- Provisional defaults: **36 units/second**, **0.65 seconds** of thrust, **4 current energy**, **2-second cooldown from activation**. Tuning is exported on `ShipFlightController`. Recharge follows the normal storage-backed rules during/after boost; no resource refill occurs.
- Thrust follows ship heading, ignores camera pitch and does not add diagonal input speed or special lift. Shared hover suspension/gravity remains active and preserves jump/ramp vertical momentum. Space is a paid jump; C no longer controls gameplay altitude. After thrust ends, normal acceleration/drag governs horizontal momentum.
- Pause freezes thrust/cooldown. Management/survey input ownership ends active thrust and blocks activation; closing UI with Shift held does not retrigger. Death ends thrust; respawn clears booster timers. Insufficient energy rejects atomically without starting cooldown.
- HUD shows active time, cooldown or energy requirement. The Ship tab lists the built-in mobility tuning outside the equipment slots.

## Dash Equipment

Dash occupies a slot and costs **10 energy** with its existing **3-second cooldown**. It delivers an immediate aimed tangent impulse at rank speeds **46 / 55.2 / 64.4**; normal acceleration/drag and radial gravity follow. Lift is at least **5 units/second**, with aimed lift capped at the ordinary jump launch speed (default **12**); existing greater ramp momentum is preserved. Activating Dash leaves hover support, clears buffered jumps and ends active boost so speeds cannot stack and booster thrust cannot overwrite the Dash impulse. This contrasts with the cheaper short forward thrust, which provides no special lift. Research choices remain sequential ranks, with speed improving 20% of rank-I speed each rank; branching upgrades are deferred. Initial prices/effects remain provisional pending hands-on balance feedback.

## Built-in Jump And Hover — 2026-10-12

Space is a fresh-press jump costing **2 current energy**, independent of slots/storage/construction. Level-ground launch is **12 units/second** under **18 units/second² radial gravity**, about **4 m above 2 m hovering**. Damped suspension follows gentle terrain/water; steep uphill faces block traversal. Natural ramp/drop launches are free and preserve momentum. No fall damage or airborne ground-relative ceiling clamp. Short **0.12s input buffer / 0.1s edge grace** permits forgiving launches without double jumps. Pause/UI/death reject payment; UI/death/respawn clear pending input. Both factions share movement/action rules. F6's diagnostic lab retains its old altitude controls. See `TERRAIN_HOVER_PLAN.md` for later compact terrain/routing/difficulty stages and verification.

## Ship UI

At the base, show:

- Owned equipment list with type, rank, activation cost and effect summary.
- Equipment-row toggles, assigned keys and a four-item cap; empty slots remain visible on the flight HUD.
- Completed research awaiting installation.
- Research/rank prerequisites and unavailable reasons.
- Current energy and energy limit, including a clear warning when an item's cost exceeds the limit.
- Built-in Claim and booster separately from the four-slot loadout.
- Applicable boon modifiers and effective stats separately from purchased equipment ranks.

Bindings use 1–4 selection, LMB activation and Tab cycling. Management remains real time and blocks selection/activation while handling GUI input. The HUD shows the selected slot number/item and selection/activation hints. The Ship tab displays owned/installed ranks, pending installation, kind, cost, effect summaries, four slots and cost-above-limit warnings.

## Verification Requirements

- Starter equipment and built-in Claim are available in a fresh match.
- At most four distinct equipment items can be equipped; utilities count toward the same limit.
- Replacing/re-equipping retains ownership/ranks and does not charge construction costs again.
- Remote completion does not change equipment until home-base installation.
- Death/respawn preserves ownership, ranks and the equipped set.
- Base-tier loss blocks research prerequisites but leaves installed equipment intact.
- Current-energy and maximum-limit activation checks reject unaffordable use without removing items.
- Sequential research cannot skip ranks or purchase the same rank twice.
- Shift boost consumes current energy, respects pause/death/input ownership and remains distinct from Dash.
- Claim remains RMB and outside loadout limits before/after installation and respawn.
- Exploration rewards change boon state rather than unlocking equipment or purchasing ranks; researched and boon modifiers combine once under explicit formulas.

## Remaining Decisions

- Damage/source taxonomy for future equipment and base-HP layer policy; current boon interactions are explicit in `EXPLORATION_BOONS.md`.
- Hands-on catalog, installation-distance, UI scrolling and rank/cost balance.
- Hands-on booster/Dash cost, duration, cooldown and movement balance; wreck/boon pacing and modifier balance.

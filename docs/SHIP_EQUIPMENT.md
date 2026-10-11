# Ship Equipment

## Status

Approved design, 2026-10-11; implementation pending. Existing combat/utility abilities and exploration upgrades remain the current playable baseline until this milestone replaces their progression/loadout rules. See `BASE_DEVELOPMENT.md` for construction costs, tier access and the ship research slot.

## Four-Slot Loadout

- Each player starts with the basic projectile weapon (legacy Firebolt); its new display name is selected in the terminology pass.
- The ship can equip up to four items, not four weapons plus additional slotted utilities.
- Offensive weapons, defensive equipment such as shield/heal, and strategic equipment such as Guardian/Dash all compete for those four slots.
- The ship UI lists owned equipment and lets the player choose the equipped set.
- When all four slots are occupied, adding an item requires choosing which item to replace.
- Unequipped items remain owned for the match and can be re-equipped at the base without paying to build them again.
- Installation and loadout changes require returning to the player's home base; remote research ordering is allowed.

The legacy five-ability LMB/Tab cycle is preserved after the completed terminology conversion. A four-slot limit must be implemented with the new inventory/loadout UI rather than silently dropping an existing ability.

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

Approved progression update (2026-10-11): equipment unlocks and equipment ranks come from base research. Random exploration rewards grant permanent stacking boons, not equipment or equipment ranks; see `EXPLORATION_BOONS.md`. The current rank-II beacons remain functional through the behavior-preserving rename and are replaced in the boon milestone. Do not treat a boon modifier as a purchased rank or apply the legacy discovery bonus a second time.

## Boon Modifiers

Boons can influence equipment selection through speed, recharge, fire rate, projectile speed, specialized damage and HP modifiers. They stack, persist through death and last for the match, independently of the owned equipment inventory and four-slot loadout. Unequipping equipment does not consume its applicable boons. Display purchased rank and boon-derived effective modifiers separately.

Damage types/source tags and modifier stacking formulas are not finalized. Define them with the initial equipment catalog before implementing damage-specialization boons.

## Built-In Claim

- Everyone has Claim on RMB by default.
- Claim never occupies one of the four equipment slots and cannot be displaced by a loadout choice.
- Optional research improvements can enhance it through ship development.
- Continue using the existing aimed, surface-following resource-claim projectile in the terminology-only pass.
- Claim stays available when base storage is full; collectors wait for storage space.

The live-management starter catalog now includes **Claim range calibration**: 15 antimatter, 8 seconds, tier 1; new Claim projectiles gain 25% range (120→150). This built-in research does not create an owned equipment item, occupy a slot or require loadout installation. Additional Claim upgrades/ranks remain undecided.

## Built-In Booster

- Shift activates a temporary short burst of speed.
- The booster is built into every ship and does not occupy an equipment slot.
- It costs a small amount of current antimatter energy, competing with attacks and other equipment use.
- Boosting does not consume stored construction resources or permanently lower the energy limit.
- Speed, duration, cost, cooldown/retrigger rules and interaction with other mobility equipment are tunable.
- This is a new mechanic, not part of the behavior-preserving terminology conversion.

## Dash Equipment

Dash remains a stronger mobility option than the built-in booster and occupies an equipment slot. It has its own energy cost and upgrade ranks. Preserve its existing behavior through the rename; tune its distinction from the booster when implementing Shift boost.

## Ship UI

At the base, show:

- Owned equipment list with type, rank, activation cost and effect summary.
- Four equipped slots, including empty slots and explicit replacement selection.
- Completed research awaiting installation.
- Research/rank prerequisites and unavailable reasons.
- Current energy and energy limit, including a clear warning when an item's cost exceeds the limit.
- Built-in Claim and booster separately from the four-slot loadout.
- Applicable boon modifiers and effective stats separately from purchased equipment ranks.

The exact activation bindings for four equipped items remain an implementation/UI decision. Retain legacy bindings until a replacement is designed. Management remains real time and must prevent clicks/keys intended for the panel from activating equipment.

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

- Installation range/docking interaction and how completed research waits at the base.
- Four-slot activation bindings and whether remote UI may preview loadouts without applying them.
- Handling completed but uninstalled items/ranks during base damage or player death.
- Damage types/source tags and researched-stat interactions with boon stacking.
- Initial equipment catalog, ranks, energy costs and booster/Dash balance.

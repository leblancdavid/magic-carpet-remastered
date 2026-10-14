# Compact Pre-Playtest UI

Redesigned from user playtest feedback, 2026-10-12. See `PLAYTEST_SIMPLIFICATION.md` for approved economy/density/UI rules and `PROGRESS_LOG.md` for verification. Hands-on readability remains iterative.

- The entire bottom HUD is centered: compact gauges left, 4×1 equipment bar middle, radar/base HP right, controls/B/M/H beneath weapons. `FlightHudLayout` shares offsets between native controls and the drawn radar. Cursor-visible slot clicks select only and suppress held combat until release.
- Three narrow vertical gauges show current values only, with **H/E/A** placeholders beneath. Health changes green/yellow/red at 50%/25%; energy is blue and storage amber. Gauge fills retain their real maxima.
- Capture and respawn have dedicated contextual displays. Routine action/reward/pickup/raid toast messages are removed. Actual health hits briefly darken the world behind readable HUD controls and shake either camera mode for 0.28s.
- H presents three short boon choices, selectable Reject and Confirm. Selection highlights without applying; confirmation consumes one reward and closes. H/Esc defers, further offers wait for H, and there is no empty/second summary panel. Full effects/stacks remain in tooltips/F3.
- Management shows resources, surviving tier and a base upgrade/rebuild button above four tabs: Offense, Defense, Resources, Ship. Five backend slots remain concurrent, represented by progress on their buttons and tab titles.
- Ship research and equipment selection share one view. Equip/Unequip beneath tiles selects a four-item plan remotely; returning home automatically installs it and newest researched levels. Passives say Passive. Preserve selected active-item identity and scroll state.
- Use original procedural line glyphs and consistent normal/hover/selected/disabled styling. No imported art is required. Tooltip width is bounded; small viewports retain fixed tier-row columns with vertical scrolling.
- Short local capture/wreck prompts supplement full survey details. Panels remain real time; GUI presses cannot leak into global weapon polling. Verify actual viewport clicks, unavailable tooltips, selection/cooldowns, live timers and rendered normal/small layouts before recording completion.

## Implemented controls and presentation

- `EquipmentBar` has four 88×98 cards in one row, original glyphs, labels 1–4, Roman rank/energy cost, cyan selected outline, Ready/Empty/Down text, real cooldown shading/countdown and amber energy-shortfall state. Unaffordable cards stay selectable. 1–4 selects, LMB activates, Tab cycles; cursor-visible mouse-down selects only and latches combat until release.
- Basic hints remain outside slots. Pending planned equipment/rank changes add amber update glyphs to affected active cells and a Return home line. Undoing changes clears it; active items remain displayed. Bar/gauges/base HP/B/M/H hide behind interfaces/setup/results.
- A = antimatter, E = rechargeable energy. Energy and stored resource gauges are independent. Death/respawn feedback is contextual; detailed status remains in tooltips/F3.
- Radar is **176×176**, with 72-pixel radius and 220m live ship sensing. Maps show home/owned points globally and remembered rival/scout-charted point locations without distant live details; other actors need sensing. Neutral mobs/AM use black/orange, resources/units faction colors. Waypoints remain in expanded survey.
- Construction buttons show A/time, faded unavailable states, Roman required-tier badges and a translucent build-progress overlay. Active buttons show remaining time and an instance-specific × refund cancel. Header base button follows sequential advancement/rebuilding. Tooltips retain full requirements/effects; tab titles show other categories' active percentage.
- Every tab has tier I–V rows. Offense/Defense/Resources use three type columns; Ship four. One tile buys the next sequential level: unbuilt has no star, bronze ★ / silver ★★ / gold ★★★. Metal-colored stars show researched ownership, with separate progress/cancel overlays. Pending content is disabled, marked Coming later and not registered for payment.
- Ship Equip/Unequip buttons plan remotely; a fifth choice requires removing another. Home physics automatically synchronizes latest ranks and the planned set without Apply. Paused/dead ships cannot change plans. Ship passives have no loadout slot. Direct home-only slot APIs remain for tests/rival policy.
- Separate Deploy/Launch buttons open a modal eligible-target dropdown, exact AM/time and confirmation; Back/Esc dismisses without payment. A backdrop blocks underlying input. Targets are revalidated at confirmation/completion; target loss cancels with an exact refund. Production progress/cancel overlays its capability tile. Menus remain real time and own ship input. No implicit attack occurs on research completion.
- A screen-space capture bar above equipment shows stage and percentage inside a capture ring without camera aiming. Departure hides it while progress drains; contested/restoring/owned feedback uses the authoritative site state. Live panels/pause/death/results hide it. World capture text/map details remain. Wreck/salvage labels are shorter within 55 units; discovery stays 120 and collection rules stay unchanged.

## Verification and next work

`CompactUiChecks.tscn` covers real viewport clicks, held-LMB suppression, sequential metal stars/payments/refunds, progress, four tier-row tabs/header, faded Roman gates, gauges, live timers, remote equipment planning/fifth-item rejection/selection, automatic rank synchronization, tooltips, pending content and both supported canvas sizes. `UpgradeTreeChecks` covers target orders/capture drones and energy/cooldown invariants. Captures cover HUD, Ship grids, active construction, pending tooltips and the paid target dialog.

New UI runs exit cleanly after queued teardown/physics drain. An equipment-reload harness needed the same cleanup; some legacy suites retain exit warnings. No engine-wide crash fix, final UI acceptance or new frame-time benchmark is claimed.

Next: a fresh Medium match targeting economy-focused tier V in 10–15 minutes. Report hunting/hauling, purchase/energy, capture and siege pacing, readability and rival resilience. Accessibility/controller support and original art remain iterative.

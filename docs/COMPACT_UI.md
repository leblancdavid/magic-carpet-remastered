# Compact Pre-Playtest UI

Redesigned from user playtest feedback, 2026-10-12. See `PLAYTEST_SIMPLIFICATION.md` for approved economy/density/UI rules and `PROGRESS_LOG.md` for verification. Hands-on readability remains iterative.

- The entire bottom HUD is centered: compact gauges left, 4×1 equipment bar middle, radar/base HP right, controls/B/M/H beneath weapons. `FlightHudLayout` shares offsets between native controls and the drawn radar. Cursor-visible slot clicks select only and suppress held combat until release.
- Three narrow vertical gauges show current values only, with **H/E/A** placeholders beneath. Health changes green/yellow/red at 50%/25%; energy is blue and storage amber. Gauge fills retain their real maxima.
- Capture and respawn have dedicated contextual displays. Routine action/reward/pickup/raid toast messages are removed. Actual health hits briefly darken the world behind readable HUD controls and shake either camera mode for 0.28s.
- H presents three short boon choices, selectable Reject and Confirm. Selection highlights without applying; confirmation consumes one reward and closes. H/Esc defers, further offers wait for H, and there is no empty/second summary panel. Full effects/stacks remain in tooltips/F3.
- Management shows resources, surviving tier and a base upgrade/rebuild button above four tabs: Offense, Defense, Resources, Ship. Five backend slots remain concurrent, represented by progress on their buttons and tab titles.
- Ship research and equipment selection share one view. Click owned equipment tiles to toggle at home, up to four items with row-order keys; pending equipped ranks have explicit Apply actions. Preserve selected item identity and scroll state.
- Use original procedural line glyphs and consistent normal/hover/selected/disabled styling. No imported art is required. Tooltip width is bounded and small viewports use fewer grid columns/scrolling.
- Short local capture/wreck prompts supplement full survey details. Panels remain real time; GUI presses cannot leak into global weapon polling. Verify actual viewport clicks, unavailable tooltips, selection/cooldowns, live timers and rendered normal/small layouts before recording completion.

## Implemented controls and presentation

- `EquipmentBar` has four 88×98 cards in one row, original glyphs, labels 1–4, Roman rank/energy cost, cyan selected outline, Ready/Empty/Down text, real cooldown shading/countdown and amber energy-shortfall state. Unaffordable cards stay selectable. 1–4 selects, LMB activates, Tab cycles; cursor-visible mouse-down selects only and latches combat until release.
- Basic 1–4/LMB/RMB/WASD/Shift/Space/F3 hints remain outside slots. Bar, gauges, base HP and B/M/H hide behind interfaces/setup/results; pending count and damage badge use that row.
- A = antimatter, E = rechargeable energy. Energy and stored resource gauges are independent. Death/respawn feedback is contextual; detailed status remains in tooltips/F3.
- Radar is **176×176**, with a 72-pixel radius and 220 m sensing. Both maps show home/owned points globally, remembered discovered rival-base locations and other actors only in ship sensing. Neutral mobs/antimatter use black/orange dots; claimed resources use faction colors. Waypoint details remain in expanded survey.
- Construction buttons show A/time, faded unavailable states, Roman required-tier badges and a translucent build-progress overlay. Active buttons show remaining time and an instance-specific × refund cancel. Header base button follows sequential advancement/rebuilding. Tooltips retain full requirements/effects; tab titles show other categories' active percentage.
- Ship rows use clickable owned tiles and numbered equipped state. A fifth item is unavailable until another is disabled. Keys compact in row order, preserving selection. Home/pause/death gates remain. **Apply rank** appears for equipped items with completed unapplied research.
- General grids adapt from two to six columns. Ship Research instead preserves **Equipment / I / II / III** columns and one row per item, plus built-in Claim; Plasma I shows Starting/Owned. Minimum label widths prevent collapsed wrapped headings; vertical scrolling keeps rows aligned at 1280×720 and 960×540. `UiIcon` art requires no imported/generated assets. StyleBox overrides use scoped owners; HUD frames are reused.
- A screen-space capture bar above equipment shows stage and percentage inside a capture ring without camera aiming. Departure hides it while progress drains; contested/restoring/owned feedback uses the authoritative site state. Live panels/pause/death/results hide it. World capture text/map details remain. Wreck/salvage labels are shorter within 55 units; discovery stays 120 and collection rules stay unchanged.

## Verification and next work

`CompactUiChecks.tscn` covers viewport clicks, held-LMB suppression, antimatter-only order/cancel, overlaid progress, four tabs/header, faded Roman-tier gates, three gauges, live timers, clickable owned equipment, fifth-item rejection, selection compaction, rank application, home/remote states, cooldowns/tooltips/interface hiding and both supported canvas sizes. Captures cover bottom HUD, Ship rows, active construction and disabled tooltips.

New UI runs exit cleanly after queued teardown/physics drain. An equipment-reload harness needed the same cleanup; some legacy suites retain exit warnings. No engine-wide crash fix, final UI acceptance or new frame-time benchmark is claimed.

Next: a fresh Medium match targeting economy-focused tier V in 10–15 minutes. Report hunting/hauling, purchase/energy, capture and siege pacing, readability and rival resilience. Accessibility/controller support and original art remain iterative.

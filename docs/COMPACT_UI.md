# Compact Pre-Playtest UI

Phase 4 implemented and regression-verified, 2026-10-11. All four readiness phases are complete for their first pass; next is hands-on economic/combat/readability balance. Existing real-time input ownership and gameplay rules are preserved.

- A bottom-centred 4×1 equipment bar shows number, original glyph, short name/rank, selected outline, cooldown overlay and energy-unavailable state. Number keys/Tab retain selection and LMB activation; cursor-visible slot clicks select only and suppress held combat until release. Claim/Shift indicators remain outside the four slots.
- Compact health/energy/base/resources at the edge, consolidated B/M/H entry controls and F3 help. Hide redundant flight HUD controls behind live panels/setup/results. Feedback remains transient; pending rewards and siege get concise badges.
- Management retains five categories/active projects. Replace verbose catalog rows with icon tiles showing name/rank, compact costs and availability/progress. Hover explains effects, duration, tier/project requirements, exact unavailable reasons and completed/suspended states, including disabled tiles.
- Ship Research and Loadout are separate views. Explicit destination/replacement selector and owned-item install tiles remain home-gated; removal stays free. Preserve control identity/scroll/tab state during refresh.
- Use original procedural line glyphs and consistent normal/hover/selected/disabled styling. No imported art is required. Tooltip width is bounded and small viewports use fewer grid columns/scrolling.
- Short local capture/wreck prompts supplement full survey details. Panels remain real time; GUI presses cannot leak into global weapon polling. Verify actual viewport clicks, unavailable tooltips, selection/cooldowns, live timers and rendered normal/small layouts before recording completion.

## Implemented controls and presentation

- `EquipmentBar` has four 88×98 cards in one row, original glyphs, labels 1–4, Roman rank/energy cost, cyan selected outline, Ready/Empty/Down text, real cooldown shading/countdown and amber energy-shortfall state. Unaffordable cards stay selectable. 1–4 selects, LMB activates, Tab cycles; cursor-visible mouse-down selects only and latches combat until release.
- Claim/Shift cost/readiness are outside the slots. Bar and B/M/H row hide behind interfaces/setup/results; pending count and damage badge use that row. Narrow canvases move it above the bar. Old separate hints and verbose loadout text are removed.
- Vitals show HP/energy, base tier/HP and A/M reserves; shield/respawn text is contextual. A = antimatter, M = matter, E = energy. Details remain in management/tooltips/F3. The short F3 hint avoids wrapping.
- Radar is a 196×224 panel with 72-pixel radius, 220-unit range header and two-line guidance. Live tracking, markers, reveal, surface distance/bearing and full survey behavior remain; radar hides behind other panels/setup/results.
- B retains five categories/concurrent summaries. Cancel appears only for an active instance, with exact refund/siege tooltip. Tiles show short name/rank, A/M/time and Research/Locked/Owned/Completed/Suspended/progress. Tooltips include full effects, costs, human-readable prerequisites, exact unavailable reason and retention/suspension, including disabled tiles.
- Ship separates **Research** and **Loadout**. Loadout shows four occupants/removal, explicit replacement selector and owned-item install tiles. Home range, pause/death gates, free replacement and ownership remain; refresh preserves controls/view/scroll/selector.
- Grids adapt from two to six columns and scroll instead of enlarging the window. `UiIcon` art requires no imported/generated assets. StyleBox overrides use scoped owners; HUD frames are reused.
- Capture text is compact within radius + 12 (30 globe units), with details on map hover. Wreck/salvage labels are shorter within 55 units; discovery stays 120 and collection rules stay unchanged.

## Verification and next work

`CompactUiChecks.tscn` covers viewport clicks, held-LMB suppression, dual-resource order/cancel, live timers, research/install separation, home/remote states, cooldown/energy display, tooltips, interface hiding and 1280×720 / 960×540 bounds/columns/dock separation. Captures cover HUD, both Ship views, a disabled tooltip and small layout; equipment/management GUI regressions verify actual installation/cancellation.

New UI runs exit cleanly after queued teardown/physics drain. An equipment-reload harness needed the same cleanup; some legacy suites retain exit warnings. No engine-wide crash fix, final UI acceptance or new frame-time benchmark is claimed.

Next: a fresh 15–20-minute Medium match. Report slow/confusing/overpowered moments: research/install, construction/energy, site travel/value, raids, rival development, wrecks and booster/Dash. Icon polish, accessibility/controller support, denser scenery and balance remain iterative.

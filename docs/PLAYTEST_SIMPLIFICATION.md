# Dense Encounters, Single-Resource Economy And Bottom HUD

User-approved playtest pass, 2026-10-12, followed by centered HUD/combat/recovery/wandering refinements on 2026-10-13. This takes priority over further rival routing/difficulty work. The economy-focused **Medium tier-V target is 10–15 minutes**; the numbers below are implemented first-pass tuning, not measured human progression acceptance.

## Economy And Siege

- Antimatter is the only construction resource. Scrap matter, crates, mixed cargo, material costs/refunds/ledgers and material opening routes are removed. Both factions begin with 30 antimatter; Claim and collector delivery remain the acquisition loop.
- Eight capture points: **four antimatter extractors, two research stations, two logistics stations**. The former material extractors keep their mirrored geographic positions and become antimatter extractors.
- Each extractor generates **0.2 antimatter/s**, one unit per five seconds. Only fractional work accumulates; full storage discards it and does not accumulate a backlog. Capturing/research/logistics policies remain in `STRATEGIC_POINTS.md`.
- Tier capacities **200/350/550/800/1100**; tier-II/III/IV/V prices **150/275/450/650 antimatter** and base durations **60/80/100/120 seconds**. Advancement costs fit preceding capacities with some combat reserve. Tier V consumes 1,525 antimatter before optional research.
- Permanent infrastructure: Repair **70 A / 60s**; Energy grid **140 A / 90s / tier II**; Collector drive **90 A / 60s**; Claim range **50 A / 60s**. Equipment first purchases are 1.5× the preceding prices and later ranks 2×; see `SHIP_EQUIPMENT.md`.
- Base layers each have **400 HP**. Plasma deals **20% damage to bases**, rounded to an integer with a one-damage minimum, for either faction. Plasma costs **2 E**, deals **13/18/23** by rank and **3/4/5** to bases without boons. An intact tier-I base takes **134 rank-I hits**, roughly 33.5 seconds at the cooldown limit before energy/defenders matter.
- Both ships slowly recover **2 HP/s after five seconds without dealing or taking combat damage**. Pause freezes recovery; real-time menus do not. Player health hits have brief decaying screen shake/darkening, without changing flight/aim direction.
- Dedicated strike drones cost **100 A / 20s / tier II** and deal **120 base damage** once. Rare neutral raids retain four damage per three seconds, 180-second grace/interval and 90-second lifetime; stronger bases make their pressure gentler, requiring hands-on tuning.
- Existing storage-backed energy/recharge, exact refunds/owned overflow, per-project half-spill/half-loss, sequential half-price rebuilding and retained equipment rules still apply. Greater storage also increases rechargeable combat energy, so hunting, buying and fighting must be tuned together.

## Encounters And Replacement

- Population targets **96/180/300/450** on 200/300/400/500 m worlds. Most mobs belong to seeded random wild habitats rather than fixed routes.
- Bounded sampling cycles through twelve regions with varied **22–65 m** spacing and a mix of isolated centres/nearby clusters. Member footprints vary instead of fixed spawn rings. Base/scenery/support guards remain; land roles prefer dry wild locations. No unsafe fallback is used.
- Swarm habitats permit **6–10 easy 16-HP members**, ordinary beasts 2–5 42-HP members, casters 2–4 and scavengers 1–3. Heavy habitats permit 1–3 **150-HP**, 1.7×-size beasts with 18 contact damage and slower movement. Initial allocation seeds each habitat, then finishes packs within the global/role budget; some habitats retain smaller populations. Idle mobs choose independently seeded destinations, variable speeds and pauses, and repick blocked/stalled goals within home bounds.
- Live rewards are **six antimatter per swarm member, 36 per ordinary beast, 72 per heavy beast, twenty per caster/scavenger**. Scavengers retain stolen reserves in addition. More mob fragments release every 90 ms with varied outward scatter; see `PLAYTEST_FLIGHT_DROPS.md` for approved follow-up tuning and measured startup work.
- Habitats remember their seeded population. Defeated members recover after 20 seconds and replenish individually every five seconds. A 0.5-second census bounds population polling, while raids keep their per-physics-tick clock.
- Site guards have an explicit site reference. Either faction's ownership blocks replacements, leaving living guards intact. Neutralization restarts a full recovery delay. Wild habitats replenish independently.
- Replacements wait while a living pilot is within 64 m of the habitat centre; with the 32 m placement ring this keeps new bodies at least roughly 32 m away. Pause freezes clocks; live management does not.
- Ordinary territorial/return/retaliation rules remain. Proximity acquisition checks live pilot positions each tick while caching modern/legacy group membership for staggered 0.4–0.6s intervals, avoiding repeated native group arrays and quadratic neutral scans.

## HUD And Management

- The **entire bottom group is centered**, including compact gauges, four weapon cards and radar/base HP. Gauge numbers show only current values, with H/E/A placeholders beneath. HP is green above 50%, yellow at/below 50%, red at/below 25%; energy is blue and antimatter amber.
- The **176×176 radar** sits to the right of weapons within the centered group, with home-base HP beneath it. Sensing rules remain. Basic controls/B/M/H sit below weapons; F3 is on demand.
- Capture progress appears only in the local capture zone; a dedicated respawn countdown remains. Routine raid/reward/action/pickup toast messages are removed. Terminal victory/defeat still owns pause and shows its result.
- The boon panel has three concise options, selectable Reject and Confirm. Confirmation applies one choice and closes with no follow-up UI; H reopens further pending rewards. H/Esc defers, tooltips/F3 expose fuller bonus details.
- B header shows antimatter/capacity, surviving Roman tier and one upgrade/rebuild button. Underneath are **Offense / Defense / Resources / Ship** tabs. Five backend slots still allow independent concurrent projects, with the base slot represented in the header.
- Construction progress is painted over its order button, with percentage/remaining time. Tab titles indicate work in other categories. Each active button has an instance-safe × full-refund cancel. No separate active-project strip is needed.
- Unavailable projects are faded and show the required base tier in Roman numerals at top right; tooltips explain prerequisites/effects/prices. Rank numerals and unlock-tier numerals are distinct.
- Clicking an owned equipment tile toggles its installed state at home; equipped items display their numbered key. Four-item cap/row-order compaction/selected-item identity remain. Newly researched equipped ranks retain a compact explicit Apply action; enabling an unequipped item applies its latest owned rank.
- Responsive bounds, fixed equipment/rank columns, scrolling, real-time input ownership and held-combat suppression are verified at 1280×720 and 960×540.

## Verification And Follow-Up

Use construction/economy, neutral/site, equipment/rival, compact UI, layout/seed/navigation and terrain-access checks. `EncounterLoadChecks.tscn` runs a live Medium/XL population workload; optional `-- --medium` or `-- --xl` isolates a world. It records 480 warmed wall-frame samples plus Godot monitor observations and managed allocations. Monitor timing updates are coarse; rendered wall intervals are not identical to physics CPU duration. No hardware-independent FPS gate is implied.

Next hands-on checks: a fresh 10–15-minute Medium economy-focused run; hunting/hauling reward pace, research-versus-tier decisions, early siege resistance, capture safety and readability. Longer live XL sessions and remaining high-density frame spikes need follow-up. Rival navigation, sustained recovery/economic resilience and difficulty remain next; normal rival deaths/respawns are permitted and are not proof of full-match balance.

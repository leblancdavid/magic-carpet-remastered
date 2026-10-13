# Dense Encounters, Single-Resource Economy And Bottom HUD

User-approved playtest pass, 2026-10-12. This takes priority over further rival routing/difficulty work. The economy-focused **Medium tier-V target is 10–15 minutes**; the numbers below are implemented first-pass tuning, not measured human progression acceptance.

## Economy And Siege

- Antimatter is the only construction resource. Scrap matter, crates, mixed cargo, material costs/refunds/ledgers and material opening routes are removed. Both factions begin with 30 antimatter; Claim and collector delivery remain the acquisition loop.
- Eight capture points: **four antimatter extractors, two research stations, two logistics stations**. The former material extractors keep their mirrored geographic positions and become antimatter extractors.
- Each extractor generates **0.2 antimatter/s**, one unit per five seconds. Only fractional work accumulates; full storage discards it and does not accumulate a backlog. Capturing/research/logistics policies remain in `STRATEGIC_POINTS.md`.
- Tier capacities **200/350/550/800/1100**; tier-II/III/IV/V prices **150/275/450/650 antimatter** and base durations **60/80/100/120 seconds**. Advancement costs fit preceding capacities with some combat reserve. Tier V consumes 1,525 antimatter before optional research.
- Permanent infrastructure: Repair **70 A / 60s**; Energy grid **140 A / 90s / tier II**; Collector drive **90 A / 60s**; Claim range **50 A / 60s**. Equipment first purchases are 1.5× the preceding prices and later ranks 2×; see `SHIP_EQUIPMENT.md`.
- Base layers each have **400 HP**. Plasma deals **20% damage to bases**, rounded to an integer with a one-damage minimum, for either faction. Unmodified rank-I Plasma deals 25 to mobs/ships and five to bases; an intact tier-I base takes 80 hits, roughly 20 seconds at the cooldown limit before energy/defenders matter.
- Dedicated strike drones cost **100 A / 20s / tier II** and deal **120 base damage** once. Rare neutral raids retain four damage per three seconds, 180-second grace/interval and 90-second lifetime; stronger bases make their pressure gentler, requiring hands-on tuning.
- Existing storage-backed energy/recharge, exact refunds/owned overflow, per-project half-spill/half-loss, sequential half-price rebuilding and retained equipment rules still apply. Greater storage also increases rechargeable combat energy, so hunting, buying and fighting must be tuned together.

## Encounters And Replacement

- Population targets **64/120/200/300** on 200/300/400/500 m worlds. Most mobs belong to seeded random wild habitats rather than fixed routes.
- Bounded random sampling cycles through twelve globe regions with a 60 m habitat spacing target and base/scenery/support guards. Land roles prefer dry wild locations; station guards can occupy water-supported stations. No unsafe fallback is used.
- Swarm habitats permit **6–10 easy 16-HP members**, ordinary beasts up to three 42-HP members, caster groups up to three and scavengers up to two. Heavy beast habitats permit one/two **150-HP**, 1.7×-size beasts with 18 contact damage and slower movement. Initial allocation seeds each habitat, then finishes packs within the global/role budget; some habitats retain smaller populations.
- Live rewards are **three antimatter per swarm member, 18 per ordinary beast, 36 per heavy beast, ten per caster**. Scavengers retain their actual stolen reserve without minting intrinsic supply. Counts and drops are tuned together to avoid making larger swarms automatic economy jackpots.
- Habitats remember their seeded population. Defeated members recover after 20 seconds and replenish individually every five seconds. A 0.5-second census bounds population polling, while raids keep their per-physics-tick clock.
- Site guards have an explicit site reference. Either faction's ownership blocks replacements, leaving living guards intact. Neutralization restarts a full recovery delay. Wild habitats replenish independently.
- Replacements wait while a living pilot is within 64 m of the habitat centre; with the 32 m placement ring this keeps new bodies at least roughly 32 m away. Pause freezes clocks; live management does not.
- Ordinary territorial/return/retaliation rules remain. Proximity acquisition scans modern pilots plus the small `legacy_rival_pilot` diagnostic group, avoiding a quadratic scan of every neutral by every neutral.

## HUD And Management

- The bottom group contains three vertical HP/energy/storage gauges to the left of the four weapon cards. Numbers are current/maximum. HP is green above 50%, yellow at/below 50%, red at/below 25%; energy is blue and antimatter amber.
- The **176×176 radar** sits at the bottom right with a horizontal home-base HP bar beneath it. Its sensing/ownership/discovery rules remain unchanged. Basic controls and B/M/H appear below weapons. Persistent corner vitals/objective and planet-size hints are removed; F3 is on demand.
- Capture progress appears only in the local capture zone; raid/reward/action and death/respawn feedback are brief/contextual. Terminal victory/defeat still owns pause and shows its result.
- B header shows antimatter/capacity, surviving Roman tier and one upgrade/rebuild button. Underneath are **Offense / Defense / Resources / Ship** tabs. Five backend slots still allow independent concurrent projects, with the base slot represented in the header.
- Construction progress is painted over its order button, with percentage/remaining time. Tab titles indicate work in other categories. Each active button has an instance-safe × full-refund cancel. No separate active-project strip is needed.
- Unavailable projects are faded and show the required base tier in Roman numerals at top right; tooltips explain prerequisites/effects/prices. Rank numerals and unlock-tier numerals are distinct.
- Clicking an owned equipment tile toggles its installed state at home; equipped items display their numbered key. Four-item cap/row-order compaction/selected-item identity remain. Newly researched equipped ranks retain a compact explicit Apply action; enabling an unequipped item applies its latest owned rank.
- Responsive bounds, fixed equipment/rank columns, scrolling, real-time input ownership and held-combat suppression are verified at 1280×720 and 960×540.

## Verification And Follow-Up

Use construction/economy, neutral/site, equipment/rival, compact UI, layout/seed/navigation and terrain-access checks. `EncounterLoadChecks.tscn` runs a live Medium/XL population workload; optional `-- --medium` or `-- --xl` isolates a world. It records 480 warmed wall-frame samples plus Godot monitor observations and managed allocations. Monitor timing updates are coarse; rendered wall intervals are not identical to physics CPU duration. No hardware-independent FPS gate is implied.

Next hands-on checks: a fresh 10–15-minute Medium economy-focused run; hunting/hauling reward pace, research-versus-tier decisions, early siege resistance, capture safety and readability. Longer live XL sessions and remaining high-density frame spikes need follow-up. Rival navigation, sustained recovery/economic resilience and difficulty remain next; normal rival deaths/respawns are permitted and are not proof of full-match balance.

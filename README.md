# Magic Carpet Remastered

A Godot C# planetary-combat prototype moving toward an original futuristic faction-conflict setting. The repository title remains a working name; Magic Carpet is a historical mechanics reference.

## Current Status

The spherical mechanics are accepted as a playtest baseline. The latest follow-up adds **Harvester automatic mob farming/physical AM claiming, larger storage and collector cargo, faster extractors, half-capacity tier prices, stronger attack drones/mobs, improved habitat recovery and playable Scatter Cannon/Seeker Missile plus affordable body-aimed Pulse Burst**. Automatic fleets, Siphon raids, Claim magnetism, T Teleport Home and map readability remain. Medium tier V in **10–15 minutes** remains a provisional human-playtest target. Remaining tier-II Hunter/Rocket/Flak precede III–V; broader rival routing/difficulty and XL performance remain pending. See `docs/ECONOMY_COMBAT_PLAYTEST.md` and the progress log.

The default prototype is a spherical planet with ship combat, deformable land/oceans, radial antimatter spheres, Claim/collector hauling, bases, rival pilots, equipment and victory/defeat. Sizes are **Small 200 / Medium 300 / Large 400 / XL 500 m radius**, Medium default. Win by destroying the rival base and defeating its pilot, in either order. Resource possession alone is not victory. Art/audio remain placeholders.

See `docs/BUILD_ROADMAP.md` and `docs/PROGRESS_LOG.md` for priorities and verification history. The original DOS package at `D:\Games\MagicCarpet` is reference material; proprietary files are not copied into this repo.

Rendered combat profiling is available in `tools/CombatFrameProfile.tscn`. It runs Medium and XL workloads and saves per-frame CSV/summary reports under `user://combat-profiles`. Add `-- --antimatter-isolation` to compare frozen bodies, native physics/radial support, and full antimatter behavior in fresh 192-body fields; the previous `--mana-isolation` flag remains an alias. New reports use antimatter/claim/base terminology; historical reports retain their original columns. See `docs/COMBAT_PERFORMANCE.md` for commands, measurements, and interpretation.

Long-distance profiling is available in `tools/TraversalFrameProfile.tscn`: two globe laps with LOD/collision, draw-count, node and frame-time diagnostics. Add `-- --all --stress` for all four sizes at accelerated streaming pressure, or `-- --xl` for an XL dash-reference-speed run. Reports go to `user://traversal-profiles`. See `docs/TRAVERSAL_PERFORMANCE.md`.

## Requirements

- Godot 4.6.3 with .NET/C# support (matches the project SDK)
- .NET SDK

Godot is available locally at `D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe`; it is not on `PATH`.

## Structure

- `docs/` - discovery, design, and implementation plans
- `scenes/` - Godot scenes
- `scripts/` - C# gameplay code
- `assets/` - original/remastered assets
- `tools/` - local research/import helpers

## Run And Verify

**Compact UI:** centered H/E/A gauges, four equipment cards, radar/base HP and B/M/H. **1–4 selects / LMB activates / Tab cycles**; card clicks select without firing. **B** has tier-row next-star research, bronze/silver/gold stars and progress/refund controls. Equip/Unequip plans up to four items remotely; home auto-installs the plan/new ranks, with amber pending HUD indicators. Passives use no slot. **27 of 68 types are playable**, seven equipable ship items; pending tiles cannot take payment. Unit tiles show automatic status/post-gold expansion. See `docs/COMPACT_UI.md` and `docs/UPGRADE_TREE_PLAN.md`.

**Planet layout:** seeded continents/coasts/islands/mountains and genuine curvature; most land is gentle, roughness localized. Bounded validation enforces **3–5% land above 40°**, **≥85% below 20°** and protected normal-hover approaches before actors spawn. Factions start opposite with configurable **120° minimum separation**, equal **30 antimatter**, dry pads and home-relative ships/collectors. Five landmarks, eight stations, three antimatter fields, scenery, random wrecks and wild habitats span the globe. Independent checks cover **160** retained random-world opening routes and real normal-hover round trips. Runtime rival navigation remains next.

**Neutrals:** targets **72/135/225/338**, irregular groups/seeded wandering across twelve regions. Swarms **16 HP/8 contact**, beasts **50/15**, heavies **175/26**, casters **32 HP/14 ranged**. Wild first-vacancy recovery is **25s**, replacements **6s** apart; later kills do not reset it. Nearby ships block spawning; owned-site guards do not replenish. F3 exposes suppression/recovery counts. Habitat retaliation, committed return, scavenger theft and separate **180s grace/interval, 4 damage/3s, 90s** raids remain. See `docs/NEUTRAL_BEHAVIOR.md`.

**Rival:** the labelled magenta ship uses the same flight, energy, paid equipment, Claim/collector hauling, capture and respawn mechanics as the player. It gathers locally, captures income, pays for development, returns home to install, explores known wrecks and makes occasional attacks after a five-minute grace. It retains a provisional 12-antimatter reserve and uses basic commitment/retry/retreat rules. F3 BASES/ENEMIES reports its task, goal, energy and paid orders. Both factions' research/loadouts/boons survive death/tier loss. Strategy, wider equipment use and routing remain minimal; see `docs/RIVAL_AI.md`.

**Economic sites:** eight ring/cross markers. Ships capture neutral sites in **10s**, takeover in **10s + 10s**, within **12/18/18/18m** rings. Scouts capture at half speed without stacking; opposing ships/scouts contest. Benefits stop at neutralization. Four extractors produce **0.35 A/s** each, with **+20/40/60%** optimization. Two research stations add **25% speed** each; two logistics provide stealable collectors/cargo. Owned sites are global; charted locations retain no distant live details. Destroyed station collectors do not respawn.

**Single resource:** scrap matter, material crates/cargo/storage/costs and material routes are removed. Antimatter funds all construction and backs a separate rechargeable combat-energy quantity.

**Combat feel:** Plasma costs **2 energy**, dealing **13/18/23** damage at ranks I–III (3/4/5 against bases before boons). Both ships heal **2 HP/s after five seconds without dealing/taking combat damage**. Real-time menus continue recovery; pause freezes it. Player health hits briefly shake and darken either camera view.

**Weapon alternatives:** Pulse Burst **5 E/2.5s**, 32/40/48 damage in 8/10/12m, now aims at mob bodies as well as terrain. Tier-II Scatter Cannon **5 E/0.8s**, seven 8/11/14-damage close-range pellets. Tier-II Seeker Missile **7 E/1.8s**, 42/56/70 guided damage; 90m visible-target lock, bounded turning and 10-HP interception. Research and plan in B → Ship, then return home for installation.

**Shift boosts forward** at 36 units/second for 0.65 seconds, costing 4 current energy with a 2-second cooldown. Press again after releasing Shift; holding it does not repeat. Boost is built in and consumes no equipment slot or stored antimatter. Slotted **Dash** costs 10 energy and delivers a stronger aimed impulse with lift (46 / 55.2 / 64.4 speed by rank). Dash ends active booster thrust. These values are provisional playtest tuning.

**Hover movement:** WASD steers over ground/water with automatic low suspension; mouse pitch aims rather than changing altitude. **Space jumps for 2 current energy**, with no double jump or fall damage. Crests preserve upward momentum and drops enter radial-gravity flight without a ground-relative ceiling snap. Steep faces block uphill travel; Dash lift is bounded by the ordinary jump launch budget. The separate F6 flight lab retains its original Space/C altitude controls. Compact radius/terrain/difficulty/density redesign stages are tracked in `docs/TERRAIN_HOVER_PLAN.md`.

Start with **Plasma bronze**. **B → Ship** researches sequential stars and plans four items anywhere. Returning within **18 surface / 16 elevation metres** of the living home automatically installs the plan/latest researched levels, preserving energy/cooldowns. Ownership, active/planned sets and ranks survive death/tier loss; passives apply remotely. Dash unlocks at **II**, Guardian at **IV**. **R** retries the world with fresh match state. See `docs/SHIP_EQUIPMENT.md`.

**Exploration:** destroy gray **50-HP wrecks**, then fly close to gold salvage. The first collector secures **three fixed boon choices**, or may **Reject for 30 antimatter** deposited directly at home with owned overflow. Select a boon or Reject, then **Confirm**; the modal closes after one reward without a second UI. **H/Esc** defers; H reopens pending offers, while F3 shows collected bonuses. Rewards/stacks survive death and tier loss, reset with R, and never grant ranks. Choices stay real time and own ship input. Twelve initial wrecks and ongoing orbital arrivals exclude every base. See `docs/EXPLORATION_BOONS.md`.

The **Tactical Radar** shows forward-up navigation; **M** opens a real-time survey with waypoints. Live actors/details use **220m ship sensing**, home/owned points are global, and discovered rival/scout-charted point locations are remembered without distant live details. Rival pilots additionally need physical line of sight on both maps. Neutral mobs/AM use single-pixel gray/orange dots; owned resources retain faction colors. Drones have role-specific glyphs. B/M transfer input while simulation stays live. Collector/scout search reaches the globe.

**Automatic fleets:** bronze Scout/Attack/Defensive/Strike/Siphon/**Harvester** research grants one slot. Free replacement **10s**, Strike **20s**; gold permits paid extras (provisional five/type). Tier-II Resources Harvesters hunt non-site mobs and claim neutral drops for ordinary collectors; they generate no direct AM. Attack drones now have **75/100/125 HP / 10/14/18 DPS**, weak siege and visible tracers. Siphons physically return stolen base AM. Guardian stays manual. Claim Calibration improves owned magnetism, retaining **120m / 1.3m / 1 E** Claim. Tier-II **T Teleport Home** channels **3s**, costs **20 E** on arrival, cooldown **60/45/30s**, no slot/heal/refill.

Open `project.godot` in Godot .NET, build the C# project, and press **F5 (Run Project)** to launch a fresh seeded spherical match. **R retries the current world seed and size**, resetting match state/terrain edits. **F2** opens paused world setup: choose a size and **Start new planet** with a blank seed for fresh random geography, or enter a signed 32-bit seed to replay a world. F2 displays the current seed; F3 reports world/terrain seeds and generation attempts. Same seed, size and generator version reproduce geography/layout/initial placement; later gameplay outcomes remain live randomness. Seed/size persist for the application session. `scenes/Game.tscn` remains the flat reference. Inspector diagnostics can set `Game.UseSessionWorldSeed=false` and use `LayoutSeed` as a fixed world seed.

**Spherical flight lab:** press **F6** during gameplay, or run `scenes/PlanetFlightTest.tscn`. Compare the four named presets with adjustable radius, speed, and altitude. **Esc** releases the mouse for settings, **V** toggles cameras, **F** dashes, **R** resets flight, and **F6** starts a fresh spherical gameplay match. The lab includes inward gravity, surface-relative upright flight, globe distance markers, and base scale references. See `docs/SPHERICAL_WORLD.md` for implementation details and integrated playtest steps.

Victory requires the rival base destroyed and pilot defeated. Both pilots respawn after **10 seconds** while their base survives, clearing the rival's earlier defeat. Home-base destruction causes defeat; results pause and release the mouse. **R** retries the seed. One rival is tracked through respawns. **F3** toggles detailed help/debug; PgUp/PgDn scroll. Persistent objective panels are removed from flight HUD.

Look for the **RIVAL PILOT** label on the magenta ship. It follows shared surface-relative flight and Claim/collector rules; it cannot absorb resources directly. Its movement/action requests come from the AI controller; the human retains the active camera, mouse and UI.

**RMB Claim** costs **1 energy / 0.4s**, outside equipment slots. Its aimed terrain/water-following bolt claims every eligible AM sphere along the swept path and keeps travelling until range/lifetime expires. Calibration expands range/width. Claimed resources remain for collectors; ships cannot absorb them. **1–4 selects / LMB activates / Tab cycles** occupied slots. Selection costs nothing; empty slots do nothing. Terrain controls remain hidden; Plasma impacts still deform terrain.

Loose AM rolls/collides and scales as `(0.35 / 3) * pow(amount, 0.65)`. Same-owner spheres merge after 1.25s supported contact with 0.6s growth; different owners stay separate. Collectors carry **80 A internally**, or **120/160/200 A** with Cargo Compression, partial-scooping without a ball-count cap. Each tier provides one home collector (1–5), 15s replacement/cargo-safe retirement; logistics collectors are additional.

Bases start at **tier I**, capacities **300/500/750/1000/1500**. Storage never auto-upgrades tiers. Five concurrent project slots pay immediately and refund exact AM with owned overflow. Starting storage/energy is **30 A / 30 E**; limit equals retained storage, recharge **4%/s**. Delivery raises the limit without refill; purchases lower/clamp it. Full storage stops collecting but not Claim; retained cargo waits for room.

Each tier contributes **400 HP**. Plasma deals **20% base damage**. Damage removes layers/spills excess storage; capacity scales the top layer's increment. Tier loss destroys active projects: floor-half investment spills neutral, remainder is lost. Repair **3 HP/s after 5 quiet seconds** never restores lost tiers/resources. Advancement costs **half the current tier's nominal capacity: 150/250/375/500 A**, durations **60/80/100/120s**. Rebuilding is sequential, rounded-up half normal price/same duration. Complete base loss is terminal.

Research follows tier/star pricing in the plan. Offense is base-operated: paid target dropdowns deploy **Scout 25 A/10s**, **Attack/Defender 40 A/10s**, **Strike 100 A/20s** after research. Invalidated production targets refund exactly. Defenders retreat and defend home on site loss; strikes are interceptable. Basic bases retaliate at **30m / 6 damage / 1.2s**, with star upgrades and line of sight; neutrals attack contacted bases. Non-ship effects suspend below lost prerequisites, while ship ownership/passives persist. B/Esc closes live management; F2 opens paused setup.

**Hunting groups:** target **96/180/300/450**, with irregular wild packs, wider wandering and shorter pauses. Habitats recover after **35s**, replacing one member every **8s** while eligible. Site ownership suppresses guard recovery; neutralization restarts it. Placement remains bounded and protected openings remain.

Live intrinsic drops are **6/36/72/20 A** for swarms/beasts/heavies/casters or scavengers, plus stolen reserves. Mob deaths scatter more fragments at **90ms**; ordinary base/cargo spills retain gravity-only behavior and exact totals. Resource-bearing spawns introduce supply; transfers conserve it. Mobs retain the **12m terrain/water flight ceiling**. Dense XL native physics remains a performance gap; the current default is Medium.

Antimatter-loop checks cover sequential tier completion, rejected/atomic payments, separate expenditure sinks, spawn-order-independent energy initialization, delivery without refill, full-storage/retained-cargo handling, damage/destruction spills, exact integer remainders, claim/drone recovery, withdrawal and re-delivery. Physics checks verify base spills settle on the heightmap outside its collider.

From the repository root in PowerShell:

```powershell
dotnet build MagicCarpetRemastered.sln
$godot = 'D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe'
& $godot --headless --path . --editor --import --quit
& $godot --headless --path . 'res://tools/PlanetLayoutChecks.tscn'
& $godot --headless --path . 'res://tools/CompactTerrainChecks.tscn'
& $godot --headless --path . 'res://tools/TerrainAccessChecks.tscn'
& $godot --headless --path . 'res://tools/TerrainAccessChecks.tscn' -- --random-worlds
& $godot --headless --path . 'res://tools/WorldSeedChecks.tscn'
& $godot --headless --path . 'res://tools/NeutralBehaviorChecks.tscn'
& $godot --headless --path . 'res://tools/RivalFactionChecks.tscn'
& $godot --headless --path . 'res://tools/CompactUiChecks.tscn'
& $godot --headless --path . 'res://tools/BaseConstructionChecks.tscn'
& $godot --headless --path . 'res://tools/BaseManagementChecks.tscn'
& $godot --headless --path . 'res://tools/AutomaticFleetChecks.tscn'
& $godot --headless --path . 'res://tools/AntimatterLoopChecks.tscn'
& $godot --headless --path . 'res://tools/AntimatterPhysicsChecks.tscn'
& $godot --headless --path . 'res://tools/LevelObjectiveChecks.tscn'
& $godot --headless --path . 'res://tools/RivalFlightChecks.tscn'
& $godot --headless --path . 'res://tools/PlaytestMechanicsChecks.tscn'
& $godot --headless --path . 'res://tools/CombatFeelChecks.tscn'
& $godot --headless --path . 'res://tools/EquipmentProgressionChecks.tscn'
& $godot --headless --path . 'res://tools/BoosterChecks.tscn'
& $godot --headless --path . 'res://tools/HoverMovementChecks.tscn'
& $godot --headless --path . 'res://tools/ExplorationBoonChecks.tscn'
& $godot --headless --path . 'res://tools/StrategicPointChecks.tscn'
& $godot --path . 'res://tools/EncounterLoadChecks.tscn' -- --xl
& $godot --headless --path . 'res://tools/StrategicNavigationChecks.tscn'
& $godot --headless --path . 'res://tools/PlanetFlightChecks.tscn'
& $godot --headless --path . 'res://tools/PlanetGameplayChecks.tscn'
& $godot --headless --path . 'res://tools/CombatPerformanceChecks.tscn'
& $godot --headless --path . 'res://tools/CameraFriendlyFireChecks.tscn'
& $godot --headless --path . --quit-after 1200
& $godot --headless --path . 'res://scenes/Game.tscn' --quit-after 600
```

The antimatter-loop regression scene checks conservation, player contact/proximity rejection, drone delivery/overflow/pop, rival collection, exact death drops, and Claim movement/hits/expiry/controls. Physics checks advance real solver frames for ground contact, size changes, rolling, collisions, merges, cargo release and water support. Objective checks cover both kill orders, resource-quota independence, defeat precedence, base damage, result pausing/UI and restart; run rendered to also check mouse recapture. Rival flight checks cover altitude, resource acquisition/deposit and base-loss movement. Regression scenes exit nonzero on failure. Manual playtesting continues for control feel, readability and balance.

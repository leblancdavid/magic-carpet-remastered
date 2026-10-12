# Magic Carpet Remastered

A Godot C# planetary-combat prototype moving toward an original futuristic faction-conflict setting. The repository title remains a working name; Magic Carpet is a historical mechanics reference.

## Current Status

The spherical mechanics and smoothness are accepted as a playtest baseline. Economy, construction/siege/rebuilding, live management, owned equipment/home installation, booster, wrecks/boons, economic sites and matter logistics are implemented for their first pass. **All four readiness phases are complete:** globe layout/separated starts, territorial neutrals/raids, shared-mechanics rival and compact icon UI/four-slot HUD. **Active redesign:** shared hover/paid jump, compact planets and mostly-accessible terrain/openings are implemented and verified; runtime rival routing, difficulty and grouped encounters follow. See `docs/TERRAIN_HOVER_PLAN.md`, `docs/COMPACT_UI.md` and `docs/RIVAL_AI.md`.

The default prototype is a spherical planet with upright ship flight, combat, deformable land/oceans, radial-gravity antimatter spheres, ownership/claim, collector-drone hauling, bases, rival enemies, equipment/upgrades, and level victory/defeat flow. Planet sizes are **Small 500 / Medium 800 / Large 1,000 / XL 1,200** radius units; Medium is the default. Win by destroying the rival base and defeating its pilot, in either order. Antimatter ownership is a resource system and does not trigger victory. Visuals and audio remain placeholders.

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

**Compact UI:** bottom-centred four numbered equipment cards show selection, rank/cost, real cooldown and insufficient-energy state. **1–4 selects / LMB activates / Tab cycles**; with the cursor visible, card mouse-down selects without casting. Claim/Shift stay outside the four slots. One **B/M/H** dock opens live interfaces; **F3** holds detailed help/debug. **B → Ship → Research** uses compact glyph/name/cost tiles with effect/prerequisite/unavailable tooltips. **Ship → Loadout** applies owned ranks/items explicitly at home through a destination/replacement selector. Five construction categories and exact full-refund cancellation remain. UI adapts to smaller canvases; see `docs/COMPACT_UI.md`.

**Planet layout:** **Small 100 / Medium 200 / Large 300 / XL 400 m radius**, Medium default, with genuine curvature. Every fresh world gets a random seed that changes actual continents/coasts/islands/mountain placement and layout. Most land is gentle; roughness stays in three localized mountain regions. Before actors spawn, bounded candidate validation enforces **3–5% land above 40°**, **at least 85% below 20°**, and ordinary-hover opening approaches. Sixteen tested terrain/size cases independently measure **3.3–4.8% steep / 88.8–94.7% gentle**. Spherical factions start opposite with a configurable **120° minimum separation**, equal **30 antimatter / 30 matter**, dry level pads and home-relative ship/collector spawns. Five landmarks, eight stations, remote fields, twelve scenery clusters and seeded initial wreck/neutral placements span the globe. Independent checks establish 192 random-world opening routes and actual normal-hover resource round trips. Runtime rival pathfinding/recovery remains next. See `docs/STRATEGIC_NAVIGATION.md`.

**Neutrals:** ordinary mobs live near stable habitats and react to either faction's pilot. Compact-world proximity/retaliation/disengagement/leash distances scale by size; on default Medium they are **16/30/40/60 m**. Escape, death or crossing the habitat leash commits a return home. Neutrals do not globally seek bases. Scavengers steal local antimatter from either faction, retain it and drop neutral salvage rather than funding the rival. Separate **one-at-a-time raids** begin after **180 seconds**, alternate living bases at **180-second intervals**, deal **4 base HP every 3 seconds** and expire after **90 seconds**. Inbound HUD feedback and labelled red raiders identify the target. Full size-specific/flat-reference defaults are in `docs/NEUTRAL_BEHAVIOR.md`; tuning awaits hands-on play.

**Rival:** the labelled magenta ship uses the same flight, energy, paid equipment, Claim/collector hauling, capture and respawn mechanics as the player. It gathers locally, captures income, pays for development, returns home to install, explores known wrecks and makes occasional attacks after a five-minute grace. It retains a provisional 12-antimatter reserve and uses basic commitment/retry/retreat rules. F3 BASES/ENEMIES reports its task, goal, energy and paid orders. Both factions' research/loadouts/boons survive death/tier loss. Strategy, wider equipment use and routing remain minimal; see `docs/RIVAL_AI.md`.

**Economic sites:** eight neutral stations appear as globally charted ring/cross markers on **M** survey/radar. Neutral capture takes **10 seconds**; enemy-owned points take **10 seconds to neutralize, then 10 seconds to capture** (8/12/18/18 m radius by size). Opposing pilots freeze progress; departure slowly drains it at half speed, while defenders restore at full speed. A screen-space bar above equipment shows progress without looking at the station. Old income/research benefits stop at neutralization; logistics suspends, then transfers the same drone and all mixed cargo on final capture. Two antimatter/two matter extractors deposit up to **2 resource/s**, stopping at independent capacities; research adds **25% project speed** per site. Full bases retain cargo; destroyed station drones do not respawn. Rates/layout/strategy remain provisional.

**Matter:** orange neutral crates are construction material. **RMB Claim** turns them cyan for drone hauling; ships cannot absorb them. Each base starts with **30 matter**, with a nearby **60-matter field** and two further mirrored fields. Material capacity defaults to **120/200/300/400/500** by tier and follows the same damage-layer rule independently. Collectors choose the nearest eligible resource with storage room, sharing three cargo objects; full antimatter storage does not block material gathering or vice versa. Matter never contributes to combat energy. Tiers 2–5 cost **30/45/60/75 matter** in addition to antimatter; repair/energy-grid/collector-drive/strike projects cost **10/20/10/15 matter**. Ship and Claim research remain antimatter-only. Construction pays both resources atomically; cancellation refunds both with owned overflow; siege losses and half-price rebuilds apply independently to both. HUD/B/F3 and survey/radar expose the reserve/costs/crates.

**Shift boosts forward** at 36 units/second for 0.65 seconds, costing 4 current energy with a 2-second cooldown. Press again after releasing Shift; holding it does not repeat. Boost is built in and consumes no equipment slot or stored antimatter. Slotted **Dash** costs 10 energy and delivers a stronger aimed impulse with lift (46 / 55.2 / 64.4 speed by rank). Dash ends active booster thrust. These values are provisional playtest tuning.

**Hover movement:** WASD steers over ground/water with automatic low suspension; mouse pitch aims rather than changing altitude. **Space jumps for 2 current energy**, with no double jump or fall damage. Crests preserve upward momentum and drops enter radial-gravity flight without a ground-relative ceiling snap. Steep faces block uphill travel; Dash lift is bounded by the ordinary jump launch budget. The separate F6 flight lab retains its original Space/C altitude controls. Compact radius/terrain/difficulty/density redesign stages are tracked in `docs/TERRAIN_HOVER_PLAN.md`.

Start with **Plasma Bolt I**. Press **B → Ship** to research equipment and sequential ranks I–III, view owned items and select a destination among **four shared slots**. Research is remote; installation/replacement requires returning within **18 surface units of home** while alive. Apply completed ranks explicitly at home. Replaced items remain owned and re-equip for free; ownership, ranks and loadout survive death and tier loss. **R** begins fresh. See `docs/SHIP_EQUIPMENT.md` for provisional prices, tiers and effects.

**Exploration:** find random gray wrecks, destroy their **50 HP** with Plasma Bolt or Pulse Burst, then fly close to the revealed gold salvage. The first collector secures **three fixed boon choices**, or may skip for **30 antimatter** deposited directly at home with owned overflow. **H / Esc** defers/closes; H reopens pending choices and the live bonus summary. Rewards and additive boon stacks survive death and tier loss, reset with R, and never grant equipment ranks. Choice UI stays real time and owns ship input. Twelve initial wrecks and ongoing harmless orbital arrivals exclude every base; radar/survey charts nearby wrecks and distinguishes revealed salvage. Tuning and the eight-boon catalog are in `docs/EXPLORATION_BOONS.md`.

The globe's **Tactical Radar** local radar shows forward-up navigation and waypoint distance/bearing. Press **M** for the **real-time** world survey; left click a marker or surface position to set a waypoint, right click/Delete to clear, and M/Esc to close. B/M transfer between management and survey. Both own ship controls while open; enemies, research and damage continue. Sites begin as anonymous hints and reveal names within 65 m. Owned antimatter/allied drones are tracked globally; neutral/rival antimatter and threats reveal locally. Drone search reaches the full globe, with longer distant delivery trips. See `docs/STRATEGIC_NAVIGATION.md` for layout, visibility rules and controls.

Open `project.godot` in Godot .NET, build the C# project, and press **F5 (Run Project)** to launch a fresh seeded spherical match. **R retries the current world seed and size**, resetting match state/terrain edits. **F2** opens paused world setup: choose a size and **Start new planet** with a blank seed for fresh random geography, or enter a signed 32-bit seed to replay a world. F2 displays the current seed; F3 reports world/terrain seeds and generation attempts. Same seed, size and generator version reproduce geography/layout/initial placement; later gameplay outcomes remain live randomness. Seed/size persist for the application session. `scenes/Game.tscn` remains the flat reference. Inspector diagnostics can set `Game.UseSessionWorldSeed=false` and use `LayoutSeed` as a fixed world seed.

**Spherical flight lab:** press **F6** during gameplay, or run `scenes/PlanetFlightTest.tscn`. Compare the four named presets with adjustable radius, speed, and altitude. **Esc** releases the mouse for settings, **V** toggles cameras, **F** dashes, **R** resets flight, and **F6** starts a fresh spherical gameplay match. The lab includes inward gravity, surface-relative upright flight, globe distance markers, and base scale references. See `docs/SPHERICAL_WORLD.md` for implementation details and integrated playtest steps.

The compact HUD shows health/energy, rival base/pilot objective status, respawn countdowns, and selected equipment. Victory requires the rival base destroyed and its pilot defeated. Both pilots respawn after **10 seconds** while their respective base survives; a respawn clears the rival's earlier defeat. Destroying the player base causes defeat. Results freeze gameplay and release the mouse; press **R** to restart. One designated rival is tracked through its respawns; the ecosystem director does not spawn extra rival ships. Use **Plasma Bolt** against the rival base. Press **F3** to show/hide the debug and controls panel (off by default); **PgUp / PgDn** scroll its grouped diagnostics.

Look for the **RIVAL PILOT** label on the magenta ship. It follows shared surface-relative flight and Claim/collector rules; it cannot absorb resources directly. Its movement/action requests come from the AI controller; the human retains the active camera, mouse and UI.

**Right-click activates built-in Claim** (2 energy, 0.4-second cooldown, outside equipment slots). Aim at an antimatter globe or the ground before it: the cyan bolt flies along the aim direction, then skims terrain/water along a fixed horizontal heading and claims the first neutral/rival loose globe it hits. Misses expire after 120 units or 4 seconds. Claimed globes turn cyan and remain in the world for your collector drone to collect and deliver to the base. The player cannot directly collect antimatter or attract globes; base delivery increases energy capacity/regeneration. **1–4 selects the corresponding equipped slot; LMB activates it.** Selection itself costs no energy and creates no effect. **Tab** cycles occupied slots in slot order, skipping empty slots; selecting an empty slot makes LMB do nothing. Q/E/F/G no longer directly activate gameplay equipment; the flight lab retains its separate F dash. Direct terrain-shaping controls are hidden for now; Plasma Bolt impact craters still work.

Loose antimatter spheres touch the ground/water, roll downhill, and collide with terrain, bases, and solid objects. Radius scales as `(0.35 / 3) * pow(amount, 0.65)` (10 antimatter ≈ 0.521 units; 80 ≈ 2.014). Same-owner spheres attract and merge after 1.25 seconds of supported contact, growing smoothly over 0.6 seconds while retaining all antimatter. Different owners stay separate. Carried spheres suspend physics until released.

Bases start at **tier 1**, with provisional intact capacities **120 / 200 / 300 / 400 / 500** for tiers 1–5. Tier completion changes capacity/visual scale; storage never automatically upgrades or demotes the base. Construction supports base/defense/resources/offense/ship slots with immediate payment, concurrent timers and no queues. Cancellation fully refunds paid antimatter, spilling owned overflow. Starting storage is **30 antimatter**, giving **30/30 ship energy**. The energy limit equals retained home-base storage, with no starting-power bonus or separate power cap. Recharge is **4% of that limit per second**. Deliveries raise the limit without refilling current energy; construction payment lowers storage/limit immediately and clamps current energy. Full storage stops new drone collection but not Claim; undeposited cargo stays owned aboard the drone until room opens.

Each tier contributes **140 HP** by default. Damage removes tiers at health-layer boundaries and spills excess storage neutral; effective storage scales the damaged top layer's incremental capacity over intact lower layers. Losing any tier destroys **all active projects**: each spills half its paid investment (rounded down) neutral and permanently loses the remainder. Surviving bases repair **3 HP/second after 5 seconds without damage**, capped at the surviving tier; repair never restores lost tiers or resources. Previously bought tiers rebuild sequentially at half price (rounded up). Tier 2/3/4/5 upgrades provisionally cost **120/180/270/360 antimatter** and take **60/80/100/120 seconds** before research bonuses; rebuild times match. Complete base destruction remains terminal. **B** provides live orders/cancellation; **F3** shows payment/refund/loss diagnostics.

Starter projects: **Repair matrix** (40 antimatter / 60s, +50% repair), **Energy grid** (80 / 90s, tier 2, +25% recharge), **Collector drive** (50 / 60s, +25% speed), **Strike drone** (60 / 15s, tier 2, repeatable one-shot 45 base damage), and **Claim range calibration** (30 / 60s, range 120→150). Equipment research prices doubled; ranks I/II/III take **60/90/120 seconds** and appear in aligned rows per item in **B → Ship → Research**. Completed research persists; higher-tier non-ship effects suspend below their prerequisite tier and return after rebuilding. **Esc/B** closes management. F2 opens paused setup; UI clicks/held combat keys cannot fire equipment through panels.

**Hunting groups:** Small/Medium/Large/XL target **16/24/32/40 neutral mobs**, grouped by role around landmarks and economic routes. Depleted habitats recover after **20 seconds**, then replace members individually every **five seconds**. Swarms start in threes; beasts in pairs; casters/scavengers are singles/pairs. Safe placement, population caps and individual territorial aggression remain; prices/timers and hunting supply need playtest balance.

Neutral creatures drop their intrinsic antimatter on death (12 each by default). Deaths, base spills, and drone destruction release randomly sized, exactly conserved fragments one at a time at **50-ms intervals**, with zero launch velocity and a brief merge grace. Drops fall under gravity. Resource-bearing neutral spawns add their reserve to the world tally; subsequent deaths/transfers conserve it. The shared flight ceiling is **12 units above terrain/water**, including swarms and ranged casters. See `docs/PLAYTEST_MECHANICS.md` for inherited defaults and the hands-on test sequence.

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
& $godot --headless --path . 'res://tools/AntimatterLoopChecks.tscn'
& $godot --headless --path . 'res://tools/AntimatterPhysicsChecks.tscn'
& $godot --headless --path . 'res://tools/LevelObjectiveChecks.tscn'
& $godot --headless --path . 'res://tools/RivalFlightChecks.tscn'
& $godot --headless --path . 'res://tools/PlaytestMechanicsChecks.tscn'
& $godot --headless --path . 'res://tools/EquipmentProgressionChecks.tscn'
& $godot --headless --path . 'res://tools/BoosterChecks.tscn'
& $godot --headless --path . 'res://tools/HoverMovementChecks.tscn'
& $godot --headless --path . 'res://tools/ExplorationBoonChecks.tscn'
& $godot --headless --path . 'res://tools/StrategicPointChecks.tscn'
& $godot --headless --path . 'res://tools/MatterChecks.tscn'
& $godot --headless --path . 'res://tools/StrategicNavigationChecks.tscn'
& $godot --headless --path . 'res://tools/PlanetFlightChecks.tscn'
& $godot --headless --path . 'res://tools/PlanetGameplayChecks.tscn'
& $godot --headless --path . 'res://tools/CombatPerformanceChecks.tscn'
& $godot --headless --path . 'res://tools/CameraFriendlyFireChecks.tscn'
& $godot --headless --path . --quit-after 1200
& $godot --headless --path . 'res://scenes/Game.tscn' --quit-after 600
```

The antimatter-loop regression scene checks conservation, player contact/proximity rejection, drone delivery/overflow/pop, rival collection, exact death drops, and Claim movement/hits/expiry/controls. Physics checks advance real solver frames for ground contact, size changes, rolling, collisions, merges, cargo release and water support. Objective checks cover both kill orders, resource-quota independence, defeat precedence, base damage, result pausing/UI and restart; run rendered to also check mouse recapture. Rival flight checks cover altitude, resource acquisition/deposit and base-loss movement. Regression scenes exit nonzero on failure. Manual playtesting continues for control feel, readability and balance.

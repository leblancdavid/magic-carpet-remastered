# Magic Carpet Remastered

A Godot C# planetary-combat prototype moving toward an original futuristic faction-conflict setting. The repository title remains a working name; Magic Carpet is a historical mechanics reference.

## Current Status

The spherical mechanics are accepted as a playtest baseline. The latest follow-up adds **irregular denser encounters with wandering, a fully centered compact HUD, confirmed boon choices, cheaper/weaker Plasma, hit feedback and shared quiet health recovery** to the antimatter-only/four-tab foundation. First-pass balance targets economy-focused Medium tier V in **10–15 minutes**, pending hands-on acceptance. Runtime rival routing/recovery, sustained XL performance and difficulty remain next. See `docs/PLAYTEST_SIMPLIFICATION.md` and the progress log.

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

**Compact UI:** the entire bottom group is centered: narrow **H/E/A** gauges with current-only values, four numbered equipment cards, radar/base HP and controls/B/M/H. HP changes green/yellow/red when low; energy is blue and antimatter amber. Capture/respawn are contextual; routine toast messages are removed. **1–4 selects / LMB activates / Tab cycles**; card mouse-down selects without firing. **B** has resources/upgrade above **Offense / Defense / Resources / Ship**, button-overlaid progress/× cancel and faded Roman-tier requirements. Click owned equipment tiles at home to toggle up to four; pending ranks use **Apply**. See `docs/COMPACT_UI.md`.

**Planet layout:** seeded continents/coasts/islands/mountains and genuine curvature; most land is gentle, roughness localized. Bounded validation enforces **3–5% land above 40°**, **≥85% below 20°** and protected normal-hover approaches before actors spawn. Factions start opposite with configurable **120° minimum separation**, equal **30 antimatter**, dry pads and home-relative ships/collectors. Five landmarks, eight stations, three antimatter fields, scenery, random wrecks and wild habitats span the globe. Independent checks cover **160** retained random-world opening routes and real normal-hover round trips. Runtime rival navigation remains next.

**Neutrals:** targets **96/180/300/450**, with irregular clusters and varied pack footprints over twelve regions. Independent seeded wandering varies idle destinations, speeds and pauses, repicking stalled goals. Easy swarms, beast/caster packs, scavengers and **150-HP heavy beasts** react to either pilot. Medium aggro/retaliation/disengagement/leash is **35/65/85/100 m**; damage rallies the habitat, while escaping commits return. Site guards replenish only while neutral; ownership suppresses replacement and neutralization restarts recovery. Scavengers retain/drop stolen resources. Separate raids retain **180s grace/interval, four base damage/3s and 90s lifetime**. See `docs/NEUTRAL_BEHAVIOR.md`.

**Rival:** the labelled magenta ship uses the same flight, energy, paid equipment, Claim/collector hauling, capture and respawn mechanics as the player. It gathers locally, captures income, pays for development, returns home to install, explores known wrecks and makes occasional attacks after a five-minute grace. It retains a provisional 12-antimatter reserve and uses basic commitment/retry/retreat rules. F3 BASES/ENEMIES reports its task, goal, energy and paid orders. Both factions' research/loadouts/boons survive death/tier loss. Strategy, wider equipment use and routing remain minimal; see `docs/RIVAL_AI.md`.

**Economic sites:** eight ring/cross markers; player-owned sites are global, others require sensing. Neutral capture takes **10s**; takeover takes **10s neutralization + 10s capture**, with **12/18/18/18 m** rings. Opposing pilots contest; departure decays at half speed and defenders restore at full speed. Benefits stop at neutralization; logistics suspends then transfers the same drone/cargo. **Four antimatter extractors produce 0.2 A/s each**, stopping at capacity. Two research stations add **25% project speed** each; two logistics stations provide collectors. Destroyed station drones do not respawn.

**Single resource:** scrap matter, material crates/cargo/storage/costs and material routes are removed. Antimatter funds all construction and backs a separate rechargeable combat-energy quantity.

**Combat feel:** Plasma costs **2 energy**, dealing **13/18/23** damage at ranks I–III (3/4/5 against bases before boons). Both ships heal **2 HP/s after five seconds without dealing/taking combat damage**. Real-time menus continue recovery; pause freezes it. Player health hits briefly shake and darken either camera view.

**Shift boosts forward** at 36 units/second for 0.65 seconds, costing 4 current energy with a 2-second cooldown. Press again after releasing Shift; holding it does not repeat. Boost is built in and consumes no equipment slot or stored antimatter. Slotted **Dash** costs 10 energy and delivers a stronger aimed impulse with lift (46 / 55.2 / 64.4 speed by rank). Dash ends active booster thrust. These values are provisional playtest tuning.

**Hover movement:** WASD steers over ground/water with automatic low suspension; mouse pitch aims rather than changing altitude. **Space jumps for 2 current energy**, with no double jump or fall damage. Crests preserve upward momentum and drops enter radial-gravity flight without a ground-relative ceiling snap. Steep faces block uphill travel; Dash lift is bounded by the ordinary jump launch budget. The separate F6 flight lab retains its original Space/C altitude controls. Compact radius/terrain/difficulty/density redesign stages are tracked in `docs/TERRAIN_HOVER_PLAN.md`.

Start with **Plasma Bolt I**. Press **B → Ship** to research equipment and sequential ranks I–III and toggle up to **four equipped items**. Research is remote; equip/unequip and explicit pending-rank application require returning within **18 surface units of home** while alive. Unequipped items remain owned and re-equip for free; ownership, ranks and loadout survive death and tier loss. **R** retries the current world with fresh match state. See `docs/SHIP_EQUIPMENT.md` for provisional prices, tiers and effects.

**Exploration:** destroy gray **50-HP wrecks**, then fly close to gold salvage. The first collector secures **three fixed boon choices**, or may **Reject for 30 antimatter** deposited directly at home with owned overflow. Select a boon or Reject, then **Confirm**; the modal closes after one reward without a second UI. **H/Esc** defers; H reopens pending offers, while F3 shows collected bonuses. Rewards/stacks survive death and tier loss, reset with R, and never grant ranks. Choices stay real time and own ship input. Twelve initial wrecks and ongoing orbital arrivals exclude every base. See `docs/EXPLORATION_BOONS.md`.

The **Tactical Radar** on the right of the centered HUD shows forward-up navigation; **M** opens a **real-time** survey with waypoint distance/bearing, left-click destination and right-click/Delete clear. Both maps use **220 m ship-only sensing**: home/owned points stay global; discovered rival-base locations are remembered without distant live details. Other actors require sensing, including owned/charted objects. Neutral mobs/antimatter are **black/orange dots**, claimed resources faction-colored. B/M transfer input ownership while simulation stays live. Drone search reaches the globe.

Open `project.godot` in Godot .NET, build the C# project, and press **F5 (Run Project)** to launch a fresh seeded spherical match. **R retries the current world seed and size**, resetting match state/terrain edits. **F2** opens paused world setup: choose a size and **Start new planet** with a blank seed for fresh random geography, or enter a signed 32-bit seed to replay a world. F2 displays the current seed; F3 reports world/terrain seeds and generation attempts. Same seed, size and generator version reproduce geography/layout/initial placement; later gameplay outcomes remain live randomness. Seed/size persist for the application session. `scenes/Game.tscn` remains the flat reference. Inspector diagnostics can set `Game.UseSessionWorldSeed=false` and use `LayoutSeed` as a fixed world seed.

**Spherical flight lab:** press **F6** during gameplay, or run `scenes/PlanetFlightTest.tscn`. Compare the four named presets with adjustable radius, speed, and altitude. **Esc** releases the mouse for settings, **V** toggles cameras, **F** dashes, **R** resets flight, and **F6** starts a fresh spherical gameplay match. The lab includes inward gravity, surface-relative upright flight, globe distance markers, and base scale references. See `docs/SPHERICAL_WORLD.md` for implementation details and integrated playtest steps.

Victory requires the rival base destroyed and pilot defeated. Both pilots respawn after **10 seconds** while their base survives, clearing the rival's earlier defeat. Home-base destruction causes defeat; results pause and release the mouse. **R** retries the seed. One rival is tracked through respawns. **F3** toggles detailed help/debug; PgUp/PgDn scroll. Persistent objective panels are removed from flight HUD.

Look for the **RIVAL PILOT** label on the magenta ship. It follows shared surface-relative flight and Claim/collector rules; it cannot absorb resources directly. Its movement/action requests come from the AI controller; the human retains the active camera, mouse and UI.

**Right-click activates built-in Claim** (2 energy, 0.4-second cooldown, outside equipment slots). Aim at an antimatter globe or the ground before it: the cyan bolt flies along the aim direction, then skims terrain/water along a fixed horizontal heading and claims the first neutral/rival loose globe it hits. Misses expire after 120 units or 4 seconds. Claimed globes turn cyan and remain in the world for your collector drone to collect and deliver to the base. The player cannot directly collect antimatter or attract globes; base delivery increases energy capacity/regeneration. **1–4 selects the corresponding equipped slot; LMB activates it.** Selection itself costs no energy and creates no effect. **Tab** cycles occupied slots in slot order, skipping empty slots; selecting an empty slot makes LMB do nothing. Q/E/F/G no longer directly activate gameplay equipment; the flight lab retains its separate F dash. Direct terrain-shaping controls are hidden for now; Plasma Bolt impact craters still work.

Loose antimatter spheres touch the ground/water, roll downhill, and collide with terrain, bases, and solid objects. Radius scales as `(0.35 / 3) * pow(amount, 0.65)` (10 antimatter ≈ 0.521 units; 80 ≈ 2.014). Same-owner spheres attract and merge after 1.25 seconds of supported contact, growing smoothly over 0.6 seconds while retaining all antimatter. Different owners stay separate. Carried spheres suspend physics until released.

Bases start at **tier I**, with capacities **200/350/550/800/1100**. Storage never auto-upgrades the base. Five backend construction slots pay immediately, run concurrently and refund exact antimatter with owned overflow on cancellation. Starting storage/energy is **30 A / 30 E**; energy limit equals retained home storage, recharging **4%/s**. Delivery raises the limit without refill; purchases lower it and clamp energy. Full storage stops drone collection but not Claim; retained cargo waits for room.

Each tier contributes **400 HP**. Plasma deals **20% base damage**, retaining normal mob/ship damage. Damage removes layers and spills excess storage; capacity scales the top layer's increment. Tier loss destroys all active projects: floor-half investment spills neutral, remainder is lost. Repair is **3 HP/s after 5 quiet seconds**, without restoring lost tiers/resources. Tier II–V cost **150/275/450/650 A** and take **60/80/100/120s** before research bonuses. Purchased tiers rebuild sequentially at rounded-up half price/same duration. Complete base loss is terminal.

Starter projects: **Repair matrix 70 A/60s**, **Energy grid 140/90s/tier II**, **Collector drive 90/60s**, **Strike drone 100/20s/tier II/120 damage**, **Claim range 50/60s**. Equipment prices are dearer; ranks take **60/90/120s** in aligned Ship rows. Higher-tier non-ship effects suspend below prerequisites and return after rebuilding; ownership/installed equipment persists. B/Esc closes live management; F2 opens paused setup. UI clicks cannot leak firing.

**Hunting groups:** target **96/180/300/450**, including clustered random wild packs, 6–10-capacity easy swarms and heavy groups of 1–3. Initial allocation seeds regions then finishes packs under budget. Depleted habitats recover after **20s**, replacing one member every **5s** while eligible. Site ownership suppresses guard recovery; neutralization restarts it. Placement remains bounded with safe base openings and delayed near-pilot replacements.

Live intrinsic drops are **three A per swarm, 18 per beast, 36 per heavy, ten per caster**; scavengers drop stolen reserves. Death/base/drone spills conserve exact amounts in staggered **50-ms**, zero-launch fragments. Resource-bearing spawns introduce supply; later deaths/transfers conserve it. Mobs retain the **12 m terrain/water flight ceiling**. See current density/economy notes alongside historical mechanics references.

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

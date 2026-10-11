# Magic Carpet Remastered

A Godot C# planetary-combat prototype moving toward an original futuristic faction-conflict setting. The repository title remains a working name; Magic Carpet is a historical mechanics reference.

## Current Status

The current spherical mechanics and smoothness are accepted as a playtest baseline. Base economy, construction/siege/rebuilding and real-time management are implemented. Press **B** to order/cancel projects remotely, view the five slots and monitor storage, energy and siege state. Starter research improves repair, recharge, collectors and built-in Claim; repeatable strike drones attack the rival base. Next: owned equipment, four-slot loadouts and home-base installation, followed by the energy-costing Shift booster. See `docs/BASE_DEVELOPMENT.md`, `docs/SHIP_EQUIPMENT.md` and `docs/TERMINOLOGY_MIGRATION.md`.

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

Start with **Plasma Bolt I**. Press **B → Ship** to research equipment and sequential ranks I–III, view owned items and select a destination among **four shared slots**. Research is remote; installation/replacement requires returning within **18 surface units of home** while alive. Apply completed ranks explicitly at home. Replaced items remain owned and re-equip for free; ownership, ranks and loadout survive death and tier loss. **R** begins fresh. Legacy gold beacons are now non-reward signals awaiting wreck/boon conversion. See `docs/SHIP_EQUIPMENT.md` for provisional prices, tiers and effects.

The globe's **Tactical Radar** local radar shows forward-up navigation and waypoint distance/bearing. Press **M** for the **real-time** world survey; left click a marker or surface position to set a waypoint, right click/Delete to clear, and M/Esc to close. B/M transfer between management and survey. Both own ship controls while open; enemies, research and damage continue. Sites begin as anonymous hints and reveal names within 65 m. Owned antimatter/allied drones are tracked globally; neutral/rival antimatter and threats reveal locally. Drone search reaches the full globe, with longer distant delivery trips. See `docs/STRATEGIC_NAVIGATION.md` for layout, visibility rules and controls.

Open `project.godot` in Godot .NET, build the C# project, and press **F5 (Run Project)** to launch `scenes/PlanetGame.tscn`. **F2** pauses and opens planet-size selection; **Start new planet** resets the match at that size. **R** restarts at the selected size. Selection lasts for the application session. Running `scenes/Game.tscn` directly opens the flat-island reference arena. Current controls are listed in `docs/PROGRESS_LOG.md`.

**Spherical flight lab:** press **F6** during gameplay, or run `scenes/PlanetFlightTest.tscn`. Compare the four named presets with adjustable radius, speed, and altitude. **Esc** releases the mouse for settings, **V** toggles cameras, **F** dashes, **R** resets flight, and **F6** starts a fresh spherical gameplay match. The lab includes inward gravity, surface-relative upright flight, globe distance markers, and base scale references. See `docs/SPHERICAL_WORLD.md` for implementation details and integrated playtest steps.

The compact HUD shows health/energy, rival base/pilot objective status, respawn countdowns, and selected equipment. Victory requires the rival base destroyed and its pilot defeated. Both pilots respawn after **10 seconds** while their respective base survives; a respawn clears the rival's earlier defeat. Destroying the player base causes defeat. Results freeze gameplay and release the mouse; press **R** to restart. One designated rival is tracked through its respawns; the ecosystem director does not spawn extra rival ships. Use **Plasma Bolt** against the rival base. Press **F3** to show/hide the debug and controls panel (off by default); **PgUp / PgDn** scroll its grouped diagnostics.

Look for the **RIVAL PILOT** label on the purple/magenta placeholder near the red base. Its flight follows terrain/water at a controlled height, approaches antimatter closely enough to collect it, and caps combat altitude so it cannot drift into the sky.

**Right-click activates built-in Claim** (2 energy, 0.4-second cooldown, outside equipment slots). Aim at an antimatter globe or the ground before it: the cyan bolt flies along the aim direction, then skims terrain/water along a fixed horizontal heading and claims the first neutral/rival loose globe it hits. Misses expire after 120 units or 4 seconds. Claimed globes turn cyan and remain in the world for your collector drone to collect and deliver to the base. The player cannot directly collect antimatter or attract globes; base delivery increases energy capacity/regeneration. **1–4 selects the corresponding equipped slot; LMB activates it.** Selection itself costs no energy and creates no effect. **Tab** cycles occupied slots in slot order, skipping empty slots; selecting an empty slot makes LMB do nothing. Q/E/F/G no longer directly activate gameplay equipment; the flight lab retains its separate F dash. Direct terrain-shaping controls are hidden for now; Plasma Bolt impact craters still work.

Loose antimatter spheres touch the ground/water, roll downhill, and collide with terrain, bases, and solid objects. Radius scales as `(0.35 / 3) * pow(amount, 0.65)` (10 antimatter ≈ 0.521 units; 80 ≈ 2.014). Same-owner spheres attract and merge after 1.25 seconds of supported contact, growing smoothly over 0.6 seconds while retaining all antimatter. Different owners stay separate. Carried spheres suspend physics until released.

Bases start at **tier 1**, with provisional intact capacities **120 / 200 / 300 / 400 / 500** for tiers 1–5. Tier completion changes capacity/visual scale; storage never automatically upgrades or demotes the base. Construction supports base/defense/resources/offense/ship slots with immediate payment, concurrent timers and no queues. Cancellation fully refunds paid antimatter, spilling owned overflow. Starting storage is **30 antimatter**, giving **30/30 ship energy**. The energy limit equals retained home-base storage, with no starting-power bonus or separate power cap. Recharge is **4% of that limit per second**. Deliveries raise the limit without refilling current energy; construction payment lowers storage/limit immediately and clamps current energy. Full storage stops new drone collection but not Claim; undeposited cargo stays owned aboard the drone until room opens.

Each tier contributes **140 HP** by default. Damage removes tiers at health-layer boundaries and spills excess storage neutral; effective storage scales the damaged top layer's incremental capacity over intact lower layers. Losing any tier destroys **all active projects**: each spills half its paid investment (rounded down) neutral and permanently loses the remainder. Surviving bases repair **3 HP/second after 5 seconds without damage**, capped at the surviving tier; repair never restores lost tiers or resources. Previously bought tiers rebuild sequentially at half price (rounded up). Tier 2/3/4/5 upgrades provisionally cost **100/150/200/250** and take **20/30/40/50 seconds**; rebuild times match. Complete base destruction remains terminal. **B** provides live orders/cancellation; **F3** shows additional payment/refund/loss diagnostics.

Starter projects: **Repair matrix** (20 antimatter / 8s, +50% repair), **Energy grid** (40 / 12s, tier 2, +25% recharge), **Collector drive** (25 / 10s, +25% speed), **Strike drone** (60 / 15s, tier 2, repeatable one-shot 45 base damage), and **Claim range calibration** (15 / 8s, range 120→150). Completed research persists; higher-tier non-ship effects suspend below their prerequisite tier and return after rebuilding. **Esc/B** closes management. F2 from management closes it and opens paused setup; result/setup remain the only active pause owners. UI clicks/held combat keys cannot fire equipment when opening or closing panels.

Neutral creatures drop their intrinsic antimatter on death (12 each by default). Deaths, base spills, and drone destruction release randomly sized, exactly conserved fragments one at a time at **50-ms intervals**, with zero launch velocity and a brief merge grace. Drops fall under gravity. Resource-bearing neutral spawns add their reserve to the world tally; subsequent deaths/transfers conserve it. The shared flight ceiling is **12 units above terrain/water**, including swarms and ranged casters. See `docs/PLAYTEST_MECHANICS.md` for inherited defaults and the hands-on test sequence.

Antimatter-loop checks cover sequential tier completion, rejected/atomic payments, separate expenditure sinks, spawn-order-independent energy initialization, delivery without refill, full-storage/retained-cargo handling, damage/destruction spills, exact integer remainders, claim/drone recovery, withdrawal and re-delivery. Physics checks verify base spills settle on the heightmap outside its collider.

From the repository root in PowerShell:

```powershell
dotnet build MagicCarpetRemastered.sln
$godot = 'D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe'
& $godot --headless --path . --editor --import --quit
& $godot --headless --path . 'res://tools/BaseConstructionChecks.tscn'
& $godot --headless --path . 'res://tools/BaseManagementChecks.tscn'
& $godot --headless --path . 'res://tools/AntimatterLoopChecks.tscn'
& $godot --headless --path . 'res://tools/AntimatterPhysicsChecks.tscn'
& $godot --headless --path . 'res://tools/LevelObjectiveChecks.tscn'
& $godot --headless --path . 'res://tools/RivalFlightChecks.tscn'
& $godot --headless --path . 'res://tools/PlaytestMechanicsChecks.tscn'
& $godot --headless --path . 'res://tools/EquipmentProgressionChecks.tscn'
& $godot --headless --path . 'res://tools/StrategicNavigationChecks.tscn'
& $godot --headless --path . 'res://tools/PlanetFlightChecks.tscn'
& $godot --headless --path . 'res://tools/PlanetGameplayChecks.tscn'
& $godot --headless --path . 'res://tools/CombatPerformanceChecks.tscn'
& $godot --headless --path . 'res://tools/CameraFriendlyFireChecks.tscn'
& $godot --headless --path . --quit-after 1200
& $godot --headless --path . 'res://scenes/Game.tscn' --quit-after 600
```

The antimatter-loop regression scene checks conservation, player contact/proximity rejection, drone delivery/overflow/pop, rival collection, exact death drops, and Claim movement/hits/expiry/controls. Physics checks advance real solver frames for ground contact, size changes, rolling, collisions, merges, cargo release and water support. Objective checks cover both kill orders, resource-quota independence, defeat precedence, base damage, result pausing/UI and restart; run rendered to also check mouse recapture. Rival flight checks cover altitude, resource acquisition/deposit and base-loss movement. Regression scenes exit nonzero on failure. Manual playtesting continues for control feel, readability and balance.

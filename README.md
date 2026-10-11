# Magic Carpet Remastered

A Godot C# planetary-combat prototype moving toward an original futuristic faction-conflict setting. The repository title remains a working name; Magic Carpet is a historical mechanics reference.

## Current Status

The current spherical mechanics and smoothness are accepted as a playtest baseline. The behavior-preserving science-fiction terminology migration is complete: ships, bases, equipment, antimatter, collector drones and Claim now appear in gameplay and code. The HUD distinguishes rechargeable ship energy from physical antimatter. Next: explicit base tiers and resource/energy accounting, followed by construction/research, real-time management UI, four-slot owned equipment and an energy-costing Shift booster. Those mechanics remain planned. See `docs/BASE_DEVELOPMENT.md`, `docs/SHIP_EQUIPMENT.md` and `docs/TERMINOLOGY_MIGRATION.md`.

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

Explore five **gold equipment beacons** for rank-II upgrades. On the globe they accompany Windward Ridge, Wellfield, Tidal Reach, distant Redglass Spire and far-side Starfall Observatory; the flat reference retains outskirts placement. Fly within four surface units to collect. Costs/cooldowns remain, upgrades survive pilot respawn, and **R** resets them. **F3** lists progression. See `docs/SPELL_PROGRESSION.md` for the inherited behavior; research and exploration boons will replace this progression in later milestones.

The globe's **Tactical Radar** local radar shows forward-up navigation and waypoint distance/bearing. Press **M** for the paused world survey; left click a marker or surface position to set a waypoint, right click/Delete to clear, and M/Esc to resume. Sites begin as anonymous hints and reveal names within 65 m. Owned antimatter/allied drones are tracked globally; neutral/rival antimatter and threats reveal locally. Drone search reaches the full globe, with longer distant delivery trips. See `docs/STRATEGIC_NAVIGATION.md` for layout, visibility rules and controls.

Open `project.godot` in Godot .NET, build the C# project, and press **F5 (Run Project)** to launch `scenes/PlanetGame.tscn`. **F2** pauses and opens planet-size selection; **Start new planet** resets the match at that size. **R** restarts at the selected size. Selection lasts for the application session. Running `scenes/Game.tscn` directly opens the flat-island reference arena. Current controls are listed in `docs/PROGRESS_LOG.md`.

**Spherical flight lab:** press **F6** during gameplay, or run `scenes/PlanetFlightTest.tscn`. Compare the four named presets with adjustable radius, speed, and altitude. **Esc** releases the mouse for settings, **V** toggles cameras, **F** dashes, **R** resets flight, and **F6** starts a fresh spherical gameplay match. The lab includes inward gravity, surface-relative upright flight, globe distance markers, and base scale references. See `docs/SPHERICAL_WORLD.md` for implementation details and integrated playtest steps.

The compact HUD shows health/energy, rival base/pilot objective status, respawn countdowns, and selected equipment. Victory requires the rival base destroyed and its pilot defeated. Both pilots respawn after **10 seconds** while their respective base survives; a respawn clears the rival's earlier defeat. Destroying the player base causes defeat. Results freeze gameplay and release the mouse; press **R** to restart. One designated rival is tracked through its respawns; the ecosystem director does not spawn extra rival ships. Use **Plasma Bolt** against the rival base. Press **F3** to show/hide the debug and controls panel (off by default); **PgUp / PgDn** scroll its grouped diagnostics.

Look for the **RIVAL PILOT** label on the purple/magenta placeholder near the red base. Its flight follows terrain/water at a controlled height, approaches antimatter closely enough to collect it, and caps combat altitude so it cannot drift into the sky.

**Right-click activates Claim** (2 energy, 0.4-second cooldown). Aim at an antimatter globe or the ground before it: the cyan bolt flies along the aim direction, then skims terrain/water along a fixed horizontal heading and claims the first neutral/rival loose globe it hits. Misses expire after 120 units or 4 seconds. Claimed globes turn cyan and remain in the world for your collector drone to collect and deliver to the base. The player cannot directly collect antimatter or attract globes; base delivery increases energy capacity/regeneration. **Tab** cycles the five LMB combat/utility items; **Q/E/F/G** activate Pulse Burst / Energy Shield / Dash / Guardian Drone. Direct terrain-shaping controls are hidden for now; Plasma Bolt impact craters still work.

Loose antimatter spheres touch the ground/water, roll downhill, and collide with terrain, bases, and solid objects. Radius scales as `(0.35 / 3) * pow(amount, 0.65)` (10 antimatter ≈ 0.521 units; 80 ≈ 2.014). Same-owner spheres attract and merge after 1.25 seconds of supported contact, growing smoothly over 0.6 seconds while retaining all antimatter. Different owners stay separate. Carried spheres suspend physics until released.

Base storage capacity still scales with remaining health (rounded up). Damage spills storage above that limit into neutral physical globes around the base; destruction spills everything and leaves a reduced ruin silhouette. Surviving bases repair **3 HP/second after 5 seconds without damage**. Repair restores storage capacity, but antimatter must still be recovered. Claim spilled globes before your drone can haul them home. Energy capacity and regeneration still use the inherited starting-power allowance plus retained base antimatter, up to the power cap. Withdrawal or damage reduces this immediately; repeated deliveries cannot stack permanent power. Current energy is clamped when capacity falls. **F3** shows effective/intact storage limits, repair state, and the base-power bonus. Visual stages still follow storage; explicit purchased tiers and rebuilding are the next mechanics milestone.

Neutral creatures drop their intrinsic antimatter on death (12 each by default). Deaths, base spills, and drone destruction release randomly sized, exactly conserved fragments one at a time at **50-ms intervals**, with zero launch velocity and a brief merge grace. Drops fall under gravity. Resource-bearing neutral spawns add their reserve to the world tally; subsequent deaths/transfers conserve it. The shared flight ceiling is **12 units above terrain/water**, including swarms and ranged casters. See `docs/PLAYTEST_MECHANICS.md` for inherited defaults and the hands-on test sequence.

Antimatter-loop checks also cover damage/destruction spillage, exact integer remainders, claim/drone recovery, spawn-order-independent initial bonuses, capped power recalculation, withdrawal, and re-delivery. Physics checks verify base spills settle on the heightmap outside its collider.

From the repository root in PowerShell:

```powershell
dotnet build MagicCarpetRemastered.sln
$godot = 'D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe'
& $godot --headless --path . --editor --import --quit
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

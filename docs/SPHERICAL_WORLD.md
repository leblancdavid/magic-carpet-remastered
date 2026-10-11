# Spherical Worlds

## Direction

User direction (2026-10-10): 800 radius felt good during flight playtesting. Adopt **Small 500 / Medium 800 / Large 1,000 / XL 1,200**, with Medium as the default, and bring all existing prototype gameplay into the spherical world. More participants on larger planets is a future direction; networked human multiplayer remains undecided.

**F5 (Run Project)** now launches `scenes/PlanetGame.tscn`, the full playable spherical prototype. **F2** opens the paused size menu; choose a size and **Start new planet** to restart the match. R restarts at the current size. Size selection persists for the application session; a fresh application starts Medium. **F6 in-game** switches to the flight lab and back to a fresh spherical match. Editor **F6 (Run Current Scene)** runs whichever scene is open; `Game.tscn` remains the flat-island regression/reference scene.

## Playable Spherical Features

- Collision-based carpet flight at the previous 24-unit/second cruise speed, acceleration, drag, banking, and first-person/chase cameras. Heading and velocity are transported with changing local up. Tangent-step curvature drift is removed without discarding the collision solver's radial displacement.
- Surface-relative flight clearance remains **2–12 units** for the player. Other actors retain their original minimum clearance and shared 12-unit ceiling. The gameplay carpet's magical lift/terrain-follow constraint maintains altitude; loose mana receives constant **9.8-unit/second² radial gravity** with global downward gravity disabled.
- Procedural colored spherical land and oceans, chunked mesh/collision terrain, crater/raise/lower/flatten APIs, deferred/coalesced rebuilds, and visible firebolt craters. Direct shaping controls remain hidden per earlier user direction.
- All six existing spell behaviors: Firebolt, Arcane Burst, Mana Shield, Wind Dash, Guardian, and RMB Possess; LMB/Tab selection and direct Q/E/F/G hotkeys remain.
- Five exploration beacons grant the previous rank-II combat/utility upgrades. Radial flight-column collection, respawn retention, and match-reset behavior are preserved.
- Amount-scaled physical mana spheres, ownership colors, real contacts/rolling, same-owner merging, collision with world/castles, and ballistic death/spill fragments. Player contact never collects mana.
- Possession starts along the aim direction, transitions to surface skimming, transports its tangent heading around the globe, and preserves mana amounts when claiming a globe.
- Upright player/rival castles, retained-storage-derived power/regeneration, growth/ruins, reduced capacity/spills on damage, and delayed surviving-castle repairs.
- Both balloon haulers use curved surface routes, radial hover/scoop/dock positions, local cargo offsets, delivery, overflow, and pop spills. Spherical seek range now covers half the circumference to reach remote claimed drops; distant hauling takes longer. Flat-arena tuning remains 46.
- All existing enemy roles and the designated rival wizard use tangent steering and radial altitude. Creature spawns follow the player into distant regions. Surface distance replaces horizontal X/Z checks. Ranged spherical attacks check line of sight and preserve their full aim direction.
- Ten-second castle-backed wizard respawns, rival-defeat reset on respawn, victory requiring rival castle plus wizard elimination, home-castle defeat, result pause/restart, HUD/debug controls, VFX, and placeholder audio.

The starting duel remains compact: castles are approximately 280–290 surface units apart and the player starts near home. Three resource/encounter fields branch around that loop, while Redglass Spire and far-side Starfall Observatory provide distant upgrades. Landmarks bias regional encounter roles. All-Seeing Eye radar and the M-key world survey provide discovery/ownership markers and waypoints. See `docs/STRATEGIC_NAVIGATION.md` for layout, navigation and long-route tuning. Size changes geometry/travel distance, not participant count or victory rules.

## Terrain Implementation

`HeightmapArena` retains flat mode for the existing regressions and exposes shared vector-based surface geometry for spherical mode. Planet center is `(0, -radius, 0)`, keeping the initial hemisphere near the former arena's origin.

`PlanetTerrain` splits six projected cube faces into eight-by-eight patches per face. Coarse globe meshes use four subdivisions per patch; nearby actors receive approximately two-unit world-space sampling and matching collision. Nearby detection includes patch bounds and is refreshed every half-second. Visual skirts cover mixed-detail mesh gaps. Both face boundaries evaluate the same direction-based procedural heights and persistent deformation stamps, so edits remain continuous and rebuild every affected patch. Sphere oceans use the shared sea elevation. Live surface queries protect placement/physics between deferred collision rebuilds.

Combat performance update: crater/LOD rebuilds use a coalesced soft frame budget, detail retention has hysteresis, deformation queries use spatially indexed stamps, and unchanged-resolution patches resample only edited vertex regions. Live height changes remain immediate. See `docs/COMBAT_PERFORMANCE.md` for implementation, measurements and profiling steps.

Current limitations: all patches retain a collider (coarse or fine); patch selection still scans active actors, and individual mesh/collider uploads are atomic and can exceed the frame budget. More aggressive collision streaming, smaller rebuild units, and rendered long-session profiling remain performance follow-ups.

## Flight Lab

`PlanetFlightTest.tscn` remains available for isolated comparison. It has the same four named presets, custom radius/speed/clearance controls, smooth reference sphere, two crossing great-circle marker routes, castle scale landmarks, explicit radial gravity/lift, and live angular-rate/distance readouts. Its landmarks have no gameplay/collision. **Esc** releases/captures the mouse for settings; radius changes reset flight. The lab dash is a timed 46-unit/second diagnostic, while gameplay retains the original mana-costed impulse and rank-II upgrade.

## Tuning Rationale

For spherical radius `R`, clearance `h`, and tangential speed `v`:

- local-up rotation rate: `v / (R + h)` radians/second
- sea-level circumference: `2πR`
- nominal sea-level cruise lap time: `2πR / v`
- tangent-plane ground drop over surface arc `s`: `R * (1 - cos(s / R))`
- surface travel is tangential travel scaled by `R / (R + h)`

At 24 units/second and six-unit clearance, the 800-unit planet turns local up at about 1.71°/second; 1,200 units gives about 1.14°/second. Sea-level lap times are about 3.5 and 5.2 minutes respectively. Slower flight reduces orientation-change rate but does not remove geometric curvature. Castle spacing is a separate setting from planet circumference.

## Hands-On Acceptance

1. Start Medium with F5. Confirm the normal HUD, home castle, balloon, and nearby gold beacons. F2 switches among all four sizes by starting a new match.
2. Fly to the well/neutral field, claim mana with RMB, and watch your balloon return it to the castle. Verify merging, rolling, and power growth.
3. Use all spells, discover upgrades, shoot the ground for craters, and assault the rival castle/wizard. The horizon naturally hides distant opponents until approached.
4. Fly around the globe through poles and face seams in both cameras. Check upright heading continuity, low-altitude clearance, chase-camera safety, and distant enemy pressure.
5. Test castle repair/spill recovery, player/rival respawns, both elimination orders, result pausing, and R restart. Verify selected size is retained and upgrades/world edits reset.
6. Compare Small's 500 radius with Medium's 800 at the same movement speed. Judge curvature during strafing, dashes, and long-distance flight. Larger presets keep the same combat speed.

## Verification

```powershell
dotnet build MagicCarpetRemastered.sln
$godot = 'D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe'
& $godot --headless --path . 'res://tools/PlanetFlightChecks.tscn'
& $godot --headless --path . 'res://tools/PlanetGameplayChecks.tscn'
& $godot --path . 'res://scenes/PlanetGame.tscn'
```

Flight checks cover transported full laps/poles, gravity, surface distances, lab controller odometer/clearance, and radius reset. Gameplay checks cover all four real gameplay presets, surface placement/ceilings, radial deformation/seams, actual flight and wizard movement on different hemispheres, underside mana gravity/contact merging, possession across a cube seam, balloon delivery/power, all spell types, discovery, castle spill/repair conservation, respawns, victory/defeat, and menu pausing. Both exit nonzero on failure. Optional rendered capture: append `-- --capture=<absolute-existing-directory>/planet.png` to a checks command without `--headless`. The existing flat-arena suites continue to exercise the shared mechanics.

## Remaining Work

- Hands-on tuning of the integrated spherical flight/combat/logistics loop, especially Small and XL.
- Longer-session terrain-edit/physics performance profiling and streaming refinement.
- Richer biome content and more distributed strategic layouts/participants. Current matches retain one designated rival.
- Stronger AI obstacle routing, globe-aware tactical map, presentation/audio polish, and the existing campaign backlog.
- A separate decision and milestone for networked human multiplayer.

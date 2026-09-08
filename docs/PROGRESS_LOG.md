# Progress Log

## 2026-09-08

### Completed

- Initialized the repository as a Godot C# project shell.
- Added `project.godot`, `MagicCarpetRemastered.csproj`, and `MagicCarpetRemastered.sln`.
- Added project folders: `scenes/`, `scripts/`, `assets/`, `tools/`, and `docs/`.
- Added project guidance in `AGENTS.md` and startup notes in `README.md`.
- Documented original DOS package discovery in `docs/DISCOVERY.md`.
- Documented the full build roadmap in `docs/BUILD_ROADMAP.md`.
- Created the first 3D gameplay scene at `scenes/Game.tscn`.
- Added procedural scene construction in `scripts/game/Game.cs`.
- Added carpet flight controller in `scripts/player/CarpetFlightController.cs`.
- Added chase and first-person camera modes with `V` camera toggle.
- Added mouse-look, WASD movement, ascend/descend, drag, banking, and terrain clearance.
- Added primary spell projectile in `scripts/spells/SpellProjectile.cs`.
- Added mana-gated casting, cooldown, and spell cost display.
- Added projectile impact burst in `scripts/spells/SpellImpactBurst.cs`.
- Added mana pickups in `scripts/world/ManaPickup.cs`.
- Added simple chasing monster in `scripts/enemies/SimpleMonster.cs`.
- Added enemy contact damage, hit flash, death, and mana drops.
- Added HUD in `scripts/ui/Hud.cs` showing health, mana, camera mode, controls, and status text.
- Added restart input with `R`.
- Replaced the flat arena with a procedural heightmap island in `scripts/world/HeightmapArena.cs`.
- Added water plane and height-band terrain colors.
- Fixed player spawning inside terrain by placing gameplay objects relative to terrain height.
- Fixed terrain trapping by clamping flight clearance directly against the heightmap surface.
- Fixed impact burst and mana drop placement warnings by setting `GlobalPosition` after nodes enter the tree.
- Fixed terrain visibility by removing transparent water overdraw and using vertex-colored terrain.
- Added speed and terrain-clearance debug readout to the HUD.
- Added player damage invulnerability after hits, including HUD state and carpet flash feedback.
- Added a ranged monster attack with windup telegraph and enemy projectile damage.
- Improved combat VFX with configurable impact burst colors, purple enemy projectile impacts, and a larger enemy death burst.
- Added code-generated placeholder audio for flight hum, spell casting, mana pickup, and hit events.
- Tuned first-pass flight feel with higher top speed, capped acceleration/deceleration, velocity-based banking, speed-responsive chase camera distance/FOV, and slightly higher terrain clearance.
- Added `docs/LEARNINGS.md` to preserve durable implementation, playtest, and research lessons.
- Updated `AGENTS.md` to require documenting reusable lessons in the appropriate `docs/` file.
- Began Phase 2 terrain deformation by changing `HeightmapArena` from formula-only height queries to a stored vertex height grid with bilinear `HeightAt` sampling.
- Added terrain deformation APIs for craters, raising, lowering, and flattening, with full terrain mesh/collision rebuilds after edits.
- Marked terrain collision as deformable and wired firebolt impacts into terrain crater creation.
- Added right mouse crater casting for direct terrain deformation testing, including mana cost, cooldown, HUD help text, and impact feedback.
- Optimized terrain deformation edits to scan only the affected height-grid bounds instead of every vertex in the arena.
- Added HUD terrain diagnostics showing the most recent edited vertex count and terrain rebuild time.
- Changed terrain edits to update the height grid immediately while deferring and coalescing mesh/collision rebuilds to one rebuild per frame.
- Verified `dotnet build MagicCarpetRemastered.sln` after each implementation pass.

### Current Prototype Controls

- `WASD`: fly horizontally
- `Space`: ascend
- `C`: descend
- Mouse: look
- Left mouse: cast primary firebolt
- Right mouse: cast terrain crater
- `V`: toggle chase/first-person camera
- `R`: restart arena
- `Esc`: capture/release mouse

### Known Gaps

- Godot runtime is tested manually by the user; CLI runtime verification is blocked because `godot` is not on `PATH`.
- Terrain deformation scans affected vertices and coalesces rebuilds, but still rebuilds the full mesh/collision instead of partial chunks.
- Terrain deformation currently has crater gameplay only; raising/lowering/flattening APIs exist but are not exposed through distinct spells yet.
- Flight feel has a first tuning pass and needs runtime playtest validation.
- Enemies fly directly at the player with no steering, avoidance, or distinct roles.
- Spell system is still one hardcoded primary projectile.
- Mana loop exists only as pickups, spell costs, and enemy death drops.
- No castle, mana storage, collector, territory, or enemy wizard systems yet.
- No original art/audio pass beyond simple procedural meshes/materials and synthesized placeholder tones.

### Next Recommended Work

- Playtest Phase 1 flight/combat in Godot and capture tuning feedback.
- Playtest terrain crater spell and firebolt craters in Godot, then tune crater radius/depth/mana cost.
- Replace full terrain rebuilds with a chunked rebuild path if deformation remains too expensive after runtime playtesting.
- Add a simple castle placeholder and mana storage target.

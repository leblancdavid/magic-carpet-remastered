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
- Split the terrain mesh/collision into reusable chunks and now rebuild only the chunks touched by deformation edits.
- Expanded HUD terrain diagnostics to include the number of rebuilt chunks.
- Verified `dotnet build MagicCarpetRemastered.sln` after each implementation pass.
- Added terrain spell mode switching for crater, raise, lower, and flatten using `1` through `4`.
- Routed right mouse terrain casting through the selected terrain mode instead of crater-only behavior.
- Added terrain mode and control hints to the HUD.
- Set terrain casting mana cost to zero so terrain mode testing is not blocked by mana.
- Added a terrain-height fallback for RMB casting so deformation works even when physics ray hits are flaky.
- Increased default terrain edit radius, crater depth, and raise/lower height to make deformation easier to see.
- Verified `dotnet build MagicCarpetRemastered.sln` after the terrain mode implementation.
- Began Phase 3 mana economy work by adding a player mana capacity, spend helper, and excess-mana routing to a mana well.
- Added `scripts/world/ManaWell.cs` as a storage source with ambient regeneration, pickup spawning, and mana deposit/steal APIs.
- Updated `ManaPickup` to home toward the player or well and to let monsters absorb mana.
- Updated `SimpleMonster` to contest mana near the well, steal mana from storage, and visibly gain mana reserve.
- Spawned a mana well in the main game scene and surfaced well storage in the HUD.
- Verified `dotnet build MagicCarpetRemastered.sln` after the Phase 3 implementation pass.
- Began Phase 4 castle and territory work by adding `CastleKeep` with storage, health, and growth stages.
- Added `ManaCollectorSpirit` to route mana between the well and castles, creating a visible territory loop.
- Spawned player and enemy castles in the main scene and surfaced their status in the HUD.
- Verified `dotnet build MagicCarpetRemastered.sln` after the Phase 4 implementation pass.
- Began Phase 5 enemy ecosystem work by adding role-based enemy archetypes for flying swarms, ground beasts, ranged casters, castle attackers, mana thieves, and an enemy wizard.
- Added `EnemyEcosystemDirector` to control biome/intensity-based spawning and initial wave setup.
- Routed enemy projectiles into castle damage so siege units can pressure territory as well as the player.
- Expanded the HUD with ecosystem pressure and role population readout.
- Verified `dotnet build MagicCarpetRemastered.sln` after the Phase 5 implementation pass.
- Tuned the phase 5 ecosystem by adding per-role population caps, a gentler intensity ramp, and better spawn fallback logic.
- Added retreat-and-deposit behavior for mana thieves and enemy wizards so they can carry pressure back to the enemy castle.
- Verified `dotnet build MagicCarpetRemastered.sln` after the phase 5 tuning pass.
- Fixed player fireballs so they damage the new phase 5 ecosystem enemies as well as the earlier `SimpleMonster`.
- Adjusted castle-attacker selection so red siege units can actually spawn from the castle ruins zone during playtest.
- Added an explicit HUD color/role legend to make the phase 5 enemy roles easier to identify in-game.
- Verified `dotnet build MagicCarpetRemastered.sln` after the phase 5 visibility and combat routing fix.

### Current Prototype Controls

- `WASD`: fly horizontally
- `Space`: ascend
- `C`: descend
- Mouse: look
- Left mouse: cast primary firebolt
- Right mouse: cast selected terrain mode
- `1`: terrain crater mode
- `2`: terrain raise mode
- `3`: terrain lower mode
- `4`: terrain flatten mode
- `V`: toggle chase/first-person camera
- `R`: restart arena
- `Esc`: capture/release mouse

### Known Gaps

- Godot runtime is tested manually by the user; CLI runtime verification is blocked because `godot` is not on `PATH`.
- Terrain deformation scans affected vertices, coalesces rebuilds, and rebuilds only affected chunks, but the chunk size is still a tuning tradeoff.
- Terrain deformation modes are working, but still need runtime tuning and feel validation.
- Flight feel has a first tuning pass and needs runtime playtest validation.
- Enemies fly directly at the player with no steering, avoidance, or distinct roles.
- Spell system is still one hardcoded primary projectile.
- Mana loop exists only as pickups, spell costs, and enemy death drops.
- No castle, mana storage, collector, territory, or enemy wizard systems yet.
- Phase 3 is only a first-pass mana loop; it still needs runtime tuning and playtest validation.
- Phase 4 is only a first-pass castle layer; it still needs runtime tuning and playtest validation.
- Phase 5 is a first-pass enemy ecosystem; it still needs runtime tuning and playtest validation.
- Phase 5 now has basic role caps and retreat loops, but it still needs runtime tuning and playtest validation.
- Phase 5 combat readability was improved by fixing damage routing to the new enemy type and adding a HUD color legend, but the mana economy still needs signoff tuning.
- No original art/audio pass beyond simple procedural meshes/materials and synthesized placeholder tones.

### Next Recommended Work

- Playtest Phase 3 mana economy in Godot and capture tuning feedback.
- Tune player mana capacity, well storage capacity, ambient regen, pickup attraction, pickup spawn rate, and enemy contesting.
- Keep Phase 2 and Phase 1 on the playtest backlog for later signoff if further feel issues appear.
- Playtest Phase 4 castle loop in Godot and capture tuning feedback.
- Tune castle scale, health, storage capacity, and collector speed/routing if the territory loop feels unclear.
- Playtest Phase 5 enemy ecosystem in Godot and capture tuning feedback.
- Tune enemy spawn pacing, role mix, enemy health/damage, and wizard pressure if the ecosystem feels too sparse or too aggressive.
- Re-test Phase 5 now that fireballs damage ecosystem enemies and confirm the red caster/wizard mix appears at low-to-mid pressure.

# Progress Log

## 2026-09-09

### Completed

- Condensed the user's mechanics research into `docs/ORIGINAL_MECHANICS_REFERENCE.md` as a clean system-design reference.
- Updated `docs/BUILD_ROADMAP.md` so Phase 3 through Phase 8 explicitly track possession, owned mana, balloon logistics, castle regression, equilibrium quotas, two-hand spell loadouts, spell discovery, auto-targeting, and map/radar needs.
- Linked `docs/DISCOVERY.md` to the new mechanics reference and added an original-spirit lesson in `docs/LEARNINGS.md`.
- Researched the original mana economy at a high level from the local DOSBox package inventory and public references.
- Updated the plan so original-style claimed mana and passive regeneration are the next active priority before further spell/enemy tuning.
- Scanned the current mana/castle/collector code and confirmed the prototype still uses fixed player mana, direct pickup refills, neutral well storage, and source-to-castle collector routing rather than original-style claimed mana ownership.
- Tried to inspect the provided YouTube gameplay reference, but automated transcript/frame access was blocked and `yt-dlp` is not installed in this environment.
- Installed `yt-dlp`, downloaded a low-resolution copy of the provided gameplay reference into temp storage only, sampled frames with `ffmpeg`, and documented observed HUD/mana/castle/balloon takeaways in `docs/DISCOVERY.md`.
- Began the original-style claimed mana implementation: added player `ClaimedMana`, passive regeneration from claimed mana, claimed-mana growth from pickups, and HUD display for claimed mana.
- Changed player castle deposits to add to the player's claimed mana pool, so collector deliveries now increase long-term player mana power.
- Stopped auto-banking current player mana into the neutral well because current mana now regenerates from claimed mana instead of acting as overflow cargo.
- Verified the claimed-mana pass with a temporary-output `dotnet build` path.
- Rebased the progress log into per-day sections so the 2026-09-08 implementation passes and the 2026-09-09 research/claimed-mana/doc work are recorded separately.

### Current Prototype Controls

- `WASD`: fly horizontally
- `Space`: ascend
- `C`: descend
- Mouse: look
- Left mouse: cast selected quick spell
- `Q`: cast Arcane Burst area damage
- `E`: cast Mana Shield defense
- `F`: cast Wind Dash mobility
- `G`: cast Guardian summon
- `Tab`: cycle LMB quick spell
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
- Flight feel has a first tuning pass and needs runtime playtest validation.
- Terrain deformation modes are working, but still need runtime tuning and feel validation; chunk size is still a tuning tradeoff.
- Enemy steering remains simple and will need a later polish pass.
- Mana ownership is not yet visually distinct. Pickups claim into the player pool or deposit to the neutral well; there are no neutral/player/enemy claim states, colors, or a possession action/spell yet.
- Collector spirits still shuttle from the well to castles. True balloon-style hauling of owned loose mana is not implemented.
- No finite world mana budget or equilibrium quota tracking yet, so the eventual level objective is not gated on stored mana.
- Spell system now has a basic data model plus firebolt, Arcane Burst, Mana Shield, Wind Dash, Guardian, terrain shaping modes, and first-pass quick-slot cycling, but still lacks unlocks, upgrades, two-hand loadout behavior, and runtime validation.
- No minimap/radar or full-world map yet; ownership and threats are not shown on a tactical display.
- Exact original timings for mana claiming, balloon hauling, castle growth, and regeneration still need higher-resolution/manual video observation, but first-pass visual references confirm balloons, large mana pearls, castle scale, minimap importance, and top-strip HUD priority.
- Phase 3 is only a first-pass mana loop; it still needs runtime tuning and playtest validation.
- Phase 4 is only a first-pass castle layer; it still needs runtime tuning, sanctuary/respawn behavior, and damage regression with mana spillage.
- Phase 5 enemy ecosystem is functionally complete for now; detailed tuning is intentionally deferred.
- Phase 6 is a first-pass spell set and needs the broader loadout/discovery systems before it can be signed off.
- No original art/audio pass beyond simple procedural meshes/materials and synthesized placeholder tones.

### Next Recommended Work

- Playtest claimed mana regeneration and spell sustainability in Godot and capture tuning feedback.
- Tune player mana capacity, regen, pickup values, and enemy contesting around the claimed-mana pool.
- Implement loose mana ownership with neutral/player/enemy states and a possession action/spell.
- Convert collector spirits into balloons that visibly haul owned loose mana back to castle storage.
- Connect enemy thieves/wizards to claimed mana ownership so resource control creates strategic pressure.
- Track finite world mana and castle-stored quota progress toward an equilibrium win condition.
- Add castle sanctuary/respawn behavior and damage regression with stored-mana spillage.
- Add two active hand/button loadout bindings with fast reassignment.
- Add a minimap/radar showing mana, castles, balloons, threats, and ownership colors.
- Keep Phase 2 and Phase 1 on the playtest backlog for later signoff if further feel issues appear.
- Defer detailed Phase 5 enemy tuning and Phase 6 spell tuning until the claimed-mana loop is in place.

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
- Fixed phase 5 enemy attack checks to use horizontal distance so ranged attacks, thief steals, wizard deposits, and castle rams can trigger reliably in the flying arena.
- Increased enemy projectile visibility and lifetime so ranged attacks are easier to notice during playtest.
- Verified `dotnet build MagicCarpetRemastered.sln` after the phase 5 interaction fix.
- Moved the player and enemy castles to opposite sides of the island so siege pressure has room to play out.
- Widened ranged enemy firing windows and projectile travel so projectiles are easier to observe in playtest.
- Closed Phase 5 as functionally complete for the current prototype and deferred detailed enemy tuning until the broader game loop is more complete.
- Began Phase 6 by adding `SpellDefinition` as a small spell data model for id, display name, kind, mana cost, cooldown, and description.
- Routed the current firebolt and terrain shaping abilities through spell definitions while preserving the existing mouse controls.
- Added HUD spell loadout text so the active LMB/RMB spell bindings are visible during playtest.
- Added `Arcane Burst`, a `Q`-cast area-damage spell that detonates at the aimed point, damages nearby enemies, and shows a larger purple burst VFX.
- Tagged the legacy `SimpleMonster` enemy group so area damage can affect both old and phase-5 enemy types.
- Verified `dotnet build MagicCarpetRemastered.sln` after adding the first Phase 6 area spell.
- Added `Mana Shield`, an `E`-cast defensive spell that spends mana, runs on its own cooldown, briefly reduces incoming damage, and shows cyan shield feedback on the carpet/HUD.
- Verified the shield spell with a temporary-output `dotnet build` because the normal Godot build DLL was locked by a running `.NET Host` process.
- Added `Wind Dash`, an `F`-cast mobility spell that spends mana, runs on its own cooldown, boosts the carpet in the aimed direction, and shows a teal movement burst.
- Verified `Wind Dash` with the same temporary-output `dotnet build` path.
- Added `Guardian`, a `G`-cast summoning spell that creates a temporary allied orb which seeks and damages nearby ecosystem enemies.
- Added `SummonedGuardian` as the first friendly summoned unit behavior.
- Verified `Guardian` with the same temporary-output `dotnet build` path.
- Added quick-slot behavior: `Tab` cycles the selected quick spell and `LMB` casts the selected spell while direct `Q`/`E`/`F`/`G` hotkeys remain available.
- Verified quick-slot behavior with the same temporary-output `dotnet build` path.
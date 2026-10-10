# Project Learnings

Durable lessons from implementation, playtesting, and research. Use this for understanding that should guide future decisions, not for daily changelog entries.

## Documentation Practice

- Keep `docs/PROGRESS_LOG.md` focused on what changed, what was verified, current gaps, and next recommended work.
- Keep `docs/BUILD_ROADMAP.md` focused on phase scope, status, priorities, and deliverables.
- Keep `docs/DISCOVERY.md` focused on original-game observations and legally safe reference notes.
- Use this file for reusable lessons, technical decisions, gotchas, and design constraints learned while building.
- When a lesson changes phase priorities or scope, update both this file and `docs/BUILD_ROADMAP.md`.

## Phase 1 Prototype Lessons

- A playable prototype benefits from code-generated primitives and procedural content first. This keeps the game runnable without proprietary assets or asset import friction.
- Terrain height must be treated as a gameplay constraint, not just collision. Directly clamping carpet clearance against `HeightmapArena.HeightAt()` fixed terrain trapping more reliably than relying only on physics contacts.
- Spawn positions should be derived from terrain height. Placing the player, pickups, enemies, and pillars with explicit terrain clearance avoids buried or floating gameplay objects after procedural terrain changes.
- Godot scene timing matters for generated nodes. For runtime-created VFX, pickups, and projectiles, add the node to the scene tree before setting `GlobalPosition` when global transforms are required.
- Transparent water over the terrain can hide or flatten terrain readability in the prototype. Opaque/darker water with vertex-colored terrain made the island easier to read.
- The HUD is a fast tuning surface, but permanent diagnostics obscure the gameplay being tuned. Keep health/mana, objective progress, selected spells, and transient feedback in small edge panels; put speed, clearance, resource totals, role legends, and full controls behind a debug toggle.
- Flight motion should use capped acceleration/deceleration rather than uncapped interpolation weights. This avoids frame-rate-sensitive overshoot and makes tuning values easier to reason about.
- Carpet banking reads better when it follows actual local velocity instead of raw input. The visual tilt then reflects drift and deceleration, not just key presses.
- Speed-responsive camera distance and FOV add useful motion feedback before bespoke art or camera effects exist.
- Short post-hit invulnerability is necessary once enemies have contact or projectile damage. Without it, overlapping enemies can drain health too quickly and make damage feel unfair.
- Ranged enemy pressure improves the arena loop even before sophisticated AI. A windup telegraph plus slow projectile gives the player something to dodge while preserving simple enemy movement.
- VFX should communicate ownership and outcome. Orange player firebolts, purple enemy projectiles, and larger red enemy death bursts make combat events easier to parse.
- Synthesized placeholder audio is useful before original audio production. Code-generated tones give immediate feedback for flight, casting, pickups, and hits without introducing asset dependencies.

## Current Technical Constraints

- Discovery rewards should derive stronger effects from authored base tuning and per-player spell rank rather than repeatedly mutating exported values. This prevents duplicate rewards from stacking and separates exploration progression from castle-derived resource power. Initialize summoned-unit lifetime/damage before adding it to the tree, since `_Ready` snapshots lifetime.
- Scene-reload checks resumed from `PhysicsFrame` should defer their restart action to the end of the frame. Synchronous removal during that signal can leave scheduled actors trying to read detached world transforms; the deferred action models normal input timing and gives clean integration checks.

- `dotnet build MagicCarpetRemastered.sln` verifies C# compilation quickly, but it does not prove the Godot scene runs correctly.
- `godot` need not be on `PATH` for runtime verification. Godot .NET 4.6.3 is installed at `D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe`; use the full path for headless imports, regression scenes, and arena smoke runs. Visual feel still requires manual playtesting.
- The prototype still favors direct systems over reusable frameworks. Keep new systems small until flight/combat feel is proven.

## Phase 2 Terrain Lessons

- Deformable terrain needs one authoritative stored height grid. Mesh vertices, collision triangles, and `HeightAt()` clearance queries should all read from that grid so visual terrain, physics, and flight safety stay in sync after edits.
- Radius-based terrain edits should convert world-space bounds into grid index bounds before touching vertices. This keeps crater and sculpt operations proportional to effect size instead of arena size.
- Terrain deformation can safely update the authoritative height grid immediately and defer visual/physics mesh rebuilding. Coalescing rebuild requests avoids duplicate terrain work when multiple spells edit terrain in one frame.
- Chunked terrain rebuilds are the right next step once deferred rebuilds still feel too expensive. The chunk size becomes a gameplay/performance tuning knob, not just an implementation detail.

## Original-Spirit Design Lessons

- Mana should not behave like ordinary shooter ammunition. The original game's strategic identity comes from mana being claimed, hauled, stored, stolen, and converted into long-term power through the castle. Current mana should regenerate from the player's owned/claimed pool so spending spells creates temporary pressure without forcing constant pickup scavenging.
- The underlying design is an embodied RTS economy disguised as first-person aerial spell combat. Prioritize systems where flight, spell choice, mana ownership, balloons, castles, rivals, and terrain deformation interact without scripted events.

## Mana Ownership Loop Lessons

- Make loose mana a first-class owned object rather than a consumable. A `Neutral`/`Player`/`Enemy` ownership state per orb, distinct colors, and a shared group turn possession, hauling, and contesting into simple queries (`GetNodesInGroup("loose_mana")`) instead of bespoke per-system bookkeeping.
- Ownership should gate collection at the source. Per user direction (2026-10-10), players possess mana for balloons to haul; they never attract or absorb spheres of any color. Player-owned mana retains gravity and collisions until a balloon picks it up. Castle delivery grows spell capacity/regeneration; possession only changes ownership.
- An economy spell should be cheap and fast (low mana, short cooldown) because it kicks off a longer ownership chain. Possession now uses a visible, straight-heading terrain-following bolt rather than instant aim-weighted selection; skip carried orbs so balloons cannot be strip-mined mid-flight.
- When a unit hauls owned mana, carry the same `ManaPickup` nodes rather than destroying them on scoop. That keeps one source of truth for amount and ownership. If destruction fragments cargo into smaller globes, debit/consume the original nodes exactly once before scattering the total; do not keep both original cargo and new fragments counted.
- Resource systems benefit from a single tally node. `WorldManaTracker` sums loose/carried spheres, castle storage, and creature reserves, separating intrinsic neutral-creature mana from rival-controlled reserves. Players have no physically absorbed-mana reserve. Current playtest direction gives recurring neutral creatures intrinsic reserves: count them at spawn and conserve them on death, recognizing that those spawns introduce supply. Wizard respawns retain a zero starting reserve and cannot mint physical mana.
- When ownership is transferred on death, revert to neutral except where the enemy is the explicit contesting agent (thieves/wizards). Player-only collection pressure comes from neutral drops; rival drops must be possessed, which creates the targeted incentive to contest, not just blast everything.
- Separate physical resource accounting from derived spell power. `ClaimedMana` is capacity/regeneration power computed from the starting allowance plus retained player-castle storage, capped by maximum power. Include initial stored reserves and recompute from source storage after deposits, withdrawals, or damage; subtracting losses from a previously capped bonus produces incorrect power. Count physical mana in spheres, castles, and enemy reserves, never again in the player's capacity bonus. Initial spell capacity is a starting-power allowance, not part of the world resource supply.
- Castle economic damage needs a deterministic storage limit: intact capacity times remaining health, rounded up, with zero capacity on destruction. Spill only storage exceeding that limit, distribute integer remainders exactly, and debit storage before publishing neutral spheres. Bound the number of new bodies and place them beyond the keep's corner footprint at terrain/water plus sphere radius. Re-delivery must recover retained power rather than stack a permanent historical bonus; power-loss capacity clamps must also clamp current spell mana.
- Physical destruction bursts must preserve airborne release height. Use the spill/launch path that marks surface initialization complete; otherwise the pickup's first tick grounds the fragments and silently cancels their visible ballistic arc. Give outward and upward velocity, small exact-remainder fragments, and a short merge grace so immediately touching same-owner fragments do not recombine at the source. Cache the arena before the first integration step so the shared ceiling applies immediately.
- Auto-repair restores structure/capacity, not resources. Accumulate repair only for the portion of a tick after the last-hit quiet delay, reset delay/fractional repair on damage, and never repair a destroyed castle. Recovered mana must still return through possession and balloon delivery.
- Transfers must debit the source by exactly the amount accepted at the destination. Zero orb amounts immediately and exclude queued-for-deletion nodes so two consumers in one frame cannot absorb the same orb. Distribute integer remainders across death drops rather than truncating them.
- Movement and interaction checks must use the same target point. A balloon flying toward a hover-offset target cannot dock by checking a smaller radius around the ground-level target. Clamp motion steps to prevent overshoot, and do not add hover height repeatedly when returning to an already elevated anchor.
- Orient moving nodes before a capped `MoveToward` step: the step may reach its target exactly, so calling `LookAt(target)` afterward uses coincident origin/target positions. Keep arrival and vertical-only facing guards, and regression-check orientation as well as position; engine errors do not necessarily throw C# exceptions.
- Physical mana uses sphere radius as surface clearance, not an arbitrary hover offset. Keep the mesh/collider at the same radius and scale radius with the cube root of amount so volume represents mana. Ownership does not change sphere movement. Freeze and disable collision while hauled, then restore physics on spill.
- Defer rigid-body contact mutations (shape resizing, merging, absorption) out of contact signals. Choose a stable merge survivor, debit the donor immediately, and revalidate availability/ownership on every callback to conserve finite mana across reciprocal and chained contacts. Check sustained contacts when ownership can change after initial contact.
- Godot uses clockwise front-face winding. Double-sided terrain rendering can conceal reversed concave collision faces, while height-based flight clearance conceals the missing physics contact. Check real rolling bodies on the actual heightmap, not only on synthetic box slopes. Leave normal terrain contact penetration to the solver; only correct significant terrain burial or support collider-free water, otherwise height snapping cancels downhill motion.
- Ground-skimming possession requires reachable globe placement: loose globes touch terrain/water while carried globes follow their hauler. Sample the live height grid in short projectile substeps and sweep each segment against each globe's current sphere radius plus bolt tolerance; deferred terrain collision rebuilds and endpoint-only hits are unreliable for fast skimming bolts. Keep horizontal heading fixed instead of steering toward a selected globe.
- Headless regression scenes can exercise actual Godot nodes, world tallies, carry state, and transfers without introducing a mock gameplay framework. `tools/ManaLoopChecks.tscn` covers transfer invariants; arena smoke runs cover startup/normal processing; neither proves flight feel or visual readability.

## UI Readability Lessons

- Container-based, padded panels with bounded widths and word wrapping are more readable over terrain than a single unbounded multiline label. Keep the normal HUD away from the aiming area and hide detailed diagnostics by default.
- Live-updating scrollable debug text must preserve its scroll offset, and key-repeat events must not repeatedly flip a toggle. Passive HUD controls should ignore mouse input so the overlay does not absorb spell clicks.
- Rendered screenshots complement headless input checks: the HUD screenshot review caught objective copy wrapping onto a third line and touching the debug panel. Shorten gameplay copy before expanding panel coverage.

## Level Objective Lessons

- User-directed victory is rival castle destruction AND rival wizard defeat, independent of mana ownership. Track one designated rival lifecycle rather than counting an ecosystem role. Record death before queued deletion and attach the same tracking callback to replacements. Current castle-backed respawns clear that defeat after ten seconds while the castle survives, so killing the wizard first works only until it respawns. Destroying its castle prevents future replacement.
- Wizard death must not unconditionally reload the arena. Keep dead players in place with flight/casting/collision disabled, tick recovery in the pausable objective while their castle survives, then restore health/current spell mana, collision, and brief protection beside the keep. A home-castle loss cancels recovery and remains terminal defeat. Validate actual elapsed physics frames as well as direct timer calls and paused countdown behavior.
- Keep the gameplay root pausable and the result HUD always-processing. An always-processing gameplay root would let newly spawned enemies/projectiles inherit processing while the result is displayed. Handle restart input in the live HUD during pause, unpause before reload, and mark input handled before scene reload detaches the old HUD.
- Test actual scene reload as well as objective flags. A surviving harness must live outside the reloaded `CurrentScene`; assign a current scene only after it is attached to the tree root. Headless mouse capture is not representative, so verify mouse recapture with a rendered regression run.

## Flying Rival Lessons

- Never normalize a combined steering vector that uses a tiny vertical bias to mean hover. When horizontal movement stops, that bias becomes full-speed ascent. Use planar steering plus proportional, speed-limited height correction against the live terrain/water surface, with an explicit altitude band and terrain burial protection.
- Apply the altitude rule to every flying actor, not only wizards: swarms/casters had the same normalization defect. Share terrain/water-relative ceiling queries across players, creatures, guardians, balloons, and physical mana. Clamp upward velocity along with position so holding ascend or dashing upward cannot accumulate pressure at the ceiling. Projectiles must ignore this movement ceiling: camera/muzzle offsets can already put their starting position above it, so altitude-based expiry makes casting from ceiling height fail. Preserve launch direction and ordinary hit/range/lifetime rules; test horizontal, upward, and downward shots from both ceiling height and an above-ceiling muzzle.
- A resource-seeking unit must approach within its actual absorption radius. Stopping eight units from a globe while absorption requires 2.7 units leaves the wizard idle and climbing forever. Real physics-frame sampling catches altitude drift and collection failures that startup/existence tests miss.
- Objective-critical enemies need recognizable silhouettes. A purple sphere reads as another monster or resource. Original primitive robe/hat/carpet geometry, a matching hit collider, and a world label make the rival identifiable without an asset dependency. Verify both default-camera and close-up rendered views.

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
- The HUD is currently the fastest tuning surface. Speed, terrain clearance, camera mode, mana, health, status, and invulnerability state give immediate playtest feedback without extra tooling.
- Flight motion should use capped acceleration/deceleration rather than uncapped interpolation weights. This avoids frame-rate-sensitive overshoot and makes tuning values easier to reason about.
- Carpet banking reads better when it follows actual local velocity instead of raw input. The visual tilt then reflects drift and deceleration, not just key presses.
- Speed-responsive camera distance and FOV add useful motion feedback before bespoke art or camera effects exist.
- Short post-hit invulnerability is necessary once enemies have contact or projectile damage. Without it, overlapping enemies can drain health too quickly and make damage feel unfair.
- Ranged enemy pressure improves the arena loop even before sophisticated AI. A windup telegraph plus slow projectile gives the player something to dodge while preserving simple enemy movement.
- VFX should communicate ownership and outcome. Orange player firebolts, purple enemy projectiles, and larger red enemy death bursts make combat events easier to parse.
- Synthesized placeholder audio is useful before original audio production. Code-generated tones give immediate feedback for flight, casting, pickups, and hits without introducing asset dependencies.

## Current Technical Constraints

- `dotnet build MagicCarpetRemastered.sln` verifies C# compilation quickly, but it does not prove the Godot scene runs correctly.
- CLI runtime verification is currently blocked because `godot` is not on `PATH`; runtime testing depends on opening the project in Godot manually.
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
- Ownership should gate behavior at the source. Player-owned orbs home and auto-collect to the player; neutral and rival orbs should not auto-claim on contact or they bypass the possession spell and the contesting loop entirely.
- An economy spell should be cheap and fast (low mana, short cooldown) because it kicks off a longer ownership chain. Aim-weighted selection avoids a manual targeting cursor while still rewarding intent; skip carried orbs so balloons cannot be strip-mined mid-flight.
- When a unit hauls owned mana, carry the same `ManaPickup` nodes rather than destroying them on scoop. That keeps one source of truth for amount and ownership, and makes "pop to spill" a reassignment, not an oracle that must reconstruct orbs.
- Emerging finite-resource systems benefit from a single tally node. Summing loose mana, castle storage, and in-transit reserves in one `WorldManaTracker` keeps the equilibrium quota readable without pushing accounting logic into every subsystem; treat monster-drop growth (infinite monster spawning) as a known world-total drift.
- When ownership is transferred on death, revert to neutral except where the enemy is the explicit contesting agent (thieves/wizards). Player-only collection pressure comes from neutral drops; rival drops must be possessed, which creates the targeted incentive to contest, not just blast everything.

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
- Full mesh/collision rebuilds are acceptable for the first terrain spell test, but repeated or larger deformations will need chunking, throttling, or deferred rebuilds before they are production-safe.

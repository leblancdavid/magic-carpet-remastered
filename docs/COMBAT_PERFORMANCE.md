# Combat Performance

## Report and first optimization pass (2026-10-10)

User reports brief freezes on fireball impacts, enemy kills, and successful mana possession. The shared effect path, mana searches/resource creation, and synchronous terrain work are the first targets. This report is about event spikes; a rendered combat frame-time baseline is still pending.

### Implemented

- Fireball impacts, ecosystem-enemy deaths, and possession successes use a scene-owned effect pool. Eight effects and their independent fade materials are prepared during game loading; up to sixteen idle effects are retained. Reuse resets age, duration, colors, scale and position. Idle effects stop processing and inherit gameplay pause/restart lifetime.
- Firebolts, possession bolts, impact bursts and mana globes share low-poly sphere meshes. Firebolts share their immutable collision sphere; transient orange effects/firebolts do not cast shadows. Mana growth scales the unit mesh instead of regenerating sphere vertices every physics tick. Collider size and exact mana accounting are preserved.
- Mana ownership swaps among three shared immutable materials instead of creating a material per globe or mutating shader settings during possession.
- `ManaNeighborhood` builds one world-space broad-phase index per physics frame. Attraction/merge and possession queries reuse caller-owned lists; bucket lists and contact dictionaries are recycled. Exact hit, availability, ownership, support, relative-speed and dwell checks remain authoritative. Negative coordinates, unusually large spheres and disposed wrappers are covered. A one-cell motion margin covers normal gameplay movement within the snapshot frame; arbitrary teleports must invalidate the snapshot before querying.
- Terrain deformation stamps are indexed in 32-unit Cartesian cells on the radius sphere. Each bucket preserves edit order, including flatten/additive interactions, and the existing exact chord-distance test still determines influence.
- At unchanged terrain resolution, cached vertex/normal/color data is resampled only near pending edits, including the normal stencil margin. Collision triangles and mesh uploads still rebuild per patch.
- Craters and LOD changes share a coalescing queue processed once per rendered frame with a **soft two-millisecond budget**. Actor-supporting fine patches take priority. The queue finishes at least one patch, so an individual mesh/collider build can exceed the budget. Initial terrain preparation drains its queue during loading. Live surface heights change immediately and retain placement/physics fallback protection before collision catches up.
- Fine-detail retention uses 25 extra units of hysteresis; hidden pending and carried mana do not act as duplicate terrain observers. Edited-vertex HUD counts are now estimates instead of an expensive exact grid scan.
- Firebolt sweep query/result wrappers and terrain skirt builders are explicitly disposed.

### Measurements and verification

`tools/CombatPerformanceChecks.tscn` contains correctness checks plus a CPU lookup microbenchmark. A local headless run with 512 distributed mana globes and 1,000 repeated local queries measured **2.54 ms / zero managed bytes** for indexed candidate lookup versus **323.47 ms / 264,000 managed bytes** for the previous full-group/exact-distance scan. The algorithms perform different broad/narrow-phase amounts of work: these timings demonstrate removed lookup overhead, not an end-to-end speedup or frame-rate guarantee. Index construction, normal frame updates, solver time and GPU rendering are not included in the repeated-query measurement. One small-world seam crater registration measured **0.50 ms**, queuing four patches rather than rebuilding them on the impact call.

Checks cover pooling/reuse and independent fades, shared mana mesh identity, spatial lookup coverage, huge neighbor radii, freed wrappers, immediate terrain heights and bounded/eventually drained queues. Existing mana physics checks compare effective visual radius (`mesh.Radius * instance.Scale.X`) against the collider during growth.

Commands:

```powershell
dotnet build MagicCarpetRemastered.sln
$godot = 'D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe'
& $godot --headless --path . 'res://tools/CombatPerformanceChecks.tscn'
& $godot --headless --path . 'res://tools/PlanetGameplayChecks.tscn'
& $godot --headless --path . 'res://tools/ManaPhysicsChecks.tscn'
& $godot --headless --path . 'res://tools/ManaLoopChecks.tscn'
& $godot --headless --path . 'res://tools/PlaytestMechanicsChecks.tscn'
```

### Next rendered playtest

On Medium and XL, compare firing into the sky, repeated ground impacts, enemy kills, possession successes and dense mana clusters. Compare first use to subsequent uses and record frame-time percentiles/worst frames, physics and GPU time, patch build time, and queue backlog. Repeat after sustained crater creation. A rendered startup smoke verifies launch, not combat hitch elimination.

Remaining possible spike sources: atomic patch collision/mesh rebuilds, immediate creation of all physical drop bodies (release is staggered but body creation still is not), first-use GPU shader compilation, audio sample generation, and unpooled effects from other spells/enemy shots. Profile these before introducing more streaming, fragment ledgers, audio conversion or pooling. The shared lightweight mana mesh/materials reduce drop setup costs without changing release/accounting rules.

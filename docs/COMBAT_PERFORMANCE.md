# Combat Performance

## Report and first optimization pass (2026-10-10)

User reports brief freezes on fireball impacts, enemy kills, and successful mana possession. The shared effect path, mana searches/resource creation, and synchronous terrain work are the first targets. The rendered baseline below now identifies atomic terrain rebuilds and dense mana simulation as follow-up targets.

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

## Repeatable rendered workload (2026-10-10)

`tools/CombatFrameProfile.tscn` constructs full gameplay worlds at Medium (800) and XL (1,200), with a fixed 1280×720 viewing camera and seeded event placement. Player movement, ecosystem AI/spawning, balloon hauling and objective evaluation are held still to reduce workload drift. Rendering, terrain rebuild queues, projectile physics, mana physics/merging, HUD/navigation and existing audio remain active. This is a controlled stationary workload; hands-on flying and longer-session profiling remain separate acceptance steps.

Each world warms up for 180 rendered frames, then records 2,310 frames across idle, first/repeated sky shots, first/repeated ground impacts, first/repeated creature deaths, first/repeated possession, 192-body dense mana creation/simulation, a 256-mana castle destruction, sustained impacts and post-stress recovery. Death/spill scenarios call the real damage/death paths directly; firebolts and possession bolts advance through normal physics. Successful possession and terrain rebuilds are checked. Worlds run sequentially and workloads accumulate stress; later windows are not isolated microbenchmarks. "First" means the first event of that type in that world after warmup, not a guaranteed cold shader cache. The second world can reuse process/driver caches. A fixed seed does not make solver/frame scheduling deterministic.

```powershell
dotnet build MagicCarpetRemastered.sln
$godot = 'D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe'
& $godot --path . 'res://tools/CombatFrameProfile.tscn'
# One size in a fresh process:
& $godot --path . 'res://tools/CombatFrameProfile.tscn' -- --medium
& $godot --path . 'res://tools/CombatFrameProfile.tscn' -- --xl
```

Run with a rendered display; headless mode is rejected. Reports are timestamped CSV and TXT files under `user://combat-profiles` (locally `%APPDATA%\Godot\app_userdata\Magic Carpet Remastered\combat-profiles`), outside source control. Reports include hardware/renderer, viewport, VSync, frame cap, physics tick rate and seed. Summary output includes nearest-rank p50/p95/p99/max, counts above 33.333/50 ms, maximum rebuild cost/backlog and loose-mana body counts (including pending release bodies). Terrain telemetry resets to zero on idle frames, avoiding stale rebuild readings.

Wall-frame samples measure intervals between post-draw signals, including VSync/pacing, physics catch-up, rendering submission and harness overhead. Event injection occurs on the subsequent ProcessFrame, not reentrantly inside post-draw. Godot process/physics monitors are recorded separately and may lag the current sample. GPU execution time is **not measured**; post-draw is not a GPU completion fence. Samples are buffered and reports/percentile sorting are written outside measured windows. Keep the window visible and foreground, and compare equivalent pacing settings.

### Initial local baseline

Godot 4.6.3, Vulkan/Forward+, RTX 4070, 1280×720, approximately 60-Hz frame pacing. Both worlds completed: **4,620 samples / 14 successful possessions**, no runtime errors. Results from one run (milliseconds):

| Window | Medium p95 / max | XL p95 / max |
| --- | ---: | ---: |
| Idle | 17.24 / 18.62 | 17.34 / 20.27 |
| First ground impact | 17.19 / 28.12 | 22.99 / 64.86 |
| Repeated ground impacts | 23.03 / 27.83 | 59.64 / 89.65 |
| Repeated creature deaths | 17.08 / 21.00 | 16.89 / 21.89 |
| Repeated possession | 17.25 / 42.46 | 17.65 / 27.23 |
| Dense mana creation | 152.52 / 165.31 | 146.90 / 151.87 |
| Dense mana simulation | 29.83 / 45.28 | 70.70 / 144.90 |
| Castle spill | 26.77 / 30.14 | 22.47 / 28.05 |
| Sustained impacts after stress | 121.78 / 169.44 | 127.57 / 195.19 |
| Post-stress | 29.48 / 54.07 | 17.38 / 27.59 |

Repeated impact windows recorded maximum atomic terrain batches of **25.57 ms Medium / 87.10 ms XL**, with maximum pending backlogs of two/three patches. XL had 49 frames over 50 ms in that window. A soft two-millisecond queue budget cannot bound a single patch build. **Prioritize smaller atomic mesh/collision work units**, preserving immediate authoritative heights and collision safety, then rerun the same workload.

A fresh-process XL verification run reproduced repeated-impact p95/max **59.09/90.26 ms** with an **86.93-ms** terrain batch, and dense creation max **161.29 ms**. It completed 2,310 samples/seven possessions. Build passed with zero warnings/errors and focused combat checks passed 19 assertions, including active/idle telemetry.

Dense creation windows had no terrain rebuilding, but recorded 48/23 frames over 50 ms. Ongoing dense XL simulation also spiked without terrain work. This implicates body setup/solver/contact/merge pressure as a second profiling track; it does not isolate their individual costs. The combined sustained-impact phase is intentionally mixed stress. Ordinary death/spill windows were relatively stable in this run; one Medium possession outlier still warrants repeat measurement. Do not infer hitch elimination or a universal hardware threshold from one baseline.

## Smaller atomic terrain tiles (2026-10-10)

Nearby fine patches now divide into independently queued mesh/collision tiles with **at most 25×25 cells (1,250 collision triangles)**. The original eight-by-eight coarse patches per face stay intact. Fine subdivisions use a divisor of the original resolution, retaining its vertex grid and approximately two-unit spacing: Small 64 → 4×4 tiles of 16 cells; Medium 100 → 4×4 of 25; Large 126 → 6×6 of 21; XL 150 → 6×6 of 25. XL previously rebuilt 45,000 collision triangles per fine patch. Only intersecting fine tiles, including the normal-stencil margin, enter the deformation queue.

Fine tile nodes are created lazily during queued builds. The existing coarse mesh/collider stays live until the entire fine replacement is ready. Fine-to-coarse transitions likewise build/publish the coarse replacement before disabling and freeing fine tiles. Unchanged-resolution edits resample cached nearby vertices; authoritative heights and chronological crater/raise/flatten semantics remain immediate. Dormant coarse vertex caches are invalidated by fine edits so later LOD cycling cannot restore stale terrain. Visual skirts cover tile/LOD boundaries. Existing hysteresis and the soft two-millisecond budget remain; `PendingPatchCount`/frame rebuild counts now count queued atomic tiles or coarse patches.

### Rendered comparison

Reran the same 4,620-frame Medium/XL workload on the same Vulkan/Forward+/RTX 4070 setup. Both worlds completed, with 14 successful possessions and no runtime errors:

| Repeated-impact measurement | Medium before → after | XL before → after |
| --- | ---: | ---: |
| p95 frame time | 23.03 → 18.00 ms | 59.64 → 17.32 ms |
| Worst frame | 27.83 → 22.15 ms | 89.65 → 18.36 ms |
| Maximum terrain batch | 25.57 → 3.23 ms | 87.10 → 3.14 ms |
| Frames over 50 ms | 0 → 0 | 49 → 0 |

First-impact maxima were 17.07 ms Medium / 18.10 ms XL. A fresh-process XL repeat completed 2,310 frames/seven possessions, with repeated-impact p95/max **17.36/20.03 ms**, maximum terrain batch **2.96 ms** and no impact-window frames over 33.333 ms. These measurements establish a substantial improvement in the controlled impact workload; frame pacing and hardware still affect the reported values.

At this stage, dense mana remained the next priority (addressed in the follow-up below). Its creation max was **190.60 ms Medium / 161.92 ms XL** in the combined run, with zero terrain rebuilds. Mixed sustained stress still reached **153.50/151.14 ms**, despite terrain batches at about three ms. Smaller terrain tiles alone did not eliminate those hitches. Increased fine-tile node/draw counts and longer-distance LOD transitions require longer-session flying/streaming profiling and hands-on acceptance.

### Correctness verification

Build passed with zero warnings/errors. Combat performance checks now pass **123 assertions**, including capped atomic resolution, queued-replacement collision coverage, actual outward radial ray hits on tile/face seams and both hemispheres, coalesced crater/raise collision updates and fine/coarse/fine edit retention on all four presets. Spherical gameplay checks pass **238 headless / 240 rendered assertions**; mana physics **94** and playtest mechanics **397** pass. Inspected the rendered full-game capture for terrain continuity and normal HUD/world visibility. The physics/gameplay suites include real mana settlement, radial merging and deformation behavior. `git diff --check` passes.

### Next playtest

On Medium and XL, compare firing into the sky, repeated ground impacts, enemy kills, possession successes and dense mana clusters. Compare first use to subsequent uses and record frame-time percentiles/worst frames, physics and GPU time, patch build time, and queue backlog. Repeat after sustained crater creation. A rendered startup smoke verifies launch, not combat hitch elimination.

Remaining possible spike sources: dense physical body setup/solver/contact work, first-use GPU shader compilation, audio sample generation, and unpooled effects from other spells/enemy shots. Drop release is staggered but body creation still is not. Terrain uploads are still atomic, now bounded by smaller geometry units; long-distance LOD swaps and increased fine-tile draw counts need continued profiling. Profile these before introducing more streaming, fragment ledgers, audio conversion or pooling. The shared lightweight mana mesh/materials reduce drop setup costs without changing release/accounting rules.

## Dense mana isolation and support-query optimization (2026-10-10)

The profiler now supports **`--mana-isolation`**. After normal world warmup it removes seeded loose mana, samples idle, then creates a fresh 192-body field for each of three cases:

- **Frozen:** real nodes/meshes/colliders registered, frozen with gameplay callbacks disabled.
- **Solver/radial:** native physics, collisions/contact monitoring and the existing custom radial gravity/surface integration remain active; attraction/contact-dwell/growth callbacks are disabled.
- **Full:** normal mana behavior, including attraction, contact dwell and animated merging.

All cases start at identical airborne positions using the real scatter launch path; the original mixed workload initially grounds its stress bodies, so compare cases within their own workload. Each case records 90 creation, 360 active and 60 recovery frames, giving 1,650 samples per world including idle. Full behavior subsequently changes positions/body counts through attraction/merging, so this is attribution evidence, not an exact native-solver subtraction. Callback-gate checks prove the requested modes actually ran; all cases conserve exactly 480 mana.

```powershell
& $godot --path . 'res://tools/CombatFrameProfile.tscn' -- --mana-isolation
& $godot --path . 'res://tools/CombatFrameProfile.tscn' -- --mana-isolation --xl
```

`ProfiledManaPickup` is a tool-only subclass, also used for the 192 stress bodies in the original mixed workload. Reports record synchronous event injection, accumulated managed `_PhysicsProcess` and `_IntegrateForces` time/call counts, and Godot 3D active objects/collision pairs/islands. Managed integration time measures custom radial/support work, **not native solver time**. Callback totals include multiple physics ticks per rendered interval and only instrumented stress bodies. Event, callbacks, physics monitor and wall time overlap; do not add them. Normal gameplay has no profiling stopwatch overhead.

### Diagnosis and implementation

Before optimization, isolated XL frozen and solver/radial-active maxima were **18.85/19.00 ms**, while full-active p95/max was **30.73/44.31 ms**, with managed logic reaching **34.95 ms**. Synchronous creation took **9–12 ms**, and custom radial integration remained below one ms. This identifies managed full-behavior work as a significant contributor in this workload, rather than demonstrating a costly body-creation event or native solver bottleneck.

Every nearby same-owner sphere repeatedly called `IsGrounded` on the same candidates. Ground-support checks now reuse their result within a physics tick while position, velocity, sphere radius, collision mask, water height and authoritative terrain revision remain identical. Availability/carry/pending/grace gates remain before the cache. `HeightmapArena.SurfaceRevision` advances immediately for both flat and spherical edits, before queued collider rebuilding. Deferred merge validation still performs the original support/distance/speed/ownership checks. Terrain/sphere body-entry events no longer allocate deferred absorption closures; only legacy monsters can use that absorption path.

### Rendered results

Final two-size isolation completed **3,300 samples** with no runtime errors. Full-active p95/max: **16.95/18.65 ms Medium**, **16.97/18.74 ms XL**; managed logic maxima **9.21/11.04 ms**. Creation maxima **23.68/25.45 ms**, synchronous injection **9.57/9.71 ms**. Frozen and solver/radial cases stayed near 60-Hz pacing. One earlier optimized fresh-process XL creation window reached **84.93 ms** with a **31.61-ms** logic total; this outlier was not reproduced in the final two-size run and remains first-use/variance follow-up evidence.

The original mixed workload then completed **4,620 samples / 14 possessions**, on the same Godot 4.6.3 Vulkan/Forward+/RTX 4070 setup:

| Measurement | Medium | XL |
| --- | ---: | ---: |
| Dense creation p95 / max | 18.11 / 30.05 ms | 17.07 / 25.53 ms |
| Previous dense creation max | 190.60 ms | 161.92 ms |
| Dense-active p95 / max | 17.94 / 19.52 ms | 17.66 / 20.31 ms |
| Sustained mixed stress p95 / max | 32.81 / 41.65 ms | 17.69 / 20.98 ms |
| Previous sustained mixed stress max | 153.50 ms | 151.14 ms |
| Repeated-impact p95 / max | 17.30 / 18.44 ms | 17.38 / 19.14 ms |
| Repeated-impact terrain batch max | 2.89 ms | 2.89 ms |

No dense-creation/active frames exceeded 33.333 ms in that mixed run. Medium sustained stress still had **15 frames over 33.333 ms**; none exceeded 50 ms. Timing instrumentation, solver scheduling and body trajectories introduce variability, so these are controlled local improvements, not a universal hitch-elimination claim.

Build passed with zero warnings/errors. Mana physics **105**, mana loop **173**, playtest mechanics **397**, spherical gameplay **238**, and combat performance **123** assertions passed. Added same-tick movement, velocity, shrinking radius, water-height, terrain-edit and carried-state support checks. Existing real solver checks retain slope rolling, same-owner contact dwell, growth and conservation behavior.

**Next priority:** hands-on combat/50-ms release acceptance, longer-session flying/LOD/node/draw-count profiling, and the remaining mixed-stress/first-use outliers. Larger populations and accumulated terrain history need separate sustained workloads before further pooling or streaming decisions.

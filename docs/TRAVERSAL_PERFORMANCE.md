# Spherical Traversal Performance

## Current Terminology (2026-10-11)

Gameplay/diagnostic types now use antimatter and equipment. `TraversalFrameProfile.tscn`, route settings, CLI flags and output locations remain unchanged. New reports use `antimatter_bodies`, `antimatter_logic_ms`, `antimatter_integration_ms` and `mixed-antimatter-create`; historical report names/measurements below remain intact. See `TERMINOLOGY_MIGRATION.md`. The terminology pass ran gameplay/combat correctness checks and rendered smoke verification, not a new traversal benchmark.

## Single-surface terrain and scratch buffers (2026-10-11)

Terrain ground and seam skirts now share one ArrayMesh surface/material instead of two draw surfaces. Ground vertices remain at the start of the cached vertex/normal/color arrays; their tail holds the same skirt triangles with independent clockwise flat normals and the same eight-unit radial depth. Ground normals/colors and vertex sampling are retained. Only ground triangle indices feed collision shapes; visual skirts remain noncolliding. Resolution-keyed per-terrain scratch buffers supply render indices and ground collision faces; native setters copy these arrays synchronously before the next build reuses them. Published meshes/shapes retain their own data. Deterministic wrapper release and delayed collision retirement remain active.

The initial single-surface version raised XL managed allocations to 387/376 MiB per lap. Reusing scratch indices/faces reduced them below the original baseline; the intermediate version was not retained. This illustrates why draw batching must be checked against allocation cost as well as render counts.

Fresh rendered XL stress on the same Godot 4.6.3/Forward+/RTX 4070/1280×720 setup, each **7,900 samples / 132 terrain probes**:

| Measurement | Two-surface baseline | One surface + scratch reuse |
| --- | ---: | ---: |
| Lap 1 / 2 peak scene draw calls | 999 / 1,024 | 569 / 595 |
| Initial home draws | 262 | 160 |
| Return 1 / 2 peak draws | 397 / 396 | 233 / 228 |
| Lap 1 / 2 managed allocations MiB | 301.7 / 291.4 | 181.7 / 175.7 |
| Lap 1 / 2 p95 ms | 18.63 / 17.70 | 17.83 / 17.84 |
| Lap 1 / 2 worst wall ms | 62.67 / 58.71 | 21.34 / 36.33 |
| Lap 1 / 2 frames >33.333 ms | 11 / 24 | 0 / 1 |
| Lap 1 / 2 peak static MiB | 468.4 / 398.9 | 286.0 / 312.7 |

Reports: `20261011-144649` baseline, `20261011-145512` final. Peak draws fall about **42%**, allocation deltas about **40%**. Return-home terrain sets stay **360 tiles / 734 meshes / 2,512 nodes**. Faster queue service and frame/physics scheduling change the transient fine working set, so peak memory/frame differences are not isolated GPU-only speedups. P95 remains near VSync pacing; long-frame variance is still present.

Final mixed Medium/XL (`20261011-145913`) passes **13,468 samples / 226 probes / 210 craters**, exact **798 mana** per world and stable terrain returns. Lap maxima are **29.94/22.64 ms Medium**, **20.62/20.49 ms XL**; peak draws **425/415** and **557/583**. A first Medium 192-body creation event reached **75.29 ms** (72.77 ms in synchronous injection, no GC or terrain batch), with the next frame at 41.27 ms. This is not a traversal/terrain-build outlier. Fresh Medium confirmation (`20261011-150108`) passes **5,478 samples / 92 probes / 84 craters**, creation max **21.96 ms**, lap maxima **47.93/20.58 ms**; three lap frames exceed 33.333 ms, none exceed 50 ms. First-use body setup and presentation variance remain gaps; reduced draws do not establish universal hitch elimination.

Verification: clean build; combat checks **183** assertions including every visible terrain mesh's single-surface vertex counts and clockwise flat skirt normals at all preset home/seam/underside locations, plus edit/LOD collision coverage and no accumulated retired children; mana physics **105**; rendered spherical gameplay **240** including captures. Reviewed gameplay capture for continuous visible terrain/world/HUD. Reports stay outside Git. Next hands-on flight/combat/logistics acceptance and focused first-use setup/presentation tracing if those remaining outliers are visible.

## Repeatable route

`tools/TraversalFrameProfile.tscn` runs full gameplay worlds with a surface-relative camera and player observer following two identical diagonal great-circle laps. The route crosses cube faces and both hemispheres, then returns home after each lap. AI, spawning, hauling, objective evaluation, and loose mana movement are held still; their original positions remain terrain observers. Terrain queues, rendering, HUD/navigation and audio remain live. This isolates terrain streaming from input/steering and changing encounters.

Default selection is Medium/XL. `--all` selects all four presets; `--medium` or `--xl` selects one. Default route speed is 46 units/second (the rank-I dash reference), with fixed 60 route steps per second. `--stress` uses 120 units/second to pressure streaming. This is prescribed observer motion, not an input-driven flight test; slow rendering slows wall-clock travel.

```powershell
dotnet build MagicCarpetRemastered.sln
$godot = 'D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe'
& $godot --path . 'res://tools/TraversalFrameProfile.tscn' -- --all --stress
& $godot --path . 'res://tools/TraversalFrameProfile.tscn' -- --xl
& $godot --path . 'res://tools/TraversalFrameProfile.tscn' -- --xl --stress --unpaced
& $godot --path . 'res://tools/TraversalFrameProfile.tscn' -- --stress --mixed
```

Rendered display required. Keep the window visible. Timestamped CSV/TXT reports go to `user://traversal-profiles`, outside source control. Reports record hardware/pacing, wall/process/physics time, terrain rebuild cost/backlog, fine parent/built tile/published mesh counts, Godot render draw/object/primitive counts, node count and Godot static memory. Published meshes include offscreen terrain; render monitors include the entire scene. Static memory is not the managed heap or total process/GPU memory.

Warmup and queue draining at home are outside measured windows. Each measured home window has 120 frames. Each lap records `ceil(2π × radius / (speed / 60))` frames. Wall intervals include frame pacing; diagnostic accounting and collision probes are excluded. Monitor values may lag; viewport GPU timing is now recorded, with the attribution limitations below. The profiler checks real radial terrain ray hits every 60 samples, validates LOD work occurred, bounds queue draining to 2,400 frames, and requires identical settled terrain counts after the two returns. Hysteresis can retain an extra arrival-side ring compared with initial loading; compare repeated returns, not only startup.

## Collision handoff correction

Early all-size runs twice found an empty terrain ray hit at the same Medium second-lap position, while a standalone Medium run passed. Enabling replacement shapes and immediately disabling/freeing the old LOD during rendering did not reliably provide query coverage before the physics broad phase advanced.

LOD swaps now publish replacement geometry immediately but keep old collision for two physics callbacks, allowing a full intervening physics step before retirement. Re-enabling a coarse collider cancels its pending retirement. Retired fine bodies are freed after that grace; meshes retire immediately. Gameplay pause also pauses collision retirement. This briefly overlaps two representations during a swap; it does not change authoritative heights or the soft rebuild budget. Existing all-size collision/edit checks now leave physics retirement active and check it drains.

## Local measurements (2026-10-10)

Godot 4.6.3, Vulkan/Forward+, RTX 4070, 1280×720, approximately 60-Hz pacing. Corrected all-size stress run: **23,434 measured frames / 394 successful actual collision probes**, two laps at each size, stable settled counts on repeated returns, no runtime errors.

| Radius | Lap 1 p95 / max ms | Lap 2 p95 / max ms | Peak pending tiles | Peak built tiles | Peak scene draw calls |
| --- | ---: | ---: | ---: | ---: | ---: |
| Small 500 | 18.66 / 163.24 | 23.01 / 28.31 | 82 | 783 | 1,118 |
| Medium 800 | 17.76 / 55.80 | 19.34 / 26.42 | 79 | 592 | 713 |
| Large 1,000 | 18.21 / 33.62 | 17.74 / 33.91 | 120 | 1,043 | 1,176 |
| XL 1,200 | 20.56 / 146.57 | 18.54 / 98.88 | 274 | 2,088 | 1,032 |

Maximum terrain batches across the laps were **17.12 / 19.72 / 10.77 / 19.80 ms** respectively. Some long wall frames had no corresponding terrain batch, including Small and XL stationary-home outliers. These intervals need further attribution; do not assume all stalls are terrain CPU builds. The stress route deliberately outruns ordinary flight, particularly on XL.

Fresh-process XL at the default **46 units/second** completed **20,030 samples / 334 successful probes**, again with stable return-home counts:

| Measurement | Lap 1 | Lap 2 |
| --- | ---: | ---: |
| p95 / p99 frame ms | 18.18 / 22.86 | 17.53 / 19.37 |
| Worst frame ms | 127.95 | 63.94 |
| Frames >33.333 / >50 ms | 8 / 3 | 5 / 5 |
| Maximum terrain batch ms | 14.01 | 16.49 |
| Maximum pending tiles | 77 | 72 |
| Maximum built fine tiles | 684 | 720 |
| Maximum scene draw calls | 1,104 | 1,184 |
| Maximum nodes | 3,484 | 3,592 |
| Maximum Godot static memory MiB | 305.1 | 318.9 |

XL returned to **360 built tiles / 734 published terrain meshes / 2,512 scene nodes** after both laps. Initial home had 252 tiles due to the smaller entry threshold. There is no monotonically growing live terrain/node working set in these two laps; this is not a long-duration memory-leak proof. Resource/cache retention and managed collection still require separate analysis.

## Verification and next work

### Mixed travelling workload and edit handoff (2026-10-10 session)

`--mixed` adds a measured 90-frame creation window for 192 live stress globes (480 mana) at home, then direct authoritative craters 16 units ahead every 60 route steps. The globes simulate/merge at their physical locations throughout both laps; they do not follow the camera and their count naturally decreases. Existing AI/hauling/seeded mana stays held still. This measures concurrent resource simulation, edits, accumulated edit history and LOD streaming, not input-driven combat. The stationary combat profiler separately exercises actual projectile hits/deaths/possession/spills.

CSV adds loose-body count and instrumented stress-globe logic/radial-integration timing. Those managed callback sums exclude native solver time and overlap other timing columns. World-mana accounting is refreshed/checksummed every 60 samples and at lap boundaries; guards require edits, live callbacks, queue draining, actual terrain hits and identical returned terrain counts. Terrain rays exclude loose mana so a passing globe cannot falsely fail the terrain-coverage check.

The first corrected-probe run reproduced an empty Medium terrain ray during live editing. Replacing an enabled CollisionShape's resource invalidated old support before the new shape entered physics, even though ordinary LOD retirement was already guarded. Live edited tiles now publish a new sibling CollisionShape and retain the previous one for two physics callbacks; old edit shapes then free rather than accumulating children. Deferred entries guard against already-freed shapes when their containing fine body retires. Disabled/unpublished tiles still assign directly. This briefly overlaps old/new support and leaves authoritative surface heights unchanged.

Corrected rendered Medium/XL stress (`20261011-063301`, same RTX 4070/Forward+ setup):

| Measurement | Medium 800 | XL 1,200 |
| --- | ---: | ---: |
| Measured samples / actual terrain probes | 5,478 / 92 | 7,990 / 134 |
| Craters over two laps | 84 | 126 |
| Conserved world mana (318 baseline + 480 stress) | 798 | 798 |
| Lap 1 / 2 p95 ms | 22.00 / 17.54 | 17.57 / 17.04 |
| Lap 1 / 2 max ms | 23.54 / 37.88 | 40.26 / 26.08 |
| Total frames >33.333 / >50 ms | 2 / 0 | 1 / 0 |
| Return 1 / 2 built tiles | 208 / 208 | 360 / 360 |
| Return 1 / 2 published meshes | 579 / 579 | 734 / 734 |

The extra Medium >33-ms frame was a stationary return window (35.69 ms). Medium's 37.88-ms lap outlier spent 32.69 ms between pre/post draw with only 1.52-ms terrain work and no GC pause; these sporadic intervals are not the earlier 100-ms busy pre-ProcessFrame cleanup pattern. No completed mixed run frame exceeded 50 ms. Guards passed without runtime errors. Actor/node counts may decrease through mana merging; exact returned terrain counts remain stable.

Build is clean. Combat performance checks now pass **171 headless and rendered assertions**, including immediate edit-handoff coverage, delayed old-shape retirement and no collision-child accumulation on all presets at home/seam/underside positions. Spherical gameplay **238**, mana physics **105** pass; spherical checks retain the known exit-time ObjectDB warning. Hands-on acceptance and other machines/longer matches remain pending.

### Phase attribution and deterministic resource release (2026-10-10 session)

CSV/TXT reports now divide each interval into: previous post-draw accounting end → ProcessFrame, prescribed route update, scene processing through an `int.MaxValue` process-priority probe, scene tail → FramePreDraw, and FramePreDraw → FramePostDraw. Every sample checks timestamp ordering. Windows `GetThreadTimes` records main-thread CPU; `Process.TotalProcessorTime` records all process threads. These OS counters are quantized (approximately 15.625 ms here); small zero deltas do not mean no work. Other platforms report -1 for unavailable main-thread timing. No forced render synchronization is added. Phase boundaries identify intervals, not native call stacks.

Fresh phase-instrumented XL paced stress baseline (`20261011-051530`) reproduced:

- Lap 2 frame 378: **121.08-ms wall**, **116.65 ms before ProcessFrame**, **109.38-ms main-thread CPU**, terrain **2.48 ms**. Immediately preceding sample contained a full GC with a **7.96-ms pause**.
- Lap 2 frame 2288: **52.47-ms wall**, **48.07 ms before ProcessFrame**, **46.88-ms main-thread CPU**, terrain **2.44 ms**. Again immediately preceded by full GC, with **5.32-ms pause**.

This implicates busy deferred main-thread cleanup rather than the current GC pause, current mesh build or an idle GPU/presentation wait. `BuildPatch` allocated a Godot array, ArrayMesh and ConcavePolygonShape3D wrapper for each rebuilt tile without deterministic release. They now use `using`: the assigned MeshInstance/CollisionShape retains the native resource, while the temporary managed owner releases immediately. This distributes resource release across bounded build work and avoids retaining old native resources until wrapper finalization. No resolution, sampling, collision handoff or gameplay tuning changes were made.

| Fresh paced XL measurement | Baseline stress | Deterministic-release stress | Deterministic-release 46 units/s |
| --- | ---: | ---: | ---: |
| Samples / collision probes | 7,900 / 132 | 7,900 / 132 | 20,030 / 334 |
| Lap 1 / 2 p95 ms | 16.58 / 17.07 | 16.55 / 16.52 | 16.95 / 17.21 |
| Lap 1 / 2 maximum ms | 21.43 / 121.08 | 28.60 / 29.79 | 22.00 / 27.74 |
| Frames >33.333 ms | 2 | 0 | 0 |
| Lap 1 / 2 peak static MiB | 782.8 / 790.9 | 522.5 / 534.2 | 227.4 / 238.5 |
| Return 1 / 2 static MiB | 767.2 / 506.4 | 162.7 / 158.8 | 157.3 / 158.5 |

After-change report stems: `20261011-051829` (stress), `20261011-052416` (46 units/s). Both return to **360 tiles / 734 published meshes / 2,512 nodes** and pass phase-ordering, queue-drain and real collision checks without runtime errors. The initial unpaced phase run (`20261011-051306`, before the release change) passed but did not reproduce historical 350-ms spikes, illustrating run variance. Those earlier intervals are not retrospectively call-stack-attributed; these results support the cleanup hypothesis and demonstrate improvement in the reproduced paced workload, not universal hitch elimination.

Clean build and combat **135**, spherical gameplay **238**, mana physics **105** checks pass. The spherical suite retains the known exit-time ObjectDB warning. Streaming still allocates roughly 300 MiB per lap and default-speed peak draws reach 1,188; deterministic release fixes lifetime batching, not allocation volume or draw count. Next: hands-on traversal/combat and mixed stress acceptance, then investigate remaining spikes with the existing phase probes/native tracing if reproduced.

### Hitch attribution follow-up (2026-10-10 session)

The profiler now enables viewport CPU/GPU timestamps and records managed allocation deltas, managed heap, generation 0/1/2 collections, cumulative GC-pause deltas, Godot resource count and pending collision retirement. TXT reports include the twelve largest wall intervals per world with adjacent samples. Render timing covers the viewport, not the whole presentation/driver path; monitor values can lag. Managed deltas cover all threads and exclude diagnostic accounting; allocation counters are approximate. Godot resource count is not a census of renderer/physics allocations.

`--unpaced` temporarily disables VSync/FPS limits and restores settings on exit. It refreshes observer detail every route step: otherwise faster rendering skips different positions between half-second physics scans, making identical-route settled-count checks nondeterministic. The first unpaced attempt exposed this harness issue (324 versus 360 return tiles); the corrected mode retains the existing equality guard. Extra scans and changed render/physics timing mean unpaced numbers are diagnostic, not a directly comparable gameplay-speed benchmark.

Fresh XL stress runs on the same RTX 4070/Forward+ setup:

| Measurement | Paced lap 1 / 2 | Unpaced lap 1 / 2 |
| --- | ---: | ---: |
| p95 wall ms | 17.42 / 18.30 | 7.93 / 7.01 |
| Maximum wall ms | 46.24 / 377.12 | 348.33 / 368.04 |
| Maximum terrain batch ms | 12.96 / 17.78 | 17.55 / 15.55 |
| Maximum viewport GPU ms | 31.70 / 31.51 | 10.61 / 9.34 |
| Maximum measured GC pause ms | 6.74 / 5.34 | 6.65 / 5.82 |
| Managed allocations MiB | 301.6 / 291.5 | 305.4 / 295.3 |

Each completed run captured **7,900 samples / 132 real collision probes**, drained its queues and returned to **360 tiles / 734 published meshes / 2,512 nodes** after both laps. Paced peak draw count was 1,047; unpaced was 1,150. Build passed with zero warnings/errors. Local report stems are `20261011-034634` (paced) and `20261011-035531` (corrected unpaced); UTC rolled over during the 2026-10-10 local session.

The largest paced interval (377.12 ms) had zero measured GC pause, 2.28-ms terrain work and 1.18/0.51-ms viewport CPU/GPU timing. The largest corrected unpaced interval (368.04 ms) likewise had zero GC pause, 2.13-ms terrain work and 0.48/0.40-ms viewport CPU/GPU timing. Adjacent samples also lack a matching GC/render spike. Disabling VSync did **not** eliminate long stalls. These measurements narrow the search but do not identify their cause: investigate main-thread work outside terrain batches, native resource cleanup, render/presentation synchronization and OS scheduling with finer phase timings/native tracing. Moderate GC pauses and hundreds of MiB of streaming allocations are a separate optimization opportunity, not an explanation for the largest recorded pauses.

- Clean C# build, zero warnings/errors.
- Combat performance **135**, spherical gameplay **238**, mana physics **105** assertions passed.
- All four stress sizes and fresh-process XL default-speed route completed with real collision coverage and settled-count checks.
- `git diff --check` passed.

Next: hands-on travel/map/combat acceptance and mixed workloads after deterministic terrain-resource release, followed by remaining allocation-volume/draw-count tuning. Reuse phase/native tracing if long stalls recur. Continue distant balloon/habitat pacing work. These controlled results do not sign off flight feel or general hitch elimination.

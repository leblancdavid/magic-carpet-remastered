# Spherical Traversal Performance

## Repeatable route

`tools/TraversalFrameProfile.tscn` runs full gameplay worlds with a surface-relative camera and player observer following two identical diagonal great-circle laps. The route crosses cube faces and both hemispheres, then returns home after each lap. AI, spawning, hauling, objective evaluation, and loose mana movement are held still; their original positions remain terrain observers. Terrain queues, rendering, HUD/navigation and audio remain live. This isolates terrain streaming from input/steering and changing encounters.

Default selection is Medium/XL. `--all` selects all four presets; `--medium` or `--xl` selects one. Default route speed is 46 units/second (the rank-I dash reference), with fixed 60 route steps per second. `--stress` uses 120 units/second to pressure streaming. This is prescribed observer motion, not an input-driven flight test; slow rendering slows wall-clock travel.

```powershell
dotnet build MagicCarpetRemastered.sln
$godot = 'D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe'
& $godot --path . 'res://tools/TraversalFrameProfile.tscn' -- --all --stress
& $godot --path . 'res://tools/TraversalFrameProfile.tscn' -- --xl
```

Rendered display required. Keep the window visible. Timestamped CSV/TXT reports go to `user://traversal-profiles`, outside source control. Reports record hardware/pacing, wall/process/physics time, terrain rebuild cost/backlog, fine parent/built tile/published mesh counts, Godot render draw/object/primitive counts, node count and Godot static memory. Published meshes include offscreen terrain; render monitors include the entire scene. Static memory is not the managed heap or total process/GPU memory.

Warmup and queue draining at home are outside measured windows. Each measured home window has 120 frames. Each lap records `ceil(2π × radius / (speed / 60))` frames. Wall intervals include frame pacing; diagnostic accounting and collision probes are excluded. Monitor values may lag, and GPU execution time is not measured. The profiler checks real radial terrain ray hits every 60 samples, validates LOD work occurred, bounds queue draining to 2,400 frames, and requires identical settled terrain counts after the two returns. Hysteresis can retain an extra arrival-side ring compared with initial loading; compare repeated returns, not only startup.

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

- Clean C# build, zero warnings/errors.
- Combat performance **135**, spherical gameplay **238**, mana physics **105** assertions passed.
- All four stress sizes and fresh-process XL default-speed route completed with real collision coverage and settled-count checks.
- `git diff --check` passed.

Next: attribute remaining long wall frames using managed GC/allocation, native resource retirement and renderer/GPU timing; inspect elevated draw counts before deciding on mesh/surface consolidation. Repeat relevant workloads after any change. Continue hands-on travel/map/combat acceptance and distant balloon/habitat pacing. These controlled results do not sign off flight feel or general hitch elimination.

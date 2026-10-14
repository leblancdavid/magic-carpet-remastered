# Hovercraft, Salvage And Startup Follow-Up — 2026-10-13

Approved tuning: double mob rewards, moderate drift/camera banking, slower steep-ground travel, more staggered/scattered salvage and an original triangular craft. Sluggishness was reported during the first minute of fresh and restarted games.

## Shared flight and craft

- Ship heading/aim stays responsive while horizontal velocity retains the previous travel direction. Forward acceleration remains 28 m/s²; lateral grip decays sideways velocity at 1.8/s on supported ground, scaled by air control when airborne. Releasing input brakes with existing drag. Booster retains lateral momentum within the existing speed envelope; jump/Dash/gravity and current-energy payment remain shared.
- Turn-rate and lateral-slip banking smoothly approach at most 24° ship roll, with approximately 10° camera roll in first-person and chase views. Roll rotates around the camera's viewing axis without changing aim. Respawn clears visual bank and velocity. Controls still belong to live interfaces while open.
- Ordinary uphill traversal supports slopes through 65°. Only the uphill component slows progressively above 20°, reaching a 30% requested-speed factor at 65°, with the existing 10 m/s climb cap. Steeper faces still reject uphill travel; lateral travel, free downhill/crest launches, ocean hover and no fall damage remain.
- Rival steering samples ten headings over an eight-metre horizon every 0.75–1s. It rejects excessive uphill slopes and solid-actor obstruction, preserves a detour preference when stalled, and tries the ordinary paid jump after two seconds without progress (three-second retry spacing). Existing twelve-second goal abort/thirty-second retry suppression remains. This is bounded local recovery, not a complete global route planner.
- `HovercraftHull` creates a pointed, faceted delta prism with raised dorsal centre. Human/rival gameplay craft have two emissive rear thrusters and faction colors. The F6 diagnostic craft reuses the hull. Banking affects the visual mesh; the shared collision footprint remains stable.

## Antimatter drops

| Role | Intrinsic reserve |
| --- | ---: |
| Swarm | 6 A |
| Beast | 36 A |
| Heavy beast | 72 A |
| Caster/scavenger | 20 A |

Scavengers now carry the live intrinsic reserve as well as stolen resources. They stop foraging after twelve **additional stolen** antimatter, allowing the last whole globe to exceed that threshold. Standalone defaults/raiders use 24 A (formerly 12); flat simple monsters use 30 (formerly 15). Explicit diagnostic reserves remain respected; live ships have no minted physical reserve.

- Mob deaths partition exact reserves into `ceil(amount × 0.75)` positive integer fragments, capped at 64 per mob. A 72-A heavy therefore produces 54 balls instead of the former 18 balls from a 36-A reward.
- Mob fragments release every 90 ms, with locally tangent outward speeds 1.5–3.5 m/s and radial lift 0.8–1.8 m/s. Golden-angle headings and independently varied speeds spread the patch. Delayed fragments preserve their scheduled velocity and count immediately in the world ledger while hidden/noninteractive.
- Scatter has 1.5s initial merge grace. Existing attraction, supported-contact merging, growth and collector-only delivery remain. Ordinary base/cargo/project spills keep their previous 50-ms gravity-only partition policy.
- Real-physics tests cover full release, landing spread, exact merging conservation, and eight-heavy simultaneous drops (432 initial bodies / 576 A).
- Isolated rendered eight-heavy stress has **40.74 ms** creation and **228.48/287.08 ms wall p95/max** over 360 samples. This deliberately concentrated native-physics workload is not accepted smoothness; use `AntimatterPhysicsChecks.tscn -- --mob-scatter-only` for follow-up. Normal smaller encounter drops and actual spherical matches need hands-on acceptance. An adaptive CCD experiment did not improve this stress and was reverted.

## Startup terrain workload

Previously every mob/loose globe/drone requested a 100–125m fine-detail neighborhood. The dense XL world requested almost all 384 parents, initially queuing about 5,800 tile builds. Terrain also refined an arbitrary north-pole bubble before seeded actors existed.

Seeded terrain now builds its coarse globe first, then Game completes initial detail **after** creating the actual actor layout. Ships retain 100m fine-detail entry / 125m retention ranges and roughly two-metre fine sampling. Other actors request twelve-metre support neighborhoods with lower-resolution parent collision; analytic height/ground constraints continue running. Fine/coarse collision swaps retain delayed retirement and edited terrain history. Runtime changes still use the bounded queue.

`StartupFrameProfile.tscn` measures scene creation and four consecutive fifteen-second wall windows with active AI/combat/collection, then creates a fresh same-seed match. Default is Medium; `-- --xl` selects XL; `-- --quick` measures one fifteen-second window. Coarse process/physics monitors can include scene creation on the first sample; they are not per-frame CPU attribution. Tile totals can likewise include the initial warm batch. Use wall intervals for comparison.

### Rendered observations (RTX 4070, seed 7319)

| Observation | Before terrain changes | Final pass |
| --- | --- | --- |
| XL scene creation, fresh / second match | 1.54 / 1.30 s | 2.49 / 2.18 s |
| XL initial peak terrain queue | ~5,800 | 32 / 39 in first windows |
| XL queue at 60s | 4,675 / 4,608 | 74 / 0 |
| XL meshes at 60s | 1,719 / 1,794 | 909 / 924 |
| XL last-window p95 / max, fresh | 125.93 / 138.75 ms | 61.91 / 75.85 ms |
| XL last-window p95 / max, second match | 81.33 / 87.98 ms | 54.74 / 76.15 ms |

Final Medium creation is 1.31 / 1.09 s; all eight fifteen-second windows have p95 **19.17–20.18 ms**, with queues normally empty and peaks at most 23. Fresh/restart first-window maxima remain **127.71 / 81.43 ms**; later maxima **34.01–43.44 ms**. XL first-window maxima remain **143.97 / 155.56 ms**, and other windows still spike. These active worlds evolve differently with timing/ordinary randomness: results establish removed warmup work and improved late XL observations, not matched deterministic simulation, universal 60 FPS, or elimination of first-use/native physics spikes. Higher-resolution distant support experiments worsened frame times and were rejected.

## Next acceptance

Hands-on first-minute fresh/R restarts, drift/braking and camera comfort, crater escape, salvage readability/hauling and Medium progression. Continue dense XL native-physics/first-use profiling and broader rival deformation/obstacle routing before difficulty and AI balance simulation.

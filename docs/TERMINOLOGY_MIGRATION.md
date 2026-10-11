# Science-Fiction Terminology Migration

## Scope

Completed on 2026-10-11. Gameplay filenames, C# types/namespaces/members, scene references, node/group identifiers, input actions, player-facing text and verification tools now use the science-fiction vocabulary while preserving the accepted playable mechanics.

The economy/construction and real-time management foundations were implemented as separately verified milestones after the rename; owned four-slot equipment and Shift booster follow. See `BASE_DEVELOPMENT.md` and `SHIP_EQUIPMENT.md`. At migration completion, inherited starting power, HP-scaled storage, storage-driven stages, discovery ranks and paused survey were preserved. Subsequent work replaces starting power/stages with explicit tiers and storage-backed energy, defines health-layer siege/storage damage and makes the survey real time with ship-input ownership. Discovery ranks remain until their boon/progression milestone.

## Working Vocabulary

| Existing concept/type | New concept/type |
| --- | --- |
| Castle / `CastleKeep` | Base / `FactionBase` |
| Carpet / `CarpetFlightController` | Ship / `ShipFlightController` |
| Wizard / wizard enemy role | Pilot / rival ship role |
| Mana / `ManaPickup` | Antimatter / `AntimatterPickup` |
| `ManaScatter`, `ManaNeighborhood` | `AntimatterScatter`, `AntimatterNeighborhood` |
| `WorldManaTracker` | `WorldAntimatterTracker` |
| `ManaWell` | `AntimatterSource` |
| Balloon / `ManaBalloon` | Collector drone / `CollectorDrone` |
| Spells namespace/directory | Equipment namespace/directory |
| `SpellDefinition`, `SpellKind` | `EquipmentDefinition`, `EquipmentKind` |
| `SpellProjectile` | `WeaponProjectile` |
| `SpellImpactBurst`, `SpellImpactPool` | `EquipmentImpactBurst`, `EquipmentImpactPool` |
| `SpellDiscovery` | `EquipmentDiscovery` |
| Possess / `PossessProjectile` | Claim / `ClaimProjectile` |
| `SummonedGuardian` | `GuardianDrone` |

Provisional display names for the existing abilities: Plasma Bolt, Pulse Burst, Energy Shield, Dash, Guardian Drone and Claim. These are vocabulary choices, not new weapons or revised balance. The existing radar can become Tactical Radar instead of All-Seeing Eye.

Stored antimatter and rechargeable equipment energy have distinct member names. The name-only pass preserved inherited numeric formulas. See `BASE_DEVELOPMENT.md` for the subsequent explicit-tier, storage-backed-energy and configurable recharge implementation.

## Implemented Paths And Identifiers

- `scripts/player/CarpetFlightController.cs` → `scripts/player/ShipFlightController.cs`.
- `scripts/spells/` → `scripts/equipment/` and `MagicCarpetRemastered.Scripts.Spells` → `MagicCarpetRemastered.Scripts.Equipment`; the assembly/root namespace remains `MagicCarpetRemastered`.
- World types in the table remain in `scripts/world/`, with filenames matching their new types. `PossessProjectile` → `ClaimProjectile`; impact/definition types and `GuardianDrone` are in `scripts/equipment/`.
- All moved scripts retain their original `.cs.uid` bytes. The unrelated `scripts/world/ManaCollectorSpirit.cs.uid` has no matching script or active reference; it is retained unchanged as an orphan sidecar.
- Verification scene/script/UID triplets: `ManaLoopChecks` → `AntimatterLoopChecks`, `ManaPhysicsChecks` → `AntimatterPhysicsChecks`, `SpellProgressionChecks` → `EquipmentProgressionChecks`, `WizardFlightChecks` → `RivalFlightChecks`. `ProfiledManaPickup` → `ProfiledAntimatterPickup`.
- Runtime nodes include `PlayerShip`, `PlayerBase`, `EnemyBase`, `PlayerDrone`, `EnemyDrone`, `RivalPilot`, `AntimatterSource` and `WorldAntimatterTracker`. The rival enemy role is `RivalShip`; other roles retain behavior with `BaseAttacker` and `AntimatterThief` names.
- Groups: `player_castle` / `enemy_castle` → `player_base` / `enemy_base`; `loose_mana` → `loose_antimatter`; `mana_well` → `antimatter_source`; `world_mana_tracker` → `world_antimatter_tracker`; `balloon` → `drone`; `spell_discovery` → `equipment_discovery`.
- Equipment IDs migrate together across definitions, ranks, landmarks and checks: `firebolt` → `plasma-bolt`, `arcane-burst` → `pulse-burst`, `mana-shield` → `energy-shield`, `wind-dash` → `dash`, `guardian` → `guardian-drone`, `possess` → `claim`. `terrain-shape` remains unchanged. These IDs are match-local; there is no persisted equipment save migration.
- Input actions: `cast_primary` / `cast_area` / `cast_shield` / `cast_mobility` / `cast_summon` / `cast_possess` → `activate_primary` / `activate_area` / `activate_shield` / `activate_mobility` / `activate_guardian` / `activate_claim`; `cycle_quick_spell` → `cycle_quick_equipment`. Physical mouse/key bindings are preserved.
- Rechargeable player `Mana` / `ManaCapacity` / `SpendMana` become `Energy` / `EnergyCapacity` / `SpendEnergy`; equipment costs become `EnergyCost`. Physical resources use `AntimatterAmount`, `StoredAntimatter` and antimatter reserve/ownership/tracker APIs. The migration's `ClaimedAntimatter` represented inherited derived power, not a physical reserve. The subsequent economy foundation removes it, `BaseAntimatterBonus`, `InitialClaimedAntimatter` and `MaxClaimedAntimatter`; `EnergyCapacity` is now derived directly from home storage, and `RefreshBaseEnergyLimit` replaces `RefreshBaseAntimatterPower`. Base `Tier` replaces `GrowthStage`; configurable `Tier1StorageCapacity` through `Tier5StorageCapacity` replace writable single-capacity tuning.

## Profiling Compatibility

`CombatFrameProfile` and `TraversalFrameProfile` scene paths, output directories, tuning and CLI size/stress/mixed options remain stable. The new `--antimatter-isolation` option accepts `--mana-isolation` as a compatibility alias. Internal types and newly emitted reports use the new vocabulary:

- CSV fields `mana_bodies`, `mana_logic_ms`, `mana_integration_ms` become `antimatter_bodies`, `antimatter_logic_ms`, `antimatter_integration_ms`; field order and units stay the same.
- Combat phases `possess-first` / `possess-repeat` → `claim-first` / `claim-repeat`; `castle-spill` → `base-spill`; `dense-mana-*` / `mana-*` → `dense-antimatter-*` / `antimatter-*`.
- Mixed traversal `mixed-mana-create` → `mixed-antimatter-create`.
- Historical reports, measurements and research retain their original names. Analysis across the migration must map these labels; no historical CSV is rewritten. Current commands are in `README.md`.

## Reference Integrity

- Move each script with its existing `.cs.uid` sidecar; preserve UID contents.
- Keep Godot script filenames matched to their primary C# classes.
- Update scene `res://` paths and every direct script reference.
- Update dynamically created node names, group lookup strings and typed test lookups together.
- Update input actions in `project.godot`, gameplay handlers and synthesized test input together; retain physical bindings.
- Migrate equipment IDs and discovery/rank lookups consistently, or explicitly retain stable IDs with documentation. Do not partially rename serialized identifiers.
- Rename relevant check scripts/scenes/UIDs and update current reproduction commands. Performance CLI flags and report columns need an explicit compatibility decision if renamed.
- Preserve unfamiliar/orphan sidecars until their references have been checked; do not delete them merely because a similarly named script is absent.
- Keep the assembly/root namespace, repository folder and working title stable until a project-title change is approved.
- Preserve historical research, dated findings and baseline reports as history. Update current guidance without rewriting old measurements or pretending their old reproduction paths are current.

## Behavior To Preserve

All four planet sizes, flight/cameras, deformation/collision, ownership and physical resources, collection/deposit/overflow, enemy roles, all existing equipment, exploration ranks, repair, respawns, objectives, map/radar, pause ownership and restart must retain their current behavior.

Placeholder carpet/robe/base geometry can remain during the name-only milestone; replacing models is presentation work. Rename debug/UI labels so the working terminology is clear.

## Verification Gate

1. Build the C# solution and run Godot import after path changes.
2. Run the renamed resource-loop/physics, equipment-progression, objective/respawn, rival-flight and navigation checks.
3. Run integrated spherical gameplay and combat-performance regressions, preserving previous performance work.
4. Run rendered startup/UI smoke verification and the flat reference scene where relevant.
5. Check remaining old terminology in active scripts/scenes/UI and stale `res://` paths; distinguish deliberate historical/project identifiers from missed migrations.
6. Record final type/path mappings, verification and remaining gaps in the progress log before beginning mechanics changes.

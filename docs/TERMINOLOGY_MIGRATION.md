# Science-Fiction Terminology Migration

## Scope

Next implementation milestone after the approved base/equipment documentation. Preserve the accepted playable mechanics while updating gameplay filenames, C# types/namespaces/members, scene paths, node/group identifiers, input actions, player-facing text and verification tools together.

Do not mix the new construction economy, four-slot restriction or Shift booster into the rename. Those mechanics are specified in `BASE_DEVELOPMENT.md` and `SHIP_EQUIPMENT.md` and follow as separately verified milestones.

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

Stored antimatter and rechargeable equipment energy should have distinct member names in the eventual economy milestone. The rename must not silently remove the current starting-power allowance, alter damage-scaled capacity or change regeneration formulas before that mechanics work.

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

# Spell Discovery And Upgrade Pass

## Status Update (2026-10-11)

This document records the **superseded** legacy rank-II beacon pass. As of 2026-10-11, new matches start with Plasma Bolt only; owned equipment and ranks I–III come from base research and explicit home installation. Legacy beacons are non-reward signals awaiting wreck/boon conversion. `tools/EquipmentProgressionChecks.tscn` now verifies the research/inventory/loadout implementation. See `SHIP_EQUIPMENT.md` for current mechanics and `EXPLORATION_BOONS.md` for future exploration rewards. The historical filename/vocabulary and behavior below remain for context.

## Arena rules

- All five existing combat/utility spells start available at rank I. Exploration grants one rank-II upgrade per spell, avoiding removal of the existing prototype loadout.
- Five labeled gold rune beacons grant the upgrades. The flat reference arena retains outskirts placement; the globe uses Windward Ridge (Firebolt), Wellfield (Burst), Tidal Reach (Shield), distant Redglass Spire (Dash), and far-side Starfall Observatory (Guardian). Fly within four surface/horizontal units inside the twelve-unit-high terrain/water-relative collection column. Only a living player can collect; enemies, balloons, projectiles, and possession cannot.
- Firebolt II deals 35 damage (base 25). Arcane Burst II multiplies configured damage by 1.25 and radius by 1.25. Mana Shield II adds two seconds to configured duration. Wind Dash II multiplies configured dash speed by 1.2. Guardian II deals 16 damage and lasts 18 seconds (base 12 damage / 12 seconds).
- Mana costs and cooldowns stay at their current values. Upgrades are independent of castle-derived mana power and never add, consume, or transfer physical world mana.
- A discovery can be consumed once. Duplicate/max-rank discoveries remain unconsumed and cannot stack bonuses. Upgrades survive wizard respawn within the arena; R resets them with the arena. Campaign persistence and locked spell unlocks remain future work.
- RMB remains dedicated to Possess. Direct terrain shaping remains hidden.

## Hands-on validation

On the globe, use M to select a landmark waypoint and follow radar guidance; on the flat arena, fly toward outskirts labels. Confirm low/high flight collection, beacon disappearance, feedback and F3 progression. Select the upgraded spell with Tab/direct hotkey and compare effects. Die with your castle intact to check retention; R restores five beacons and rank-I spells. Tune landmark visibility and distant travel/reward pacing. See `docs/STRATEGIC_NAVIGATION.md`.

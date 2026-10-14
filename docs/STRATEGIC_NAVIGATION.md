# Strategic Planet Layout And Navigation

## Coordinated Globe Layout (2026-10-11)

Current layout uses **200/300/400/500 m**, Medium default, antipodal lowland starts and protected approaches. **2026-10-12 simplification removes matter fields/pads/markers**, retains eight stations (four antimatter extractors) and adds dense random wild habitats. Independent checks cover **160** retained openings across four sizes/four seeds and actual normal-hover resource round trips. Older 192-route/material notes are historical. Runtime rival navigation/recovery remains next; see `PLAYTEST_SIMPLIFICATION.md` and the progress log. AI wreck knowledge does not reveal human chart knowledge.

**Both maps share 220 m ship-only sensing.** Home/player-owned points stay global; discovered rival-base locations are remembered, with live details only in range. Other actors require sensing even when owned/charted. Cached drawing/hover/snap checks cannot identify hidden markers. Memory survives respawn and resets with the match. Neutral mobs/antimatter are black (light outline)/orange dots; claimed resources are cyan/red. Rival pilots retain a ship glyph. Radar is bottom-right, with home HP beneath it; full waypoint details remain in M survey.

- Session seeds derive independent terrain/layout/content streams. R retries; F2 blank creates a new world, entered seed replays. Up to sixteen candidates validate slopes/land and **ten** retained ordinary-hover openings before actors/meshes. Sixty-four seeded candidates score antipodal lowland starts with **120°** minimum separation. Failed generation exposes setup without unsafe actors. Same seed/size/version reproduces accepted geography/initial content.
- Factions start with **30 antimatter** beside dry radial home pads. Opponent distance is πR: **628/942/1257/1571 m**, about **26/39/52/65s** at 24 m/s before terrain/combat.
- Each home has four **12-A** globes around a field **55 m** away. A third field sits at Redglass Spire; six **10-A** globes seed Wellfield. Initial loose supply is **204 antimatter**. Matter fields are removed.
- Five stable named landmarks/habitats use a seeded start-relative frame: northern/southern frontier landmarks and three equatorial destinations at separate bearings. Their descriptions no longer promise obsolete equipment-rank rewards. Every landmark is more than 0.85×radius surface units from both bases; ordinary spawns also enforce every-base exclusions.
- Eight sites fan out with matched angular distances: two extractors per faction at **0.38 radians**, logistics **0.68**, research **0.92**. Travel scales with radius; equal angular opportunity alone does not prove practical balance.
- Twelve scenery clusters contain up to three pillars each; current geometry seeds yield **34–36**. Clearances are 28 m around bases, capture radius + 5 at stations, 12 at landmarks and 18 at antimatter fields. Wild encounter centres additionally target 60 m mutual habitat spacing.
- Random wreck selection now prioritizes the least occupied of twelve planetary regions, then cumulative nearest-base fairness. Minimum spacing, base exclusion, support, bounded attempts, arrivals and active cap remain. This improves globe coverage without making wrecks globally visible; local discovery still governs navigation.

Verification: `tools/PlanetLayoutChecks.tscn` covers all sizes/four seeds, deterministic generation, invalid settings, antipodal/minimum separation, ten-region authored coverage, scenery exclusions/orientation, equal opening distances, actual match reserves/spawns/supply and initial wreck coverage/spacing. Rendered runs save gameplay/survey images under the approved temporary directory. Existing navigation/capture/material/exploration/gameplay suites also pass. Remaining playtest questions: long-haul absence, combat travel and terrain accessibility. Ordinary mobs now use stable habitat spawning, local aggression and return-home behaviour; raiders are separately paced and navigation hover shows state/target details.

Terminology/UI update (2026-10-11): the implemented radar is now Tactical Radar. Current markers/UI use bases, pilots, antimatter, drones and equipment. The name-only conversion preserved the paused survey; the subsequent management milestone makes it real time, with ship-input/cursor ownership and B/M transfer between interfaces. Historical layout notes below retain the legacy vocabulary. See `TERMINOLOGY_MIGRATION.md` for IDs/types and `README.md` for current controls and commands.

## Approved Direction Update (2026-10-11)

The sections below describe the implemented legacy navigation/landmark milestone, not the new reward/capture systems. Basic spherical gameplay is now accepted as a smooth playtest baseline; detailed route and balance tuning continues.

- Exploration rewards now use random destructible wrecks and permanent stacking match boons. First collector secures three choices or a skip-for-antimatter option. H choice/summary is real time and deferrable. See `EXPLORATION_BOONS.md`.
- Random initial wrecks and ongoing orbital arrivals exclude all faction bases using spherical surface distance. Reward placement must remain reachable and bounded across all planet sizes.
- Capturable economic sites provide direct base resource income, faster research or an associated collector drone. Capture is timed; opposing player presence halts progress; benefits remain with the previous owner until completion. See `STRATEGIC_POINTS.md`.
- Logistics capture transfers the drone and its cargo immediately, including stolen resources, and reroutes it to the new owner's base.
- Wrecks/salvage and unowned economic sites require 220 m ship sensing on both maps; chart memory does not expose distant markers. Owned sites are globally visible ring/cross markers. Compact local text appears within capture radius + 12; B/F3 summarize owned sites/rates. Wreck labels remain shorter/visible within 55 units; actual collection is unchanged. See `STRATEGIC_POINTS.md` and `COMPACT_UI.md`.
- Antimatter dots are orange while neutral and cyan/red when claimed. All loose resources require local sensing; carried/consumed objects disappear immediately. Drone details report antimatter cargo objects.
- Equipment unlocks/ranks use base research and home installation. Random wrecks replace authored landmark beacons; boons cannot grant purchased ranks. The original layout/history below retains the earlier exploration context.

M survey, B management and H boon choices run in real time: they block ship controls, restore prior mouse mode on close and cannot open over setup/terminal pause. H/B/M transfer ownership with one expanded live interface at a time. The historical notes below describe the former reward layout; current playtests should find/destroy random wrecks instead of collecting named rank-II beacons.

## First playable milestone (2026-10-10)

User approved proceeding with planet layout and tactical navigation after the combat optimization pass. The spherical match now has five authored landmarks, distributed upgrades, regional encounter bias, a local radar and a world survey with waypoints. There is still one designated rival and the castle-plus-wizard elimination objective. Layout, exploration reward pacing and long-haul logistics await hands-on acceptance.

## Layout

| Site | Role | Upgrade | Placement |
| --- | --- | --- | --- |
| Windward Ridge | Swarm habitat; four 12-mana neutral globes | Firebolt II | Home-side approach; projected coordinates -140 / -100 |
| Wellfield | Contested central well; thieves; four 12-mana globes plus six 10-mana well globes | Arcane Burst II | Starting hemisphere center |
| Tidal Reach | Beast habitat; four 12-mana neutral globes | Mana Shield II | Branch from central/rival approach; projected coordinates 40 / 155 |
| Redglass Spire | Caster habitat; distant exploration | Wind Dash II | 0.9 radians from starting hemisphere center |
| Starfall Observatory | Far-side caster habitat and exploration | Guardian II | 2.25 radians from starting hemisphere center |

The initial loose field has **204 mana in 18 physical globes**, redistributed rather than filling the globe with active bodies. Castle and creature reserves are separate. Outer sites have no seeded loose field; their reward is the existing rank-II upgrade, and combat can introduce creature drops. All spells still start at rank I; costs, upgrade effects, respawn retention and restart reset remain as before.

First three sites have similar strategic travel distances across presets. Outer sites use angular placement so travel distance grows with planet size. At 24-unit/second cruise, the observatory is roughly a 40–110-second direct trip from the player start across the presets. Castles retain the previous approximately 280–290-unit duel route.

Each landmark uses three original primitive stone columns, a colored crystal and label. Columns are visual silhouettes, not new solid obstacles. The gold spell beacon is fifteen surface units beside the site. Landmark/beacon placement follows terrain/water with radial orientation and live deformation.

Within 120 surface units of a site, the ecosystem favors its role for 65% of non-initial selections and spawns in a local 24–40-unit ring. Existing role caps, ten-active-enemy limit, pressure ramp and one-rival rule remain. Initial waves retain mixed roles; away from sites, roaming-player pressure still applies. Persistent territorial populations/despawning are not implemented yet.

Spherical balloon search covers half the circumference, allowing possession anywhere to trigger a curved collection route. Speed, cargo capacity and docking remain. One balloon can be absent for minutes on a far-side route: tune this tradeoff before adding more haulers/outposts. Flat-arena hauling retains its old range.

## Controls and information

- Heading-relative **220 m local radar** sits below vitals. Forward is up; distant castles/landmarks/upgrades sit on the rim. Gold target line/ring and text give surface distance and left/right bearing. Default guidance is home.
- **M** opens/closes the world survey; **Esc** closes. The match stays live and cursor releases while open; ship input is blocked and closing restores prior mouse mode. It cannot open over a size menu or terminal result. **B** transfers to base management.
- **Left click** a marker or surface location for a static waypoint (twelve-pixel marker snap). **Right click / Delete** clears it, returning guidance to home. Waypoints do not automate flight or command balloons.
- **F4** restarts with the map open. R no longer restarts. Input is modal: close the map before F2/F3/F6. It cannot steal/unpause another menu's pause.
- Castles show known locations plus health/storage/ruins. Owned loose mana and allied balloon are globally tracked. Balloon cargo uses a count rather than duplicate loose dots.
- Neutral/rival loose mana, rival balloons and threats reveal within **120 surface units**. Pending/carried/consumed globes are excluded. Ownership colors update when claiming, and deleted actors disappear.
- Five sites begin as anonymous survey hints. Map name/habitat is retained after a living player approaches within **65 surface units**. Nearby upgrades reveal within 120 m; a visited site's uncollected upgrade remains charted afterward. Collected upgrades disappear; landmarks remain.
- Restart clears chart knowledge/waypoints/upgrades. Wizard respawn retains them.

## Geometry and performance

The full survey is a starting-hemisphere-centered equirectangular map of both hemispheres with horizontal wrapping. Inverse projection converts clicks to surface directions; heading differences wrap at the seam. The local radar projects angular surface distance into the player's tangent frame, working on the underside and through pole travel.

The 192×96 terrain survey texture is generated once during loading; it is an initial coarse survey, not a live crater display. Visibility/metadata snapshots refresh at five Hz. The local radar redraws every rendered frame and resolves live positions from those already-known actors, eliminating the previous ten-Hz heading steps and five-Hz moving-marker jumps without per-frame group scans. Invalid/deleted, dead, carried, pending and collected actors are rejected before drawing. New markers and ownership metadata can still take up to 0.2 seconds to appear/change.

Radar guidance text and survey hover/text refresh at ten Hz. Guidance derives distance/bearing directly from radial directions rather than querying terrain heights. Glyph buffers and the theme font are reused instead of allocated/resolved per marker per draw; timers preserve normal frame overshoot. Opening the map never regenerates terrain meshes or the noise atlas. Automated/rendered checks confirm correctness; frame-time gains and hands-on smoothness are not yet measured/accepted.

Well seeds/director spawns attach to their owning world parent instead of whichever scene is `CurrentScene` during construction. This prevents dynamic scene tests/restarts leaving actors outside their world with removed player/castle references.

## Verification and acceptance

```powershell
dotnet build MagicCarpetRemastered.sln
$godot = 'D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe'
& $godot --headless --path . 'res://tools/StrategicNavigationChecks.tscn'
& $godot --headless --path . 'res://tools/PlanetGameplayChecks.tscn'
& $godot --headless --path . 'res://tools/EquipmentProgressionChecks.tscn'
```

Navigation checks cover projection/seam/pole geometry, multi-hemisphere radar, all-radius layouts, actual grounded resource totals, ownership/local visibility, pending/carried exclusion, chart persistence, far-side upgrades, actual curved distant hauling, habitat spawn placement, waypoint snapping, viewport-routed M/Esc/F2 input, pause/mouse ownership, and restart. Optional rendered capture: append `-- --capture=<absolute-existing-directory>/survey.png` without `--headless`; it also saves `survey_radar.png`.

Hands-on next:

1. On Medium, select Windward Ridge with M, fly there and collect Firebolt II. Check bearing/distance, landmark naming and upgrade disappearance.
2. Claim fields and watch ownership/hauling. Compare home, Wellfield and Tidal routes; contest resources with thieves/rival.
3. Visit spire/observatory on Small and XL. Judge reward/travel pacing and balloon absence after claiming a remote kill drop.
4. Cross survey edges and globe poles in both cameras; compare radar bearing with actual flight.
5. Check map opening during combat, free-surface waypoints/clear, F2 size changes, R resets and complete-match respawn/victory behavior. Keep watching impact/kill/possession hitches during longer sessions.

After acceptance: tune routes/habitats/logistics and spell assignment/progression, then broaden realm/campaign structure. Participant expansion, networking, richer biomes and original art/audio remain later work/decisions.

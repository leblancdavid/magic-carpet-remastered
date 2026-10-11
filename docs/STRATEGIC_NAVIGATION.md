# Strategic Planet Layout And Navigation

Terminology/UI update (2026-10-11): the implemented radar is now Tactical Radar. Current markers/UI use bases, pilots, antimatter, drones and equipment. The name-only conversion preserved the paused survey; the subsequent management milestone makes it real time, with ship-input/cursor ownership and B/M transfer between interfaces. Historical layout notes below retain the legacy vocabulary. See `TERMINOLOGY_MIGRATION.md` for IDs/types and `README.md` for current controls and commands.

## Approved Direction Update (2026-10-11)

The sections below describe the implemented legacy navigation/landmark milestone, not the new reward/capture systems. Basic spherical gameplay is now accepted as a smooth playtest baseline; detailed route and balance tuning continues.

- Exploration rewards now use random destructible wrecks and permanent stacking match boons. First collector secures three choices or a skip-for-antimatter option. H choice/summary is real time and deferrable. See `EXPLORATION_BOONS.md`.
- Random initial wrecks and ongoing orbital arrivals exclude all faction bases using spherical surface distance. Reward placement must remain reachable and bounded across all planet sizes.
- Capturable economic sites provide direct base resource income, faster research or an associated collector drone. Capture is timed; opposing player presence halts progress; benefits remain with the previous owner until completion. See `STRATEGIC_POINTS.md`.
- Logistics capture transfers the drone and its cargo immediately, including stolen resources, and reroutes it to the new owner's base.
- Wrecks reveal within 120 surface units and remain charted; outlined gray-square wrecks turn into gold-diamond salvage markers on destruction and disappear on collection. Charted distant wreck/salvage markers also appear at the radar rim. Eight economic sites are globally charted ring/cross markers (gray neutral, cyan yours, red rival, orange contested), including two matter extractors; hover shows role, ownership, benefit and capture state. Local world text appears within 65 units. B/F3 summarizes owned sites and rates. See `STRATEGIC_POINTS.md` for current policies.
- Matter crates use outlined-square-with-center-dot markers, orange neutral/cyan owned/red rival. Owned material is charted globally; neutral/rival material reveals within 120 units. Attached/consumed crates disappear immediately; drone details report mixed cargo and material amount. Radar now leaves room below the four-line vitals/material HUD.
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
- **R** restarts with the map open. Input is modal: close the map before F2/F3/F6. It cannot steal/unpause another menu's pause.
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

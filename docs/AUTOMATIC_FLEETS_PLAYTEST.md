# Automatic Fleets And Readability Pass — 2026-10-14

This user-approved pass supersedes manual paid drone production/target dropdowns and Claim range research. The catalog now contains **67 types, 24 playable**; the existing 43 future types remain disabled.

## Automatic unit ownership

- Researching Scout/Attack/Defensive/Strike/Siphon bronze grants one permanent fleet slot. Each slot produces its first unit after its production time, then replaces destroyed units for **no AM cost**. Scout/Attack/Defensive/Siphon take **10s**, Strike **20s**. Clocks run independently of research/category slots and station project-speed bonuses.
- Tactical fleet units no longer expire. Strikes remain interceptable one-use siege units; impact or loss starts replacement. Guardian remains an activated, temporary ship ability.
- Silver/gold improve existing tactical units' damage/durability as well as future production. Strikes retain their launch stats. Tier loss pauses the affected production clocks and Siphon operation; existing strikes finish their sortie. Research/slot ownership survives rebuilding. Destroyed homes stop production and retire their units, spilling carried theft cargo.
- After gold, buy additional slots through ordinary category construction (including exact cancellation/siege accounting). Provisional tuning is **five units per type**, including the original; each extra slot takes **60s**, costing **1× / 2× / 3× / 4× the type's gold research price**. These are source tuning defaults in `AutomaticDroneFleet`, not final balance. A slot starts producing only after its purchase completes.
- B displays live fleet counts/replacement clocks and a post-gold **+1 drone** button. Manual drone payment/target controls are unavailable. The first unit's only AM cost is its research; replacements spend no stored AM and generate no salvage investment refunds.

## Assignments

- Assignment updates run once a second, validating match membership, liveness and ownership. Prefer different targets before sharing; distance and a small current-target bias select among equivalent assignments.
- Scouts seek non-owned points, including enemy-owned points. One assigned friendly scout per point; excess scouts return/wait, then reassign when eligible points appear. Existing half-rate/nonstacking capture and contest remain.
- Attack drones spread among non-owned points/opponent bases. Defenders cover home/owned points, with nearby hostile ships, neutrals and deployed units increasing priority among equally covered locations. Lost-site defenders return home or receive another valid owned assignment.
- Strikes and Siphons spread across opposing bases when several exist; a single opponent necessarily shares its base as a target. Automatic missions discover their objectives independently of the human's map knowledge; this does not reveal hidden live targets to the UI.

## Siphon Drone

- New **tier-II Offense** type; ordinary tier-II three-star research prices/times.
- **45/60/75 HP**, **12m/s** baseline travel with Command Relay speed bonuses.
- **10/20/30 A** cargo; withdraw **2/4/6 A each second** while within 8 surface metres of the opposing base. The withdrawal changes actual storage/energy limits and alerts the victim's retaliatory defense.
- Returns full or when the victim empties. Deposits only what home can accept and retains overflow. Destruction/home loss spills exactly the remaining cargo as neutral physical AM. Internal cargo is included in world faction accounting, without treating theft as new supply or construction spending.

## Claim Calibration

Claim remains **120m / 1.3m hit radius / 1 E**, at every star. The existing research ID `claim-range` is retained, but now improves only same-faction owned-ball magnetism:

| Property | Unresearched/neutral | Bronze | Silver | Gold |
| --- | --- | --- | --- | --- |
| Attraction gap | 6m | 8m | 10m | 12m |
| Acceleration multiplier | 1 | 1.25 | 1.5 | 1.75 |
| Contact dwell before merging | 1.25s | 1s | 0.8s | 0.6s |

Grounding, growth/drop grace, matching ownership, relative-speed limits, physical collision and exact amount conservation remain. Neutral balls gain no research bonus; reassigning ownership immediately uses the new owner's research. Attraction speed remains capped at 1.5m/s to preserve stable contact rather than causing high-speed collisions.

## Teleport Home

- New **tier-II Ship** research, ordinary tier-II prices/times. Built-in **T** action outside equipment slots; no home installation needed.
- **3s** channel; movement input/displacement, firing, jump/boost/other energy use, incoming accepted damage, input-owning UI, death or loss of home interrupts. Ordinary vertical hover settling is allowed. Pause freezes channel/cooldown.
- Costs **20 current E on successful arrival**. Cooldown **60/45/30s**. Failed/blocked/interrupted channels do not spend energy or start cooldown.
- Checks the actual hull against native physics at twelve home-adjacent 12m landing candidates. A blocked destination rejects; a valid arrival clears velocity/hover state and runs normal home loadout synchronization. No respawn path, invulnerability, healing, energy refill or unrelated cooldown reset. Teleport's cooldown persists through pilot respawn.
- HUD shows the key/readiness/cooldown or channel countdown once researched. Both factions can use the shared ability API; deliberate rival teleport tactics remain future AI work.

## Density And Maps

- Initial mob targets **72/135/225/338** at Small/Medium/Large/XL (about 25% fewer). Wild habitat budgets change; site guards/openings, rewards and 35s/8s recovery remain.
- AM and neutral enemies are one canvas-pixel square markers on both maps, drawn before important icons. Neutral enemies use light gray for contrast; AM retains ownership colors.
- Collectors, scouts, attackers, defenders, strikes, Guardians and Siphons have distinct small original procedural map glyphs and hover labels. Siphons show cargo in their hover details.
- Rival pilots need **220m sensing plus an unobstructed physical line of sight** on both radar and survey. Cached markers recheck sight and never retain last-known ship positions. Remembered base/point locations retain their separate existing policies.
- Offense tier II has four columns; Ship tier II five. Other rows retain three/four columns. Labels/controls fit the supported 1280×720 and 960×540 canvases.

## Verification / Remaining Acceptance

`AutomaticFleetChecks` exercises actual research/expansion/refunds, initial/loss clocks, distinct and exhausted scout targets, gold durability, both factions, theft transfer/delivery/death conservation, consumed strikes, tier-loss clocks/rebuilding, ownership-based magnetism, actual native convergence/merging, teleport costs/interruption/safe return, occluded/reappearing rivals and rendered management.

Human acceptance remains: automatic pressure/coverage, Siphon raid usefulness, post-gold prices/limit, free-strike pacing, teleport travel/combat feel, magnetism readability and Medium economic progression at lower hunting density. This pass is not an XL performance signoff. Follow the progress log for actual commands/results.

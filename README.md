# IFZ Compilation — Infection Free Zone mods

> **File version:** 1.6.0 · **Last edit:** 2026-10-06 08:14 UTC

A set of BepInEx mods for **Infection Free Zone** that are built to work together.
Each mod is a separate DLL, so you install only what you want.

**Platforms:** Windows · Linux (Steam Proton) · Steam Deck

---

## What this is

- Mods that **add features** to improve play.
- Mods that **add challenge** for players who want a harder game.
- Mods that **improve visuals** and how the game works.
- Every mod uses unique hotkeys and settings, so all mods run together without conflict.

## What this is not

- **No cheats.** No free resources, god mode, infinite ammo, or similar.
- Not a total conversion. The game stays Infection Free Zone.
- Not a replacement for game files. The mods load through BepInEx and leave the game install unchanged.

---

## Download

| What | Where |
|------|-------|
| Full pack (all mods, one zip) | [Releases](https://github.com/kurzonmorris/IFZ-Infection-free-zone-mods-compilation/releases) |
| One mod | [`plugins/`](plugins) — one DLL per mod |
| Install guide | [docs/INSTALL.md](docs/INSTALL.md) |
| Hotkeys | [docs/HOTKEYS.md](docs/HOTKEYS.md) |
| Change history | [FEATURES_AND_VERSIONS.md](FEATURES_AND_VERSIONS.md) |

**Requires:** BepInEx 5.4.23.2 (Windows x64 build, also on Linux).
Linux and Steam Deck also need the Steam launch option `WINEDLLOVERRIDES="winhttp=n,b" %command%`.
See the [install guide](docs/INSTALL.md).

**File names:** `IFZ-<modName>-v<version>.dll`. The version changes with every update.

---

## Mods

| Mod | Version | Type | Summary |
|-----|---------|------|---------|
| [Production Planner](#productionplanner--v060) | 0.6.0 | Function | Daily output goals, 9 work priorities, building and house on/off, named workers locked to jobs and houses, job experience, foremen, squad skill levels and Marksman. |

<!--
Copy this block for each mod. Keep the heading = the mod name.

### modName — vX.Y.Z

Short one-line description.

<details>
<summary><b>Feature name</b> — one-line summary</summary>

- Detail.
- Settings: `Section / Key` (default).
- Hotkey: `Key`.

</details>
-->

### productionPlanner — v0.6.0

Set a daily production goal on a building. The mod works out how many workers the building needs and tells you, or sets the max workers for you.
File: [`IFZ-productionPlanner-v0.6.0.dll`](plugins/IFZ-productionPlanner-v0.6.0.dll) · Hotkey: **F6** · Status: test build.

<details>
<summary><b>Planner window</b> — a floating box that follows the building you select</summary>

- Press **F6** to open or close it. Drag it by its title.
- It shows the building whose info panel is open. Select another building and the box changes.
- Works for every building with a production (cookhouse, workshops, factories and similar).

</details>

<details>
<summary><b>Priorities and on/off</b> — 9 priority levels and a turn-off button for every staffed building</summary>

- Priority buttons 1–9 in the planner window (the game's own panel shows only 1–5). Higher = filled first.
- **Turn off** removes all workers and clears the building's locks. **Turn on** restores the old priority.
- Works for every building with permanent workers: production, towers and defences, research, crematorium, child care and similar. Not houses.

</details>

<details>
<summary><b>Named workers and locks</b> — keep the same people on the same job</summary>

- **Workers…** opens a separate window. Move it by its title, resize it from the bottom-right corner.
- Left: all available workers (name, gender, age, experience in this job, current job). Sort by experience, name, gender or job; search by name.
- **→** assigns and locks a worker to this building. **←** unlocks them. **▲ / ▼** set the order.
- **Lock current workers** locks everyone working there now. **Unlock all** frees them.
- Unlocked workers come and go as normal. A locked worker works only this job. They leave the list only if the building is turned off, they die, they are drafted into a squad or the army, or max workers drops below their place on the list (the bottom of the list goes first).
- Sick workers keep their place and show **In hospital**. They return when they are well.

</details>

<details>
<summary><b>Job experience</b> — workers get better at the jobs they do</summary>

- Each building type is its own job (cookhouse, sawmill, forester's hut, farm, tower, …).
- One experience day = one full work shift actually worked, travel to work included.

  | Level | Speed boost | Days in the job |
  |-------|-------------|-----------------|
  | None | 0 % | 0 |
  | Novice | +10 % | 3 |
  | Moderate | +25 % | 8 |
  | Expert | +50 % | 15 |

- A worker keeps up to 3 jobs. One new job can be in training at a time; when it reaches Novice, the oldest job is forgotten. The **i** button in the worker window lists a worker's jobs, with **Forget** buttons.
- **Experience limit** per building (Any / Novice / Moderate / Expert): only qualified workers are taken on. Locked workers stay even if the limit rises. With a limit on, the mod fills the building itself; set its max workers with − / + in the planner window.
- **Auto-pick:** when a more experienced worker exists, the mod moves them into the job in place of a less experienced unlocked worker. Free workers and scavengers go first. Turn it off with `Experience / AutoPickExperienced`.
- The planner's worker calculation includes the experience of the current workers (tick box **Experience**).

</details>

<details>
<summary><b>Foremen</b> — an Expert can lead their building</summary>

- In the worker window, press **i** on a worker, then **Make foreman** next to a job they are Expert in.
- The worker loses that Expert level and starts learning the foreman role: Novice after 3 days, Moderate after 8, Expert after 15 (30 days in all from no experience).
- The foreman boosts **every worker in the building** they work in: +10 % / +25 % / +50 % by foreman level. The foreman gets no personal boost.
- One foreman counts per building (the most experienced one). Title example: **Expert Foreman of the Cookhouse**.
- **Stop foreman** removes the role (its progress is lost). Foremen are kept in place by auto-pick and count as Expert for experience limits.

</details>

<details>
<summary><b>Houses</b> — turn houses off and lock people into them</summary>

- Select a house and press **F6**: residents, capacity and locked count.
- **Turn off:** everyone moves out and nobody can move in. **Turn on** opens it again. The HQ cannot be turned off.
- **Residents…** opens a separate, resizable window: all citizens on the left (name, gender, age, worker / child / soldier, current house), locked residents on the right. Sort by name, gender or house; search by name.
- **→** moves a citizen in and locks them there (if the house is full, an unlocked resident moves out). **←** unlocks them. **Lock current residents** and **Unlock all** work on the whole house.
- A locked citizen keeps that house when they change job. Unlocked citizens behave as normal.

</details>

<details>
<summary><b>Squad skills and Marksman</b> — soldiers and guards get better with real use</summary>

- **Squad skills** (the game's own skills) get levels. A taught skill starts at **Novice**; **Moderate** after 10 shifts and **Expert** after 24 shifts active outside the walls (1 shift = 12 hours: moving, scavenging, on an expedition or facing an enemy; not standing still or inside a building).
- Normal skills (Strong, HawkEye, RaceDriver, EconomicDriver, Shoplifter, Inspector): the skill's effect +10 % / +25 % / +50 %.
- Combat skills (Slasher, SharpShooter) and **Marksman** use the combat table:

  | Level | Damage | Fire rate | Range |
  |-------|--------|-----------|-------|
  | Novice | +10 % | +5 % | +10 % |
  | Moderate | +20 % | +10 % | +20 % |
  | Expert | +40 % | +20 % | +30 % |

- **Marksman:** tower, gate and bunker guards earn it in real fights: Novice after 6 hours of shooting, Moderate after 15 more, Expert after 40 more (in-game hours). Shooting-range practice does not count.
- Select a squad and press **F6** to see each member's skills, levels and shifts. A guard's Marksman level shows under **i** in the worker window.
- Turn it all off with `Combat / Enabled`.

</details>

<details>
<summary><b>Daily goal</b> — type a number, use the buttons, or use the colony's food need</summary>

- Type a goal per day, or use −50 / −10 / +10 / +50.
- Ration buildings get a button: **Use colony food need**. It adds up what every worker, soldier and child eats per day, with the rations law.
- The goal is saved per building and per save game.

</details>

<details>
<summary><b>Advice or Auto</b> — choose per building</summary>

- **Advice:** shows the worker number and a button to set it once.
- **Auto:** sets the building's max workers every few seconds, so the goal holds when mood or weather change.

</details>

<details>
<summary><b>Factors</b> — tick boxes for what the calculation includes</summary>

- **Work hours:** the real working day (sunrise, sunset, work laws). Off = 24 hours.
- **Mood:** the efficiency of the building's current workers.
- **Weather:** the building's current temperature efficiency.
- **Hauling allowance:** time workers spend carrying goods (default 20 %, change with − / +).

</details>

<details>
<summary><b>Limits and inputs</b> — tells you when the building cannot reach the goal</summary>

- Warns when the building has too few worker slots and gives the volume (m³) it would need.
- Warns when the goal is above the building's daily production cap.
- Lists each input per day (for example meat or vegetables) with stock and days left.
- Shows what the building produced today, so you can compare it with the plan.

</details>

---

## Planned areas

<details>
<summary><b>Gameplay</b> — features that improve normal play</summary>

- Better control of squads, workers, and buildings.
- Fewer repeated manual tasks.

</details>

<details>
<summary><b>Challenge</b> — harder, optional game modes</summary>

- Stronger or more frequent threats through the game's own systems.
- Every challenge can be turned off in its settings.

</details>

<details>
<summary><b>Visuals</b> — a better-looking game</summary>

- Lighting, night, and effect improvements.
- Each visual setting has a performance note.

</details>

<details>
<summary><b>Function</b> — fixes and quality of life</summary>

- Fixes for game bugs and rough edges.
- Clearer information on screen.

</details>

---

## For mod developers

- Source for each mod: [`mods/`](mods). Start a new mod from [`mods/_template/`](mods/_template).
- Engine notes and project rules: [Explained-game_infectionFreeZone.md](Explained-game_infectionFreeZone.md).
- Third-party mods kept for study: [`research/`](research). Nothing there ships.

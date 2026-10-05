# productionPlanner — feature spec (workforce expansion)

> **File version:** 1.0.0 · **Last edit:** 2026-10-05 19:28 UTC
>
> Kurzon's requests of 2026-10-04 and 2026-10-05, with a feasibility check
> against the decompiled game (`Ifz.dll`, 2026-09-18). Status per feature:
> 🟢 clear and feasible · 🟡 feasible, open questions · 🔴 hard or needs research.
> Open questions are in §11. Build order is in §12.

---

## 1. Building controls  🟢

- **9 priority levels** instead of the game's 5.
  - Game fact: `WorksPriorityManager` holds 11 groups (0–10). The panel uses
    1–5. The alarm adds +5 to guard works (`WorkersCountChanger.AlarmPriorityOffset`).
  - Plan: patch the manager to hold 15 groups (0–14). Priorities 1–9 for
    buildings; alarm guards up to 14. The 9 buttons live in the mod window.
- **Turn off button** per building.
  - Game fact: priority 0 removes all workers (`ChangeWorkPriority`).
  - Plan: Off = priority 0, and the mod remembers the old priority for On.
  - Off also clears the building's lock list (§2).

## 2. Assign and lock workers  🟢

- Buildings stay open: the game moves unlocked workers as normal.
- A worker becomes **locked** to a building when the player assigns them in
  the window, or presses **Lock** (locks the current workers).
- A locked worker works only that job. They leave the list only when:
  the building is turned off · they die · max workers drops below their place
  on the list · they are drafted (army or squad).
- **Illness:** a sick worker keeps their place and shows "In hospital".
- Raising the building's experience limit does **not** remove locked workers.
- Game facts: workers move through `WorksPriorityManager.RealignWorkers`,
  `GetClosestWorker`, `PriorityWorkGroup.AlignWorkers / TryGetClosestWorker`,
  and `GetLeastNeededWorker` (squad draft). Patch these to skip locked
  workers. A free slot is refilled with `Work.AddWorker(character)`.
- Names: `CharacterInfo.Name`. Id: `Character.Id` (saved by the game).

## 3. Worker window  🟢

- Separate window: movable, **resizable**, opened from the planner window.
- Two lists: **Available** (left) and **Assigned** (right).
- Move a worker with an arrow button (→ / ←). Drag and drop is a later extra.
- Sort by: experience in this job (default, highest first), name, gender.
- Each row: name, gender, age, experience level in this job, current job,
  locked mark, "In hospital" mark.
- Buttons: Lock current workers · Unlock all · Forget job (per worker).

## 4. Job experience  🟡

- A **job** = one building type with permanent workers (cookhouse, sawmill,
  forester's hut, farm, tower, crematorium, child care, …). Not houses, not
  one-off works (excavation, building, scavenging).
- Levels and boost to the worker's own work speed:

  | Level | Boost | Time to reach |
  |-------|-------|---------------|
  | None | 0 % | — |
  | Novice | 10 % | 3 days |
  | Moderate | 25 % | +5 days (8 total) |
  | Expert | 50 % | +7 days (15 total) |

- A worker keeps up to **3 jobs**. Learning a 4th forgets the **oldest**,
  unless the player forgets one by choice first.
- **Experience limit** per building: only workers at or above the level may be
  newly assigned.
- **Auto-pick** when a slot opens (order):
  1. the most experienced worker in this job who is not locked elsewhere;
  2. if that worker is busy: free workers and scavengers first, then other
     unlocked workers with experience in this job.
- Boost method: scale the worker's work time in
  `WorkModule.ExecuteWork(timeSinceLastTick)` (same lever as MiKanSei39's
  work-speed mod). Applies to production, research and other `Work` types.
- Data saved by the mod per save game (the game has no work experience; its
  `KnowledgeActivity` covers only Shooting, Melee, Scavenging, Driving).

## 5. Foreman  🟡

- An **Expert** can be upgraded to **Foreman** of that job. The upgrade
  resets their job experience; they start learning the Foreman role.
- Foreman levels use the same times (3 / 5 / 7 days). Total from no experience
  to Expert Foreman: 15 + 15 = 30 days. (Kurzon wrote 29; see §11 Q4.)
- One foreman per building. The foreman boosts the **whole building**.
- Title example: "Expert Foreman of the Cookhouse".

## 6. Houses  🟢

- Turn a house off: nobody may move in. Residents move out.
- Lock citizens into a house. A locked citizen keeps the house.
- Unlocked citizens keep their current house unless the player assigns one.
- Game facts: `HouseRequest.AssignFoundHouse` picks the nearest house with
  space from the citizen's position; `Structure.HasSpaceForCitizen()`;
  `Character.FindBestHouse()`. Patch these.

## 7. Warehouse staff  🔴

- Unstaffed: normal. Staffed: max 1 worker per 1000 capacity. Each worker adds
  +500 capacity, ramping up over 7 days; it ramps down over 7 days without them.
- Capacity can be changed live: `StructureResourceContainer.SetCapacity`, and
  the global stock reacts (`StockroomsController.OnStockroomCapacityUpdate`).
- **Hard part:** warehouses have no workers module. A new job type must join
  the game's work system and survive save/load. Needs research before build.

## 8. Squad skills and Marksman  🟡

- Squad skills (`SkillId`: Slasher, Strong, HawkEye, SharpShooter, RaceDriver,
  EconomicDriver, Shoplifter, Inspector) get levels Novice / Moderate / Expert.
  A taught skill starts at Novice. Levels grow with days active outside the
  walls (patrol, scavenge, fight, expedition).
- **Marksman** (new): earned by tower, gate and bunker guards in real fights.
  Levels at 6, 15 and 40 hours of shooting.
- Boost method: `CharacterFightHandler.GetDamage` / reach patches.

## 9. House guards  🔴

- New job in houses: max 1 guard per 25 living spaces.
- A fully guarded house cuts the chance that a resident turns by 50 %.
- If a resident turns, the guards slowly kill the infected. A guard dies only
  if they are the one who turns. Guards shoot enemies near the house.
- Game facts: turning = `SicknessController.KillAndTurn` → critical sick
  citizens die, `GetChanceToTurn() × deaths` spawn a fresh horde
  (`inf_human_fresh`) in one building (`GetEnterableForHorde`).
- **Hard part:** units inside a house do not fight in the game. Shooting from
  houses and "slowly kill" need custom logic.

## 10. Night shift  🔴

- When there are more people than jobs, buildings get a second shift. Night
  workers sleep by day and work by night.
- Game fact: work time = `Work.IsWorkHour()` (sunrise/sunset + laws).
- Reference mod: Nexus IFZ mod 84. **Not in `research/` yet** and Nexus is
  blocked from the build session.

---

## 11. Open questions

1. **What is a "day" of experience?** A day with a full shift worked (my
   suggestion), or a calendar day while assigned?
2. **Does experience fade** when a worker does not use a job, or only through
   the 3-job limit?
3. **Defensive structures:** what does job experience boost there (damage,
   fire rate, reach)? Is it separate from Marksman?
4. **Foreman boost:** fixed +25 % to the building, or by foreman level
   (10 / 25 / 50 %)? Kurzon's "29 days" vs 30 by the table — which is right?
   Does the foreman also work like a normal worker?
5. **Experience limit with nobody qualified:** the building stays empty.
   Acceptable?
6. **Warehouse:** confirm +500 per worker on top of the base, and that this
   can be a later phase.
7. **Squad skill boosts:** what does each level add (for example the skill's
   effect × 1.0 / 1.25 / 1.5)? Days per level after Novice?
8. **Marksman:** boost per level? Are 6 / 15 / 40 hours totals or steps
   (6, +15, +40)? Game hours?
9. **House guards:** do guards also live in the house? Is the 50 % per resident
   of that house, or for the whole colony?
10. **Night shift:** upload the Nexus mod 84 DLL to `research/`.

## 12. Build order (proposal)

| Phase | Content | Status |
|-------|---------|--------|
| 1 | §1 priorities and Off · §2 lock · §3 worker window | ready to build |
| 2 | §4 job experience | after Q1, Q2, Q5 |
| 3 | §5 foreman | after Q4 |
| 4 | §6 houses | ready after phase 1 |
| 5 | §8 squad skills and Marksman | after Q3, Q7, Q8 |
| 6 | §10 night shift | after the reference mod |
| 7 | §7 warehouse staff | needs research |
| 8 | §9 house guards | needs research |

Each phase ships as a new productionPlanner version for in-game testing.

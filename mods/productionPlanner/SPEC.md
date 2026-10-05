# productionPlanner — feature spec (workforce expansion)

> **File version:** 1.5.0 · **Last edit:** 2026-10-05 23:43 UTC
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
- Built rule (0.3.0): only one new job is in training at a time; a learned
  job is forgotten only when the new one reaches Novice. With a limit on, the
  mod sets the game's max workers itself (silent write to `_maxWorkers`) so
  the game never adds unqualified workers.
- Data saved by the mod per save game (the game has no work experience; its
  `KnowledgeActivity` covers only Shooting, Melee, Scavenging, Driving).

## 5. Foreman  🟡

- An **Expert** can be upgraded to **Foreman** of that job. The upgrade
  resets their job experience; they start learning the Foreman role.
- Foreman levels use the same times (3 / 5 / 7 days). Total from no experience
  to Expert Foreman: 15 + 15 = 30 days. (Kurzon wrote 29; see §11 Q4.)
- One foreman per building. The foreman boosts the **whole building**.
- Title example: "Expert Foreman of the Cookhouse".
- Built (0.4.0): the role is a job entry `foreman:<job>`; it replaces the Expert
  job entry. Building boost and personal boost multiply. The highest foreman
  in a building counts; others give nothing. Foremen count as Expert for
  limits and auto-pick.

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

## 10. Night shift  🟡

- When there are more people than jobs, buildings get a second shift. Night
  workers sleep by day and work by night.
- Game facts:
  - A worker works only while `WorkModule.IsWorking()` is true. It returns
    `CurrentWork.IsWorkHour()` (sunrise/sunset + laws).
    `WorkerBehaviour.IsWorkHour()` gates the forum (law voting).
  - Tired workers (`Character.IsTired`, fatigue > 0) go home to rest. Guards
    keep working while tired.
  - `WorkerBehaviour.ReturnResourceToStockroom(…, isNight, …)` stops carrying
    goods at night.
  - `DefenseWork` has its own `IsWorkHour`.
- Reference: `research/IFZ24HourWorkers.dll` (MHMejren, "IFZ 24/7 Workers"
  1.0.0, F12 toggle). It only forces `Work.IsWorkHour` and
  `WorkerBehaviour.IsWorkHour` to true and clears `isNight` for carrying.
  Everyone works all the time; there are no shifts and no sleep schedule.
  ⚠️ Its F12 key is also Steam's screenshot key.
- Plan (real shifts, per worker):
  - A building with night shift on gets a second crew. The building's
    `IsWorkHour` returns true day and night.
  - Each worker has a shift (Day or Night) stored by the mod. Patch
    `WorkModule.IsWorking` per worker: Day crew → the normal hours; Night crew →
    the opposite hours. Off-shift workers go home and sleep.
  - Night crew may carry goods at night (`isNight` → false for them).
  - Night crew slots: extra slots on top of the day crew (see §11 Q10).
  - The worker window gets a Day / Night column. Locks (§2) work per shift.

---

## 11. Decisions (Kurzon, 2026-10-05) and open questions

**Decided:**

1. **Experience day** = one full work shift actually worked, travel from home
   to the workplace included.
2. **Experience fades** only through the 3-job limit.
3. **Defensive structures and squad combat** use three separate skills that
   level up together:

   | Level | Marksman accuracy (damage) | Marksman fire rate | Marksman range |
   |-------|----------------------------|--------------------|----------------|
   | Novice | +10 % | +5 % | +10 % |
   | Moderate | +20 % | +10 % | +20 % |
   | Expert | +40 % | +20 % | +30 % |

   (Replaces the single "Marksman" skill of §8.)
4. **Foreman** boost = by foreman level, for the whole building (10 / 25 /
   50 %). None → Expert Foreman = **30 days**. The foreman works as a normal
   worker with no personal boost.
5. **Experience limit with nobody qualified:** the building stays empty. OK.
6. **Warehouse:** +500 per worker above the normal capacity. Later phase.
7. **Squad skills:** time counts in **shifts**; day and night are separate
   shifts, so 24 hours = 2 shifts. Normal squad skills: 10 / 25 / 50 %.
   Combat skills: the same as guards (table in item 3).
8. **Marksman** = the three skills in item 3.
9. **House guards** live in the house they guard. The 50 % applies only to the
   residents of that house.

**Still open:**

- Q7a. Squad skills: how many shifts per level? (Novice is reached when the
  skill is taught.) For example 6 / 10 / 14 shifts (= 3 / 5 / 7 days)?
- Q8a. Marksman: 6 / 15 / 40 hours of shooting — totals or steps
  (6, then +15, then +40)? In-game hours?
10. **Night crew size:** the same as the day max workers (doubles output),
    or its own number?
11. **When night shift starts:** a manual switch per building, automatic when
    there are free workers, or both?
12. **Outdoor night work:** allow night shift on outdoor jobs (farms,
    foresters), or only indoor buildings?

## 12. Build order (proposal)

| Phase | Content | Status |
|-------|---------|--------|
| 1 | §1 priorities and Off · §2 lock · §3 worker window | built in 0.2.0, testing |
| 2 | §4 job experience | built in 0.3.0, testing |
| 3 | §5 foreman | built in 0.4.0, testing |
| 4 | §6 houses | ready |
| 5 | §8 squad skills and Marksman | after Q7a, Q8a |
| 6 | §10 night shift | after Q10–Q12 |
| 7 | §9 house guards | needs research |
| 8 | §7 warehouse staff | needs research |

Each phase ships as a new productionPlanner version for in-game testing.

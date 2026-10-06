# productionPlanner

> **File version:** 1.6.0 · **Last edit:** 2026-10-06 11:30 UTC

Mod version **0.7.0** · DLL `IFZ-productionPlanner-v0.7.0.dll` · GUID `kurzon.ifz.productionPlanner` · Hotkey **F6**

Source files:
- `Plugin.cs` — config, F6 window (IMGUI), Auto loop.
- `Planner.cs` — game lookups and the worker calculation.
- `GoalStore.cs` — saves goals to `BepInEx/config/kurzon.ifz.productionPlanner.goals.txt`.
- `Crews.cs` — priorities, on/off, lock lists, the 2-second keep-locked loop. Saves to `kurzon.ifz.productionPlanner.crews.txt`.
- `Patches.cs` — Harmony: 15 priority groups; the game skips locked workers when it moves people.
- `WorkerWindow.cs` — the resizable worker window.
- `Experience.cs` — job experience per worker; accrual every 2 s from game time; saves to `kurzon.ifz.productionPlanner.experience.txt`.
- `Houses.cs` — house on/off and locked residents; saves to `kurzon.ifz.productionPlanner.houses.txt`.
- `HouseWindow.cs` — the resizable residents window.
- `Combat.cs` — squad skill levels, Marksman hours, combat bonuses (`CombatRules` = testable thresholds); saves to `kurzon.ifz.productionPlanner.combat.txt`.
- `Guards.cs` — house guards: turn weighting, clearing infected inside, shooting nearby infected; saves to `kurzon.ifz.productionPlanner.guards.txt`.
- `UiStyle.cs` — solid window background and the opacity slider.
- `ExperienceRules.cs` — levels and the 3-job / forget-oldest rule (no game types, so it can be tested outside the game).
- `SPEC.md` — the full workforce plan, decisions and build order.

## Calculation

```
per worker per day = workHours × HourLengthInGts × mood × weather × (1 − hauling%)
                     ÷ cycleTimeGts × outputPerCycle
workers needed     = ceil(goal ÷ per worker per day)
```

Limits: `InitialMaxWorkers` (slots from building volume) and `MaxDayProduction` (daily cap from volume).
Game facts behind this: `Explained-game_infectionFreeZone.md` §9.5a–§9.5c.

## Not handled in 0.7.0

- Buildings without a `ProductionWork` (for example farms, sawmill, forester, gather works) show "not supported".
- Rest and fatigue are not modelled separately. The hauling allowance covers them.
- Typing in the goal box can also trigger game hotkeys (for example 1–9). Use the step buttons if that happens.
- When an unlocked citizen changes job, the game picks them a new house near where they stand (vanilla). Lock them into a house to stop this.
- Priorities 6–9 do not light up a button in the game's own panel. Use the planner window.
- If the mod is removed, buildings at priority 6–9 keep that number; guards at 6–9 during an alarm can then drop out of the game's lists until the priority is set again.
- One new job can be in training at a time. Starting a different new job resets that training.
- With an experience limit on, the native panel's max workers shows the mod's working number; the wanted max is in the planner window.
- The foreman role takes the slot of the job it replaced. If the foreman later learns 3 other jobs, the foreman role can be forgotten as the oldest.
- The foreman boost and the personal boost multiply: (1 + personal) × (1 + foreman).
- Squad activity is sampled every 2 seconds; a short trip shorter than that may not count.
- HighGround (JaySNL) also patches `GetWeaponAttackReach`; both multiply, so range bonuses stack if both are installed.
- House guards keep their day job; they guard only while inside the house.
- Guard shots have no visible tracer and target infected only, not raiders.
- Auto-pick may fight the game's own balancing and move people more often than vanilla. Watch for this in testing.

## Tests

Tested outside the game: 37 checks. `ExperienceRules` (25: levels, 3-job limit, training slot, forget oldest, cap, foreman rules) and `CombatRules` (12: Marksman at 6 / 21 / 61 h, squad skills at 10 / 24 shifts). All passed on 2026-10-06.

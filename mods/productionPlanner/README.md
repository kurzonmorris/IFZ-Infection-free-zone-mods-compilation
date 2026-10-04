# productionPlanner

> **File version:** 1.0.0 · **Last edit:** 2026-10-04 10:26 UTC

Mod version **0.1.0** · DLL `IFZ-productionPlanner-v0.1.0.dll` · GUID `kurzon.ifz.productionPlanner` · Hotkey **F6**

Source files:
- `Plugin.cs` — config, F6 window (IMGUI), Auto loop.
- `Planner.cs` — game lookups and the worker calculation.
- `GoalStore.cs` — saves goals to `BepInEx/config/kurzon.ifz.productionPlanner.goals.txt`.

## Calculation

```
per worker per day = workHours × HourLengthInGts × mood × weather × (1 − hauling%)
                     ÷ cycleTimeGts × outputPerCycle
workers needed     = ceil(goal ÷ per worker per day)
```

Limits: `InitialMaxWorkers` (slots from building volume) and `MaxDayProduction` (daily cap from volume).
Game facts behind this: `Explained-game_infectionFreeZone.md` §9.5a–§9.5c.

## Not handled in 0.1.0

- Buildings without a `ProductionWork` (for example farms, sawmill, forester, gather works) show "not supported".
- Rest and fatigue are not modelled separately. The hauling allowance covers them.
- Typing in the goal box can also trigger game hotkeys (for example 1–9). Use the step buttons if that happens.

# productionPlanner

> **File version:** 1.1.0 · **Last edit:** 2026-10-05 19:48 UTC

Mod version **0.2.0** · DLL `IFZ-productionPlanner-v0.2.0.dll` · GUID `kurzon.ifz.productionPlanner` · Hotkey **F6**

Source files:
- `Plugin.cs` — config, F6 window (IMGUI), Auto loop.
- `Planner.cs` — game lookups and the worker calculation.
- `GoalStore.cs` — saves goals to `BepInEx/config/kurzon.ifz.productionPlanner.goals.txt`.
- `Crews.cs` — priorities, on/off, lock lists, the 2-second keep-locked loop. Saves to `kurzon.ifz.productionPlanner.crews.txt`.
- `Patches.cs` — Harmony: 15 priority groups; the game skips locked workers when it moves people.
- `WorkerWindow.cs` — the resizable worker window.
- `SPEC.md` — the full workforce plan, decisions and build order.

## Calculation

```
per worker per day = workHours × HourLengthInGts × mood × weather × (1 − hauling%)
                     ÷ cycleTimeGts × outputPerCycle
workers needed     = ceil(goal ÷ per worker per day)
```

Limits: `InitialMaxWorkers` (slots from building volume) and `MaxDayProduction` (daily cap from volume).
Game facts behind this: `Explained-game_infectionFreeZone.md` §9.5a–§9.5c.

## Not handled in 0.2.0

- Buildings without a `ProductionWork` (for example farms, sawmill, forester, gather works) show "not supported".
- Rest and fatigue are not modelled separately. The hauling allowance covers them.
- Typing in the goal box can also trigger game hotkeys (for example 1–9). Use the step buttons if that happens.
- When a worker changes job, the game picks them a new house near where they stand (vanilla). House control comes in phase 4.
- Priorities 6–9 do not light up a button in the game's own panel. Use the planner window.
- If the mod is removed, buildings at priority 6–9 keep that number; guards at 6–9 during an alarm can then drop out of the game's lists until the priority is set again.

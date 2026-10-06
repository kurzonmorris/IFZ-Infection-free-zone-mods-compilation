# Features and versions

> **File version:** 1.11.0 · **Last edit:** 2026-10-06 11:35 UTC

Every edit, feature, and change to any file in this repository. Newest first.
Rules: see `Explained-game_infectionFreeZone.md` §1.2 and §1.3.

## Current versions

| File | Version |
|------|---------|
| README.md | 1.8.0 |
| FEATURES_AND_VERSIONS.md | 1.11.0 |
| Explained-game_infectionFreeZone.md | 1.9.0 |
| Explained-game_engineMap.md | 1.0.0 |
| docs/INSTALL.md | 1.0.0 |
| docs/HOTKEYS.md | 1.1.0 |
| mods/_template (Plugin.cs, template.csproj, README.md) | 1.0.0 (template mod 0.1.0) |
| mods/Directory.Build.props | 1.0.0 |
| plugins/README.md | 1.8.0 |
| research/README.md | 1.2.0 |
| .github/workflows/release.yml | 1.0.0 |
| mods/productionPlanner (Plugin.cs, Planner.cs, GoalStore.cs, Crews.cs, Patches.cs, WorkerWindow.cs, Experience.cs, ExperienceRules.cs, Houses.cs, HouseWindow.cs, Combat.cs, Guards.cs, UiStyle.cs, productionPlanner.csproj) | 0.7.1 |
| mods/productionPlanner/README.md | 1.7.0 |
| mods/productionPlanner/SPEC.md | 1.8.0 |
| plugins/IFZ-productionPlanner-v0.7.1.dll | 0.7.1 |
| mods/modMenu (Plugin.cs, modMenu.csproj) | 0.1.0 |
| mods/modMenu/README.md | 1.0.0 |
| plugins/IFZ-modMenu-v0.1.0.dll | 0.1.0 |
| Pack (Releases zip) | not released |

## Change log

| Date and time (UTC) | File | Version | Change |
|---------------------|------|---------|--------|
| 2026-10-06 11:35 | mods/modMenu, plugins/IFZ-modMenu-v0.1.0.dll | 0.1.0 | New mod. Mods button bottom-right; lists window keys found in every BepInEx mod's settings; opens them by direct ToggleWindow() call or a one-frame faked key; ExtraEntries for hand-added keys. |
| 2026-10-06 11:35 | mods/modMenu/README.md | 1.0.0 | New. |
| 2026-10-06 11:35 | mods/productionPlanner, plugins/IFZ-productionPlanner-v0.7.1.dll | 0.7.1 | Public static ToggleWindow() for the Mod Menu. Replaces v0.7.0 DLL. |
| 2026-10-06 11:35 | mods/productionPlanner/README.md | 1.7.0 | ToggleWindow note. |
| 2026-10-06 11:35 | Explained-game_infectionFreeZone.md | 1.9.0 | Coexistence rule: ToggleWindow hook and key naming; Input patch owner. |
| 2026-10-06 11:35 | README.md | 1.8.0 | Mod Menu section. |
| 2026-10-06 11:35 | plugins/README.md | 1.8.0 | Listed both DLLs. |
| 2026-10-06 11:35 | FEATURES_AND_VERSIONS.md | 1.11.0 | Recorded the changes above. |
| 2026-10-06 11:30 | mods/productionPlanner, plugins/IFZ-productionPlanner-v0.7.0.dll | 0.7.0 | Window opacity slider in every title bar (solid background, 30–100 %). Phase 7: house guards (1 per 25 spaces) — fewer residents turn in fully guarded houses, guards clear turned infected inside and shoot infected nearby with ammo. Replaces v0.6.0 DLL. |
| 2026-10-06 11:30 | mods/productionPlanner/SPEC.md | 1.8.0 | Phase 7 built design. |
| 2026-10-06 11:30 | mods/productionPlanner/README.md | 1.6.0 | Guards and UI files; limits. |
| 2026-10-06 11:30 | Explained-game_infectionFreeZone.md | 1.8.0 | Turning details, damage API; new patch owners. |
| 2026-10-06 11:30 | README.md | 1.7.0 | Opacity slider and house guards. |
| 2026-10-06 11:30 | plugins/README.md | 1.7.0 | DLL renamed to v0.7.0. |
| 2026-10-06 11:30 | FEATURES_AND_VERSIONS.md | 1.10.0 | Recorded the changes above. |
| 2026-10-06 08:14 | mods/productionPlanner, plugins/IFZ-productionPlanner-v0.6.0.dll | 0.6.0 | Phase 5: squad skill levels (Novice/Moderate/Expert by shifts active outside the walls), skill effect +10/25/50 %, Marksman for guards (6/21/61 h of real shooting), combat table for damage/fire rate/range; squad view in F6. Replaces v0.5.0 DLL. |
| 2026-10-06 08:14 | mods/productionPlanner/SPEC.md | 1.7.0 | Recorded answers for phases 5 and 6; built interpretation of phase 5. |
| 2026-10-06 08:14 | mods/productionPlanner/README.md | 1.5.0 | Combat file, limits, 37 tests. |
| 2026-10-06 08:14 | Explained-game_infectionFreeZone.md | 1.7.0 | Skill and fire-rate facts; new patch owners. |
| 2026-10-06 08:14 | README.md | 1.6.0 | Squad skills and Marksman features. |
| 2026-10-06 08:14 | plugins/README.md | 1.6.0 | DLL renamed to v0.6.0. |
| 2026-10-06 08:14 | FEATURES_AND_VERSIONS.md | 1.9.0 | Recorded the changes above. |
| 2026-10-06 00:05 | mods/productionPlanner, plugins/IFZ-productionPlanner-v0.5.0.dll | 0.5.0 | Phase 4: houses. Turn a house off (all move out, nobody moves in); residents window to lock citizens into a house; locked citizens keep their house. Replaces v0.4.0 DLL. |
| 2026-10-06 00:05 | mods/productionPlanner/SPEC.md | 1.6.0 | Phase 4 marked built. |
| 2026-10-06 00:05 | mods/productionPlanner/README.md | 1.4.0 | House files and limits. |
| 2026-10-06 00:05 | Explained-game_infectionFreeZone.md | 1.6.0 | House facts; new patch owners. |
| 2026-10-06 00:05 | README.md | 1.5.0 | House features. |
| 2026-10-06 00:05 | plugins/README.md | 1.5.0 | DLL renamed to v0.5.0. |
| 2026-10-06 00:05 | FEATURES_AND_VERSIONS.md | 1.8.0 | Recorded the changes above. |
| 2026-10-05 23:43 | mods/productionPlanner, plugins/IFZ-productionPlanner-v0.4.0.dll | 0.4.0 | Phase 3: foreman role. Expert can be made foreman (loses the Expert level, learns the role in 3/8/15 shifts); building-wide boost +10/25/50 %; title shown; Stop foreman. Replaces v0.3.0 DLL. |
| 2026-10-05 23:43 | mods/productionPlanner/SPEC.md | 1.5.0 | Phase 3 marked built; build notes. |
| 2026-10-05 23:43 | mods/productionPlanner/README.md | 1.3.0 | Foreman limits; 25 tests. |
| 2026-10-05 23:43 | Explained-game_infectionFreeZone.md | 1.5.0 | Patch owner note for foreman boost. |
| 2026-10-05 23:43 | README.md | 1.4.0 | Foreman features. |
| 2026-10-05 23:43 | plugins/README.md | 1.4.0 | DLL renamed to v0.4.0. |
| 2026-10-05 23:43 | FEATURES_AND_VERSIONS.md | 1.7.0 | Recorded the changes above. |
| 2026-10-05 21:57 | mods/productionPlanner, plugins/IFZ-productionPlanner-v0.3.0.dll | 0.3.0 | Phase 2: job experience per building type (Novice/Moderate/Expert = +10/25/50 % after 3/8/15 shifts), 3-job limit with forget, experience limit per building, auto-pick of experienced workers, experience in worker window and planner. Replaces v0.2.0 DLL. |
| 2026-10-05 21:57 | mods/productionPlanner/SPEC.md | 1.4.0 | Phase 2 marked built; build notes. |
| 2026-10-05 21:57 | mods/productionPlanner/README.md | 1.2.0 | New files, limits, test record. |
| 2026-10-05 21:57 | Explained-game_infectionFreeZone.md | 1.4.0 | FindWork/AddWorker trap; new patch owners. |
| 2026-10-05 21:57 | README.md | 1.3.0 | Job experience features. |
| 2026-10-05 21:57 | plugins/README.md | 1.3.0 | DLL renamed to v0.3.0. |
| 2026-10-05 21:57 | FEATURES_AND_VERSIONS.md | 1.6.0 | Recorded the changes above. |
| 2026-10-05 19:48 | mods/productionPlanner, plugins/IFZ-productionPlanner-v0.2.0.dll | 0.2.0 | Phase 1: priorities 1–9, building on/off, worker window (resizable, sort, search, arrows), lock lists that keep named workers on a job; sick keep their place. Replaces v0.1.0 DLL. |
| 2026-10-05 19:48 | mods/productionPlanner/SPEC.md | 1.2.0 | Recorded Kurzon's answers (experience day, foreman, Marksman skills, squad shifts, guards); new build order. |
| 2026-10-05 19:48 | mods/productionPlanner/SPEC.md | 1.3.0 | Phase 1 marked built. |
| 2026-10-05 19:48 | mods/productionPlanner/README.md | 1.1.0 | New source files and 0.2.0 limits. |
| 2026-10-05 19:48 | Explained-game_infectionFreeZone.md | 1.3.0 | §9.5d worker assignment facts; patch owners. |
| 2026-10-05 19:48 | README.md | 1.2.0 | productionPlanner 0.2.0 features. |
| 2026-10-05 19:48 | plugins/README.md | 1.2.0 | DLL renamed to v0.2.0. |
| 2026-10-05 19:48 | FEATURES_AND_VERSIONS.md | 1.5.0 | Recorded the changes above. |
| 2026-10-05 19:31 | mods/productionPlanner/SPEC.md | 1.1.0 | Night shift: studied IFZ24HourWorkers, per-worker shift design, questions Q10–Q12. |
| 2026-10-05 19:31 | research/README.md | 1.2.0 | Listed IFZ24HourWorkers.dll. |
| 2026-10-05 19:31 | FEATURES_AND_VERSIONS.md | 1.4.0 | Recorded the changes above. |
| 2026-10-05 19:28 | mods/productionPlanner/SPEC.md | 1.0.0 | New. Spec for the workforce expansion: 9 priorities, building off, worker locking and window, job experience, foreman, houses, warehouse staff, squad skills, Marksman, house guards, night shift. Feasibility, open questions, build order. |
| 2026-10-05 19:28 | FEATURES_AND_VERSIONS.md | 1.3.0 | Recorded the spec. |
| 2026-10-04 22:32 | Explained-game_engineMap.md | 1.0.0 | New. Look-up lists from the decompiled game: assemblies, Rewired input actions, enums, difficulty settings, configs, signals, controllers, 178 console commands, namespaces. |
| 2026-10-04 22:32 | Explained-game_infectionFreeZone.md | 1.2.0 | Linked the engine map; recorded removal of Managed.zip and that builds still need the game DLLs; updated open questions. |
| 2026-10-04 22:32 | FEATURES_AND_VERSIONS.md | 1.2.0 | Recorded the changes above. |
| 2026-10-04 10:26 | mods/productionPlanner, plugins/IFZ-productionPlanner-v0.1.0.dll | 0.1.0 | New mod. F6 planner window for the selected building: daily goal, workers needed, Advice/Auto, factor tick boxes, slot and daily-cap warnings, inputs, produced today. |
| 2026-10-04 10:26 | mods/productionPlanner/README.md | 1.0.0 | New. Mod notes and formula. |
| 2026-10-04 10:26 | Explained-game_infectionFreeZone.md | 1.1.0 | Added production, food, selection facts (§9.5a–c), cloud build steps (§5), research notes (§15), F6 in hotkey registry. |
| 2026-10-04 10:26 | README.md | 1.1.0 | Added productionPlanner to the mod list with feature drop-downs. |
| 2026-10-04 10:26 | docs/HOTKEYS.md | 1.1.0 | Added F6 productionPlanner. |
| 2026-10-04 10:26 | plugins/README.md | 1.1.0 | Listed the productionPlanner DLL. |
| 2026-10-04 10:26 | research/README.md | 1.1.0 | Listed the uploaded DLLs and Managed.zip. |
| 2026-10-04 10:26 | FEATURES_AND_VERSIONS.md | 1.1.0 | Recorded the changes above. |
| 2026-10-04 01:00 | Explained-game_infectionFreeZone.md | 1.0.0 | New. Game, BepInEx, Proton, Harmony and engine reference from 5 researched repos; project rules; hotkey registry. |
| 2026-10-04 01:00 | README.md | 1.0.0 | Rewrote front page: purpose, does / does not, downloads, mod list layout with drop-downs. |
| 2026-10-04 01:00 | FEATURES_AND_VERSIONS.md | 1.0.0 | New. Change record for all files. |
| 2026-10-04 01:00 | docs/INSTALL.md | 1.0.0 | New. Install guide for Windows, Linux, Steam Deck. |
| 2026-10-04 01:00 | docs/HOTKEYS.md | 1.0.0 | New. Public hotkey list. |
| 2026-10-04 01:00 | mods/Directory.Build.props | 1.0.0 | New. Shared build settings and game path. |
| 2026-10-04 01:00 | mods/_template | 1.0.0 | New. Starter mod: csproj (versioned DLL name, auto deploy), Plugin.cs, README. |
| 2026-10-04 01:00 | plugins/README.md | 1.0.0 | New. Folder for built DLLs. |
| 2026-10-04 01:00 | research/README.md | 1.0.0 | New. Folder for third-party mods to study. |
| 2026-10-04 01:00 | .github/workflows/release.yml | 1.0.0 | New. Tag `pack-v*` builds the release zip. |
| 2026-10-04 01:00 | .gitignore | 1.0.0 | New. Ignore build output and decompiled code. |

# Infection Free Zone — Modding Reference

> **File version:** 1.6.0 · **Last edit:** 2026-10-06 00:05 UTC
>
> **Purpose:** Read this file before you work on any mod in this repository. It
> holds every fact we found about the game, the tools, and our own project
> rules. Read `Explained-user_kurzon.md` first for how Kurzon works.
>
> **How to use this file with few tokens:** Read §0 only. Then `grep` for the
> tag of the section you need, for example `grep -n "\[#hotkeys\]"`. Read only
> that section with `sed -n`. Each section starts with a tag in square brackets.

---

## 0. Index  [#index]

| § | Tag | Topic |
|---|-----|-------|
| 1 | `[#rules]` | Project rules: names, versions, records, git, PR flow |
| 2 | `[#layout]` | Repository layout and what goes where |
| 3 | `[#game]` | Game facts: engine, ids, paths, assemblies |
| 4 | `[#bepinex]` | BepInEx version, install, Windows and Proton |
| 5 | `[#build]` | Build setup: csproj, references, target framework |
| 6 | `[#coexist]` | Rules so that our mods work together |
| 7 | `[#hotkeys]` | Vanilla hotkeys and the hotkey registry |
| 8 | `[#harmony]` | Harmony patterns and traps |
| 9 | `[#api]` | Game classes and methods (the engine map) |
| 10 | `[#visuals]` | Lighting, post-processing, UI |
| 11 | `[#perf]` | Performance facts |
| 12 | `[#console]` | Developer console commands |
| 13 | `[#debug]` | Logs, crashes, troubleshooting |
| 14 | `[#sources]` | The researched repositories and what each gives |
| 15 | `[#research]` | The `research/` folder |
| 16 | `[#open]` | Open questions and facts not yet verified |
| — | — | **Look-up lists** (enums, resource ids, laws, configs, signals, controllers, console commands, input actions): `Explained-game_engineMap.md` |

Legend used below: ✅ confirmed works · ❌ does not work · ⚠️ works with a
caveat · 🧭 fact or how-to · ❓ not verified by us. A fact with a source tag
such as `(JaySNL)` came from that repository (see §14). We did not verify it
in-game ourselves unless the line says so.

---

## 1. Project rules  [#rules]

### 1.1 Purpose of the project

- The project is a compilation of mods for Infection Free Zone (IFZ).
- The mods improve play, add challenges, and improve visuals and function.
- **No cheats.** Do not make a mod that gives free resources, god mode,
  infinite ammo, or similar. A cheat repository can teach a technique. Use the
  technique for a fair feature only.
- Each mod is a separate DLL. All mods must work together without conflict.
- Every mod must work on Windows **and** on Linux / Steam Deck through Proton.

### 1.2 File names and versions

- A mod DLL name: `IFZ-<modName>-v<MAJOR>.<MINOR>.<PATCH>.dll`.
  Example: `IFZ-darkerNights-v1.0.0.dll`. Use camelCase for `<modName>`.
- `MAJOR.MINOR.PATCH`: MAJOR = large or breaking change, MINOR = new feature,
  PATCH = bug fix.
- **Every edit of any file raises that file's version.** This rule is specific
  to this project. It applies to code, DLLs, and documents.
  - A DLL or mod carries the version in its file name, in the
    `[BepInPlugin]` version string, and in its `README` entry.
  - A document carries the version in a `File version:` line at the top. A
    document name does not carry the version, so links stay valid.
  - A small text fix to a document is a PATCH bump. New content is a MINOR
    bump.
- If Kurzon names a version, use his number.
- Update every location of a version in the same commit (see
  `Explained-user_kurzon.md` §5). Then `grep` for the old number.

### 1.3 The change record

- `FEATURES_AND_VERSIONS.md` records **every** edit, feature, and change for
  all files.
- One line per change: date and time (UTC), file name, new version, short
  description.
- Add the line in the same commit as the change. Put the newest line at the
  top of the table.

### 1.4 Git, PR, and main

- Develop on the branch that the session names.
- After each edit: commit, push, open a pull request to `main`, and merge it.
  Kurzon asked for this automatic flow on 2026-10-04. It is his permission to
  update `main` this way for this repository.
- Write the commit message as: what changed and why.

### 1.5 Releases

- `plugins/` holds the built DLLs. A user can download one DLL from there.
- A git tag `pack-v<X.Y.Z>` starts `.github/workflows/release.yml`. The
  workflow zips `plugins/` and the install guide and makes a GitHub Release.
- The pack version is separate from each mod version. Raise the pack version
  when any DLL in `plugins/` changes.

---

## 2. Repository layout  [#layout]

```
/                                   repository root
├─ README.md                        public front page: purpose, mod list, downloads
├─ FEATURES_AND_VERSIONS.md         change record for every file (§1.3)
├─ Explained-user_kurzon.md         how Kurzon works (general, all projects)
├─ Explained-game_infectionFreeZone.md   this file
├─ docs/
│  ├─ INSTALL.md                    install guide: Windows, Linux, Steam Deck
│  └─ HOTKEYS.md                    public hotkey list (mirror of §7.3)
├─ mods/                            mod source, one folder per mod
│  ├─ Directory.Build.props         shared build settings (game path)
│  └─ _template/                    copy this to start a new mod
├─ plugins/                         built DLLs, ready to download
├─ research/                        third-party mods for study only (§15)
└─ .github/workflows/release.yml    makes the zip release
```

- Do not put a game file (for example `Ifz.dll`) in the repository. The game
  files are not ours to share. The build reads them from the local install.

---

## 3. Game facts  [#game]

- 🧭 Developer: Jutsu Games. Publisher folder name: `JutsuGames`.
- 🧭 Steam App ID: **1465460** (full game). Prologue: **2485640** (old demo).
- 🧭 Engine: **Unity 6000.0.26**, scripting backend **Mono** (not IL2CPP).
  Mono runtime file: `mono-2.0-bdwgc.dll`. (trainer, JaySNL)
- 🧭 The game is a Windows binary. On Linux and Steam Deck it runs in Proton.
  There is no native Linux build.
- 🧭 Graphics API: D3D11 only. `-force-d3d12` and `-force-vulkan` fail. (JaySNL)
- 🧭 Executable: `Infection Free Zone.exe`.
- 🧭 Managed folder: `Infection Free Zone_Data/Managed/`.
- 🧭 Main game assembly: **`Ifz.dll`**. It ships with full managed code and a
  PDB, so a decompiler (ILSpy, dnSpy) shows readable code. (trainer)
- 🧭 Other assemblies in use: `Zenject.dll` (dependency injection),
  `Sirenix.Serialization.dll` (Odin serialization), `Unity.TextMeshPro.dll`,
  `UnityEngine.UI.dll`, PostProcessing v2, A* Pathfinding, Burst jobs.
- 🧭 Known Steam build ids: `24801014` (trainer), `25321152` (2026-09, added a
  second water check to placement — JaySNL). A game update can change type and
  method names. Check after each update.
- 🧭 The map is built from OpenStreetMap data, not generated. (JaySNL)
- 🧭 Default install paths:
  - Windows: `C:\Program Files (x86)\Steam\steamapps\common\Infection Free Zone`
  - Linux: `~/.steam/steam/steamapps/common/Infection Free Zone` or
    `~/.local/share/Steam/steamapps/common/Infection Free Zone`
  - Steam Deck SD card: `/run/media/mmcblk0p1/steamapps/common/Infection Free Zone`
- 🧭 Save and log folder (Windows):
  `%USERPROFILE%\AppData\LocalLow\JutsuGames\Infection Free Zone\`
  (holds `Player.log` and `Saves`).
- 🧭 Save and log folder (Proton): `<steamapps>/compatdata/1465460/pfx/drive_c/users/steamuser/AppData/LocalLow/JutsuGames/Infection Free Zone/` ❓ path built from the Proton
  standard layout. Verify on the Steam Deck.
- 🧭 Language files: `StreamingAssets/Languages/*.csv`, list in
  `_LanguageList.txt`. `LEManager` loads them. (JaySNL)

---

## 4. BepInEx  [#bepinex]

### 4.1 Which version

- ✅ Use **BepInEx 5.4.23.2, Windows x64, Mono**:
  `https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.2/BepInEx_win_x64_5.4.23.2.zip`
  (JaySNL uses it with 30+ mods on the current game.)
- Use the **Windows** build on Linux too. The game is a Windows binary in
  Proton.
- ⚠️ abevol used BepInEx 6.0.0-be.672 for the old Prologue. Do not mix BepInEx
  5 and 6 plugins. A BepInEx 6 plugin does not load in BepInEx 5.
- 🧭 ConfigurationManager (BepInEx plugin) gives an in-game settings window for
  every `ConfigEntry`. JaySNL ships it in `BepInEx/plugins/ConfigurationManager/`.

### 4.2 Install (Windows)

1. Unzip BepInEx into the game folder. `winhttp.dll` and
   `doorstop_config.ini` must be next to `Infection Free Zone.exe`.
2. Start the game once. BepInEx makes `BepInEx/config`, `BepInEx/plugins` and
   `BepInEx/LogOutput.log`.
3. Copy the mod DLLs into `BepInEx/plugins/`.

- ⚠️ Windows 11 **Smart App Control** can block `winhttp.dll` with no message.
  Symptom: no `LogOutput.log`, F1 only changes game speed. Only fix: turn
  Smart App Control off. (JaySNL)

### 4.3 Install (Linux, Steam Deck, Proton)

1. Do steps 1 to 3 of §4.2 in the Linux game folder.
2. **Required:** Steam → game → Properties → Launch Options:

   ```
   WINEDLLOVERRIDES="winhttp=n,b" %command%
   ```

   Without it, Proton loads its own `winhttp.dll` and BepInEx never starts.
   This is the most common failure.
- macOS CrossOver: set `winhttp` to `native,builtin` in `winecfg` →
  Libraries. Steam launch options do not work in CrossOver. (JaySNL)

### 4.4 Plugin folder rules

- 🧭 BepInEx 5 loads DLLs from `BepInEx/plugins/` **and its subfolders**.
- 🧭 Load order follows `[BepInDependency]`, not the file name.
- ⚠️ Our file names carry a version. An update leaves the old DLL in place
  unless the user deletes it. BepInEx 5 skips a duplicate GUID and keeps the
  higher version, but tell users to delete the old file anyway.
- Our install target: `BepInEx/plugins/IFZ-Compilation/`. One folder holds
  all our DLLs, so a user can remove or update the whole set at once.

---

## 5. Build setup  [#build]

- Target framework: `netstandard2.1` (JaySNL) or `net472` (MiKanSei39). Both
  work. **We use `netstandard2.1`.**
- **Build in the cloud session (how productionPlanner was built):**
  1. `apt-get install -y dotnet-sdk-8.0` (the dot.net install script is blocked
     by the proxy; NuGet.org works).
  2. Get the game's `Managed` folder and unzip it to a scratch folder. It was
     `research/Managed.zip` until Kurzon removed it (2026-10-04). If it is not
     in the repository, ask Kurzon where the game DLLs are now. **Without them
     no mod can be built.**
  3. Make a fake game folder: `<scratch>/gamedir/BepInEx/core/` with
     `BepInEx.dll` and `0Harmony.dll` (BepInEx 5.4.23.2, for example from a
     clone of JaySNL/IFZMods `manual-install/BepInEx/core/`), and
     `<scratch>/gamedir/Infection Free Zone_Data/Managed` → link to the unzipped
     `Managed`.
  4. `dotnet build mods/<mod>/<mod>.csproj -c Release -p:IFZGameDir=<scratch>/gamedir`.
     The DLL lands in `plugins/`. Delete `bin/` and `obj/` after.
- Decompile: `dotnet tool install -g ilspycmd --version 8.2.0.7535`, then
  `DOTNET_ROLL_FORWARD=Major ilspycmd -p -o <out> Ifz.dll -r <Managed>`.
- Extra references often needed: `Laungage.dll` (`MonoBehaviourSingleton`),
  `JutsuGamesConfig.dll` (`Config` base class), `Zenject.dll`.
- References (all with `<Private>false</Private>`, from the local game):
  - `BepInEx/core/BepInEx.dll`, `BepInEx/core/0Harmony.dll`
  - `Infection Free Zone_Data/Managed/Ifz.dll`
  - `.../Managed/UnityEngine.dll`, `UnityEngine.CoreModule.dll`
  - Add when needed: `Sirenix.Serialization.dll` (any type that derives from
    `SerializedScriptableObject`, for example `GroupDraft`, `CombatStats`),
    `Zenject.dll`, `UnityEngine.UI.dll`, `Unity.TextMeshPro.dll`,
    `UnityEngine.PhysicsModule.dll`, `UnityEngine.IMGUIModule.dll`,
    `UnityEngine.InputLegacyModule.dll`.
- The game path comes from the MSBuild property `IFZGameDir`, set in
  `mods/Directory.Build.props`. Override it on the command line:

  ```powershell
  dotnet build mods\darkerNights\darkerNights.csproj -c Release -p:IFZGameDir="D:\SteamLibrary\steamapps\common\Infection Free Zone"
  ```

- ⚠️ CI cannot build our mods. The game DLLs are not in the repository. Build
  on a machine with the game installed, then commit the DLL to `plugins/`.
- Pure managed code only. Do not use a native DLL or a Windows-only API. Then
  the same DLL works on Windows and in Proton.
- Use `Path.Combine` for paths. Do not hard-code `\` or `/`.

---

## 6. Coexistence rules  [#coexist]

These rules keep our mods from fighting each other or other people's mods.

1. **GUID:** `kurzon.ifz.<modName>`. The Harmony id is the same string.
2. **Config file:** BepInEx makes `BepInEx/config/kurzon.ifz.<modName>.cfg`.
3. **Master switch:** every mod binds `General / Enabled` (bool, default
   true). When false, the mod does nothing and patches return early.
4. **Hotkeys:** bind every key as a `ConfigEntry<KeyboardShortcut>`, so the
   user can change it. Take the default from the free list in §7.2 and
   register it in §7.3 **before** you write the code.
5. **One owner per patch target:** if two of our mods need the same game
   method, put the patch in one shared place (a future `IFZ-core` library) or
   give the method to one mod. Record the owner in §9.9.
6. **Never** call `harmony.PatchAll()` on the whole assembly when one bad
   target can stop all patches. Patch each class in its own try/catch (§8).
7. **Do not throw.** Wrap event handlers, UI callbacks, and `Update` code in
   try/catch. An uncaught exception opens the game's BugDetector window (§13).
8. **Read the game, do not replace it.** Prefer a postfix that changes a
   result over a prefix that skips the original.
9. **Keep `Awake` light.** Resolve reflection lazily on first use, not in
   `Awake`. Reflection at load time was linked to a BepInEx load stall.
   (JaySNL, MassDeconstruct)
10. **Gate in-game code:** run gameplay code only when a game is loaded, for
    example when `HqController.MainHeadquarter != null`. (JaySNL)

---

## 7. Hotkeys  [#hotkeys]

### 7.1 Vanilla keys (do not use)

Source: abevol (Prologue era) and JaySNL. ❓ Verify on the current build.

| Key | Vanilla action |
|-----|----------------|
| Space | Pause / resume |
| F1 / F2 / F3 | Game speed 1 / 2 / 3 |
| Esc | Game menu |
| C | Show / hide labels |
| V | Show / hide resource markers |
| B | Show / hide UI |
| N | Show / hide full info |
| Q / E | Rotate camera left / right |
| Z / X | Rotate placement template left / right |
| Ctrl (hold) | Disable placement snap; with left click: select many squads |
| Ctrl + 1–9 | Assign squad group |
| 1–9 | Select squad group |
| Shift + right click | Queue squad orders |
| Alt (hold) | Off-road driving; force attack on neutral squad |
| Keypad + / − | Zoom in / out |
| W A S D, arrows ❓ | Camera move |
| `` ` `` (backtick) | Developer console |

Other keys to avoid:

- **F12** — Steam screenshot. **Shift+Tab** — Steam overlay.
- **F1** — ConfigurationManager default. It **conflicts** with vanilla speed 1.
  Both actions happen. Recommend users set the ConfigurationManager key to
  **F10** in `BepInEx/config/com.bepis.bepinex.configurationmanager.cfg`.
- Keys that JaySNL's popular mods use (a user may run them with ours): `I`
  (hide icons), `K` (mass deconstruct), `B` (ShellLairs, bridge tool — this
  collides with vanilla B), `,` and `.` (farm size), `F7` (panel template).
- Keys that abevol's mod used: F5, H, G, F1, F4, P, J, M.

### 7.2 Free keys for our defaults

Use these first, in this order. All must stay rebindable.

1. `F6`, `F8`, `F9`, `F11` (single function keys, no vanilla use known).
2. `Ctrl + Shift + <letter>` for a second action in the same mod.
3. `Alt + Shift + <letter>` only when 1 and 2 are full.

On the Steam Deck there is no keyboard. Steam Input can map a function key to
a button. Keep each mod usable with the mouse only where possible.

### 7.3 Registry of our hotkeys

Add a row before you write the code. `docs/HOTKEYS.md` mirrors this table.

| Key | Mod | Action | Since version |
|-----|-----|--------|---------------|
| F10 (recommended) | ConfigurationManager | Open mod settings | — |
| F6 | productionPlanner | Open / close the planner window | 0.1.0 |

---

## 8. Harmony patterns and traps  [#harmony]

- ✅ Basic forms: `[HarmonyPatch(typeof(T), "Method")]` + `Prefix` /
  `Postfix(ref TResult __result)` / `Finalizer`. Use `__instance` for the
  object and `__state` to pass data from prefix to postfix.
- ✅ A property getter is patched as `"get_Name"`, a setter as `"set_Name"`.
  Example: `set_HordeSizeMultiplier` (MiKanSei39), `get_SwarmsIntensity` (JaySNL).
- ⚠️ **Overloads:** give the argument types, for example
  `[HarmonyPatch(typeof(PlaceableObjectDraft), "GetCapacity", new Type[] { typeof(float), typeof(float) })]`,
  or use `TargetMethod()` with `AccessTools.Method` / `AccessTools.Constructor`.
  An ambiguous target throws `AmbiguousMatchException`.
- ⚠️ **`PatchAll()` is all or nothing.** One bad target aborts `Awake` after
  some patches already applied. The exception can be invisible. Use:

  ```csharp
  foreach (var t in typeof(Plugin).Assembly.GetTypes())
  {
      if (t.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0) continue;
      try { _harmony.CreateClassProcessor(t).Patch(); }
      catch (Exception e) { Logger.LogError($"Patch {t.Name} failed: {e}"); }
  }
  ```
  (JaySNL, Surrounded)
- ⚠️ A Zenject class with no public constructor (for example `SwarmSpawner`)
  gives a null `TargetMethod`. Patch a different method.
- ⚠️ The Harmony `___field` shorthand failed for `Movement._character` on this
  runtime. Use `AccessTools.Field(typeof(Movement), "_character")` and cache
  the `FieldInfo`. (MiKanSei39)
- ⚠️ A transition-only setter (for example `SetSunIntensity`) does not run
  every frame. If your change must hold, capture the base value in the patch
  and re-apply it in your own `LateUpdate`. (JaySNL)
- ⚠️ Beta builds remove members. Read a volatile member with reflection and a
  fallback, so one DLL works on beta and stable. (JaySNL)
- 🧭 To find patch targets, decompile `Ifz.dll` with ILSpy. The PowerShell
  script `inspect_ifz.ps1` (trainer repo) lists console commands and all
  `ResourceID` names without a decompiler.

---

## 9. Game classes and methods  [#api]

Names are from `Ifz.dll`. Source tag in brackets. ❓ = not checked by us.

### 9.1 Time, day, weather

- `Controllers.Time.TimeController`: `Day` (static int), `Hour`,
  `SunriseHour`, `SunsetHour`, `GameplayTime`, `IsPaused`. Use `GameplayTime`
  for timers that must pause with the game. (JaySNL)
- `Controllers.Weather.WeatherController`. (MiKanSei39)

### 9.2 Difficulty and swarms

- `GameCustomization.GameCustomize`:
  - `SwarmsIntensity` getter — smaller value = more frequent swarms.
  - `HordeSizeMultiplier` getter/setter — higher = bigger and more frequent.
  - ✅ Patch these getters to scale hordes. The game then makes correct
    vanilla swarms. (JaySNL Surrounded, MiKanSei39 HordeSize)
- ⛔ **Do not spawn infected with `VirtualGroupsController.Create`.** The
  groups miss Zenject injection (`LairFinder._weatherController` is null) and
  crash the game every frame. (JaySNL)
- `EventsSystemController.HideoutsController` → `HideoutsController`:
  `Hideouts`, event `HideoutCleared (Action<Hideout>)`. "Hideout" and "Lair"
  are one system keyed by `Fraction`. (JaySNL)
- `Hideout`: `Id`, `Position`, `Fraction`, `PatrollingGroups`,
  `MainGroupCurrentCapacity` (true size; the live count is wrong for unseen
  camps), `Draft.PatrolGroupSpawnData`, `TryCreateGroupFromMainGroup(out Group)`.
- Lair formation: `SwarmGroupsToLairConverter`, settings in `HideoutsConfig`
  getters: `MinGroupsAmountToLairConvert`, `ConvertDelayGts`,
  `ChanceToConvert`, `MinCharactersAmountToConvert`,
  `MinBuildingVolumeToConvertLair`. (JaySNL)
- Swarm aggro can overflow to Infinity/NaN late game. Then night waves stop.
  JaySNL SwarmFix clamps the tier lookup. (JaySNL)
- `Fraction` values: `Player`, `Infected`, `Bandits`, `Bandits_ransom`,
  `Army`, `Immigrants`. Hostility: `group.EnemiesProvider.IsEnemy`. (JaySNL)

### 9.3 Groups, squads, orders, combat

- ⚠️ **Player squads are in `SquadsController.Squads`**, not in
  `GroupsController` (that one holds infected groups). (JaySNL)
- Give an order: `group.OrdersHandlers.GiveOrder(new MoveOrder(pos, true, null), true)`.
  Other orders: `AttackGroupOrder(Group)`, `AttackOrder(IGroup)`,
  `AttackOnGroundOrder(point)` / `AttackOnGroundOrder(new AttackOnGroundInfo(point, accessory))`.
- ⚠️ `AttackOnGroundOrder` moves the squad toward the target if it is out of
  reach. Check `distance <= weapon.Stats.GetReach(...)` first.
- Enemies in range: `group.EnemiesProvider.GetAllEnemiesInFightRange(null)`.
  Combat gate: `group.EnemiesProvider?.HaveEnemyInViewRange()`.
- State: `IsInState(StateType.Idle)`. Only 5 `StateType` values exist; a squad
  in a building or in combat is still `Idle`.
- Spawn a group: `GroupBuilder.SpawnGroupAt(pos, groupDraft, size)` — spawns
  **unarmed**. Arm through `GroupBuilder.Clear().SetWeapon(list)...Build(draft)`.
- Damage: `CombatStats.GetDamageForDistance(dist, expedition, sharpshooter, reachMod)`.
  There is no hit/miss roll for ranged fire. `GetChanceToHit` is display only.
- Damage seam: `CharacterFightHandler.GetDamage(weapon, opponent)` (private).
  Reach seam: `CharacterFightHandler.GetWeaponAttackReach`,
  `IsInAttackRange`.
- Stop-to-shoot: `IdleState.PauseOrdersExecution`,
  `StancesBehaviourHelper.CanStopWhileMoveToOrder` / `CanStopToAttackEnemy`,
  `GroupDraft.StopToShoot*`.
- Movement speed: `Gameplay.Units.Movements.Movement.CalculateSpeed(float, float)`.
  Filter by `Character.Type == CharacterType.Human`. (MiKanSei39)
- Death: `Character.OnCharacterDeath`, `ICharacterDyingSignal`,
  `DropResourceOnDeathController`. `ICharacter` is in `Gameplay.Units.Virtual`.
- Vehicle squad cap: `GetMaxGroupCount`; vehicle capacity
  `4 + floor(cargoSlots / 4)`. (JaySNL)
- Grenades: `GrenadesAccessoryResource`, `Group.AccessoryHandler`
  (`HasGrenadesAccessory`, `TryGetAccessoryResource`). Mortar = weapon
  `eq_mortar`. Bunker weapons: `StructureDefenceModule.SelectedWeapon`.

### 9.4 Resources and storage

- `Gameplay.GameResources.ResourceID` — a fixed enum (about 78 values). You
  cannot add a new resource. Examples: `res_ammo`, `res_metal`, `res_wood`,
  `res_bricks`, `res_planks`, `res_food_rations`, `res_fuel`, `res_cans`,
  `res_drugs`, `res_vegetables`, `res_meat`, `res_electronics`,
  `res_scientificmaterial`, `res_grenades`, `eq_assault_rifle`,
  `eq_sniper_rifle`, `eq_hcal` (heavy MG), `eq_mortar`, `eq_tank_cannon`.
- Global stock: `StockroomsController.Container` (not the HQ container).
- ✅ Remove safely: `TryRemoveResource(id, qty)`. Check `HasResource` before
  `GetResourceQuantity`.
- ❌ `ResourcesContainer.ForceRemoveResource` corrupts state → crash next frame.
- Weapon data: `Data.Resorces.ResourcesDataContainer.Instance.WeaponsData`
  (dictionary `ResourceID → Weapon`), filled in `SetupWeapons`. Weapon
  fields: `ammoAmountUsed`, `magazineCapacity`, `Stats` (`CombatStats`:
  `_reach`, `_reachOnExpedition`, `maxDmgCloseDistance`,
  `maxDmgReachDistance`, `sharpshooterMaxDmgReachDistance`). (MiKanSei39)
- Loot: `ResourcesToFindProvider.DrawResourcesForTag(poi, cubicMeter, amountMultiplier, expedition)`.
  Expedition loot ignores `amountMultiplier`; scale `cubicMeter` instead. (JaySNL)

### 9.5 Buildings and production

- `PlaceableObjectDraft` (building template): `Id` (for example
  `bld_greenhouse`, `bld_barn`), `ProductionsData`, `GetCapacity()`,
  `GetCapacity(float, float)`, `GetLivingCapacity(...)`, `HasMinesModule`,
  `HasGatheringModule`, `ResourceToGather`. (MiKanSei39)
- All drafts: `Data.Building.BuildingsDatas.Instance.GetAllBuildings()`;
  patch `Initialize` or `GetAllBuildings` postfix to change drafts once.
- `ProductionsData`: `productions` (list), `GetMaxWorkers(...)`,
  `minProductionQuantity`, `maxProductionQuantity`, `isProductionLimited`.
  A production: `GetProductionTime()`, `GetCycleCost()`, `GetCycleProfit()`.
- `ProductionWork` constructor — has overloads; select with
  `AccessTools.Constructor` and a full type list. (MiKanSei39)
- Work speed: `Gameplay.Units.Workers.WorkModule.ExecuteWork(float timeSinceLastTick)`.
  ⚠️ Scale `timeSinceLastTick`. Do not scale the efficiency value; it is a
  denominator in `2 - efficiency` and can go negative. (MiKanSei39)
- Gathering: `GatherableObject.InitWork`, `GatherResourcesWork._resourcesToDrop`,
  `AddResourceToDrop(ResourceID, float)`, `SetDefaultProfit()`. (MiKanSei39)
- Deconstruct: `Structure.StartDeconstruction()` (protected virtual). Gate:
  `Draft.IsDeconstructable`, not the main HQ, `!IsDeconstructing`,
  `!HasMarkerWithBlockedDeconstruction()`, `!IsRevealedHideout()`. (JaySNL)
- Structure states: `StructureStateType` (`Abandoned`, `Ruined`, `Rubble`),
  `StructureStateMachine.ChangeTo(...)`. Ruin look: `BuildingMaterialChanger`.
- Box select of buildings: `Physics.OverlapBox` on layer `"Building"`, then
  `GetComponentInParent<Building>()`. (JaySNL)
- Custom building checks: `CustomBuildingData.TestIsValid()`; size gates
  `IsBelowMaximalAreaSize()` (max 1000 m²) and `HasMinimalAreaSize()` (min 5 m²).
- Placement check: `MapObjectValidator.IsInValidPosition`. (JaySNL)
- Walls and gates: `WallConstructor.CreateWall(...)`, `GatesController.CreateGate(...)`.
- HQ: `HqController.MainHeadquarter`.

### 9.5a Production mechanics (verified in decompiled `Ifz.dll`, 2026-10-04)

- `ProductionWork` (`Gameplay.Production`) is the work of a production
  building. Get it from `structure.CurrentWork as ProductionWork`.
- **Speed is linear in workers.** Each producing worker adds
  `worker.WorkModule.DeltaTimeExecute × GetCurrentTemperatureEfficiency()` to
  the shared `CurrentProductionTime`. A cycle ends when it reaches
  `ProductionData.GetProductionTime()` (GTS). Then the building gets the
  `GetProfitPairs()` amounts. Workers that carry inputs or outputs
  ("logistics workers") do not add production time.
- `DeltaTimeExecute += timeSinceLastTick × 1 / (2 − Character.WorkerEfficiencyModifier)`
  (`WorkModule.ExecuteWork`). Mood modifier 1 → 100 %, 0.5 → 67 %.
- Work only happens in work hours: `Work.IsWorkHour()` = between
  `SunriseHour + WorkersConfig.WorkStartHourAfterSunrise + LawsController.GetWorkStartingHourModifier()`
  and `SunsetHour − WorkersConfig.WorkEndHourBeforeSunset + LawsController.GetWorkEndingHourModifier()`.
- Time: `TimeController.HourLengthInGts = DayLengthInSeconds / 24`. Convert
  with `ConvertGthToGts` / `ConvertGtsToGth`.
- **Slots by volume:** `ProductionsData.GetMaxWorkers(structure)` =
  `floor(Draft.MaxWorkers × Volume × 0.01 × PartialValue)`, min 1.
  `WorkBase.InitialMaxWorkers` = this cap. `WorkBase.MaxWorkers` = the
  player's limit (public setter; the panel's +/− button sets it, see
  `UI.InfoPanels.WorkersCountChanger`).
- **Daily cap by volume:** `ProductionWork.MaxDayProduction` =
  `floor(ProductionData.maxDayProductionPer100m3 × Volume × 0.01)`. 0 = no cap.
- ⚠️ `ProductionWork.DailyProducedAmount` resets each day **only** for virtual
  productions (`ResetDailyLimits`). For normal production it keeps growing.
  Track a day-start value yourself.
- Weather: `ProductionsData.GetTemperatureEfficiency(temp)` (a curve);
  `ProductionWork.GetCurrentTemperatureEfficiency()` (1 with PerfectWeather).
- Private fields in `Work`: `_lawsController`, `_workersConfig`,
  `_stockroomsController`, `_structure`.

### 9.5b Food

- `Game.FoodConsumingConfig` (ScriptableObject): `GetFoodConsumingData()` →
  `WorkerConsumePerDay`, `SoldierConsumePerDay`, `ChildConsumePerDay`.
- `FoodConsumeController` feeds everyone once per day at
  `GameConfig.hourOfConsuming`. Each person eats `res_food_rations`, or
  `res_cans` if rations run short. Amount × `LawsController.GetConsumptionModifier()`
  (Rations_Halved law).
- People: `CitizensController.Citizens` (`List<Character>`), `Character.IsChild`,
  `Character.IsSoldier`. Static counts: `CitizensController.CitizensCount`,
  `ChildrenCount`, `WorkersController.WorkersCount`.

### 9.5c Selection and service lookup

- Selected building = `InfoPanelController.CurrentDisplayedPanel.Structure`
  (`Controllers.InfoPanelController`, a `MonoBehaviourSingleton` from
  `Laungage.dll`). Find it with `Object.FindAnyObjectByType<InfoPanelController>()`.
- Any Zenject service: `Object.FindAnyObjectByType<Zenject.SceneContext>().Container.TryResolve<T>()`.
- Buildings: `Controllers.BuildingsController.Buildings` / `AdaptedBuildings`.
- Building name: `structure.Draft.GetNameBuilding()`. Stable id:
  `structure.GetId()`. Save id: `SaveHandler.SaveData.GameplayId`.
- Resource name: `ResourceData.GetName()` (localised).

### 9.5d Worker assignment (verified 2026-10-05)

- `Gameplay.Units.Workers.WorkSystem.WorkController` (Zenject): `Works`,
  `AvailableWorkers`, `EmploymentPriorities`. It handles
  `WorkPriorityChangedSignal(work, priority, allBuildings)` (namespace
  `Gameplay.Units.Player.Workers.WorkSystem.Signals`); fire it on the
  `SignalBus` to change a priority like the panel does.
- `WorksPriorityManager`: `_prioritiesWorks = new PriorityWorkGroup[11]`
  (priority 0–10). Panel buttons = 1–5. Alarm adds +5 to guard works
  (`WorkersCountChanger.AlarmPriorityOffset`). Priority 0 = no workers.
- Who moves workers: `RealignWorkers` (takes the closest worker from a lower
  priority via `GetClosestWorker` → `PriorityWorkGroup.TryGetClosestWorker`),
  `PriorityWorkGroup.AlignWorkers` (balances inside one priority, uses
  `WorkBase.GetClosestWorker`), `GetLeastNeededWorker` (squad draft, takes
  `Workers[0]`, sets `SetUnderSquadProduction(true)`).
- `WorkBase.RemoveWorkersAboveLimit` (max workers lowered) and the panel "−"
  remove `Workers[0]` first.
- `WorkBase.AddWorker` refuses when full, paused or already in the list.
  `Work.AddWorker` then calls `WorkModule.AssignWork`, which unassigns the
  old work. A worker inside an area work (scavenging) must be unassigned
  first (`CanBeAssignedToWork`).
- `WorkModule.UnassignWork` also calls `Character.FindBestHouse()`: changing
  job can change house.
- ⚠️ `WorksPriorityManager.FindWork` returns true even if `AddWorker` then
  refuses the worker; `WorkController.AddAvailableWorker` then never adds the
  worker to the free list. **Never make `AddWorker` refuse a worker** — the
  worker gets lost. To keep people out, make the work look full instead
  (write `WorkBase._maxWorkers` directly; the setter fires
  `OnMaxWorkerChange` and the game assigns the closest free worker at once).
- Sick: `SicknessController` unassigns the worker and calls
  `WorkersController.RemoveWorker` — the sick person stays in
  `CitizensController.Citizens` but leaves `WorkersController.Workers`.
- Soldiers: `CharactersConverter` removes them from the worker list;
  `Character.IsSoldier`.
- Character data: `Character.Name`, `Character.Id`,
  `CharacterInfo.Gender` (type in `CharacterNameGenerator.dll`), `.Age`.
- No work-experience system exists. `KnowledgeGainer` (squads) tracks only
  `Shooting`, `MeleeAttack`, `Scavenging`, `Driving`; `SkillId`: Slasher,
  Strong, HawkEye, SharpShooter, RaceDriver, EconomicDriver, Shoplifter,
  Inspector.
- House facts: a house = `Draft.HasLivingQuartersModule && Draft.HousePriority > 0`;
  `Structure.GetCitizensCapacity()` (virtual), `LivingCitizens`,
  `HasSpaceForCitizen()` (HQ always true), `FindNewHousesForCitizens(n)`.
  `Character.HouseData.House` setter calls `RemoveCharacterFromHouse` /
  `AssignBuildingAsHouse`. The game moves people out by setting the house to
  `HqController.MainHeadquarter` and calling `FindBestHouse()`.
- Houses: `HouseAssigner` → `HouseRequest` sorts houses by distance from the
  citizen's **current position**, takes the first with
  `HasSpaceForCitizen()`. `Character.HouseData.House`.
- Turning: `SicknessController.KillAndTurn` kills critical sick, then spawns
  `GetChanceToTurn() × deaths` fresh infected (`inf_human_fresh`) in one
  building.
- Work time per worker: `WorkModule.IsWorking()` = `CurrentWork.IsWorkHour()`.
  `WorkerBehaviour.Tick`: tired workers go home; carrying goods stops at night.

### 9.6 Map, fog of war, camera

- Map generation: `Map.GetMapGenerator()`, `Map.OnGenerated`,
  `HideoutsLocationsGenerator : LateGenerator`.
- Fog of war reveal without an observer object: push `(pos, radius)` each
  frame into `_fogOfWarObserversCountFinder.Add(...)` and
  `_fogOfWarUnitsVoid.Add(...)` (take both from a squad's
  `Character._fogOfWarObserver`). (JaySNL)
- Camera: `Cameras.GameCamera.Active` → private `_gameCameraParameters`
  (`GameCameraParameters`): `_minXAxisAngle`, `_maxXAxisAngle`,
  `_minYDistance`, `_maxYDistance`, `_pitchWithMouse`, `_unlockVerticalAxis`.
  The camera reads them every frame, so a field write takes effect with no
  Harmony. (JaySNL)
- Map overlays: `ViewLayersController`. (JaySNL)

### 9.7 Dependency injection (Zenject)

- Most controllers are Zenject singletons. To get one, patch its `Inject`
  method (postfix) and store `__instance`. Example:
  `[HarmonyPatch(typeof(SquadsController), "Inject")]`. (JaySNL)
- `GameInstaller` installs the session bindings. It is part-way done while the
  loading screen shows. Wait until the scene `Game` is active and a controller
  resolves. (JaySNL)

### 9.8 Developer command handler

- `Commands.GameConsoleCommandHandler.Instance` has public methods marked
  `[GameCommand]`. See §12. A mod can call them on the Unity main thread.

### 9.9 Patch-target owners (our mods)

| Game method | Owner mod | Note |
|-------------|-----------|------|
| `WorksPriorityManager` constructor (postfix) | productionPlanner | Resizes priority groups 11 → 15 (priorities 1–9, alarm +5) |
| `PriorityWorkGroup.TryGetClosestWorker` (postfix) | productionPlanner | Skips locked workers |
| `WorkBase.GetClosestWorker` (postfix) | productionPlanner | Picks the unlocked, least experienced worker to move |
| `Structure.HasSpaceForCitizen` (postfix) | productionPlanner | False for houses turned off |
| `Character.FindBestHouse` (prefix) | productionPlanner | Keeps locked citizens in their house |
| `WorkModule.ExecuteWork(float)` (prefix) | productionPlanner | Scales work time by job experience (+10/25/50 %) × foreman boost of the building |

---

## 10. Visuals and UI  [#visuals]

- ✅ Night brightness = the directional light `Light.intensity`, through
  `LightController.Light`. Re-apply every frame in `LateUpdate`. (JaySNL)
- ❌ PostProcessing v2 AutoExposure has no visible effect in this build.
- ⚠️ `RenderSettings.ambientMode == Flat`. Use `RenderSettings.ambientLight`
  (color). `ambientIntensity` is ignored.
- ⚠️ `ColorSwitcher` writes color grading only during sunrise and sunset. At a
  stable time, write the live `colorGrading` parameters each frame. Run every
  frame during the transition, or dawn and dusk flicker.
- 🧭 A PPv2 parameter does nothing unless `overrideState` is true.
- ✅ Real-time point and spot lights render. They cost the most GPU at night.
  Cull them with a viewport test, not 3D distance (the camera is high).
- 🧭 UI uses **TextMeshPro**. Take the font from any live `TextMeshProUGUI.font`.
  `TrailRenderer` / `LineRenderer` need `Shader.Find("Sprites/Default")`.
- 🧭 Read-only values in ConfigurationManager: a `ConfigEntry<string>` with a
  local class `ConfigurationManagerAttributes { ReadOnly = true }`.
  ConfigurationManager finds the class by name. (JaySNL)
- 🧭 Fonts: Brother1816 (latin), AlibabaPuHuiTi (CJK), Binggrae (Korean).
  Add a fallback with `TMP_Settings.fallbackFontAssets`. (JaySNL)

---

## 11. Performance  [#perf]

- 🧭 The game is CPU and main-thread bound, not GPU bound. GC is not the issue.
- ✅ Wins: fewer real-time lights; throttle A* graph updates
  (`GraphsUpdatesQueue`, vanilla up to 10 per frame; about 2 is smoother).
- 🧭 To reduce hitches, do **less** work per frame, not more.
- Our rule: no per-frame `FindObjectsOfType`. Cache controllers (§9.7).
  Throttle work with a timer when per-frame work is not needed.

---

## 12. Developer console  [#console]

- Open with backtick `` ` ``. `EnableCheats` unlocks locked commands.
- Useful for **testing** only (not for shipped features): `AddResourcesToHq <id> <qty>`,
  `AddCommonResources`, `AddWorkersToHq`, `SetTimeSpeed`, `SetHour`, `SetDay`,
  `SpawnImmigrants`, `RevealLairs`, `RevealHideouts`, `ShowVehicles`,
  `EnableFreeCameraMode`, `UnlockCamera`, `SetCameraZoomLevel`,
  `ShowTilesStats`, `FastConstructionState`, `ImmortalWalls`. (trainer, build 24801014)
- Avoid `KillAllGroups()` and `EndGame(...)`; they are destructive.

---

## 13. Debug and troubleshooting  [#debug]

- 🧭 BepInEx log: `<game>/BepInEx/LogOutput.log`. Overwritten each launch.
- ⚠️ Under **Proton**, the `LogOutput.log` writer can be dead. Use
  `UnityEngine.Debug.Log` for important lines; they land in `Player.log`
  (path in §3). (JaySNL)
- 🧭 `BugDetector` opens the bug window on any **uncaught** exception
  (`LogType.Exception`). `Debug.LogError` does not open it.
- Symptom table:

| Symptom | Cause | Fix |
|---------|-------|-----|
| No `LogOutput.log`, no `BepInEx/config` | BepInEx did not load | Windows: `winhttp.dll` beside the exe; Smart App Control. Linux: launch option §4.3 |
| F1 changes speed, no mod window | Same as above, or ConfigurationManager missing | Check BepInEx; check `BepInEx/plugins/ConfigurationManager/` |
| Mod missing from F1 list | Exception in `Awake` (often `TypeLoadException` from a missing dependency) | Read the log; install the dependency |
| Crash at launch, log not rewritten | Native injector (Lossless Scaling, RTSS) | Not a mod problem |
| "Unsupported save file" | Save from an older game build | Not our scope |
| Comma decimal locale cannot edit floats | Culture `,` decimal | JaySNL LocaleFix forces InvariantCulture |

---

## 14. Researched repositories  [#sources]

Researched on 2026-10-04. Clone again for full detail.

### 14.1 JaySNL/IFZMods — **best source**

- 32+ BepInEx 5 mods, MIT licence. **DLLs only** in the repo (source is in a
  private dev repo). Docs are excellent.
- Read first: `MODDING_NOTES.md` (engine facts, summarised in §8–§13),
  `README.md`, `STATUS.md`, `CHANGELOG.md` (119 KB, many class names),
  `INSTALL.md`, `install.sh` (Linux/Deck installer, game path detection),
  `install.ps1`, `docs/MACOS_CROSSOVER_FIX.md`,
  `examples/panel-mod-template/` (UI library template),
  `tools/site/api/IFZModAPI.json` and `IFZModPanels.json` (API docs of their
  shared libraries).
- Shared libraries: `000_IFZModAPI.dll` (GUID `com.ifzmod.api`; controller
  cache `Cache.*`, time and night helpers, VFX, reflection, save store,
  damage-multiplier registry, placement override, custom production API) and
  `IFZModPanels.dll` (GUID `ifz.modpanels`; in-game window builder). Both are
  "additive only" APIs.
- Lesson: their mods broke when a mod needed a newer API than the user had
  (`TypeLoadException`). Ship a library and its users together.
- Mod ideas: DarkerNights, GunfireLights, WindowGlow, CinematicFX (visuals);
  Surrounded, Hives, RaiderEscalation, SwarmFix (challenge); SquadMerge,
  SquadMoveFire, SquadGrenades, SquadAutoBehavior, VehicleSquadSize (squads);
  MassDeconstruct, DeconstructCancel, NoPath-YesPath, ConstructionETA,
  HouseRebalance, SmartWorkerRedist, ClayPitFixes, Expanded Farming,
  HighGround, BridgeProto, Unlocked Buildings (base building); PerfPack
  (performance); LocaleFix, SaveUnlock, IFZMacFix, ThaiLanguage (fixes).

### 14.2 MiKanSei39/InfectionFreeZoneMods

- 8 small BepInEx/Harmony projects **with full C# source** (`net472`).
  Good, short examples of patch targets (§9.4, §9.5).
- `Directory.Build.props` with `IFZGameDir`; csproj copies the DLL to
  `BepInEx/plugins` after build. We copied this idea.
- Probes: `IFZResourceProbe` (dumps building production data),
  `IFZWeaponProbe` (dumps weapon stats). Useful for research.
- The balance mods are strong multipliers (some are close to cheats). Use them
  for technique only.

### 14.3 tianq021/InfectionFreeZoneTrainer — cheat, technique only

- An external Python trainer that injects a C# bridge into Mono. Not a
  BepInEx mod. Do not copy the approach.
- Useful: `inspect_ifz.ps1` (lists `[GameCommand]` methods and `ResourceID`
  names from `Ifz.dll`), the console command list (§12), the game facts in
  `IFZ_修改器分析.md` (App ID, build id, Mono, `Ifz.dll` SHA-256).

### 14.4 abevol/InfectionFreeZoneMod — old, Prologue

- Chinese mod for the **Prologue** (`Infection Free Zone Prologue.exe`),
  BepInEx 6.0.0-be.672, UniverseLib, BepInExConfigManager, own updater. DLL
  only, no source. v1.5.5.
- Useful: the vanilla hotkey list (§7.1), feature ideas (auto-repair at
  sunrise, auto-scavenge, tower weapon fixes, worker shift times, Ctrl to
  maximise building adaptation, extra hotkeys). The Prologue code is probably
  out of date for the full game.
- Known issue there: a path with Chinese characters stops the plugin load.
  Rule for us: test with a non-ASCII path. ❓

### 14.5 bessone-mere/Infection-Free-Zone-hacks-kit-addition — ⛔ UNSAFE

- **No code.** The README is SEO spam with image links to an unknown external
  domain. It is a typical malware lure. **Do not download anything from it.
  Do not link to it from our pages.** Nothing useful for modding.

---

## 15. The research folder  [#research]

- `research/` holds third-party mods for study. Kurzon uploads them.
- `research/Managed.zip` held the game's `Managed` folder (incl. `Ifz.dll`).
  Kurzon removes it from the public repository on 2026-10-04 (game property).
  Everything useful from it is in `Explained-game_engineMap.md` and §9.
  A new build still needs the DLLs (see §5).
- Decompiled research mods worth reading: `ProductionDashboard.dll` (selected
  building via `InfoPanelController`), `IFZBuildingManager.dll` (save ids,
  worker limits, F5 key), `ExpandedCitizensPanel.dll`.
- Do not ship anything from `research/` in `plugins/` or in a release.
- Before you copy an idea or code, check the licence. JaySNL is MIT (credit
  them). If a repo has no licence, take ideas only, not code.
- To inspect a DLL from `research/`, decompile it with ILSpy
  (`ilspycmd <file>.dll -o <outdir>`). Do not commit the decompiled output.
- Record each studied mod in §14 or in `research/README.md`.

---

## 16. Open questions  [#open]

- ❓ Default keys of the Rewired actions (engine map §2). They are in a game
  asset, not in code. Check the in-game Controls menu.
- ❓ Exact `Player.log` path under Proton on the Steam Deck.
- ❓ Whether ConfigurationManager's F1 conflict hurts play (both actions fire).
- ❓ Whether to build our own shared library (`IFZ-core`) or depend on
  JaySNL's `IFZModAPI`. Decide when the second mod needs a shared feature.
- ❓ Current Steam build id. Record it with the date when you check.

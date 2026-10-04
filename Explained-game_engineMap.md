# Infection Free Zone — Engine Map

> **File version:** 1.0.0 · **Last edit:** 2026-10-04 10:40 UTC
>
> **Purpose:** Names and data from the decompiled game code (`Ifz.dll`), so a
> future session can plan a mod without the game files. Read the main file
> `Explained-game_infectionFreeZone.md` first. Behaviour details (formulas,
> traps) are there in §8–§13; this file is the look-up list.
>
> **Source:** `research/Managed.zip`, `Ifz.dll` SHA-256
> `6b4f99b026be9efaa9d84bffd925239a4cae9424ba0a4439268f57c79dfb6fb8`, size
> 4 717 568 bytes, file date 2026-09-18. A game update can change any name.
>
> **Search:** `grep -n "\[#<tag>\]"`, then read that section only.

| § | Tag | Topic |
|---|-----|-------|
| 1 | `[#asm]` | Game assemblies and third-party libraries |
| 2 | `[#input]` | Input system (Rewired) and action names |
| 3 | `[#enums]` | Key enums (resources, laws, jobs, states, fractions) |
| 4 | `[#custom]` | Difficulty settings (`GameCustomize`) |
| 5 | `[#configs]` | Config ScriptableObjects |
| 6 | `[#signals]` | Zenject signals and static events to hook |
| 7 | `[#controllers]` | Main controllers and their public members |
| 8 | `[#commands]` | All developer console commands |
| 9 | `[#namespaces]` | Namespace map |

---

## 1. Assemblies  [#asm]

- Game code: **`Ifz.dll`** (almost everything), `Laungage.dll`
  (`MonoBehaviourSingleton<T>`, language), `JutsuGamesConfig.dll` (`Config`
  base class), `MapOperator.dll` (map streaming), `TileMapSystemCamera.dll`
  (camera base), `Launcher.dll`, `CallSystem.dll`, `InputHelper.dll`.
- `Assembly-CSharp.dll` exists but holds little; use `Ifz.dll`.
- Libraries: **Rewired** (input), **Zenject** (DI), **Sirenix Odin**
  (serialization), **MessagePack** + **protobuf-net** (saves), **Newtonsoft.Json**,
  **AstarPathfindingProject** (navigation), **FMOD** (audio), **DOTween**
  (tweens), **Beautify** (post-processing / colour), **VolumetricFog**,
  **CloudShadows**, **AmplifyImpostors**, **The Vegetation Engine**,
  **MeshAnimator** (unit animation), **BehaviorDesigner** (AI trees),
  **XNode**, **OsmSharp** (OpenStreetMap), **Opencoding.Console** (the `` ` `` console),
  **Steamworks.NET**, **TextMeshPro**, **UnityUIExtensions**.
- Build references used so far: `Ifz`, `Laungage`, `JutsuGamesConfig`,
  `Zenject`, `Sirenix.Serialization`, `UnityEngine`, `UnityEngine.CoreModule`,
  `UnityEngine.IMGUIModule`, `UnityEngine.InputLegacyModule`.

---

## 2. Input  [#input]

- The game reads keys through **Rewired** actions. The default key for each
  action is in the Rewired data asset, **not** in code. Players can rebind.
- Keyboard layouts: Rewired rule sets `Default`, `German` (QWERTZ), `French`
  (AZERTY) are switched by system language (`GameInput.KeyboardControllerBase`).
- `UnityEngine.Input.GetKeyDown` (legacy input) still works for mods. Other
  mods (IFZBuildingManager, JaySNL) use it.
- Modifier state: `KeyboardControllerBase.IsShifPressed` (sic),
  `IsControlPressed`, `IsAltPressed`.
- Rewired action names: `CenterOnHQ`, `ClickModifier1/2/3`,
  `ExpeditionViewToggle`, `GetGroup1–9`, `SaveGroup1–9`, `MiddleAction`,
  `MoveCameraHorizontal(Fast)`, `MoveCameraVertical(Fast)`, `PauseGame`,
  `PrimaryAction`, `SecondaryAction`, `RotateCamera`,
  `RotatePlaceableObjectCW/CCW`, `ScavengeView`, `SelectNextSquad`,
  `SetNormalSpeed`, `SetFasterSpeed`, `SetFastestSpeed`,
  `StructureOutlineToggle`, `Submit`, `ToggleGameMenu`, `ToggleLabels`,
  `ToggleUI`, `UICancel`, `UISubmit`, `ZoomChange`.
- Info-panel button actions (`InfoPanelAction` enum names):
  `RepairInfoPanelButton`, `RepairAllInfoPanelButton`, `DeadaptInfoPanelButton`,
  `DeconstructInfoPanelButton`, `CancelInfoPanelButton`,
  `ReplenishInfoPanelButton`, `ExchangeInfoPanelButton`, `SplitInfoPanelButton`,
  `DisbandInfoPanelButton`, `GoToHqInfoPanelButton`, `MoveStanceSwitcher`,
  `FireStanceSwitcher`, `ThrowGrenadeInfoPanelButton`.
- ❓ The default keys for these actions are still unverified. Check the
  in-game Controls menu and record them in the main file §7.1.

---

## 3. Enums  [#enums]

**`Gameplay.GameResources.ResourceID`** (fixed; no new values possible):
`None res_cans res_fuel res_ammo res_bricks res_metal res_wood res_fertiliser
res_drugs res_explosives res_ammotank res_vegetables res_meat res_electronics
res_basic_tool res_adv_tool res_scientificmaterial res_planks
eq_basic_melee_weapon eq_pistol eq_sniper_rifle eq_assault_rifle eq_shotgun
eq_wear_protectors eq_wear_riot_gear eq_wear_plate eq_wear_camouflage
eq_wear_soldier_gear eq_bow eq_crossbow eq_flamethrower res_grenades eq_molotov
res_grain res_food_rations eq_wear_default res_unknown eq_inf_alpha eq_inf_bear
eq_inf_bird eq_inf_dog eq_inf_growler eq_inf_huge eq_inf_human eq_inf_moose
eq_inf_screamer eq_inf_tank_bull eq_inf_tank_moose res_chicken
eq_inf_*_boosted (11 kinds) res_scientists_case eq_polearm res_scientists_vaccine
res_scientists_infected virt_children_care virt_bar_spot res_scientists_sample
eq_inf_bull eq_animal_boar eq_animal_deer virt_metal_scraps eq_hcal res_mines
eq_mortar eq_tank_cannon res_beverages eq_inf_human_explosives virt_corpse
virt_infected_corpse`
(`eq_inf_*` = infected "weapons"; `virt_*` = virtual resources.)

**`Gameplay.Laws.LawID`**: `Birth_Control / Birth_NoLaw / Birth_ChildSupport`,
`Age_ChildWorkers / Age_NoLaw / Age_FullMaturity`,
`Rations_Halved / Rations_NoLaw / Rations_Double`,
`Work_Extra / Work_NoLaw / Work_Off`,
`Migrants_Deny / Migrants_NoLaw / Migrants_Accept`,
`Sickness_Kill / Sickness_NoLaw / Sickness_ExtraCare`,
`Dead_Cannibalism / Dead_NoLaw / Dead_CeremonialBurial`,
`Infected_Autopsy / Infected_NoLaw / Infected_BodyDisposal`.
Read with `LawsController.GetLaw(LawID).IsEnabled`.

**`ScriptableObjectScripts.UI.Employment`** (jobs): `Unemployed SquadMember
prof_builder prof_scavenger prof_farmer prof_food prof_guard prof_factory
prof_scientist prof_nurse TotalCitizens Children Elderly Homeless prof_teacher Sick`.

**`Gameplay.Units.Workers.WorkSystem.WorkType`**: `Research Forecast
Production Defence Deconstruct Area Build Mend Handyman Nothing CarWorkshop
PlantTree Forester ReplantTree Sawmill Scrapyard`.

**`Gameplay.Rebuilding.StateMachine.StructureStateType`**: `None Empty
Abandoned Constructing Completed Ruined Repairing Deconstructing Placing
Demolishing Rubble ExtraAdapting Deadaptation Creating Customization Split
Hideout Depleted NpcZoneDefault NpcZoneHostile PlantingExplosives`.

**`Gameplay.Units.Fraction`**: `Player Infected Bandits Army Survivors
Immigrants Bandits_ransom Vendor Animals Provirus None`.

**`Core.States.StateType`** (group state): `Idle Inactive Climb InWater
ExchangeInactive Hiding`.

**`Gameplay.Units.CharacterType`**: `Human Infected Animal`.

---

## 4. Difficulty settings  [#custom]

`GameCustomization.GameCustomize` (Zenject; new-game options). Patch a getter
postfix to change a value for the whole game:

| Type | Property |
|------|----------|
| float | `PeopleCountMultiplier`, `ResourcesFoundQuantityMultiplier`, `HordeSizeMultiplier`, `DayToNightRatio`, `SwarmsIntensity` |
| int | `InitialDayOfYear`, `DayLengthInSeconds`, `MinBlackoutStorms`, `MaxBlackoutStorms`, `MaxPopulation`, `SwarmsIntensityBySlider`, `WorkManagement` |
| bool | `TutorialEvents`, `StoryEvents`, `TerrainElevation`, `Sickness`, `SuperMoons`, `SwimmingInfected`, `InfectedAnimals`, `Lairs`, `Hideouts`, `ConvoyStart`, `FriendlyFire` |
| other | `Citizens` (CitizensData), `Emblem`, `DifficultyLevel` (GameDifficulty), `LocationType` |

Challenge-mod levers: `HordeSizeMultiplier`, `SwarmsIntensity`,
`MaxPopulation`, `ResourcesFoundQuantityMultiplier`, `DayToNightRatio`,
`MinBlackoutStorms/MaxBlackoutStorms`, `SuperMoons`.

---

## 5. Config ScriptableObjects  [#configs]

All derive from `JutsuGamesConfig.Runtime.Config`. Get one with
`SceneContext.Container.TryResolve<T>()` or
`Resources.FindObjectsOfTypeAll<T>()`. Their numbers live in game assets, not
in code, so read them at run time (for example with a probe mod).

`AdaptConfig AreaDamageConfig AudioConfig BuildingCustomizationConfig
BuildingDestructionConfig BuildingVisualsConfig CarWorkshopConfig
CharactersConfig ColorsMaterialsConfig ContentConfig ConvoyLocationsConfig
ConvoyMigrationConfig CustomBuildingConfig DebugConfig DropResourceOnDeathConfig
DropSettingsConfig EconomyConfig EventSystemConfig ExpeditionConfig
FallAnimConfig FarmlandsConfig FightSimulatorConfig FilterPOIConfig
FirstHqConfig FoodConsumingConfig FractionsConfig GameCameraParameters
GameConfig GameCustomizeConfig GroupSpawningConfig GroupsConfig HideoutsConfig
ImmigrantsSettings LawsConfig MapConfig MilitaryBackupConfig MoodConfig
MovementConfig NpcZoneGeneratorConfig OsmWallsGeneratorConfig PathsConfig
PerformanceConfig PoiGeneratorConfig PrefabConfig RegenerableObjectsConfig
ResearchConfig RoadmapConfig SFXConfig SaveUIConfig
ShootingDistanceNamingConfig SicknessConfig SplitConfig SquadsConfig
SwarmConfig TowersConfig TransmissionsSystemConfig UIConfig VehiclesConfig
VendorConfig ViewLayersConfig WallConfig WeatherConfig WorkersConfig`

Known fields: `FoodConsumingConfig` (worker/soldier/child consume per day,
shelf life) · `WorkersConfig.WorkStartHourAfterSunrise`,
`WorkEndHourBeforeSunset`, `WorkerCrateCapacity` · `EconomyConfig.workersBehaviourConfig`,
`timeToChangeWorkerPositionDuringProductionInGTS` · `GameConfig.hourOfConsuming`.

---

## 6. Signals and events  [#signals]

**Zenject signals** (subscribe with `SignalBus.Subscribe<T>(handler)`; get the
`SignalBus` from `SceneContext.Container`):
`AbandonBuildingEnteredSignal AntennaCreatedSignal AntennaDestroyedSignal
AssignWorkersSignal CancelAdaptationSignal CharacterHpSubtractedSignal
CharacterInsideUpdatedSignal ClearAllSelectedObjectsSignal EventTriggerSignal
ExpeditionMarkerCreateSignal ExpeditionMarkerRemoveSignal FilterPOIChangedSignal
FindWorkSignal GameCommunicateSignal GroupAttackedSignal GroupBecameViableSignal
GroupCreateSignal GroupDiedSignal GroupEnteredEnterableSignal
GroupGameCommunicateSignal HqLoadedSignal ICharacterDyingSignal
ImmigrantsGroupConvertedSignal ImmigrantsReachedHqSignal ImmigrantsSpottedSignal
InitialWorkersCreatedSignal InitiateMigrationSignal LodChangeSignal
MapGenerationProgressSignal NotificationStateChangeSignal OnSavedSignal
PhysicInitializedSignal ProduceResourcesSignal ProductionChangedSignal
RefreshMissionPanelSignal RemoveOsmObjectSignal RequestTreeRegrowSignal
ResourceAddedToStockroomSignal ResourceDroppedSignal SetCollisionRadiusStateSignal
SoldiersAmountChangedSignal SquadStateChangedSignal SquadWeaponCursorSelectSignal
StartActionSignal StructureDraftChangedSignal StructureHPChangedSignal
StructureStateChangedSignal TreeFallenSignal VendorCooldownFinishSignal
VendorExitZoneSignal VendorReachedHqSignal VirtualGroupCreateSignal
VirtualGroupDiedSignal WorkCreatedSignal WorkPriorityChangedSignal
WorkersAmountChangedSignal`

Useful ones: `ProduceResourcesSignal(draftId, productionId, profit)` (every
finished production cycle — exact production counting), `HqLoadedSignal`
(game ready), `OnSavedSignal`, `StructureStateChangedSignal`,
`GroupDiedSignal`, `ResourceAddedToStockroomSignal`.

**Static C# events** (subscribe directly; unsubscribe on scene unload):
`TimeController.OnTimeOfDayChanged`, `WorkBase.OnWorkplaceActivate`,
`OnGameStart`, `AfterGameStarted`, `LoadingFinished`, `OnSavingStarted`,
`SceneChanged`, `UnloadActiveSceneStarted`, `OnSettingsChanged`,
`OnCheatsEnabled`, `OnResourceFoundInBuilding(Building, ResourceCrate)`,
`OnScavengeProgressUpdate(Building)`, `OnVehicleSpotted(Vehicle)`,
`OnAbandonedVehicleTaken(Vehicle)`, `OnGroupHpChanged(Group)`,
`OnAvailableWorkersCountChange(int)`, `OnMissionComplete(string, MissionResult)`,
`OnEventAppeared(string)`, `OnEventEnded(string)`, `OnSciMaterialsUpdated(float)`.
(Find the declaring class with `grep -rn "event .* <Name>"` in a decompile.)

---

## 7. Controllers  [#controllers]

All are Zenject services: `SceneContext.Container.TryResolve<T>()`.

- **`Gameplay.Mood.MoodController`**: `CurrentMood`, `CurrentMoodLevel`,
  `MoodModifiers`, `AddModifier`, `RemoveModifier`, `GetMoodModifier`,
  `GetCurrentWorkersEfficiencyModifier`, `GetBirthRateModifier`, `SetMaxMood`.
- **`Gameplay.Research.ResearchController`**: `Researches`, `CurrentTechnology`,
  `CurrentLaw`, `GetResearch`, `GetResearchProgress`, `IsResearchCompleted`,
  `AddTechnologyResearchProgress`, `SetResearchState`, `SetResearchAvailable`,
  `UnlockResearchType`, `ScientistsNumber`, `GatheredScientificMaterials`.
- **`Controllers.Sickness.SicknessController`**: `GetSickCount`,
  `GetCriticalSickCount`, `GetCuredCount`, `GetVaccinatedCount`,
  `GetVaccineSupplyDays`, `MakeCharacterSick`, `SetBasicChanceToGetSick`,
  `IsWorkingMedbayOrHospital`.
- **`Controllers.Weather.WeatherController`**: `CurrentTemperature`,
  `CurrentWeather`, `CurrentSeason`, `IsWinter`, `IsSnow`, `IsFrost`,
  `IsIceSheet`, `Forecast`, `ForecastPoints`, `History`,
  `IsInfectedTimeToHide`, `RemoveSupermoon`.
- **`Gameplay.Units.Spawning.SwarmSpawner`**: `Swarms`, `SpawnSwarm`,
  `CreateSwarm`, `GetAggroPoints`, `ModifyAggroPoints`, `NextAggroLevel`,
  `PreviousAggroLevel`, `GetSwarmInZoneGroupCount`.
- **`Controllers.HqController`**: `MainHeadquarter`, `MainHq`,
  `IsMainHeadquarterExists`, `GetClosetHqFrom`, `CenterOnMainHq`.
- **`Gameplay.Units.SquadsController`**: `Squads`, `SelectedSquads`,
  `SoldiersCount`, `GetActiveSoldiersCount`, `MoveSelectedTo`,
  `ReturnAllToHq`, `MoveToHqAllSquads`, `GetNearestSquad`,
  `SelectSquadsInArea`, `ConsumeFood`, `IncreaseMaxSquadsCount`,
  `MaxSquadToCreateCount`.
- **`Gameplay.Immigrants.ImmigrantsController`**: `AcceptImmigrants`,
  `DeclineImmigrants`, `SpawnImmigrantsAccepted`, `StartBroadcastInviteSystem`.
- **`EventsSystem.EventsSystemController`**: `ActiveEvents`, `ActiveMissions`,
  `HideoutsController`, `ImmigrantsController`, `BuildingsController`,
  `CompleteActiveMissions`, `IsEventLaunched`, `IsMissionComplete`.
- **`Controllers.BuildingsController`**: `Buildings`, `AdaptedBuildings`,
  `DetonatedBuildings`.
- **`Controllers.InfoPanelController`**: `CurrentDisplayedPanel(.Structure)`,
  `CurrentSecondDisplayedPanel`, `ShowInfoPanelType`, `HideAllPanels`,
  events `OnShowPanel`, `OnCloseOrHidePanel`.
- **`Controllers.CharacterLogic.CitizensController`**: `Citizens`, static
  `CitizensCount`, `ChildrenCount`, `AdultCitizensCount`.
- **`Gameplay.Units.Player.Workers.WorkersController`**: `Workers`, static
  `WorkersCount`, `PercentOfAvailableWorkers`.
- Other controllers in `Controllers/`: `AdaptController`, `RepairAllController`,
  `FogOfWarController`, `MapViewModesController`, `StructureLabelsController`,
  `VehiclesController`, `DeadCharactersController`, `GameOverController`,
  `ResourceDiscardController`, `FightCounter`, `FightTensionTimer`,
  `BulletTracerController`, `MusicSystemController`.

---

## 8. Console commands  [#commands]

`Commands.GameConsoleCommandHandler.Instance` — 178 `[GameCommand]` methods
(build of 2026-09-18). For testing only; many are cheats. Grouped:

- **Camera / view:** `UnlockCamera`, `SetClosestCamPosY`, `SetCameraZoomLevel`,
  `ShowDebugZoomInfo`, `EnableFreeCameraMode`, `DisableFreeCameraMode`,
  `EnableOldCamera`, `DisableOldCamera`, `HideTerrain`, `ResetTerrainMaterial`,
  `DisableVolumetricFog`, `SetVolumetricFogState`, `SetLodBias`,
  `DisableBldLod`, `EnableBldLod`, `ChangeMainUILayerState`.
- **World objects on/off:** `SetTreesState`, `SetDebrisState`,
  `SetStreetlightsState`, `SetVehiclesState`, `SetWreckedCarsState`,
  `SetBuildingsState`, `SetBuildingsRendererState`, `SetGroundDecorationsState`,
  `SetEntrancesState`, `SetCullingManagerState`, `DestroyBuildingsDecorations`,
  `SetBuildingsChildShadowCast`.
- **Info:** `GetBuildingsCount`, `GetTreesCount`, `LogNpcGroupsCount`,
  `PrintAggroPoints`, `TestAggroPoints`, `ClearedLairsCount`,
  `ClearedHideoutsCount`, `ShowBldDuplicates`, `LogPoiTypesCount`,
  `ShowTilesStats`, `SwarmDebug`, `IsBuildingWithOsmIdExists`,
  `IsBuildingWithIDExists`, `IsStandingBuilding`.
- **Spawning:** `SpawnSquadAtCursor`, `SpawnBigSquadAtCursor`,
  `SpawnGroupAtCursor(WithWeapon)`, `SpawnGroupsAtExpedition`,
  `SpawnVirtualGroupAtCursor(WithWeapon)`, `SpawnVehicleGroupAtCursor`,
  `SpawnGroupInBuilding`, `SpawnHideoutInBuilding`, `SpawnVehicle`,
  `SpawnArmoredVehicleWithTowerAt`, `SpawnSwarm`, `SpawnMultipleSwarm`,
  `SpawnExtendedSwarm(AtCursor/CloseToZone)`, `SpawnSwarmAtEdge`,
  `SpawnSwarmCloseToZone`, `SpawnSwarmAtCursor`, `SpawnImmigrantsAccept`,
  `StartMigration`.
- **Build:** `CreateWall`, `CreateFarmland`, `CreateMinefield`, `CreateGate`,
  `CreateTower`, `AdaptBuilding`, `SelectCustomBuildingCursor`,
  `SelectRebuildCursor`, `SetImmediateSplit`, `SetBuildingCapacity`.
- **Resources:** `AddResourcesToBuilding`, `DiscardResourcesFromBuilding`,
  `DiscardResourcesFromHq`, `AddCommonResources`, `CreateResourceCursor`,
  `ScavengeAllBuildings`, `ScavengeAllExpeditions`, `FillFractionResourcesContainer`,
  `ClearFractionResourcesContainer`, `AddResourceSupplyList`.
- **Time / weather / events:** `SetTimeSpeed`, `StartWeatherFog`,
  `BlockSupermoon`, `SetEventsSystemState`, `RemoveEventFromPool`,
  `CompleteActiveMissions`, `CheckCondition`, `RunAutoSave`.
- **Research / content:** `UnlockAllContent`, `SetResearchState`,
  `SetResearchAvailable`, `UnlockAllExpeditionTiles`.
- **Cheat toggles:** `FastConstructionState`, `FastResearchState`,
  `FreeResearchState`, `NoHungerState`, `PerfectMoodState`, `FastScavengeState`,
  `InfiniteAmmoState`, `PerfectWeatherState`, `ImmortalWalls`, `SetSoldierHp`,
  `SetNpcHp`, `SetFriendlyFire`, `SetCitizenChanceToGoSick01`.
- **Destructive (avoid):** `KillAllGroups`, `KillWorker`, `KillAllWorkersExcept`,
  `SelectAreaDamageCursor`.
- **Vendor / army:** `SetVendorState`, `ResetVendorCooldown`,
  `SetVendorCooldown`, `InvokeStartMilitaryBackupSystem`, `ClearImmigrantsCd`.

Source of each command's code = a good example of how to do that action from
a mod.

---

## 9. Namespaces  [#namespaces]

277 namespaces. Main groups (count): `Gameplay.*` 109, `UI.*` 32,
`MapEssentials.*` 13, `Controllers.*` 12, `Utilities.*` 11, `EventsSystem.*` 11,
`Serialization.*` 9.

`Gameplay.*` areas: Buildings, CollisionDetections, Communicates,
Core.EnterableSystem, Core.Icons, Explosives, FogOfWar, GameResources, Guns,
Immigrants, InGameResources, Laws, LevelOfDetail, MapLayers, Military, Mood,
Notifications, ObjectOnMap (POI, Initializers), ObjectsSearching, Production,
Rebuilding (CustomBuildings, Databases, Farmlands, Gates, PredefinedBuildings,
Split, StateMachine, Towers, Walls), Research, Scavenge, Signals, Simulators.Fight,
Stats, Trading, Units (AI.Swarm, AI.Tasks.*, Characters, Combat, Enemy.Hideouts,
Enemy.TargetAppetite, Equipment, FoodConsuming, LineOfSlight, Modifiers,
Movements, Orders, Player.Workers.WorkSystem, Skills, Spawning, Swarms, Virtual,
Workers.*), Vehicles, Vendor, Weather.

`Controllers.*`: Birds, CharacterLogic, Dragging, Exchange, InputTips,
Pausing, Radio, Sickness, Time, Weather.

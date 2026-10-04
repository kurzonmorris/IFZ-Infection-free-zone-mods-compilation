# Mod template

> **File version:** 1.0.0 · **Last edit:** 2026-10-04 01:00 UTC

Copy this folder to start a new mod. It is never built or shipped as it is.

1. Copy `mods/_template/` to `mods/<modName>/`. Use camelCase for `<modName>`.
2. Rename `template.csproj` to `<modName>.csproj`.
3. In the `.csproj`, set `ModName`, `Version`, and `RootNamespace`.
4. In `Plugin.cs`, set the namespace, `Guid` (`kurzon.ifz.<modName>`), `Name`, and `Version`.
   The `Version` must match the `.csproj`.
5. Register any hotkey in `Explained-game_infectionFreeZone.md` §7.3 and in `docs/HOTKEYS.md`.
6. Build: `dotnet build mods/<modName>/<modName>.csproj -c Release`.
   Add `-p:IFZGameDir="<game folder>"` if the game is not in the default Steam folder.
   The build deletes the old version and copies `IFZ-<modName>-v<version>.dll`
   to the game (`BepInEx/plugins/IFZ-Compilation/`) and to the repository `plugins/`.
7. Add the mod to `README.md` and a line to `FEATURES_AND_VERSIONS.md`.

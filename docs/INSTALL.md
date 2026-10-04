# Install guide

> **File version:** 1.0.0 · **Last edit:** 2026-10-04 01:00 UTC

This guide installs the mod loader (BepInEx) and the IFZ Compilation mods.
It works for Windows, Linux, and Steam Deck (Proton).

## 1. Find the game folder

Steam → right-click **Infection Free Zone** → **Manage** → **Browse local files**.
The folder holds `Infection Free Zone.exe`. All steps below use this folder.

## 2. Install BepInEx (one time only)

1. Download **BepInEx 5.4.23.2 for Windows x64**:
   <https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.2/BepInEx_win_x64_5.4.23.2.zip>
   Use this Windows file on Linux and Steam Deck too. The game runs as a Windows program in Proton.
2. Extract the zip into the game folder.
   `winhttp.dll` and `doorstop_config.ini` must be next to `Infection Free Zone.exe`.
3. **Linux and Steam Deck only:** Steam → right-click the game → **Properties** →
   **Launch Options**. Paste this line:

   ```
   WINEDLLOVERRIDES="winhttp=n,b" %command%
   ```

   Without this line, the mods do not load.
4. Start the game once, then close it. BepInEx creates `BepInEx/config` and
   `BepInEx/LogOutput.log`.

## 3. Install the mods

### Option A — the full pack

1. Open the [Releases](https://github.com/kurzonmorris/IFZ-Infection-free-zone-mods-compilation/releases) page. Download the newest
   `IFZ-Compilation-pack-v<version>.zip`.
2. Extract it into the game folder. Choose **Replace** if asked.
   The mods go to `BepInEx/plugins/IFZ-Compilation/`.

### Option B — one mod

1. Open the [`plugins/`](../plugins) folder. Click the DLL you want → **Download raw file**.
2. Put the DLL in `BepInEx/plugins/IFZ-Compilation/`. Create the folder if it does not exist.

## 4. Update a mod

Each DLL name holds its version, for example `IFZ-darkerNights-v1.2.0.dll`.
Delete the old version of the same mod before you add the new one.

## 5. Settings

Each mod makes a settings file `BepInEx/config/kurzon.ifz.<modName>.cfg` on the first start.
Edit it with a text editor while the game is closed.

For an in-game settings window, install
[BepInEx ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases)
(BepInEx 5 version) into `BepInEx/plugins/`. Its default key is **F1**, which is also the
game's speed key. Change it to **F10** in
`BepInEx/config/com.bepis.bepinex.configurationmanager.cfg`.

Every mod has `General / Enabled`. Set it to `false` to turn the mod off without deleting it.

## 6. Uninstall

- One mod: delete its DLL from `BepInEx/plugins/IFZ-Compilation/`.
- All our mods: delete the `BepInEx/plugins/IFZ-Compilation/` folder.
- Everything: delete `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, and the
  `BepInEx` folder. On Linux, also clear the launch option.

## 7. Problems

| Problem | Fix |
|---------|-----|
| No `BepInEx/LogOutput.log` after a start | BepInEx did not load. Windows: check that `winhttp.dll` is next to the exe. Windows 11: Smart App Control can block it; turn Smart App Control off. Linux/Deck: add the launch option in step 2.3. |
| A mod does nothing | Open `BepInEx/LogOutput.log` and search for the mod name. On Linux, also read `Player.log` in the Proton prefix. |
| The game crashes after you add a mod | Remove the mods one at a time to find the cause. Report it with the log. |

Logs:
- `BepInEx/LogOutput.log` in the game folder.
- Windows: `%USERPROFILE%\AppData\LocalLow\JutsuGames\Infection Free Zone\Player.log`
- Linux/Deck: `<steamapps>/compatdata/1465460/pfx/drive_c/users/steamuser/AppData/LocalLow/JutsuGames/Infection Free Zone/Player.log`

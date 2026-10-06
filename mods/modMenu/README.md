# modMenu

> **File version:** 1.0.0 · **Last edit:** 2026-10-06 11:35 UTC

Mod version **0.1.0** · DLL `IFZ-modMenu-v0.1.0.dll` · GUID `kurzon.ifz.modMenu` · No hotkey (a button)

A **Mods** button in the bottom-right corner of the screen. Click it to open a list of every mod window it can find, then click an entry to open that window.

## How it finds mod windows

- Every 30 seconds, and each time the menu opens, it reads the settings of every loaded BepInEx mod.
- A setting counts as a window key when it is a key (`KeyboardShortcut` or `KeyCode`), is not None, and its section, name or description contains: toggle, window, menu, open, show, panel, dashboard, gui or ui. The rule is in `MenuRules.LooksLikeWindow`.
- If a mod has exactly one such key and a public `ToggleWindow()` method, the menu calls that method (our own mods have it). Otherwise it makes the mods see the key pressed for one frame (Harmony postfixes on `Input.GetKeyDown(KeyCode)` and `Input.GetKey(KeyCode)`).
- Mods whose keys are written in their code, not in settings, can be added by hand: `Menu / ExtraEntries`, for example `Mod Panels=F7;Other=Ctrl+F8`.

## Settings

`General / Enabled` · `Button / OffsetX`, `OffsetY` (pixels from the bottom-right corner) · `Button / MenuOpacity` · `Menu / ExtraEntries`.

## Limits

- Feature toggles that use words like "toggle" are listed too, for example IFZ 24/7 Workers. Clicking one switches that feature.
- The game itself reads keys through Rewired, so a faked key does not trigger the game's own actions.
- The button shows only in a loaded game (an HQ exists).

## Tests

`MenuRules.LooksLikeWindow` tested outside the game: 7 checks, all passed on 2026-10-06.

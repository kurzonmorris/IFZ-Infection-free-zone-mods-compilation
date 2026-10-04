# IFZ Compilation — Infection Free Zone mods

> **File version:** 1.0.0 · **Last edit:** 2026-10-04 01:00 UTC

A set of BepInEx mods for **Infection Free Zone** that are built to work together.
Each mod is a separate DLL, so you install only what you want.

**Platforms:** Windows · Linux (Steam Proton) · Steam Deck

---

## What this is

- Mods that **add features** to improve play.
- Mods that **add challenge** for players who want a harder game.
- Mods that **improve visuals** and how the game works.
- Every mod uses unique hotkeys and settings, so all mods run together without conflict.

## What this is not

- **No cheats.** No free resources, god mode, infinite ammo, or similar.
- Not a total conversion. The game stays Infection Free Zone.
- Not a replacement for game files. The mods load through BepInEx and leave the game install unchanged.

---

## Download

| What | Where |
|------|-------|
| Full pack (all mods, one zip) | [Releases](https://github.com/kurzonmorris/IFZ-Infection-free-zone-mods-compilation/releases) |
| One mod | [`plugins/`](plugins) — one DLL per mod |
| Install guide | [docs/INSTALL.md](docs/INSTALL.md) |
| Hotkeys | [docs/HOTKEYS.md](docs/HOTKEYS.md) |
| Change history | [FEATURES_AND_VERSIONS.md](FEATURES_AND_VERSIONS.md) |

**Requires:** BepInEx 5.4.23.2 (Windows x64 build, also on Linux).
Linux and Steam Deck also need the Steam launch option `WINEDLLOVERRIDES="winhttp=n,b" %command%`.
See the [install guide](docs/INSTALL.md).

**File names:** `IFZ-<modName>-v<version>.dll`. The version changes with every update.

---

## Mods

| Mod | Version | Type | Summary |
|-----|---------|------|---------|
| _No mods released yet._ | | | |

<!--
Copy this block for each mod. Keep the heading = the mod name.

### modName — vX.Y.Z

Short one-line description.

<details>
<summary><b>Feature name</b> — one-line summary</summary>

- Detail.
- Settings: `Section / Key` (default).
- Hotkey: `Key`.

</details>
-->

---

## Planned areas

<details>
<summary><b>Gameplay</b> — features that improve normal play</summary>

- Better control of squads, workers, and buildings.
- Fewer repeated manual tasks.

</details>

<details>
<summary><b>Challenge</b> — harder, optional game modes</summary>

- Stronger or more frequent threats through the game's own systems.
- Every challenge can be turned off in its settings.

</details>

<details>
<summary><b>Visuals</b> — a better-looking game</summary>

- Lighting, night, and effect improvements.
- Each visual setting has a performance note.

</details>

<details>
<summary><b>Function</b> — fixes and quality of life</summary>

- Fixes for game bugs and rough edges.
- Clearer information on screen.

</details>

---

## For mod developers

- Source for each mod: [`mods/`](mods). Start a new mod from [`mods/_template/`](mods/_template).
- Engine notes and project rules: [Explained-game_infectionFreeZone.md](Explained-game_infectionFreeZone.md).
- Third-party mods kept for study: [`research/`](research). Nothing there ships.

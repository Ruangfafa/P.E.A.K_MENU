# P.E.A.K_MENU

A lightweight in-game utility menu for **PEAK**, created by **Ruangfafa**.

The menu currently includes item spawning, player teleportation, coordinate-based flight, status controls, themes, category icons, configurable hotkeys, and a movable/resizable interface.

> [!WARNING]
> This mod changes gameplay and may affect multiplayer sessions. Use it only in private or consenting lobbies.

## Features

### Item Spawner

- Browse available PEAK items
- Display native item icons
- Spawn selected items through PEAK's network flow
- Scrollable item list

### Teleport

- Teleport yourself to another player
- Bring another player to your position
- Refresh player objects before every teleport action
- Prevent access to the teleport page when no other players are present
- Re-resolve the selected player before teleporting to reduce stale-object errors

### Flight

- Coordinate-based flight using PEAK's warp RPC
- Optional double-tap `Space` activation mode
- Adjustable flight speed
- Default flight speed: `16`
- Optional mouse-wheel speed control
- Mouse wheel changes speed by `5`
- Optional slow-fall effect after flight synchronization
- Automatically enables and locks invincibility and anti-knockback while flight mode is enabled
- Restores the previous protection settings when flight mode is disabled

### Status

- Invincibility
- Anti-knockback
- Infinite stamina
- Native PEAK status application
- Configurable status amount and duration
- Set or additive application modes
- Clear negative effects

### Menu

- Movable window
- Resizable window
- Multiple color themes
- Category icons
- Configurable menu hotkey
- Input blocking while the menu is open

## Default Controls

| Control | Action |
|---|---|
| `F6` | Open or close the menu |
| Double-tap `Space` | Toggle active flight when enabled |
| `W` / `A` / `S` / `D` | Fly horizontally |
| `Space` | Ascend |
| `Ctrl` | Descend |
| `Shift` | Accelerate |
| Mouse wheel up | Increase flight speed by 5 |
| Mouse wheel down | Decrease flight speed by 5 |

## Installation

### Mod Manager

Install the package using Thunderstore Mod Manager, r2modman, or Gale.

Required dependencies should be installed automatically through the Thunderstore package manifest.

### Manual Installation

1. Install BepInExPack for PEAK.
2. Copy `ruangfafa.peakmenu.dll` into:

   `PEAK/BepInEx/plugins/P.E.A.K_MENU/`

3. Launch PEAK.
4. Press `F6` to open the menu.

## Development Setup

Create a copy of:

```text
Config.Build.user.props.template
```

Rename it to:

```text
Config.Build.user.props
```

Then configure the local PEAK installation path and BepInEx plugin output path.

This will:

- Resolve PEAK and Unity assembly references
- Copy the compiled plugin assembly to `BepInEx/plugins/`
- Simplify local testing after each build

Search for `TODO` across the project to find any remaining template-specific values that should be configured or replaced.

## Building

Build the debug configuration:

```sh
dotnet build
```

Build the release configuration:

```sh
dotnet build -c Release -v d
```

The compiled plugin assembly is produced under:

```text
./artifacts/bin/P.E.A.K_MENU/
```

## Thunderstore Packaging & Publishing

This project uses [ThunderPipe](https://github.com/WarperSan/ThunderPipe) for Thunderstore packaging.

Build a local Thunderstore package with:

```sh
dotnet build -c Release -v d
```

The generated package will be placed under:

```text
./artifacts/thunderstore/
```

> [!NOTE]
> Review the generated package before publishing. Confirm that the manifest, icon, README, dependencies, version number, and plugin DLL are correct.

To publish directly to Thunderstore, configure the required publishing values in `Config.Build.user.props`, then run:

```sh
dotnet build -c Release -p:PublishTS=true -v d
```

> [!TIP]
> Always inspect and test the local package in `./artifacts/thunderstore/` before using `PublishTS=true`.

## Multiplayer Notes

Some features use PEAK network methods and RPC calls.

- Item spawning may affect the whole lobby
- Bringing another player may depend on network authority
- Flight sends repeated position synchronization RPCs while active
- Host permissions and game updates may affect behavior

Use the mod responsibly and only where all players agree.

## Known Limitations

- PEAK updates may change internal classes, fields, methods, or RPC behavior
- Some status visual and audio effects depend on PEAK's internal implementation
- Flight smoothness may vary with latency and RPC rate
- Features that depend on network authority may behave differently for host and clients

## Project Structure

```text
src/P.E.A.K_MENU/
├─ Features/
│  ├─ Flight/
│  ├─ ItemSpawn/
│  ├─ Status/
│  └─ Teleport/
├─ Input/
├─ UI/
├─ Plugin.cs
└─ P.E.A.K_MENU.csproj
```

## License

Add your chosen license before public release.

## Credits

Created by **Ruangfafa**.

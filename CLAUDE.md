# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Bertahan** is a third-person 3D zombie-survival game set in an Indonesian village. The player picks a family member (bapak, ibu, kakak, ade, kake, nene) and fights zombies from Indonesian folklore (warga, tuyul, pocong, kuntilanak, satpam, genderuwo, dukun) with improvised weapons (linggis, kampak, pacul, sapu, wajan, bambu, pentungan, senapan, molotov). The full spec is in `requirements.md.txt` (Indonesian). Hard requirements from it:
- Stack: .NET 10, Avalonia, and ThreeNet (a wgpu-based 3D library). All 3D assets are made in Blender through the Blender MCP server, using the reference art in `art/`.
- Flow: opening story animation → menu over a live 3D village backdrop → New Game (name, default "Si Otong") → difficulty (Bayi / Pemberani / Mimpi Buruk) → character select → play.
- Levels 1–4 are playable, and 5–10 show as "to be released". A level can be retried at most 3 times after the player runs out of lives, then the game restarts from level 1.
- Top Score is tracked per level. There is an Options screen (sound, music, graphics) and an About screen with scrolling credits that must include "Dibuat oleh Ariana Mischa Fadhila dari Subadubin Studios".

All in-game text, and identifiers such as asset ids, zones and time of day, are in Indonesian. Keep them that way.

## Commands

```bash
# Build / run the game
dotnet build src/Bertahan/Bertahan.csproj
dotnet run --project src/Bertahan

# Headless smoke test: renders N frames (default 40), writes <out>.png (3D frame + UI overlay)
# and <out>.txt (load/render log), then exits
src/Bertahan/bin/Debug/net10.0/Bertahan.exe out.png [frames]

# Regenerate all procedural audio (WAV) into src/Bertahan/Assets/Audio
dotnet run --project tools/AudioGen [projectRoot]

# Rebuild all 3D models into src/Bertahan/Assets/Models (or exec the file through the Blender MCP)
blender -b --python blender/build_all.py
```

There is no test project, solution file or linter. Verify a change by building it and running the headless screenshot mode.

Force a GPU backend with the `WGPU_BACKEND=dx12|vulkan` environment variable. `Core/GpuBackend.cs` otherwise probes DX12 and falls back to Vulkan, because some AMD GPUs render black through DX12 offscreen targets. The probe result is cached in the settings file.

## Architecture

### Current state
`MainWindow.axaml.cs` is still a **temporary asset test harness**: it loads the six character GLBs and plays their clips. Nothing constructs the game systems in `Game/` or `Core/` yet: `GameSession`, `MenuStage`, `AudioManager` and `GameSettings.Load`/`GpuBackend.Select` all still need wiring from the window. The menus, HUD, opening and credits UI are not written yet.

### Asset pipelines (generated, not hand-made)
Every runtime asset is produced by code. Edit the generator and regenerate; never hand-edit the outputs.
- **`blender/`**: Python `bpy` scripts. `btk.py` is the shared toolkit (primitives, materials, armatures, skinning, `export_glb`). `characters.py`, `zombies.py`, `weapons.py` and `props.py` build models. `anims.py` and `zanims.py` author the player and zombie animation clips. `build_all.py` reloads the modules and rebuilds everything. `ui_renders.py` (`render_all()`) renders the PNG portraits and icons into `Assets/UI`; `build_all.py` does not call it. Blender conventions: Z up, characters face −Y, the character's left is +X, and bones are suffixed `_L`/`_R`. The glTF export converts this to Y up, facing +Z, which the game expects.
- **`tools/AudioGen/`**: a console app that synthesizes every SFX and music loop from DSP code (`Dsp.cs`, `Instruments.cs`, `Sfx.cs`, `Music.cs`). The job list in `Program.cs` sets the WAV file names.
- `src/Bertahan/Assets/**` is copied to the output directory (`PreserveNewest`). At runtime, assets load by name from `AppContext.BaseDirectory/Assets/...`.

### Naming contracts between the pipelines and the game
These names are only matched as strings, so renaming one side breaks things silently:
- Models are `char_<id>.glb`, `zombie_<id>.glb`, `weapon_<id>.glb` and `prop_<name>.glb`. The ids match the `Id` fields in `Game/Defs.cs`.
- Character GLBs include every weapon mesh as a child node named `W_<weaponId>`. `AnimatedModel.ShowWeapon` toggles their visibility.
- Clip names are fixed. The base (looping) layer uses `idle, walk, run, spawn, dodge, die, cheer`, and the action layer uses `aim, swing, thrust, attack, cast, throw, shoot, hit` (see `AnimatedModel.BaseOrder`/`ActionOrder`). `AnimatedModel` plays every clip at once and blends between them by weight.
- Audio ids are the WAV file names (`sfx_*`, `music_*`, `jingle_*`). `AudioManager` loads everything in `Assets/Audio`, and `LevelDef.Music` refers to music by id.

### Game runtime (`src/Bertahan/Game`, namespace `Bertahan.Game`)
- `Defs.cs` holds the data tables: `WeaponDef`, `CharacterDef`, `ZombieDef` and `LevelDef` (each level has its waves of `SpawnGroup`s, and the last wave is `Boss: true`). Game balancing happens here.
- `GameSession` is one playthrough of one level. It owns its own ThreeNet `Scene` and builds the `Level` (via `LevelBuilder` + `GroundPainter`), `Player`, zombie object pools, `Combat`, `Pickups`, `Particles`, `WaveDirector` and `CameraRig`. Its state is `Playing`/`Won`/`Lost`.
- `MenuStage` is a separate `Scene` used as the menu and character-select backdrop.
- `Navigation` does 2D ground-plane collision (box/circle `Obstacle`s) and a flow field toward the player that zombies follow. `LevelBuilder` registers an obstacle for every prop it places, then calls `Nav.Bake()`.
- `PropLibrary` caches loaded GLB prototypes and instances them. `AnimatedModel` caches GLB bytes per file.

### Core (`src/Bertahan/Core`)
- `GameSettings` is JSON (source-generated `SettingsJson` context) saved at `%APPDATA%/Bertahan/settings.json`. It holds volumes, quality, backend, unlocked level, best scores and last character.
- `AudioManager` wraps the ThreeNet `AudioEngine`: music crossfade, 3D positional one-shots and loops, and per-sound rate limiting. It degrades gracefully when no audio device exists (`Available`/`Unavailable`).
- `Screenshot` reads back the renderer's pixels, forces alpha opaque and optionally composites the Avalonia UI on top.

### ThreeNet usage notes
- `ThreeNetView` (from `ThreeNet.Avalonia`) is the viewport control. Set `Scene`, `Camera` and `RendererOptions` (`BgraOutput = true` is required), and drive per-frame logic from the `Frame` event, calling `scene.UpdateAnimations(dt)`.
- `scene.LoadGltf` appends clips to `scene.Animations`. To get a model's own clips, record the count before loading and take the entries after it.

## Blender MCP
`.mcp.json` configures the `blender` MCP server, which needs a running Blender 5.2 with the MCP add-on on localhost:9876. When generating assets through it, exec the scripts in `blender/` (for example, run `build_all.py` or call a module's `build(...)`) rather than issuing ad-hoc `bpy` code, so the scripts remain the source of truth.

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Bertahan** is a third-person 3D zombie-survival game set in an Indonesian village (.NET 10 + Avalonia 12 + ThreeNet 0.6). The player picks a family member (bapak, ibu, kakak, ade, kake, nene) and fights zombies from Indonesian folklore (warga, tuyul, pocong, satpam, kuntilanak, genderuwo, dukun) with improvised weapons. The spec is `requirements.md.txt` (Indonesian). Hard requirements:
- All 3D assets are made in Blender (via the Blender MCP server) from the reference art in `art/`.
- Flow: opening story → menu over a living 3D village → New Game (name, default "Si Otong") → difficulty (Bayi / Pemberani / Mimpi Buruk) → character select → play.
- Levels 1–4 are playable and 5–10 show as "segera hadir". After running out of lives a level can be retried 3 times; after that the run restarts at level 1.
- Top Score per level, an Options screen, and an About screen with scrolling credits that must include "Dibuat oleh Ariana Mischa Fadhila dari Subadubin Studios".

All in-game text and data ids (levels `gerbang/sawah/pasar/kuburan`, `TimeOfDay.Siang/Sore/Malam`, asset ids) are Indonesian. Keep them that way. User-facing docs live in `README.md` and `docs/` (Indonesian).

## Commands

```bash
dotnet build src/Bertahan/Bertahan.csproj
dotnet run --project src/Bertahan

# Headless screenshot run: renders, writes out.png + out.png.txt (state summary), exits
src/Bertahan/bin/Debug/net10.0/Bertahan.exe --shot out.png --scene <title|village|name|difficulty|chars|map|loading|scores|options|controls|about|ending|opening|level> [--level N] [--warp S] [--autoplay] [--overlay pause|result|preview] [--time S] [--cam x,y,z,tx,ty,tz] [--char id] [--difficulty Bayi] [--frames N]

# Regenerate procedural audio (WAV) into src/Bertahan/Assets/Audio
dotnet run --project tools/AudioGen

# Rebuild 3D models into src/Bertahan/Assets/Models (headless; or exec the same scripts via Blender MCP)
blender -b --python blender/build_all.py
```

There are no tests. Verify changes by building and running screenshot mode. `--autoplay` drives a bot (`Game/Autopilot.cs`); `--warp` fast-forwards simulation before capture. Set `BERTAHAN_SETTINGS=<file>` so test runs don't touch the real `%APPDATA%/Bertahan/settings.json` (settings, Top Scores, saved run). `WGPU_BACKEND=dx12|vulkan` forces the GPU backend.

## Architecture

### Shell (`MainWindow.axaml.cs`)
One window: a `ThreeNetView` at the bottom, the `Hud` control over it, and a `Viewbox` (1280x720 design size) that hosts a stack of `UI/Screen`s. `ShellMode` is Boot → Opening | Menu → Loading → Playing. It owns `GameSettings`, `AudioManager`, the current `Campaign` (run) and `GameSession`, plus input (`Core/InputMap`) and level-end handling (`LevelFinished`: records the Top Score, then `Campaign.LevelWon/LevelLost`). UI is built in C# (not XAML) on `UI/Kit` (colours, panels, text) and `UI/MenuButton`. Screens implement `IShell`-driven navigation (`Show/Replace/Back`) and pick the menu backdrop via `StageView`.

### Scenes
Each stage owns its own `Scene`: `OpeningStage` (scripted story shots), `MenuStage` (village tour and family line-up), and `GameSession` (a level). **Every scene change recreates the `ThreeNetView`** (`SetScene` → `NewView`), because ThreeNet's renderer caches GPU resources by handle and mixes up meshes and materials across scenes.

### Gameplay (`Game/`)
- Data tables are in `Defs.cs` (weapons, characters, zombies, levels and waves) and `Difficulty.cs`.
- `GameSession` owns the level (`LevelBuilder`), player, zombie pools, `Combat`, `Pickups`, `WaveDirector`, `Particles`, `Atmosphere` and `Fauna`. It handles lives and respawn, and the occluder cut-away (houses and trees between the camera and the player are hidden).
- `Navigation` does 2D collision plus a flow field toward the player.
- `AnimatedModel` plays every clip at once and blends them by weight.
- `Atmosphere` covers:
  - the camera-following sky dome (a procedurally painted texture with sun/moon, stars and mountains) and a cloud dome;
  - cloud shadows, layered mist, and GPU rain (a GLSL `user_vertex` shader hook);
  - lightning, fireflies, dust;
  - wind sway of plant props (`Swayer`s registered in `LevelBuilder.Prop` / `VillageLife.P`).
- `Fauna` holds `CritterDef` (animals and villagers). They wander, perform their action clip, make sounds, and flee from threats (active zombie positions). Birds fly off and land again later.
- `VillageLife` holds the menu and opening villagers with chores, the intruding zombies, and a `Fauna`.

### ThreeNet gotchas (learned the hard way)
- Default linear fog (`FogStart/FogEnd` = 10–100 m) applies on top of `FogDensity`; every scene sets `FogStart = 1000, FogEnd = 5000`.
- DX12 + MSAA renders black on some AMD GPUs at ≥256 px. `Core/GpuBackend` probes a 256 px MSAA frame and falls back to Vulkan.
- `Geometry.ComputeTangents()` without a normal map made the ground vanish. Large far planes should be Lambert, not PBR.
- `Node.Visible` has no getter; `Node.Light` is `Light?` (use `.Value with {}`); imported GLB materials can't be read back from nodes (so wind sways the model node, not vertices).
- The view only starts rendering once it has a `Scene` (the shell sets an empty boot scene).
- Shaders: `Scene.CreateShader(source, ShaderLanguage.Glsl, name)`.

## Asset pipelines (generated — edit the generator, never the output)
- `blender/`: `btk.py` is the shared toolkit (primitives rigidly bound to bones, armatures, the `Clip` keyframe DSL, `export_glb`, `preview`). The other scripts:
  - `characters.py` (family; proportions follow `art/players.png`), `villagers.py` (9 professions), `animals.py` (ayam, sapi, kambing, kucing, anjing, ular, burung);
  - `zombies.py`, `weapons.py`, `props.py`;
  - `anims.py` and `zanims.py` (animation clips);
  - `ui_renders.py` (portraits, cards and icons into `Assets/UI`; not called by `build_all.py`).

  Blender conventions: Z up, characters face −Y, their left is +X, bones `_L/_R`. The glTF export gives Y up, facing +Z.
- `tools/AudioGen/`: `Sfx.cs` (includes animal sounds and villager screams) and `Music.cs`. The job list in `Program.cs` names the WAV files.
- `Assets/UI/scene_level_N.png` are in-engine renders (`--scene level --autoplay --overlay preview`) used by the loading screen and the map.

### Naming contracts (matched only as strings)
- Model files: `char_<id>`, `npc_<id>`, `animal_<id>`, `zombie_<id>`, `weapon_<id>`, `prop_<name>`.glb. Ids match `Defs.cs` and `CritterDef.All`.
- Human GLBs carry every weapon as a node `W_<weaponId>`; `AnimatedModel.ShowWeapon` shows one of them (or none).
- Clip names:
  - base: `idle, walk, run, spawn, dodge, die, cheer`
  - actions: `aim, swing, thrust, attack, cast, throw, shoot, hit`

  Animals use `idle/walk/run/die` plus `attack` as their action (peck, graze, bark).
- Audio ids are the WAV names (`sfx_*`, `music_*`, `jingle_*`).
- Wind-sway plants are recognised by prop-name prefix in `Atmosphere.Plants`.

## Blender MCP
`.mcp.json` configures the `blender` server. In Blender 5.2 the **Blender MCP** add-on must be enabled and its server started on `localhost:9876`. Run the repo scripts through `execute_blender_code` (`sys.path.insert(0, <repo>/blender)`, import, `importlib.reload`, call `build(...)`) rather than ad-hoc `bpy` code, so the scripts remain the source of truth. Builders call `btk.clear_scene()`, which wipes the open Blender scene.

# TowerDefensive-FinalProyect

Unity 6 tower-defense game (3D, URP). No CI or README yet; Unity EditMode tests live under `Assets/_Custom/Tests/Editor/`.

## Stack (verified in ProjectSettings/Packages)
- Unity **6000.3.19f1**, URP **17.3.0** (Gamma color space), ProBuilder 6.1.2 (level geometry), Recorder 5.1.7
- **New Input System only** (`activeInputHandler: 1`): legacy `UnityEngine.Input` is disabled — use `UnityEngine.InputSystem` / PlayerInput components
- MCPForUnity (`com.coplaydev.unity-mcp`, git package) is installed: the unityMCP tools drive the editor, but only work while the Unity Editor is open with the bridge running
- No `.asmdef` files — default compilation

## Layout
- All game content lives in `Assets/_Custom/` (`Scenes/`, `Scripts/`, `Prefabs/`, `Models/`, `Materials/`); URP pipeline assets live in `Assets/URP/`. Keep new content inside `_Custom/`, never at Assets root.
- Only scene: `Assets/_Custom/Scenes/0_Main.unity` (also the only scene in Build Settings). Contains Ground, Main Camera, Directional Light, Global Volume and the persistent `Board` object.
- `Assets/_Custom/Scripts/` contains the board model/view and input prototype; `Assets/_Custom/Materials/` contains `CellMaterial.mat`. `Prefabs/` and `Models/` remain empty.

## Gotchas
- No CLI build/test/lint — verification happens in the Unity Editor: watch the Console for compile errors after script changes, then test in Play mode.
- `.meta` files are versioned (see .gitignore) and must never be deleted or recreated — they hold the GUIDs that scene/prefab references depend on. Create/move/rename assets only through the Unity editor (or MCP), never by hand.
- Script class name must match its file name; use URP-compatible shaders (e.g. `URP/Lit`), not Built-in RP shaders.
- Don't hand-edit scene/prefab `.unity`/`.prefab` YAML when an editor/MCP operation (or a C# script) can do the job.
- `BoardManager` uses `ExecuteAlways`: its editable scene children live under `Board/Cells` (16 cells) and `Board/Towers`. Use its custom Inspector to change levels and rebuild the view; do not recreate those children manually.

## Code style
- Comments in **Spanish**, always single-line with `//`. Never use `/// <summary>` XML docs.
- Write code readable by junior developers: descriptive names, short methods, obvious flow, no fancy patterns.
- Tests live in `Assets/_Custom/Tests/Editor/` (no asmdef needed for EditMode); in tests, build boards from readable strings (e.g. `"1,1,0,0|0,0,0,0|..."`) instead of `int[,]` literals — same readability, no compiler quirks.

## Unity/MCP gotchas (verified)
- `execute_code` compiles with CodeDom (C# 6) which **mangles multi-dimensional array literals** (it transposes `int[,] { { ... } }` initializers). In snippets, build arrays programmatically (cell by cell); never trust a snippet that passes `int[,]` literals.
- After creating scripts, wait for compilation + domain reload to finish before running `run_tests`: running too early fails initialization or returns stale results (the editor may still be running the previous assembly). If test results contradict the source on disk, force a clean recompile (refresh with compile request) and re-verify.
- If compiled behavior differs from the `.cs` on disk, inspect the compiled assembly before touching the logic: fixture literal data ends up in `<PrivateImplementationDetails>` static fields and member calls can be resolved via `Module.ResolveMethod`/`ResolveField` on the IL tokens.

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Spacebox is a voxel space-survival game (asteroid belt, mining, base/ship building, procedural generation, data-driven modding) built on a custom engine over OpenTK (OpenGL 3.3 core, forward-compatible context). All projects target .NET 8.

## Coding rules

- Do not add any comments to code (no `//`, `/* */` or XML doc comments) in new or modified code.
- Never delete or rewrite existing comments, including commented-out code; leave them exactly as they are, even when editing the surrounding code.
- Write clean, extendable code following best practices and SOLID principles: small classes with a single responsibility, extension through new types rather than editing existing switch/if chains, depending on abstractions where it fits the existing design, and no duplicated logic.
- Match the style of the surrounding code (naming, namespaces, structure).
- In new code, private fields are `_camelCase` and new files use file-scoped namespaces (`namespace X;`). Do not rename or reformat existing code unless asked.
- Log with `Debug.Log` / `Debug.Warning` / `Debug.Error` / `Debug.Success`, never `Console.WriteLine`, in game and engine code.
- After code changes, run `dotnet build Spacebox/Spacebox.csproj`, then run both test projects after every change to `Engine` or the game: `dotnet test Engine.Tests` and `dotnet test Spacebox.Tests`. A task is not done while the build or any test fails.
- Tests: engine-only tests go to `Engine.Tests` (references only `Engine`), game tests to `Spacebox.Tests`. Add tests for new logic that can run without an OpenGL context (math, serialization, data rules, inventory). A project hook (`.claude/hooks/build-check.sh`) also builds after every `.cs` edit and reports errors.

## Project rules

- **Threading:** OpenGL calls only on the main thread. Run background work through `WorkerPoolManager.Enqueue` and hand results back with `MainThreadDispatcher` (game side, `Spacebox/Game/Generation/Structures/`); engine code uses `EngineWindow`'s main-thread queue, and GL disposal from workers goes through `DisposalManager`.
- **Hot paths** (meshing, generation, lighting, `Update`/`Render`, `Block`): no LINQ, no per-frame allocations; reuse pools and caches. `Block` packs its state into bits of one `long`, so don't add fields to it carelessly.
- **Engine boundary:** `Engine` must never reference `Spacebox.*`. Reusable, game-independent features belong in `Engine`.
- **Save and content compatibility:** new fields in NBT saves or GameSet JSON need defaults so old data still loads. Renamed or removed block/item ids and changed NBT keys need a world upgrade step (`Spacebox/Game/Versioning/Steps/`, one file per step, registered in `WorldUpgradeRegistry.CreateDefault`) written with the `WorldUpgradeContext` operations (`RenameBlock`, `RenameItem`, `RemoveBlock`, `RemoveItem`, `EditChunks`, `EditPlayers`, …), plus a test in `Spacebox.Tests/Versioning/` that builds the old data with `WorldFixtureBuilder` and checks it with `WorldInspector`. Worlds older than `WorldUpgradeRegistry.DefaultMinimumSupportedVersion` (0.1.2) are refused. Never break the `Default` GameSet.
- **Game version bumps:** when `Application.Version` changes, add a step from the previous version to the new one in the same change (`VersionBumpStep` if no files change) and a test for it; `WorldUpgraderTests` fails until the chain reaches the new version. Before a release, copy a small played world saved with that version into `Spacebox.Tests/Fixtures/Worlds/<version>/`; `SavedWorldFixtureTests` upgrades every fixture to the current version and fails on unknown ids. See the `release` skill.
- **Network:** the game is client, host and dedicated server in one executable (`Engine/Networking` + `Spacebox/Network`, see `spec.md` §8). The server is authoritative; sync new state with `NetworkVariable<T>`, `[Rpc]` methods on `NetworkBehaviour`s or `INetMessage` structs registered on `NetworkManager.Messages`.
- **Do not edit** `Spacebox/Build/**` or the vendored `Documentation/doxygen-awesome-css/**`.
- **Git:** feature branches are named `<feature>-<version>` (e.g. `generation-0.1.2`). Do not commit or push unless asked. Commit messages are short and imperative.
- `Spacebox/TODO.txt` and `BUGS.txt` belong to the author; edit them only when asked.
- After every game change, add a very short line to `Spacebox/DONE.txt` in the section of the current version (`Application.Version` in `Engine/Core/Application.cs`), under `Game`, `Small`, `Fixes` or `Engine`. Create the version section in the same format if it is missing.
- Keep `spec.md` (full technical description) up to date when architecture, file formats or limits change.
- **Tutorials stay in sync with the game:** the tutorials in `Spacebox/Mods/GameSets/*/Tutorials/*.json` (see `tutorial-design.md`) teach real mechanics. Whenever a change touches something a tutorial step relies on, check the tutorials and fix steps, texts, order and rewards in the same change. This includes item or block ids, crafting and processing recipes, drill tiers and ore hardness, start items, input actions and default keys, UI window names, `PlayerStatistics` fields, `PlayerActivity` events, and ship, console or machine behaviour. Rewards must never be items that a tutorial task asks for. A new feature a beginner must know gets a tutorial step. `DefaultTutorialContentTests` and `DefaultTutorialProgressionTests` must stay green.

Project skills in `.claude/skills/` cover repeated tasks: `add-block`, `add-item`, `add-network-message`, `release`, `play-game`.

**Testing in the running game:** use the `play-game` skill (`Tools/agent/game.sh start`, then `state`, `ui`, `click`, `goto`, `cmd`, `shot`, …) instead of asking the user to try things. It drives the Release build through a localhost HTTP API (`--agent`, `--agent-world <name>`; compiled into Debug/Release/Profile, never Final).

## Commands

```bash
dotnet build Spacebox.sln                       # build everything
dotnet run --project Spacebox/Spacebox.csproj   # run the game (Debug)
dotnet run --project Spacebox/Spacebox.csproj -- --editor Default Blocks   # open the GameSet editor (optional start panel)
dotnet run --project Spacebox/Spacebox.csproj -- --server   # run the dedicated server
Tools/agent/game.sh start   # build Release, launch with the agent API and load a test world (see play-game skill)

dotnet test Engine.Tests                                            # engine xUnit tests
dotnet test Spacebox.Tests                                          # game xUnit tests
dotnet test Spacebox.Tests --filter "FullyQualifiedName~StorageTests" # single class
dotnet test Spacebox.Tests --filter "FullyQualifiedName~BlockTests.SomeMethod"

dotnet run -c Debug --project Spacebox.Benchmarks   # BenchmarkDotNet; edit Program.cs to choose which benchmark runs
dotnet run --project Tools/Spacebox.AssetAudit -- audit . --list   # list assets that release builds leave out as unused
dotnet run --project Tools/Spacebox.GameSetTool -- convert Spacebox/Mods/GameSets/Default   # flat GameSet JSON -> folder format (verified identical); also export-flat, snapshot, check

dotnet publish Spacebox/Spacebox.csproj -c Release -r linux-x64 --self-contained true --output ./publish   # also win-x64 / osx-x64; single-file: only the executable + game folders
Spacebox/Build/BuildWindows.ps1 ; Spacebox/Build/BuildLinux.ps1   # Final release builds: files directly in Final/Windows and Final/Linux (repo root) plus a zip next to them; SPACEBOX_NO_EXPLORER=1 skips opening Explorer
Spacebox/Build/BuildMacOS.ps1   # Final macOS builds (osx-x64, osx-arm64) into Final/MacOS-Intel and Final/MacOS-AppleSilicon plus zips; built on Windows, untested on a Mac
```

- `Engine.Tests`, `Spacebox.Tests` and `Spacebox.Benchmarks` are marked non-buildable in `Release`; use `Debug` for them.
- `Debug`/`Profile` define `PROFILING` and `ASSERTS` (also via `Directory.Build.props`); `Release` defines only `TRACE`; `Release` and `Final` build the game as `WinExe` (no terminal window; `--server` attaches or opens its own console via `TerminalConsole`).
- `Spacebox/Build/AddAfterBuild/OpenAL32.dll` (Windows audio) is bundled into Windows publishes by `Spacebox.csproj`; normal builds don't copy it.
- There is no linter/formatter configured.

## Solution layout

Detailed per-project notes: `Engine/CLAUDE.md` (engine architecture) and `Spacebox/CLAUDE.md` (game logic).

Dependency direction: `Spacebox` → `Engine`. `Engine` must not reference game code.

- **Engine/** — reusable engine: `EngineWindow` (abstract `GameWindow` with hook methods `OnRegisterScenes`, `OnGameLoad`, `OnSetupPostProcessing`, …), `SceneManagement` (`Scene` is a `Node3D`; scenes are registered then loaded via `SceneManager.Load<T>()`), `Node3D`/`Components` scene graph, `Resources.Load<T>(pathOrName, global)` resource cache, rendering (shaders, materials, post-processing, lights, particles), CanvasUI retained GUI (game, editor and debug tools), `InputPro` input actions, `Audio` (OpenAL + NVorbis), octrees, `ServiceLocator`, `EventBus`, `Debug` logging.
- **Spacebox/** — the game. `Program.cs` → `SpaceboxWindow` (subclasses `EngineWindow`, registers scenes, starts at `MenuScene`). `Scenes/` holds menu, single-player (`LocalSpaceScene` / `BaseSpaceScene`) and multiplayer scenes, `Game/` the game logic by feature, `Network/` multiplayer, `Editor/` the GameSet editor.

## World / voxel architecture (`Spacebox/Game/Generation/`)

Hierarchy: `World` (singleton `Component`, octree of sectors, loads/unloads sectors around the player) → `Sector` (8192³ blocks, holds `SpaceEntity` list plus lazily generated `NotGeneratedEntity` placeholders) → `SpaceEntity` (an asteroid/ship; grid of chunks) → `Chunk` (32³ `Block`s). All implement `ISpaceStructure`; these live in `Structures/`, with `WorldGenerator`, `BiomeGenerator` and `WorldPersistenceManager` (saving).

- Meshing: `MeshGenerator` + `AOVoxels` (ambient occlusion) build chunk meshes; neighbor-chunk lookups go through `MeshGenerator.GetNeighborChunkIndexAndLocalCoords`. Changing block state (e.g. emission/light) needs the chunk mesh to be regenerated.
- Block behaviour: `Block` base class with subclasses in `Blocks/` (`InteractiveBlock`, `ElectricalBlock`, `StorageBlock`, furnace/crusher/generator, …). `ElectricalManager` and `LightManager` handle power and light propagation.
- Stateless generation algorithms: `Spacebox/Game/Generation/Algorithms/` (noise, Perlin worms, cavities, ore placement, LOD meshing).
- Background generation work is marshalled back to the GL thread through `MainThreadDispatcher`; GL calls must stay on the main thread.

## Data-driven content (GameSets)

Game content is JSON, not code: `Spacebox/Mods/GameSets/<Name>/` (`Empty` is the read-only base every GameSet is built on with `"Extends": "empty"`, `Default` is the main set) uses the folder format: one folder per block (`Blocks/<group>/<id>/<id>.json`, `block.json` also works) and item (`Items/<group>/<id>/<id>.json`) with its textures, `Emissions/`, sounds, `blueprint.json` and `recipes.json`, plus `Blocks|Items/order.json`, `Recipes/<type>.json`, `Crafting/categories.json` and `config.json`. The old flat `blocks.json`/`items.json`/`crafting.json`/`recipes.json` still load (mods). Extension GameSets (`"Extends"`) add/patch content per world, texture packs (`Spacebox/Mods/TexturePacks/`) override textures globally; format and rules in `modding-guide.md` and spec.md §6.1. Loading lives in `Game/Resource/` (`Content/` stack, reader and asset index, `GameSetLoader`, `BlocksLoader`, `*JSON.cs` DTOs, `BlockFactory`, `GameAtlasFactory`, `GameSetsUnpacker` which extracts .zip/.rar/.7z/.tar(.gz/.bz2/.xz/.zst) GameSets, extensions and texture packs through `ModArchiveUnpacker` + an `IModArchiveRule` per kind). Tests read Default content through `Spacebox.Tests/DefaultGameSetContent` (never the flat files). Adding a block usually means an entry folder + texture; a new block *behaviour* means a `Block` subclass wired up by its `Type` in `BlockFactory`. `VersionConverter` / `WorldLoader` handle save-format upgrades.

## Notes

- Shaders are GLSL files under `Spacebox/Resources/Shaders/`, loaded by path without extension (e.g. `Resources.Load<Shader>("Resources/Shaders/PostProcessing/vignette", true)`).
- `Spacebox/TODO.txt`, `BUGS.txt` are the author's task tracking; `DONE.txt` is the changelog (see Project rules).
- API docs can be generated with Doxygen (`Doxyfile` at the root, theme in `Documentation/`).
- `ships-design.md`: design of player-built ships (movable entities, thrusters, consoles, cannons, camera blocks, boarding, collisions, multiplayer) and the planned work packages. Read it before working on movable entities or ship blocks.
- `tutorial-design.md`: the JSON tutorial system (format, task types, runner, UI) and the Default tutorial content. Read it before changing tutorials or adding task types.

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Spacebox is a voxel space-survival game (asteroid belt, mining, base/ship building, procedural generation, data-driven modding) built on a custom engine over OpenTK (OpenGL 3.3 core, forward-compatible context). All projects target .NET 8 (`SpaceServerUI` is WPF, `net8.0-windows`, Windows only).

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
- **Save and content compatibility:** new fields in NBT saves or GameSet JSON need defaults so old data still loads; breaking format changes need a `VersionConverter` step. Never break the `Default` GameSet.
- **Network:** the game is client, host and dedicated server in one executable (`Engine/Networking` + `Spacebox/Network`, see `spec.md` §8). The server is authoritative; sync new state with `NetworkVariable<T>`, `[Rpc]` methods on `NetworkBehaviour`s or `INetMessage` structs registered on `NetworkManager.Messages`. `SpaceNetwork`, `SpaceServer`, `SpaceServerUI` and `CommonLibrary` are deprecated: do not change them, the game no longer references them.
- **Do not edit** the prebuilt `Spacebox/Plugins/**/*.dll`, `Spacebox/Build/**` or the vendored `Documentation/doxygen-awesome-css/**`.
- **Git:** feature branches are named `<feature>-<version>` (e.g. `generation-0.1.2`). Do not commit or push unless asked. Commit messages are short and imperative.
- `Spacebox/TODO.txt` and `BUGS.txt` belong to the author; edit them only when asked.
- After every game change, add a very short line to `Spacebox/DONE.txt` in the section of the current version (`Application.Version` in `Engine/Application.cs`), under `Game`, `Small`, `Fixes` or `Engine`. Create the version section in the same format if it is missing.
- Keep `spec.md` (full technical description) up to date when architecture, file formats or limits change.

Project skills in `.claude/skills/` cover repeated tasks: `add-block`, `add-item`, `add-network-message`, `release`.

## Commands

```bash
dotnet build Spacebox.sln                       # build everything (SpaceServerUI fails off Windows; build projects individually there)
dotnet run --project Spacebox/Spacebox.csproj   # run the game (Debug)
dotnet run --project SpaceServer                # run the dedicated server

dotnet test Engine.Tests                                            # engine xUnit tests
dotnet test Spacebox.Tests                                          # game xUnit tests
dotnet test Spacebox.Tests --filter "FullyQualifiedName~StorageTests" # single class
dotnet test Spacebox.Tests --filter "FullyQualifiedName~BlockTests.SomeMethod"

dotnet run -c Debug --project Spacebox.Benchmarks   # BenchmarkDotNet; edit Program.cs to choose which benchmark runs

dotnet publish Spacebox/Spacebox.csproj -c Release -r linux-x64 --self-contained true --output ./publish   # also win-x64 / osx-x64; single-file: only the executable + game folders
```

- `Engine.Tests`, `Spacebox.Tests` and `Spacebox.Benchmarks` are marked non-buildable in `Release`; use `Debug` for them.
- `Debug`/`Profile` define `PROFILING` and `ASSERTS` (also via `Directory.Build.props`); `Release` defines only `TRACE` and builds the game as `WinExe`.
- `Spacebox/Build/AddAfterBuild/OpenAL32.dll` (Windows audio) is bundled into Windows publishes by `Spacebox.csproj`; normal builds don't copy it.
- There is no linter/formatter configured.

## Solution layout

Detailed per-project notes: `Engine/CLAUDE.md` (engine architecture) and `Spacebox/CLAUDE.md` (game logic).

Dependency direction: `Spacebox` → `Engine`, `Spacebox.Core`. Servers (`SpaceServer`, `SpaceServerUI`) → `ServerCommon` (folder `CommonLibrary/`) → `SpaceNetwork`. `Engine` must not reference game code.

- **Engine/** — reusable engine: `EngineWindow` (abstract `GameWindow` with hook methods `OnRegisterScenes`, `OnGameLoad`, `OnSetupPostProcessing`, …), `SceneManagement` (`Scene` is a `Node3D`; scenes are registered then loaded via `SceneManager.Load<T>()`), `Node3D`/`Components` scene graph, `Resources.Load<T>(pathOrName, global)` resource cache, rendering (shaders, materials, post-processing, lights, particles), ImGui-based GUI, `InputPro` input actions, `Audio` (OpenAL + NVorbis), octrees, `ServiceLocator`, `EventBus`, `Debug` logging.
- **Spacebox/** — the game. `Program.cs` → `SpaceboxWindow` (subclasses `EngineWindow`, registers scenes, starts at `MenuScene`). `Scenes/` holds menu, single-player (`LocalSpaceScene` / `BaseSpaceScene`) and multiplayer scenes. `Client/` is the multiplayer client side.
- **Spacebox.Core/** — tiny plugin contract (`IPlugin`, `IGameApi`, `IWorldConverter`) that external DLL plugins compile against. `Game/PluginLoader.cs` loads every `*.dll` in `Plugins/<mod>/` and instantiates `IPlugin` types; `Game/Plugins/GameApi.cs` implements `IGameApi`.
- **LegacyWorldConverter/** — a plugin (built against `Spacebox.Core`) whose compiled DLL is checked in at `Spacebox/Plugins/LegacyWorldConverter/`.
- **SpaceNetwork/** — shared Lidgren.Network messages (`Messages/`, `MessageRegistry`, `MessageFactory`) used by both client and server.

## World / voxel architecture (`Spacebox/Game/Generation/`)

Hierarchy: `World` (singleton `Component`, octree of sectors, loads/unloads sectors around the player) → `Sector` (8192³ blocks, holds `SpaceEntity` list plus lazily generated `NotGeneratedEntity` placeholders) → `SpaceEntity` (an asteroid/ship; grid of chunks) → `Chunk` (32³ `Block`s). All implement `ISpaceStructure`; these live in `Structures/`, with `WorldGenerator`, `BiomeGenerator` and `WorldPersistenceManager` (saving).

- Meshing: `MeshGenerator` + `AOVoxels` (ambient occlusion) build chunk meshes; neighbor-chunk lookups go through `MeshGenerator.GetNeighborChunkIndexAndLocalCoords`. Changing block state (e.g. emission/light) needs the chunk mesh to be regenerated.
- Block behaviour: `Block` base class with subclasses in `Blocks/` (`InteractiveBlock`, `ElectricalBlock`, `StorageBlock`, furnace/crusher/generator, …). `ElectricalManager` and `LightManager` handle power and light propagation.
- Generation helpers outside `Game/`: `Spacebox/Generation/` (noise, Perlin worms, cavities, ore placement, LOD meshing).
- Background generation work is marshalled back to the GL thread through `MainThreadDispatcher`; GL calls must stay on the main thread.

## Data-driven content (GameSets)

Game content is JSON, not code: `Spacebox/GameSets/<Name>/` (`Default` is the base set) contains `blocks.json`, `items.json`, `crafting.json`, `recipes.json`, `config.json`, and texture/sound/emission folders. Loading lives in `Game/Resource/` (`GameSetLoader`, `BlocksLoader`, `*JSON.cs` DTOs, `BlockFactory`, `UVAtlas`/`AtlasTexture` for the texture atlas, `GameSetsUnpacker` which extracts .zip/.rar mods). Adding a block usually means a JSON entry + texture; a new block *behaviour* means a `Block` subclass wired up by its `Type` in `BlockFactory`. `VersionConverter` / `WorldLoader` handle save-format upgrades.

## Notes

- Shaders are GLSL files under `Spacebox/Resources/Shaders/`, loaded by path without extension (e.g. `Resources.Load<Shader>("Resources/Shaders/PostProcessing/vignette", true)`).
- `Spacebox/TODO.txt`, `BUGS.txt` are the author's task tracking; `DONE.txt` is the changelog (see Project rules).
- API docs can be generated with Doxygen (`Doxyfile` at the root, theme in `Documentation/`).
- `ships-design.md`: design of player-built ships (movable entities, thrusters, consoles, cannons, camera blocks, boarding, collisions, multiplayer) and the planned work packages. Read it before working on movable entities or ship blocks.

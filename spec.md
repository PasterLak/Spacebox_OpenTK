# Spacebox — Technical Specification

Version: `0.1.2` (`Engine/Application.cs` → `Application.Version`)

## 1. Overview

Spacebox is a single-player voxel survival game set in an asteroid belt, with an experimental client/server multiplayer mode. It runs on a custom C# engine built on OpenTK. Game content (blocks, items, recipes, generation) is data-driven through JSON "GameSets", and code plugins are loaded as DLLs.

| | |
|---|---|
| Language / runtime | C# / .NET 8 (`net8.0`; `SpaceServerUI` is `net8.0-windows`) |
| Graphics | OpenGL 3.3 core, forward-compatible context (OpenTK 4.8.2) |
| UI | ImGui.NET 1.91 (game UI and debug tools), SkiaSharp (engine-side 2D) |
| Audio | OpenAL (via OpenTK) + NVorbis (`.ogg`) |
| Networking | Lidgren.Network 1.0.2 (UDP) |
| Save format | NBT (SharpNBT) for world entities, JSON for metadata/player |
| Archives | SharpCompress (`.zip` / `.rar` GameSets) |
| Tests / benchmarks | xUnit 2.5, BenchmarkDotNet 0.14 |
| Platforms | Windows, Linux, macOS (`win-x64`, `linux-x64`, `osx-x64`) |

## 2. Solution Structure

| Project | Type | Depends on | Purpose |
|---|---|---|---|
| `Engine` | Library | — | Reusable engine: window, scenes, rendering, input, audio, physics, GUI, threading |
| `Spacebox` | Exe (WinExe in Release) | Engine, Spacebox.Core, SpaceNetwork | The game client |
| `Spacebox.Core` | Library | — | Public plugin API (`IPlugin`, `IGameApi`, `IWorldConverter`, `WorldInfo`) |
| `SpaceNetwork` | Library | Lidgren | Network message definitions shared by client and server |
| `ServerCommon` (`CommonLibrary/`) | Library | SpaceNetwork | Server logic: networking, players, bans, commands, config |
| `SpaceServer` | Exe | ServerCommon | Console dedicated server |
| `SpaceServerUI` | WinExe (WPF) | ServerCommon | Windows GUI server (MaterialDesign) |
| `LegacyWorldConverter` | Library | Spacebox.Core | Plugin that upgrades old saves; prebuilt DLL ships in `Spacebox/Plugins/` |
| `Spacebox.Tests` | xUnit | Spacebox | Unit tests (Debug only) |
| `Spacebox.Benchmarks` | Exe | Spacebox | Performance benchmarks (Debug only) |

### Build configurations

| Config | Defines | Notes |
|---|---|---|
| Debug | `DEBUG; TRACE; PROFILING; ASSERTS` | Console window, tests and benchmarks buildable |
| Profile | `DEBUG; TRACE; PROFILING; ASSERTS` | |
| Release | `TRACE` | `WinExe` (no console), tests and benchmarks excluded |

## 3. Engine

### 3.1 Application lifecycle

`Program.Main` creates a 1280×720 `SpaceboxWindow`, centred on the primary monitor. `SpaceboxWindow` extends the abstract `EngineWindow` (an OpenTK `GameWindow`), which drives the loop and exposes these hooks:

| Hook | Use in Spacebox |
|---|---|
| `OnRegisterScenes` | Registers all scenes with `SceneManager` |
| `OnGameLoad` | Sets the icon and loads `MenuScene` |
| `OnSetupInput` | Input actions (`InputPro`) |
| `OnSetupPostProcessing` | Adds post effects (vignette enabled) |
| `OnLoadImGui`, `OnRenderGameGUI`, `OnUpdateInput`, `OnPostRender` | ImGui setup, GUI, input, screenshot (F12) |

### 3.2 Scenes and scene graph

- `Scene` (abstract, inherits `Node3D`) implements `LoadContent` / `UnloadContent` / `Update` / `Render`.
- `SceneManager.Register<T>()` then `SceneManager.Load<T>()`; `ErrorScene` is shown when a scene fails to load.
- Game scenes: `LogoScene`, `MenuScene`, `LocalSpaceScene` (single-player, built on `BaseSpaceScene`), `MultiplayerLoadScene`, `MultiplayerScene`, `ServerMessageScene`, `PlaygroundScene`. Engine tool scene: `ParticleSystemEditor`.
- `Node3D` + `Transform3D` form the hierarchy; behaviour is attached as `Component`s.

### 3.3 Rendering

- Shaders are single `.glsl` files with `--Vert` / `--Frag` sections. They support `#include` (shared code in `Resources/Shaders/includes/`: `core.fs`, `fog.glsl`) and are loaded by path without the extension, e.g. `Resources.Load<Shader>("Resources/Shaders/block", true)`.
- Shared per-frame uniforms (camera position, fog density, …) go through `GlobalUniforms` / `UniformBuffer`; GL state is tracked in `GLState`.
- Camera-relative rendering is available (toggled by a console command) to keep precision across the large world.
- Culling: `CameraFrustum`, `BHVCuller`, octrees (`Octree<T>`, `PointOctree`).
- Lighting (`Engine/Light`): directional, point and spot lights from a `LightPool`, managed by `LightSystem`. Voxel light and emission are computed separately on the game side.
- Post-processing (`PostProcessManager`): vignette (on by default); black-and-white, edge detection, SSAO, normal view and depth view are also available.
- Other renderers: skybox / procedural skybox, particles, sprites/billboards, lines/axes/bounding-box debug draws, planets/atmosphere/sun/water shaders.

### 3.4 Other subsystems

| Subsystem | Location | Notes |
|---|---|---|
| Resources | `Engine/Resources.cs` | `Resources.Load<T>(pathOrName, global)` cache for `IResource` types; `global` resources survive scene changes |
| Input | `Engine/InputPro`, `Engine/Input.cs` | Action/binding system (`InputManager`, `KeyBinding`) |
| Audio | `Engine/Audio` | `AudioDevice` singleton, `AudioClip`, `AudioSource` |
| Physics | `Engine/Physics` | AABB/OBB/sphere bounds, rays, `StaticBody` / `DynamicBody`, collision layers |
| Threading | `Engine/Multithreading/WorkerPoolManager` | `ProcessorCount − 1` background workers with priority queue, plus named dedicated workers; `Enqueue(Action<CancellationToken>, Priority)` |
| Debug | `Debug`, `VisualDebug`, `Prof`, `Overlay` | Logging, debug drawing, profiler (`PROFILING`), overlay panels (FPS, camera, threads, lighting) |
| Console commands | `Engine/Commands` + `Spacebox/Game/Commands` | `CommandBase` / `CommandManager`; e.g. help, clear, version, teleport, gamemode, tag, clear inventory |
| Misc | | `ServiceLocator`, `EventBus`, `TimedEventManager`, `TickTaskManager`, `DisposalManager`, `Pool<T>` |

## 4. World Model

### 4.1 Spatial hierarchy

```
World (Component, singleton)        octree of Sectors, 8192 sectors per axis
 └─ Sector (8192³ blocks)           SpaceEntity list + lazily generated placeholders
     └─ SpaceEntity (asteroid/ship) 16³ chunks = 512³ blocks max
         └─ Chunk (32³ blocks)      Block[32,32,32], mesh, lighting
             └─ Block
```

- `World` loads sectors around the player and unloads distant ones, checked periodically.
- Each `Sector` places entities from the generator as `NotGeneratedEntity` placeholders. A placeholder becomes a full `SpaceEntity` when the player comes close. Destroyed entities are tracked by name in `EntitiesDestroyed`.
- All levels implement `ISpaceStructure`.

### 4.2 Block

`Block` packs its core state into one 64-bit field (37 bits used):

| Bits | Field | Range |
|---|---|---|
| 0–11 | BlockId | 12 bits → max 4096 block types |
| 12–14 | Direction | `Down, Up, Left, Right, Back, Forward` (default `Up`) |
| 15–22 | Mass | byte |
| 23–30 | Durability | byte |
| 31–34 | Flags | `Transparent, Air, Light, EnableEmission` |
| 35–36 | Rotation | `None, Right, Half, Left` |

It also has non-packed properties: `Color`, `LightColor` (`Color3Byte`), `LightLevel` and an `OnDestroy` callback.

Behaviour subclasses are created by `BlockFactory` from the JSON `Type` string (case-insensitive):

| `Type` | Class |
|---|---|
| `block` | `Block` |
| `mineral` | `MineralBlock` |
| `interactive` | `InteractiveBlock` |
| `storage` | `StorageBlock` (uses `StorageBlockData`) |
| `craftingtable` | `CraftingTableBlock` |
| `furnace`, `crusher`, `disassembler` | resource processing blocks |
| `analyzer`, `radar` | utility blocks |
| `generator`, `consumer`, `cable` | electrical network (`ElectricalManager`). Generators read JSON `Generator { Power }` (default 50, up to 1600); generation/consumption rates are 16-bit. In multi-cell electrical blocks only the origin cell generates/consumes, other cells only transfer (`ConfigureAsMultiblockPart`) |
| `gravity` | gravity generator (`GravityGeneratorBlock`, consumer). JSON `Gravity { Radius, MinRadius, MaxRadius, Strength, MinStrength, MaxStrength, Power, StrengthPowerFactor, MaxRoomCells, Presets[{Name, Strength}] }` (32, 4, 512, 9.81, 0.5, 30, 200, 0.25, 200000, Moon/Mars/Earth/Jupiter). Consumption = Power × radius/Radius × (1 + StrengthPowerFactor × (strength/Strength − 1)), 0 when switched off. `GravityGeneratorUI` sets radius, strength (presets), on/off and "only inside closed rooms" (flood fill from the block, `SealedRoomFill`/`GravityRoom`, leaks past the radius or `MaxRoomCells` count as open). Per-block settings saved as NBT `gravity_generator_data` (radius, strength, enabled, inside_only; omitted when default). Fade = min(6, radius/4). While powered it pulls toward the block's bottom side; registered in `GravityField` via `IEntityBlockListener`. Fields fade over the last 6 blocks; overlapping fields blend their directions by distance. Inside a field the player uses `GravityWalker` (capsule 1.6 m, eye at 1.5 m, walk/jump, auto-upright); drops and `SphereBody` objects fall |
| `dnareconstructor` | DNA reconstructor (`DnaReconstructorBlock`, consumer). JSON `DnaReconstructor { DnaItem, Power, ReconstructionTime }` (`dna_sample`, 50, 5). One DNA slot (`DnaReconstructorUI`), saved as NBT `dna_reconstructor_data`. Registered in `DnaReconstructorRegistry`; after death the player can respawn at the nearest powered one with DNA (uses 1 sample, `DnaReconstructionScreen` animation, free cell found by `SafeSpawnFinder`) |
| `explosive` | Bomb (`ExplosiveBlock`, `IProjectileReactive`). JSON `Explosive { Radius, Penetration, Damage, DamageFalloff, PushStrength, PushRadiusMultiplier, ChainDelay, DropBlocks, DestroyStorageItems }` (DropBlocks false: destroyed blocks drop nothing; DestroyStorageItems false: storages and machines still drop their contents). Detonates when hit by a projectile; `ExplosionSystem` (`World.Explosions`, max 2 per frame) casts Fibonacci rays (`BlastRayCaster`, penetration is spent on block durability), removes plain blocks in bulk with merged drops and special blocks (storages, electrical, doors, multiblocks) through the normal path (`ExplosionBlockBreaker`), chains other bombs, pushes the player/drops/bodies within Radius × PushRadiusMultiplier and damages within Radius (`ExplosionForces`, survival only for the player) |
| `door` | sliding multi-cell door (`Game/Generation/Doors`). JSON `Door`: `MoveDirection` (model direction, e.g. `Right`), `Speed` (blocks/s), `OpenTime` (s), `AutoClose`. Opening/closing moves the real cells between the doorway and the slide area (oxygen, collisions and saves follow); the slide is animated by a separate mesh while the cells are hidden from the chunk mesh (`Block.IsMeshHidden`, bit 49, mass still counted). The area a door does not occupy is reserved (no blocks can be placed). While sliding, the door's cells do not collide or get hit by raycasts; instead the moving door box collides (`DoorSystem.IntersectsMovingDoor`) and pushes the player, physics bodies and resting drops along the slide direction (crushing the player if the push would end inside a wall); raycasts (projectiles, thrown drops, tools) hit the moving door box (`DoorSystem.RaycastMovingDoors`) and resolve to a real door cell. Saved via `DoorSerializer` (`is_open`, `auto_close_timer`) |
| `oxygengenerator` | electrical consumer (20); needs water in its tank; fills a sealed room with air (`Game/Generation/Oxygen`). JSON `OxygenGenerator`: `WaterItem`, `TankCapacity` (30), `WaterConsumeInterval` (120 s), `WaterFillInterval` (1 s). Saved via `OxygenGeneratorSerializer` (`water`, `consume_progress`, `slot_data`) |

### 4.3 Meshing and lighting

- `MeshGenerator` builds chunk meshes and reads neighbouring chunks for faces at chunk borders. `ChunkLODMeshGenerator` builds reduced-detail meshes for distant chunks.
- Block vertex layout (`block.glsl`): position `vec3`, UV `vec2`, colour `vec3`, normal `vec3`, AO `vec2`.
- Multi-cell blocks: `Size {x,y,z}` in `blocks.json` (1..16 per axis, model axes: x width, y height, z depth, front = `FrontDirection`). Every cell stores the same block id/direction/rotation plus its model offset (`Block.MultiblockOffset`, bits 37–48); the cell with offset 0 is the origin. `MultiblockLayout` maps model offsets to entity cells for the block orientation. Placement checks all cells (`SpaceEntity.CanPlaceStructure`), removing any cell removes the structure (`SpaceEntity.RemoveStructure`). Face textures larger than one tile (`BlockSize`) span the face; tile-sized textures repeat per cell. Saved per chunk in `multiblock_offsets` (index, packed offset).
- Rotation: each `SpaceEntity` has an `Orientation` (quaternion, pivot = `PositionWorld`). Procedural asteroids get a random orientation from their seed; it is saved in the entity's `rotation_*` tags. Voxel code works in *entity space* (world space as if unrotated: `chunk.PositionWorld`, `BoundingBox`, `GeometryBoundingBox`, `CenterOfMass`); `ToEntitySpace`/`ToWorldSpace` convert at the boundaries (rendering, culling, raycasts, collisions, oxygen, effects, build preview).
- Oxygen: each `SpaceEntity` has an `OxygenSystem`. Air is not a block: breathable cells are bit masks per chunk, rebuilt by a flood fill from every oxygen generator over empty blocks (radius 32). Reaching the radius limit or a missing chunk counts as a leak (no air). Recalculated only when a generator is placed/removed, a chunk is added/removed, or a block changes next to a reached cell. Not saved; rebuilt on load.
- `AOVoxels` computes per-vertex ambient occlusion. `LightManager` propagates block light per RGB channel (each channel keeps the strongest value decayed by 0.8 per block, so overlapping lights blend instead of meeting in a hard line); the mesher lights each vertex by averaging the light of the up to 4 air cells around that corner (smooth lighting, diagonal corners ignored when both sides are solid); changing emission or light requires rebuilding the chunk mesh.
- Textures are packed by `Engine.Graphics.Atlas.TextureAtlas` (skyline packing, any texture sizes, power-of-two atlas, optional padding, extra layers such as `emission`). Textures can be added from folders, files, `Texture2D` or pixels at any time; the atlas rebuilds lazily and `AtlasRegion` UVs update in place. `GameAtlasFactory` builds the blocks atlas (GameSet `Blocks/` + `Resources/Textures/Blocks`, e.g. `LOD.png`) and the items atlas (GameSet `Items/` + `Resources/Textures/Items` creative tools + small UI images from `Resources/Textures/UI` + generated isometric block icons). UI draws icons and slots with `AtlasSprite` (one texture for a whole inventory).

### 4.4 Procedural generation

- Driven by `GameSets/<set>/Generation/generator.json`. Default: 1250–2500 asteroids per sector, at least 128 blocks apart, rejection sampling, biome-map noise frequency `0.001`.
- `BiomeGenerator` / `BiomesMap` choose a biome per region (`asteroid_field`, `dense_cluster`, `debug`). Biomes reference asteroid presets (`light`, `medium`, `heavy`, `asteroid_icy`, `cheese`).
- Asteroid voxels come from `AsteroidVoxelDataGenerator`, using `Spacebox/Generation/` helpers: `NoiseGenerator`, `PerlinWorms`, `InternalCavities`, `AsteroidOreGenerator`, `MaskContainer`, `SectorPointProvider`, `SeedHelper`.
- Loot tables (`Generation/Loot`) and structure definitions (`abandoned_ship`, `alien_base`, `ruins`) are data files.
- Generation runs on the worker pool. Results are handed back to the GL thread through `MainThreadDispatcher`; all OpenGL calls must happen on the main thread.

## 5. Gameplay Systems

- **Player:** `Astronaut` → `LocalAstronaut` (local player) and `RemoteAstronaut` (network players). Includes inertia-based 6-DoF movement (`InertiaController`, `MovementMode`), camera/item sway, flashlight, equipment, mood, statistics, death handling (`DeathCase`).
- **Game modes:** `SurvivalMode`, `CreativeMode`, `SpectatorMode`, `CameraMode` (all `GameModeBase`).
- **Interactions:** `InteractionHandler` switches between modes such as place block, destroy block (survival/creative), shoot, consume item, eraser and camera point.
- **Items** (`Type` in `items.json`): base `item`, `drill` (tier, power usage, range, colour), `weapon` (projectiles from `ProjectilesPool`, defined by `ProjectileJSON`), `consumable`, `equipment` (`Slot`: Helmet/Shoulders/Chest/Arms/Legs/Boots/PowerGenerator/Flashlight/Scanner, `Health`/`Power` bonuses to max stats; stack size 1). Max stack is limited to 255.
- **Inventory and crafting:** `Game/Inventory`, `crafting.json`, `recipes.json`.
- **Physics:** voxel ray hits and collisions in `VoxelPhysics`. Damage goes to `IDamageable` objects registered with `World`.
- **Effects:** drops (`DropManager`), block destruction and mining effects, particles.

## 6. Content & Modding

### 6.1 GameSets

A GameSet is a folder (or a `.zip` / `.rar` archive, extracted by `GameSetsUnpacker`) under `GameSets/`:

```
GameSets/Default/
  config.json      ModId, ModName, Author, Version, BlockSize (no longer limits texture sizes), OxygenConsumeInterval (s per 1 oxygen, default 10), Dependencies, ItemsOnStart
  blocks.json      block definitions (ID, Name, Type, PowerToDrill, Mass, Durability,
                   Textures, Sounds, Drop, DropQuantity, …)
  items.json       item definitions
  crafting.json    crafting categories/blueprints
  recipes.json     processing recipes (furnace, crusher, …)
  Blocks/ Items/ Emissions/ Sounds/ UI/ Projectiles/ AI/
  Generation/      generator.json, Asteroids/, Biomes/, Loot/, Structures/
  Player/          spawn parameters
  Optional/        lighting.json
```

`GameSetLoader` loads the active set, registers assets (`GameAssetsRegister`) and runs queued post-load actions. A GameSet declares the game version it targets in `Dependencies`.

### 6.2 Code plugins

- Built against `Spacebox.Core` only.
- `PluginLoader` scans `Plugins/<mod>/*.dll`, loads each DLL into the default `AssemblyLoadContext`, creates every non-abstract `IPlugin` type and calls `OnInitialize(IGameApi)`.
- `IWorldConverter` lets plugins upgrade saves; `LegacyWorldConverter` is the reference example.

## 7. Persistence

Worlds are stored in `<app dir>/Worlds/<world>/`:

| File | Format | Content |
|---|---|---|
| `world.json` | JSON | `WorldInfo`: name, seed, game version, GameSet, … |
| `player.json` | JSON | Player state (`PlayerSaveLoadManager`): position, `SpawnPoint` (first join position, optional; falls back to position), health/power/oxygen, `InventorySlots`, `PanelSlots`, `EquipmentSlots` (optional) |
| `Sectors/<sector>/<entity>.entity` | NBT | Modified `SpaceEntity` data (`NBTHelper.SpaceEntityToTag`). Procedural asteroids set `only_changed_chunks` = 1 and store only edited/previously saved chunks plus `removed_chunks` (packed chunk indices); unchanged chunks are regenerated from the seed on load. A chunk with `needs_generation` = 1 was created before its natural content was generated and gets it merged in later. Files without the flag (older saves, player-built entities) contain all chunks. |

- Only modified entities are saved. When an entity unloads, it goes into a concurrent cache in `WorldPersistenceManager`. The cache is written to disk on a background worker (`SaveAllCacheToDiskAsync`) or immediately (`SaveAllCacheToDiskNow`). Saving is available from the pause menu (Esc).
- `VersionConverter` upgrades `WorldInfo` step by step (e.g. `0.0.8 → 0.0.9 → …`). Larger format changes are handled by converter plugins.

## 8. Multiplayer

- **Transport:** Lidgren UDP. The Lidgren app identifier is the game version (`Application.Version`), so client and server versions must match exactly.
- **Discovery:** `LocalServerFinder` uses Lidgren LAN discovery (`DiscoverLocalPeers`).
- **Messages** (`SpaceNetwork/Messages`, created by `MessageFactory`, registered in `MessageRegistry`):
  `InitMessage`, `ServerInfoMessage`, `SeedMessage`, `PlayersMessage`, `Node3DMessage` (transform sync), `BlockPlaceMessage`, `BlockDestroyedMessage`, `ItemInHandMessage`, `FlashlightMessage`, `ChatMessage`, `RpcMessage`, `KickMessage`, `RequestZipMessage` / `ZipMessage` (the server sends its GameSet archive to the client).
- **Client:** `ClientNetwork`, `ClientPacketHandler` (message-type → handler map), `NetworkPlayerRegistry`, `NetworkIdentity`, `NetworkNode3DComponent`. The server's GameSet is stored under `GameSets/Server`.
- **Server** (`ServerCommon`): `ServerNetwork`, `MessageProcessor`, `PlayerManager`, `BanManager`, `CommandProcessor`, `ConfigManager`. Configured with `config.txt` (key=value):

```
name, description, port (default 14242), key (version), maxPlayers, seed, pingInterval, connectionTimeout
```

## 9. Build, Test & Distribution

```bash
dotnet build Spacebox.sln
dotnet run --project Spacebox/Spacebox.csproj
dotnet test Spacebox.Tests [--filter "FullyQualifiedName~<Class>"]
dotnet run -c Debug --project Spacebox.Benchmarks
dotnet publish Spacebox/Spacebox.csproj -c Release -r <win-x64|linux-x64|osx-x64> --self-contained true --output ./publish
```

- `Resources/`, `GameSets/`, `Plugins/` and shaders are copied to the output folder as content files.
- Windows: `Build/AddAfterBuild/OpenAL32.dll` may need to be placed next to the executable if audio doesn't work.
- Tests cover packed vector types, blocks, atlas packing (`AtlasLayout`), AO, mesh generation, world logic and version conversion.
- API docs: Doxygen (`Doxyfile`, theme in `Documentation/doxygen-awesome-css`).
- Releases: https://pasterlak.itch.io/spacebox

## 10. Limits & Constants

| Constant | Value | Location |
|---|---|---|
| Chunk size | 32 blocks | `Chunk.Size` |
| SpaceEntity size | 16 chunks (512 blocks) | `SpaceEntity.SizeChunks` |
| Sector size | 8192 blocks | `Sector.SizeBlocks` |
| World size | 8192 sectors per axis | `World.SizeSectors` |
| Max block types | 4096 | 12-bit BlockId |
| Max stack | 255 | `ItemJSON.ValidateMaxStack` |
| Atlas size | power of two, up to 8192 px; textures of any size | `TextureAtlas.MaxSize` |
| Default server port | 14242 | `ServerCommon/Settings.cs` |
| OpenGL | 3.3 core | `Program.cs` |

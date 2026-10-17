# Spacebox — game architecture notes for agents

Game project on top of `Engine` (see `Engine/CLAUDE.md`). World hierarchy, GameSets and coding rules: see root `CLAUDE.md`.

## Scene flow

- `SpaceboxWindow.OnRegisterScenes` registers every scene (new scenes must be added here); `OnGameLoad` loads `MenuScene`. Post-processing uses only the vignette.
- `Scenes/MenuScene.cs`: loads `Resources/default_input.json` into InputPro, skybox, logo, `GameMenu` (`Game/GUI/Menu/*Window.cs`).
- Single-player: `GameMenu` → `SceneManager.Load<LocalSpaceScene, SpaceSceneArgs>(args)`. `LocalSpaceScene` only creates a Creative `LocalAstronaut`; the work is in `BaseSpaceScene`.
- `Scenes/BaseSpaceScene.cs` (abstract, `ISceneWithArgs<SpaceSceneArgs>`):
  - `Initialize`: `SceneAssetsPreloader.Preload` → atlases into `GameAssets`, `GameSetLoader.Load`.
  - `LoadContent` order: `GameTime`, skybox, `RadarUI`, `BlockSelector`, `LocalAstronaut`, `World` component + `world.Load()`, `PlayerSaveLoadManager.LoadPlayer`, overlays, debug commands, static UIs (`InventoryUI`, `StorageUI`, `PanelUI`, `GeneratorUI`, `CreativeWindowUI`, `WelcomeUI`, `PauseUI`), `SpheresPool`, `ProjectileHitEffectsManager`, `TagManager`.
  - `Update`: `Time.HandleTicks`, `MainThreadDispatcher.ExecutePending`, Escape via `UIManager`, chat/panel updates.
  - `OnGUI` calls each static UI's `OnGUI`; `UnloadContent` disposes them and calls `GameAssets.DisposeAll()`.
- Multiplayer: `MultiplayerLoadScene` connects and downloads the server GameSet zip → `MultiplayerScene` (subclass of `BaseSpaceScene`) subscribes to `ClientNetwork.Instance` events and spawns `RemoteAstronaut`s.
- Test/legacy: `PlaygroundScene`, `PlanetsScene*`, `LogoScene`, `ServerMessageScene`, `Scenes/Test/`.

## Folder map

| Path | Purpose |
|---|---|
| `Game/Player/` | `Astronaut`, `LocalAstronaut`, `RemoteAstronaut`, movement, flashlight, projectiles, death, stats, player save |
| `Game/Player/GameModes/` | `GameModeBase` + Survival / Creative / Spectator / Camera |
| `Game/Player/Interactions/` | `InteractionMode` subclasses + `InteractionHandler` |
| `Game/Inventory/` | `Item` + `BlockItem`, `DrillItem`, `WeaponItem`, `ConsumableItem`, tool items; `Inventory`, `Storage`, `ItemSlot`, item models |
| `Game/GUI/` | Static ImGui UIs (inventory, storage, crafting, radar, chat, `PanelUI` hotbar, tags); `Menu/` main menu |
| `Game/Commands/` | Debug-console commands |
| `Game/Effects/` | Block destruction/mining effects, `DropManager`, dust, hit effects, stars |
| `Game/Events/` | `IEvent` structs for settings |
| `Game/Physics/` | `VoxelPhysics`, hit/collide info |
| `Game/GameMath/` | World ↔ Sector ↔ Entity ↔ Chunk coordinate conversions |
| `Game/Resource/` | GameSet loading, `BlockFactory`, atlases |
| `Game/Plugins/`, `Game/PluginLoader.cs` | `GameApi` and plugin DLL loading |
| `Game/Generation/` | World structures, `Blocks/`, meshing/AO, light and electric managers, `Tools/` (NBT, serializers) |
| `Generation/` | Stateless algorithms: noise, `PerlinWorms`, `InternalCavities`, `AsteroidOreGenerator`, `ChunkLODMeshGenerator`, seeds |
| `Client/` | Multiplayer client: `ClientNetwork`, `ClientPacketHandler`, `NetworkPlayerRegistry`, `NetworkIdentity`, `LocalServerFinder`, `SceneLauncher` |
| `Scenes/` | All scenes |
| `Resources/` | Shaders (`includes/`, `PostProcessing/`, `Engine/`), audio, fonts, models, textures, `Settings.json`, `default_input.json`, `theme.json` |

## Global state

- `Game/GameAssets.cs`: static session registry — `Blocks`/`Items` (by short id or string id), models, icons, sounds, recipes, blueprints, projectiles, atlases; `CreateBlockFromId`; cleared by `DisposeAll` on scene unload.
- `Globals.cs`: path constants. `Settings.cs`: static settings sections saved to `Resources/Settings.json`; DTOs in `GameSettings.cs` (with defaults and `Validate`).
- `Game/UIManager.cs`: named window stack controlling the cursor (`Open("pause")`, `IsUIMode`, `CloseTop`).
- Static singletons: `World.Instance`, `ClientNetwork.Instance`, `BlockSelector`, `RadarUI`, `TagManager`, `SpheresPool`, `ProjectileHitEffectsManager`, `MainThreadDispatcher`.
- Static UIs follow `Init` / `OnGUI` / `Dispose` and must be wired by hand into `BaseSpaceScene` (`LoadContent`, `OnGUI`, `UnloadContent`). Missing `Dispose` leaks state across scene loads.

## Blocks (`Game/Generation/Blocks/`)

`Block` packs state into one `long data`:

| Bits | Field |
|---|---|
| 0–11 | Id (max 4096) |
| 12–14 | Direction |
| 15–22 | Mass |
| 23–30 | Durability |
| 31–34 | `BlockFlags` (Transparent, Air, Light, EnableEmission) |
| 35–36 | Rotation |

Bits 37+ are free. Color, light color/level and `OnDestroy` are regular members.

- `Game/Resource/BlockFactory.cs`: dictionary `type string → constructor` (interactive, crusher, analyzer, mineral, furnace, disassembler, craftingtable, radar, cable, block, generator, consumer, storage). `RegisterBlockType` is currently a no-op — add new types to the dictionary.
- Blocks with extra persistent state need an `IBlockSerializer` registered in `Game/Generation/Tools/BlockSerializerRegistry.cs`.
- `ElectricNetworkManager` (file `ElectricalManager.cs`) — one per `SpaceEntity` (`SpaceEntity.ElectricManager`). `LightManager` — one per `Chunk`.
- Changing block visuals/emission requires regenerating the chunk mesh.

## Items and interactions

- Item type is chosen by a switch on JSON `Type` in `Game/Resource/GameSetLoader.cs` (weapon / drill / consumable / item).
- Interactions use a state pattern: `InteractionMode` (`OnEnable`, `OnDisable`, `Update(LocalAstronaut)`, `Render`, `AllowReload`) held by `InteractionHandler`. Each game mode passes a whitelist of allowed interaction types (e.g. `SurvivalMode.CreateInteractionHandler`); non-whitelisted types are rejected with an error log.
- The active interaction is picked from the held item in `Game/GUI/PanelUI.cs` → `UpdatePlayerInteraction`.
- Item-independent behaviour: `IPassiveInteraction`, passed to `InteractionHandler` by the game mode and updated after the active interaction (e.g. `CreativeBlockDestroying` in `CreativeMode`).
- Interactions that own left click (`block_destroy`/`shoot`) override `UsesPrimaryAction => true` (drills, shoot, eraser, camera point); passive left-click actions skip them to avoid double actions.
- Destroy blocks through `BlockDestroyer.Destroy` / `DestroyInstantly` (callback, removal, network, stats, sound). Hold-to-repeat input: `RepeatingAction`.

## Dynamic entities

- `World.Physics` (`Game/Physics/PhysicsWorld.cs`) holds every `SphereBody`: sphere-vs-voxel/body overlap and `Raycast` (closest body, no allocations, main thread only).
- Attach `SphereBody(physics, radius)` to a node: it registers itself and unregisters on detach (`RemoveChild` does not detach, so call `DetachComponent`). Non-kinematic bodies move by `Velocity` with per-axis substeps and bounce (`Bounciness`). The player has a kinematic body (`LocalAstronaut.EnablePhysics`).
- Projectiles damage `body.Owner is IDamageable`; the shooter's body is ignored. Drills damage `body.Owner is IDrillable` (tier damage per second, `InteractionDestroyBlock.TryDrillTarget`).
- The player pushes non-kinematic bodies on contact (`MovementMode.PushBody`, `BodyPushFactor`).

## Menu

- `Scenes/MenuScene.cs` draws static UIs in `OnGUI`. Menu music: `MenuMusicController` keeps playback in sync with `Settings.Audio.MenuMusic`; `MenuMusicButton` is the top-right toggle (icons `Resources/Textures/UI/musicOn.png` / `musicOff.png`).
- Save settings with `SettingsService.Save(Settings.AsGameSettings())`.

## Saving

- World data: NBT (SharpNBT `CompoundTag`, GZip) — `Game/Generation/Tools/WorldSaveLoad.cs` (sector folders, `.entity` files), `Structures/WorldPersistenceManager.cs` (caches and writes unloaded entities), `NBTHelper` / `NBTKey` / `PackingTools`.
- Player and tags: JSON in the world folder (`PlayerSaveLoadManager`, `TagsSaveLoader`).
- `Game/WorldLoader.cs` loads `WorldInfo`; `Game/VersionConverter.cs` upgrades step by step (0.0.8 → 0.0.9 → 0.1.0 → 0.1.1 → 0.1.2). New format versions add a step there; new fields need defaults.

## Multiplayer

- `ClientNetwork.Instance` (Lidgren): `Send<T>`, `SendBlockPlaced`, … plus C# events consumed by `MultiplayerScene`. Incoming messages: `Client/ClientPacketHandler.cs`.
- World edits in multiplayer must also be sent via `ClientNetwork.Send*`. New messages: use the `add-network-message` skill.

## Events, ticks, threads

- Engine `EventBus` is used only for settings events; everything else uses plain C# `event`/`Action`.
- Tick-based logic: `Time.OnTick` → `TickTaskManager`.
- `MainThreadDispatcher` (`Game/Generation/Structures/MainThreadDispatcher.cs`) brings worker results back to the GL thread; drained in `BaseSpaceScene.Update` and `Sector`. `DisposalManager.ProcessDisposals` is also driven from `BaseSpaceScene`.

## Where to add things

| Feature | Steps |
|---|---|
| Data-only block / item | GameSet JSON + texture (`add-block` / `add-item` skills) |
| Block behaviour | `Block` subclass in `Game/Generation/Blocks/` + `BlockFactory` entry (+ serializer if stateful) |
| Equipment item | `items.json` entry with `"Type": "equipment"`, `Slot`, `Health`/`Power`; bonuses applied by `EquipmentStatsBinder`, UI in `EquipmentUI` |
| Item type | `Item` subclass in `Game/Inventory/` + case in `GameSetLoader` + interaction branch in `PanelUI` |
| Interaction | `InteractionMode` subclass + game-mode whitelists + `PanelUI.UpdatePlayerInteraction` |
| Action for any held item | `IPassiveInteraction` + add it in the game mode's `CreateInteractionHandler` |
| UI window | `Game/GUI/` + wiring in `BaseSpaceScene` (`LoadContent`, `OnGUI`, `UnloadContent`) |
| Debug command | `Game/Commands/` + registration in `BaseSpaceScene.LoadContent` |
| Scene | `Scenes/` + `SpaceboxWindow.OnRegisterScenes` |
| Save format change | NBT keys with defaults; breaking change → `VersionConverter` step |
| Network message | `add-network-message` skill |
| Reusable, game-independent code | `Engine/`, not here |

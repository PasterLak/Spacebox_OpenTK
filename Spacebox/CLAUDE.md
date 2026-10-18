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
- Multiplayer: `MultiplayerWindow`/`QuickJoin` → `ConnectionScreen.Show` + `ClientConnector` (join steps, GameSet check/download) → `LocalSpaceScene` with `RemoteWorldInfo.ToSceneArgs`; errors and in-game disconnects end on `ConnectionScreen` in the menu. See the Multiplayer section.
- Test/legacy: `PlaygroundScene`, `PlanetsScene*`, `LogoScene`, `ServerMessageScene`, `Scenes/Test/`.

## Folder map

| Path | Purpose |
|---|---|
| `Game/Mobs/` | Mobs: `Mob`, `MobSystem`, `MobDefinition`, senses, locomotion, `Navigation/`, `Feeding/`, `States/`, `Brains/`, `Spawning/` (see Mobs) |
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
| `Network/` | Multiplayer (see the Multiplayer section) |
| `Scenes/` | All scenes |
| `Resources/` | Shaders (`includes/`, `PostProcessing/`, `Engine/`), audio, fonts, models, textures, `Settings.json`, `default_input.json`, `theme.json` |

## Global state

- `Game/GameAssets.cs`: static session registry — `Blocks`/`Items` (by short id or string id), models, icons, sounds, recipes, blueprints, projectiles, atlases; `CreateBlockFromId`; cleared by `DisposeAll` on scene unload.
- `Globals.cs`: path constants. `Settings.cs`: static settings sections saved to `Resources/Settings.json`; DTOs in `GameSettings.cs` (with defaults and `Validate`).
- `Game/UIManager.cs`: named window stack controlling the cursor (`Open("pause")`, `IsUIMode`, `CloseTop`).
- Static singletons: `World.Instance`, `NetworkSession.Current`, `BlockSelector`, `RadarUI`, `TagManager`, `SpheresPool`, `ProjectileHitEffectsManager`, `MainThreadDispatcher`.
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

## Multi-cell blocks

- `BlockData.Size` > 1: every cell is a `Block` with `MultiblockOffset` (model-space offset, origin = 0). Use `MultiblockLayout` for offsets/UVs, `SpaceEntity.TryPlaceBlock` (places all cells or nothing), `CanPlaceStructure`, `RemoveStructure` (called by `Chunk.RemoveBlock`), `TryGetStructureBounds`. Raw writes to `chunk.Blocks` (bulk tools) bypass this and can leave partial structures.

## Gravity

- `GravityField.Sample(worldPos)` returns the gravity acceleration from all powered `GravityGeneratorBlock`s. Blocks that must know when they enter/leave an entity implement `IEntityBlockListener` (called on place, load and removal).
- `MovementMode` switches the player to `GravityWalker` while the field is stronger than 0.3 m/s² and back to zero-g (keeping velocity) outside.
- Fall damage (`Game/Player/FallDamage.cs`, survival only): `GravityWalker.Landed` reports the peak fall speed; the fallen height is `v² / 2g` with the current gravity. Settings (`FallDamage`: `SafeHeight`, `DamagePerBlock`) come from the gravity generator JSON, overridden by the preset nearest to the dominant generator's strength (`GravityField.GetDominantSource`). The impact sound (`FallImpactSound`) plays locally and is relayed by `AstronautNetwork` (owner → server RPC → `ClientsExceptOwner`) as a 3D sound at the player.
- `GravityWalker` collides as an axis-aligned box in entity-local space (`EntityBoxCollider.Sweep`, `SpaceEntity.WorldToEntityLocal`/`EntityLocalToWorld`), so walls slide and float precision stays local.

## Respawn

- `PlayerRespawner` (`Game/Player/Respawn/`): spawn point respawn (preload + `WorldLoadingScreen`, then `SafeSpawnFinder`) or DNA reconstruction (`DnaReconstructorRegistry.TryFindNearestReady`, `DnaReconstructionScreen`). `DeathScreen` asks it via `ReconstructionBlocker` / `OnReconstruct`.

## Explosions

- `World.Explosions` (`Game/Physics/Explosions/`): `Detonate(chunk, index, data, delay)`. Blocks that react to bullets implement `IProjectileReactive` (checked in `Projectile` before ricochet). Bodies take damage through `IExplosionDamageable`. Bulk block removal: `Chunk.ClearCellForBulkEdit` + `FinishBulkEdit` + `Oxygen.OnChunksChanged`.

## Doors

- `DoorBlock` (type `door`, settings in `DoorBlockData`) + per-entity `DoorSystem` (`entity.Doors`): registration, toggling, animation, auto-close, crushing and reserved cells (`IsReserved`, checked by `TryPlaceBlock`/`CanPlaceStructure`). Moving cells uses `SpaceEntity.ClearCellSilently`/`PlaceCellRaw` (no drops/effects). Toggles go through the server (`DoorNetwork`).

## Rotated entities

- `SpaceEntity.Orientation` rotates the whole entity around `PositionWorld`. Chunk positions, bounding boxes, `CenterOfMass` and placement math are in entity space (unrotated). Convert world input with `ToEntitySpace` / `DirectionToEntitySpace`, and entity-space output with `ToWorldSpace` / `ToWorldBounds` / `CenterOfMassWorld`.
- `SpaceEntity.Raycast` takes a world ray and returns world `hitPosition`/`blockPositionWorld`/`normalWorld`; `hit.normal` and `blockPositionIndex` stay in entity space for block operations. `IsColliding` accepts world spheres.
- Effects and drops at block positions: `BlockCenterToWorldCorner`. Build preview: pass the entity to `BlockSelector.UpdatePosition`.

## Atlases

- Engine `TextureAtlas` (`Engine/Graphics/Atlas/`): add with `AddDirectory`/`AddFile`/`Add(name, Texture2D|pixels)`, layers via `AddLayer*`; rebuilds lazily on `Texture`/`TryGetRegion`. Pixels passed to `Add` are top-down (like PNG files); `TryGetPixels(name, bottomUp: true)` returns GL-order pixels.
- `GameAssets.AtlasBlocks` / `AtlasItems` are built by `GameAtlasFactory`; built-in textures live in `Resources/Textures/Blocks` and `Resources/Textures/Items`.
- Item icons: `Item.Icon` (`AtlasSprite`, draw with `Draw`/`Image`/`ImageButton`). UI elements: `UIAtlas.Get("slot")` (lower-case path under `Resources/Textures/UI`, files up to 128 px; inside a world they share the items atlas with item icons). `GameAssets.GetItemIconTexture(id)` creates a standalone texture only when needed (drops).

## Dynamic entities

- `World.Physics` (`Game/Physics/PhysicsWorld.cs`) holds every `SphereBody`: sphere-vs-voxel/body overlap and `Raycast` (closest body, no allocations, main thread only).
- Attach `SphereBody(physics, radius)` to a node: it registers itself and unregisters on detach (`RemoveChild` does not detach, so call `DetachComponent`). Non-kinematic bodies move by `Velocity` with per-axis substeps and bounce (`Bounciness`). The player has a kinematic body (`LocalAstronaut.EnablePhysics`).
- Projectiles damage `body.Owner is IDamageable`; the shooter's body is ignored. Drills damage `body.Owner is IDrillable` (`DrillItem.Damage` per second from the drill's JSON `Damage`, `InteractionDestroyBlock.TryDrillTarget`).
- The player pushes non-kinematic bodies on contact (`MovementMode.PushBody`, `BodyPushFactor`).

## Mobs

- Definitions: `GameSets/<set>/Mobs/*.json` (`MobJSON` → `MobDefinition`, `MobsLoader`, `GameAssets.Mobs`). A new mob with existing behaviour is JSON + model/texture only.
- New behaviour: build a `StateMachine<MobContext>` from states in `Game/Mobs/States/` (or new `State<MobContext>` classes) in an `IMobBrain`, register it in `MobBrainFactory` under the JSON `Behaviour` name.
- AI, spawning and despawning run only on the server; replicas only animate. Mob changes visible to clients go through `IMobLink` (`MobNetwork`).
- Spawner items: `MobSpawnerItem` (`Type: "mob_spawner"`), `InteractionSpawnMob`, `MobSpawnNetwork`.

## Item transport

- `PipeBlock` (type `pipe`, settings `PipeBlockData`) moves items between neighbours through `IItemTransferEndpoint` (`Game/Logistics/`). A new block that should work with pipes implements `IItemTransferEndpoint` itself or gets an adapter registered with `ItemTransferEndpoints.Register`; existing adapters: `StorageEndpoint` (storages, filtered container slots) and `MachineEndpoint` (`ResourceProcessingBlock`). Transfers run on the server only.

## Oxygen

- `Game/Generation/Oxygen/`: `OxygenSystem` per `SpaceEntity` (`entity.Oxygen`), `AirFloodFill` (pooled buffers, main thread only), `AirMasks` (per-chunk bit masks), `AirGrid` (entity-local coordinate helpers).
- Block edits must notify it: `Chunk.PlaceBlock`/`RemoveBlock` call `Oxygen.OnBlockChanged`; bulk edits call `Oxygen.OnChunksChanged()`.
- Query with `World.IsBreathable(worldPos)`.
- `OxygenGeneratorBlock` works when `IsActive` (network power) and its tank has water; its timers run from `OxygenSystem.Update`. UI: `OxygenGeneratorUI`; settings: `OxygenGeneratorBlockData`.
- Slot filters (`ItemSlot.Filter`) are respected by drag-and-drop and `Storage.TryAddItem`.

## Menu

- `Scenes/MenuScene.cs` draws static UIs in `OnGUI`. Menu music: `MenuMusicController` keeps playback in sync with `Settings.Audio.MenuMusic`; `MenuMusicButton` is the top-right toggle (icons `Resources/Textures/UI/musicOn.png` / `musicOff.png`).
- Save settings with `SettingsService.Save(Settings.AsGameSettings())`.

## Saving

- World data: NBT (SharpNBT `CompoundTag`, GZip) — `Game/Generation/Tools/WorldSaveLoad.cs` (sector folders, `.entity` files), `Structures/WorldPersistenceManager.cs` (caches and writes unloaded entities), `NBTHelper` / `NBTKey` / `PackingTools`.
- Procedural asteroids save only changed chunks (`SpaceEntity.SavesOnlyChangedChunks`, `Chunk.ShouldBeSaved` = modified or loaded from a save, `Asteroid.RemovedChunks`). Anything that changes chunk data must set `chunk.IsModified = true`, otherwise the change is lost on save.
- Player and tags: JSON in the world folder (`PlayerSaveLoadManager`, `TagsSaveLoader`).
- `Game/WorldLoader.cs` loads `WorldInfo`; `Game/VersionConverter.cs` upgrades step by step (0.0.8 → 0.0.9 → 0.1.0 → 0.1.1 → 0.1.2). New format versions add a step there; new fields need defaults.

## Multiplayer

- `Network/`: `GameSession` (Singleplayer/Host/Client/Dedicated), `NetworkSession` (per game scene: starts the mode, join handling, player spawn, saves), `ClientConnector` (menu join), `PlayerRegistry`, `ChatService`, `WorldEditNetwork` (block edits), `EntitySync` (modified entity data), `DropNetwork` (server-owned drops via `DropManager.Replicator`), `Combat/` (`CombatNetwork` shots and damage, `PlayerHitboxes` lag-compensated hit tests), `Containers/` (server-authoritative item containers, new container blocks implement `IContainerBlock`), `BlockStateNetwork` (blocks with visible state implement `INetworkStateBlock` and call `MarkDirty`), `DoorNetwork`, `ExplosionNetwork`, `SectorNetwork` (destroyed asteroids, player-built entities per sector), `EntityCreationNetwork` (new entities only through the server), `TimeNetwork`, `GameSets/` (hash, server package, client download into `Server/<host>_<port>/GameSet`), `Npc/` (`spacer` prefab, `SpacerNetwork`), `Moderation/` (`ServerModeration`: kick/ban/mute, `moderation.json`), `PlayerListNetwork` (player list with ping for the pause menu), `Players/` (`PlayerSpawnService`, `AstronautNetwork`), `Dedicated/` (`--server`, `serverconfig.json`, console commands), `LaunchOptions`.
- Item containers implement `IContainer` (`Game/Inventory/`); blocks use `IContainerBlock`, network objects (spacer) are resolved with `ContainerKey.ForObject(netId)`. Bodies that react to player pushes over the network implement `IPushable`.
- Every game scene runs a session; check `NetworkManager.Instance.IsServer` before authoritative changes and `GameSession.IsRemoteClient` before touching save files.
- New world changes must be sent: block edits through `WorldEditNetwork`, everything else through a message, an RPC or a `NetworkVariable`. The `add-network-message` skill describes the old Lidgren system and is outdated.

## Events, ticks, threads

- Engine `EventBus` is used only for settings events; everything else uses plain C# `event`/`Action`.
- Tick-based logic: `Time.OnTick` → `TickTaskManager`.
- `MainThreadDispatcher` (`Game/Generation/Structures/MainThreadDispatcher.cs`) brings worker results back to the GL thread; drained in `BaseSpaceScene.Update` with a time budget (`StreamingBudget`, unlimited while loading) and in `Sector`.
- World loading: `World.PreloadAroundPlayer` runs after the player is loaded; `WorldLoadingScreen` keeps the player frozen until `World.GetPendingStreamingWork` (entities, chunks, mesh jobs) and LOD jobs reach zero. `DisposalManager.ProcessDisposals` is also driven from `BaseSpaceScene`.

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

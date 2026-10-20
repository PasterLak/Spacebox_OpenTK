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
| `Game/Player/GameModes/` | `GameModeBase` + Survival / Creative / Spectator |
| `Game/Player/Interactions/` | `InteractionMode` subclasses + `InteractionHandler` |
| `Game/Player/Building/` | `BlockSelector` (build preview, magnet, reserved areas) |
| `Game/Inventory/` | `Item` + `BlockItem`, `DrillItem`, `WeaponItem`, `ConsumableItem`, tool items; `Inventory`, `Storage`, `ItemSlot`, item models |
| `Game/GUI/` | Static ImGui UIs; root: `UIManager`, `UIAtlas`, `Theme`, `GUIHelper`, `KeyNames` |
| `Game/GUI/Hud/` | In-game overlay: `PanelUI` hotbar, health/power/oxygen bars, crosshair hit marker, `HudVisibility`, FPS counter, camera feed overlay |
| `Game/GUI/Inventory/` | Inventory, equipment (`PlayerPreview`), storage, creative window |
| `Game/GUI/Crafting/` | Crafting window, categories, crafting logic |
| `Game/GUI/Machines/` | Block windows: analyzer, disassembler, DNA reconstructor, generators, oxygen generator, processing machines, radar, ship console |
| `Game/GUI/Screens/` | Full screens and dialogs: pause, death, loading, connection, welcome, chat, statistics, player list, auth, update |
| `Game/GUI/Tags/`, `Game/GUI/Worlds/` | Waypoint tags (save/load); world info and the world list |
| `Game/GUI/Menu/` | Main menu (`GameMenu`, windows); `Options/` options screens shared with the pause menu, `Multiplayer/` server list and join |
| `Game/Commands/` | Debug-console commands |
| `Game/Protection/` | `EntityProtection`: admin-protected asteroids/ships (no player edits, damage or movement), synced by `Network/ProtectionNetwork` |
| `Game/Tutorial/` | JSON tutorials: parser, task types (`Tasks/`), runner, progress, service, UI (`UI/`); see `tutorial-design.md` |
| `Game/Effects/` | Block destruction/mining effects, `DropManager`, dust, hit effects |
| `Game/Settings/` | `Settings` (static sections), `GameSettings` DTOs, `SettingsApplier`, settings `IEvent`s |
| `Game/Rendering/` | Game-side rendering: post-processing controllers (SSAO, color grading, vignette, camera feed), `SimpleBlock`, `SpaceTexture` skybox, `ShootAnimation` |
| `Game/Npc/` | `Spacer` NPC (network side in `Network/Npc/`) |
| `Game/Versioning/` | `WorldLoader`, `VersionConverter`, world upgrade chain (`Steps/`) |
| `Game/Physics/` | `VoxelPhysics`, hit/collide info |
| `Game/GameMath/` | World ↔ Sector ↔ Entity ↔ Chunk coordinate conversions |
| `Game/Resource/` | `GameAssets` registry, `Globals`; `Loaders/` (GameSet, blocks, mobs, generation, `BlockFactory`, atlases), `Json/` (DTOs), `BlockTypes/` (`BlockData` and its subclasses, `TextureTiling`), `Materials/` |
| `Game/Generation/` | World structures, `Blocks/`, meshing/AO, light and electric managers, `Tools/` (NBT, serializers), `Algorithms/` (stateless: noise, `PerlinWorms`, `InternalCavities`, `AsteroidOreGenerator`, `ChunkLODMeshGenerator`, seeds) |
| `Network/` | Multiplayer (see the Multiplayer section) |
| `Scenes/` | All scenes |
| `Resources/` | Shaders (`includes/`, `PostProcessing/`, `Engine/`), audio, fonts, models, textures, `Settings.json`, `default_input.json`, `theme.json` |

## Global state

- `Game/Resource/GameAssets.cs`: static session registry — `Blocks`/`Items` (by short id or string id), models, icons, sounds, recipes, blueprints, projectiles, atlases; `CreateBlockFromId`; cleared by `DisposeAll` on scene unload.
- `Globals.cs`: path constants. `Settings.cs`: static settings sections saved to `Resources/Settings.json`; DTOs in `GameSettings.cs` (with defaults and `Validate`). `SettingsApplier` pushes them into the systems (audio mixer, window, frame limiter, render scale, post processing, AO/voxel lighting remesh, FOV, mouse); call it after changing a setting, at startup it runs from `SpaceboxWindow.ApplyDisplaySettings`.
- Options screens (`Game/GUI/Menu/Options/OptionsMenu`, `OptionsWindow`, `AudioWindow`, `GameWindow`, `GraphicsWindow`, `InterfaceWindow` (font, `Settings.Interface`), `ControlsWindow`) are shared by the main menu (`OptionsHostWindow`) and the pause menu (`PauseUI`, UI layer `pauseOptions`). They talk to an `IOptionsNavigator`; rows go through `OptionsRows` (label + widget + tooltip). Changes apply live, Save writes the file, Back/Escape restore the saved settings. A new setting: field with default + range check in `GameSettings.cs`, a row with a tooltip, and its effect in `SettingsApplier`.
- Sounds pick a mixer channel: `new AudioSource(clip, AudioChannel.UI)` for interface sounds, `Music`/`Ambient` for loops, `Effects` (default) for gameplay.
- `Game/GUI/UIManager.cs`: named window stack controlling the cursor (`Open("pause")`, `IsUIMode`, `CloseTop`).
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

Bits 37–48 hold `MultiblockOffset`, 49 `IsMeshHidden`, 50 `HasCustomShape`; 51+ are free. Color, light color/level and `OnDestroy` are regular members.

- `Game/Resource/Loaders/BlockFactory.cs`: dictionary `type string → constructor` (interactive, crusher, analyzer, mineral, furnace, disassembler, craftingtable, radar, cable, block, generator, consumer, storage). `RegisterBlockType` is currently a no-op — add new types to the dictionary.
- Blocks with extra persistent state need an `IBlockSerializer` registered in `Game/Generation/Tools/BlockSerializerRegistry.cs`.
- `ElectricNetworkManager` (file `ElectricalManager.cs`) — one per `SpaceEntity` (`SpaceEntity.ElectricManager`). `LightManager` — one per `Chunk`.
- Changing block visuals/emission requires regenerating the chunk mesh.

## Items and interactions

- Item type is chosen by a switch on JSON `Type` in `Game/Resource/Loaders/GameSetLoader.cs` (weapon / drill / consumable / item).
- Interactions use a state pattern: `InteractionMode` (`OnEnable`, `OnDisable`, `Update(LocalAstronaut)`, `Render`, `AllowReload`) held by `InteractionHandler`. Each game mode passes a whitelist of allowed interaction types (e.g. `SurvivalMode.CreateInteractionHandler`); non-whitelisted types are rejected with an error log.
- The active interaction is picked from the held item in `Game/GUI/Hud/PanelUI.cs` → `UpdatePlayerInteraction`.
- Item-independent behaviour: `IPassiveInteraction`, passed to `InteractionHandler` by the game mode and updated after the active interaction (e.g. `CreativeBlockDestroying` in `CreativeMode`).
- Interactions that own left click (`block_destroy`/`shoot`) override `UsesPrimaryAction => true` (drills, shoot, eraser, camera point); passive left-click actions skip them to avoid double actions.
- Destroy blocks through `BlockDestroyer.Destroy` / `DestroyInstantly` (callback, removal, network, stats, sound). Hold-to-repeat input: `RepeatingAction`.

## Multi-cell blocks

- `BlockData.Size` > 1: every cell is a `Block` with `MultiblockOffset` (model-space offset, origin = 0). Use `MultiblockLayout` for offsets/UVs, `SpaceEntity.TryPlaceBlock` (places all cells or nothing), `CanPlaceStructure`, `RemoveStructure` (called by `Chunk.RemoveBlock`), `TryGetStructureBounds`. Raw writes to `chunk.Blocks` (bulk tools) bypass this and can leave partial structures.

## Custom block geometry

- `Game/Generation/Shapes/`: JSON `Model` → `BlockShapeResolver` (code geometry from `BlockGeometryLibrary`, else GameSet `Models/Blocks/<name>.obj`) → `BlockData.Shape`; blocks get `HasCustomShape`. New code shapes: build with `BlockGeometryBuilder` and `BlockGeometryLibrary.Register`. Prefer OBJ files for content so shapes change without code.
- Placement: `BlockPlacementHelper` gives shaped blocks the clicked face's normal as `Direction` (bottom on the clicked block); `BlockSelector` previews the model via `BlockShapePreviewMesh`.
- Mesh: `MeshGenerator.AddShape`. Collisions/raycasts: `BlockCollision.TryGetSolidBounds` / `GetChunkCellBounds` (box depends on the support neighbour); new solidity checks should use it instead of treating every non-air cell as a full cube.

## Gravity

- `GravityField.Sample(worldPos)` returns the gravity acceleration from all powered `GravityGeneratorBlock`s. Blocks that must know when they enter/leave an entity implement `IEntityBlockListener` (called on place, load and removal).
- `MovementMode` switches the player to `GravityWalker` while the field is stronger than 0.3 m/s² and back to zero-g (keeping velocity) outside.
- Fall damage (`Game/Player/FallDamage.cs`, survival only): `GravityWalker.Landed` reports the peak fall speed; the fallen height is `v² / 2g` with the current gravity. Settings (`FallDamage`: `SafeHeight`, `DamagePerBlock`) come from the gravity generator JSON, overridden by the preset nearest to the dominant generator's strength (`GravityField.GetDominantSource`). The impact sound (`FallImpactSound`) plays locally and is relayed by `AstronautNetwork` (owner → server RPC → `ClientsExceptOwner`) as a 3D sound at the player.
- `GravityWalker` collides as an axis-aligned box in entity-local space (`EntityBoxCollider.Sweep`, `SpaceEntity.WorldToEntityLocal`/`EntityLocalToWorld`), so walls slide and float precision stays local.

## Respawn

- `PlayerRespawner` (`Game/Player/Respawn/`): spawn point respawn (preload + `WorldLoadingScreen`, then `SafeSpawnFinder`) or DNA reconstruction (`DnaReconstructorRegistry.TryFindNearestReady`, `DnaReconstructionScreen`). `DeathScreen` asks it via `ReconstructionBlocker` / `OnReconstruct`.

## Explosions

- `World.Explosions` (`Game/Physics/Explosions/`): `Detonate(chunk, index, data, delay)`. Blocks that react to bullets implement `IProjectileReactive` (checked in `Projectile` before ricochet). Bodies take damage through `IExplosionDamageable`. Any block can explode: implement `IExplosiveBlock` (data side `IExplosiveBlockData`, e.g. `GeneratorBlockData` with a JSON `Explosive` section) and trigger with `ExplosiveTrigger.Detonate` (bombs on any hit through `IProjectileReactive`, other explosive blocks from `Chunk.DamageBlock` when durability reaches 0); multi-cell blocks blast from their structure center and are removed as a whole. Bulk block removal: `Chunk.ClearCellForBulkEdit` + `FinishBulkEdit` + `Oxygen.OnChunksChanged`.

## Doors

- `DoorBlock` (type `door`, settings in `DoorBlockData`) + per-entity `DoorSystem` (`entity.Doors`): registration, toggling, animation, auto-close, crushing and reserved cells (`IsReserved`, checked by `TryPlaceBlock`/`CanPlaceStructure`). Moving cells uses `SpaceEntity.ClearCellSilently`/`PlaceCellRaw` (no drops/effects). Toggles go through the server (`DoorNetwork`).

## Ships

- `Game/Ships/`: every dynamic entity has a `ShipBody` (`entity.Ship`); move entities only with `SpaceEntity.SetPose` (keeps chunk caches and the sector octree in sync). Physics settings: GameSet `ships.json` → `ShipPhysicsSettings.Current`.
- Ship blocks (`ThrusterBlock`, `ShipConsoleBlock`, `CannonBlock`, `ShipCameraBlock`) implement `IShipPart` and register with `entity.ShipControl` from `IEntityBlockListener` (origin cell only for multi-cell blocks). Bindings are keyed by the part's origin cell (`ConsoleBindings`), saved by `ShipConsoleSerializer`.
- Cells a block keeps free (door slide, thruster nozzle `NozzleLength`): implement `IReservedAreaBlockData` on its block data (checked by `SpaceEntity.CanPlaceBlock` and the build preview); reserve them at runtime with `entity.Reservations.Set(owner, cells)` from `OnAddedToEntity` and `Remove` on removal.
- A new trigger block: implement `IShipPart`, register in `OnAddedToEntity`, react in `ShipControlSystem.ApplyCommand`, allow it in `CanBind`.
- Local pilot state is static (`ShipPilot`, `ShipRider`, `ShipBindingTool`, `ShipConsoleUI`) and reset in `BaseSpaceScene.UnloadContent`. Remote clients never simulate ships; they apply `ShipStateMessage` snapshots. Everything that changes ships goes through `ShipNetwork`.
- While piloting, `PanelUI.BlockHands`/`UnblockHands` hide the held item, block selector and hotbar input.
- Block tooltips: block data implementing `IBlockTooltip` appends its parameters in `InventoryUIHelper.ShowTooltip`; keep numbers out of JSON `Description`.
- Any entity becomes a ship when a ship block registers (`SpaceEntity.EnsureShipBody`), asteroids included. Mass (`ShipMassCache`) and hull (`ShipCollider`) are cached per chunk by `Chunk.DataVersion`.
- Entities are not bound to a sector: `World` transfers them when `PositionWorld` leaves the sector (`EntitySectorTracker`) and registers them as visitors of neighbour sectors they overlap, so `Sector.IsColliding`/`Raycast`/`TryGetNearestEntity` also see them. Do not cache an entity's sector; use `entity.Sector`. Fresh asteroid generation only happens for ids generated in that sector and not suppressed.
- Collisions: `ShipCollider` samples hull faces (`ShipHull`) against blocks of nearby entities; the math is in `ShipContactSolver` (tested in `Spacebox.Tests/Ships`).

## Rotated entities

- `SpaceEntity.Orientation` rotates the whole entity around `PositionWorld`. Chunk positions, bounding boxes, `CenterOfMass` and placement math are in entity space (unrotated). Convert world input with `ToEntitySpace` / `DirectionToEntitySpace`, and entity-space output with `ToWorldSpace` / `ToWorldBounds` / `CenterOfMassWorld`.
- `Sector.IsColliding` and `Sector.Raycast` test every entity whose block bounds are near (closest hit wins); `Sector.TryGetNearestEntity` picks by distance to the entity's block bounds, not its center of mass. Never resolve collisions against a single "nearest" entity — entities can touch.
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

## Armor visuals

- Equipment JSON `ArmorModel`/`ArmorTexture` (GameSet-relative, resolved by `GameSetLoader`; missing files = no visual). `ArmorRenderer` (component on every `Astronaut`, does not own the shared meshes) draws one piece per equipment slot, follows body visibility and block light. Local equipment changes update it directly; remote players get the slot item ids from `AstronautNetwork`. Meshes must match the astronaut pose (`Astronaut_Body_Fly`, `Astronaut_Helmet_Closed`) and the 64×64 skin UV layout.

## Item transport

- `PipeBlock` (type `pipe`, settings `PipeBlockData`) moves items between neighbours through `IItemTransferEndpoint` (`Game/Logistics/`). A new block that should work with pipes implements `IItemTransferEndpoint` itself or gets an adapter registered with `ItemTransferEndpoints.Register`; existing adapters: `StorageEndpoint` (storages, filtered container slots), `MachineEndpoint` (`ResourceProcessingBlock`) and `DisassemblerEndpoint`. Transfers run on the server only.

## Oxygen

- `Game/Generation/Oxygen/`: `OxygenSystem` per `SpaceEntity` (`entity.Oxygen`), `AirFloodFill` (pooled buffers, main thread only), `AirMasks` (per-chunk bit masks), `AirGrid` (entity-local coordinate helpers).
- Block edits must notify it: `Chunk.PlaceBlock`/`RemoveBlock` call `Oxygen.OnBlockChanged`; bulk edits call `Oxygen.OnChunksChanged()`.
- Query with `World.IsBreathable(worldPos)`.
- `OxygenGeneratorBlock` works when `IsActive` (network power) and its tank has water; its timers run from `OxygenSystem.Update`. UI: `OxygenGeneratorUI`; settings: `OxygenGeneratorBlockData`.
- Slot filters (`ItemSlot.Filter`) are respected by drag-and-drop and `Storage.TryAddItem`.

## Disassembly

- `Game/Disassembly/`: `DisassemblyRecipes` (static, built in `GameSetLoader` after crafting from `disassembly.json` via `DisassemblySettingsLoader`) resolves one `DisassemblyRecipe` per item through `IDisassemblyRule`s (configured → legacy `recipes.json` → derived from `IDisassemblySourceProvider`s: crafting, processing). New recipe sources are new providers; the math is in `DisassemblyCalculator`.
- `DisassemblerBlock` (type `disassembler`, `DisassemblerBlockData`) keeps its slots in `DisassemblerInventory` and ticks only while powered and busy (server only, `TickTask` started through `MainThreadDispatcher`). UI: `DisassemblerUI`.

## Menu

- `Scenes/MenuScene.cs` draws static UIs in `OnGUI`. Menu music: `MenuMusicController` keeps playback in sync with `Settings.Audio.MenuMusic`; `MenuMusicButton` is the top-right toggle (icons `Resources/Textures/UI/musicOn.png` / `musicOff.png`).
- Save settings with `SettingsService.Save(Settings.AsGameSettings())`.

## Saving

- World data: NBT (SharpNBT `CompoundTag`, GZip) — `Game/Generation/Tools/WorldSaveLoad.cs` (sector folders, `.entity` files), `Structures/WorldPersistenceManager.cs` (caches and writes unloaded entities), `NBTHelper` / `NBTKey` / `PackingTools`.
- Procedural asteroids save only changed chunks (`SpaceEntity.SavesOnlyChangedChunks`, `Chunk.ShouldBeSaved` = modified or loaded from a save, `Asteroid.RemovedChunks`). Anything that changes chunk data must set `chunk.IsModified = true`, otherwise the change is lost on save.
- Player and tags: JSON in the world folder (`PlayerSaveLoadManager`, `TagsSaveLoader`).
- `Game/Versioning/WorldLoader.cs` loads `WorldInfo`; `Game/Versioning/VersionConverter.cs` (menu API, `CheckWorld` / `Upgrade`) runs `Game/Versioning/WorldUpgrader`, the dedicated server upgrades on start (`DedicatedServer.UpgradeIfNeeded`). The chain starts at 0.1.2 (`Steps/Upgrade_0_1_2_To_0_1_5`); older versions, newer versions and old 0.1.2 save formats (`WorldFormatProbe`: `worldX` legacy, `world_x` positions) are refused before any change. Upgrade: first-step `Check` → zip backup to `Worlds/_backups/` → steps → restore on failure → `WorldValidator` warns about ids missing from the world's GameSet (`GameSetIdIndex`).
- Step toolkit: `WorldUpgradeContext` operations (`RenameBlock`/`RenameItem`/`RemoveBlock`/`RemoveItem` touch chunk palettes, player slots and drops; `EditChunks`/`EditEntities`/`EditSectors`/`EditPlayers`/`EditDrops`/`EditJson` for the rest), `NbtFileEditor` (read/write/edit GZip NBT, `RenameKey`), `ChunkTagAccess`, `IdRemap`, `WorldFileLayout`. Test kit: `Spacebox.Tests/Versioning/WorldFixtureBuilder` + `WorldInspector`.

## Multiplayer

- `Network/`: `GameSession` (Singleplayer/Host/Client/Dedicated), `NetworkSession` (per game scene: starts the mode, join handling, player spawn, saves), `ClientConnector` (menu join), `PlayerRegistry`, `ChatService`, `WorldEditNetwork` (block edits; box erase/fill of creative tools via `RequestBulkEdit` → `BulkBlockEditMessage`, rules in `Game/Generation/Tools/BulkBlockEdit`), `EntitySync` (modified entity data), `DropNetwork` (server-owned drops via `DropManager.Replicator`), `Combat/` (`CombatNetwork` shots and damage, `PlayerHitboxes` lag-compensated hit tests), `Containers/` (server-authoritative item containers, new container blocks implement `IContainerBlock`), `BlockStateNetwork` (blocks with visible state implement `INetworkStateBlock` and call `MarkDirty`), `DoorNetwork`, `ExplosionNetwork`, `SectorNetwork` (destroyed asteroids, player-built entities per sector), `EntityCreationNetwork` (new entities only through the server), `TimeNetwork`, `GameSets/` (hash, server package, client download into `Server/<host>_<port>/GameSet`), `Npc/` (`spacer` prefab, `SpacerNetwork`), `Moderation/` (`ServerModeration`: kick/ban/mute, `moderation.json`), `Admin/` (`ServerAdmins`: `Admins`/`ShowAdmins` from `serverconfig.json`, rights only with registration; `ChatCommands`: `/` commands from chat, output sent back to the sender), `PlayerListNetwork` (player list with ping for the pause menu), `Players/` (`PlayerSpawnService`, `AstronautNetwork`), `Dedicated/` (`--server`, `serverconfig.json`, console commands, local-only commands override `AvailableInTerminal => false`; runs headless, so presentation-only content checks `Application.IsHeadless`), `Auth/` (whitelist `ServerAccessSettings`, `ServerAuthService` + `AccountStore` on the server, `ClientAuth` + `SavedCredentialStore` + `AuthPromptWindow` on the client; challenge-response, passwords never sent), `Discovery/` (`ServerStatus`, `ServerAdvertiser` answers server queries, `ServerBrowser` pings saved servers and finds LAN servers for the menu), `Dashboard/` (web dashboard of the dedicated server: `DashboardService` wires `IDashboardModule`s in `Modules/` onto an `Engine.Web.HttpRouter`; a new page feature is a module + UI code in `Resources/Dashboard/app.js`; admin endpoints go through `DashboardGuard.Admin` and touch game state only inside `MainThread.Run`; optional HTTPS in `DashboardCertificate` + `DashboardHttpsPolicy`, automatic Let's Encrypt in `AutoCertificateService`, method chosen by `HttpsMethodSelector` from `PublicAddress` and the DNS tokens). Server or dashboard changes that players see (config fields, commands, pages, ports, files) must also update `Documentation/server-guide/server-guide.html` and the PDF (`Documentation/server-guide/build-pdf.ps1` → `Spacebox/Spacebox Server Guide.pdf`), `LaunchOptions`.
- Item containers implement `IContainer` (`Game/Inventory/`); blocks use `IContainerBlock`, network objects (spacer) are resolved with `ContainerKey.ForObject(netId)`. Bodies that react to player pushes over the network implement `IPushable`.
- Every game scene runs a session; check `NetworkManager.Instance.IsServer` before authoritative changes and `GameSession.IsRemoteClient` before touching save files.
- New world changes must be sent: block edits through `WorldEditNetwork`, everything else through a message, an RPC or a `NetworkVariable`.

## Events, ticks, threads

- Engine `EventBus` is used only for settings events; everything else uses plain C# `event`/`Action`.
- Tick-based logic: `Time.OnTick` → `TickTaskManager`.
- `MainThreadDispatcher` (`Game/Generation/Structures/MainThreadDispatcher.cs`) brings worker results back to the GL thread; drained in `BaseSpaceScene.Update` with a time budget (`StreamingBudget`, unlimited while loading) and in `Sector`.
- World loading: `World.PreloadAroundPlayer` runs after the player is loaded; `WorldLoadingScreen` keeps the player frozen until `World.GetPendingStreamingWork` (entities, chunks, mesh jobs) and LOD jobs reach zero. `DisposalManager.ProcessDisposals` is also driven from `BaseSpaceScene`.

## GameSet editor

- `Spacebox/Editor/`: `GameSetEditorScene` (+ `GameSetEditorLauncher` for `--editor`), `EditorContext` (project, runtime assets, previews, status), `Data/` (JSON files, entry sources, `JsonSchema`, `GameSetCatalog`), `UI/` (`EditorShell`, `JsonInspector`, `Fields/`, `Panels/`), `Preview/` (offscreen previews), `Generation/` (biome map).
- New content type: add an `EntryDefinition` (+ schema DTO) to `GameSetCatalog` and a `CollectionPanel` in `EditorPanelFactory`. New reference/value widget: an `IJsonFieldEditor` (or `ReferenceFieldEditor`) registered in `JsonFieldEditors.CreateDefault`. New preview: `IPreviewContent` + `IEntryPreview`.
- The editor theme (`EditorTheme`) and font scale are applied only during the editor's GUI pass and restored from an `ImGuiStyleSnapshot`, so menu and game styles are untouched; the editor does not change fonts.
- Previews read runtime assets (need Save & Reload after edits), projectile previews read the JSON live. GL work only in `IPreviewContent.Update/Render`; asteroid meshes are built on workers.

## Where to add things

| Feature | Steps |
|---|---|
| Data-only block / item | GameSet JSON + texture (`add-block` / `add-item` skills) |
| Block behaviour | `Block` subclass in `Game/Generation/Blocks/` + `BlockFactory` entry (+ serializer if stateful) |
| Tutorial / tutorial step | `GameSets/<set>/Tutorials/*.json` (see `tutorial-design.md`); a new task type is a `TutorialTask` subclass + `TutorialTaskFactory` entry (+ `ITutorialContext` member or `PlayerActivity` event) |
| Equipment item | `items.json` entry with `"Type": "equipment"`, `Slot`, `Health`/`Power`, `ExplosionProtection` (%); bonuses applied by `EquipmentStatsBinder`, UI in `EquipmentUI` (layout `ArmorLayout`/`DeviceSlots`, 3D figure `PlayerPreview`) |
| Item type | `Item` subclass in `Game/Inventory/` + case in `GameSetLoader` + interaction branch in `PanelUI` |
| Interaction | `InteractionMode` subclass + game-mode whitelists + `PanelUI.UpdatePlayerInteraction` |
| Action for any held item | `IPassiveInteraction` + add it in the game mode's `CreateInteractionHandler` |
| UI window | `Game/GUI/` + wiring in `BaseSpaceScene` (`LoadContent`, `OnGUI`, `UnloadContent`) |
| Debug command | `Game/Commands/` + registration in `BaseSpaceScene.LoadContent` (also usable from chat with `/`); argument suggestions through `IArgumentSuggestions` (`CommandSuggestionSources`); commands on a player (self or by name, server forwarding and admin check) derive from `PlayerTargetCommand<T>` |
| Scene | `Scenes/` + `SpaceboxWindow.OnRegisterScenes` |
| Save format change | NBT keys with defaults; renamed/removed ids or keys → `WorldUpgradeStep` in `Game/Versioning/Steps/` registered in `WorldUpgradeRegistry.CreateDefault` + test with `WorldFixtureBuilder` |
| Waypoint features | `Game/Waypoints/` (`WaypointService` over `TagManager` static tags, saved in `tags.json`; window `WaypointWindowUI`, key `waypoints` = M) |
| Network message | `add-network-message` skill |
| Reusable, game-independent code | `Engine/`, not here |

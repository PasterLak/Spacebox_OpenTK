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
| `Game/GUI/Menu/` | Main menu (`GameMenu`, windows); `Options/` options screens shared with the pause menu, `Multiplayer/` server list and join. `MenuWindow` hooks: `OnWindowChanged` (opened), `OnWindowClosed`, `OnSceneRender` (offscreen GL work, called from `MenuScene.Render` for the current window) |
| `Game/ShipViewer/` | Mods → Ship viewer: read-only world catalog, ship scan, sandboxed in-memory upgrade (`ReadOnlyShipLoader`), worker meshing (`ShipPreviewContent`), session (`ShipViewerSession`); see spec.md §6.2 |
| `Game/Commands/` | Debug-console commands |
| `Game/Protection/` | `EntityProtection` (admin-protected entities) and claim beacons (`ClaimRegistry`, `ClaimSettings` from `ServerConfig.Claims`, `ClaimVisuals`); every edit/use/damage check goes through `ProtectionRules` (cell-based), synced by `Network/ProtectionNetwork` and `ClaimNetwork` |
| `Game/Tutorial/` | JSON tutorials: parser, task types (`Tasks/`), runner, progress, service, UI (`UI/`); see `tutorial-design.md` |
| `Game/Effects/` | Block destruction/mining effects, `DropManager`, dust, hit effects |
| `Game/Settings/` | `Settings` (static sections), `GameSettings` DTOs, `SettingsApplier`, settings `IEvent`s |
| `Game/Rendering/` | Game-side rendering: post-processing controllers (SSAO, color grading, vignette, camera feed), `SimpleBlock`, `SpaceTexture` skybox, `ShootAnimation` |
| `Game/Npc/` | `Spacer` NPC (network side in `Network/Npc/`) |
| `Game/Versioning/` | `WorldLoader`, `VersionConverter`, world upgrade chain (`Steps/`) |
| `Game/Physics/` | `VoxelPhysics`, hit/collide info |
| `Game/GameMath/` | World ↔ Sector ↔ Entity ↔ Chunk coordinate conversions |
| `Game/Resource/` | `GameAssets` registry, `Globals`; `Content/` (GameSet stack: `GameSetStackResolver`, `ExtensionResolver`, `GameSetContentReader` for flat + folder format, merge rules in `KeyedContentList`, `GameSetAssetIndex` for textures/emissions/sounds, `Conversion/` flat → folder converter + `ContentSnapshot`), `TexturePacks/` (global texture packs, `TexturePackService`, server packs added per session), `Loaders/` (GameSet, blocks, mobs, generation, `BlockFactory`, atlases), `Json/` (DTOs), `BlockTypes/` (`BlockData` and its subclasses, `TextureTiling`), `Materials/` |
| `Game/Generation/` | World structures, `Blocks/`, meshing/AO, light and electric managers, `Tools/` (NBT, serializers), `Algorithms/` (stateless: noise, `PerlinWorms`, `InternalCavities`, `AsteroidOreGenerator`, `ChunkLODMeshGenerator`, seeds) |
| `Network/` | Multiplayer (see the Multiplayer section) |
| `Agent/` | Agent API for automated play testing (left out of Final builds, `AGENT_API` define): `AgentHost` (started from `SpaceboxWindow` with `--agent`, loopback `MiniHttpServer`), `AgentWorldLauncher` (`--agent-world`), one `IAgentModule` per route group in `Modules/` (registered in `AgentModuleCatalog`), `Navigation/AgentNavigator` (A* over `MobNavigationGrid`); client script `Tools/agent/game.sh`, usage in the `play-game` skill |
| `Scenes/` | All scenes |
| `Resources/` | Shaders (`includes/`, `PostProcessing/`, `Engine/`), audio, fonts, models, textures, `Settings.json`, `default_input.json`, `theme.json` |

## Global state

- `Game/Resource/GameAssets.cs`: static session registry — `Blocks`/`Items` (by short id or string id), models, icons, sounds, recipes, blueprints, projectiles, atlases; `CreateBlockFromId`; cleared by `DisposeAll` on scene unload.
- `Globals.cs`: path constants. `Settings.cs`: static settings sections saved to `Resources/Settings.json`; DTOs in `GameSettings.cs` (with defaults and `Validate`). `SettingsApplier` pushes them into the systems (audio mixer, window, frame limiter, render scale, post processing, AO/voxel lighting remesh, FOV, mouse); call it after changing a setting, at startup it runs from `SpaceboxWindow.ApplyDisplaySettings`.
- Options screens (`Game/GUI/Menu/Options/OptionsMenu`, `OptionsWindow`, `AudioWindow`, `GameWindow`, `GraphicsWindow`, `InterfaceWindow` (font, `Settings.Interface`), `ControlsWindow`) are shared by the main menu (`OptionsHostWindow`) and the pause menu (`PauseUI`, UI layer `pauseOptions`). They talk to an `IOptionsNavigator`; settings categories derive from `RowsCategoryWindow` and describe their rows once (`Options/Rows/`: `SliderRow`, `CheckRow`, `ChoiceRow`, `ButtonRow`, `TextRow`, `SectionRow` with `VisibleWhen`/`EnabledWhen`), drawn by `OptionRowsImGui` (pause menu) and `OptionRowsCanvas` (main menu). Changes apply live, Save writes the file, Back asks Save / Don't Save / Cancel when the category's state differs from when it was opened (`IOptionsCategory`: `CaptureState`/`Save`/`Discard`, settings windows derive from `SettingsCategoryWindow`, prompt `UnsavedChangesPrompt`), Escape in the pause menu restores the saved settings. A new setting: field with default + range check in `GameSettings.cs`, a row with a tooltip, and its effect in `SettingsApplier`.
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

- Damage cracks: the chunk vertex alpha byte holds the emission flag (0x80) and the damage stage (bits 0-2, `DamageStage.Of`, 0 = intact, 1-6 cracks); `block.glsl` draws `Resources/Textures/Effects/blockCracks.png` (6 stage columns × 8 variant rows of 32 px) from the chunk-local position, the variant and per-face rotation come from a cell hash (`CrackVariants` mirrors it for tests). Code that changes `Block.Durability` must call `BlockDamageVisuals.OnDurabilityChanged(chunk, block, before)`; it re-meshes the chunk only when the stage changes, at most every 0.3 s per chunk.
- `Game/Resource/Loaders/BlockFactory.cs`: dictionary `type string → constructor` (interactive, crusher, analyzer, mineral, furnace, disassembler, craftingtable, radar, cable, block, generator, consumer, storage). `RegisterBlockType` is currently a no-op — add new types to the dictionary.
- Blocks with extra persistent state need an `IBlockSerializer` registered in `Game/Generation/Tools/BlockSerializerRegistry.cs`.
- Cables (`CableBlock`, `CableBlockData` from the JSON `Cable` section) look like straight wires, angles or hubs depending on their electrical neighbours: `Generation/Wires/WireShape` (pure, tested) picks a texture and quarter turn per face, `MeshGenerator.AddWire` applies it. Look only, no extra block ids.
- `ElectricNetworkManager` (file `ElectricalManager.cs`) — one per `SpaceEntity` (`SpaceEntity.ElectricManager`). `LightManager` — one per `Chunk`.
- Changing block visuals/emission requires regenerating the chunk mesh.

## Items and interactions

- Item type is chosen by a switch on JSON `Type` in `Game/Resource/Loaders/GameSetLoader.cs` (weapon / drill / consumable / repair_tool / mob_spawner / picture / equipment / item).
- Interactions use a state pattern: `InteractionMode` (`OnEnable`, `OnDisable`, `Update(LocalAstronaut)`, `Render`, `AllowReload`) held by `InteractionHandler`. Each game mode passes a whitelist of allowed interaction types (e.g. `SurvivalMode.CreateInteractionHandler`); non-whitelisted types are rejected with an error log.
- The active interaction is picked from the held item in `Game/GUI/Hud/PanelUI.cs` → `UpdatePlayerInteraction`.
- Item-independent behaviour: `IPassiveInteraction`, passed to `InteractionHandler` by the game mode and updated after the active interaction (e.g. `CreativeBlockDestroying` in `CreativeMode`).
- Interactions that own left click (`block_destroy`/`shoot`) override `UsesPrimaryAction => true` (drills, shoot, eraser, camera point); passive left-click actions skip them to avoid double actions.
- Interactions that let the player use blocks call `BlockUseTarget.Update(player)` (fixed `InteractiveBlock.InteractionDistance`), never their own tool ray.
- Destroy blocks through `BlockDestroyer.Destroy` / `DestroyInstantly` (callback, removal, network, stats, sound). Hold-to-repeat input: `RepeatingAction`.

## Multi-cell blocks

- `BlockData.Size` > 1: every cell is a `Block` with `MultiblockOffset` (model-space offset, origin = 0). Use `MultiblockLayout` for offsets/UVs, `SpaceEntity.TryPlaceBlock` (places all cells or nothing), `CanPlaceStructure`, `RemoveStructure` (called by `Chunk.RemoveBlock`), `TryGetStructureBounds`. Raw writes to `chunk.Blocks` (bulk tools) bypass this and can leave partial structures.

## Custom block geometry

- `Game/Generation/Shapes/`: JSON `Model` → `BlockShapeResolver` (code geometry from `BlockGeometryLibrary`, else GameSet `Models/Blocks/<name>.obj`) → `BlockData.Shape`; blocks get `HasCustomShape`. New code shapes: build with `BlockGeometryBuilder` and `BlockGeometryLibrary.Register`. Prefer OBJ files for content so shapes change without code.
- Blocks whose geometry `RequiresSupport` (signs, frames) break with drops when their support cell is removed: `SupportedBlocks.BreakAround` runs from `Chunk.RemoveBlockCell` and after bulk clears (explosions, area erase) on every machine; drops only spawn on the server.
- Placement: `BlockPlacementHelper` gives shaped blocks the clicked face's normal as `Direction` (bottom on the clicked block); `BlockSelector` previews the model via `BlockShapePreviewMesh`.
- Mesh: `MeshGenerator.AddShape`. Collisions/raycasts: `BlockCollision.TryGetSolidBounds` / `GetChunkCellBounds` (box depends on the support neighbour); new solidity checks should use it instead of treating every non-air cell as a full cube.

## Signs

- Type `sign` (`SignBlock`, settings `SignBlockData` from the JSON `Sign` section: `MaxLines`, `MaxLineLength`, `DefaultText`, `TextColor`, `Align` (default center), `PixelsPerBlock`, `Margin`, `Glow`, `ViewDistance`, `Font`). A new sign is a block entry (`block.json`) with `"Type": "sign"` and textures (`Up` = front plate); no code. Wider signs set `Sign.Width` (1-4), which becomes `Size.X`; give them one texture spanning all cells with `CellPixels`.
- The plate is the code shape `sign_plate` (`SignPlateGeometry`, 1/32 thick, `RequiresSupport`, `FacingUp` so placement picks the `Rotation` that keeps the text upright via `BlockShape.BestRotation`). Wide signs are normal multi-cell blocks (`BaseFrontDirection` Up, cells along the text's right); every cell needs support, all cells can be used and broken, and only the origin cell registers with the renderer.
- Text markup (`Game/Signs/SignMarkup`): `<b>`, `<color=name|#rgb|#rrggbb>`, `<left> <center> <right>`, sign-wide `<size=x>` / `<fill>` (`SignTextSize`, resolved against `SignTextBaker.FitScale` so text never leaves the area); unknown tags stay text; `Sanitize` limits lines and visible characters. Text is baked per sign with SkiaSharp (`SignTextBaker`) into a small texture only when it changes and drawn as one quad by the entity's `SignRenderer` (`SpaceEntity.Signs`, rendered after doors, distance and facing culled, max 4 bakes per frame).
- Editing: `SignUI` (window, toolbar) + `SignTextEditor` (custom ImGui rich-text field: caret, selection, clipboard, keys) over `SignDocument` (per-character bold/colour, per-line alignment, converts from/to markup) → `SignNetwork` (`SignTextMessage`, server checks protection and reach, sanitizes) → `BlockStateNetwork`; saved by `SignSerializer` (`sign_data` with `text`).

## Picture frames

- Type `frame` (`PictureFrameBlock`, settings `PictureFrameBlockData` from the JSON `Frame` section: `Width`, `Height`, `Pictures` = accepted item ids, `ViewDistance`). Pictures are items of type `picture` (`PictureItem`: `Picture` = GameSet-relative PNG in `Pictures/`, `Width`, `Height`; the 32 px `Sprite` is only the icon). A new frame or picture is JSON + PNG; list a new picture in every frame that should accept it.
- Shape `PictureFrameGeometry.Shape`: plate + raised rim, one geometry per edge mask (`PlateEdges`); `BlockShape.ForCell` picks the variant in `MeshGenerator.AddShape` and `BlockShapePreviewMesh`, so the rim follows the outer edges of multi-cell frames.
- No window: using a frame (`PictureFrameInteraction`) with a fitting picture in hand inserts or swaps it (old one back to the player; survival takes the held one), with anything else takes it out. The picture lives in the origin cell (`PictureFrameBlock.TrySwap`); clients ask the server through `PictureFrameNetwork` (request/result, protection + reach). The shown picture is synced with `BlockStateNetwork`, drawn by `SpaceEntity.Pictures` (`PictureFrameRenderer`), saved by `PictureFrameSerializer`. Signs and frames share `Game/Rendering/PlateQuad` (quad placement on a shaped block, culling, light, draw).

## Gravity

- `GravityField.Sample(worldPos)` returns the gravity acceleration from all powered `GravityGeneratorBlock`s. Blocks that must know when they enter/leave an entity implement `IEntityBlockListener` (called on place, load and removal).
- Placed gravity generators start off (`IPlacedBlockListener`, JSON `EnabledOnPlace`); on/off sounds `EnableSound`/`DisableSound` play from `Configure` via `Game/Effects/BlockEventSounds` (shared 3D one-shots for block events, 24 blocks). Gravity and air-zone sounds come from `Tools/sounds/machine_sounds.py`.
- `MovementMode` switches the player to `GravityWalker` while the field is stronger than 0.3 m/s² and back to zero-g (keeping velocity) outside.
- Fall damage (`Game/Player/FallDamage.cs`, survival only): `GravityWalker.Landed` reports the peak fall speed; the fallen height is `v² / 2g` with the current gravity. Settings (`FallDamage`: `SafeHeight`, `DamagePerBlock`) come from the gravity generator JSON, overridden by the preset nearest to the dominant generator's strength (`GravityField.GetDominantSource`). The impact sound (`FallImpactSound`) plays locally and is relayed by `AstronautNetwork` (owner → server RPC → `ClientsExceptOwner`) as a 3D sound at the player.
- Slow walk (`slow_walk` action, Left Alt held) in `GravityWalker.MoveSpeed`. Crouch (`crouch` action, Left Ctrl) lives in `CrouchStance` (pure, eye height with headroom check); `GravityWalker.Enter` auto-crouches when only a crouched box fits. Footsteps: `Player/Footsteps/PlayerFootsteps` (distance cadence `FootstepCadence`, surface `FootstepSurfaces`: the block's JSON `Sounds.Footsteps` list (`BlockData.FootstepSounds`) if set, else natural blocks = stone, else metal, sounds via `FootstepSounds` + `Game/Effects/LocalSoundPlayer`, generated by `Tools/sounds/footstep_sounds.py`); local player only.
- `GravityWalker` collides as an axis-aligned box in entity-local space (`EntityBoxCollider.Sweep`, `SpaceEntity.WorldToEntityLocal`/`EntityLocalToWorld`), so walls slide and float precision stays local.

## Respawn

- `PlayerRespawner` (`Game/Player/Respawn/`): spawn point respawn (preload + `WorldLoadingScreen`, then `SafeSpawnFinder`) or DNA reconstruction through an `IReconstructionChannel` (`LocalReconstructionChannel` → `DnaReconstructionService` on the authority, `ReconstructionNetwork.Channel` on remote clients) that returns a `ReconstructionGrant`, then `DnaReconstructionScreen`. `DnaReconstructorRegistry` keeps loaded blocks and records of unloaded ones (`reconstructors.json`, pure parts `ReconstructorRecords`/`ReconstructorSelector`); the service loads only the reconstructor's entity by id (`EntityLoadRequests`, kept loaded by `WorldObservers.AddAnchor`). `DeathScreen` asks it via `ReconstructionBlocker` / `OnReconstruct`. Reconstruction sounds: `DnaReconstructionAudio` (local loop + stage cues, an `IReconstructionEffects` driven by `DnaReconstructionScreen`) and `DnaReconstructorSounds` (spawn sound at the block; other players get it from `AstronautNetwork` owner → server → `ClientsExceptOwner`); Default sounds from `Tools/sounds/dna_reconstructor_sounds.py`.

## Explosions

- `World.Explosions` (`Game/Physics/Explosions/`): `Detonate(chunk, index, data, delay)`. Blocks that react to bullets implement `IProjectileReactive` (checked in `Projectile` before ricochet). Bodies take damage through `IExplosionDamageable`. Any block can explode: implement `IExplosiveBlock` (data side `IExplosiveBlockData`, e.g. `GeneratorBlockData` with a JSON `Explosive` section) and trigger with `ExplosiveTrigger.Detonate` (bombs on any hit through `IProjectileReactive`, other explosive blocks from `Chunk.DamageBlock` when durability reaches 0); multi-cell blocks blast from their structure center and are removed as a whole. Bulk block removal: `Chunk.ClearCellForBulkEdit` + `FinishBulkEdit` + `Oxygen.OnChunksChanged`.
- Server gameplay rules (`Game/Rules/ServerRules`, `GameplayRules`): read `ServerRules.Current` only on the server; pass the causing peer (`GameplayRules.NoPeer` if none) into damage and explosion calls, explosions carry their resolved `ExplosionRules` to clients. A new rule: a property on `ServerRules` (+ `CopyFrom`) and a `SwitchRuleDefinition` or `NumberRuleDefinition` in `Network/ServerRulesService` (command, dashboard, client sync via `ServerRulesNetwork` and saving pick it up; dashboard inputs are named after the rule). Single player and host keep them in the world's `rules.json` (`WorldRulesStore`). Death drops: `Game/Player/DeathDrops`.

## Doors

- `DoorBlock` (type `door`, settings in `DoorBlockData`) + per-entity `DoorSystem` (`entity.Doors`): registration, toggling, animation, auto-close, crushing and reserved cells (`IsReserved`, checked by `TryPlaceBlock`/`CanPlaceStructure`). Moving cells uses `SpaceEntity.ClearCellSilently`/`PlaceCellRaw` (no drops/effects). Toggles go through the server (`DoorNetwork`). `Door.OpenSound` / `Door.CloseSound` (GameSet sound names) play from `DoorSystem.StartAnimation` on every machine via `DoorSounds` (shared `Game/Effects/RangedSoundPlayer`, 20 blocks); the Default door sounds come from `Tools/sounds/door_sounds.py` (3 styles per door: 1 pneumatic, 2 motor, 3 servo).

## Ships

- `Game/Ships/`: every dynamic entity has a `ShipBody` (`entity.Ship`); move entities only with `SpaceEntity.SetPose` (keeps chunk caches and the sector octree in sync). Physics settings: GameSet `ships.json` → `ShipPhysicsSettings.Current`.
- Ship blocks (`ThrusterBlock`, `ShipConsoleBlock`, `CannonBlock`, `ShipCameraBlock`) implement `IShipPart` and register with `entity.ShipControl` from `IEntityBlockListener` (origin cell only for multi-cell blocks). Bindings are keyed by the part's origin cell (`ConsoleBindings`), saved by `ShipConsoleSerializer`.
- Cells a block keeps free (door slide, thruster nozzle `NozzleLength`): implement `IReservedAreaBlockData` on its block data (checked by `SpaceEntity.CanPlaceBlock` and the build preview); reserve them at runtime with `entity.Reservations.Set(owner, cells)` from `OnAddedToEntity` and `Remove` on removal.
- A new trigger block: implement `IShipPart`, register in `OnAddedToEntity`, react in `ShipControlSystem.ApplyCommand`, allow it in `CanBind`.
- Local pilot state is static (`ShipPilot`, `ShipRider`, `ShipBindingTool`, `ShipConsoleUI`) and reset in `BaseSpaceScene.UnloadContent`. Remote clients never simulate ships; they apply `ShipStateMessage` snapshots. Everything that changes ships goes through `ShipNetwork`.
- While piloting, `PanelUI.BlockHands`/`UnblockHands` hide the held item, block selector and hotbar input.
- Block tooltips: block data implementing `IBlockTooltip` appends its parameters in `InventoryUIHelper.ShowTooltip`; keep numbers out of JSON `Description`. Items add text with `IItemTooltip` and an image under the text with `IItemTooltipImage` (pictures show their artwork).
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
- Item icons: `Item.Icon` (`AtlasSprite`, draw with `Draw`/`Image`/`ImageButton`). Block icons: plain 1x1 cubes use `IsometricIcon` (three face textures); blocks with a shape (code/OBJ model) or more than one cell are rendered from their preview mesh by `BlockIconRenderer` → engine `Graphics/Icons/IsometricRasterizer` (CPU, same 2:1 view and face shading, depth buffer, fits the model into 64x64; flat facing shapes like signs and frames stand facing the viewer). Mob spawner items get their icon rendered from the mob OBJ + texture (`MobIconRenderer`, `IsometricRasterizer` with `fitToSize`, `PixelIconSampler`; the JSON `Sprite` stays the held model and the fallback). UI elements: `UIAtlas.Get("slot")` (lower-case path under `Resources/Textures/UI`, files up to 128 px; inside a world they share the items atlas with item icons). `GameAssets.GetItemIconTexture(id)` creates a standalone texture only when needed (drops).

## Dynamic entities

- `World.Physics` (`Game/Physics/PhysicsWorld.cs`) holds every `SphereBody`: sphere-vs-voxel/body overlap and `Raycast` (closest body, no allocations, main thread only).
- Attach `SphereBody(physics, radius)` to a node: it registers itself and unregisters on detach (`RemoveChild` does not detach, so call `DetachComponent`). Non-kinematic bodies move by `Velocity` with per-axis substeps and bounce (`Bounciness`). The player has a kinematic body (`LocalAstronaut.EnablePhysics`).
- Projectiles damage `body.Owner is IDamageable`; the shooter's body is ignored. Drills damage `body.Owner is IDrillable` (`DrillItem.Damage` per second from the drill's JSON `Damage`, `InteractionDestroyBlock.TryDrillTarget`).
- The player pushes non-kinematic bodies on contact (`MovementMode.PushBody`, `BodyPushFactor`).

## Mobs

- Definitions: `Mods/GameSets/<set>/Mobs/*.json` (`MobJSON` → `MobDefinition`, `MobsLoader`, `GameAssets.Mobs`). A new mob with existing behaviour is JSON + model/texture only. Optional `Sounds` section (`MobSoundsJSON`: `Idle`, `Alert`, `Bite`, `Hurt`, `Death`, `Eat` = GameSet sound names, `IdleIntervalMin/Max`, `Volume`) is played as 3D one-shots (heard up to `MobSounds.MaxDistance` = 16 blocks from the camera) by `MobSounds` from hooks that run on every machine (hurt, bite, death, action changes, eating, idle timer). The Default parasite sounds are generated by `Tools/sounds/parasite_sounds.py`. Optional `DeathParticles` (`MobDeathParticlesJSON`: `Color {r,g,b}`, `Count`, `Speed`, `Size`, `Life`, `Radius`; 0 particles by default) bursts from `Mob.PlayDeathEffect` through `MobSystem.DeathEffects`.
- New behaviour: build a `StateMachine<MobContext>` from states in `Game/Mobs/States/` (or new `State<MobContext>` classes) in an `IMobBrain`, register it in `MobBrainFactory` under the JSON `Behaviour` name.
- AI, spawning and despawning run only on the server; replicas only animate. Mob changes visible to clients go through `IMobLink` (`MobNetwork`).
- Spawner items: `MobSpawnerItem` (`Type: "mob_spawner"`), `InteractionSpawnMob`, `MobSpawnNetwork`.
- Where mobs spawn: the mob JSON `Spawn` section holds the global rules; asteroid JSON `Mobs` entries (`AsteroidMobRule`, pure checks in `Spawning/MobSpawnRules`) restrict a listed mob to those asteroids, layers, depths, biomes and distances.

## Generation options

- New generation features only apply to worlds with `GeneratorVersion` 2 (`GeneratorVersions.UsesRules`); version 1 must stay bit-identical (`LegacyGenerationTests`). Sector planning: `SectorAsteroidPlanner`, biome choice: `BiomeSelector`, per-asteroid inputs: `AsteroidGenerationContext` (saved by `AsteroidGenerationTags`), vein checks: `VeinRules`, caves: `AsteroidCavities`. New options get a JSON field with a default, a runtime field in `WorldGenerator.cs`, loading in `GenerationLoader` and a line in modding-guide "Generation".
- Asteroid forms (new worlds, version 2): `Shapes/Asteroids/` (`AsteroidShape`, `ShapeSdf`, `AsteroidSolidMask` + `AsteroidSolidMaskBuilder` for floating-block removal); a new form is a part or cut in `AsteroidShape` with its JSON section in `AsteroidShapeJSON`.

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

- `Scenes/MenuScene.cs` draws static UIs in `OnGUI`. The main menu is CanvasUI: `MenuCanvas` (`Game/GUI/MenuUi/`) shows the `MenuPage` of `GameMenu`'s current window (`MenuWindow.CreatePage`; windows with a page skip their ImGui `Render`), plus logo, links, music button, connection screen, login and world conversion dialogs; `GameMenu` stays the controller. Page widgets: `MenuLayout` (window frame, buttons, fields, scroll lists), `SelectableRow`, `ChoiceBox`, `MenuModal` (Escape closes the top one). Menu music: `MenuMusicController` keeps playback in sync with `Settings.Audio.MenuMusic`; `MenuMusicButton` is the top-right toggle (icons `Resources/Textures/UI/musicOn.png` / `musicOff.png`).
- Save settings with `SettingsService.Save(Settings.AsGameSettings())`.

## Saving

- World data: NBT (SharpNBT `CompoundTag`, GZip) — `Game/Generation/Tools/WorldSaveLoad.cs` (sector folders with `.sector` files), `Structures/WorldPersistenceManager.cs` (caches and writes unloaded entities, owns the `EntityCatalog`), `NBTHelper` / `NBTKey` / `PackingTools`. Entity files live in `Entities/<name>.entity` (not per sector; they store `sector_x/y/z`, `EntityTagLocation`), indexed by `entities.index` (`EntityCatalogFile`); find/load one entity by id with `WorldPersistenceManager.TryLocate` / `EntityLoadRequests.Load`. Never write entity files directly: use `WorldPersistenceManager.WriteEntityFile` / `DeleteEntityFile` so the catalog stays in sync.
- Procedural asteroids save only changed chunks (`SpaceEntity.SavesOnlyChangedChunks`, `Chunk.ShouldBeSaved` = modified or loaded from a save, `Asteroid.RemovedChunks`). Anything that changes chunk data must set `chunk.IsModified = true`, otherwise the change is lost on save.
- Player and tags: NBT in the world folder (`PlayerSaveLoadManager` → `localplayer.player` / `Players/<name>.player`, `TagsSaveLoader` → `tags.nbt`). Every world-folder file is NBT: data classes that were JSON go through `Generation/Tools/SaveData/SaveDataFile` (`Read<T>`/`Write<T>`, `ReadJson`/`WriteJson`, each with the old `.json` path as legacy fallback); new world files must use it too, never plain JSON, and get a line in `Versioning/WorldJsonToNbt` (converts old JSON files when a world is opened). `WorldInfo` lives in `world.nbt` (`WorldInfoFiles`).
- Own worlds are written only by the Save button, autosave (`GameplaySettings.Autosave`) and server saves (`NetworkSession.SaveWorld` → `World.Save`). Unloaded modified entities and sectors stay in memory (`WorldPersistenceManager`) until then and are dropped when the world is left; `WorldSaveState` (marked by `Sector.IsModified`, plus the player JSON without statistics) decides whether Go to menu / Exit / closing the window asks Save / Don't Save / Cancel (`PauseUI.TryPromptBeforeLeaving`, `SpaceboxWindow.OnClosing`). Final builds (`WorldSavePolicy.SavesOnLeave`, `FINAL` define) save without asking when leaving, like Minecraft; Debug/Release only save manually.
- `Game/Versioning/WorldLoader.cs` loads `WorldInfo`; `Game/Versioning/VersionConverter.cs` (menu API, `CheckWorld` / `Upgrade`) runs `Game/Versioning/WorldUpgrader`, the dedicated server and `--agent-world` create or upgrade their world through `Game/GUI/Worlds/WorldProvisioner.Ensure`. The chain starts at 0.1.2 (`Steps/Upgrade_0_1_2_To_0_1_5`); older versions, newer versions and old 0.1.2 save formats (`WorldFormatProbe`: `worldX` legacy, `world_x` positions) are refused before any change. Upgrade: first-step `Check` → zip backup to `Worlds/_backups/` → steps → restore on failure → `WorldValidator` warns about ids missing from the world's GameSet (`GameSetIdIndex`).
- Step toolkit: `WorldUpgradeContext` operations (`RenameBlock`/`RenameItem`/`RemoveBlock`/`RemoveItem` touch chunk palettes, player slots and drops; `EditChunks`/`EditEntities`/`EditSectors`/`EditPlayers`/`EditDrops`/`EditJson` for the rest), `NbtFileEditor` (read/write/edit GZip NBT, `RenameKey`), `ChunkTagAccess`, `IdRemap`, `WorldFileLayout`. Test kit: `Spacebox.Tests/Versioning/WorldFixtureBuilder` + `WorldInspector`.

## Multiplayer

- `Network/`: `GameSession` (Singleplayer/Host/Client/Dedicated), `NetworkSession` (per game scene: starts the mode, join handling, player spawn, saves), `ClientConnector` (menu join), `PlayerRegistry`, `ChatService`, `WorldEditNetwork` (block edits; box erase/fill of creative tools via `RequestBulkEdit` → `BulkBlockEditMessage`, rules in `Game/Generation/Tools/BulkBlockEdit`), `EntitySync` (modified entity data), `DropNetwork` (server-owned drops via `DropManager.Replicator`), `Combat/` (`CombatNetwork` shots and damage, `PlayerHitboxes` lag-compensated hit tests), `Containers/` (server-authoritative item containers, new container blocks implement `IContainerBlock`), `BlockStateNetwork` (blocks with visible state implement `INetworkStateBlock` and call `MarkDirty`), `DoorNetwork`, `ExplosionNetwork`, `SectorNetwork` (destroyed asteroids, player-built entities per sector), `EntityCreationNetwork` (new entities only through the server), `TimeNetwork`, `GameSets/` (hash, server package with extensions and shared texture packs `ServerTexturePacks`, client download into `Multiplayer/Mods/<host>_<port>/`), `Npc/` (`spacer` prefab, `SpacerNetwork`), `Items/` (server item authority: `ItemAuthority` + `ItemLedger` per remote survival player, sequenced server changes, craft/tutorial claims, `ItemSync` on the client; every new way to give items to a player must go through it, see spec §8), `Moderation/` (`ServerModeration`: kick/ban/mute, `moderation.json`), `Admin/` (`ServerAdmins`: `Admins`/`ShowAdmins` from `serverconfig.json`, rights only with registration; `ChatCommands`: `/` commands from chat, output sent back to the sender), `PlayerListNetwork` (player list with ping for the pause menu), `Players/` (`PlayerSpawnService`, `AstronautNetwork`), `Dedicated/` (`--server`, `serverconfig.json`, console commands, local-only commands override `AvailableInTerminal => false`; runs headless, so presentation-only content checks `Application.IsHeadless`), `Auth/` (whitelist `ServerAccessSettings`, `ServerAuthService` + `AccountStore` on the server, `ClientAuth` + `SavedCredentialStore` + `AuthPromptWindow` on the client; challenge-response, passwords never sent), `Discovery/` (`ServerStatus`, `ServerAdvertiser` answers server queries, `ServerBrowser` pings saved servers and finds LAN servers for the menu), `Dashboard/` (web dashboard of the dedicated server: `DashboardService` wires `IDashboardModule`s in `Modules/` onto an `Engine.Web.HttpRouter`; a new page feature is a module + UI code in `Resources/Dashboard/app.js`; the Map page is `Modules/MapModule` + `Map/` (`WorldMapSource` reads loaded sectors, claims and players, `MapSnapshotBuilder` filters by radius and packs compact rows) + `Resources/Dashboard/map.js` (canvas 2D projection, no libraries); admin endpoints go through `DashboardGuard.Admin` and touch game state only inside `MainThread.Run`; optional HTTPS in `DashboardCertificate` + `DashboardHttpsPolicy`, automatic Let's Encrypt in `AutoCertificateService`, method chosen by `HttpsMethodSelector` from `PublicAddress` and the DNS tokens). Server or dashboard changes that players see (config fields, commands, pages, ports, files) must also update `Documentation/server-guide/server-guide.html` and the PDF (`Documentation/server-guide/build-pdf.ps1` → `Spacebox/Spacebox Server Guide.pdf`), `LaunchOptions`.
- Item containers implement `IContainer` (`Game/Inventory/`); blocks use `IContainerBlock`, network objects (spacer) are resolved with `ContainerKey.ForObject(netId)`. Bodies that react to player pushes over the network implement `IPushable`.
- Every game scene runs a session; check `NetworkManager.Instance.IsServer` before authoritative changes and `GameSession.IsRemoteClient` before touching save files.
- New world changes must be sent: block edits through `WorldEditNetwork`, everything else through a message, an RPC or a `NetworkVariable`.

## Events, ticks, threads

- Engine `EventBus` is used only for settings events; everything else uses plain C# `event`/`Action`.
- Tick-based logic: `Time.OnTick` → `TickTaskManager`.
- `MainThreadDispatcher` (`Game/Generation/Structures/MainThreadDispatcher.cs`) brings worker results back to the GL thread; drained in `BaseSpaceScene.Update` with a time budget (`StreamingBudget`, unlimited while loading) and in `Sector`.
- World loading: `World.PreloadAroundPlayer` runs after the player is loaded; `WorldLoadingScreen` keeps the player frozen until `World.GetPendingStreamingWork` (entities, chunks, mesh jobs) and LOD jobs reach zero. `DisposalManager.ProcessDisposals` is also driven from `BaseSpaceScene`.

## GameSet editor

- `Spacebox/Editor/`: `GameSetEditorScene` (+ `GameSetEditorLauncher` for `--editor`), `EditorContext` (project, runtime assets, previews, status), `Data/` (JSON files, entry sources, `JsonSchema`, `GameSetCatalog`), `Canvas/` (the CanvasUI editor: `EditorHost`, `EditorCanvasTheme`, `EditorPages`, `Inspector/` property grid and field widgets, `Pages/`, `Preview/` canvas controls for 3D previews, biome map and atlas), `Preview/` (offscreen previews), `Generation/` (biome map data).
- New content type: add an `EntryDefinition` (+ schema DTO) to `GameSetCatalog` and a `CollectionPage` in `EditorPages.CreateDefault`. New page: an `EditorPage` subclass (`Build` once, `Tick` per frame); new entry preview or summary: an `IEntryView` (`PreviewEntryView` for 3D, `RefreshingEntryView` for views rebuilt when the JSON changes). Every new JSON field (any DTO the editor shows) needs a tooltip text in `Resources/Editor/field-help.json`; `FieldHelpTests` lists missing ones. Blocks, items, blueprints, categories and processing recipes come from `GameSetSources` (flat files and folder format combined by `CompositeEntrySource`; `EntryFolderSource` for entry folders, `RecipeGroupFolderSource` for `Recipes/<type>.json`). Overview converts flat GameSets (`GameSetFolderConverter`). New reference/value widget: an `IFieldWidget` registered in `FieldWidgetRegistry.CreateDefault`. New 3D preview: `IPreviewContent` + a `PreviewEntryView`.
- The editor draws no ImGui; `PreviewViewport` has an ImGui adapter (`PreviewViewportImGui`) only for the menu's ship viewer.
- Previews read runtime assets (need Save & Reload after edits), projectile previews read the JSON live.
- `GameSetProject.IsReadOnly` (the `Empty` GameSet): saving is refused, the toolbar and status bar say so, Overview hides the converter. GL work only in `IPreviewContent.Update/Render`; asteroid meshes are built on workers.

## Where to add things

| Feature | Steps |
|---|---|
| Data-only block / item | entry folder with JSON + texture (`add-block` / `add-item` skills, `modding-guide.md`) |
| Extension GameSet / texture pack | `Mods/Extensions/<Name>/` with `"Extends"` / `Mods/TexturePacks/<Name>/` (see `modding-guide.md`, spec.md §6.1) |
| Sign | block entry with `"Type": "sign"` and a `Sign` section (see Signs) |
| Picture / picture frame | item entry with `"Type": "picture"` + PNG in `Pictures/`; frame: block entry with `"Type": "frame"` and a `Frame` section (see Picture frames) |
| Block behaviour | `Block` subclass in `Game/Generation/Blocks/` + `BlockFactory` entry (+ serializer if stateful) |
| Tutorial / tutorial step | `Mods/GameSets/<set>/Tutorials/*.json` (see `tutorial-design.md`); a new task type is a `TutorialTask` subclass + `TutorialTaskFactory` entry (+ `ITutorialContext` member or `PlayerActivity` event) |
| Equipment item | item entry with `"Type": "equipment"`, `Slot`, `Health`/`Power`, `ExplosionProtection` (%); bonuses applied by `EquipmentStatsBinder`, UI in `EquipmentUI` (layout `ArmorLayout`/`DeviceSlots`, 3D figure `PlayerPreview`) |
| Item type | `Item` subclass in `Game/Inventory/` + case in `GameSetLoader` + interaction branch in `PanelUI` |
| Interaction | `InteractionMode` subclass + game-mode whitelists + `PanelUI.UpdatePlayerInteraction` |
| Action for any held item | `IPassiveInteraction` + add it in the game mode's `CreateInteractionHandler` |
| UI window | `Game/GUI/` + wiring in `BaseSpaceScene` (`LoadContent`, `OnGUI`, `UnloadContent`) |
| Debug command | `Game/Commands/` + registration in `BaseSpaceScene.LoadContent` (also usable from chat with `/`); argument suggestions through `IArgumentSuggestions` (`CommandSuggestionSources`); commands on a player (self or by name, server forwarding and admin check) derive from `PlayerTargetCommand<T>` |
| Scene | `Scenes/` + `SpaceboxWindow.OnRegisterScenes` |
| Save format change | NBT keys with defaults; renamed/removed ids or keys → `WorldUpgradeStep` in `Game/Versioning/Steps/` registered in `WorldUpgradeRegistry.CreateDefault` + test with `WorldFixtureBuilder` |
| Waypoint features | `Game/Waypoints/` (`WaypointService` over `TagManager` static tags, saved in `tags.nbt`; window `WaypointWindowUI`, key `waypoints` = M) |
| Network message | `add-network-message` skill |
| Reusable, game-independent code | `Engine/`, not here |

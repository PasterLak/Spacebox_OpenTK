# Engine — architecture notes for agents

Game-independent engine over OpenTK (GL 3.3 core). Namespace `Engine.*`. Must never reference `Spacebox.*`. Coding rules: see root `CLAUDE.md`.

## Folder map

| Path | Purpose |
|---|---|
| `EngineWindow.cs` | Abstract `GameWindow`; owns the main loop and hook methods (see Lifecycle) |
| `Application.cs` | Static: version, platform, `OpenLink`, `Quit()` |
| `Time.cs` | `Time.Delta` (scaled by `TimeSize`), `Total`, FPS/TPS, per-phase timings, ticks |
| `SceneManagement/` | `Scene`, `SceneManager`, `ErrorScene`, `ParticleSystemEditor` |
| `Node3D.cs`, `Transform3D.cs`, `Components/` | Scene graph; `Component` base + built-ins (`ModelRendererComponent`, `ColliderComponent`, `BillboardComponent`, …) |
| `Resources.cs` | Typed resource cache (`IResource`) |
| `Shader.cs`, `Materials/`, `Texture2D.cs`, `Mesh.cs`, `Model.cs`, `MeshBuffer.cs` | Rendering primitives |
| `SceneRenderer.cs`, `Graphics/` | Scene FBO (color/normal/depth); `GLState`, `GPUDebug`, `UniformBuffer`, `GlobalUniforms` |
| `Graphics/Atlas/` | `TextureAtlas` (skyline packing, any sizes, layers, lazy rebuild), `AtlasLayout`, `AtlasRegion`, `AtlasSprite` (ImGui drawing), `ImagePixels` |
| `Light/` | `LightSystem` (UBO), `Lighting` (ambient, fog, `Lighting.Skybox`), light types, `LightPool` |
| `PostProcessing/` | `PostProcessManager` + effects (`DefaultEffect`, SSAO, Vignette, …) |
| `Skybox.cs` | Skybox `Node3D`, assigned to `Lighting.Skybox` |
| `Camera*.cs`, `FreeCamera.cs`, `OrbitCamera.cs`, `CameraFrustum.cs` | Cameras; `Camera.Main` is the active one |
| `RenderSpace.cs` | Camera-relative origin for large-coordinate rendering |
| `Physics/` | Bounding volumes, `Ray`, `Collision`, bodies, `BVHCuller` (file `BHVCuller.cs`) |
| `Octree*.cs`, `PointOctree*.cs` | Spatial structures (see "Which to use") |
| `GUI/` | ImGui debug `Overlay` (F3) and its elements, `Sprite`, `WorldTextDebug` |
| `UI/` | `DevGui`, `Canvas`/`Node2D`/`Rect` (2D layer, rarely used) |
| `ImGuiController.cs`, `ThemeUIEngine.cs`, `UIManager.cs` | ImGui backend, theme, window stack |
| `InputPro/` | Current input system (`InputManager.Instance`) |
| `Input.cs` | Raw keyboard/mouse state |
| `InputManager0.cs`, `InputAction0.cs` | Legacy input — do not use in new code |
| `Audio/` | OpenAL `AudioDevice`, `AudioClip`, `AudioSource`, `AudioMixer`, Ogg/Wave loaders |
| `Animation/` | `Animator`, move/rotate tweens |
| `ParticleSystem/` | Emitters and renderer |
| `Commands/` | Debug-console commands (`CommandManager`, `CommandBase`) |
| `Multithreading/` | `WorkerPoolManager` |
| `Math/`, `VectorExtensions.cs` | Byte vectors, colors, helpers |
| `Utils/` | `ObjLoader`, `PixelDataLoader`, `FastNoiseLite`, `GenMesh`, `ShaderHotReloader`, `DirectoryArchive` (zip folders, content hash, safe extract) |
| `Debug.cs`, `VisualDebug.cs`, `Prof.cs` | Logging + console, debug lines (F4), profiler scopes; `Debug.MirrorToStandardOutput`, `Debug.ExecuteCommand` |
| `AI/` | `StateMachine/` (`State<TContext>`, hierarchical `StateMachine<TContext>`), `Pathfinding/` (`INavigationGrid`, `VoxelLine` DDA, `VoxelPathfinder` A*, `PathRequest`, `PathScheduler` with per-frame node budget) — main thread only |
| `Networking/` | `NetworkManager` (modes, peers, send), `Transport/` (`INetTransport`, `LiteNetTransport`), `Serialization/` (`NetWriter`, `NetReader`, `NetSerializer`), `Messages/`, `Objects/` (`NetworkObject`, `NetworkBehaviour`, `NetworkVariable<T>`, `NetworkSpawner`, `ReplicationSystem`), `Rpc/` (`[Rpc]`, `RpcSystem`), `Behaviours/NetworkTransform`, `Transfer/` (`NetTransferService` = `NetworkManager.Transfers`, chunked blob downloads) |
| `ServiceLocator.cs`, `EventBus.cs`, `DisposalManager.cs`, `Pool.cs`, `TickTaskManager.cs`, `TimedEventManager.cs` | Services (see below) |

## Lifecycle (`EngineWindow.cs`)

Hooks for the game subclass (`Spacebox/SpaceboxWindow.cs`):
- abstract: `OnRegisterScenes`, `OnGameLoad`
- virtual: `OnLoadImGui`, `OnSetupInput`, `OnSetupPostProcessing(PostProcessManager)`, `OnRenderGameGUI`, `OnUpdateInput`, `OnPostRender`

`OnLoad` order: `Input.Initialize` → `ResourceFallbacks.Initialize` → `LightSystem.InitializeGpu` → `VisualDebug.InitializeGpu` → `SceneRenderer` → `FrameLimiter` → `SceneManager.Initialize(OnRegisterScenes)` → `ImGuiController` → `OnLoadImGui` → default F-key bindings → `OnSetupInput` → `PostProcessManager` → `OnSetupPostProcessing` → `OnGameLoad`.

`OnUpdateFrame`: `SceneManager.ApplyPendingSceneChange` → `RenderSpace.BeginFrame` → `Input.Update` → `Time.Update` → ImGui update → `InputManager.Update` (InputPro) → `InputManager0.Update` → drain main-thread action queue → `OnUpdateInput` → `SceneManager.Update` → `GlobalUniforms.Push` → `AudioDevice.Update`.

`OnRenderFrame`: `LightSystem.Update` → `SceneRenderer.Render` (into FBO: `Scene.Render` draws skybox, then `BVHCuller.Render(scene, Camera.Main)`; then `VisualDebug.Render`) → backbuffer + `PostProcessManager.Process` → GUI (`Scene.OnGUI`, `Debug.Render`, `Overlay.OnGUI`, `DevGui.Draw`, `OnRenderGameGUI`, ImGui render — skipped entirely if `Camera.Main == null`) → swap → `OnPostRender`.

`Quit()` unloads scene, audio and all resources (including global), shuts down `WorkerPoolManager`, resets `ServiceLocator`.

## Scenes and scene graph

- Register scenes with `SceneManager.Register<T>()` inside `OnRegisterScenes`; loading an unregistered scene falls back to `ErrorScene`.
- `SceneManager.Load<T>()`, `Load<T, TParam>(arg)` (scene implements `ISceneWithArgs<TParam>`), `Reload()`, `LoadPrevious()`. Switching is deferred to the start of the next update.
- Load order: constructor → `LoadContent()` → `Start()`.
- Unload clears: scene commands and overlay elements, `Destroy()`, `UnloadContent()`, skybox and `LightSystem`, non-global `InputManager0` actions, **all `EventBus` subscribers**, `Camera.Main`, **all non-global resources**, `VisualDebug`; then a forced GC.
- `Scene` is an abstract `Node3D` with `LoadContent`/`UnloadContent`.
- `Node3D`: `Start`, `Update`, `Render`, `OnGUI`, `Destroy`; recurse into enabled components then children. Exception: `Node3D.Render` only renders its own components and does not recurse — `BVHCuller` walks the tree and calls `Render()` per node.
- `Component`: `OnAttached`, `Start`, `OnUpdate`, `OnRender`, `OnGUI`, `OnDetached`; attach with `node.AttachComponent(c)`.

## Resources (`Resources.cs`)

- `Resources.Load<T>(path, global = false)`: cached per type; key is the path without extension with `/` separators, plus a short file-name alias.
- On failure it logs and returns the fallback from `ResourceFallbacks` (pink texture, error shader/mesh/clip, created in `EngineWindow.OnLoad`) — it does not throw. A type without a fallback throws `InvalidOperationException`.
- `Resources` itself is a plain registry without GPU work, so it is usable in tests.
- Non-global resources are disposed on every scene switch. Use `global: true` for anything that outlives a scene (all engine-owned resources).
- `Get<T>` loads on miss; `AddResource(name, res, global)` registers code-created resources. Leaked "controlled" resources are reported by `UnloadAll`.

## Rendering

- Shaders are single files with `--Vert` / `--Frag` sections, loaded by path without extension; hot-reloaded via a file watcher (reload runs on the main thread). UBOs are bound automatically.
- Matrix convention is row-vector: `vec4(aPos, 1) * model * view * projection`; `SetMatrix4` transposes by default.
- `LightSystem` limits: `MAX_DIR = 4`, `MAX_POINT = 64`, `MAX_SPOT = 32`.
- Materials derive from `MaterialBase` (shader, color, render mode/queue, textures, `Apply(node)`).
- Large coordinates: build model matrices with `GetRenderModelMatrix` relative to `RenderSpace`.
- Wrap GPU work in `GPUDebug` groups and `Prof.Time(token)` scopes where useful.

## GUI and input

- GUI is ImGui (ImGui.NET) via `ImGuiController`. `UIManager` keeps a stack of named windows (`Open`, `Toggle`, `CloseTop`, `IsOpen`); gate gameplay input on `IsUIMode`. The debug console is the `"debug"` window.
- Input: named actions → `InputPro.InputManager.Instance` (JSON bindings, remappable); raw state → `Input.IsKey` / `IsKeyDown` / `Input.Mouse`. `InputManager0`/`InputAction0` are legacy (still used for engine F-keys).

## Threading and services

- GL calls only on the main thread (buffer/texture/shader creation **and** disposal).
- Static systems never create GPU objects in static constructors or field initializers: keep their bookkeeping GL-free and create GPU objects in an explicit `InitializeGpu()` called from `EngineWindow.OnLoad` (`ResourceFallbacks`, `LightSystem`, `VisualDebug`). This keeps them testable (`Engine.Tests` covers `SceneManager`, `Resources`, `Scene`) and prevents GL calls from worker threads that first touch a type.
- `WorkerPoolManager.Enqueue(action, Priority.High|Medium|Low, token)` returns a `Task`; named dedicated workers via `AddDedicatedWorker` / `EnqueueDedicated`.
- Main-thread marshalling inside the engine: `EngineWindow`'s main-thread action queue. (The game's `MainThreadDispatcher` lives in `Spacebox`, not here.)
- `DisposalManager`: thread-safe queue to defer GL disposal from worker threads; processed by the game scene.
- `EventBus`: struct events implementing `IEvent`, listeners `IEventListener<T>`, `Publish(ref e)`. Cleared on every scene switch — subscribe again in each scene.
- `ServiceLocator` (`Register`/`Get`/`TryGet`), `Pool<T>` (`Get`/`Release`), `TickTaskManager` (game ticks), `TimedEventManager` (unused), `Debug` (`Log`/`Warning`/`Error`/`Success`, console on backtick, log file).

## Which to use

- `Octree<T>` — bounds-based (world sectors, chunks). `PointOctree<T>` — point-based (drops, sector entities).
- Input: InputPro / `Input`, not `InputManager0`.

## Gotchas

- Scene switches are deferred and wipe EventBus listeners, non-global input actions and resources, lights and `Camera.Main`.
- No `Camera.Main` → no ImGui at all.
- `Node3D.Render` does not recurse; culling drives rendering.
- Engine code must not reference `Spacebox.*`; put reusable features here, game specifics in `Spacebox/`.

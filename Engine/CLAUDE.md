# Engine — architecture notes for agents

Game-independent engine over OpenTK (GL 3.3 core). Namespace `Engine.*`. Must never reference `Spacebox.*`. Coding rules: see root `CLAUDE.md`.

## Folder map

| Path | Purpose |
|---|---|
| `EngineWindow.cs` | Abstract `GameWindow`; owns the main loop and hook methods (see Lifecycle). The only file at the root |
| `Core/` | `Application` (version, platform, `OpenLink`, `Quit()`), `Time` (`Delta` scaled by `TimeSize`, `Total`, FPS/TPS, per-phase timings, ticks, `AlwaysCalculateFPS`), `FrameLimiter`, `Debug` (logging + console; `MirrorToStandardOutput`, `ExecuteCommand`, `MessageLogged` event), `Prof` (profiler scopes), services: `ServiceLocator`, `EventBus`, `Pool`, `ConcurrentPool`, `DisposalManager`, `TickTask`/`TickTaskManager`, `TimedEventManager` (see below) |
| `SceneManagement/` | `Scene`, `SceneManager`, `ErrorScene`, `ParticleSystemEditor`, scene graph `Node3D`/`Transform3D` |
| `Components/` | `Component` base + built-ins (`ModelRendererComponent`, `ColliderComponent`, `BillboardComponent`, …) |
| `Assets/` | `Resources` (typed resource cache, `IResource`), `ResourceFallbacks` |
| `Cameras/` | `Camera` (`Camera.Main` is the active one), `Camera360Base`, `FreeCamera`, `OrbitCamera`, `OrbitViewCamera`, `CameraStatic`, `CameraFrustum` |
| `Graphics/` | Rendering primitives `Shader`, `Texture2D`, `Mesh`, `MeshBuffer`, `Model`; scene FBO `SceneRenderer` (color/normal/depth); `GLState`, `GPUDebug`, `UniformBuffer`, `GlobalUniforms`, `RenderTexture`, `ViewModelProjection`; `Skybox` (assigned to `Lighting.Skybox`), `RenderSpace` (camera-relative origin for large coordinates), `VisualDebug` (debug lines, F4), `FramebufferCapture` |
| `Graphics/Primitives/` | `LineRenderer`, `CubeRenderer`, `SphereRenderer`, `Axes`, `BoundingBoxRenderer` |
| `Graphics/Atlas/` | `TextureAtlas` (skyline packing, any sizes, layers, lazy rebuild), `AtlasLayout`, `AtlasRegion`, `AtlasSprite` (ImGui drawing), `ImagePixels` |
| `Materials/` | `MaterialBase` and built-in materials |
| `Light/` | `LightSystem` (UBO), `Lighting` (ambient, fog, `Lighting.Skybox`), light types, `LightPool` |
| `PostProcessing/` | `PostProcessManager` + effects (`DefaultEffect`, SSAO, Vignette, …) |
| `Physics/` | Bounding volumes, `Ray`, `Collision`, bodies, `BVHCuller` (file `BHVCuller.cs`) |
| `Spatial/` | `Octree<T>`, `PointOctree<T>` (see "Which to use") |
| `GUI/` | ImGui debug `Overlay` (F3) and its elements, `Sprite`, `WorldTextDebug`; ImGui backend `ImGuiController`, `ThemeUIEngine`, window stack `UIManager` |
| `UI/` | `DevGui`, `Canvas`/`Node2D`/`Rect` (2D layer, rarely used) |
| `Input/` | `Input` (raw keyboard/mouse state); `InputPro/` the current input system (`InputManager.Instance`); `InputManager0`/`InputAction0` legacy input — do not use in new code |
| `Audio/` | OpenAL `AudioDevice`, `AudioClip`, `AudioSource`, `AudioMixer` (master + channels Effects/Music/Ambient/UI, `Muted`; set a source's `Channel`), Ogg/Wave loaders |
| `Animation/` | `Animator`, move/rotate tweens |
| `ParticleSystem/` | Emitters and renderer |
| `Commands/` | Debug-console commands (`CommandManager`, `CommandBase`) |
| `Multithreading/` | `WorkerPoolManager`, `MainThread.Run` (run a function on the main thread from any thread and await its result) |
| `Web/` | Dependency-free HTTP/1.1 server: `MiniHttpServer` (TCP, IPv4 + IPv6 listeners, connection limit, timeouts, body limit, optional TLS on the same port via `CertificateProvider` + `PrefixedStream`), `NetworkAddresses.IsLocal`, `Acme/` (`AcmeClient`: Let's Encrypt / RFC 8555 certificates for names and IPs through an `IAcmeChallengeSolver`: `DnsChallengeSolver` over an `IDnsChallenge` (`DuckDnsClient`, `CloudflareDnsClient`) or `HttpChallengeSolver` (HTTP-01 on a port); `AcmeJws` ES256 signing; a new DNS provider is one `IDnsChallenge` class), `HttpProtocol` (parse/write), `HttpRouter` (exact and prefix routes), `HttpRequest`/`HttpResponse` (JSON with camelCase, cookies, query) |
| `Math/` | Byte vectors, colors, `VectorExtensions` |
| `Utils/` | `ObjLoader`, `PixelDataLoader`, `FastNoiseLite`, `GenMesh`, `ShaderHotReloader`, `DirectoryArchive` (zip folders, content hash, safe extract) |
| `AI/` | `StateMachine/` (`State<TContext>`, hierarchical `StateMachine<TContext>`), `Pathfinding/` (`INavigationGrid`, `VoxelLine` DDA, `VoxelPathfinder` A*, `PathRequest`, `PathScheduler` with per-frame node budget) — main thread only |
| `Networking/` | `NetworkManager` (modes, peers, send), `Transport/` (`INetTransport`, `LiteNetTransport`), `Serialization/` (`NetWriter`, `NetReader`, `NetSerializer`), `Messages/`, `Objects/` (`NetworkObject`, `NetworkBehaviour`, `NetworkVariable<T>`, `NetworkSpawner`, `ReplicationSystem`), `Rpc/` (`[Rpc]`, `RpcSystem`), `Behaviours/NetworkTransform`, `Transfer/` (`NetTransferService` = `NetworkManager.Transfers`, chunked blob downloads), `Query/` (connectionless server info: `ServerQueryResponder` = `NetworkManager.Query`, `ServerQueryClient` over an `IQuerySocket` for pings, LAN broadcast and icon download) |
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
- Offscreen rendering: `Graphics/RenderTexture.Render(camera, draw)` binds its FBO, swaps `Camera.Main`, pushes `GlobalUniforms` and restores everything; show it with `ImGui.Image(ColorTexture, size, (0,1), (1,0))`. `OrbitViewCamera` is a non-main orbit camera (`Orbit`, `Zoom`, `Frame(center, radius)`).
- G-buffer: the scene FBO's attachment 1 holds view-space normals (`rgb`) and an AO mask (`a`: 0 none, 0.5 view-model, 1 world). Only materials with `WritesGBuffer = true` (Opaque/Cutout) write it; their shader must write `location = 1` (game include `includes/gbuffer.glsl`). A first-person model drawn with its own projection calls `ViewModelProjection.Set(projection)` each frame (reset before every scene render).
- SSAO: `PostProcessing/Ssao/SsaoEffect` (+ `SsaoSettings`, `SsaoPreset`, `SsaoKernel`, `SsaoTargets`, `SsaoProjection`); shaders are injected via `SsaoShaders`, settings via `Configure`. Effects that need scene depth/normals get `EngineWindow.SceneRenderer`. Upload uniform arrays with `Shader.SetVector3Array`.
- Color grading: `PostProcessing/ColorGrading/ColorGradingEffect` (brightness/gamma, contrast, saturation, film grain; `Configure(ColorGradingSettings)`, disabled while neutral, runs even when post processing is off). `Time.AlwaysCalculateFPS` keeps `Time.FPS` updated without the F3 overlay. `PostProcessing/CameraFeed/CameraFeedEffect`: security-camera look, shown while `Active` and `Intensity > 0`.
- `GLState` and `Texture2D.Use` cache GL state: code that changes blend/texture state with raw GL calls must restore it or reset the cache (`Texture2D.ResetBindingCache`, done after ImGui).

## GUI and input

- GUI is ImGui (ImGui.NET) via `ImGuiController`. `UIManager` keeps a stack of named windows (`Open`, `Toggle`, `CloseTop`, `IsOpen`); gate gameplay input on `IsUIMode`. The debug console is the `"debug"` window.
- ImGui style is global: a scene with its own look captures `GUI/ImGuiStyleSnapshot.Capture()` on load, applies its theme only around its own GUI pass and calls `Restore()` after it and on unload.
- `InputManager.LoadConfiguration` keeps the existing `InputAction` objects (and their `Subscribe` callbacks) and only replaces their bindings, so reloading the key config is safe at any time.
- `ParticleSystem.GloballyEnabled` hides and stops all particle systems (the particle setting). Post-processing effects that must always run (the passthrough `DefaultEffect`) override `IsRequired`; `PostProcessManager.Enabled = false` skips only the others.
- Input: named actions → `InputPro.InputManager.Instance` (JSON bindings, remappable); raw state → `Input.IsKey` / `IsKeyDown` / `Input.Mouse`. `InputManager0`/`InputAction0` are legacy (still used for engine F-keys).

## Threading and services

- GL calls only on the main thread (buffer/texture/shader creation **and** disposal).
- Static systems never create GPU objects in static constructors or field initializers: keep their bookkeeping GL-free and create GPU objects in an explicit `InitializeGpu()` called from `EngineWindow.OnLoad` (`ResourceFallbacks`, `LightSystem`, `VisualDebug`). This keeps them testable (`Engine.Tests` covers `SceneManager`, `Resources`, `Scene`) and prevents GL calls from worker threads that first touch a type.
- `WorkerPoolManager.Enqueue(action, Priority.High|Medium|Low, token)` returns a `Task`; named dedicated workers via `AddDedicatedWorker` / `EnqueueDedicated`.
- Main-thread marshalling inside the engine: `EngineWindow`'s main-thread action queue. (The game's `MainThreadDispatcher` lives in `Spacebox`, not here.)
- `DisposalManager`: thread-safe queue to defer GL disposal from worker threads; processed by the game scene.
- `EventBus`: struct events implementing `IEvent`, listeners `IEventListener<T>`, `Publish(ref e)`. Cleared on every scene switch — subscribe again in each scene.
- `ServiceLocator` (`Register`/`Get`/`TryGet`), `Pool<T>` (`Get`/`Release`), `ConcurrentPool<T>` (thread-safe `Rent`/`Return` with reset and a retain limit, for buffers shared by workers and the main thread), `TickTaskManager` (game ticks), `TimedEventManager` (unused), `Debug` (`Log`/`Warning`/`Error`/`Success`, console on backtick, log file).

## Headless mode

- `Application.EnableHeadless()` (before the window is created; the game calls it for `--server`): `EngineWindow` skips rendering, post-processing, scene renderer, light/debug GPU init and `GlobalUniforms`; ImGui keeps a context and frames but draws nothing; `AudioDevice` opens no OpenAL device and `AudioClip`/`AudioSource`/`AudioListener` do nothing; `Texture2D` from files is a 1x1 CPU placeholder with `Handle` 0, other textures keep CPU pixels only; `Shader` does not compile; `MeshBuffer`/`Mesh`/`UniformBuffer`/`ParticleRenderer` create no GL objects; `TextureAtlas` registers images as 1x1 placeholders. New GPU or audio code must keep working with handle 0 / `Application.IsHeadless`.

## Which to use

- `Octree<T>` — bounds-based (world sectors, chunks). `PointOctree<T>` — point-based (drops, sector entities).
- Input: InputPro / `Input`, not `InputManager0`.

## Gotchas

- Scene switches are deferred and wipe EventBus listeners, non-global input actions and resources, lights and `Camera.Main`.
- No `Camera.Main` → no ImGui at all.
- `Node3D.Render` does not recurse; culling drives rendering.
- Engine code must not reference `Spacebox.*`; put reusable features here, game specifics in `Spacebox/`.

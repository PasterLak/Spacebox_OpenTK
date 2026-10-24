# Graphics backend

The engine talks to the GPU through one interface, `IGraphicsDevice` (`Engine/Graphics/Rhi/`). OpenGL through OpenTK is the only backend today (`Engine/Graphics/Rhi/OpenGL/OpenGlDevice`). The goal is that a second backend (Silk.NET with Vulkan, OpenGL ES for Android) can be added without touching the game.

## Architecture

```
Spacebox (game)  ->  engine classes (Texture2D, Shader, MeshBuffer, RenderTexture, SceneRenderer,
                     CanvasRenderer, PostProcessManager, materials, particles, GLState facade)
                 ->  Gpu.Device : IGraphicsDevice
                 ->  OpenGlDevice (OpenTK)  |  NullGraphicsDevice (headless, tests)
```

- `Gpu.Device` is the active device. It is a `NullGraphicsDevice` (every call is a no-op, every handle is 0) until `EngineWindow` installs `new OpenGlDevice()` in its constructor (not in headless mode) and calls `ResetState()` so the GL state matches the cache. Tests swap the device with `Gpu.Install(device)`, which returns the previous one.
- Handles are `readonly record struct`s around an `int`: `TextureHandle`, `BufferHandle`, `VertexArrayHandle`, `ProgramHandle`, `FramebufferHandle` (`Default` = the window), `RenderbufferHandle`, `QueryHandle`. They never allocate. Existing public `int` properties (`Texture2D.Handle`, `MeshBuffer.VAO`, `RenderTexture.ColorTexture`, `SceneRenderer.ColorTexture`, ...) stay and expose the same id, so `TextureHandleRect` and old code keep working.
- All enums in the public surface belong to the engine: `TextureType`, `TextureFormat`, `TextureFilter`, `TextureWrap`, `CubeFace`, `BufferKind`, `BufferUsage`, `VertexComponentType`, `IndexType`, `PrimitiveTopology`, `BlendFactor`, `CullFace`, `CompareFunction`, `FillMode`, `ClearTargets`, `AttachmentPoint`, `DebugObjectKind`. `GlEnumMap` translates them for OpenGL; `GlEnumMapTests` fails when a new enum value has no mapping.
- `SamplerState` describes filters, wrap modes and max mip level; `TextureFormat` also fixes the CPU data layout (8-bit formats take bytes, float and depth formats take floats; `TextureFormatInfo`).
- The API is OpenGL-shaped on purpose (bind, then draw; uniforms by location on the current program; `BufferData` of an index buffer is recorded in the bound vertex array). It takes `ReadOnlySpan<byte>` for uploads; `GraphicsDeviceExtensions` adds typed overloads (`ReadOnlySpan<float>`, `uint`, structs) without copies.
- State cache: `GpuStateTracker` (GL-free, tested) remembers depth test/write, blend and blend function, culling and cull face, per-attachment colour writes, the active texture unit, texture bindings per unit and type, and the current program. The OpenGL device skips calls that would not change anything. `GLState` is now a thin facade over the device with engine enums. Code that still changes this state with raw GL must invalidate it: `Texture2D.ResetBindingCache()` / `Gpu.Device.InvalidateTextureBindings()`, `GLState.InvalidateBlendFunc()`, `Gpu.Device.InvalidateProgram()`.
- Not cached (always forwarded): framebuffer and vertex array bindings, viewport, scissor, fill mode, depth function, depth bias, line width.
- Debug output, groups, markers and object labels go through the device (`GPUDebug` only keeps the on/off switch).

Hot paths: one interface call per GL call, no allocations; `MeshBuffer.DrawIndexed`, `DrawArrays` and `UploadVertices` cover the common bind-draw-unbind pattern.

## Adding a backend

1. Implement `IGraphicsDevice` in a new folder next to `OpenGL/` (for example `Engine/Graphics/Rhi/SilkVulkan/`). Keep backend types (Silk.NET, OpenTK) inside that folder only.
2. Map every engine enum in a `*EnumMap` class and copy `GlEnumMapTests` for it.
3. Reuse `GpuStateTracker` for redundant-state filtering, or keep the equivalent pipeline-state cache of the new API.
4. Shaders: `CreateProgram(ShaderSources)` receives GLSL 330 source after `#include` expansion. A Vulkan backend has to compile it to SPIR-V (glslang, the same compiler the shader tests use) and map uniforms by name to push constants or a uniform buffer; `GetActiveUniforms` must return stable locations for that mapping.
5. Window and context creation still come from OpenTK's `GameWindow` in `EngineWindow`; a new backend also needs its own window/surface bootstrap and must install the device before `OnLoad`.
6. Run the game with the agent API and compare screenshots against the OpenGL backend (menu, world, SSAO and F10 debug views, particles, held item, UI, signs, GameSet editor).

## Remaining direct OpenGL use

Lines with a `GL.` call outside `Engine/Graphics/Rhi/OpenGL/` (comments excluded), counted with a script over `Engine/` and `Spacebox/`:

| File | `GL.` lines |
|---|---|
| `Engine/Graphics/Headless/NoOpGraphicsBindings.cs` | 1 |
| **Total** | **1 line in 1 file** |

Before this change there were 543 lines in 48 files (`ImGuiController` alone had 117; ImGui was removed on `ui-0.1.5` in the meantime). Everything else (core resources, post processing, canvas UI, particles, debug drawing, occlusion queries, game renderers and test scenes) now goes through `Gpu.Device`.

What still ties the engine to OpenTK/OpenGL:

- `NoOpGraphicsBindings` loads no-op GL entry points for the headless server window; it goes away when headless mode stops creating a GL window.
- Window, context, input and monitors come from OpenTK's `GameWindow`/GLFW in `EngineWindow` (not part of this layer).
- Shaders are GLSL 330 files; a Vulkan backend has to translate them (see step 4 above).
- Public APIs that expose ids as `int` (`Texture2D.Handle`, `TextureHandleRect`, `PostProcessEffect.Apply(int inputTexture, int outputFbo, ...)`, `SceneRenderer.ColorTexture`) work on every backend with integer ids; switch them to the handle structs when a backend needs something else.
- `OpenTK.Mathematics` types (`Vector3`, `Matrix4`, `Color4`, `Box2i`) appear in the interface; they are plain math structs and do not depend on OpenGL.

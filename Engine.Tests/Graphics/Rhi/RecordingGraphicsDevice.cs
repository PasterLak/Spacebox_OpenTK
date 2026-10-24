using Engine.Graphics.Rhi;
using OpenTK.Mathematics;

namespace Engine.Tests.Graphics.Rhi;

public sealed class RecordingGraphicsDevice : IGraphicsDevice
{
    private readonly GpuStateTracker _state = new GpuStateTracker();
    private int _nextId = 1;
    private FramebufferHandle _framebuffer;
    private Box2i _viewport = new Box2i(0, 0, 640, 360);

    public List<string> Calls { get; } = new List<string>();
    public Dictionary<string, int> Uniforms { get; } = new Dictionary<string, int>();

    public GraphicsDeviceInfo Info { get; } = new GraphicsDeviceInfo("Recording", "Test", "Test", "1", "1", 16);

    public int Count(string prefix) => Calls.Count(c => c.StartsWith(prefix, StringComparison.Ordinal));

    public void Clear() => Calls.Clear();

    private int Next() => _nextId++;

    private void Record(string call) => Calls.Add(call);

    public void ResetState() { _state.Reset(); Record("ResetState"); }
    public void InvalidateTextureBindings() => _state.InvalidateTextures();
    public void InvalidateBlendFunc() => _state.InvalidateBlendFunc();
    public void InvalidateProgram() => _state.InvalidateProgram();

    public BufferHandle CreateBuffer() { var h = new BufferHandle(Next()); Record($"CreateBuffer {h.Id}"); return h; }
    public void DeleteBuffer(BufferHandle buffer) => Record($"DeleteBuffer {buffer.Id}");
    public void BindBuffer(BufferKind kind, BufferHandle buffer) => Record($"BindBuffer {kind} {buffer.Id}");
    public void BufferData(BufferKind kind, BufferHandle buffer, ReadOnlySpan<byte> data, BufferUsage usage) => Record($"BufferData {kind} {buffer.Id} {data.Length} {usage}");
    public void AllocateBuffer(BufferKind kind, BufferHandle buffer, int sizeInBytes, BufferUsage usage) => Record($"AllocateBuffer {kind} {buffer.Id} {sizeInBytes} {usage}");
    public void BufferSubData(BufferKind kind, BufferHandle buffer, int offsetInBytes, ReadOnlySpan<byte> data) => Record($"BufferSubData {kind} {buffer.Id} {offsetInBytes} {data.Length}");
    public void BindBufferBase(BufferKind kind, int binding, BufferHandle buffer) => Record($"BindBufferBase {kind} {binding} {buffer.Id}");

    public VertexArrayHandle CreateVertexArray() { var h = new VertexArrayHandle(Next()); Record($"CreateVertexArray {h.Id}"); return h; }
    public void DeleteVertexArray(VertexArrayHandle vertexArray) => Record($"DeleteVertexArray {vertexArray.Id}");
    public void BindVertexArray(VertexArrayHandle vertexArray) => Record($"BindVertexArray {vertexArray.Id}");
    public void SetVertexAttribute(int index, int components, VertexComponentType type, bool normalized, int stride, int offset) => Record($"SetVertexAttribute {index} {components} {type} {normalized} {stride} {offset}");
    public void SetVertexAttributeDivisor(int index, int divisor) => Record($"SetVertexAttributeDivisor {index} {divisor}");

    public void Draw(PrimitiveTopology topology, int first, int vertexCount) => Record($"Draw {topology} {first} {vertexCount}");
    public void DrawIndexed(PrimitiveTopology topology, int indexCount, IndexType indexType, int indexOffsetInBytes) => Record($"DrawIndexed {topology} {indexCount} {indexType} {indexOffsetInBytes}");
    public void DrawInstanced(PrimitiveTopology topology, int first, int vertexCount, int instanceCount) => Record($"DrawInstanced {topology} {first} {vertexCount} {instanceCount}");
    public void DrawIndexedInstanced(PrimitiveTopology topology, int indexCount, IndexType indexType, int indexOffsetInBytes, int instanceCount) => Record($"DrawIndexedInstanced {topology} {indexCount} {indexType} {indexOffsetInBytes} {instanceCount}");

    public TextureHandle CreateTexture(TextureType type) { var h = new TextureHandle(Next()); Record($"CreateTexture {type} {h.Id}"); return h; }
    public void DeleteTexture(TextureHandle texture) { _state.ForgetTexture(texture.Id); Record($"DeleteTexture {texture.Id}"); }
    public void BindTexture(int unit, TextureType type, TextureHandle texture)
    {
        _state.SetActiveUnit(unit);
        if (_state.SetTexture(unit, type, texture.Id)) Record($"BindTexture {unit} {type} {texture.Id}");
    }
    public void SetTextureData2D(TextureHandle texture, int level, TextureFormat format, int width, int height, ReadOnlySpan<byte> data) => Record($"SetTextureData2D {texture.Id} {level} {format} {width}x{height} {data.Length}");
    public void UpdateTexture2D(TextureHandle texture, int level, int x, int y, int width, int height, TextureFormat format, ReadOnlySpan<byte> data) => Record($"UpdateTexture2D {texture.Id} {level} {x},{y} {width}x{height} {format} {data.Length}");
    public void SetTextureDataCube(TextureHandle texture, CubeFace face, TextureFormat format, int size, ReadOnlySpan<byte> data) => Record($"SetTextureDataCube {texture.Id} {face} {format} {size} {data.Length}");
    public void SetTextureDataArray(TextureHandle texture, int level, TextureFormat format, int width, int height, int layers, ReadOnlySpan<byte> data) => Record($"SetTextureDataArray {texture.Id} {level} {format} {width}x{height}x{layers} {data.Length}");
    public void UpdateTextureArrayLayer(TextureHandle texture, int level, int layer, int width, int height, TextureFormat format, ReadOnlySpan<byte> data) => Record($"UpdateTextureArrayLayer {texture.Id} {level} {layer} {width}x{height} {format} {data.Length}");
    public void SetSampler(TextureHandle texture, TextureType type, in SamplerState sampler) => Record($"SetSampler {texture.Id} {sampler.MinFilter} {sampler.MagFilter} {sampler.WrapS} {sampler.MaxLevel}");
    public void GenerateMipmaps(TextureHandle texture, TextureType type) => Record($"GenerateMipmaps {texture.Id}");
    public void ReadTexture2D(TextureHandle texture, int level, TextureFormat format, Span<byte> destination) => Record($"ReadTexture2D {texture.Id} {format} {destination.Length}");

    public ProgramHandle CreateProgram(in ShaderSources sources) { var h = new ProgramHandle(Next()); Record($"CreateProgram {h.Id}"); return h; }
    public void DeleteProgram(ProgramHandle program) { _state.ForgetProgram(program.Id); Record($"DeleteProgram {program.Id}"); }
    public void UseProgram(ProgramHandle program)
    {
        if (_state.SetProgram(program.Id)) Record($"UseProgram {program.Id}");
    }
    public void GetActiveUniforms(ProgramHandle program, IDictionary<string, int> locations)
    {
        foreach (var pair in Uniforms) locations[pair.Key] = pair.Value;
    }
    public int GetUniformBlockCount(ProgramHandle program) => 0;
    public string GetUniformBlockName(ProgramHandle program, int index) => "";
    public void SetUniformBlockBinding(ProgramHandle program, int index, int binding) => Record($"SetUniformBlockBinding {program.Id} {index} {binding}");
    public int GetAttributeLocation(ProgramHandle program, string name) => -1;

    public void SetUniform(int location, int value) => Record($"SetUniform {location} {value}");
    public void SetUniform(int location, float value) => Record($"SetUniform {location} {value}");
    public void SetUniform(int location, Vector2 value) => Record($"SetUniform {location} {value}");
    public void SetUniform(int location, Vector3 value) => Record($"SetUniform {location} {value}");
    public void SetUniform(int location, Vector4 value) => Record($"SetUniform {location} {value}");
    public void SetUniform(int location, Color4 value) => Record($"SetUniform {location} {value}");
    public void SetUniform(int location, in Matrix3 value, bool transpose) => Record($"SetUniformMatrix3 {location} {transpose}");
    public void SetUniform(int location, in Matrix4 value, bool transpose) => Record($"SetUniformMatrix4 {location} {transpose}");
    public void SetUniform(int location, ReadOnlySpan<Vector3> values) => Record($"SetUniformArray {location} {values.Length}");

    public FramebufferHandle CreateFramebuffer() { var h = new FramebufferHandle(Next()); Record($"CreateFramebuffer {h.Id}"); return h; }
    public void DeleteFramebuffer(FramebufferHandle framebuffer) => Record($"DeleteFramebuffer {framebuffer.Id}");
    public void BindFramebuffer(FramebufferHandle framebuffer) { _framebuffer = framebuffer; Record($"BindFramebuffer {framebuffer.Id}"); }
    public FramebufferHandle GetBoundFramebuffer() => _framebuffer;
    public void AttachTexture(AttachmentPoint attachment, TextureHandle texture, int level) => Record($"AttachTexture {attachment} {texture.Id}");
    public RenderbufferHandle CreateRenderbuffer(TextureFormat format, int width, int height) { var h = new RenderbufferHandle(Next()); Record($"CreateRenderbuffer {format} {width}x{height} {h.Id}"); return h; }
    public void DeleteRenderbuffer(RenderbufferHandle renderbuffer) => Record($"DeleteRenderbuffer {renderbuffer.Id}");
    public void AttachRenderbuffer(AttachmentPoint attachment, RenderbufferHandle renderbuffer) => Record($"AttachRenderbuffer {attachment} {renderbuffer.Id}");
    public void SetDrawBuffers(int colorAttachmentCount) => Record($"SetDrawBuffers {colorAttachmentCount}");
    public bool IsFramebufferComplete() => true;
    public void ReadPixels(int x, int y, int width, int height, TextureFormat format, Span<byte> destination) => Record($"ReadPixels {x},{y} {width}x{height} {format} {destination.Length}");

    public void SetClearColor(Color4 color) => Record("SetClearColor");
    public void Clear(ClearTargets targets) => Record($"Clear {targets}");
    public void ClearColorAttachment(int attachment, Color4 color) => Record($"ClearColorAttachment {attachment}");

    public void SetViewport(int x, int y, int width, int height) { _viewport = new Box2i(x, y, x + width, y + height); Record($"SetViewport {x},{y} {width}x{height}"); }
    public Box2i GetViewport() => _viewport;
    public void SetScissorTest(bool enabled) => Record($"SetScissorTest {enabled}");
    public void SetScissor(int x, int y, int width, int height) => Record($"SetScissor {x},{y} {width}x{height}");
    public void SetDepthTest(bool enabled) { if (_state.SetDepthTest(enabled)) Record($"SetDepthTest {enabled}"); }
    public void SetDepthWrite(bool enabled) { if (_state.SetDepthWrite(enabled)) Record($"SetDepthWrite {enabled}"); }
    public void SetDepthFunction(CompareFunction function) => Record($"SetDepthFunction {function}");
    public void SetBlend(bool enabled) { if (_state.SetBlend(enabled)) Record($"SetBlend {enabled}"); }
    public void SetBlendFunction(BlendFactor source, BlendFactor destination) { if (_state.SetBlendFunction(source, destination)) Record($"SetBlendFunction {source} {destination}"); }
    public void SetCulling(bool enabled) { if (_state.SetCulling(enabled)) Record($"SetCulling {enabled}"); }
    public void SetCullFace(CullFace face) { if (_state.SetCullFace(face)) Record($"SetCullFace {face}"); }
    public void SetColorWrite(int attachment, bool enabled) { if (_state.SetColorWrite(attachment, enabled)) Record($"SetColorWrite {attachment} {enabled}"); }
    public void SetFillMode(FillMode mode) => Record($"SetFillMode {mode}");
    public void SetLineWidth(float width) => Record($"SetLineWidth {width}");
    public void SetDepthBias(bool enabled, float factor, float units) => Record($"SetDepthBias {enabled} {factor} {units}");

    public bool InitializeDebugOutput(bool installCallback, bool synchronous) => false;
    public void PushDebugGroup(string name) { }
    public void PopDebugGroup() { }
    public void InsertDebugMarker(string name) { }
    public void SetDebugLabel(DebugObjectKind kind, int id, string label) { }
}

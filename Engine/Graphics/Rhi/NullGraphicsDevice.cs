using OpenTK.Mathematics;

namespace Engine.Graphics.Rhi;

public sealed class NullGraphicsDevice : IGraphicsDevice
{
    public static readonly NullGraphicsDevice Instance = new();

    private NullGraphicsDevice()
    {
    }

    public GraphicsDeviceInfo Info => GraphicsDeviceInfo.None;

    public void ResetState() { }
    public void InvalidateTextureBindings() { }
    public void InvalidateBlendFunc() { }
    public void InvalidateProgram() { }

    public BufferHandle CreateBuffer() => BufferHandle.None;
    public void DeleteBuffer(BufferHandle buffer) { }
    public void BindBuffer(BufferKind kind, BufferHandle buffer) { }
    public void BufferData(BufferKind kind, BufferHandle buffer, ReadOnlySpan<byte> data, BufferUsage usage) { }
    public void AllocateBuffer(BufferKind kind, BufferHandle buffer, int sizeInBytes, BufferUsage usage) { }
    public void BufferSubData(BufferKind kind, BufferHandle buffer, int offsetInBytes, ReadOnlySpan<byte> data) { }
    public void BindBufferBase(BufferKind kind, int binding, BufferHandle buffer) { }

    public VertexArrayHandle CreateVertexArray() => VertexArrayHandle.None;
    public void DeleteVertexArray(VertexArrayHandle vertexArray) { }
    public void BindVertexArray(VertexArrayHandle vertexArray) { }
    public void SetVertexAttribute(int index, int components, VertexComponentType type, bool normalized, int stride, int offset) { }
    public void SetVertexAttributeDivisor(int index, int divisor) { }

    public void Draw(PrimitiveTopology topology, int first, int vertexCount) { }
    public void DrawIndexed(PrimitiveTopology topology, int indexCount, IndexType indexType, int indexOffsetInBytes) { }
    public void DrawInstanced(PrimitiveTopology topology, int first, int vertexCount, int instanceCount) { }
    public void DrawIndexedInstanced(PrimitiveTopology topology, int indexCount, IndexType indexType, int indexOffsetInBytes, int instanceCount) { }

    public TextureHandle CreateTexture(TextureType type) => TextureHandle.None;
    public void DeleteTexture(TextureHandle texture) { }
    public void BindTexture(int unit, TextureType type, TextureHandle texture) { }
    public void SetTextureData2D(TextureHandle texture, int level, TextureFormat format, int width, int height, ReadOnlySpan<byte> data) { }
    public void UpdateTexture2D(TextureHandle texture, int level, int x, int y, int width, int height, TextureFormat format, ReadOnlySpan<byte> data) { }
    public void SetTextureDataCube(TextureHandle texture, CubeFace face, TextureFormat format, int size, ReadOnlySpan<byte> data) { }
    public void SetTextureDataArray(TextureHandle texture, int level, TextureFormat format, int width, int height, int layers, ReadOnlySpan<byte> data) { }
    public void UpdateTextureArrayLayer(TextureHandle texture, int level, int layer, int width, int height, TextureFormat format, ReadOnlySpan<byte> data) { }
    public void SetSampler(TextureHandle texture, TextureType type, in SamplerState sampler) { }
    public void GenerateMipmaps(TextureHandle texture, TextureType type) { }
    public void ReadTexture2D(TextureHandle texture, int level, TextureFormat format, Span<byte> destination) { }

    public ProgramHandle CreateProgram(in ShaderSources sources) => ProgramHandle.None;
    public void DeleteProgram(ProgramHandle program) { }
    public void UseProgram(ProgramHandle program) { }
    public void GetActiveUniforms(ProgramHandle program, IDictionary<string, int> locations) { }
    public int GetUniformBlockCount(ProgramHandle program) => 0;
    public string GetUniformBlockName(ProgramHandle program, int index) => "";
    public void SetUniformBlockBinding(ProgramHandle program, int index, int binding) { }
    public int GetAttributeLocation(ProgramHandle program, string name) => -1;

    public void SetUniform(int location, int value) { }
    public void SetUniform(int location, float value) { }
    public void SetUniform(int location, Vector2 value) { }
    public void SetUniform(int location, Vector3 value) { }
    public void SetUniform(int location, Vector4 value) { }
    public void SetUniform(int location, Color4 value) { }
    public void SetUniform(int location, in Matrix3 value, bool transpose) { }
    public void SetUniform(int location, in Matrix4 value, bool transpose) { }
    public void SetUniform(int location, ReadOnlySpan<Vector3> values) { }

    public FramebufferHandle CreateFramebuffer() => FramebufferHandle.Default;
    public void DeleteFramebuffer(FramebufferHandle framebuffer) { }
    public void BindFramebuffer(FramebufferHandle framebuffer) { }
    public FramebufferHandle GetBoundFramebuffer() => FramebufferHandle.Default;
    public void AttachTexture(AttachmentPoint attachment, TextureHandle texture, int level) { }
    public RenderbufferHandle CreateRenderbuffer(TextureFormat format, int width, int height) => RenderbufferHandle.None;
    public void DeleteRenderbuffer(RenderbufferHandle renderbuffer) { }
    public void AttachRenderbuffer(AttachmentPoint attachment, RenderbufferHandle renderbuffer) { }
    public void SetDrawBuffers(int colorAttachmentCount) { }
    public bool IsFramebufferComplete() => false;
    public void ReadPixels(int x, int y, int width, int height, TextureFormat format, Span<byte> destination) { }

    public void SetClearColor(Color4 color) { }
    public void Clear(ClearTargets targets) { }
    public void ClearColorAttachment(int attachment, Color4 color) { }

    public void SetViewport(int x, int y, int width, int height) { }
    public Box2i GetViewport() => default;
    public void SetScissorTest(bool enabled) { }
    public void SetScissor(int x, int y, int width, int height) { }
    public void SetDepthTest(bool enabled) { }
    public void SetDepthWrite(bool enabled) { }
    public void SetDepthFunction(CompareFunction function) { }
    public void SetBlend(bool enabled) { }
    public void SetBlendFunction(BlendFactor source, BlendFactor destination) { }
    public void SetCulling(bool enabled) { }
    public void SetCullFace(CullFace face) { }
    public void SetColorWrite(int attachment, bool enabled) { }
    public void SetFillMode(FillMode mode) { }
    public void SetLineWidth(float width) { }
    public void SetDepthBias(bool enabled, float factor, float units) { }

    public bool InitializeDebugOutput(bool installCallback, bool synchronous) => false;
    public void PushDebugGroup(string name) { }
    public void PopDebugGroup() { }
    public void InsertDebugMarker(string name) { }
    public void SetDebugLabel(DebugObjectKind kind, int id, string label) { }
}

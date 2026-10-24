using OpenTK.Mathematics;

namespace Engine.Graphics.Rhi;

public interface IGraphicsDevice
{
    GraphicsDeviceInfo Info { get; }

    void ResetState();
    void InvalidateTextureBindings();
    void InvalidateBlendFunc();
    void InvalidateProgram();

    BufferHandle CreateBuffer();
    void DeleteBuffer(BufferHandle buffer);
    void BindBuffer(BufferKind kind, BufferHandle buffer);
    void BufferData(BufferKind kind, BufferHandle buffer, ReadOnlySpan<byte> data, BufferUsage usage);
    void AllocateBuffer(BufferKind kind, BufferHandle buffer, int sizeInBytes, BufferUsage usage);
    void BufferSubData(BufferKind kind, BufferHandle buffer, int offsetInBytes, ReadOnlySpan<byte> data);
    void BindBufferBase(BufferKind kind, int binding, BufferHandle buffer);

    VertexArrayHandle CreateVertexArray();
    void DeleteVertexArray(VertexArrayHandle vertexArray);
    void BindVertexArray(VertexArrayHandle vertexArray);
    void SetVertexAttribute(int index, int components, VertexComponentType type, bool normalized, int stride, int offset);
    void SetVertexAttributeDivisor(int index, int divisor);

    void Draw(PrimitiveTopology topology, int first, int vertexCount);
    void DrawIndexed(PrimitiveTopology topology, int indexCount, IndexType indexType, int indexOffsetInBytes);
    void DrawInstanced(PrimitiveTopology topology, int first, int vertexCount, int instanceCount);
    void DrawIndexedInstanced(PrimitiveTopology topology, int indexCount, IndexType indexType, int indexOffsetInBytes, int instanceCount);

    TextureHandle CreateTexture(TextureType type);
    void DeleteTexture(TextureHandle texture);
    void BindTexture(int unit, TextureType type, TextureHandle texture);
    void SetTextureData2D(TextureHandle texture, int level, TextureFormat format, int width, int height, ReadOnlySpan<byte> data);
    void UpdateTexture2D(TextureHandle texture, int level, int x, int y, int width, int height, TextureFormat format, ReadOnlySpan<byte> data);
    void SetTextureDataCube(TextureHandle texture, CubeFace face, TextureFormat format, int size, ReadOnlySpan<byte> data);
    void SetTextureDataArray(TextureHandle texture, int level, TextureFormat format, int width, int height, int layers, ReadOnlySpan<byte> data);
    void UpdateTextureArrayLayer(TextureHandle texture, int level, int layer, int width, int height, TextureFormat format, ReadOnlySpan<byte> data);
    void SetSampler(TextureHandle texture, TextureType type, in SamplerState sampler);
    void GenerateMipmaps(TextureHandle texture, TextureType type);
    void ReadTexture2D(TextureHandle texture, int level, TextureFormat format, Span<byte> destination);

    ProgramHandle CreateProgram(in ShaderSources sources);
    void DeleteProgram(ProgramHandle program);
    void UseProgram(ProgramHandle program);
    void GetActiveUniforms(ProgramHandle program, IDictionary<string, int> locations);
    int GetUniformBlockCount(ProgramHandle program);
    string GetUniformBlockName(ProgramHandle program, int index);
    void SetUniformBlockBinding(ProgramHandle program, int index, int binding);
    int GetAttributeLocation(ProgramHandle program, string name);

    void SetUniform(int location, int value);
    void SetUniform(int location, float value);
    void SetUniform(int location, Vector2 value);
    void SetUniform(int location, Vector3 value);
    void SetUniform(int location, Vector4 value);
    void SetUniform(int location, Color4 value);
    void SetUniform(int location, in Matrix3 value, bool transpose);
    void SetUniform(int location, in Matrix4 value, bool transpose);
    void SetUniform(int location, ReadOnlySpan<Vector3> values);

    FramebufferHandle CreateFramebuffer();
    void DeleteFramebuffer(FramebufferHandle framebuffer);
    void BindFramebuffer(FramebufferHandle framebuffer);
    FramebufferHandle GetBoundFramebuffer();
    void AttachTexture(AttachmentPoint attachment, TextureHandle texture, int level);
    RenderbufferHandle CreateRenderbuffer(TextureFormat format, int width, int height);
    void DeleteRenderbuffer(RenderbufferHandle renderbuffer);
    void AttachRenderbuffer(AttachmentPoint attachment, RenderbufferHandle renderbuffer);
    void SetDrawBuffers(int colorAttachmentCount);
    bool IsFramebufferComplete();
    void ReadPixels(int x, int y, int width, int height, TextureFormat format, Span<byte> destination);

    void SetClearColor(Color4 color);
    void Clear(ClearTargets targets);
    void ClearColorAttachment(int attachment, Color4 color);

    void SetViewport(int x, int y, int width, int height);
    Box2i GetViewport();
    void SetScissorTest(bool enabled);
    void SetScissor(int x, int y, int width, int height);
    void SetDepthTest(bool enabled);
    void SetDepthWrite(bool enabled);
    void SetDepthFunction(CompareFunction function);
    void SetBlend(bool enabled);
    void SetBlendFunction(BlendFactor source, BlendFactor destination);
    void SetCulling(bool enabled);
    void SetCullFace(CullFace face);
    void SetColorWrite(int attachment, bool enabled);
    void SetFillMode(FillMode mode);
    void SetLineWidth(float width);
    void SetDepthBias(bool enabled, float factor, float units);

    bool InitializeDebugOutput(bool installCallback, bool synchronous);
    void PushDebugGroup(string name);
    void PopDebugGroup();
    void InsertDebugMarker(string name);
    void SetDebugLabel(DebugObjectKind kind, int id, string label);
}

using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.Graphics.Rhi.OpenGL;

public sealed unsafe class OpenGlDevice : IGraphicsDevice
{
    private static readonly DrawBuffersEnum[] DrawBufferTargets =
    {
        DrawBuffersEnum.ColorAttachment0,
        DrawBuffersEnum.ColorAttachment1,
        DrawBuffersEnum.ColorAttachment2,
        DrawBuffersEnum.ColorAttachment3,
        DrawBuffersEnum.ColorAttachment4,
        DrawBuffersEnum.ColorAttachment5,
        DrawBuffersEnum.ColorAttachment6,
        DrawBuffersEnum.ColorAttachment7
    };

    private readonly GpuStateTracker _state = new GpuStateTracker();
    private readonly OpenGlDebugOutput _debug = new OpenGlDebugOutput();

    public OpenGlDevice()
    {
        Info = new GraphicsDeviceInfo(
            "OpenGL",
            GL.GetString(StringName.Renderer) ?? "",
            GL.GetString(StringName.Vendor) ?? "",
            GL.GetString(StringName.Version) ?? "",
            GL.GetString(StringName.ShadingLanguageVersion) ?? "",
            GL.GetInteger(GetPName.MaxCombinedTextureImageUnits));
    }

    public GraphicsDeviceInfo Info { get; }

    public GpuStateTracker State => _state;

    public void ResetState()
    {
        _state.Reset();
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Back);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.Zero);
        for (int i = 0; i < GpuStateTracker.MaxColorAttachments; i++)
            GL.ColorMask(i, true, true, true, true);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.UseProgram(0);
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
    }

    public void InvalidateTextureBindings() => _state.InvalidateTextures();

    public void InvalidateBlendFunc() => _state.InvalidateBlendFunc();

    public void InvalidateProgram() => _state.InvalidateProgram();

    public BufferHandle CreateBuffer() => new BufferHandle(GL.GenBuffer());

    public void DeleteBuffer(BufferHandle buffer)
    {
        if (buffer.IsValid) GL.DeleteBuffer(buffer.Id);
    }

    public void BindBuffer(BufferKind kind, BufferHandle buffer)
    {
        GL.BindBuffer(GlEnumMap.ToGl(kind), buffer.Id);
    }

    public void BufferData(BufferKind kind, BufferHandle buffer, ReadOnlySpan<byte> data, BufferUsage usage)
    {
        var target = GlEnumMap.ToGl(kind);
        GL.BindBuffer(target, buffer.Id);
        fixed (byte* ptr = data)
        {
            GL.BufferData(target, data.Length, (IntPtr)ptr, GlEnumMap.ToGl(usage));
        }
    }

    public void AllocateBuffer(BufferKind kind, BufferHandle buffer, int sizeInBytes, BufferUsage usage)
    {
        var target = GlEnumMap.ToGl(kind);
        GL.BindBuffer(target, buffer.Id);
        GL.BufferData(target, sizeInBytes, IntPtr.Zero, GlEnumMap.ToGl(usage));
    }

    public void BufferSubData(BufferKind kind, BufferHandle buffer, int offsetInBytes, ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return;
        var target = GlEnumMap.ToGl(kind);
        GL.BindBuffer(target, buffer.Id);
        fixed (byte* ptr = data)
        {
            GL.BufferSubData(target, (IntPtr)offsetInBytes, data.Length, (IntPtr)ptr);
        }
    }

    public void BindBufferBase(BufferKind kind, int binding, BufferHandle buffer)
    {
        GL.BindBufferBase(GlEnumMap.ToGlRange(kind), binding, buffer.Id);
    }

    public VertexArrayHandle CreateVertexArray() => new VertexArrayHandle(GL.GenVertexArray());

    public void DeleteVertexArray(VertexArrayHandle vertexArray)
    {
        if (vertexArray.IsValid) GL.DeleteVertexArray(vertexArray.Id);
    }

    public void BindVertexArray(VertexArrayHandle vertexArray)
    {
        GL.BindVertexArray(vertexArray.Id);
    }

    public void SetVertexAttribute(int index, int components, VertexComponentType type, bool normalized, int stride, int offset)
    {
        GL.VertexAttribPointer(index, components, GlEnumMap.ToGl(type), normalized, stride, offset);
        GL.EnableVertexAttribArray(index);
    }

    public void SetVertexAttributeDivisor(int index, int divisor)
    {
        GL.VertexAttribDivisor(index, divisor);
    }

    public void Draw(PrimitiveTopology topology, int first, int vertexCount)
    {
        GL.DrawArrays(GlEnumMap.ToGl(topology), first, vertexCount);
    }

    public void DrawIndexed(PrimitiveTopology topology, int indexCount, IndexType indexType, int indexOffsetInBytes)
    {
        GL.DrawElements(GlEnumMap.ToGl(topology), indexCount, GlEnumMap.ToGl(indexType), (IntPtr)indexOffsetInBytes);
    }

    public void DrawInstanced(PrimitiveTopology topology, int first, int vertexCount, int instanceCount)
    {
        GL.DrawArraysInstanced(GlEnumMap.ToGl(topology), first, vertexCount, instanceCount);
    }

    public void DrawIndexedInstanced(PrimitiveTopology topology, int indexCount, IndexType indexType, int indexOffsetInBytes, int instanceCount)
    {
        GL.DrawElementsInstanced(GlEnumMap.ToGl(topology), indexCount, GlEnumMap.ToGl(indexType), (IntPtr)indexOffsetInBytes, instanceCount);
    }

    public TextureHandle CreateTexture(TextureType type) => new TextureHandle(GL.GenTexture());

    public void DeleteTexture(TextureHandle texture)
    {
        if (!texture.IsValid) return;
        GL.DeleteTexture(texture.Id);
        _state.ForgetTexture(texture.Id);
    }

    public void BindTexture(int unit, TextureType type, TextureHandle texture)
    {
        if (_state.SetActiveUnit(unit)) GL.ActiveTexture(TextureUnit.Texture0 + unit);
        if (_state.SetTexture(unit, type, texture.Id)) GL.BindTexture(GlEnumMap.ToGl(type), texture.Id);
    }

    public void SetTextureData2D(TextureHandle texture, int level, TextureFormat format, int width, int height, ReadOnlySpan<byte> data)
    {
        BindForEdit(TextureType.Texture2D, texture);
        var gl = GlEnumMap.ToGl(format);
        fixed (byte* ptr = data)
        {
            GL.TexImage2D(TextureTarget.Texture2D, level, gl.InternalFormat, width, height, 0, gl.Format, gl.Type, (IntPtr)ptr);
        }
    }

    public void UpdateTexture2D(TextureHandle texture, int level, int x, int y, int width, int height, TextureFormat format, ReadOnlySpan<byte> data)
    {
        BindForEdit(TextureType.Texture2D, texture);
        var gl = GlEnumMap.ToGl(format);
        fixed (byte* ptr = data)
        {
            GL.TexSubImage2D(TextureTarget.Texture2D, level, x, y, width, height, gl.Format, gl.Type, (IntPtr)ptr);
        }
    }

    public void SetTextureDataCube(TextureHandle texture, CubeFace face, TextureFormat format, int size, ReadOnlySpan<byte> data)
    {
        BindForEdit(TextureType.TextureCube, texture);
        var gl = GlEnumMap.ToGl(format);
        fixed (byte* ptr = data)
        {
            GL.TexImage2D(GlEnumMap.ToGl(face), 0, gl.InternalFormat, size, size, 0, gl.Format, gl.Type, (IntPtr)ptr);
        }
    }

    public void SetTextureDataArray(TextureHandle texture, int level, TextureFormat format, int width, int height, int layers, ReadOnlySpan<byte> data)
    {
        BindForEdit(TextureType.Texture2DArray, texture);
        var gl = GlEnumMap.ToGl(format);
        fixed (byte* ptr = data)
        {
            GL.TexImage3D(TextureTarget.Texture2DArray, level, gl.InternalFormat, width, height, layers, 0, gl.Format, gl.Type, (IntPtr)ptr);
        }
    }

    public void UpdateTextureArrayLayer(TextureHandle texture, int level, int layer, int width, int height, TextureFormat format, ReadOnlySpan<byte> data)
    {
        BindForEdit(TextureType.Texture2DArray, texture);
        var gl = GlEnumMap.ToGl(format);
        fixed (byte* ptr = data)
        {
            GL.TexSubImage3D(TextureTarget.Texture2DArray, level, 0, 0, layer, width, height, 1, gl.Format, gl.Type, (IntPtr)ptr);
        }
    }

    public void SetSampler(TextureHandle texture, TextureType type, in SamplerState sampler)
    {
        BindForEdit(type, texture);
        var target = GlEnumMap.ToGl(type);
        GL.TexParameter(target, TextureParameterName.TextureWrapS, GlEnumMap.ToGl(sampler.WrapS));
        GL.TexParameter(target, TextureParameterName.TextureWrapT, GlEnumMap.ToGl(sampler.WrapT));
        if (type != TextureType.Texture2D)
            GL.TexParameter(target, TextureParameterName.TextureWrapR, GlEnumMap.ToGl(sampler.WrapR));
        GL.TexParameter(target, TextureParameterName.TextureMinFilter, GlEnumMap.ToGl(sampler.MinFilter));
        GL.TexParameter(target, TextureParameterName.TextureMagFilter, GlEnumMap.ToGlMag(sampler.MagFilter));
        if (sampler.HasMaxLevel)
            GL.TexParameter(target, TextureParameterName.TextureMaxLevel, sampler.MaxLevel);
    }

    public void GenerateMipmaps(TextureHandle texture, TextureType type)
    {
        BindForEdit(type, texture);
        GL.GenerateMipmap((GenerateMipmapTarget)GlEnumMap.ToGl(type));
    }

    public void ReadTexture2D(TextureHandle texture, int level, TextureFormat format, Span<byte> destination)
    {
        BindForEdit(TextureType.Texture2D, texture);
        var gl = GlEnumMap.ToGl(format);
        fixed (byte* ptr = destination)
        {
            GL.GetTexImage(TextureTarget.Texture2D, level, gl.Format, gl.Type, (IntPtr)ptr);
        }
    }

    public ProgramHandle CreateProgram(in ShaderSources sources)
    {
        int vertex = 0, fragment = 0, geometry = 0;
        try
        {
            vertex = CompileStage(ShaderType.VertexShader, sources.Vertex);
            fragment = CompileStage(ShaderType.FragmentShader, sources.Fragment);
            if (sources.HasGeometry) geometry = CompileStage(ShaderType.GeometryShader, sources.Geometry!);

            int program = GL.CreateProgram();
            if (program == 0) throw new Exception("Program linking failed. Program handle is 0.");
            GL.AttachShader(program, vertex);
            if (geometry != 0) GL.AttachShader(program, geometry);
            GL.AttachShader(program, fragment);
            GL.LinkProgram(program);
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int status);

            GL.DetachShader(program, vertex);
            GL.DetachShader(program, fragment);
            if (geometry != 0) GL.DetachShader(program, geometry);

            if (status != (int)All.True)
            {
                string log = GL.GetProgramInfoLog(program);
                GL.DeleteProgram(program);
                throw new Exception($"[Shader] Error linking program ({program}): {log}");
            }

            return new ProgramHandle(program);
        }
        finally
        {
            if (vertex != 0) GL.DeleteShader(vertex);
            if (fragment != 0) GL.DeleteShader(fragment);
            if (geometry != 0) GL.DeleteShader(geometry);
        }
    }

    public void DeleteProgram(ProgramHandle program)
    {
        if (!program.IsValid) return;
        GL.DeleteProgram(program.Id);
        if (_state.Program == program.Id)
        {
            GL.UseProgram(0);
            _state.SetProgram(0);
        }
    }

    public void UseProgram(ProgramHandle program)
    {
        if (_state.SetProgram(program.Id)) GL.UseProgram(program.Id);
    }

    public void GetActiveUniforms(ProgramHandle program, IDictionary<string, int> locations)
    {
        GL.GetProgram(program.Id, GetProgramParameterName.ActiveUniforms, out int count);
        for (int i = 0; i < count; i++)
        {
            string name = GL.GetActiveUniform(program.Id, i, out _, out _);
            locations[name] = GL.GetUniformLocation(program.Id, name);
        }
    }

    public int GetUniformBlockCount(ProgramHandle program)
    {
        GL.GetProgram(program.Id, GetProgramParameterName.ActiveUniformBlocks, out int count);
        return count;
    }

    public string GetUniformBlockName(ProgramHandle program, int index)
    {
        GL.GetActiveUniformBlockName(program.Id, index, 64, out int length, out string raw);
        return length > 0 ? raw.Substring(0, length) : raw;
    }

    public void SetUniformBlockBinding(ProgramHandle program, int index, int binding)
    {
        GL.UniformBlockBinding(program.Id, index, binding);
    }

    public int GetAttributeLocation(ProgramHandle program, string name) => GL.GetAttribLocation(program.Id, name);

    public void SetUniform(int location, int value) => GL.Uniform1(location, value);

    public void SetUniform(int location, float value) => GL.Uniform1(location, value);

    public void SetUniform(int location, Vector2 value) => GL.Uniform2(location, value);

    public void SetUniform(int location, Vector3 value) => GL.Uniform3(location, value);

    public void SetUniform(int location, Vector4 value) => GL.Uniform4(location, value);

    public void SetUniform(int location, Color4 value) => GL.Uniform4(location, value);

    public void SetUniform(int location, in Matrix3 value, bool transpose)
    {
        var matrix = value;
        GL.UniformMatrix3(location, transpose, ref matrix);
    }

    public void SetUniform(int location, in Matrix4 value, bool transpose)
    {
        var matrix = value;
        GL.UniformMatrix4(location, transpose, ref matrix);
    }

    public void SetUniform(int location, ReadOnlySpan<Vector3> values)
    {
        if (values.IsEmpty) return;
        GL.Uniform3(location, values.Length, ref MemoryMarshal.GetReference(values).X);
    }

    public FramebufferHandle CreateFramebuffer() => new FramebufferHandle(GL.GenFramebuffer());

    public void DeleteFramebuffer(FramebufferHandle framebuffer)
    {
        if (!framebuffer.IsDefault) GL.DeleteFramebuffer(framebuffer.Id);
    }

    public void BindFramebuffer(FramebufferHandle framebuffer)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.Id);
    }

    public FramebufferHandle GetBoundFramebuffer() => new FramebufferHandle(GL.GetInteger(GetPName.FramebufferBinding));

    public void AttachTexture(AttachmentPoint attachment, TextureHandle texture, int level)
    {
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, GlEnumMap.ToGl(attachment), TextureTarget.Texture2D, texture.Id, level);
    }

    public RenderbufferHandle CreateRenderbuffer(TextureFormat format, int width, int height)
    {
        int renderbuffer = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderbuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, GlEnumMap.ToGlRenderbuffer(format), width, height);
        return new RenderbufferHandle(renderbuffer);
    }

    public void DeleteRenderbuffer(RenderbufferHandle renderbuffer)
    {
        if (renderbuffer.IsValid) GL.DeleteRenderbuffer(renderbuffer.Id);
    }

    public void AttachRenderbuffer(AttachmentPoint attachment, RenderbufferHandle renderbuffer)
    {
        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, GlEnumMap.ToGl(attachment), RenderbufferTarget.Renderbuffer, renderbuffer.Id);
    }

    public void SetDrawBuffers(int colorAttachmentCount)
    {
        if (colorAttachmentCount <= 0) GL.DrawBuffer(DrawBufferMode.None);
        else if (colorAttachmentCount == 1) GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        else GL.DrawBuffers(Math.Min(colorAttachmentCount, DrawBufferTargets.Length), DrawBufferTargets);
    }

    public bool IsFramebufferComplete() =>
        GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) == FramebufferErrorCode.FramebufferComplete;

    public void ReadPixels(int x, int y, int width, int height, TextureFormat format, Span<byte> destination)
    {
        var gl = GlEnumMap.ToGl(format);
        GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
        fixed (byte* ptr = destination)
        {
            GL.ReadPixels(x, y, width, height, gl.Format, gl.Type, (IntPtr)ptr);
        }
    }

    public void SetClearColor(Color4 color) => GL.ClearColor(color.R, color.G, color.B, color.A);

    public void Clear(ClearTargets targets) => GL.Clear(GlEnumMap.ToGl(targets));

    public void ClearColorAttachment(int attachment, Color4 color)
    {
        float* value = stackalloc float[4];
        value[0] = color.R;
        value[1] = color.G;
        value[2] = color.B;
        value[3] = color.A;
        GL.ClearBuffer(ClearBuffer.Color, attachment, value);
    }

    public void SetViewport(int x, int y, int width, int height) => GL.Viewport(x, y, width, height);

    public Box2i GetViewport()
    {
        int* viewport = stackalloc int[4];
        GL.GetInteger(GetPName.Viewport, viewport);
        return new Box2i(viewport[0], viewport[1], viewport[0] + viewport[2], viewport[1] + viewport[3]);
    }

    public void SetScissorTest(bool enabled)
    {
        if (enabled) GL.Enable(EnableCap.ScissorTest);
        else GL.Disable(EnableCap.ScissorTest);
    }

    public void SetScissor(int x, int y, int width, int height) => GL.Scissor(x, y, width, height);

    public void SetDepthTest(bool enabled)
    {
        if (!_state.SetDepthTest(enabled)) return;
        if (enabled) GL.Enable(EnableCap.DepthTest);
        else GL.Disable(EnableCap.DepthTest);
    }

    public void SetDepthWrite(bool enabled)
    {
        if (_state.SetDepthWrite(enabled)) GL.DepthMask(enabled);
    }

    public void SetDepthFunction(CompareFunction function) => GL.DepthFunc(GlEnumMap.ToGl(function));

    public void SetBlend(bool enabled)
    {
        if (!_state.SetBlend(enabled)) return;
        if (enabled) GL.Enable(EnableCap.Blend);
        else GL.Disable(EnableCap.Blend);
    }

    public void SetBlendFunction(BlendFactor source, BlendFactor destination)
    {
        if (_state.SetBlendFunction(source, destination))
            GL.BlendFunc(GlEnumMap.ToGl(source), GlEnumMap.ToGl(destination));
    }

    public void SetCulling(bool enabled)
    {
        if (!_state.SetCulling(enabled)) return;
        if (enabled) GL.Enable(EnableCap.CullFace);
        else GL.Disable(EnableCap.CullFace);
    }

    public void SetCullFace(CullFace face)
    {
        if (_state.SetCullFace(face)) GL.CullFace(GlEnumMap.ToGl(face));
    }

    public void SetColorWrite(int attachment, bool enabled)
    {
        if (_state.SetColorWrite(attachment, enabled)) GL.ColorMask(attachment, enabled, enabled, enabled, enabled);
    }

    public void SetFillMode(FillMode mode) => GL.PolygonMode(MaterialFace.FrontAndBack, GlEnumMap.ToGl(mode));

    public void SetLineWidth(float width) => GL.LineWidth(width);

    public void SetDepthBias(bool enabled, float factor, float units)
    {
        if (!enabled)
        {
            GL.Disable(EnableCap.PolygonOffsetFill);
            return;
        }

        GL.Enable(EnableCap.PolygonOffsetFill);
        GL.PolygonOffset(factor, units);
    }

    public QueryHandle CreateOcclusionQuery() => new QueryHandle(GL.GenQuery());

    public void DeleteQuery(QueryHandle query)
    {
        if (query.IsValid) GL.DeleteQuery(query.Id);
    }

    public void BeginOcclusionQuery(QueryHandle query) => GL.BeginQuery(QueryTarget.AnySamplesPassed, query.Id);

    public void EndOcclusionQuery() => GL.EndQuery(QueryTarget.AnySamplesPassed);

    public bool TryGetOcclusionQueryResult(QueryHandle query, out bool anySamplesPassed)
    {
        GL.GetQueryObject(query.Id, GetQueryObjectParam.QueryResultAvailable, out int available);
        if (available == 0)
        {
            anySamplesPassed = true;
            return false;
        }

        GL.GetQueryObject(query.Id, GetQueryObjectParam.QueryResult, out int result);
        anySamplesPassed = result != 0;
        return true;
    }

    public bool InitializeDebugOutput(bool installCallback, bool synchronous) => _debug.Initialize(installCallback, synchronous);

    public void PushDebugGroup(string name) => _debug.PushGroup(name);

    public void PopDebugGroup() => _debug.PopGroup();

    public void InsertDebugMarker(string name) => _debug.Marker(name);

    public void SetDebugLabel(DebugObjectKind kind, int id, string label) => _debug.Label(kind, id, label);

    private void BindForEdit(TextureType type, TextureHandle texture)
    {
        int unit = _state.ActiveUnit;
        if (unit < 0)
        {
            unit = 0;
            _state.SetActiveUnit(0);
            GL.ActiveTexture(TextureUnit.Texture0);
        }

        if (_state.SetTexture(unit, type, texture.Id)) GL.BindTexture(GlEnumMap.ToGl(type), texture.Id);
    }

    private static int CompileStage(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        if (shader == 0) throw new Exception("Shader creation failed. Could not create shader handle.");
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
        if (status == (int)All.True) return shader;

        string log = GL.GetShaderInfoLog(shader);
        GL.DeleteShader(shader);
        throw new Exception($"[Shader] Error compiling shader ({type}): {log}");
    }
}

using Engine.Graphics;
using Engine.Graphics.Rhi;

namespace Engine.Tests.Graphics.Rhi;

[Collection(GlobalStateCollection.Name)]
public sealed class DeviceResourceTests : IDisposable
{
    private readonly DeviceScope _scope = new DeviceScope();

    private RecordingGraphicsDevice Device => _scope.Device;

    public void Dispose() => _scope.Dispose();

    [Fact]
    public void Texture2D_FromPixels_CreatesUploadsAndDeletes()
    {
        var pixels = new PixelData { Width = 2, Height = 2, Data = new byte[16] };

        var texture = new Texture2D(pixels, FilterMode.Nearest);
        int id = texture.Handle;

        Assert.NotEqual(0, id);
        Assert.Equal(new[]
        {
            $"CreateTexture Texture2D {id}",
            $"BindTexture 0 Texture2D {id}",
            $"SetTextureData2D {id} 0 Rgba8 2x2 16",
            $"SetSampler {id} Nearest Nearest Repeat -1",
            $"GenerateMipmaps {id}"
        }, Device.Calls);

        Device.Clear();
        texture.Dispose();
        texture.Dispose();

        Assert.Equal(new[] { $"DeleteTexture {id}" }, Device.Calls);
    }

    [Fact]
    public void Texture2D_Use_SkipsRedundantBinds()
    {
        var texture = new Texture2D(new PixelData { Width = 1, Height = 1, Data = new byte[4] });
        Device.Clear();

        texture.Use();
        texture.Use();
        texture.Use(3);
        Texture2D.ResetBindingCache();
        texture.Use();

        Assert.Equal(1, Device.Count("BindTexture 0"));
        Assert.Equal(1, Device.Count("BindTexture 3"));
        texture.Dispose();
    }

    [Fact]
    public void Texture2D_LinearFilter_UsesTrilinearSampling()
    {
        var texture = new Texture2D(new PixelData { Width = 1, Height = 1, Data = new byte[4] });

        Assert.Contains($"SetSampler {texture.Handle} LinearMipmapLinear Linear Repeat -1", Device.Calls);
        texture.Dispose();
    }

    [Fact]
    public void MeshBuffer_UploadsAndDescribesTheVertexLayout()
    {
        var buffer = new MeshBuffer(new[]
        {
            new BufferAttribute { Name = "aPos", Size = 3 },
            new BufferAttribute { Name = "aColor", Size = 4, Type = VertexComponentType.UnsignedByte, Normalized = true },
            new BufferAttribute { Name = "aUv", Size = 2 }
        });
        int vao = buffer.VAO, vbo = buffer.VBO, ebo = buffer.EBO;
        var vertices = new float[12];
        var indices = new uint[] { 0, 1, 2 };

        buffer.Upload(vertices, 6, indices, 3);
        buffer.SetAttributes();

        Assert.Equal(6, buffer.FloatsPerVertex);
        Assert.Equal(new[]
        {
            $"CreateVertexArray {vao}",
            $"CreateBuffer {vbo}",
            $"CreateBuffer {ebo}",
            $"BindVertexArray {vao}",
            $"BufferData Vertex {vbo} 24 Static",
            $"BufferData Index {ebo} 12 Static",
            $"BindVertexArray {vao}",
            $"BindBuffer Vertex {vbo}",
            "SetVertexAttribute 0 3 Float False 24 0",
            "SetVertexAttribute 1 4 UnsignedByte True 24 12",
            "SetVertexAttribute 2 2 Float False 24 16",
            "BindVertexArray 0"
        }, Device.Calls);

        Device.Clear();
        buffer.Dispose();
        Assert.Equal(new[] { $"DeleteBuffer {vbo}", $"DeleteBuffer {ebo}", $"DeleteVertexArray {vao}" }, Device.Calls);
    }

    [Fact]
    public void MeshBuffer_DrawHelpersBindAndUnbindTheVertexArray()
    {
        var buffer = new MeshBuffer(new[] { new BufferAttribute { Name = "aPos", Size = 3 } });
        int vao = buffer.VAO, vbo = buffer.VBO;
        Device.Clear();

        buffer.UploadVertices(new float[9], BufferUsage.Dynamic);
        buffer.DrawArrays(PrimitiveTopology.Lines, 3);
        buffer.DrawIndexed(PrimitiveTopology.Triangles, 6);

        Assert.Equal(new[]
        {
            $"BufferData Vertex {vbo} 36 Dynamic",
            $"BindVertexArray {vao}",
            "Draw Lines 0 3",
            "BindVertexArray 0",
            $"BindVertexArray {vao}",
            "DrawIndexed Triangles 6 UnsignedInt 0",
            "BindVertexArray 0"
        }, Device.Calls);
        buffer.Dispose();
    }

    [Fact]
    public void Mesh_RenderDrawsItsIndices()
    {
        var mesh = new Mesh(new float[16], 16, new uint[] { 0, 1, 2, 2, 3, 0 }, 6, new MeshBuffer(new[] { new BufferAttribute { Name = "aPos", Size = 4 } }));
        Device.Clear();

        mesh.Render();

        Assert.Contains("DrawIndexed Triangles 6 UnsignedInt 0", Device.Calls);
        Assert.Equal("BindVertexArray 0", Device.Calls[^1]);
        mesh.Dispose();
    }

    [Fact]
    public void RenderTexture_CreatesColorAndDepthTargetsAndDeletesThem()
    {
        var target = new RenderTexture(64, 32);
        int color = target.ColorTexture;

        Assert.Equal(1, Device.Count("CreateFramebuffer"));
        Assert.Contains($"SetTextureData2D {color} 0 Rgba8 64x32 0", Device.Calls);
        Assert.Contains($"AttachTexture Color0 {color}", Device.Calls);
        Assert.Contains(Device.Calls, c => c.StartsWith("CreateRenderbuffer Depth24 64x32", StringComparison.Ordinal));
        Assert.Contains("SetDrawBuffers 1", Device.Calls);
        Assert.Equal("BindFramebuffer 0", Device.Calls[^1]);

        Device.Clear();
        target.Resize(64, 32);
        Assert.Empty(Device.Calls);

        target.Dispose();
        Assert.Equal(1, Device.Count("DeleteFramebuffer"));
        Assert.Equal(1, Device.Count($"DeleteTexture {color}"));
        Assert.Equal(1, Device.Count("DeleteRenderbuffer"));
    }

    [Fact]
    public void RenderTexture_ReadPixelsRestoresTheBoundFramebuffer()
    {
        var target = new RenderTexture(4, 4);
        Device.BindFramebuffer(new FramebufferHandle(99));
        Device.Clear();

        var pixels = target.ReadPixels();

        Assert.Equal(64, pixels.Length);
        Assert.Contains("ReadPixels 0,0 4x4 Rgba8 64", Device.Calls);
        Assert.Equal("BindFramebuffer 99", Device.Calls[^1]);
        target.Dispose();
    }

    [Fact]
    public void SceneRenderer_HasColorNormalAndDepthAttachments()
    {
        var renderer = new SceneRenderer(8, 4);

        Assert.Contains($"AttachTexture Color0 {renderer.ColorTexture}", Device.Calls);
        Assert.Contains($"AttachTexture Color1 {renderer.NormalTexture}", Device.Calls);
        Assert.Contains($"AttachTexture Depth {renderer.DepthTexture}", Device.Calls);
        Assert.Contains($"SetTextureData2D {renderer.DepthTexture} 0 Depth24 8x4 0", Device.Calls);
        Assert.Contains("SetDrawBuffers 2", Device.Calls);

        Device.Clear();
        renderer.Dispose();
        Assert.Equal(3, Device.Count("DeleteTexture"));
        Assert.Equal(1, Device.Count("DeleteFramebuffer"));
    }

    [Fact]
    public void Shader_CompilesThroughTheDeviceAndSetsCachedUniforms()
    {
        Device.Uniforms["uValue"] = 7;
        var shader = new Shader(new[]
        {
            "--Vert",
            "#version 330 core",
            "void main() { gl_Position = vec4(0.0); }",
            "--Frag",
            "#version 330 core",
            "uniform float uValue;",
            "out vec4 FragColor;",
            "void main() { FragColor = vec4(uValue); }"
        });
        int program = shader.Handle;
        Device.Clear();

        shader.SetFloat("uValue", 2f);
        shader.SetFloat("uValue", 3f);
        shader.SetFloat("uMissing", 1f);

        Assert.Equal(new[] { $"UseProgram {program}", "SetUniform 7 2", "SetUniform 7 3" }, Device.Calls);

        Device.Clear();
        shader.Dispose();
        Assert.Equal(new[] { $"DeleteProgram {program}" }, Device.Calls);
    }

    [Fact]
    public void GLState_ForwardsToTheDeviceAndSkipsRedundantChanges()
    {
        GLState.Blend(true);
        GLState.Blend(true);
        GLState.BlendFunc(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);
        GLState.BlendFunc(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);
        GLState.InvalidateBlendFunc();
        GLState.BlendFunc(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);
        GLState.GBufferWrite(false);
        GLState.GBufferWrite(false);

        Assert.Equal(new[]
        {
            "SetBlend True",
            "SetBlendFunction SrcAlpha OneMinusSrcAlpha",
            "SetBlendFunction SrcAlpha OneMinusSrcAlpha",
            $"SetColorWrite {GLState.GBufferAttachment} False"
        }, Device.Calls);
    }

    [Fact]
    public void OcclusionQuery_RunsThroughTheDevice()
    {
        var query = new Engine.Graphics.Occlusion.OcclusionQuery();

        query.Begin();
        query.End();
        bool ready = query.TryGetResult(out bool visible);
        query.Dispose();
        query.Dispose();

        Assert.True(ready);
        Assert.False(visible);
        Assert.Equal(new[]
        {
            "CreateOcclusionQuery 1",
            "BeginOcclusionQuery 1",
            "EndOcclusionQuery",
            "TryGetOcclusionQueryResult 1",
            "DeleteQuery 1"
        }, Device.Calls);
    }

    [Fact]
    public void NullDevice_IsInstalledByDefault()
    {
        _scope.Dispose();

        Assert.False(Gpu.IsAvailable);
        Assert.False(Gpu.Device.CreateTexture(TextureType.Texture2D).IsValid);
    }
}

using Engine.Graphics.Debugging;
using Engine.Graphics.Rhi;
using OpenTK.Mathematics;

namespace Engine.Graphics.Occlusion;

public sealed class OcclusionBoxRenderer : IDisposable
{
    internal static readonly string[] Source =
    {
        "--Vert",
        "#version 330 core",
        "layout (location = 0) in vec3 aPos;",
        "uniform mat4 model;",
        "void main()",
        "{",
        "    gl_Position = vec4(aPos, 1.0) * model * VIEW * PROJECTION;",
        "}",
        "--Frag",
        "#version 330 core",
        "out vec4 FragColor;",
        "void main()",
        "{",
        "    FragColor = vec4(1.0);",
        "}"
    };

    private readonly Shader _shader;
    private readonly MeshBuffer _cube;

    public OcclusionBoxRenderer()
    {
        _shader = new Shader(Source);
        ResourceScopes.Own(_shader);

        _cube = new MeshBuffer(new[] { new BufferAttribute("aPos", 3) });
        ResourceScopes.Own(_cube);
        float[] vertices =
        {
            0f, 0f, 0f, 1f, 0f, 0f, 1f, 1f, 0f, 0f, 1f, 0f,
            0f, 0f, 1f, 1f, 0f, 1f, 1f, 1f, 1f, 0f, 1f, 1f
        };
        uint[] indices =
        {
            0, 1, 2, 2, 3, 0,
            4, 6, 5, 6, 4, 7,
            0, 4, 5, 5, 1, 0,
            3, 2, 6, 6, 7, 3,
            0, 3, 7, 7, 4, 0,
            1, 5, 6, 6, 2, 1
        };
        _cube.BindBuffer(ref vertices, ref indices);
        _cube.SetAttributes();
    }

    public void Begin()
    {
        var device = Gpu.Device;
        if (RenderDebugView.IsWireframe) device.SetFillMode(FillMode.Solid);
        GLState.Blend(false);
        GLState.CullFace(false);
        GLState.DepthTest(true);
        GLState.DepthMask(false);
        GLState.GBufferWrite(false);
        device.SetColorWrite(0, false);
        device.SetDepthFunction(CompareFunction.LessOrEqual);
        _shader.Use();
        device.BindVertexArray(_cube.VertexArray);
    }

    public void Draw(Matrix4 model, Vector3 min, Vector3 max)
    {
        var box = Matrix4.CreateScale(max - min) * Matrix4.CreateTranslation(min) * model;
        _shader.SetMatrix4("model", box);
        Gpu.Device.DrawIndexed(PrimitiveTopology.Triangles, 36, IndexType.UnsignedInt, 0);
    }

    public void End()
    {
        var device = Gpu.Device;
        device.BindVertexArray(VertexArrayHandle.None);
        device.SetDepthFunction(CompareFunction.Less);
        device.SetColorWrite(0, true);
        GLState.DepthMask(true);
        if (RenderDebugView.IsWireframe) device.SetFillMode(FillMode.Wireframe);
    }

    public void Dispose()
    {
        _shader.Dispose();
        _cube.Dispose();
    }
}

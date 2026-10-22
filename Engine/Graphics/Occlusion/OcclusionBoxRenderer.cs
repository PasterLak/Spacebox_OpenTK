using Engine.Graphics.Debugging;
using OpenTK.Graphics.OpenGL4;
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
        Resources.RemoveResourceFromControll(_shader);

        _cube = new MeshBuffer(new[] { new BufferAttribute("aPos", 3) });
        Resources.RemoveResourceFromControll(_cube);
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
        if (RenderDebugView.IsWireframe) GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        GLState.Blend(false);
        GLState.CullFace(false);
        GLState.DepthTest(true);
        GLState.DepthMask(false);
        GLState.GBufferWrite(false);
        GL.ColorMask(0, false, false, false, false);
        GL.DepthFunc(DepthFunction.Lequal);
        _shader.Use();
        GL.BindVertexArray(_cube.VAO);
    }

    public void Draw(Matrix4 model, Vector3 min, Vector3 max)
    {
        var box = Matrix4.CreateScale(max - min) * Matrix4.CreateTranslation(min) * model;
        _shader.SetMatrix4("model", box);
        GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, 0);
    }

    public void End()
    {
        GL.BindVertexArray(0);
        GL.DepthFunc(DepthFunction.Less);
        GL.ColorMask(0, true, true, true, true);
        GLState.DepthMask(true);
        if (RenderDebugView.IsWireframe) GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
    }

    public void Dispose()
    {
        _shader.Dispose();
        _cube.Dispose();
    }
}

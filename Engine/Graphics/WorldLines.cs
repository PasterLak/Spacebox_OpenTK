using Engine.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine;

public static class WorldLines
{
    private static Shader _shader;
    private static MeshBuffer _buffer;
    private static readonly List<float> _triangles = new List<float>();

    public static bool IsGpuInitialized => _shader != null;
    public static int VertexCount => _triangles.Count / 7;

    public static void InitializeGpu()
    {
        if (IsGpuInitialized) return;

        _shader = Resources.Load<Shader>("Resources/Shaders/debug", true);
        _buffer = new MeshBuffer(new[]
        {
            new BufferAttribute { Name = "position", Size = 3 },
            new BufferAttribute { Name = "color", Size = 4 }
        });
        _buffer.SetAttributes();
        Resources.RemoveResourceFromControll(_buffer);
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color4 color, float thickness)
    {
        var camera = Camera.Main;
        var direction = end - start;
        if (camera == null || direction.LengthSquared < 0.000001f) return;

        var side = Vector3.Cross(direction, camera.Position - (start + end) * 0.5f);
        if (side.LengthSquared < 0.000001f) side = Vector3.Cross(direction, camera.Up);
        side = side.Normalized() * (thickness * 0.5f);

        AddTriangle(start - side, start + side, end + side, color);
        AddTriangle(end + side, end - side, start - side, color);
    }

    public static void DrawBox(Vector3 min, Vector3 max, Color4 color, float thickness, Func<Vector3, Vector3> toWorld = null)
    {
        Span<Vector3> c = stackalloc Vector3[8];
        c[0] = new Vector3(min.X, min.Y, min.Z);
        c[1] = new Vector3(max.X, min.Y, min.Z);
        c[2] = new Vector3(max.X, max.Y, min.Z);
        c[3] = new Vector3(min.X, max.Y, min.Z);
        c[4] = new Vector3(min.X, min.Y, max.Z);
        c[5] = new Vector3(max.X, min.Y, max.Z);
        c[6] = new Vector3(max.X, max.Y, max.Z);
        c[7] = new Vector3(min.X, max.Y, max.Z);
        if (toWorld != null)
        {
            for (int i = 0; i < 8; i++) c[i] = toWorld(c[i]);
        }

        DrawLine(c[0], c[1], color, thickness);
        DrawLine(c[1], c[2], color, thickness);
        DrawLine(c[2], c[3], color, thickness);
        DrawLine(c[3], c[0], color, thickness);
        DrawLine(c[4], c[5], color, thickness);
        DrawLine(c[5], c[6], color, thickness);
        DrawLine(c[6], c[7], color, thickness);
        DrawLine(c[7], c[4], color, thickness);
        DrawLine(c[0], c[4], color, thickness);
        DrawLine(c[1], c[5], color, thickness);
        DrawLine(c[2], c[6], color, thickness);
        DrawLine(c[3], c[7], color, thickness);
    }

    public static void Render()
    {
        if (!IsGpuInitialized || _triangles.Count == 0) return;

        _shader.Use();
        _shader.SetMatrix4("model", Matrix4.Identity);
        GLState.DepthTest(true);
        GLState.CullFace(false);
        GLState.DepthMask(false);
        GLState.GBufferWrite(false);
        GLState.Blend(true);
        GLState.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.BindVertexArray(_buffer.VAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _buffer.VBO);
        float[] data = _triangles.ToArray();
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.DynamicDraw);
        GL.DrawArrays(PrimitiveType.Triangles, 0, data.Length / 7);
        GL.BindVertexArray(0);

        GLState.Blend(false);
        GLState.DepthMask(true);
        _triangles.Clear();
    }

    public static void Clear() => _triangles.Clear();

    private static void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color4 color)
    {
        AddVertex(a, color);
        AddVertex(b, color);
        AddVertex(c, color);
    }

    private static void AddVertex(Vector3 position, Color4 color)
    {
        position = RenderSpace.ToRender(position);
        _triangles.Add(position.X);
        _triangles.Add(position.Y);
        _triangles.Add(position.Z);
        _triangles.Add(color.R);
        _triangles.Add(color.G);
        _triangles.Add(color.B);
        _triangles.Add(color.A);
    }
}

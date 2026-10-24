using Engine.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public sealed class GlCanvasRenderer : ICanvasRenderer, IDisposable
{
    private const int MaxQuads = 8192;
    private const int FloatsPerVertex = 8;
    private const int FloatsPerQuad = FloatsPerVertex * 4;

    internal static readonly string[] ShaderCode =
    {
        "--Vert",
        "#version 330 core",
        "layout (location = 0) in vec2 aPos;",
        "layout (location = 1) in vec2 aUV;",
        "layout (location = 2) in vec4 aColor;",
        "uniform vec2 screenSize;",
        "out vec2 vUV;",
        "out vec4 vColor;",
        "void main()",
        "{",
        "    vec2 ndc = vec2(aPos.x / screenSize.x * 2.0 - 1.0, 1.0 - aPos.y / screenSize.y * 2.0);",
        "    gl_Position = vec4(ndc, 0.0, 1.0);",
        "    vUV = aUV;",
        "    vColor = aColor;",
        "}",
        "--Frag",
        "#version 330 core",
        "in vec2 vUV;",
        "in vec4 vColor;",
        "uniform sampler2D mainTexture;",
        "out vec4 FragColor;",
        "void main()",
        "{",
        "    FragColor = texture(mainTexture, vUV) * vColor;",
        "}"
    };

    private readonly float[] _vertices = new float[MaxQuads * FloatsPerQuad];
    private readonly Stack<Rect2> _clips = new Stack<Rect2>();
    private readonly Shader _shader;
    private readonly Texture2D _white;
    private int _vao, _vbo, _ebo;
    private int _quadCount;
    private int _texture;
    private Transform2D _screen = Transform2D.Identity;
    private Transform2D _transform = Transform2D.Identity;
    private Transform2D _screenTransform = Transform2D.Identity;
    private Vector2 _screenSize;
    private bool _drawing;

    public Transform2D Transform
    {
        get => _transform;
        set
        {
            _transform = value;
            _screenTransform = _screen * value;
        }
    }

    public Color4 Modulate { get; set; } = Color4.White;
    public int DrawCalls { get; private set; }
    public int Quads { get; private set; }

    public GlCanvasRenderer()
    {
        if (Application.IsHeadless) return;

        _shader = new Shader(ShaderCode);
        Resources.RemoveResourceFromControll(_shader);
        _white = new Texture2D(new PixelData { Width = 1, Height = 1, Data = new byte[] { 255, 255, 255, 255 } }, FilterMode.Nearest);
        Resources.RemoveResourceFromControll(_white);
        CreateBuffers();
    }

    public void Begin(Vector2 screenSize, Transform2D screenTransform)
    {
        if (_vao == 0) return;
        _drawing = true;
        _screenSize = screenSize;
        _screen = screenTransform;
        _quadCount = 0;
        _texture = 0;
        DrawCalls = 0;
        Quads = 0;
        _clips.Clear();
        Transform = Transform2D.Identity;
        Modulate = Color4.White;

        GLState.DepthTest(false);
        GLState.DepthMask(false);
        GLState.CullFace(false);
        GLState.Blend(true);
        GLState.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.ScissorTest);

        _shader.Use();
        _shader.SetVector2("screenSize", screenSize);
        _shader.SetInt("mainTexture", 0);
    }

    public void End()
    {
        if (!_drawing) return;
        Flush();
        _drawing = false;
        GL.Disable(EnableCap.ScissorTest);
        GL.BindVertexArray(0);
        Texture2D.ResetBindingCache();
        GLState.DepthMask(true);
        GLState.DepthTest(true);
    }

    public void DrawRect(Rect2 rect, Color4 color)
    {
        if (_white == null) return;
        AddQuad(_white.Handle, rect, new Rect2(0f, 0f, 1f, 1f), color);
    }

    public void DrawTexture(Texture2D texture, Rect2 rect, Rect2 uv, Color4 color)
    {
        if (texture == null || texture.Handle == 0) return;
        AddQuad(texture.Handle, rect, uv, color);
    }

    public void DrawTextureHandle(int handle, Rect2 rect, Rect2 uv, Color4 color)
    {
        if (handle == 0) return;
        AddQuad(handle, rect, uv, color);
    }

    public void PushClip(Rect2 rect)
    {
        var screen = _screenTransform.TransformAxisAligned(rect);
        if (_clips.Count > 0) screen = screen.Intersection(_clips.Peek());
        Flush();
        _clips.Push(screen);
        ApplyClip();
    }

    public void PopClip()
    {
        if (_clips.Count == 0) return;
        Flush();
        _clips.Pop();
        ApplyClip();
    }

    public void Dispose()
    {
        if (_vao != 0) GL.DeleteVertexArray(_vao);
        if (_vbo != 0) GL.DeleteBuffer(_vbo);
        if (_ebo != 0) GL.DeleteBuffer(_ebo);
        _vao = _vbo = _ebo = 0;
        _white?.Dispose();
        _shader?.Dispose();
    }

    private void AddQuad(int texture, Rect2 rect, Rect2 uv, Color4 color)
    {
        if (!_drawing) return;
        if (texture != _texture || _quadCount == MaxQuads)
        {
            Flush();
            _texture = texture;
        }

        var m = Modulate;
        float r = color.R * m.R, g = color.G * m.G, b = color.B * m.B, a = color.A * m.A;
        if (a <= 0f) return;

        var t = _screenTransform;
        var p0 = t.Apply(rect.Position);
        var p1 = t.Apply(new Vector2(rect.End.X, rect.Y));
        var p2 = t.Apply(rect.End);
        var p3 = t.Apply(new Vector2(rect.X, rect.End.Y));

        int i = _quadCount * FloatsPerQuad;
        Write(i, p0, uv.X, uv.Y, r, g, b, a);
        Write(i + FloatsPerVertex, p1, uv.End.X, uv.Y, r, g, b, a);
        Write(i + FloatsPerVertex * 2, p2, uv.End.X, uv.End.Y, r, g, b, a);
        Write(i + FloatsPerVertex * 3, p3, uv.X, uv.End.Y, r, g, b, a);
        _quadCount++;
    }

    private void Write(int index, Vector2 p, float u, float v, float r, float g, float b, float a)
    {
        _vertices[index] = p.X;
        _vertices[index + 1] = p.Y;
        _vertices[index + 2] = u;
        _vertices[index + 3] = v;
        _vertices[index + 4] = r;
        _vertices[index + 5] = g;
        _vertices[index + 6] = b;
        _vertices[index + 7] = a;
    }

    private void Flush()
    {
        if (_quadCount == 0 || _vao == 0) return;

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _texture);
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _quadCount * FloatsPerQuad * sizeof(float), _vertices);
        GL.DrawElements(PrimitiveType.Triangles, _quadCount * 6, DrawElementsType.UnsignedInt, 0);

        DrawCalls++;
        Quads += _quadCount;
        _quadCount = 0;
    }

    private void ApplyClip()
    {
        if (_clips.Count == 0)
        {
            GL.Disable(EnableCap.ScissorTest);
            return;
        }

        var clip = _clips.Peek();
        int x = (int)MathF.Floor(clip.X);
        int y = (int)MathF.Floor(_screenSize.Y - clip.End.Y);
        GL.Enable(EnableCap.ScissorTest);
        GL.Scissor(x, y, Math.Max(0, (int)MathF.Ceiling(clip.Width)), Math.Max(0, (int)MathF.Ceiling(clip.Height)));
    }

    private void CreateBuffers()
    {
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        _ebo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);

        var indices = new uint[MaxQuads * 6];
        for (uint q = 0, i = 0; q < MaxQuads; q++, i += 6)
        {
            uint v = q * 4;
            indices[i] = v;
            indices[i + 1] = v + 1;
            indices[i + 2] = v + 2;
            indices[i + 3] = v + 2;
            indices[i + 4] = v + 3;
            indices[i + 5] = v;
        }
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        int stride = FloatsPerVertex * sizeof(float);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, 4 * sizeof(float));
        GL.BindVertexArray(0);
    }
}

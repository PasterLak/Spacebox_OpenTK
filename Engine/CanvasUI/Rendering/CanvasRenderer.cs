using Engine.Graphics;
using Engine.Graphics.Rhi;
using OpenTK.Mathematics;

namespace Engine.CanvasUI;

public sealed class CanvasRenderer : ICanvasRenderer, IDisposable
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
    private VertexArrayHandle _vao;
    private BufferHandle _vbo, _ebo;
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

    public CanvasRenderer()
    {
        if (Application.IsHeadless) return;

        _shader = new Shader(ShaderCode);
        ResourceScopes.Own(_shader);
        _white = new Texture2D(new PixelData { Width = 1, Height = 1, Data = new byte[] { 255, 255, 255, 255 } }, FilterMode.Nearest);
        ResourceScopes.Own(_white);
        CreateBuffers();
    }

    public void Begin(Vector2 screenSize, Transform2D screenTransform)
    {
        if (!_vao.IsValid) return;
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
        GLState.BlendFunc(BlendFactor.SrcAlpha, BlendFactor.OneMinusSrcAlpha);
        Gpu.Device.SetScissorTest(false);

        _shader.Use();
        _shader.SetVector2("screenSize", screenSize);
        _shader.SetInt("mainTexture", 0);
    }

    public void End()
    {
        if (!_drawing) return;
        Flush();
        _drawing = false;
        Gpu.Device.SetScissorTest(false);
        Gpu.Device.BindVertexArray(VertexArrayHandle.None);
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
        var device = Gpu.Device;
        device.DeleteVertexArray(_vao);
        device.DeleteBuffer(_vbo);
        device.DeleteBuffer(_ebo);
        _vao = VertexArrayHandle.None;
        _vbo = _ebo = BufferHandle.None;
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
        if (_quadCount == 0 || !_vao.IsValid) return;

        var device = Gpu.Device;
        device.BindTexture(0, TextureType.Texture2D, new TextureHandle(_texture));
        device.BindVertexArray(_vao);
        device.BufferSubData(BufferKind.Vertex, _vbo, 0, new ReadOnlySpan<float>(_vertices, 0, _quadCount * FloatsPerQuad));
        device.DrawIndexed(PrimitiveTopology.Triangles, _quadCount * 6, IndexType.UnsignedInt, 0);

        DrawCalls++;
        Quads += _quadCount;
        _quadCount = 0;
    }

    private void ApplyClip()
    {
        var device = Gpu.Device;
        if (_clips.Count == 0)
        {
            device.SetScissorTest(false);
            return;
        }

        var clip = _clips.Peek();
        int x = (int)MathF.Floor(clip.X);
        int y = (int)MathF.Floor(_screenSize.Y - clip.End.Y);
        device.SetScissorTest(true);
        device.SetScissor(x, y, Math.Max(0, (int)MathF.Ceiling(clip.Width)), Math.Max(0, (int)MathF.Ceiling(clip.Height)));
    }

    private void CreateBuffers()
    {
        var device = Gpu.Device;
        _vao = device.CreateVertexArray();
        _vbo = device.CreateBuffer();
        _ebo = device.CreateBuffer();

        device.BindVertexArray(_vao);
        device.AllocateBuffer(BufferKind.Vertex, _vbo, _vertices.Length * sizeof(float), BufferUsage.Dynamic);

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
        device.BufferData(BufferKind.Index, _ebo, new ReadOnlySpan<uint>(indices), BufferUsage.Static);

        int stride = FloatsPerVertex * sizeof(float);
        device.BindBuffer(BufferKind.Vertex, _vbo);
        device.SetVertexAttribute(0, 2, VertexComponentType.Float, false, stride, 0);
        device.SetVertexAttribute(1, 2, VertexComponentType.Float, false, stride, 2 * sizeof(float));
        device.SetVertexAttribute(2, 4, VertexComponentType.Float, false, stride, 4 * sizeof(float));
        device.BindVertexArray(VertexArrayHandle.None);
    }
}

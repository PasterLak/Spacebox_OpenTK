using Engine;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;

namespace Spacebox.Game.Signs;

public sealed class SignRenderer : IDisposable
{
    private const string ShaderPath = "Resources/Shaders/movingBlock";
    private const int BakesPerFrame = 4;
    private const float RefreshInterval = 1f;
    private const float TextLift = 0.004f;

    private sealed class Entry
    {
        public SignBlock Sign;
        public int BakedVersion = -1;
        public Texture2D Texture;
        public TextureMaterial Material;
        public Matrix4 Local;
        public Vector3 Center;
        public Vector3 Normal;
        public float RefreshTimer;
        public bool HasText;
    }

    private static MeshBuffer _quad;

    private readonly SpaceEntity _entity;
    private readonly List<Entry> _entries = new List<Entry>();

    public int Count => _entries.Count;

    public SignRenderer(SpaceEntity entity)
    {
        _entity = entity;
    }

    public void Register(SignBlock sign)
    {
        if (sign == null || IndexOf(sign) >= 0) return;
        _entries.Add(new Entry { Sign = sign });
    }

    public void Unregister(SignBlock sign)
    {
        int index = IndexOf(sign);
        if (index < 0) return;

        Release(_entries[index]);
        _entries.RemoveAt(index);
    }

    public void Render()
    {
        if (_entries.Count == 0 || Application.IsHeadless || Camera.Main == null) return;

        var camera = Camera.Main;
        var cameraLocal = _entity.WorldToEntityLocal(camera.PositionWorld);
        var entityMatrix = _entity.RotationMatrix * Matrix4.CreateTranslation(RenderSpace.ToRender(_entity.PositionWorld));
        int bakes = 0;

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            var sign = entry.Sign;
            var data = sign.Data;

            entry.RefreshTimer -= Time.Delta;
            if (entry.RefreshTimer <= 0f || entry.BakedVersion < 0)
            {
                entry.RefreshTimer = RefreshInterval;
                UpdatePlacement(entry);
            }

            var toCamera = cameraLocal - entry.Center;
            if (toCamera.LengthSquared > data.ViewDistance * data.ViewDistance) continue;
            if (Vector3.Dot(toCamera, entry.Normal) <= 0f) continue;

            if (entry.BakedVersion != sign.Version)
            {
                if (bakes >= BakesPerFrame) continue;
                Bake(entry);
                bakes++;
            }

            if (!entry.HasText) continue;

            entry.Material.ExtraLight = data.Glow ? Vector3.One : SampleLight(entry);
            entry.Material.Apply(entry.Local * entityMatrix);
            DrawQuad();
        }
    }

    public void Dispose()
    {
        for (int i = 0; i < _entries.Count; i++) Release(_entries[i]);
        _entries.Clear();
    }

    private void UpdatePlacement(Entry entry)
    {
        var sign = entry.Sign;
        var data = sign.Data;
        var shape = data.Shape;
        if (shape == null) return;

        bool supported = BlockShapes.IsSupported(_entity, sign.LocalCell, shape, sign.Direction, sign.Rotation);
        float width = data.Width;
        float y = SignPlateGeometry.Thickness + TextLift;
        var cell = new Vector3(sign.LocalCell.X, sign.LocalCell.Y, sign.LocalCell.Z);

        var topLeft = cell + shape.ToCellSpace(new Vector3(-0.5f, y, -0.5f), sign.Direction, sign.Rotation, supported);
        var topRight = cell + shape.ToCellSpace(new Vector3(width - 0.5f, y, -0.5f), sign.Direction, sign.Rotation, supported);
        var bottomLeft = cell + shape.ToCellSpace(new Vector3(-0.5f, y, 0.5f), sign.Direction, sign.Rotation, supported);

        var right = topRight - topLeft;
        var down = bottomLeft - topLeft;
        var normal = Vector3.Cross(down, right).Normalized();

        entry.Normal = normal;
        entry.Center = topLeft + (right + down) * 0.5f;
        entry.Local = new Matrix4(
            new Vector4(right, 0f),
            new Vector4(normal, 0f),
            new Vector4(down, 0f),
            new Vector4(topLeft, 1f));
    }

    private void Bake(Entry entry)
    {
        var sign = entry.Sign;
        var data = sign.Data;
        entry.BakedVersion = sign.Version;
        entry.HasText = !string.IsNullOrWhiteSpace(SignMarkup.StripTags(sign.Text));
        if (!entry.HasText) return;

        var lines = SignMarkup.Parse(sign.Text, data.TextColor, data.Align);
        var pixels = SignTextBaker.Bake(lines, data.PixelsPerBlock * data.Width, data.PixelsPerBlock, data.Margin, data.Font);
        var texture = new Texture2D(pixels, FilterMode.Nearest);
        Resources.RemoveResourceFromControll(texture);

        if (entry.Texture != null) DisposalManager.EnqueueForDispose(entry.Texture);
        entry.Texture = texture;

        if (entry.Material == null)
            entry.Material = new TextureMaterial(texture, Resources.Load<Shader>(ShaderPath, true)) { WritesGBuffer = true, RenderFace = RenderFace.Both };
        else
            entry.Material.MainTexture = texture;
    }

    private Vector3 SampleLight(Entry entry)
    {
        return BlockLightSampler.Sample(_entity.EntityLocalToWorld(entry.Center + entry.Normal * 0.5f), _entity);
    }

    private static void DrawQuad()
    {
        _quad ??= CreateQuad();
        GL.BindVertexArray(_quad.VAO);
        GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

    private static MeshBuffer CreateQuad()
    {
        var buffer = TextureMaterial.GetMeshBuffer();
        Resources.RemoveResourceFromControll(buffer);
        float[] vertices =
        {
            0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f,
            1f, 0f, 0f, 0f, 1f, 0f, 1f, 0f,
            1f, 0f, 1f, 0f, 1f, 0f, 1f, 1f,
            0f, 0f, 1f, 0f, 1f, 0f, 0f, 1f
        };
        uint[] indices = { 0, 1, 2, 2, 3, 0 };
        buffer.BindBuffer(ref vertices, ref indices);
        buffer.SetAttributes();
        return buffer;
    }

    private int IndexOf(SignBlock sign)
    {
        for (int i = 0; i < _entries.Count; i++)
            if (_entries[i].Sign == sign) return i;
        return -1;
    }

    private static void Release(Entry entry)
    {
        if (entry.Texture != null) DisposalManager.EnqueueForDispose(entry.Texture);
        entry.Texture = null;
    }
}

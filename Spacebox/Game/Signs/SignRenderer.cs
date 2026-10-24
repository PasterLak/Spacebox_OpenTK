using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;
using Spacebox.Game.Rendering;

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
        public PlatePlacement Placement;
        public float RefreshTimer;
        public bool HasText;
    }

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

            if (!PlateQuad.IsVisible(entry.Placement, cameraLocal, data.ViewDistance)) continue;

            if (entry.BakedVersion != sign.Version)
            {
                if (bakes >= BakesPerFrame) continue;
                Bake(entry);
                bakes++;
            }

            if (!entry.HasText) continue;

            entry.Material.ExtraLight = data.Glow ? Vector3.One : PlateQuad.SampleLight(_entity, entry.Placement);
            entry.Material.Apply(entry.Placement.Local * entityMatrix);
            PlateQuad.Draw();
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
        var shape = sign.Data.Shape;
        if (shape == null) return;

        var area = new Box2(-0.5f, -0.5f, sign.Data.Width - 0.5f, 0.5f);
        entry.Placement = PlateQuad.Place(_entity, sign.LocalCell, shape, sign.Direction, sign.Rotation, area, SignPlateGeometry.Thickness + TextLift);
    }

    private void Bake(Entry entry)
    {
        var sign = entry.Sign;
        var data = sign.Data;
        entry.BakedVersion = sign.Version;
        entry.HasText = !string.IsNullOrWhiteSpace(SignMarkup.StripTags(sign.Text));
        if (!entry.HasText) return;

        var lines = SignMarkup.Parse(sign.Text, data.TextColor, data.Align);
        var pixels = SignTextBaker.Bake(lines, data.PixelsPerBlock * data.Width, data.PixelsPerBlock, data.Margin, data.Font, SignMarkup.ReadTextSize(sign.Text));
        var texture = new Texture2D(pixels, FilterMode.Nearest);
        ResourceScopes.Own(texture);

        if (entry.Texture != null) DisposalManager.EnqueueForDispose(entry.Texture);
        entry.Texture = texture;

        if (entry.Material == null)
            entry.Material = new TextureMaterial(texture, Resources.Load<Shader>(ShaderPath, true)) { WritesGBuffer = true, RenderFace = RenderFace.Both };
        else
            entry.Material.MainTexture = texture;
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

using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Generation.Shapes;
using Spacebox.Game.Rendering;

namespace Spacebox.Game.Pictures;

public sealed class PictureFrameRenderer
{
    private const string ShaderPath = "Resources/Shaders/movingBlock";
    private const float RefreshInterval = 1f;
    private const float PictureLift = 0.002f;

    private sealed class Entry
    {
        public PictureFrameBlock Frame;
        public int ShownVersion = -1;
        public TextureMaterial Material;
        public bool HasPicture;
        public PlatePlacement Placement;
        public float RefreshTimer;
    }

    private readonly SpaceEntity _entity;
    private readonly List<Entry> _entries = new List<Entry>();

    public int Count => _entries.Count;

    public PictureFrameRenderer(SpaceEntity entity)
    {
        _entity = entity;
    }

    public void Register(PictureFrameBlock frame)
    {
        if (frame == null || IndexOf(frame) >= 0) return;
        _entries.Add(new Entry { Frame = frame });
    }

    public void Unregister(PictureFrameBlock frame)
    {
        int index = IndexOf(frame);
        if (index >= 0) _entries.RemoveAt(index);
    }

    public void Render()
    {
        if (_entries.Count == 0 || Application.IsHeadless || Camera.Main == null) return;

        var cameraLocal = _entity.WorldToEntityLocal(Camera.Main.PositionWorld);
        var entityMatrix = _entity.RotationMatrix * Matrix4.CreateTranslation(RenderSpace.ToRender(_entity.PositionWorld));

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            var frame = entry.Frame;

            entry.RefreshTimer -= Time.Delta;
            if (entry.RefreshTimer <= 0f || entry.ShownVersion < 0)
            {
                entry.RefreshTimer = RefreshInterval;
                UpdatePlacement(entry);
            }

            if (entry.ShownVersion != frame.Version) Show(entry);
            if (!entry.HasPicture) continue;
            if (!PlateQuad.IsVisible(entry.Placement, cameraLocal, frame.Data.ViewDistance)) continue;

            entry.Material.ExtraLight = PlateQuad.SampleLight(_entity, entry.Placement);
            entry.Material.Apply(entry.Placement.Local * entityMatrix);
            PlateQuad.Draw();
        }
    }

    public void Dispose()
    {
        _entries.Clear();
    }

    private void UpdatePlacement(Entry entry)
    {
        var frame = entry.Frame;
        var shape = frame.Data.Shape;
        if (shape == null) return;

        float inset = PictureFrameGeometry.PictureInset;
        var area = new Box2(-0.5f + inset, -0.5f + inset, frame.Data.Width - 0.5f - inset, frame.Data.Height - 0.5f - inset);
        entry.Placement = PlateQuad.Place(_entity, frame.LocalCell, shape, frame.Direction, frame.Rotation, area, PictureFrameGeometry.Thickness + PictureLift);
    }

    private static void Show(Entry entry)
    {
        entry.ShownVersion = entry.Frame.Version;
        var texture = PictureTextures.Get(entry.Frame.Picture);
        entry.HasPicture = texture != null;
        if (!entry.HasPicture) return;

        if (entry.Material == null)
            entry.Material = new TextureMaterial(texture, Resources.Load<Shader>(ShaderPath, true)) { WritesGBuffer = true, RenderFace = RenderFace.Both };
        else
            entry.Material.MainTexture = texture;
    }

    private int IndexOf(PictureFrameBlock frame)
    {
        for (int i = 0; i < _entries.Count; i++)
            if (_entries[i].Frame == frame) return i;
        return -1;
    }
}

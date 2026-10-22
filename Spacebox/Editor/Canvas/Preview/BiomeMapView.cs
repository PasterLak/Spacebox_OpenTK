using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Editor.Generation;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Editor.Canvas.Preview;

public sealed class BiomeMapView : Control
{
    private const int MaxRingSegments = 4096;
    private const float RingThickness = 2f;

    private readonly BiomeMapPreview _map;
    private readonly Label _empty = new Label { FontColor = EditorCanvasTheme.TextDim, MouseFilter = MouseFilter.Ignore };

    public BiomeMapView(BiomeMapPreview map)
    {
        _map = map;
        ClipContents = true;
        MouseFilter = MouseFilter.Stop;
        CustomMinimumSize = new Vector2(160f, 160f);
        AddChild(_empty);
        _empty.Position = new Vector2(6f, 4f);
    }

    public int Slice { get; set; } = 64;
    public bool ShowRings { get; set; } = true;

    public override void Process(float deltaTime)
    {
        _map.Update();
        _empty.Visible = !_map.HasMap;
        _empty.Text = _map.IsBuilding ? "Building biome map..." : "Press Build to generate the biome map of a sector.";
    }

    protected override void OnMouseMove(Vector2 localPosition, bool pressed)
    {
        var square = Square();
        if (!_map.HasMap || !square.Contains(localPosition))
        {
            TooltipText = "";
            return;
        }

        var point = (localPosition - square.Position) / square.Width;
        int x = (int)(point.X * BiomesMap.Resolution);
        int z = (int)(point.Y * BiomesMap.Resolution);
        var biome = _map.BiomeAt(x, Slice, z);
        TooltipText = biome == null ? "No biome" : $"{biome.Name} ({biome.IdString})\nblocks x {x * BiomesMap.SmallestPoint}, z {z * BiomesMap.SmallestPoint}";
    }

    protected override void Draw(ICanvasRenderer renderer)
    {
        renderer.DrawRect(LocalRect, new Color4(0.04f, 0.045f, 0.055f, 1f));
        var texture = _map.GetSliceTexture(Slice);
        if (texture == null) return;

        var square = Square();
        renderer.DrawTexture(texture, square, new Rect2(0f, 0f, 1f, 1f), Color4.White);
        if (ShowRings) DrawRings(renderer, square);
        DrawAsteroids(renderer, square);
    }

    private Rect2 Square()
    {
        float side = MathF.Min(Size.X, Size.Y);
        return new Rect2(0f, 0f, side, side);
    }

    private void DrawAsteroids(ICanvasRenderer renderer, Rect2 square)
    {
        foreach (var asteroid in _map.Asteroids)
        {
            bool inSlice = BiomeMapImage.IsInSlice(asteroid.Y, Slice, BiomesMap.Resolution, Sector.SizeBlocks, BiomeMapPreview.SliceBand);
            var p = BiomeMapImage.ToImage(asteroid, Sector.SizeBlocks, square.Size);
            float size = inSlice ? 5f : 3f;
            renderer.DrawRect(new Rect2(square.X + p.X - size * 0.5f, square.Y + p.Y - size * 0.5f, size, size), inSlice ? Color4.White : new Color4(1f, 1f, 1f, 0.38f));
        }
    }

    private void DrawRings(ICanvasRenderer renderer, Rect2 square)
    {
        float scale = square.Width / Sector.SizeBlocks;
        var origin = _map.Origin;
        float sliceY = origin.Y + (Slice + 0.5f) * BiomesMap.SmallestPoint;
        var center = new Vector2(square.X - origin.X * scale, square.Y - origin.Z * scale);

        renderer.PushClip(square);
        foreach (var biome in _map.Biomes)
        {
            if (biome == null || biome.Distance.IsUnlimited) continue;
            var color = new Color4(biome.DebugColor.R / 255f, biome.DebugColor.G / 255f, biome.DebugColor.B / 255f, 1f);
            DrawRing(renderer, center, BiomeMapImage.RingRadius(biome.Distance.Min, sliceY) * scale, color);
            DrawRing(renderer, center, BiomeMapImage.RingRadius(biome.Distance.Max, sliceY) * scale, color);
        }
        renderer.PopClip();
    }

    private static void DrawRing(ICanvasRenderer renderer, Vector2 center, float radius, Color4 color)
    {
        if (radius <= 0f) return;
        int segments = Math.Clamp((int)(MathF.Tau * radius / RingThickness), 32, MaxRingSegments);
        for (int i = 0; i < segments; i++)
        {
            float angle = i * MathF.Tau / segments;
            var p = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            renderer.DrawRect(new Rect2(p.X - RingThickness * 0.5f, p.Y - RingThickness * 0.5f, RingThickness, RingThickness), color);
        }
    }
}

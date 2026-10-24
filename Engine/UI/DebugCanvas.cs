using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.UI;

public sealed class DebugCanvas : IDisposable
{
    private SkiaCanvasFont _font;

    public DebugCanvas(string name)
    {
        Layer = new CanvasLayer(name);
        _font = new SkiaCanvasFont(Path.Combine(AppContext.BaseDirectory, DarkTheme.FontPath), (int)DarkTheme.FontSize);
        Configure(Layer.Root, _font);
    }

    public CanvasLayer Layer { get; }
    public CanvasRoot Root => Layer.Root;
    public CanvasFont Font => _font;

    public static void Configure(CanvasRoot root, CanvasFont font)
    {
        root.Theme = DarkTheme.Create(font);
        root.Stretch.Mode = StretchMode.Expand;
        root.Stretch.VirtualSize = new Vector2(1280f, 720f);
        root.Stretch.ScaleStep = 0.5f;
        root.Stretch.MinScale = 1f;
    }

    public static SafeArea Area(CanvasRoot root, UiLayer layer)
    {
        return root.Add(new SafeArea { Name = layer + "Area", MouseFilter = MouseFilter.Ignore }, layer);
    }

    public void Dispose()
    {
        Layer.Dispose();
        _font?.Dispose();
        _font = null;
    }
}

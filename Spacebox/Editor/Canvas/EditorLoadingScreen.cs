using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Spacebox.Editor.Canvas;

public sealed class EditorLoadingScreen : IDisposable
{
    public const string Title = "Opening the GameSet editor";

    private readonly CanvasLayer _layer = new CanvasLayer("GameSet editor loading");
    private readonly Label _step = new Label { HorizontalAlignment = HorizontalAlignment.Center };
    private readonly ProgressBar _bar = new ProgressBar { MaxValue = 1f, CustomMinimumSize = new Vector2(320f, 8f), ShowPercentage = false };
    private SkiaCanvasFont _font;

    public EditorLoadingScreen()
    {
        if (!Engine.Application.IsHeadless) _font = new SkiaCanvasFont(Path.Combine(AppContext.BaseDirectory, EditorCanvasTheme.FontPath), (int)EditorCanvasTheme.FontSize);
        var root = _layer.Root;
        root.Theme = EditorCanvasTheme.Create(_font);
        root.Stretch.Mode = StretchMode.Expand;
        root.Stretch.VirtualSize = new Vector2(1280f, 720f);
        root.Stretch.ScaleStep = 0.5f;
        root.Stretch.MinScale = 1f;

        var center = root.Add(new CenterContainer { MouseFilter = MouseFilter.Ignore });
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        var column = center.AddChild(new VBoxContainer { MouseFilter = MouseFilter.Ignore });
        column.AddThemeConstantOverride("separation", 10f);
        column.AddChild(new Label { Text = Title, HorizontalAlignment = HorizontalAlignment.Center, Bold = true });
        column.AddChild(_bar);
        column.AddChild(_step);
    }

    public CanvasRoot Root => _layer.Root;

    public void Show(string step, float progress)
    {
        if (_step.Text != step) _step.Text = step;
        _bar.Value = progress;
    }

    public void Update(float deltaTime) => _layer.Update(deltaTime);

    public void Render() => _layer.Render();

    public void Dispose()
    {
        _layer.Dispose();
        _font?.Dispose();
        _font = null;
    }
}

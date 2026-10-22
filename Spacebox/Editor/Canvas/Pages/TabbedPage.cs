using Engine.CanvasUI;

namespace Spacebox.Editor.Canvas.Pages;

public sealed class TabbedPage : EditorPage
{
    private readonly EditorPage[] _pages;
    private readonly TabContainer _tabs = new TabContainer { SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand };
    private int _current = -1;

    public TabbedPage(string title, string description, params EditorPage[] pages)
    {
        Title = title;
        Description = description;
        _pages = pages;
    }

    public override string Title { get; }
    public override string Description { get; }

    protected override void Build()
    {
        AddChild(_tabs);
        foreach (var page in _pages)
        {
            page.Attach(Editor);
            page.SizeFlagsVertical = SizeFlags.Fill | SizeFlags.Expand;
            _tabs.AddTab(page, page.Title);
        }
        _tabs.TabChanged += Switch;
    }

    protected override void OnShown()
    {
        Switch(Math.Max(0, _tabs.CurrentTab));
    }

    public override void Tick(float deltaTime)
    {
        if (_current >= 0) _pages[_current].Tick(deltaTime);
    }

    public override void Hide()
    {
        if (_current >= 0) _pages[_current].Hide();
    }

    public override void Release()
    {
        foreach (var page in _pages) page.Release();
    }

    private void Switch(int index)
    {
        if (index < 0 || index >= _pages.Length) return;
        if (_current >= 0 && _current != index) _pages[_current].Hide();
        _current = index;
        _pages[index].Show();
    }
}

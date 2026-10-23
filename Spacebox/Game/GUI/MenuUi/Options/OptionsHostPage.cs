using Spacebox.Game.GUI.Menu;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class OptionsHostPage : MenuPage
{
    private readonly OptionsMenu _options;
    private MenuWindow _shown;
    private MenuPage _page;

    public OptionsHostPage(OptionsMenu options)
    {
        _options = options;
    }

    protected override void Build()
    {
    }

    public override void OnShow()
    {
        _shown = null;
        Sync();
    }

    public override void Tick(float deltaTime)
    {
        _options.Update();
        Sync();
        _page?.Tick(deltaTime);
    }

    private void Sync()
    {
        var current = _options.Current;
        if (current == _shown) return;
        _shown = current;
        if (_page != null) RemoveChild(_page);
        _page = current?.Page;
        if (_page == null) return;
        _page.EnsureBuilt();
        AddChild(_page);
        _page.OnShow();
    }
}

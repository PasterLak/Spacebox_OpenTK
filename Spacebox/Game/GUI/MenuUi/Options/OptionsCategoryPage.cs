using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.Menu;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class OptionsCategoryPage : MenuPage
{
    private const float Width = 600f;
    private const float ListHeight = 380f;

    private readonly RowsCategoryWindow _window;
    private readonly OptionRowsCanvas _rows;
    private readonly VBoxContainer _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.Fill | SizeFlags.Expand };

    public OptionsCategoryPage(RowsCategoryWindow window)
    {
        _window = window;
        _rows = new OptionRowsCanvas(window.Options);
    }

    protected override void Build()
    {
        var frame = MenuLayout.Window(_window.Title, Width);
        AddChild(frame.Root);
        _list.AddThemeConstantOverride("separation", 2f);
        frame.Body.AddChild(MenuLayout.ScrollList(_list, ListHeight));
        _rows.Build(_list, _window.Rows);

        frame.Footer.AddChild(OptionsButtons.Save(_window.Options));
        frame.Footer.AddChild(OptionsButtons.Back(_window.Options));
    }

    public override void OnShow() => _rows.Refresh();

    public override void Tick(float deltaTime) => _rows.Refresh();
}

namespace Spacebox.Game.GUI.Menu;

public abstract class RowsCategoryWindow : SettingsCategoryWindow
{
    private IReadOnlyList<OptionRow> _rows;

    protected RowsCategoryWindow(IOptionsNavigator options)
    {
        Options = options;
    }

    public IOptionsNavigator Options { get; }
    public abstract string Title { get; }
    public IReadOnlyList<OptionRow> Rows => _rows ??= BuildRows();

    protected abstract IReadOnlyList<OptionRow> BuildRows();

    protected override Spacebox.Game.GUI.MenuUi.MenuPage CreatePage() => new Spacebox.Game.GUI.MenuUi.OptionsCategoryPage(this);
}

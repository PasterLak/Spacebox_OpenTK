namespace Spacebox.Game.GUI.Menu;

public abstract class RowsCategoryWindow : SettingsCategoryWindow
{
    private readonly OptionRowsImGui _imGui = new OptionRowsImGui();
    private IReadOnlyList<OptionRow> _rows;

    protected RowsCategoryWindow(IOptionsNavigator options)
    {
        Options = options;
    }

    public IOptionsNavigator Options { get; }
    public abstract string Title { get; }
    public IReadOnlyList<OptionRow> Rows => _rows ??= BuildRows();

    protected abstract IReadOnlyList<OptionRow> BuildRows();

    public override void Render()
    {
        SettingsUI.Render(Title, Title, 5, (listSize, _) => _imGui.Draw("table##" + Title, Rows, listSize.X), Options.Save, Options.Back);
    }

    protected override Spacebox.Game.GUI.MenuUi.MenuPage CreatePage() => new Spacebox.Game.GUI.MenuUi.OptionsCategoryPage(this);
}

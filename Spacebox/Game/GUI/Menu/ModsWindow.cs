namespace Spacebox.Game.GUI.Menu;

public class ModsWindow : MenuWindow
{
    private readonly GameMenu _menu;

    public ModsWindow(GameMenu menu)
    {
        _menu = menu;
    }

    protected override Spacebox.Game.GUI.MenuUi.MenuPage CreatePage() => new Spacebox.Game.GUI.MenuUi.ModsPage(_menu);
}

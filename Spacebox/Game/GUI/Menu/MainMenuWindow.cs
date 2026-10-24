using Spacebox.Game.GUI.Menu;

public abstract class MenuWindow
{
    private Spacebox.Game.GUI.MenuUi.MenuPage _page;
    private bool _pageCreated;

    public Spacebox.Game.GUI.MenuUi.MenuPage Page
    {
        get
        {
            if (!_pageCreated)
            {
                _pageCreated = true;
                _page = CreatePage();
            }
            return _page;
        }
    }

    protected virtual Spacebox.Game.GUI.MenuUi.MenuPage CreatePage() => null;
    public virtual void OnWindowChanged()
    {

    }

    public virtual void OnWindowClosed()
    {
    }

    public virtual void OnSceneRender()
    {
    }
}

public class MainMenuWindow : MenuWindow
{
    public GameMenu menu;
    public MainMenuWindow(GameMenu menu)
    {
        this.menu = menu;
    }

    protected override Spacebox.Game.GUI.MenuUi.MenuPage CreatePage() => new Spacebox.Game.GUI.MenuUi.MainMenuPage(menu);
}

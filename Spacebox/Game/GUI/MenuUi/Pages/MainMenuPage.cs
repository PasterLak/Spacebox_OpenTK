using Spacebox.Game.GUI.Menu;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class MainMenuPage : MenuPage
{
    private const float Width = 230f;

    private readonly GameMenu _menu;

    public MainMenuPage(GameMenu menu)
    {
        _menu = menu;
    }

    protected override void Build()
    {
        var frame = MenuLayout.Window(null, Width);
        AddChild(frame.Root);
        frame.Body.AddThemeConstantOverride("separation", 10f);

        frame.Body.AddChild(MenuLayout.Button(_menu, "Play", _menu.SetStateToWorldSelect));
        frame.Body.AddChild(MenuLayout.Button(_menu, "Multiplayer", _menu.SetStateToMultiplayer));
        frame.Body.AddChild(MenuLayout.Button(_menu, "Mods", _menu.SetStateToMods));
        frame.Body.AddChild(MenuLayout.Button(_menu, "Options", _menu.SetStateToOptions));
        frame.Body.AddChild(MenuLayout.Button(_menu, "Exit", () => SpaceboxWindow.Instance.Quit()));
        frame.Footer.Visible = false;
    }
}

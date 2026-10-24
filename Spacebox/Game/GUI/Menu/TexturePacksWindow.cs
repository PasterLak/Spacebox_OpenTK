using Spacebox.Game.Resource.TexturePacks;

namespace Spacebox.Game.GUI.Menu;

public sealed class TexturePacksWindow : MenuWindow
{
    private readonly GameMenu _menu;

    public TexturePacksWindow(GameMenu menu)
    {
        _menu = menu;
    }

    public override void OnWindowChanged()
    {
        TexturePackService.Rescan();
    }

    protected override Spacebox.Game.GUI.MenuUi.MenuPage CreatePage() => new Spacebox.Game.GUI.MenuUi.TexturePacksPage(_menu);
}

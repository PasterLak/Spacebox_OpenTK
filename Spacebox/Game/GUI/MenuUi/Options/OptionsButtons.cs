using Engine.CanvasUI;
using Spacebox.Game.GUI.Menu;

namespace Spacebox.Game.GUI.MenuUi;

public static class OptionsButtons
{
    public static Button Save(IOptionsNavigator options)
    {
        var button = MenuLayout.Button(null, "Save", options.Save);
        button.TooltipText = "Keep these settings and save them for the next start.";
        return button;
    }

    public static Button Back(IOptionsNavigator options)
    {
        var button = MenuLayout.Button(null, "Back", options.Back);
        button.TooltipText = "Go back. If something was changed, asks whether to save it.";
        return button;
    }
}

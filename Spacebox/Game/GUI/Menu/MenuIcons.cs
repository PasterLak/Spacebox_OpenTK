using Engine;

namespace Spacebox.Game.GUI.Menu;

public static class MenuIcons
{
    private const string ResourcePrefix = "menu_icon_";

    public static Texture2D Load(string path)
    {
        if (!File.Exists(path)) return null;

        var texture = new Texture2D(path, true, false);
        Resources.AddResource(ResourcePrefix + Guid.NewGuid().ToString("N"), texture, false);
        return texture;
    }
}

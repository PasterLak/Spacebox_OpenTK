using Engine;

namespace Spacebox.Game.GUI.MenuUi;

public static class MenuTextures
{
    public static Texture2D Load(string path)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, path);
        if (!File.Exists(fullPath)) return null;
        var texture = new Texture2D(fullPath, false, true);
        Resources.AddResourceToDispose(texture);
        return texture;
    }
}

using Engine;

namespace Spacebox.Game.Pictures;

public static class PictureTextures
{
    private static readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

    public static Texture2D Get(PictureItem picture)
    {
        if (picture == null || string.IsNullOrEmpty(picture.PicturePath) || Application.IsHeadless) return null;
        if (_textures.TryGetValue(picture.PicturePath, out var texture)) return texture;

        texture = new Texture2D(picture.PicturePath, pixelated: true, flipY: false);
        ResourceScopes.Own(texture);
        _textures[picture.PicturePath] = texture;
        return texture;
    }

    public static void Clear()
    {
        foreach (var texture in _textures.Values) texture.Dispose();
        _textures.Clear();
    }
}

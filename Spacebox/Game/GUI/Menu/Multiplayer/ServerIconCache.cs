using Engine;
using Spacebox.Network.Discovery;

namespace Spacebox.Game.GUI.Menu;

public sealed class ServerIconCache
{
    private const string DefaultIconPath = "Resources/Textures/icon.png";
    private const string ResourcePrefix = "server_icon_";

    private readonly string _folder;
    private readonly Dictionary<ulong, Texture2D> _textures = new Dictionary<ulong, Texture2D>();
    private Texture2D _defaultIcon;

    public ServerIconCache(string folder = null)
    {
        _folder = folder ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Multiplayer", "ServerIcons");
    }

    public Texture2D DefaultIcon => _defaultIcon ??= CreateTexture(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultIconPath), "default");

    public bool Has(ulong hash)
    {
        return hash != 0 && File.Exists(GetPath(hash));
    }

    public void Store(ulong hash, byte[] png)
    {
        if (hash == 0 || !ServerIconFile.IsValid(png)) return;

        try
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllBytes(GetPath(hash), png);
            _textures.Remove(hash);
        }
        catch (Exception ex)
        {
            Debug.Error($"[Multiplayer] Failed to cache server icon: {ex.Message}");
        }
    }

    public Texture2D Get(ulong hash)
    {
        if (hash == 0) return DefaultIcon;
        if (_textures.TryGetValue(hash, out var texture)) return texture ?? DefaultIcon;

        string path = GetPath(hash);
        texture = File.Exists(path) ? CreateTexture(path, ServerIconHash.Format(hash)) : null;
        _textures[hash] = texture;
        return texture ?? DefaultIcon;
    }

    private string GetPath(ulong hash)
    {
        return Path.Combine(_folder, ServerIconHash.Format(hash) + ".png");
    }

    private static Texture2D CreateTexture(string path, string name)
    {
        if (!File.Exists(path)) return null;

        var texture = new Texture2D(path, false, false);
        Resources.AddResource(ResourcePrefix + name, texture, false);
        return texture;
    }
}

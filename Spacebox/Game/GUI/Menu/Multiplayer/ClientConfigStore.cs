using System.Text.Json;
using Engine;

namespace Spacebox.Game.GUI.Menu;

public static class ClientConfigStore
{
    public const string FileName = "client.json";
    public const string DefaultSkinColor = "Yellow";

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

    public static string FilePath => Spacebox.Network.MultiplayerFolders.File(FileName);

    public static ClientConfig Load()
    {
        return Load(FilePath);
    }

    public static ClientConfig Load(string path)
    {
        ClientConfig config = null;
        bool exists = File.Exists(path);

        if (exists)
        {
            try
            {
                config = JsonSerializer.Deserialize<ClientConfig>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Debug.Error($"[Multiplayer] Failed to read {FileName}: {ex.Message}");
            }
        }

        config ??= new ClientConfig();
        bool changed = Normalize(config);
        if (!exists || changed) Save(config, path);
        return config;
    }

    public static void Save(ClientConfig config)
    {
        Save(config, FilePath);
    }

    public static void Save(ClientConfig config, string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path, JsonSerializer.Serialize(config, Options));
        }
        catch (Exception ex)
        {
            Debug.Error($"[Multiplayer] Failed to write {FileName}: {ex.Message}");
        }
    }

    public static bool Normalize(ClientConfig config)
    {
        config.PlayerNickname ??= "";
        bool changed = config.GenerateNameIfEmpty();

        if (string.IsNullOrEmpty(config.SkinColor))
        {
            config.SkinColor = DefaultSkinColor;
            changed = true;
        }

        if (config.Servers == null)
        {
            config.Servers = new List<Spacebox.Network.ServerEntry>();
            changed = true;
        }

        if (config.LocalServers == null)
        {
            config.LocalServers = new List<string>();
            changed = true;
        }

        changed |= config.Servers.RemoveAll(server => server == null) > 0;
        foreach (var server in config.Servers)
        {
            server.Name ??= "";
            server.Description ??= "";
            server.IconHash ??= "";
            server.IP ??= "";
        }

        return changed;
    }
}

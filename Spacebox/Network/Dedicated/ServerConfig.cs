using System.Text.Json;
using Engine;
using Engine.Networking;

namespace Spacebox.Network.Dedicated;

public sealed class ServerConfig
{
    private const string FileName = "serverconfig.json";

    public string ServerName { get; set; } = "Spacebox Server";
    public string WorldName { get; set; } = "ServerWorld";
    public string Seed { get; set; } = "";
    public string ModId { get; set; } = "default";
    public int Port { get; set; } = NetworkManager.DefaultPort;
    public int MaxPlayers { get; set; } = 16;
    public byte GameMode { get; set; } = 0;
    public float AutoSaveInterval { get; set; } = 300f;

    public static ServerConfig LoadOrCreate()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
        var options = new JsonSerializerOptions { WriteIndented = true };

        if (File.Exists(path))
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<ServerConfig>(File.ReadAllText(path));
                if (loaded != null) return loaded;
            }
            catch (Exception ex)
            {
                Debug.Error($"[Server] Failed to read {FileName}: {ex.Message}. Using defaults.");
            }
        }

        var config = new ServerConfig { Seed = new Random().Next().ToString() };
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(config, options));
            Debug.Warning($"[Server] Created default {FileName}");
        }
        catch (Exception ex)
        {
            Debug.Error($"[Server] Failed to write {FileName}: {ex.Message}");
        }

        return config;
    }
}

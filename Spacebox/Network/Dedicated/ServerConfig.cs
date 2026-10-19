using System.Text.Json;
using Engine;
using Engine.Networking;

namespace Spacebox.Network.Dedicated;

public sealed class ServerConfig
{
    private const string FileName = "serverconfig.json";

    public string ServerName { get; set; } = "Spacebox Server";
    public string Description { get; set; } = "A Spacebox server";
    public string IconFile { get; set; } = "server-icon.png";
    public bool LanDiscovery { get; set; } = true;
    public string WorldName { get; set; } = "ServerWorld";
    public string Seed { get; set; } = "";
    public string ModId { get; set; } = "default";
    public int Port { get; set; } = NetworkManager.DefaultPort;
    public int MaxPlayers { get; set; } = 16;
    public byte GameMode { get; set; } = 0;
    public float AutoSaveInterval { get; set; } = 300f;
    public int UpdateRate { get; set; } = DefaultUpdateRate;

    public const int DefaultUpdateRate = 30;
    public const int MinUpdateRate = 10;
    public const int MaxUpdateRate = 240;

    public int GetClampedUpdateRate() => Math.Clamp(UpdateRate, MinUpdateRate, MaxUpdateRate);

    public static ServerConfig LoadOrCreate()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
        var options = new JsonSerializerOptions { WriteIndented = true };

        if (File.Exists(path))
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<ServerConfig>(File.ReadAllText(path));
                if (loaded != null)
                {
                    Save(loaded, path, options);
                    return loaded;
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"[Server] Failed to read {FileName}: {ex.Message}. Using defaults.");
            }
        }

        var config = new ServerConfig { Seed = new Random().Next().ToString() };
        if (Save(config, path, options)) Debug.Warning($"[Server] Created default {FileName}");

        return config;
    }

    private static bool Save(ServerConfig config, string path, JsonSerializerOptions options)
    {
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(config, options));
            return true;
        }
        catch (Exception ex)
        {
            Debug.Error($"[Server] Failed to write {FileName}: {ex.Message}");
            return false;
        }
    }
}

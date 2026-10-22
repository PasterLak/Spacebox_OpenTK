using System.Text.Json;
using Engine;
using Engine.Networking;

namespace Spacebox.Network.Dedicated;

public sealed class ServerConfig
{
    public const string FileName = "serverconfig.json";

    public string ServerName { get; set; } = "Spacebox Server";
    public string Description { get; set; } = "A Spacebox server";
    public string IconFile { get; set; } = "server-icon.png";
    public bool LanDiscovery { get; set; } = true;
    public bool WhitelistEnabled { get; set; } = false;
    public List<string> Whitelist { get; set; } = new List<string>();
    public bool RegistrationEnabled { get; set; } = false;
    public List<string> Admins { get; set; } = new List<string>();
    public bool ShowAdmins { get; set; } = true;
    public string WorldName { get; set; } = "ServerWorld";
    public string Seed { get; set; } = "";
    public string ModId { get; set; } = "default";
    public List<string> TexturePacks { get; set; } = new List<string>();
    public int Port { get; set; } = NetworkManager.DefaultPort;
    public int MaxPlayers { get; set; } = 16;
    public byte GameMode { get; set; } = (byte)Spacebox.Game.Player.GameModes.GameMode.Creative;
    public float AutoSaveInterval { get; set; } = 300f;
    public int UpdateRate { get; set; } = DefaultUpdateRate;
    public int IdleUpdateRate { get; set; } = 10;
    public float SleepAfterMinutes { get; set; } = 0f;
    public bool FriendlyMobs { get; set; } = false;
    public Spacebox.Game.Rules.ServerRules Gameplay { get; set; } = new Spacebox.Game.Rules.ServerRules();
    public Spacebox.Game.Protection.ClaimSettings Claims { get; set; } = new Spacebox.Game.Protection.ClaimSettings();
    public ServerBackupSettings Backups { get; set; } = new ServerBackupSettings();
    public bool DashboardEnabled { get; set; } = true;
    public int DashboardPort { get; set; } = 0;
    public string DashboardCertificateFile { get; set; } = "";
    public string DashboardCertificateKeyFile { get; set; } = "";
    public string DashboardCertificatePassword { get; set; } = "";
    public string PublicAddress { get; set; } = "";
    public bool DashboardAllowPublicHttp { get; set; } = false;
    public bool HttpsAutomatic { get; set; } = true;
    public int HttpsChallengePort { get; set; } = 80;
    public string DuckDnsDomain { get; set; } = "";
    public string DuckDnsToken { get; set; } = "";
    public string CloudflareApiToken { get; set; } = "";
    public bool DuckDnsUpdateIp { get; set; } = true;
    public string LetsEncryptEmail { get; set; } = "";
    public bool LetsEncryptStaging { get; set; } = false;

    public int GetDashboardPort() => DashboardPort > 0 ? DashboardPort : Port;

    public const int DefaultUpdateRate = 60;
    public const int MinUpdateRate = 10;
    public const int MaxUpdateRate = 240;

    public int GetClampedUpdateRate() => Math.Clamp(UpdateRate, MinUpdateRate, MaxUpdateRate);

    public static ServerConfig LoadOrCreate()
    {
        string path = FilePath;
        var options = new JsonSerializerOptions { WriteIndented = true };

        if (File.Exists(path))
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<ServerConfig>(File.ReadAllText(path));
                if (loaded != null)
                {
                    loaded.Claims ??= new Spacebox.Game.Protection.ClaimSettings();
                    loaded.TexturePacks ??= new List<string>();
                    loaded.Gameplay ??= new Spacebox.Game.Rules.ServerRules();
                    loaded.Claims.Validate();
                    loaded.Gameplay.Validate();
                    Save(loaded, path, options);
                    return loaded;
                }
            }
            catch (Exception ex)
            {
                Debug.Error($"[Server] Failed to read {FileName}: {ex.Message}. Using defaults.");
            }
        }

        var config = CreateNew();
        if (Save(config, path, options)) Debug.Warning($"[Server] Created default {FileName}");

        return config;
    }

    public static ServerConfig CreateNew()
    {
        return new ServerConfig { Seed = new Random().Next().ToString(), RegistrationEnabled = true };
    }

    public bool Save()
    {
        return Save(this, FilePath, new JsonSerializerOptions { WriteIndented = true });
    }

    public static string FilePath => MultiplayerFolders.File(FileName);

    private static bool Save(ServerConfig config, string path, JsonSerializerOptions options)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
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

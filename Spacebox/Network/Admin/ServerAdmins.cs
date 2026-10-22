using Engine.Networking;
using Spacebox.Network.Dedicated;

namespace Spacebox.Network.Admin;

public static class ServerAdmins
{
    public const string Prefix = "[ADMIN] ";
    public const int MaxNameLength = 32;

    public static bool Contains(IReadOnlyList<string> admins, string name)
    {
        if (admins == null || string.IsNullOrWhiteSpace(name)) return false;

        string trimmed = name.Trim();
        for (int i = 0; i < admins.Count; i++)
        {
            if (string.Equals(admins[i]?.Trim(), trimmed, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    public static bool TryAdd(List<string> admins, string name, out string error)
    {
        string trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxNameLength)
        {
            error = $"Enter a name (up to {MaxNameLength} characters)";
            return false;
        }

        error = null;
        if (!Contains(admins, trimmed)) admins.Add(trimmed);
        return true;
    }

    public static bool Remove(List<string> admins, string name)
    {
        string trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return false;
        return admins.RemoveAll(entry => string.Equals(entry?.Trim(), trimmed, StringComparison.OrdinalIgnoreCase)) > 0;
    }

    public static bool RightsActive(ServerConfig config) => config != null && config.RegistrationEnabled;

    public static bool IsAdmin(ServerConfig config, string name) => RightsActive(config) && Contains(config.Admins, name);

    public static bool IsAdmin(string name) => GameSession.IsDedicated && IsAdmin(DedicatedServer.Config, name);

    public static bool CanUseCommands(int peer)
    {
        var network = NetworkManager.Instance;
        if (peer == network.LocalPeerId) return !GameSession.IsDedicated;
        return !GameSession.IsDedicated || IsAdmin(PlayerRegistry.GetName(peer));
    }

    public static bool ShowsBadge(ServerConfig config, string name) => config != null && config.ShowAdmins && IsAdmin(config, name);

    public static bool ShowsBadge(string name) => GameSession.IsDedicated && ShowsBadge(DedicatedServer.Config, name);

    public static string Label(string name, bool admin) => admin ? Prefix + name : name;
}

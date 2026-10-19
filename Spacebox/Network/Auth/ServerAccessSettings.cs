namespace Spacebox.Network.Auth;

public sealed class ServerAccessSettings
{
    public static readonly ServerAccessSettings Open = new ServerAccessSettings(false, Array.Empty<string>(), false);

    private readonly HashSet<string> _whitelist;

    public ServerAccessSettings(bool whitelistEnabled, IEnumerable<string> whitelist, bool registrationEnabled)
    {
        WhitelistEnabled = whitelistEnabled;
        RegistrationEnabled = registrationEnabled;
        _whitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (whitelist == null) return;

        foreach (var name in whitelist)
        {
            if (!string.IsNullOrWhiteSpace(name)) _whitelist.Add(name.Trim());
        }
    }

    public bool WhitelistEnabled { get; }
    public bool RegistrationEnabled { get; }

    public bool IsAllowed(string name)
    {
        return !WhitelistEnabled || (!string.IsNullOrWhiteSpace(name) && _whitelist.Contains(name.Trim()));
    }
}

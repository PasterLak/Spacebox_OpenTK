namespace Spacebox.Network.Discovery;

public static class ServerAddressValidator
{
    public const int MaxNameLength = 64;
    public const int MaxHostLength = 253;

    public static bool TryValidate(string host, int port, out string error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(host))
        {
            error = "Enter a server address";
            return false;
        }

        string trimmed = host.Trim();
        if (trimmed.Length > MaxHostLength || trimmed.Contains(' '))
        {
            error = "The address is not valid";
            return false;
        }

        if (port < 1 || port > ushort.MaxValue)
        {
            error = $"Port must be between 1 and {ushort.MaxValue}";
            return false;
        }

        return true;
    }

    public static string ResolveName(string name, string host)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            string trimmed = name.Trim();
            return trimmed.Length <= MaxNameLength ? trimmed : trimmed.Substring(0, MaxNameLength);
        }

        return string.IsNullOrWhiteSpace(host) ? "Server" : host.Trim();
    }
}

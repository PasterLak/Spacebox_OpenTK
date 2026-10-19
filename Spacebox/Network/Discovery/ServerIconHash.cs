using System.Globalization;

namespace Spacebox.Network.Discovery;

public static class ServerIconHash
{
    public static string Format(ulong hash)
    {
        return hash == 0 ? "" : hash.ToString("x16", CultureInfo.InvariantCulture);
    }

    public static ulong Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        return ulong.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hash) ? hash : 0;
    }
}

using System.Net;

namespace Spacebox.Network.Discovery;

public sealed class LocalServerHistory
{
    public const int MaxEntries = 16;

    private readonly List<string> _addresses;

    public LocalServerHistory(List<string> addresses)
    {
        _addresses = addresses ?? new List<string>();
        _addresses.RemoveAll(address => !TryParse(address, out _));
        if (_addresses.Count > MaxEntries) _addresses.RemoveRange(MaxEntries, _addresses.Count - MaxEntries);
    }

    public IReadOnlyList<string> Addresses => _addresses;

    public bool Remember(IPEndPoint endPoint)
    {
        if (endPoint == null || IPAddress.IsLoopback(endPoint.Address)) return false;

        var address = endPoint.Address.IsIPv4MappedToIPv6 ? endPoint.Address.MapToIPv4() : endPoint.Address;
        string text = new IPEndPoint(address, endPoint.Port).ToString();

        int index = _addresses.IndexOf(text);
        if (index == 0) return false;
        if (index > 0) _addresses.RemoveAt(index);

        _addresses.Insert(0, text);
        if (_addresses.Count > MaxEntries) _addresses.RemoveAt(_addresses.Count - 1);
        return true;
    }

    public static bool TryParse(string address, out IPEndPoint endPoint)
    {
        endPoint = null;
        return !string.IsNullOrWhiteSpace(address) && IPEndPoint.TryParse(address, out endPoint) && endPoint.Port > 0;
    }
}

using System.Net;
using System.Net.Sockets;

namespace Engine.Web;

public static class NetworkAddresses
{
    public static bool IsLocal(string address)
    {
        if (!IPAddress.TryParse(address, out var ip)) return false;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip)) return true;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();
            return bytes[0] == 10
                   || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                   || (bytes[0] == 192 && bytes[1] == 168)
                   || (bytes[0] == 169 && bytes[1] == 254);
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = ip.GetAddressBytes();
            return (bytes[0] & 0xFE) == 0xFC || ip.IsIPv6LinkLocal;
        }

        return false;
    }

    public static IReadOnlyList<string> LocalIPv4()
    {
        var result = new List<string>();
        try
        {
            foreach (var adapter in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                if (adapter.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback) continue;

                foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                {
                    var ip = unicast.Address;
                    if (ip.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ip)) continue;

                    string text = ip.ToString();
                    if (IsLocal(text) && !text.StartsWith("169.254.", StringComparison.Ordinal) && !result.Contains(text)) result.Add(text);
                }
            }
        }
        catch (System.Net.NetworkInformation.NetworkInformationException)
        {
        }

        return result;
    }

    public static string DescribeUrls(int port, string route, IReadOnlyList<string> lanAddresses)
    {
        var urls = new List<string> { $"http://localhost:{port}{route}" };
        for (int i = 0; i < lanAddresses.Count; i++) urls.Add($"http://{lanAddresses[i]}:{port}{route}");
        return string.Join(" or ", urls);
    }
}

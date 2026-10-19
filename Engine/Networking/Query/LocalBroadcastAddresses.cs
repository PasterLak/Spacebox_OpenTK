using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Engine.Networking.Query;

public static class LocalBroadcastAddresses
{
    public static IPAddress GetBroadcastAddress(IPAddress address, IPAddress mask)
    {
        if (address == null || mask == null) return null;
        if (address.AddressFamily != AddressFamily.InterNetwork || mask.AddressFamily != AddressFamily.InterNetwork) return null;

        byte[] ip = address.GetAddressBytes();
        byte[] maskBytes = mask.GetAddressBytes();
        var result = new byte[4];
        for (int i = 0; i < 4; i++)
            result[i] = (byte)(ip[i] | ~maskBytes[i]);

        return new IPAddress(result);
    }

    public static List<IPAddress> Collect()
    {
        var addresses = new List<IPAddress>();

        try
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up) continue;
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                {
                    var broadcast = GetBroadcastAddress(unicast.Address, unicast.IPv4Mask);
                    if (broadcast == null || broadcast.Equals(IPAddress.Broadcast) || addresses.Contains(broadcast)) continue;
                    addresses.Add(broadcast);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Warning($"[ServerQuery] Could not list network interfaces: {ex.Message}");
        }

        return addresses;
    }
}

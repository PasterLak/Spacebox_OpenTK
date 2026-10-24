using Engine.Networking;

namespace Spacebox.Network.Discovery;

public static class LanPorts
{
    public const int Count = 10;

    public static readonly IReadOnlyList<int> Scanned = Range(NetworkManager.DefaultPort, Count);

    public static bool IsScanned(int port) => port >= NetworkManager.DefaultPort && port < NetworkManager.DefaultPort + Count;

    public static List<int> Candidates(int preferred)
    {
        var result = new List<int>(Count + 1);
        if (preferred > 0 && preferred <= ushort.MaxValue) result.Add(preferred);
        for (int i = 0; i < Scanned.Count; i++)
        {
            if (Scanned[i] != preferred) result.Add(Scanned[i]);
        }
        return result;
    }

    private static int[] Range(int first, int count)
    {
        var ports = new int[count];
        for (int i = 0; i < count; i++) ports[i] = first + i;
        return ports;
    }
}

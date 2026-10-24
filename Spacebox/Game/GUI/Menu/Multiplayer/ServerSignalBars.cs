using Spacebox.Network.Discovery;

namespace Spacebox.Game.GUI.Menu;

public static class ServerSignalBars
{
    public const int BarCount = 4;

    private static readonly int[] PingThresholds = { 300, 150, 80 };

    public static int GetLevel(ServerListItem item)
    {
        if (item.State != ServerOnlineState.Online) return 0;

        int level = 1;
        for (int i = 0; i < PingThresholds.Length; i++)
        {
            if (item.PingMs < PingThresholds[i]) level++;
        }
        return level;
    }
}

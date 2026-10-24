using System.Numerics;
using Spacebox.Game.Player.GameModes;
using Spacebox.Network.Discovery;

namespace Spacebox.Game.GUI.Menu;

public static class ServerText
{
    public static readonly Vector4 OnlineColor = new Vector4(0.2f, 0.75f, 0.3f, 1f);
    public static readonly Vector4 ErrorColor = new Vector4(0.85f, 0.2f, 0.15f, 1f);
    public static readonly Vector4 PendingColor = new Vector4(0.35f, 0.35f, 0.35f, 1f);

    public static string FirstLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        int newLine = text.IndexOfAny(new[] { '\r', '\n' });
        return (newLine >= 0 ? text.Substring(0, newLine) : text).Trim();
    }

    public static string AddressLine(ServerListItem item)
    {
        return item.IsSaved ? item.Entry.Address : item.Entry.Address + "  (LAN)";
    }

    public static string StateLabel(ServerListItem item)
    {
        return item.State switch
        {
            ServerOnlineState.Online => $"{item.PingMs} ms",
            ServerOnlineState.Offline => "Offline",
            ServerOnlineState.Pinging => "Pinging...",
            _ => "-"
        };
    }

    public static Vector4 StateColor(ServerListItem item)
    {
        return item.State switch
        {
            ServerOnlineState.Online => OnlineColor,
            ServerOnlineState.Offline => ErrorColor,
            _ => PendingColor
        };
    }

    public static string PlayersLabel(ServerListItem item)
    {
        if (item.Status == null) return "? players";
        return item.Status.MaxPlayers > 0
            ? $"{item.Status.Players} / {item.Status.MaxPlayers} players"
            : $"{item.Status.Players} players";
    }

    public static string VersionLabel(ServerListItem item)
    {
        if (item.Status == null) return "";
        return item.IsCompatible ? "v" + item.Status.GameVersion : $"v{item.Status.GameVersion} (yours v{Engine.Application.Version})";
    }

    public static string GameModeLabel(byte gameMode)
    {
        return Enum.IsDefined(typeof(GameMode), (int)gameMode) ? ((GameMode)gameMode).ToString() : "Unknown";
    }
}

using System.Globalization;
using OpenTK.Mathematics;

namespace Spacebox.Network.Dedicated;

public readonly record struct WhoisInfo(
    string Name,
    int Peer,
    string Address,
    int PingMs,
    string GameMode,
    Vector3? Position,
    bool Alive,
    bool Admin,
    bool Muted,
    TimeSpan Session);

public static class WhoisReport
{
    public static List<string> Lines(WhoisInfo info)
    {
        var tags = new List<string>();
        if (info.Admin) tags.Add("admin");
        if (info.Muted) tags.Add("muted");
        if (!info.Alive) tags.Add("dead");

        return new List<string>
        {
            tags.Count > 0 ? $"{info.Name} [{string.Join(", ", tags)}]" : info.Name,
            $"  Peer {info.Peer}, address {(string.IsNullOrEmpty(info.Address) ? "unknown" : info.Address)}, ping {info.PingMs} ms",
            $"  Game mode: {info.GameMode}",
            $"  Position: {FormatPosition(info.Position)}",
            $"  Online for {FormatSession(info.Session)}"
        };
    }

    public static string FormatPosition(Vector3? position)
    {
        if (position == null) return "not spawned yet";
        var value = position.Value;
        return string.Format(CultureInfo.InvariantCulture, "{0:0.0} {1:0.0} {2:0.0}", value.X, value.Y, value.Z);
    }

    public static string FormatSession(TimeSpan session)
    {
        if (session < TimeSpan.Zero) session = TimeSpan.Zero;
        int hours = (int)session.TotalHours;
        return hours > 0 ? $"{hours} h {session.Minutes} min" : $"{session.Minutes} min {session.Seconds} s";
    }
}

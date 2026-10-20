using Engine.Web;
using Spacebox.Network.Dedicated;

namespace Spacebox.Network.Dashboard;

public interface IDashboardModule
{
    void Map(HttpRouter router, DashboardContext context);
}

public sealed class DashboardContext
{
    public ServerConfig Config { get; init; }
    public DashboardAdminStore Admins { get; init; }
    public DashboardSessions Sessions { get; init; }
    public LoginThrottle Throttle { get; init; }
    public DashboardGuard Guard { get; init; }
    public DashboardLog Log { get; init; }
    public DashboardMetrics Metrics { get; init; }
    public DashboardItemIcons Icons { get; init; }

    public static string Api(string path) => DashboardPaths.Api + path;

    public static void Audit(string user, string action)
    {
        Engine.Debug.Log($"[Dashboard] {user}: {action}");
    }
}

public sealed class NameRequest
{
    public string Name { get; set; }
}

public sealed class TextRequest
{
    public string Text { get; set; }
}

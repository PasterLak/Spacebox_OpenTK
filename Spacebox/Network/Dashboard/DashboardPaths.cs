namespace Spacebox.Network.Dashboard;

public static class DashboardPaths
{
    public const string Route = "/dashboard";
    public const string Api = Route + "/api";

    public static string DataFolder => Path.Combine(AppContext.BaseDirectory, "dashboard");
    public static string AdminFile => Path.Combine(DataFolder, "admin.json");
    public static string SetupKeyFile => Path.Combine(DataFolder, "setup-key.txt");
    public static string CertificatesFolder => Path.Combine(DataFolder, "certificates");
    public static string WebFolder => Path.Combine(AppContext.BaseDirectory, "Resources", "Dashboard");
    public static string FontFile => Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts", "StarPixel.ttf");
}

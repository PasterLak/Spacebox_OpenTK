using Engine.Web;

namespace Spacebox.Network.Dashboard.Modules;

public sealed class StaticFilesModule : IDashboardModule
{
    private static readonly Dictionary<string, string> ContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { ".html", "text/html; charset=utf-8" },
        { ".css", "text/css; charset=utf-8" },
        { ".js", "text/javascript; charset=utf-8" },
        { ".png", "image/png" },
        { ".svg", "image/svg+xml" },
        { ".ico", "image/x-icon" },
        { ".ttf", "font/ttf" }
    };

    private readonly string _webFolder;
    private readonly string _fontFile;

    public StaticFilesModule(string webFolder, string fontFile)
    {
        _webFolder = Path.GetFullPath(webFolder);
        _fontFile = fontFile;
    }

    public void Map(HttpRouter router, DashboardContext context)
    {
        router.Map("GET", "/", _ => HttpResponse.Redirect(DashboardPaths.Route + "/"));
        router.Map("GET", DashboardPaths.Route, _ => HttpResponse.Redirect(DashboardPaths.Route + "/"));
        router.Map("GET", DashboardPaths.Route + "/font.ttf", _ => File.Exists(_fontFile) ? HttpResponse.Bytes(File.ReadAllBytes(_fontFile), "font/ttf") : HttpResponse.NotFound());
        router.MapPrefix("GET", DashboardPaths.Route + "/", (_, rest) => Serve(string.IsNullOrEmpty(rest) ? "index.html" : rest));
    }

    public HttpResponse Serve(string relative)
    {
        if (!TryResolve(_webFolder, relative, out var path)) return HttpResponse.NotFound();
        if (!ContentTypes.TryGetValue(Path.GetExtension(path), out var contentType)) return HttpResponse.NotFound();

        return HttpResponse.Bytes(File.ReadAllBytes(path), contentType).WithHeader("Cache-Control", "no-cache");
    }

    public static bool TryResolve(string root, string relative, out string path)
    {
        path = null;
        if (string.IsNullOrEmpty(relative) || relative.Contains('\0')) return false;

        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string candidate = Path.GetFullPath(Path.Combine(fullRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!candidate.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(candidate)) return false;

        path = candidate;
        return true;
    }
}

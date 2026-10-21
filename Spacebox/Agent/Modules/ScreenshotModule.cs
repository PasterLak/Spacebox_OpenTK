using Engine;
using Engine.Web;

namespace Spacebox.Agent.Modules;

public sealed class ScreenshotModule : IAgentModule
{
    public static string Directory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AgentScreenshots");

    public void Map(AgentRouter router)
    {
        router.PostAsync("/screenshot", "Save the next rendered frame (with UI) as PNG: ?path=<file> (default AgentScreenshots/shot_<time>.png); returns the full path", async request =>
        {
            string path = Path.GetFullPath(AgentQuery.Text(request, "path") ?? Path.Combine(Directory, $"shot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png"));
            string folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder)) System.IO.Directory.CreateDirectory(folder);

            var size = await MainThread.RunAfterRender(() =>
            {
                var clientSize = EngineWindow.Instance.ClientSize;
                FramebufferCapture.SaveScreenshotUsingTexture(clientSize, path);
                return clientSize;
            });

            return AgentJson.Response(new { path, width = size.X, height = size.Y });
        });
    }
}

using Engine;
using Engine.CanvasUI;
using Engine.CanvasUI.Validation;
using Engine.Web;
using OpenTK.Mathematics;

namespace Spacebox.Agent.Modules;

public sealed class CanvasModule : IAgentModule
{
    public void Map(AgentRouter router)
    {
        router.GetAsync("/canvas", "CanvasUI layers with a layout check: screen, viewport, scale, safe area and layout issues; ?tree=1 adds the visible controls", async request =>
        {
            bool tree = AgentQuery.Bool(request, "tree");
            var layers = await MainThread.Run(() => CanvasLayer.Active.Select(layer => Describe(layer, tree)).ToList());
            return AgentJson.Response(new { layers });
        });

        router.PostAsync("/window/size", "Resize the game window: ?w=1280&h=720", async request =>
        {
            int width = Math.Max(320, AgentQuery.Int(request, "w", 1280));
            int height = Math.Max(240, AgentQuery.Int(request, "h", 720));
            var size = await MainThread.Run(() =>
            {
                var window = EngineWindow.Instance;
                window.ClientSize = new Vector2i(width, height);
                return new[] { window.ClientSize.X, window.ClientSize.Y };
            });
            return AgentJson.Response(new { size });
        });
    }

    private static object Describe(CanvasLayer layer, bool tree)
    {
        var root = layer.Root;
        var report = layer.Validate();
        return new
        {
            name = layer.Name,
            screen = new[] { root.ScreenSize.X, root.ScreenSize.Y },
            viewport = new[] { AgentGame.Round(root.ViewportSize.X), AgentGame.Round(root.ViewportSize.Y) },
            scale = AgentGame.Round(root.Scale),
            uiScale = root.Stretch.UiScale,
            safe = Rect(root.SafeRect),
            clean = report.IsClean,
            errors = report.ErrorCount,
            issues = report.Issues.Select(i => new { kind = i.Kind.ToString(), severity = i.Severity.ToString(), path = i.Path, rect = Rect(i.Rect), other = i.Other == null ? null : ControlPath.Of(i.Other), message = i.Message }),
            controls = tree ? Controls(root) : null
        };
    }

    private static List<object> Controls(CanvasRoot root)
    {
        var context = new LayoutContext(root, new LayoutRules());
        var list = new List<object>();
        foreach (var entry in context.Entries)
        {
            var control = entry.Control;
            if (context.IsTransparent(control)) continue;
            string text = control is Label label ? label.Text : control is Button button ? button.Text : null;
            list.Add(new { path = ControlPath.Of(control), type = control.GetType().Name, rect = Rect(control.Rect), layer = entry.Layer?.ToString(), text });
        }
        return list;
    }

    private static float[] Rect(Rect2 rect) => new[] { AgentGame.Round(rect.X), AgentGame.Round(rect.Y), AgentGame.Round(rect.Width), AgentGame.Round(rect.Height) };
}

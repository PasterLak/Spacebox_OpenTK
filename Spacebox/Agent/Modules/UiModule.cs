using Engine;
using Engine.Automation;
using Engine.Web;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Agent.Modules;

public sealed class UiModule : IAgentModule
{
    private static readonly TimeSpan CaptureTimeout = TimeSpan.FromSeconds(5);

    private sealed record UiMatch(UiWindowText Window, UiTextRun Run, bool Exact);

    public void Map(AgentRouter router)
    {
        router.GetAsync("/ui", "Text drawn by ImGui this frame, per window (top window first): ?window=<part of name>&all=1 (include windows without text)&flat=1 (just strings)", async request =>
        {
            var snapshot = await CaptureAsync();
            var context = await MainThread.Run(Context);
            string window = AgentQuery.Text(request, "window");
            bool all = AgentQuery.Bool(request, "all");
            bool flat = AgentQuery.Bool(request, "flat");

            var windows = snapshot.Windows
                .Where(w => (all || w.Texts.Count > 0) && (window == null || w.Id.Contains(window, StringComparison.OrdinalIgnoreCase)))
                .Select(w => flat
                    ? (object)new { window = w.Id, texts = w.Texts.Select(t => t.Text) }
                    : new { window = w.Id, name = w.Name, layer = w.Layer, rect = Rect(w.X, w.Y, w.Width, w.Height), texts = w.Texts.Select(t => new { text = t.Text, rect = Rect(t.X, t.Y, t.Width, t.Height) }) });

            return AgentJson.Response(new { context, display = new[] { snapshot.DisplayWidth, snapshot.DisplayHeight }, windows });
        });

        router.GetAsync("/ui/find", "Find text on screen: ?text=Craft&window=&exact=0", async request =>
        {
            var snapshot = await CaptureAsync();
            var matches = Find(snapshot, request);
            return AgentJson.Response(matches.Select(m => new { window = m.Window.Id, text = m.Run.Text, exact = m.Exact, center = new[] { AgentGame.Round(m.Run.CenterX), AgentGame.Round(m.Run.CenterY) } }));
        });

        router.PostAsync("/ui/click", "Click ImGui text (buttons, tabs, list items): ?text=Craft&window=&exact=0&index=0&button=left, or ?x=&y=", request => PointAsync(request, true));

        router.PostAsync("/ui/hover", "Move the mouse over ImGui text (tooltips): same parameters as /ui/click", request => PointAsync(request, false));
    }

    private static async Task<HttpResponse> PointAsync(HttpRequest request, bool click)
    {
        var button = Enum.TryParse(AgentQuery.Text(request, "button", "left"), true, out MouseButton parsed) ? parsed : MouseButton.Left;
        bool grabbed = await MainThread.Run(() => Input.GetCursorState() == CursorState.Grabbed);
        if (grabbed) throw new AgentRequestException("The cursor is captured by the game (no UI open); open a window first, e.g. POST /input/action?name=inventory or /input/key?key=Escape", 409);

        Vector2 point;
        object clicked;
        if (AgentQuery.Has(request, "x"))
        {
            point = new Vector2(AgentQuery.RequiredFloat(request, "x"), AgentQuery.RequiredFloat(request, "y"));
            clicked = null;
        }
        else
        {
            var snapshot = await CaptureAsync();
            var matches = Find(snapshot, request);
            int index = AgentQuery.Int(request, "index", 0);
            if (matches.Count == 0) throw new AgentRequestException($"Text '{AgentQuery.Text(request, "text")}' is not on screen, see GET /ui", 404);
            if (index < 0 || index >= matches.Count) throw new AgentRequestException($"index must be 0..{matches.Count - 1}");

            var match = matches[index];
            var scale = await MainThread.Run(() => new Vector2(EngineWindow.Instance.ClientSize.X / snapshot.DisplayWidth, EngineWindow.Instance.ClientSize.Y / snapshot.DisplayHeight));
            point = new Vector2(match.Run.CenterX, match.Run.CenterY) * scale;
            clicked = new { window = match.Window.Id, text = match.Run.Text, matches = matches.Count };
        }

        await MainThread.Run(() =>
        {
            if (click) InputInjector.Click(point, button);
            else InputInjector.MoveMouse(point);
        });

        return AgentJson.Response(new { ok = true, action = click ? "click" : "hover", point = new[] { AgentGame.Round(point.X), AgentGame.Round(point.Y) }, target = clicked });
    }

    private static List<UiMatch> Find(UiTextSnapshot snapshot, HttpRequest request)
    {
        string text = AgentQuery.RequiredText(request, "text").Trim();
        string window = AgentQuery.Text(request, "window");
        bool exactOnly = AgentQuery.Bool(request, "exact");

        var exact = new List<UiMatch>();
        var partial = new List<UiMatch>();
        foreach (var w in snapshot.Windows)
        {
            if (window != null && !w.Id.Contains(window, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var run in w.Texts)
            {
                string value = run.Text.Trim();
                if (value.Equals(text, StringComparison.OrdinalIgnoreCase)) exact.Add(new UiMatch(w, run, true));
                else if (!exactOnly && value.Contains(text, StringComparison.OrdinalIgnoreCase)) partial.Add(new UiMatch(w, run, false));
            }
        }

        exact.AddRange(partial);
        return exact;
    }

    private static async Task<UiTextSnapshot> CaptureAsync()
    {
        var capture = ImGuiTextCapture.CaptureNextFrameAsync();
        if (await Task.WhenAny(capture, Task.Delay(CaptureTimeout)) != capture)
            throw new AgentRequestException("No ImGui frame was rendered (no camera, minimized window or loading)", 503);
        return await capture;
    }

    private static object Context()
    {
        return new
        {
            cursor = Input.GetCursorState().ToString(),
            mouse = new[] { AgentGame.Round(Input.Mouse.Position.X), AgentGame.Round(Input.Mouse.Position.Y) },
            uiMode = UIManager.IsUIMode,
            openWindows = UIManager.OpenWindows.ToArray()
        };
    }

    private static float[] Rect(float x, float y, float w, float h) => new[] { AgentGame.Round(x), AgentGame.Round(y), AgentGame.Round(w), AgentGame.Round(h) };
}

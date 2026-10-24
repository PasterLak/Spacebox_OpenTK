using Engine;
using Engine.Automation;
using Engine.InputPro;
using Engine.Web;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Agent.Modules;

public sealed class InputModule : IAgentModule
{
    public void Map(AgentRouter router)
    {
        router.Post("/input/key", "Keyboard key: ?key=E&mode=tap|hold|release&frames=N (key names: W, E, Escape, Tab, Enter, Space, LeftShift, F3, 1..9)", request =>
        {
            var key = ParseKey(AgentQuery.RequiredText(request, "key"));
            return Apply(request, key, null);
        });

        router.Post("/input/action", "Input action from default_input.json: ?name=forward&mode=tap|hold|release&frames=N", request =>
        {
            string name = AgentQuery.RequiredText(request, "name");
            if (!InputManager.Instance.HasAction(name)) throw new AgentRequestException($"Unknown input action '{name}', see GET /input/actions");
            var action = InputManager.Instance.GetAction(name);
            foreach (var binding in action.Bindings)
            {
                if (binding is KeyBinding key) return Apply(request, key.Key, null);
                if (binding is MouseKeyBinding mouse) return Apply(request, null, mouse.Key);
            }
            throw new AgentRequestException($"Action '{name}' has no key or mouse binding");
        });

        router.Post("/input/mouse", "Mouse button: ?button=left|right|middle&mode=click|hold|release&frames=N[&x=&y= window pixels]", request =>
        {
            var button = ParseButton(AgentQuery.Text(request, "button", "left"));
            string mode = AgentQuery.Text(request, "mode", "click");
            if (mode == "click" && AgentQuery.Has(request, "x"))
            {
                InputInjector.Click(new Vector2(AgentQuery.RequiredFloat(request, "x"), AgentQuery.RequiredFloat(request, "y")), button);
                return Ok("click", button.ToString());
            }
            return Apply(request, null, button, mode == "click" ? "tap" : mode);
        });

        router.Post("/input/move", "Move the mouse cursor to ?x=&y= (window pixels)", request =>
        {
            InputInjector.MoveMouse(new Vector2(AgentQuery.RequiredFloat(request, "x"), AgentQuery.RequiredFloat(request, "y")));
            return Ok("move", "mouse");
        });

        router.Post("/input/text", "Type text into the focused text field: ?text=hello or body", request =>
        {
            string text = AgentQuery.TextOrBody(request, "text");
            InputInjector.TypeText(text);
            return Ok("type", text);
        });

        router.Post("/input/release", "Release every injected key and button", _ =>
        {
            InputInjector.ReleaseAll();
            return Ok("release", "all");
        });

        router.Get("/input/actions", "List input actions and their bindings", _ => AgentJson.Response(
            InputManager.Instance.GetAllActions().Select(a => new { name = a.id, bindings = a.action.Bindings.Select(b => b.GetDisplayName()) })));

        router.Get("/input/held", "Injected keys and buttons that are held now", _ => AgentJson.Response(new
        {
            keys = InputInjector.HeldKeys.Select(k => k.ToString()),
            buttons = InputInjector.HeldButtons.Select(b => b.ToString()),
            idle = InputInjector.IsIdle
        }));
    }

    private static HttpResponse Apply(HttpRequest request, Keys? key, MouseButton? button, string modeOverride = null)
    {
        if (!InputInjector.IsAvailable) throw new AgentRequestException("Input injection is not available for this OpenTK version", 501);

        string mode = modeOverride ?? AgentQuery.Text(request, "mode", "tap");
        int frames = AgentQuery.Int(request, "frames", mode == "tap" ? 1 : -1);
        string name = key?.ToString() ?? button.ToString();

        switch (mode)
        {
            case "tap":
            case "hold":
                if (key.HasValue) InputInjector.HoldKey(key.Value, frames);
                else InputInjector.HoldButton(button.Value, frames);
                break;
            case "release":
                if (key.HasValue) InputInjector.ReleaseKey(key.Value);
                else InputInjector.ReleaseButton(button.Value);
                break;
            default:
                throw new AgentRequestException($"Unknown mode '{mode}' (tap, hold, release)");
        }

        return Ok(mode, name, frames);
    }

    private static HttpResponse Ok(string mode, string input, int frames = 0)
    {
        return AgentJson.Response(new { ok = true, mode, input, frames });
    }

    private static Keys ParseKey(string value)
    {
        if (value.Length == 1 && char.IsDigit(value[0])) value = "D" + value;
        if (Enum.TryParse(value, true, out Keys key) && key != Keys.Unknown) return key;
        throw new AgentRequestException($"Unknown key '{value}'");
    }

    private static MouseButton ParseButton(string value)
    {
        if (Enum.TryParse(value, true, out MouseButton button)) return button;
        throw new AgentRequestException($"Unknown mouse button '{value}' (left, right, middle)");
    }
}

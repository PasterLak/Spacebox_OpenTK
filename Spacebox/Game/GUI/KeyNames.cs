using Engine.InputPro;

namespace Spacebox.Game.GUI;

public static class KeyNames
{
    private static readonly Dictionary<string, string> _names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Mouse Button1", "LMB" },
        { "Mouse Button2", "RMB" },
        { "Mouse Button3", "MMB" },
        { "Mouse Left", "LMB" },
        { "Mouse Right", "RMB" },
        { "Mouse Middle", "MMB" },
        { "LeftControl", "Ctrl" },
        { "RightControl", "Right Ctrl" },
        { "LeftShift", "Shift" },
        { "RightShift", "Right Shift" },
        { "LeftAlt", "Alt" },
        { "RightAlt", "Right Alt" },
        { "GraveAccent", "`" },
        { "Escape", "Esc" },
        { "Enter", "Enter" },
        { "Tab", "Tab" },
        { "Space", "Space" }
    };

    public static string Pretty(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return "?";
        if (_names.TryGetValue(displayName, out var name)) return name;
        if (displayName.Length == 2 && displayName[0] == 'D' && char.IsDigit(displayName[1])) return displayName.Substring(1);
        return displayName;
    }

    public static string ForAction(string action)
    {
        var input = InputManager.Instance;
        if (!input.HasAction(action)) return null;

        var inputAction = input.GetAction(action);
        return inputAction == null || !inputAction.HasBindings ? null : Pretty(inputAction.Bindings[0].GetDisplayName());
    }
}

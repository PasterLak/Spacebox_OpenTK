using System.Reflection;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.Automation;

internal static class OpenTkInputWriter
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static readonly Action<KeyboardState, Keys, bool> _setKey = Create<Action<KeyboardState, Keys, bool>>(typeof(KeyboardState), "SetKeyState");
    private static readonly Action<MouseState, MouseButton, bool> _setButton = Create<Action<MouseState, MouseButton, bool>>(typeof(MouseState), "set_Item");
    private static readonly Action<MouseState, Vector2> _setPosition = Create<Action<MouseState, Vector2>>(typeof(MouseState), "set_Position");

    public static bool IsAvailable => _setKey != null && _setButton != null && _setPosition != null;

    public static void SetKey(KeyboardState state, Keys key, bool down) => _setKey?.Invoke(state, key, down);

    public static void SetButton(MouseState state, MouseButton button, bool down) => _setButton?.Invoke(state, button, down);

    public static void SetPosition(MouseState state, Vector2 position) => _setPosition?.Invoke(state, position);

    private static T Create<T>(Type type, string methodName) where T : Delegate
    {
        var method = type.GetMethod(methodName, Flags);
        if (method == null)
        {
            Debug.Error($"[Automation] OpenTK member {type.Name}.{methodName} not found, input injection is disabled");
            return null;
        }

        return method.CreateDelegate<T>();
    }
}

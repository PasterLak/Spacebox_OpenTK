using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Game.Ships;

public static class ShipKeys
{
    public const int None = 0;
    public const int MouseLeft = -10;
    public const int MouseRight = -11;

    private static readonly Keys[] DefaultGroupKeys =
    {
        Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0
    };

    public static int DefaultKey(int group, bool weapons)
    {
        if (weapons && group == 0) return MouseLeft;
        return group >= 0 && group < DefaultGroupKeys.Length ? (int)DefaultGroupKeys[group] : None;
    }

    public static bool IsValid(int key)
    {
        return key == None || key == MouseLeft || key == MouseRight || (key > 0 && Enum.IsDefined(typeof(Keys), key));
    }

    public static bool IsHeld(int key)
    {
        return key switch
        {
            None => false,
            MouseLeft => Engine.Input.IsMouseButton(MouseButton.Left),
            MouseRight => Engine.Input.IsMouseButton(MouseButton.Right),
            _ => Engine.Input.IsKey((Keys)key)
        };
    }

    public static string GetName(int key)
    {
        return key switch
        {
            None => "-",
            MouseLeft => "LMB",
            MouseRight => "RMB",
            _ => Enum.IsDefined(typeof(Keys), key) ? ((Keys)key).ToString() : key.ToString()
        };
    }
}

using OpenTK.Mathematics;

namespace Spacebox.Game.GUI;

public static class Chat
{
    public static bool FocusInput => GameUi.Current?.Chat.FocusInput ?? false;

    public static void Write(string text) => GameUi.Current?.Chat.Write(text);

    public static void Write(string text, Color4 color) => GameUi.Current?.Chat.Write(text, color);

    public static void CancelInput() => GameUi.Current?.Chat.CancelInput();
}

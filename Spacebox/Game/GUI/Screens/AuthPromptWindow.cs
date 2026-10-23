using Spacebox.Network;

namespace Spacebox.Game.GUI;

public static class AuthPromptWindow
{
    public static void Cancel()
    {
        ClientConnector.Cancel();
        ConnectionScreen.Hide();
    }
}

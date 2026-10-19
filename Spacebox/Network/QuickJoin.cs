using Engine;
using Engine.SceneManagement;
using Spacebox.Game.GUI;
using Spacebox.Scenes;

namespace Spacebox.Network;

public static class QuickJoin
{
    public static void Start(string address, int port, string playerName)
    {
        GameSession.SetPlayer(string.IsNullOrWhiteSpace(playerName) ? "Player" + new Random().Next(1000, 9999) : playerName, "Yellow");
        Debug.Log($"[Network] Quick join {address}:{port} as {GameSession.PlayerName}");

        if (SceneManager.Current is not MenuScene) SceneManager.Load<MenuScene>();
        ConnectionScreen.Show($"Joining {address}:{port}");
        ClientConnector.Connect(address, port, OnAccepted, OnFailed);
    }

    private static void OnAccepted(RemoteWorldInfo world)
    {
        SceneManager.Load<LocalSpaceScene, SpaceSceneArgs>(world.ToSceneArgs(GameSession.SkinColor, GameSession.PlayerName));
    }

    private static void OnFailed(string reason)
    {
        Debug.Error($"[Network] Quick join failed: {reason}");
        ConnectionScreen.ShowError("Connection failed", reason);
        if (SceneManager.Current is not MenuScene) SceneManager.Load<MenuScene>();
    }
}

using Engine;
using Engine.SceneManagement;
using Spacebox.Scenes;

namespace Spacebox.Network.Dedicated;

public static class ServerSleep
{
    public const string WakingReason = "The server is starting, please wait";

    public static bool IsSleeping { get; private set; }

    public static void Enter(float emptyMinutes)
    {
        if (IsSleeping || !GameSession.IsDedicated) return;

        Debug.Warning($"[Server] Nobody was online for {emptyMinutes:0.#} min: saving and unloading the world until someone joins");
        ServerRestart.Save("Sleep");
        NetworkSession.Current?.Dispose();
        IsSleeping = true;
        SceneManager.Load<ServerSleepScene>();
    }

    public static void Wake()
    {
        if (!IsSleeping) return;

        Debug.Success("[Server] A player is joining: loading the world");
        DedicatedServer.Relaunch();
    }

    internal static void Left() => IsSleeping = false;
}

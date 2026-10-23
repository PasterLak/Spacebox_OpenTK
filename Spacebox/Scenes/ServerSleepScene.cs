using Engine;
using Engine.Networking;
using Engine.SceneManagement;
using Spacebox.Network;
using Spacebox.Network.Dedicated;
using Spacebox.Network.Messages;

namespace Spacebox.Scenes;

public sealed class ServerSleepScene : Scene
{
    private const int SleepUpdateRate = 5;
    private const float WakeDelay = 0.5f;

    private float _wakeTimer = -1f;

    public override void LoadContent()
    {
        FrameLimiter.TargetFPS = SleepUpdateRate;

        var network = NetworkManager.Instance;
        network.ConnectionKey = "Spacebox:" + Application.Version;
        network.Messages.Register<JoinRequestMessage>(OnJoinRequest);
        network.Messages.AllowBeforeAdmission<JoinRequestMessage>();
        network.RequireAdmission = true;
        if (!network.StartDedicated(GameSession.Port))
        {
            Debug.Error("[Server] Could not keep the port open while sleeping, loading the world again");
            _wakeTimer = 0f;
            return;
        }

        Spacebox.Network.Discovery.ServerAdvertiser.Start(NetworkSession.CreateAdvertisement());
    }

    public override void Update()
    {
        base.Update();
        if (_wakeTimer < 0f) return;

        _wakeTimer -= Time.Delta;
        if (_wakeTimer <= 0f) ServerSleep.Wake();
    }

    private void OnJoinRequest(int sender, JoinRequestMessage message)
    {
        var network = NetworkManager.Instance;
        string reason = JoinRequestRules.Validate(message, network.GetPeerAddress(sender));
        network.Send(sender, new JoinRejectedMessage { Reason = reason ?? ServerSleep.WakingReason });
        network.Kick(sender, reason ?? ServerSleep.WakingReason);
        if (reason == null && _wakeTimer < 0f) _wakeTimer = WakeDelay;
    }

    public override void UnloadContent()
    {
        var network = NetworkManager.Instance;
        network.Messages.Unregister<JoinRequestMessage>();
        network.RequireAdmission = false;
        network.Shutdown("Waking up");
        Spacebox.Network.Discovery.ServerAdvertiser.Stop();
        ServerSleep.Left();
    }
}

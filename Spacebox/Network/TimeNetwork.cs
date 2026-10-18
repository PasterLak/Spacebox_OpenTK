using Engine;
using Engine.Networking;
using Spacebox.Game.Resource;

namespace Spacebox.Network;

public struct TimeSyncMessage : INetMessage
{
    public int Day;
    public int Tick;

    public void Serialize(NetWriter writer)
    {
        writer.Write(Day);
        writer.Write(Tick);
    }

    public void Deserialize(NetReader reader)
    {
        Day = reader.ReadInt();
        Tick = reader.ReadInt();
    }
}

public static class TimeNetwork
{
    private const float BroadcastInterval = 10f;
    private const int MaxTickDrift = 40;

    private static float _timer;

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<TimeSyncMessage>(OnTimeSync);
    }

    public static void SendTo(int peer)
    {
        NetworkManager.Instance.Send(peer, Create());
    }

    public static void Update()
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || network.Peers.Count == 0) return;

        _timer += Time.Delta;
        if (_timer < BroadcastInterval) return;

        _timer = 0f;
        network.SendToClients(Create());
    }

    private static TimeSyncMessage Create()
    {
        return new TimeSyncMessage { Day = GameTime.Day, Tick = GameTime.DayTick };
    }

    private static void OnTimeSync(int sender, TimeSyncMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;

        if (GameTime.Day != message.Day) GameTime.SetDay(message.Day);
        if (Math.Abs(GameTime.DayTick - message.Tick) > MaxTickDrift) GameTime.SetTick(message.Tick);
    }
}

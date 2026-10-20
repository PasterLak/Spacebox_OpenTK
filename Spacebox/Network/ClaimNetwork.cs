using Engine.Networking;
using Spacebox.Game.Protection;

namespace Spacebox.Network;

public struct ClaimSettingsMessage : INetMessage
{
    public string Json;

    public void Serialize(NetWriter writer) => writer.Write(Json ?? "");

    public void Deserialize(NetReader reader) => Json = reader.ReadString();
}

public static class ClaimNetwork
{
    public static void Register(NetworkManager network)
    {
        network.Messages.Register<ClaimSettingsMessage>(OnSettings);
        ClaimMembersNetwork.Register(network);
    }

    public static void SendSettingsTo(int peer)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || peer == network.LocalPeerId) return;
        network.Send(peer, new ClaimSettingsMessage { Json = ClaimSettings.Current.ToJson() });
    }

    public static void BroadcastSettings()
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || network.Peers.Count == 0) return;
        network.SendToClients(new ClaimSettingsMessage { Json = ClaimSettings.Current.ToJson() });
    }

    private static void OnSettings(int sender, ClaimSettingsMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;
        ClaimSettings.SetOverride(ClaimSettings.FromJson(message.Json));
    }
}

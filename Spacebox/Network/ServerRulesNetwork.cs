using Engine.Networking;
using Spacebox.Game.Rules;

namespace Spacebox.Network;

public struct ServerRulesMessage : INetMessage
{
    public string Json;

    public void Serialize(NetWriter writer) => writer.Write(Json ?? "");

    public void Deserialize(NetReader reader) => Json = reader.ReadString();
}

public static class ServerRulesNetwork
{
    public static void Register(NetworkManager network)
    {
        network.Messages.Register<ServerRulesMessage>(OnRules);
    }

    public static void SendTo(int peer)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || peer == network.LocalPeerId) return;
        network.Send(peer, new ServerRulesMessage { Json = ServerRules.Current.ToJson() });
    }

    public static void Broadcast()
    {
        var network = NetworkManager.Instance;
        if (!network.IsActive || !network.IsServer || network.Peers.Count == 0) return;
        network.SendToClients(new ServerRulesMessage { Json = ServerRules.Current.ToJson() });
    }

    private static void OnRules(int sender, ServerRulesMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;
        ServerRules.SetOverride(ServerRules.FromJson(message.Json));
    }
}

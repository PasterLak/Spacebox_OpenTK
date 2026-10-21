using Engine.Networking;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Network.Containers;
using Spacebox.Network.Messages;

namespace Spacebox.Network;

public struct SignTextMessage : INetMessage
{
    public ContainerKey Key;
    public string Text;

    public void Serialize(NetWriter writer)
    {
        Key.Write(writer);
        writer.Write(Text ?? "");
    }

    public void Deserialize(NetReader reader)
    {
        Key = ContainerKey.Read(reader);
        Text = reader.ReadString() ?? "";
    }
}

public static class SignNetwork
{
    public const int MaxMessageLength = 4096;

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<SignTextMessage>(OnText);
    }

    public static void SetText(SignBlock sign, string text)
    {
        if (sign == null) return;

        if (!NetworkManager.Instance.IsRemoteClient || sign.Entity == null)
        {
            sign.SetText(text);
            return;
        }

        string sanitized = sign.Data.SanitizeText(text);
        if (sanitized == sign.Text) return;

        NetworkManager.Instance.SendToServer(new SignTextMessage
        {
            Key = new ContainerKey(sign.Entity.EntityID, sign.LocalCell),
            Text = sanitized
        });
    }

    private static void OnText(int sender, SignTextMessage message)
    {
        if (!NetworkManager.Instance.IsServer) return;
        if (message.Text == null || message.Text.Length > MaxMessageLength) return;
        if (!ContainerResolver.TryResolveBlock(message.Key, out var block, out _) || block is not SignBlock sign || sign.Entity == null) return;
        if (ProtectionNetwork.BlocksPeer(sender, sign.Entity, sign.LocalCell))
        {
            ProtectionNetwork.Deny(sender);
            return;
        }
        if (!WorldEditNetwork.IsInReach(sender, sign.Entity, sign.LocalCell)) return;

        sign.SetText(message.Text);
    }
}

using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Network.Messages;

namespace Spacebox.Network;

public static class ChatService
{
    public static void Register(NetworkManager network)
    {
        network.Messages.Register<ChatSendMessage>(OnChatSend);
        network.Messages.Register<ChatBroadcastMessage>(OnChatBroadcast);
    }

    public static void Send(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var network = NetworkManager.Instance;
        if (!network.IsActive)
        {
            Chat.Write(text, Color4.White);
            return;
        }

        network.SendToServer(new ChatSendMessage { Text = text });
    }

    public static void Broadcast(string text, Color4 color)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer) return;

        network.SendToClients(new ChatBroadcastMessage { Text = text, Color = Pack(color) });
        Show(text, color);
    }

    private static void OnChatSend(int sender, ChatSendMessage message)
    {
        if (!NetworkManager.Instance.IsServer || string.IsNullOrWhiteSpace(message.Text)) return;

        string name = sender == NetworkManager.Instance.LocalPeerId ? GameSession.PlayerName : PlayerRegistry.GetName(sender);
        string text = message.Text.Length > 256 ? message.Text.Substring(0, 256) : message.Text;
        Broadcast($"[{name}]: {text}", Color4.White);
    }

    private static void OnChatBroadcast(int sender, ChatBroadcastMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;
        Show(message.Text, Unpack(message.Color));
    }

    private static void Show(string text, Color4 color)
    {
        if (GameSession.IsDedicated)
            Debug.Log("[Chat] " + text);
        else
            Chat.Write(text, color);
    }

    private static uint Pack(Color4 color)
    {
        return ((uint)(color.R * 255) << 24) | ((uint)(color.G * 255) << 16) | ((uint)(color.B * 255) << 8) | (uint)(color.A * 255);
    }

    private static Color4 Unpack(uint value)
    {
        return new Color4(((value >> 24) & 255) / 255f, ((value >> 16) & 255) / 255f, ((value >> 8) & 255) / 255f, (value & 255) / 255f);
    }
}

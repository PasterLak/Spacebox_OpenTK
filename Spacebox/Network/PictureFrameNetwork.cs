using Engine.Networking;
using Spacebox.Game;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Network.Containers;
using Spacebox.Network.Messages;

namespace Spacebox.Network;

public struct PictureFrameSwapMessage : INetMessage
{
    public ContainerKey Key;
    public short PictureId;
    public int RequestId;

    public void Serialize(NetWriter writer)
    {
        Key.Write(writer);
        writer.Write(PictureId);
        writer.Write(RequestId);
    }

    public void Deserialize(NetReader reader)
    {
        Key = ContainerKey.Read(reader);
        PictureId = reader.ReadShort();
        RequestId = reader.ReadInt();
    }
}

public struct PictureFrameSwapResultMessage : INetMessage
{
    public int RequestId;
    public bool Granted;
    public short RemovedId;

    public void Serialize(NetWriter writer)
    {
        writer.Write(RequestId);
        writer.Write(Granted);
        writer.Write(RemovedId);
    }

    public void Deserialize(NetReader reader)
    {
        RequestId = reader.ReadInt();
        Granted = reader.ReadBool();
        RemovedId = reader.ReadShort();
    }
}

public static class PictureFrameNetwork
{
    private static readonly Dictionary<int, Action<bool, PictureItem>> _callbacks = new Dictionary<int, Action<bool, PictureItem>>();
    private static int _nextRequest = 1;

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<PictureFrameSwapMessage>(OnSwap);
        network.Messages.Register<PictureFrameSwapResultMessage>(OnResult);
    }

    public static void RequestSwap(PictureFrameBlock frame, PictureItem picture, Action<bool, PictureItem> callback)
    {
        if (frame == null) return;

        if (!NetworkManager.Instance.IsRemoteClient || frame.Entity == null)
        {
            bool granted = frame.TrySwap(picture, out var removed);
            callback(granted, removed);
            return;
        }

        int requestId = _nextRequest++;
        _callbacks[requestId] = callback;
        NetworkManager.Instance.SendToServer(new PictureFrameSwapMessage
        {
            Key = new ContainerKey(frame.Entity.EntityID, frame.LocalCell),
            PictureId = picture?.Id ?? PictureFrameBlock.NoPicture,
            RequestId = requestId
        });
    }

    public static void Reset()
    {
        _callbacks.Clear();
    }

    private static void OnSwap(int sender, PictureFrameSwapMessage message)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer) return;

        bool granted = TrySwap(sender, message, out var removed);
        network.Send(sender, new PictureFrameSwapResultMessage
        {
            RequestId = message.RequestId,
            Granted = granted,
            RemovedId = removed?.Id ?? PictureFrameBlock.NoPicture
        });
    }

    private static bool TrySwap(int sender, PictureFrameSwapMessage message, out PictureItem removed)
    {
        removed = null;
        if (!ContainerResolver.TryResolveBlock(message.Key, out var block, out _) || block is not PictureFrameBlock frame || frame.Entity == null) return false;
        if (ProtectionNetwork.BlocksPeer(sender, frame.Entity, frame.LocalCell))
        {
            ProtectionNetwork.Deny(sender);
            return false;
        }
        if (!WorldEditNetwork.IsInReach(sender, frame.Entity, frame.LocalCell)) return false;

        PictureItem picture = null;
        if (message.PictureId != PictureFrameBlock.NoPicture)
        {
            if (!GameAssets.Items.TryGetValue(message.PictureId, out var item) || item is not PictureItem requested) return false;
            picture = requested;
        }

        return frame.TrySwap(picture, out removed);
    }

    private static void OnResult(int sender, PictureFrameSwapResultMessage message)
    {
        if (NetworkManager.Instance.IsServer || !_callbacks.Remove(message.RequestId, out var callback)) return;

        var removed = message.RemovedId != PictureFrameBlock.NoPicture && GameAssets.Items.TryGetValue(message.RemovedId, out var item) ? item as PictureItem : null;
        callback(message.Granted, removed);
    }
}

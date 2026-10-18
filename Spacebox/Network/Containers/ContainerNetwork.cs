using Engine;
using Engine.Networking;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Network.Messages;

namespace Spacebox.Network.Containers;

public static class ContainerNetwork
{
    private static readonly ContainerServer _server = new ContainerServer();
    private static readonly ContainerClient _client = new ContainerClient();
    private static readonly Dictionary<int, Action<bool>> _takeCallbacks = new Dictionary<int, Action<bool>>();
    private static int _nextTakeRequest = 1;
    private static IContainer _localOpenBlock;
    private static ContainerKey _localOpenKey;

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<ContainerOpenMessage>((peer, message) => { if (network.IsServer && peer != network.LocalPeerId) _server.Open(peer, message.Key); });
        network.Messages.Register<ContainerCloseMessage>((peer, message) => { if (network.IsServer) _server.Close(peer, message.Key); });
        network.Messages.Register<ContainerChangeMessage>((peer, message) => { if (network.IsServer && peer != network.LocalPeerId) _server.HandleChange(peer, message); });
        network.Messages.Register<ContainerSnapshotMessage>((peer, message) => { if (!network.IsServer) _client.OnSnapshot(message); });
        network.Messages.Register<ContainerChangeResultMessage>((peer, message) => { if (!network.IsServer) _client.OnResult(message); });
        network.Messages.Register<ContainerTakeRequestMessage>(OnTakeRequest);
        network.Messages.Register<ContainerTakeResultMessage>(OnTakeResult);
        network.PeerDisconnected += (peer, _) => _server.RemovePeer(peer);
    }

    public static void OnInteract(IContainerBlock block, Chunk chunk, Vector3Byte index)
    {
        if (chunk?.SpaceEntity == null) return;

        Track(block, ContainerResolver.KeyFor(chunk, index));
    }

    public static void OnInteract(IContainer container, NetworkObject networkObject)
    {
        if (networkObject == null || !networkObject.IsSpawned) return;

        Track(container, ContainerKey.ForObject(networkObject.NetId));
    }

    private static void Track(IContainer container, ContainerKey key)
    {
        _localOpenBlock = container;
        _localOpenKey = key;

        if (NetworkManager.Instance.IsRemoteClient) _client.Open(container, key);
    }

    public static bool CanDropFrom(ItemSlot slot)
    {
        if (!NetworkManager.Instance.IsRemoteClient) return true;
        if (_client.HasPendingChanges) return false;
        return slot == null || !_client.IsMirrored(slot.Storage);
    }

    public static void RequestTake(ContainerKey key, Item item, int amount, Action<bool> callback)
    {
        var network = NetworkManager.Instance;

        if (network.IsServer)
        {
            callback(_server.TryTake(key, item.Id, amount));
            return;
        }

        int requestId = _nextTakeRequest++;
        _takeCallbacks[requestId] = callback;
        network.SendToServer(new ContainerTakeRequestMessage { Key = key, ItemId = item.Id, Amount = amount, RequestId = requestId });
    }

    public static void Update()
    {
        CloseWindowIfContainerRemoved();

        var network = NetworkManager.Instance;
        if (network.IsServer) _server.Update();
        else if (network.IsRemoteClient) _client.Update();
    }

    private static void CloseWindowIfContainerRemoved()
    {
        if (_localOpenBlock == null) return;

        if (!UIManager.IsOpen(_localOpenBlock.ContainerWindowId))
        {
            _localOpenBlock = null;
            return;
        }

        if (ContainerResolver.TryResolve(_localOpenKey, out var current) && current == _localOpenBlock) return;

        _localOpenBlock = null;
        UIManager.CloseAll();
    }

    public static void Reset()
    {
        _localOpenBlock = null;
        _server.Clear();
        _client.Clear();
        _takeCallbacks.Clear();
    }

    private static void OnTakeRequest(int peer, ContainerTakeRequestMessage message)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer) return;

        bool granted = _server.TryTake(message.Key, message.ItemId, message.Amount);
        network.Send(peer, new ContainerTakeResultMessage { RequestId = message.RequestId, Granted = granted });
    }

    private static void OnTakeResult(int peer, ContainerTakeResultMessage message)
    {
        if (NetworkManager.Instance.IsServer || !_takeCallbacks.Remove(message.RequestId, out var callback)) return;
        callback(message.Granted);
    }
}

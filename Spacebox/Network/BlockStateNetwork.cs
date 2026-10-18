using Engine;
using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Network.Containers;
using Spacebox.Network.Messages;

namespace Spacebox.Network;

public struct BlockStateMessage : INetMessage
{
    public ContainerKey Key;
    public byte[] State;

    public void Serialize(NetWriter writer)
    {
        Key.Write(writer);
        writer.WriteBytes(State ?? Array.Empty<byte>());
    }

    public void Deserialize(NetReader reader)
    {
        Key = ContainerKey.Read(reader);
        State = reader.ReadBytes();
    }
}

public static class BlockStateNetwork
{
    private static readonly Dictionary<ContainerKey, INetworkStateBlock> _dirty = new Dictionary<ContainerKey, INetworkStateBlock>();
    private static readonly NetWriter _writer = new NetWriter(64);
    private static readonly NetReader _reader = new NetReader();

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<BlockStateMessage>(OnBlockState);
    }

    public static void MarkDirty(SpaceEntity entity, Vector3i cell, INetworkStateBlock block)
    {
        if (entity == null || !NetworkManager.Instance.IsServer) return;
        _dirty[new ContainerKey(entity.EntityID, cell)] = block;
    }

    public static void Update()
    {
        if (_dirty.Count == 0) return;

        var network = NetworkManager.Instance;
        if (!network.IsServer || network.Peers.Count == 0)
        {
            _dirty.Clear();
            return;
        }

        foreach (var pair in _dirty)
        {
            _writer.Reset();
            pair.Value.WriteNetworkState(_writer);
            network.SendToClients(new BlockStateMessage { Key = pair.Key, State = _writer.ToArray() });
        }

        _dirty.Clear();
    }

    public static void Clear()
    {
        _dirty.Clear();
    }

    private static void OnBlockState(int sender, BlockStateMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;
        if (!ContainerResolver.TryResolveBlock(message.Key, out var block, out _) || block is not INetworkStateBlock stateBlock) return;

        _reader.Set(message.State, 0, message.State.Length);
        try
        {
            stateBlock.ReadNetworkState(_reader);
        }
        catch (Exception ex)
        {
            Debug.Error($"[BlockState] Bad state for block at {message.Key.Cell}: {ex.Message}");
        }
    }
}

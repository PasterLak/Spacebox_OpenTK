using Engine.Networking;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Network.Messages;

namespace Spacebox.Network.Containers;

public struct StorageNameMessage : INetMessage
{
    public ContainerKey Key;
    public string Name;

    public void Serialize(NetWriter writer)
    {
        Key.Write(writer);
        writer.Write(Name);
    }

    public void Deserialize(NetReader reader)
    {
        Key = ContainerKey.Read(reader);
        Name = reader.ReadString();
    }
}

public static class StorageNameNetwork
{
    public const int MaxNameLength = 32;

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<StorageNameMessage>(OnStorageName);
    }

    public static void Rename(StorageBlock block, string name)
    {
        name = Sanitize(name);
        if (name == null) return;

        var network = NetworkManager.Instance;
        if (block.chunk?.SpaceEntity == null || !network.IsActive)
        {
            block.Name = name;
            return;
        }

        var message = new StorageNameMessage
        {
            Key = ContainerResolver.KeyFor(block.chunk, StorageBlock.PositionIndexToPositionInChunk(block.PositionIndex)),
            Name = name
        };

        if (network.IsServer)
        {
            block.Name = name;
            network.SendToClients(message);
        }
        else
        {
            network.SendToServer(message);
        }
    }

    private static void OnStorageName(int sender, StorageNameMessage message)
    {
        var network = NetworkManager.Instance;
        string name = Sanitize(message.Name);
        if (name == null || !ContainerResolver.TryResolve(message.Key, out var container) || container is not StorageBlock block) return;

        if (network.IsServer)
        {
            if (sender == network.LocalPeerId) return;

            block.Name = name;
            network.SendToClients(new StorageNameMessage { Key = message.Key, Name = name });
            return;
        }

        block.Name = name;
    }

    private static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        name = name.Trim();
        return name.Length > MaxNameLength ? name.Substring(0, MaxNameLength) : name;
    }
}

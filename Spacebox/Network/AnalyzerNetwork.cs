using Engine.Networking;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Network.Containers;
using Spacebox.Network.Messages;

namespace Spacebox.Network;

public enum AnalyzerAction : byte
{
    Start,
    Cancel,
    Clear
}

public struct AnalyzerActionMessage : INetMessage
{
    public ContainerKey Key;
    public AnalyzerAction Action;

    public void Serialize(NetWriter writer)
    {
        Key.Write(writer);
        writer.Write((byte)Action);
    }

    public void Deserialize(NetReader reader)
    {
        Key = ContainerKey.Read(reader);
        Action = (AnalyzerAction)reader.ReadByte();
    }
}

public static class AnalyzerNetwork
{
    private static readonly List<AnalyzerBlock> _buffer = new List<AnalyzerBlock>();

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<AnalyzerActionMessage>(OnAction);
    }

    public static void Request(AnalyzerBlock block, AnalyzerAction action)
    {
        if (!NetworkManager.Instance.IsRemoteClient)
        {
            Apply(block, action);
            return;
        }

        if (block.Entity == null) return;
        NetworkManager.Instance.SendToServer(new AnalyzerActionMessage
        {
            Key = new ContainerKey(block.Entity.EntityID, block.LocalCell),
            Action = action
        });
    }

    public static void Update()
    {
        if (!NetworkManager.Instance.IsServer) return;

        _buffer.Clear();
        foreach (var analyzer in AnalyzerRegistry.All)
        {
            if (analyzer.IsScanning) _buffer.Add(analyzer);
        }

        for (int i = 0; i < _buffer.Count; i++) AnalyzerScanner.Update(_buffer[i]);
        _buffer.Clear();
    }

    public static void Reset()
    {
        _buffer.Clear();
        AnalyzerRegistry.Clear();
    }

    private static void OnAction(int sender, AnalyzerActionMessage message)
    {
        if (!NetworkManager.Instance.IsServer) return;
        if (!ContainerResolver.TryResolveBlock(message.Key, out var block, out _) || block is not AnalyzerBlock analyzer) return;
        if (ProtectionNetwork.BlocksPeerKey(sender, message.Key) || !Containers.ContainerServer.IsInReach(sender, message.Key, WorldEditNetwork.MaxEditDistance)) return;

        Apply(analyzer, message.Action);
    }

    private static void Apply(AnalyzerBlock block, AnalyzerAction action)
    {
        switch (action)
        {
            case AnalyzerAction.Start:
                AnalyzerScanner.Start(block);
                break;
            case AnalyzerAction.Cancel:
                AnalyzerScanner.Cancel(block);
                break;
            case AnalyzerAction.Clear:
                AnalyzerScanner.Clear(block);
                break;
        }
    }
}

using Engine.Networking;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Network.Containers;
using Spacebox.Network.Messages;

namespace Spacebox.Network;

public struct GravityConfigMessage : INetMessage
{
    public ContainerKey Key;
    public int Radius;
    public float Strength;
    public bool Enabled;
    public bool InsideOnly;

    public void Serialize(NetWriter writer)
    {
        Key.Write(writer);
        writer.Write(Radius);
        writer.Write(Strength);
        writer.Write(Enabled);
        writer.Write(InsideOnly);
    }

    public void Deserialize(NetReader reader)
    {
        Key = ContainerKey.Read(reader);
        Radius = reader.ReadInt();
        Strength = reader.ReadFloat();
        Enabled = reader.ReadBool();
        InsideOnly = reader.ReadBool();
    }
}

public static class GravityConfigNetwork
{
    private static GravityConfigMessage _lastSent;
    private static bool _hasLastSent;

    public static void Register(NetworkManager network)
    {
        network.Messages.Register<GravityConfigMessage>(OnConfig);
    }

    public static void Configure(GravityGeneratorBlock block, int radius, float strength, bool enabled, bool insideOnly)
    {
        if (!NetworkManager.Instance.IsRemoteClient || block.Entity == null)
        {
            block.Configure(radius, strength, enabled, insideOnly);
            return;
        }

        if (radius == block.RadiusSetting && strength == block.Strength && enabled == block.IsGravityEnabled && insideOnly == block.InsideOnly) return;

        var message = new GravityConfigMessage
        {
            Key = new ContainerKey(block.Entity.EntityID, block.LocalCell),
            Radius = radius,
            Strength = strength,
            Enabled = enabled,
            InsideOnly = insideOnly
        };

        if (_hasLastSent && message.Key.Equals(_lastSent.Key) && message.Radius == _lastSent.Radius && message.Strength == _lastSent.Strength &&
            message.Enabled == _lastSent.Enabled && message.InsideOnly == _lastSent.InsideOnly) return;

        _lastSent = message;
        _hasLastSent = true;
        NetworkManager.Instance.SendToServer(message);
    }

    private static void OnConfig(int sender, GravityConfigMessage message)
    {
        if (!NetworkManager.Instance.IsServer) return;
        if (!ContainerResolver.TryResolveBlock(message.Key, out var block, out _) || block is not GravityGeneratorBlock gravity) return;
        if (ProtectionNetwork.BlocksPeerKey(sender, message.Key)) return;

        gravity.Configure(message.Radius, message.Strength, message.Enabled, message.InsideOnly);
        gravity.ApplyPendingPower();
    }
}

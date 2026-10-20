using Engine.Networking;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Protection;
using Spacebox.Network.Containers;
using Spacebox.Network.Messages;

namespace Spacebox.Network;

public struct ClaimMembersMessage : INetMessage
{
    public ContainerKey Key;
    public string Name;
    public bool Add;

    public void Serialize(NetWriter writer)
    {
        Key.Write(writer);
        writer.Write(Name ?? "");
        writer.Write(Add);
    }

    public void Deserialize(NetReader reader)
    {
        Key = ContainerKey.Read(reader);
        Name = reader.ReadString();
        Add = reader.ReadBool();
    }
}

public static class ClaimMembersNetwork
{
    public static void Register(NetworkManager network)
    {
        network.Messages.Register<ClaimMembersMessage>(OnMembers);
    }

    public static bool CanManage(ClaimBeaconBlock beacon, string player, bool isAdmin)
    {
        return beacon != null && (isAdmin || ClaimMembership.SameName(beacon.Owner, player));
    }

    public static void Request(ClaimBeaconBlock beacon, string name, bool add)
    {
        if (beacon?.Entity == null) return;

        if (!NetworkManager.Instance.IsRemoteClient)
        {
            Apply(beacon, name, add);
            return;
        }

        NetworkManager.Instance.SendToServer(new ClaimMembersMessage
        {
            Key = new ContainerKey(beacon.Entity.EntityID, beacon.LocalCell),
            Name = ClaimMembership.Sanitize(name),
            Add = add
        });
    }

    private static void OnMembers(int sender, ClaimMembersMessage message)
    {
        if (!NetworkManager.Instance.IsServer) return;
        if (!ContainerResolver.TryResolveBlock(message.Key, out var block, out _) || block is not ClaimBeaconBlock beacon) return;

        string player = PlayerRegistry.GetName(sender);
        if (!CanManage(beacon, player, ProtectionNetwork.CanPeerEdit(sender)))
        {
            ProtectionNetwork.Deny(sender);
            return;
        }

        Apply(beacon, message.Name, message.Add);
    }

    private static void Apply(ClaimBeaconBlock beacon, string name, bool add)
    {
        if (add) beacon.TryAddMember(name);
        else beacon.RemoveMember(name);
    }
}

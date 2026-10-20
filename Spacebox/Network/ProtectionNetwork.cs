using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.Protection;
using Spacebox.Network.Admin;

namespace Spacebox.Network;

public struct ProtectionStateMessage : INetMessage
{
    public long[] EntityIds;
    public bool CanEdit;

    public void Serialize(NetWriter writer)
    {
        writer.Write(EntityIds.Length);
        for (int i = 0; i < EntityIds.Length; i++) writer.Write(EntityIds[i]);
        writer.Write(CanEdit);
    }

    public void Deserialize(NetReader reader)
    {
        int count = reader.ReadInt();
        EntityIds = new long[count];
        for (int i = 0; i < count; i++) EntityIds[i] = reader.ReadLong();
        CanEdit = reader.ReadBool();
    }
}

public struct ProtectionDeniedMessage : INetMessage
{
    public void Serialize(NetWriter writer)
    {
    }

    public void Deserialize(NetReader reader)
    {
    }
}

public static class ProtectionNetwork
{
    public static void Register(NetworkManager network)
    {
        network.Messages.Register<ProtectionStateMessage>(OnState);
        network.Messages.Register<ProtectionDeniedMessage>(OnDenied);
    }

    public static bool CanPeerEdit(int peer)
    {
        var network = NetworkManager.Instance;
        if (!network.IsActive || peer == network.LocalPeerId) return !GameSession.IsDedicated;
        return ServerAdmins.IsAdmin(PlayerRegistry.GetName(peer));
    }

    public static bool BlocksPeer(int peer, SpaceEntity entity, Vector3i cell) => !ProtectionRules.CanEdit(PlayerRegistry.GetName(peer), CanPeerEdit(peer), entity, cell);

    public static bool BlocksPeerBox(int peer, SpaceEntity entity, Vector3i min, Vector3i max) => !ProtectionRules.CanEditBox(PlayerRegistry.GetName(peer), CanPeerEdit(peer), entity, min, max);

    public static bool BlocksPeerKey(int peer, Messages.ContainerKey key)
    {
        if (key.IsObject) return false;
        var world = World.Instance;
        return world != null && world.TryFindEntity(key.EntityId, out var entity) && BlocksPeer(peer, entity, key.Cell);
    }

    public static bool BlocksPeerUse(int peer, SpaceEntity entity) => !ProtectionRules.CanUseEntity(PlayerRegistry.GetName(peer), CanPeerEdit(peer), entity);

    public static void Deny(int peer)
    {
        var network = NetworkManager.Instance;
        if (peer == network.LocalPeerId) EntityProtection.NotifyDenied();
        else network.Send(peer, new ProtectionDeniedMessage());
    }

    public static void SendTo(int peer)
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer || peer == network.LocalPeerId) return;

        var ids = new long[EntityProtection.Ids.Count];
        int i = 0;
        foreach (var id in EntityProtection.Ids) ids[i++] = id;
        network.Send(peer, new ProtectionStateMessage { EntityIds = ids, CanEdit = CanPeerEdit(peer) });
    }

    public static void BroadcastState()
    {
        var network = NetworkManager.Instance;
        if (!network.IsServer) return;
        foreach (var player in PlayerRegistry.All) SendTo(player.Peer);
    }

    private static void OnState(int sender, ProtectionStateMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;

        EntityProtection.ReplaceAll(message.EntityIds);
        EntityProtection.LocalCanEdit = message.CanEdit;
    }

    private static void OnDenied(int sender, ProtectionDeniedMessage message)
    {
        if (NetworkManager.Instance.IsServer) return;
        EntityProtection.NotifyDenied();
    }

    public static bool TryFindNear(Vector3 position, float maxDistance, out SpaceEntity entity, out float distance)
    {
        entity = null;
        distance = float.MaxValue;
        var sector = World.CurrentSector;
        if (sector == null || !sector.TryGetNearestEntity(position, out var nearest)) return false;

        var box = nearest.GeometryBoundingBox;
        distance = AsteroidApproach.SurfaceDistance(new AsteroidTarget(nearest.ToWorldSpace(box.Center), box.Size.Length * 0.5f), position);
        if (distance > maxDistance) return false;

        entity = nearest;
        return true;
    }
}

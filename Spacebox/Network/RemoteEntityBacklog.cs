using Engine;
using Engine.Networking;
using Spacebox.Game.Generation;

namespace Spacebox.Network;

public static class RemoteEntityBacklog
{
    private const float Timeout = 30f;
    private const int MaxMessages = 4096;
    private const int LocalPeer = 0;

    private static readonly PendingEntityEdits<Action> Pending = new PendingEntityEdits<Action>(Timeout, MaxMessages);

    public static bool TryDefer(long entityId, Action apply)
    {
        if (NetworkManager.Instance.IsServer) return false;

        var world = World.Instance;
        if (world == null) return false;

        bool waiting = Pending.Has(entityId) || (!world.TryFindEntity(entityId, out _) && world.IsEntityLoading(entityId));
        return waiting && Pending.TryAdd(LocalPeer, entityId, apply);
    }

    public static void Update()
    {
        Pending.Update(Time.Delta, IsLoaded, (_, apply) => apply(), (_, _) => { });
    }

    public static void Clear()
    {
        Pending.Clear();
    }

    private static bool IsLoaded(long entityId)
    {
        var world = World.Instance;
        return world != null && (world.TryFindEntity(entityId, out _) || !world.IsEntityLoading(entityId));
    }
}

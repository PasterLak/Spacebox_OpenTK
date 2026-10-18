using Engine.Networking;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Network.Messages;

namespace Spacebox.Network.Containers;

public static class ContainerResolver
{
    public static ContainerKey KeyFor(Chunk chunk, Vector3Byte index)
    {
        var origin = chunk.PositionIndex * Chunk.Size;
        return new ContainerKey(chunk.SpaceEntity.EntityID, new Vector3i(origin.X + index.X, origin.Y + index.Y, origin.Z + index.Z));
    }

    public static bool TryResolve(ContainerKey key, out IContainer container)
    {
        container = null;
        if (key.IsObject) return TryResolveObject(key, out container);
        if (!TryResolveBlock(key, out var block, out var chunk) || block is not IContainerBlock found) return false;

        if (found is InteractiveBlock interactive) interactive.chunk = chunk;
        container = found;
        return true;
    }

    public static bool TryResolveBlock(ContainerKey key, out Block block, out Chunk chunk)
    {
        block = null;
        chunk = null;
        if (key.IsObject) return false;

        var world = World.Instance;
        if (world == null || !world.TryFindEntity(key.EntityId, out var entity)) return false;
        if (!entity.TryGetBlockLocal(key.Cell, out chunk, out var index)) return false;

        block = chunk.Blocks[index.X, index.Y, index.Z];
        return block != null;
    }

    private static bool TryResolveObject(ContainerKey key, out IContainer container)
    {
        container = null;
        if (key.EntityId < 0 || key.EntityId > uint.MaxValue) return false;
        if (!NetworkManager.Instance.Spawner.TryGet((uint)key.EntityId, out var networkObject) || networkObject.Owner is not IContainer found) return false;

        container = found;
        return true;
    }
}

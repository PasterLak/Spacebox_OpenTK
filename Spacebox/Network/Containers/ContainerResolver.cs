using OpenTK.Mathematics;
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

    public static bool TryResolve(ContainerKey key, out IContainerBlock container)
    {
        container = null;

        var world = World.Instance;
        if (world == null || !world.TryFindEntity(key.EntityId, out var entity)) return false;
        if (!entity.TryGetBlockLocal(key.Cell, out var chunk, out var index)) return false;
        if (chunk.Blocks[index.X, index.Y, index.Z] is not IContainerBlock found) return false;

        if (found is InteractiveBlock interactive) interactive.chunk = chunk;
        container = found;
        return true;
    }
}

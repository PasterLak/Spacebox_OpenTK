using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Blocks;

public interface IEntityBlockListener
{
    void OnAddedToEntity(Chunk chunk, Vector3i localCell);
    void OnRemovedFromEntity();
}

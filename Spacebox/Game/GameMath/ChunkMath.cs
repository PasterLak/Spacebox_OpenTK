
using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;

namespace Spacebox.Game.GameMath;

public class ChunkMath
{
    public Vector3SByte GetChunkIndex(Vector3 worldPosition, Vector3 entityWorldPosition)
    {
        Vector3 relativePosition = worldPosition - entityWorldPosition;

        int indexX = (int)MathF.Floor(relativePosition.X / Chunk.Size);
        int indexY = (int)MathF.Floor(relativePosition.Y / Chunk.Size);
        int indexZ = (int)MathF.Floor(relativePosition.Z / Chunk.Size);

        return new Vector3SByte((sbyte)indexX, (sbyte)indexY, (sbyte)indexZ);
    }
}

using OpenTK.Mathematics;

namespace Engine.AI;

public interface INavigationGrid
{
    bool IsPassable(Vector3i cell);
}

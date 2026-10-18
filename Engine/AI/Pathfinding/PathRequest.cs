using OpenTK.Mathematics;

namespace Engine.AI;

public sealed class PathRequest
{
    public INavigationGrid? Grid { get; internal set; }
    public Vector3 Start { get; internal set; }
    public Vector3 Goal { get; internal set; }
    public int MaxNodes { get; set; } = VoxelPathfinder.DefaultMaxNodes;
    public VoxelPath Path { get; } = new VoxelPath();
    public PathStatus Status { get; internal set; }
    public bool IsPending { get; internal set; }
    public bool IsCompleted => !IsPending && Status != PathStatus.None;
    public int Version { get; internal set; }
}

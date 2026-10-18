using Engine.AI;
using OpenTK.Mathematics;

namespace Spacebox.Tests.Engine;

public class VoxelPathfinderTests
{
    private sealed class SetGrid : INavigationGrid
    {
        public readonly HashSet<Vector3i> Solid = new HashSet<Vector3i>();
        public bool IsPassable(Vector3i cell) => !Solid.Contains(cell);
    }

    [Fact]
    public void OpenSpace_ReturnsDirectPath()
    {
        var pathfinder = new VoxelPathfinder();
        var path = new VoxelPath();

        var status = pathfinder.FindPath(new SetGrid(), new Vector3(0.5f), new Vector3(40.5f, 3.5f, -20.5f), path);

        Assert.Equal(PathStatus.Found, status);
        Assert.Equal(1, path.Count);
        Assert.Equal(0, pathfinder.LastExpandedNodes);
    }

    [Fact]
    public void Wall_PathGoesAroundAndAvoidsSolidCells()
    {
        var grid = new SetGrid();
        for (int y = -5; y <= 5; y++)
        for (int z = -5; z <= 5; z++)
            grid.Solid.Add(new Vector3i(5, y, z));

        var pathfinder = new VoxelPathfinder();
        var path = new VoxelPath();
        var start = new Vector3(0.5f);
        var goal = new Vector3(10.5f, 0.5f, 0.5f);

        var status = pathfinder.FindPath(grid, start, goal, path);

        Assert.Equal(PathStatus.Found, status);
        Assert.Equal(goal, path.Destination);

        var previous = start;
        for (int i = 0; i < path.Count; i++)
        {
            Assert.True(VoxelLine.IsClear(grid, previous, path[i]));
            previous = path[i];
        }
    }

    [Fact]
    public void EnclosedGoal_ReturnsPartialOrFailed()
    {
        var grid = new SetGrid();
        var goalCell = new Vector3i(6, 0, 0);
        for (int x = -1; x <= 1; x++)
        for (int y = -1; y <= 1; y++)
        for (int z = -1; z <= 1; z++)
            if (x != 0 || y != 0 || z != 0) grid.Solid.Add(goalCell + new Vector3i(x, y, z));

        var pathfinder = new VoxelPathfinder();
        var path = new VoxelPath();

        var status = pathfinder.FindPath(grid, new Vector3(0.5f), VoxelLine.CellCenter(goalCell), path, 500);

        Assert.NotEqual(PathStatus.Found, status);
        Assert.True(pathfinder.LastExpandedNodes <= 500);
    }

    [Fact]
    public void DiagonalMove_DoesNotCutCorners()
    {
        var grid = new SetGrid();
        grid.Solid.Add(new Vector3i(1, 0, 0));
        grid.Solid.Add(new Vector3i(0, 1, 0));

        var pathfinder = new VoxelPathfinder();
        var path = new VoxelPath();

        pathfinder.FindPath(grid, new Vector3(0.5f), new Vector3(1.5f, 1.5f, 0.5f), path);

        Assert.True(path.Count > 1);
    }

    [Fact]
    public void Scheduler_RespectsNodeBudgetAcrossUpdates()
    {
        var grid = new SetGrid();
        for (int y = -20; y <= 20; y++)
        for (int z = -20; z <= 20; z++)
            grid.Solid.Add(new Vector3i(5, y, z));

        var scheduler = new PathScheduler { NodeBudgetPerUpdate = 1 };
        var first = new PathRequest();
        var second = new PathRequest();

        scheduler.Request(first, grid, new Vector3(0.5f), new Vector3(10.5f, 0.5f, 0.5f));
        scheduler.Request(second, grid, new Vector3(0.5f), new Vector3(10.5f, 0.5f, 0.5f));
        scheduler.Update();

        Assert.True(first.IsCompleted);
        Assert.True(second.IsPending);

        scheduler.Update();
        Assert.True(second.IsCompleted);
    }
}

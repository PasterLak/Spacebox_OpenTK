using Engine.GUI;
using OpenTK.Mathematics;

namespace Engine.Tests;

public class AxesGizmoTests
{
    [Fact]
    public void Project_LookingDownMinusZ_XGoesRightAndYGoesUpOnScreen()
    {
        var x = AxesGizmo.Project(Vector3.UnitX, Vector3.UnitX, Vector3.UnitY);
        var y = AxesGizmo.Project(Vector3.UnitY, Vector3.UnitX, Vector3.UnitY);
        var z = AxesGizmo.Project(Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY);

        Assert.Equal(new Vector2(1f, 0f), x);
        Assert.Equal(new Vector2(0f, -1f), y);
        Assert.Equal(Vector2.Zero, z);
    }

    [Fact]
    public void Sort_DrawsAxesPointingAwayFirst()
    {
        var order = new int[3];
        var front = new Vector3(0.2f, -0.5f, 0.8f).Normalized();

        AxesGizmo.SortBackToFront(front, order);

        Assert.Equal(new[] { 2, 0, 1 }, order);
    }
}

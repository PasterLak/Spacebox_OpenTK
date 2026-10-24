using Engine.GUI;
using OpenTK.Mathematics;

namespace Engine.Tests;

public class GizmoIconsTests
{
    [Fact]
    public void Size_ShrinksWithDistance()
    {
        Assert.Equal(GizmoIcons.NearSize, GizmoIcons.SizeAt(0f), 3);
        Assert.Equal(GizmoIcons.FarSize, GizmoIcons.SizeAt(GizmoIcons.MaxDistance), 3);
        Assert.Equal(GizmoIcons.FarSize, GizmoIcons.SizeAt(GizmoIcons.MaxDistance * 3f), 3);
        Assert.True(GizmoIcons.SizeAt(10f) > GizmoIcons.SizeAt(100f));
    }

    [Fact]
    public void Add_IsIgnoredWhileVisualDebugIsOff()
    {
        bool before = VisualDebug.Enabled;
        VisualDebug.Enabled = false;
        GizmoIcons.Clear();

        GizmoIcons.Add(Vector3.Zero, GizmoIcon.CenterOfMass, Color4.Lime);

        Assert.Equal(0, GizmoIcons.Count);
        VisualDebug.Enabled = before;
    }
}

using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.Tests.CanvasUI;

public sealed class CanvasShapesTests
{
    private static bool Covers(CanvasCommandList list, Vector2 point)
    {
        foreach (var command in list.Commands)
        {
            var r = command.Rect;
            if (point.X >= r.X && point.X < r.X + r.Width && point.Y >= r.Y && point.Y < r.Y + r.Height) return true;
        }
        return false;
    }

    [Fact]
    public void HorizontalLine_IsOneRectCenteredOnTheLine()
    {
        var list = new CanvasCommandList();
        CanvasShapes.Line(list, new Vector2(10f, 20f), new Vector2(50f, 20f), Color4.White, 4f);

        var command = Assert.Single(list.Commands);
        Assert.Equal(10f, command.Rect.X, 3);
        Assert.Equal(18f, command.Rect.Y, 3);
        Assert.Equal(40f, command.Rect.Width, 3);
        Assert.Equal(4f, command.Rect.Height, 3);
    }

    [Fact]
    public void Line_RestoresTheTransform()
    {
        var list = new CanvasCommandList();
        var transform = Transform2D.Translation(new Vector2(5f, 5f));
        list.Transform = transform;
        CanvasShapes.Line(list, Vector2.Zero, new Vector2(10f, 10f), Color4.White, 2f);
        Assert.Equal(transform.Origin, list.Transform.Origin);
        Assert.Equal(transform.X, list.Transform.X);
    }

    [Fact]
    public void ZeroLengthOrTransparentLines_DrawNothing()
    {
        var list = new CanvasCommandList();
        CanvasShapes.Line(list, Vector2.One, Vector2.One, Color4.White, 2f);
        CanvasShapes.Line(list, Vector2.Zero, Vector2.One, new Color4(1f, 1f, 1f, 0f), 2f);
        Assert.Empty(list.Commands);
    }

    [Fact]
    public void FilledCircle_CoversTheCenterButNotTheBoxCorners()
    {
        var list = new CanvasCommandList();
        var center = new Vector2(50f, 50f);
        CanvasShapes.FilledCircle(list, center, 20f, Color4.White);

        Assert.True(Covers(list, center));
        Assert.True(Covers(list, center + new Vector2(18f, 0f)));
        Assert.False(Covers(list, center + new Vector2(18f, 18f)));
        Assert.False(Covers(list, center + new Vector2(-19f, -19f)));
        Assert.Equal(CanvasShapes.CircleRows(20f), list.Commands.Count);
    }

    [Fact]
    public void CircleOutline_DrawsOneSegmentPerStep()
    {
        var list = new CanvasCommandList();
        CanvasShapes.Circle(list, Vector2.Zero, 10f, Color4.White, 1f, 12);
        Assert.Equal(12, list.Commands.Count);
    }
}

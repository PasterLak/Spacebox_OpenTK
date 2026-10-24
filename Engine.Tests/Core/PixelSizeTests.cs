using OpenTK.Mathematics;

namespace Engine.Tests.Core;

public class PixelSizeTests
{
    [Fact]
    public void Resolve_UsesFramebufferWhenKnown()
    {
        Assert.Equal(new Vector2i(2560, 1600), PixelSize.Resolve(new Vector2i(2560, 1600), new Vector2i(1280, 800)));
        Assert.Equal(new Vector2i(1280, 800), PixelSize.Resolve(Vector2i.Zero, new Vector2i(1280, 800)));
    }

    [Fact]
    public void Scale_IsFramebufferOverWindow()
    {
        Assert.Equal(2f, PixelSize.Scale(new Vector2i(2560, 1600), new Vector2i(1280, 800)));
        Assert.Equal(1f, PixelSize.Scale(new Vector2i(1280, 800), new Vector2i(1280, 800)));
        Assert.Equal(1f, PixelSize.Scale(new Vector2i(100, 100), Vector2i.Zero));
    }
}

using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.Tests.CanvasUI;

public class CanvasStretchTests
{
    private static StretchSettings Settings() => new StretchSettings { VirtualSize = new Vector2(1280f, 720f) };

    [Theory]
    [InlineData(1280, 720, 1f)]
    [InlineData(1920, 1080, 1.5f)]
    [InlineData(3840, 2160, 3f)]
    [InlineData(1024, 768, 0.8f)]
    [InlineData(5120, 1440, 2f)]
    [InlineData(1080, 1920, 0.84375f)]
    public void Expand_ScalesByTheTighterAxisAndCoversTheVirtualSize(int width, int height, float scale)
    {
        var result = CanvasStretch.Compute(Settings(), new Vector2(width, height));

        Assert.Equal(scale, result.Scale, 4);
        Assert.True(result.ViewportSize.X >= 1280f - 0.01f);
        Assert.True(result.ViewportSize.Y >= 720f - 0.01f);
        Assert.Equal(width, result.ViewportSize.X * result.Scale, 2);
    }

    [Fact]
    public void UiScale_IsCappedByTheMinimumViewport()
    {
        var settings = Settings();
        settings.UiScale = 2f;
        settings.MinimumViewportSize = new Vector2(1024f, 576f);

        var result = CanvasStretch.Compute(settings, new Vector2(1280f, 720f));

        Assert.Equal(1.25f, result.Scale, 4);
        Assert.Equal(new Vector2(1024f, 576f), result.ViewportSize);
    }

    [Fact]
    public void MinScale_KeepsSmallScreensReadableWhileTheMinimumViewportFits()
    {
        var settings = Settings();
        settings.MinScale = 0.75f;
        settings.MinimumViewportSize = new Vector2(1024f, 576f);

        var result = CanvasStretch.Compute(settings, new Vector2(800f, 600f));

        Assert.Equal(0.75f, result.Scale, 4);
        Assert.True(result.ViewportSize.X >= 1024f);
    }

    [Fact]
    public void ScaleStep_SnapsDownToPixelFriendlySteps()
    {
        var settings = Settings();
        settings.ScaleStep = 0.5f;

        Assert.Equal(1.5f, CanvasStretch.Compute(settings, new Vector2(2200f, 1300f)).Scale, 4);
        Assert.Equal(0.4f, CanvasStretch.Compute(settings, new Vector2(512f, 288f)).Scale, 4);
    }

    [Fact]
    public void SafeMargins_AreConvertedToCanvasUnits()
    {
        var settings = Settings();
        settings.SafeMargins = new Thickness(96f, 54f);

        var result = CanvasStretch.Compute(settings, new Vector2(1920f, 1080f));

        Assert.Equal(new Rect2(64f, 36f, 1152f, 648f), result.SafeRect);
    }

    [Fact]
    public void MaxSafeAspect_CentresTheSafeAreaOnUltrawideScreens()
    {
        var settings = Settings();
        settings.MaxSafeAspect = 16f / 9f;

        var result = CanvasStretch.Compute(settings, new Vector2(5120f, 1440f));

        Assert.Equal(new Rect2(640f, 0f, 1280f, 720f), result.SafeRect);
    }

    [Fact]
    public void KeepAspect_LetterboxesAndSafeAreaStaysInsideTheVirtualArea()
    {
        var settings = Settings();
        settings.Mode = StretchMode.KeepAspect;
        settings.SafeMargins = new Thickness(0f, 200f, 0f, 0f);

        var result = CanvasStretch.Compute(settings, new Vector2(1280f, 1000f));

        Assert.Equal(new Vector2(1280f, 720f), result.ViewportSize);
        Assert.Equal(new Vector2(0f, 140f), result.ScreenTransform.Origin);
        Assert.Equal(new Rect2(0f, 60f, 1280f, 660f), result.SafeRect);
    }

    [Fact]
    public void Root_RelayoutsWhenStretchSettingsChange()
    {
        var root = new CanvasRoot();
        var panel = root.Add(new Panel());
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        root.Resize(new Vector2(1920f, 1080f));
        Assert.Equal(1280f, panel.Rect.Width);

        root.Stretch.UiScale = 1.5f;
        root.Resize(new Vector2(1920f, 1080f));

        Assert.Equal(1920f / 2.25f, panel.Rect.Width, 0);
    }
}

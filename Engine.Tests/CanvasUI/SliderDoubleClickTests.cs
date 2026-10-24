using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.Tests.CanvasUI;

public class SliderDoubleClickTests
{
    private static CanvasRoot CreateRoot(HSlider slider)
    {
        var root = new CanvasRoot { StretchMode = StretchMode.Disabled, Theme = new Theme { DefaultFont = new MonospaceFont(10f, 6f, 10f), DefaultFontSize = 10f } };
        root.Resize(new Vector2(800f, 600f));
        root.Add(slider);
        slider.SetOffsets(0, 0, 200, 20);
        root.Layout();
        return root;
    }

    private static void Frame(CanvasRoot root, Vector2 pointer, bool down = false)
    {
        root.Update(0.016f, new PointerState { Position = pointer, LeftDown = down }, new KeyboardFrame(Array.Empty<KeyPress>(), ""));
    }

    private static void Click(CanvasRoot root, Vector2 point)
    {
        Frame(root, point);
        Frame(root, point, true);
        Frame(root, point);
    }

    [Fact]
    public void DoubleClick_RaisesEventAndKeepsHandlerValue()
    {
        var slider = new HSlider { MinValue = 0, MaxValue = 100, Step = 1, Value = 50 };
        int raised = 0;
        slider.DoubleClicked += () =>
        {
            raised++;
            slider.Value = 50;
        };
        var root = CreateRoot(slider);

        Click(root, new Vector2(180f, 10f));
        Assert.Equal(90, slider.Value);
        Click(root, new Vector2(180f, 10f));

        Assert.Equal(1, raised);
        Assert.Equal(50, slider.Value);
    }

    [Fact]
    public void DoubleClick_WithoutHandler_MovesToPointer()
    {
        var slider = new HSlider { MinValue = 0, MaxValue = 100, Step = 1, Value = 50 };
        var root = CreateRoot(slider);

        Click(root, new Vector2(20f, 10f));
        Click(root, new Vector2(20f, 10f));

        Assert.Equal(10, slider.Value);
    }
}

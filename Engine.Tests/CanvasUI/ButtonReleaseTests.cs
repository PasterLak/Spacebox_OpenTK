using Engine.CanvasUI;
using OpenTK.Mathematics;

namespace Engine.Tests.CanvasUI;

public class ButtonReleaseTests
{
    private static CanvasRoot CreateRoot(Button button)
    {
        var root = new CanvasRoot { StretchMode = StretchMode.Disabled, Theme = new Theme { DefaultFont = new MonospaceFont(10f, 6f, 10f), DefaultFontSize = 10f } };
        root.Resize(new Vector2(800f, 600f));
        root.Add(button);
        button.SetOffsets(0, 0, 100, 30);
        root.Layout();
        return root;
    }

    private static void Frame(CanvasRoot root, Vector2 pointer, bool down = false)
    {
        root.Update(0.016f, new PointerState { Position = pointer, LeftDown = down }, new KeyboardFrame(Array.Empty<KeyPress>(), ""));
    }

    [Fact]
    public void Release_OnVisibleButton_Presses()
    {
        var button = new Button { Text = "Ok" };
        int pressed = 0;
        button.Pressed += _ => pressed++;
        var root = CreateRoot(button);
        var point = new Vector2(50f, 15f);

        Frame(root, point);
        Frame(root, point, true);
        Frame(root, point);

        Assert.Equal(1, pressed);
    }

    [Fact]
    public void Release_AfterButtonWasHidden_DoesNotPress()
    {
        var button = new Button { Text = "Ok" };
        int pressed = 0;
        button.Pressed += _ => pressed++;
        var root = CreateRoot(button);
        var point = new Vector2(50f, 15f);

        Frame(root, point);
        Frame(root, point, true);
        button.Visible = false;
        Frame(root, point);

        Assert.Equal(0, pressed);
    }
}

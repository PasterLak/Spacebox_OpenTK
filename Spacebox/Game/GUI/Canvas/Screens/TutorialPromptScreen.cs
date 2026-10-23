using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.MenuUi;
using Spacebox.Game.Tutorial.UI;

namespace Spacebox.Game.GUI.Canvas.Screens;

public sealed class TutorialPromptScreen : GameScreen
{
    private const float Width = 340f;

    public TutorialPromptScreen()
    {
        Name = "TutorialPrompt";
        var center = AddChild(new CenterContainer { MouseFilter = MouseFilter.Ignore });
        center.SetAnchorsPreset(LayoutPreset.FullRect);

        var frame = MenuLayout.Window(TutorialPromptUI.Title, Width);
        frame.Root.Name = "TutorialPromptWindow";
        center.AddChild(frame.Root);
        frame.Body.AddChild(new Label { Text = TutorialPromptUI.Text, Autowrap = true, CustomMinimumSize = new Vector2(Width - 40f, 0f) });
        frame.Footer.AddChild(MenuLayout.Button(null, "Start", TutorialPromptUI.Start));
        frame.Footer.AddChild(MenuLayout.Button(null, "No, thanks", TutorialPromptUI.Decline));
    }

    public override bool IsShown => TutorialPromptUI.IsOpen;

    protected override bool BlocksPointer => true;
}

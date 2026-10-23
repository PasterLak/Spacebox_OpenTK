using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.MenuUi;
using Spacebox.Game.Tutorial.UI;

namespace Spacebox.Game.GUI.Canvas.Screens;

public sealed class TutorialPromptScreen : GameScreen
{
    private const float Width = 340f;

    private readonly TutorialPromptUI _prompt;

    public TutorialPromptScreen(TutorialPromptUI prompt)
    {
        _prompt = prompt;
        Name = "TutorialPrompt";
        var center = AddChild(new CenterContainer { MouseFilter = MouseFilter.Ignore });
        center.SetAnchorsPreset(LayoutPreset.FullRect);

        var frame = MenuLayout.Window(TutorialPromptUI.Title, Width);
        frame.Root.Name = "TutorialPromptWindow";
        center.AddChild(frame.Root);
        frame.Body.AddChild(new Label { Text = TutorialPromptUI.Text, Autowrap = true, CustomMinimumSize = new Vector2(Width - 40f, 0f) });
        frame.Footer.AddChild(MenuLayout.Button(null, "Start", _prompt.Start));
        frame.Footer.AddChild(MenuLayout.Button(null, "No, thanks", _prompt.Decline));
    }

    public override bool IsShown => _prompt.IsOpen;

    protected override bool BlocksPointer => true;
}

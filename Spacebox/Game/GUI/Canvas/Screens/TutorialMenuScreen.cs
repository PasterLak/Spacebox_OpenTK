using Engine.CanvasUI;
using OpenTK.Mathematics;
using Spacebox.Game.GUI.MenuUi;
using Spacebox.Game.Tutorial.UI;

namespace Spacebox.Game.GUI.Canvas.Screens;

public sealed class TutorialMenuScreen : GameScreen
{
    private const float Width = 300f;

    private readonly Label _description = new Label { Autowrap = true, CustomMinimumSize = new Vector2(Width - 40f, 0f) };
    private readonly VBoxContainer _buttons = new VBoxContainer();
    private readonly List<string> _shownLabels = new List<string>();

    private readonly TutorialMenuUI _menu;
    private readonly PauseUI _pause;

    public TutorialMenuScreen(TutorialMenuUI menu, PauseUI pause)
    {
        _menu = menu;
        _pause = pause;
        Name = "TutorialMenu";
        var center = AddChild(new CenterContainer { MouseFilter = MouseFilter.Ignore });
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        var frame = MenuLayout.Window("TUTORIAL", Width);
        frame.Root.Name = "TutorialMenuWindow";
        frame.Footer.Visible = false;
        center.AddChild(frame.Root);
        frame.Body.AddChild(_description);
        frame.Body.AddChild(_buttons);
        _buttons.AddThemeConstantOverride("separation", 8f);
    }

    public override bool IsShown => _menu.IsOpen && _pause.IsVisible && !_pause.IsOptionsOpen;

    protected override bool BlocksPointer => true;

    protected override void Refresh(float deltaTime)
    {
        _menu.Refresh();
        SetText(_description, _menu.Description);
        var buttons = _menu.Buttons;
        if (SameLabels(buttons)) return;

        _shownLabels.Clear();
        _buttons.ClearChildren();
        for (int i = 0; i < buttons.Count; i++)
        {
            int index = i;
            _shownLabels.Add(buttons[i].Label);
            var button = _buttons.AddChild(new Button { Text = buttons[i].Label, CustomMinimumSize = new Vector2(0f, MenuLayout.ButtonHeight), SizeFlagsHorizontal = SizeFlags.ExpandFill });
            button.Pressed += _ => _menu.Press(index);
        }
    }

    private bool SameLabels(IReadOnlyList<(string Label, Action Click)> buttons)
    {
        if (buttons.Count != _shownLabels.Count) return false;
        for (int i = 0; i < buttons.Count; i++)
            if (buttons[i].Label != _shownLabels[i]) return false;
        return true;
    }
}

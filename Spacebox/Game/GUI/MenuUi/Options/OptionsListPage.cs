using Spacebox.Game.GUI.Menu;

namespace Spacebox.Game.GUI.MenuUi;

public sealed class OptionsListPage : MenuPage
{
    private const float Width = 260f;

    private readonly IOptionsNavigator _options;

    public OptionsListPage(IOptionsNavigator options)
    {
        _options = options;
    }

    protected override void Build()
    {
        var frame = MenuLayout.Window("Options", Width);
        AddChild(frame.Root);
        Add(frame, "Audio", "Master, music, ambient, effects and interface volume", _options.Show<AudioWindow>);
        Add(frame, "Game", "Draw distance, mouse and camera settings", _options.Show<GameWindow>);
        Add(frame, "Graphics", "Display mode, frame rate, resolution and visual quality", _options.Show<GraphicsWindow>);
        Add(frame, "Interface", "Interface design and scale", _options.Show<InterfaceWindow>);
        Add(frame, "Controls", "Change key bindings", _options.Show<ControlsWindow>);
        frame.Footer.AddChild(Button("Back", "Return to the previous menu", _options.Close));
    }

    private void Add(MenuWindowFrame frame, string text, string tooltip, Action open) => frame.Body.AddChild(Button(text, tooltip, open));

    private Engine.CanvasUI.Button Button(string text, string tooltip, Action onClick)
    {
        var button = MenuLayout.Button(null, text, () =>
        {
            _options.PlayClick();
            onClick();
        });
        button.TooltipText = tooltip;
        return button;
    }
}

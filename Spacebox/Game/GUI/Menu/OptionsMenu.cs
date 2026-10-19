using Engine.Audio;
using Engine.InputPro;

namespace Spacebox.Game.GUI.Menu;

public interface IOptionsNavigator
{
    void PlayClick();
    void Show<T>() where T : MenuWindow;
    void ShowOptions();
    void Close();
}

public sealed class OptionsMenu : IOptionsNavigator
{
    public const string InputConfigPath = "Resources/default_input.json";

    private readonly Dictionary<Type, MenuWindow> _windows = new Dictionary<Type, MenuWindow>();
    private readonly AudioSource _click;
    private readonly Action _onClose;
    private MenuWindow _current;

    public OptionsMenu(AudioSource click, Action onClose)
    {
        _click = click;
        _onClose = onClose;

        Register(new OptionsWindow(this));
        Register(new AudioWindow(this));
        Register(new GameWindow(this));
        Register(new GraphicsWindow(this));
        Register(new ControlsWindow(this));
        _current = _windows[typeof(OptionsWindow)];
    }

    public void Render()
    {
        if (_current is ControlsWindow controls) controls.Update();
        _current?.Render();
    }

    public void PlayClick()
    {
        _click?.Play();
    }

    public void Show<T>() where T : MenuWindow
    {
        if (!_windows.TryGetValue(typeof(T), out var window)) return;
        _current = window;
        window.OnWindowChanged();
    }

    public void ShowOptions()
    {
        Show<OptionsWindow>();
    }

    public void Close()
    {
        ShowOptions();
        _onClose?.Invoke();
    }

    public static void DiscardUnsavedChanges()
    {
        Settings.LoadSettings();
        SettingsApplier.ApplyAll();
        InputManager.Instance.LoadConfiguration(InputConfigPath);
    }

    private void Register(MenuWindow window)
    {
        _windows[window.GetType()] = window;
    }
}

public sealed class OptionsHostWindow : MenuWindow
{
    public OptionsMenu Options { get; }

    public OptionsHostWindow(GameMenu menu)
    {
        Options = new OptionsMenu(menu.Click1, menu.SetStateToMain);
    }

    public override void Render()
    {
        Options.Render();
    }
}

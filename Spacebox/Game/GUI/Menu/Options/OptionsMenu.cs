using Engine.Audio;
using Engine.InputPro;

namespace Spacebox.Game.GUI.Menu;

public interface IOptionsNavigator
{
    void PlayClick();
    void Show<T>() where T : MenuWindow;
    void ShowOptions();
    void Save();
    void Back();
    void Close();
}

public sealed class OptionsMenu : IOptionsNavigator
{
    public const string InputConfigPath = "Resources/default_input.json";

    private readonly Dictionary<Type, MenuWindow> _windows = new Dictionary<Type, MenuWindow>();
    private readonly AudioSource _click;
    private readonly Action _onClose;
    private readonly UnsavedChangesPrompt _unsavedPrompt = new UnsavedChangesPrompt();
    private MenuWindow _current;
    private string _savedState;

    public OptionsMenu(AudioSource click, Action onClose)
    {
        _click = click;
        _onClose = onClose;

        Register(new OptionsWindow(this));
        Register(new AudioWindow(this));
        Register(new GameWindow(this));
        Register(new GraphicsWindow(this));
        Register(new InterfaceWindow(this));
        Register(new ControlsWindow(this));
        _current = _windows[typeof(OptionsWindow)];
    }

    public void Render()
    {
        if (_current is ControlsWindow controls) controls.Update();
        _current?.Render();
        _unsavedPrompt.Render(Save, LeaveWithoutSaving, PlayClick);
    }

    public void PlayClick()
    {
        _click?.Play();
    }

    public void Show<T>() where T : MenuWindow
    {
        if (!_windows.TryGetValue(typeof(T), out var window)) return;
        _current = window;
        _savedState = window is IOptionsCategory category ? category.CaptureState() : null;
        window.OnWindowChanged();
    }

    public void ShowOptions()
    {
        Show<OptionsWindow>();
    }

    public void Save()
    {
        PlayClick();
        SaveAndLeave();
    }

    public void Back()
    {
        PlayClick();
        if (_current is IOptionsCategory category && category.CaptureState() != _savedState)
        {
            _unsavedPrompt.Open();
            return;
        }

        DiscardAndLeave();
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

    private void SaveAndLeave()
    {
        if (_current is IOptionsCategory category) category.Save();
        ShowOptions();
    }

    private void LeaveWithoutSaving()
    {
        PlayClick();
        DiscardAndLeave();
    }

    private void DiscardAndLeave()
    {
        if (_current is IOptionsCategory category) category.Discard();
        ShowOptions();
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

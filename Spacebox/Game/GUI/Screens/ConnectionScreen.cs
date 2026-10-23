using Spacebox.Network;

namespace Spacebox.Game.GUI;

public static class ConnectionScreen
{
    private static readonly List<string> _steps = new List<string>();
    private static string _title = string.Empty;
    private static string? _error;
    private static float? _progress;
    private static bool _subscribed;

    public static bool IsVisible { get; private set; }
    public static bool HasError => _error != null;
    public static string Title => _title;
    public static string Error => _error;
    public static float? Progress => _progress;
    public static IReadOnlyList<string> Steps => _steps;

    public static void Show(string title)
    {
        _steps.Clear();
        _title = title;
        _error = null;
        _progress = null;
        IsVisible = true;
        Subscribe();
    }

    public static void AddStep(string text)
    {
        _steps.Add(text);
        _progress = null;
    }

    public static void SetProgress(float progress)
    {
        _progress = Math.Clamp(progress, 0f, 1f);
    }

    public static void ShowError(string title, string message)
    {
        Unsubscribe();
        _title = title;
        _error = message;
        _progress = null;
        IsVisible = true;
    }

    public static void ShowMessage(string title, string message)
    {
        _steps.Clear();
        ShowError(title, message);
    }

    public static void Hide()
    {
        Unsubscribe();
        IsVisible = false;
        _steps.Clear();
        _error = null;
        _progress = null;
    }

    public static void Leave()
    {
        if (ClientConnector.IsConnecting) ClientConnector.Cancel();
        Hide();
    }

    private static void Subscribe()
    {
        if (_subscribed) return;

        ClientConnector.StepAdded += AddStep;
        ClientConnector.ProgressChanged += SetProgress;
        _subscribed = true;
    }

    private static void Unsubscribe()
    {
        if (!_subscribed) return;

        ClientConnector.StepAdded -= AddStep;
        ClientConnector.ProgressChanged -= SetProgress;
        _subscribed = false;
    }
}

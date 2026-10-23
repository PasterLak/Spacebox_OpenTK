using System.Numerics;
using ImGuiNET;
using Spacebox.Network;

namespace Spacebox.Game.GUI;

public static class ConnectionScreen
{
    private const int MaxVisibleSteps = 10;

    private static readonly uint TitleColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
    private static readonly uint StepColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.6f, 0.6f, 1f));
    private static readonly uint CurrentStepColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.85f, 0.1f, 1f));
    private static readonly uint ErrorColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.25f, 0.25f, 1f));
    private static readonly uint ShadowColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.8f));
    private static readonly uint BarBackgroundColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.12f, 0.85f));
    private static readonly uint BarFillColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.85f, 0.1f, 1f));
    private static readonly uint BarBorderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.85f, 0.1f, 0.6f));

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

    public static void OnGUI()
    {
        if (!IsVisible) return;

        var displaySize = ImGui.GetIO().DisplaySize;
        var drawList = ImGui.GetForegroundDrawList();
        var font = ImGui.GetFont();
        float lineHeight = ImGui.GetTextLineHeightWithSpacing();
        float titleSize = ImGui.GetFontSize() * 2f;
        float y = displaySize.Y * 0.3f;
        float centerX = displaySize.X * 0.5f;

        DrawCentered(drawList, font, titleSize, _title, centerX, y, TitleColor);
        y += titleSize + lineHeight;

        int first = Math.Max(0, _steps.Count - MaxVisibleSteps);
        for (int i = first; i < _steps.Count; i++)
        {
            bool isCurrent = i == _steps.Count - 1 && _error == null;
            DrawCentered(drawList, font, ImGui.GetFontSize(), _steps[i], centerX, y, isCurrent ? CurrentStepColor : StepColor);
            y += lineHeight;
        }

        if (_progress.HasValue)
        {
            y += lineHeight * 0.3f;
            var barSize = new Vector2(displaySize.Y * 0.5f, displaySize.Y * 0.012f);
            var barMin = new Vector2(centerX - barSize.X * 0.5f, y);
            drawList.AddRectFilled(barMin, barMin + barSize, BarBackgroundColor);
            drawList.AddRectFilled(barMin, barMin + new Vector2(barSize.X * _progress.Value, barSize.Y), BarFillColor);
            drawList.AddRect(barMin, barMin + barSize, BarBorderColor);
            y += barSize.Y + lineHeight;
        }

        if (_error != null)
        {
            y += lineHeight * 0.5f;
            DrawCentered(drawList, font, ImGui.GetFontSize() * 1.2f, _error, centerX, y, ErrorColor);
            y += lineHeight * 2f;
        }

        if (!AuthPromptWindow.IsVisible) DrawButton(displaySize, y + lineHeight);
    }

    public static void Leave()
    {
        if (ClientConnector.IsConnecting) ClientConnector.Cancel();
        Hide();
    }

    private static void DrawButton(Vector2 displaySize, float y)
    {
        string label = _error != null ? "Back to menu" : "Cancel";
        var buttonSize = new Vector2(Math.Max(displaySize.X * 0.12f, 160f), Math.Max(displaySize.Y * 0.04f, 30f));
        var padding = ImGui.GetStyle().WindowPadding;

        ImGui.SetNextWindowPos(new Vector2((displaySize.X - buttonSize.X) * 0.5f - padding.X, y - padding.Y));
        ImGui.SetNextWindowSize(buttonSize + padding * 2f);
        ImGui.Begin("ConnectionScreenButton", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoSavedSettings);

        if (ImGui.Button(label, buttonSize)) Leave();

        ImGui.End();
    }

    private static void DrawCentered(ImDrawListPtr drawList, ImFontPtr font, float size, string text, float centerX, float y, uint color)
    {
        var textSize = font.CalcTextSizeA(size, float.MaxValue, 0f, text);
        var position = new Vector2(centerX - textSize.X * 0.5f, y);
        drawList.AddText(font, size, position + Vector2.One, ShadowColor, text);
        drawList.AddText(font, size, position, color, text);
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

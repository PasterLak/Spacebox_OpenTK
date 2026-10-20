using System.Numerics;
using Engine;
using ImGuiNET;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;

public static class WorldLoadingScreen
{
    private const int StableFramesRequired = 5;
    private const float MinDuration = 0.3f;
    private const float MaxDuration = 60f;
    private const string Title = "Loading world";

    private static readonly uint TitleColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
    private static readonly uint StepColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.85f, 0.85f, 0.85f, 1f));
    private static readonly uint ShadowColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.8f));
    private static readonly uint BarBackgroundColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.12f, 0.85f));
    private static readonly uint BarFillColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.85f, 0.1f, 1f));
    private static readonly uint BarBorderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.85f, 0.1f, 0.6f));

    private static LocalAstronaut _player;
    private static float _elapsed;
    private static int _stableFrames;
    private static int _maxPending;
    private static float _progress;
    private static string _step = "Preparing";
    private static Action _onComplete;

    public static bool IsActive { get; private set; }

    public static void Begin(LocalAstronaut player, Action onComplete = null)
    {
        _player = player;
        _onComplete = onComplete;
        _elapsed = 0f;
        _stableFrames = 0;
        _maxPending = 1;
        _progress = 0f;
        _step = "Preparing";
        IsActive = true;
        StreamingBudget.IsLoading = true;
        if (_player != null) _player.IsFrozen = true;
    }

    public static void Update()
    {
        if (!IsActive) return;

        _elapsed += Time.Delta;

        var world = World.Instance;
        var camera = Camera.Main;
        var work = world != null && camera != null ? world.CollectStreamingWork(camera.PositionWorld) : new StreamingWork();
        work.Meshes += Chunk.PendingLodJobs;
        int pending = work.Total + MainThreadDispatcher.Instance.PendingCount;

        _step = GetStep(work);
        _maxPending = Math.Max(_maxPending, pending);
        _progress = MathF.Max(_progress, 1f - pending / (float)_maxPending);
        _stableFrames = pending == 0 ? _stableFrames + 1 : 0;

        bool isReady = _stableFrames >= StableFramesRequired && _elapsed >= MinDuration;
        if (isReady || _elapsed >= MaxDuration) Finish();
    }

    public static void OnGUI()
    {
        if (!IsActive) return;

        var displaySize = ImGui.GetIO().DisplaySize;
        var drawList = ImGui.GetForegroundDrawList();
        var center = displaySize * 0.5f;
        float lineHeight = ImGui.GetTextLineHeight();

        DrawCenteredText(drawList, Title, new Vector2(center.X, center.Y - lineHeight * 2.2f), TitleColor);

        var barSize = new Vector2(displaySize.Y * 0.5f, displaySize.Y * 0.012f);
        var barMin = new Vector2(center.X - barSize.X * 0.5f, center.Y);
        drawList.AddRectFilled(barMin, barMin + barSize, BarBackgroundColor);
        drawList.AddRectFilled(barMin, barMin + new Vector2(barSize.X * _progress, barSize.Y), BarFillColor);
        drawList.AddRect(barMin, barMin + barSize, BarBorderColor);

        DrawCenteredText(drawList, _step, new Vector2(center.X, barMin.Y + barSize.Y + lineHeight * 0.8f), StepColor);
    }

    public static void Reset()
    {
        IsActive = false;
        StreamingBudget.IsLoading = false;
        _player = null;
        _onComplete = null;
    }

    private static string GetStep(StreamingWork work)
    {
        if (work.Entities > 0) return $"Generating asteroids ({work.Entities})";
        if (work.Chunks > 0) return $"Generating chunks ({work.Chunks})";
        if (work.Meshes > 0) return $"Building meshes ({work.Meshes})";
        return "Finishing";
    }

    private static void DrawCenteredText(ImDrawListPtr drawList, string text, Vector2 center, uint color)
    {
        var position = new Vector2(center.X - ImGui.CalcTextSize(text).X * 0.5f, center.Y);
        drawList.AddText(position + Vector2.One, ShadowColor, text);
        drawList.AddText(position, color, text);
    }

    private static void Finish()
    {
        if (_elapsed >= MaxDuration) Debug.Warning("[WorldLoadingScreen] Loading timed out, showing the world anyway");
        else Debug.Log($"[WorldLoadingScreen] World ready in {_elapsed:0.0}s");

        if (_player != null) _player.IsFrozen = false;
        var onComplete = _onComplete;
        Reset();
        onComplete?.Invoke();
    }
}

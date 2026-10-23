using Engine;
using Spacebox.Game.Generation;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;

public sealed class WorldLoadingController : IGameUiController
{
    private const int StableFramesRequired = 5;
    private const float MinDuration = 0.3f;
    private const float MaxDuration = 60f;
    private const float MaxConditionWait = 15f;
    public const string Title = "Loading world";

    private LocalAstronaut _player;
    private float _elapsed;
    private int _stableFrames;
    private int _maxPending;
    private float _progress;
    private string _step = "Preparing";
    private Action _onComplete;
    private Func<bool> _waitFor;

    public bool IsActive { get; private set; }
    public float Progress => _progress;
    public string Step => _step;

    public void Begin(LocalAstronaut player, Action onComplete = null, Func<bool> waitFor = null)
    {
        _player = player;
        _onComplete = onComplete;
        _waitFor = waitFor;
        _elapsed = 0f;
        _stableFrames = 0;
        _maxPending = 1;
        _progress = 0f;
        _step = "Preparing";
        IsActive = true;
        StreamingBudget.IsLoading = true;
        if (_player != null) _player.IsFrozen = true;
    }

    public void Tick()
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

        bool isReady = _stableFrames >= StableFramesRequired && _elapsed >= MinDuration && IsConditionMet();
        if (isReady || _elapsed >= MaxDuration) Finish();
    }

    public void Reset()
    {
        IsActive = false;
        StreamingBudget.IsLoading = false;
        _player = null;
        _onComplete = null;
        _waitFor = null;
    }

    private bool IsConditionMet()
    {
        return _waitFor == null || _elapsed >= MaxConditionWait || _waitFor();
    }

    private static string GetStep(StreamingWork work)
    {
        if (work.Entities > 0) return $"Generating asteroids ({work.Entities})";
        if (work.Chunks > 0) return $"Generating chunks ({work.Chunks})";
        if (work.Meshes > 0) return $"Building meshes ({work.Meshes})";
        return "Finishing";
    }

    private void Finish()
    {
        if (_elapsed >= MaxDuration) Debug.Warning("[WorldLoadingScreen] Loading timed out, showing the world anyway");
        else Debug.Log($"[WorldLoadingScreen] World ready in {_elapsed:0.0}s");

        if (_player != null) _player.IsFrozen = false;
        var onComplete = _onComplete;
        Reset();
        onComplete?.Invoke();
    }
}

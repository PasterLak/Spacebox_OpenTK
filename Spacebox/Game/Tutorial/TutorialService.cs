using Engine;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Tutorial;

public static class TutorialService
{
    private static readonly TutorialRunner _runner = new TutorialRunner();
    private static LocalTutorialContext _context;
    private static LocalAstronaut _player;
    private static bool _subscribed;

    static TutorialService()
    {
        _runner.StepCompleted += (_, rewards, _) => GiveRewards(rewards);
        _runner.ChapterCompleted += (_, rewards) => GiveRewards(rewards);
    }

    public static TutorialRunner Runner => _runner;
    public static ITutorialTextResolver Text => _context;
    public static bool IsAttached => _player != null;
    public static bool NeedsJoinPrompt { get; private set; }
    public static bool IsCollapsed => _runner.IsLoaded && _runner.Progress.Collapsed;

    public static bool HasTutorialFor(GameMode mode)
    {
        return TutorialLibrary.FindFor(mode) != null;
    }

    public static void Attach(LocalAstronaut player, bool welcomeWillAsk)
    {
        Detach();
        if (player == null || Application.IsHeadless || Spacebox.Network.GameSession.IsDedicated) return;

        _player = player;
        _context = new LocalTutorialContext(player, GameSetLoader.ModInfo?.ModId ?? "");
        PlayerActivity.Happened += OnActivity;
        _subscribed = true;

        var progress = player.Tutorial;
        if (progress != null && !progress.Declined)
        {
            var definition = TutorialLibrary.Find(progress.TutorialId) ?? TutorialLibrary.FindFor(player.GameMode);
            if (definition != null) _runner.Load(definition, progress, _context);
        }

        NeedsJoinPrompt = progress == null && player.IsNewPlayer && !welcomeWillAsk && HasTutorialFor(player.GameMode);
    }

    public static void Detach()
    {
        if (_subscribed) PlayerActivity.Happened -= OnActivity;
        _subscribed = false;
        _runner.Unload();
        _player = null;
        _context = null;
        NeedsJoinPrompt = false;
    }

    public static void Begin()
    {
        if (_player == null) return;

        NeedsJoinPrompt = false;
        var definition = TutorialLibrary.FindFor(_player.GameMode);
        if (definition == null) return;

        var progress = TutorialProgress.StartOver(definition.Id, _player.Tutorial);
        _player.Tutorial = progress;
        _runner.Load(definition, progress, _context);
    }

    public static void Decline()
    {
        if (_player == null) return;

        NeedsJoinPrompt = false;
        var definition = TutorialLibrary.FindFor(_player.GameMode);
        _player.Tutorial ??= TutorialProgress.CreateDeclined(definition?.Id);
        _player.Tutorial.Enabled = false;
        _player.Tutorial.Declined = true;
        _runner.Unload();
    }

    public static void Stop()
    {
        _runner.Stop();
    }

    public static void Resume()
    {
        if (_player == null) return;
        if (!_runner.IsLoaded || _runner.Progress.Finished) Begin();
        else _runner.Resume(_context);
    }

    public static void Restart()
    {
        if (_player == null) return;
        if (!_runner.IsLoaded) Begin();
        else _runner.Restart(_context);
    }

    public static bool Skip()
    {
        return _context != null && _runner.Skip(_context);
    }

    public static void SetCollapsed(bool collapsed)
    {
        if (_runner.IsLoaded) _runner.Progress.Collapsed = collapsed;
    }

    public static void Update(float deltaTime)
    {
        if (_player == null || !_runner.IsRunning) return;
        if (!_player.IsAlive || _player.IsFrozen || WorldLoadingScreen.IsActive) return;

        _runner.Update(_context, deltaTime);
    }

    public static void SyncProgress()
    {
        _runner.SyncProgress();
    }

    private static void OnActivity(PlayerActivityEvent activity)
    {
        _runner.OnActivity(activity);
    }

    private static void GiveRewards(IReadOnlyList<TutorialReward> rewards)
    {
        if (_player == null) return;

        for (int i = 0; i < rewards.Count; i++)
        {
            if (!GameAssets.TryGetItemById(rewards[i].ItemId, out var item)) continue;

            int left = rewards[i].Count;
            while (left > 0)
            {
                byte count = (byte)Math.Min(left, Math.Max(1, (int)item.StackSize));
                if (!_player.Panel.TryAddItem(item, count, out var notAdded) && !_player.Inventory.TryAddItem(item, notAdded, out notAdded))
                    World.DropEffectManager?.DropItem(_player.Position, _player.Front, 1f, item, notAdded);
                left -= count;
            }
        }
    }
}

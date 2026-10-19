using System.Text.Json;
using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Tutorial;
using Xunit;

namespace Spacebox.Tests.Tutorial;

public class TutorialRunnerTests
{
    private readonly FakeContext _context = new FakeContext();
    private readonly List<string> _log = new List<string>();

    private TutorialRunner CreateRunner(TutorialProgress progress = null)
    {
        var runner = new TutorialRunner();
        runner.StepStarted += step => _log.Add("start:" + step.Id);
        runner.TaskCompleted += task => _log.Add("task:" + task.Definition.Type);
        runner.StepCompleted += (step, rewards, skipped) => _log.Add($"step:{step.Id}:{rewards.Count}:{skipped}");
        runner.ChapterCompleted += (chapter, rewards) => _log.Add($"chapter:{chapter.Id}:{rewards.Count}");
        runner.Finished += _ => _log.Add("finished");
        runner.Load(TutorialTestData.Parse(TutorialTestData.TwoChapters), progress, _context);
        return runner;
    }

    private void PressForwardTwice(TutorialRunner runner)
    {
        _context.Pressed.Add("forward");
        runner.Update(_context, 0.1f);
        runner.Update(_context, 0.1f);
        runner.Update(_context, 0.1f);
        _context.Pressed.Clear();
    }

    [Fact]
    public void Runner_PlaysThroughChaptersWithRewards()
    {
        var runner = CreateRunner();
        Assert.Equal("fly", runner.CurrentStep.Id);

        PressForwardTwice(runner);
        Assert.Equal("ore", runner.CurrentStep.Id);

        _context.Items[10] = 5;
        runner.Update(_context, 0.1f);
        Assert.Equal("place", runner.CurrentStep.Id);

        runner.OnActivity(new PlayerActivityEvent(PlayerActivityKind.BlockPlaced, 20, 1, null));
        runner.Update(_context, 0.1f);

        Assert.True(runner.Progress.Finished);
        Assert.Null(runner.CurrentStep);
        Assert.Equal(new[]
        {
            "start:fly", "task:press", "step:fly:1:False", "start:ore", "task:have_item", "step:ore:0:False",
            "chapter:first:1", "start:place", "task:place_block", "step:place:0:False", "chapter:second:0", "finished"
        }, _log);
    }

    [Fact]
    public void Runner_IncludesStepsOfTheCurrentGameMode()
    {
        _context.GameMode = GameMode.Creative;
        var runner = CreateRunner(new TutorialProgress { StepId = "place", CompletedSteps = new List<string> { "fly", "ore" } });

        runner.OnActivity(new PlayerActivityEvent(PlayerActivityKind.BlockPlaced, 20, 1, null));
        runner.Update(_context, 0.1f);

        Assert.Equal("creative_only", runner.CurrentStep.Id);
    }

    [Fact]
    public void Skip_GivesNoStepReward()
    {
        var runner = CreateRunner();

        Assert.True(runner.Skip(_context));

        Assert.Contains("step:fly:0:True", _log);
        Assert.Equal("ore", runner.CurrentStep.Id);
    }

    [Fact]
    public void Restart_DoesNotRewardTwice()
    {
        var runner = CreateRunner();
        PressForwardTwice(runner);

        runner.Restart(_context);
        PressForwardTwice(runner);

        Assert.Equal(2, _log.Count(e => e.StartsWith("step:fly")));
        Assert.Contains("step:fly:1:False", _log);
        Assert.Contains("step:fly:0:False", _log);
    }

    [Fact]
    public void Restart_DoesNotRepeatChapterRewards()
    {
        var runner = CreateRunner();
        PlayFirstChapter(runner);
        runner.Restart(_context);
        PlayFirstChapter(runner);

        Assert.Equal(new[] { "chapter:first:1", "chapter:first:0" }, _log.Where(e => e.StartsWith("chapter:first")));
    }

    [Fact]
    public void StartingOverAfterFinishing_KeepsRewardHistory()
    {
        var runner = CreateRunner();
        PlayFirstChapter(runner);
        runner.OnActivity(new PlayerActivityEvent(PlayerActivityKind.BlockPlaced, 20, 1, null));
        runner.Update(_context, 0.1f);
        Assert.True(runner.Progress.Finished);

        _context.Items.Clear();
        var again = CreateRunner(TutorialProgress.StartOver("basics", runner.Progress));
        PlayFirstChapter(again);

        Assert.Equal("place", again.CurrentStep.Id);
        Assert.Equal(2, _log.Count(e => e == "chapter:first:1" || e == "chapter:first:0"));
        Assert.Single(_log, e => e == "chapter:first:1");
        Assert.Single(_log, e => e == "step:fly:1:False");
    }

    [Fact]
    public void StartOver_ResetsStepsButKeepsRewards()
    {
        var previous = new TutorialProgress { StepId = "ore", Finished = true, CompletedSteps = new List<string> { "fly" }, Rewarded = new List<string> { "fly", "chapter:first" } };

        var progress = TutorialProgress.StartOver("basics", previous);

        Assert.Empty(progress.CompletedSteps);
        Assert.False(progress.Finished);
        Assert.True(progress.Enabled);
        Assert.Equal(previous.Rewarded, progress.Rewarded);
        Assert.NotSame(previous.Rewarded, progress.Rewarded);
    }

    private void PlayFirstChapter(TutorialRunner runner)
    {
        PressForwardTwice(runner);
        _context.Items[10] = 5;
        runner.Update(_context, 0.1f);
        _context.Items[10] = 0;
    }

    [Fact]
    public void Stop_PausesAndResumeContinues()
    {
        var runner = CreateRunner();
        runner.Stop();

        _context.Pressed.Add("forward");
        runner.Update(_context, 0.1f);
        runner.Update(_context, 0.1f);
        Assert.False(runner.IsRunning);
        Assert.Equal("fly", runner.Progress.StepId);

        runner.Resume(_context);
        runner.Update(_context, 0.1f);
        runner.Update(_context, 0.1f);
        runner.Update(_context, 0.1f);

        Assert.Equal("ore", runner.CurrentStep.Id);
    }

    [Fact]
    public void Progress_RoundTripsThroughJson()
    {
        var runner = CreateRunner();
        PressForwardTwice(runner);
        _context.Items[10] = 3;
        runner.Update(_context, 0.1f);
        runner.SyncProgress();

        string json = JsonSerializer.Serialize(runner.Progress);
        var loaded = JsonSerializer.Deserialize<TutorialProgress>(json);
        _context.Items[10] = 0;
        var restored = CreateRunner(loaded);

        Assert.Equal("ore", restored.CurrentStep.Id);
        Assert.Equal(3, restored.Tasks[0].Current);
        Assert.Contains("fly", restored.Progress.CompletedSteps);
    }

    [Fact]
    public void ReadOnlyStep_CompletesAfterReadTime()
    {
        var tutorial = TutorialTestData.Parse("""{ "Chapters": [ { "Steps": [ { "Id": "read", "ReadSeconds": 2, "Delay": 1 }, { "Id": "next" } ] } ] }""");
        var runner = new TutorialRunner();
        runner.Load(tutorial, null, _context);

        runner.Update(_context, 1.5f);
        Assert.False(runner.IsStepVisible && runner.CurrentStep.Id != "read");
        runner.Update(_context, 1.6f);

        Assert.Equal("next", runner.CurrentStep.Id);
    }

    [Fact]
    public void OptionalTasks_DoNotBlockTheStep()
    {
        var runner = CreateRunner(new TutorialProgress { StepId = "ore", CompletedSteps = new List<string> { "fly" } });

        _context.Items[10] = 5;
        runner.Update(_context, 0.1f);

        Assert.Equal("place", runner.CurrentStep.Id);
    }

    [Fact]
    public void Progress_ForUi()
    {
        var runner = CreateRunner(new TutorialProgress { StepId = "ore", CompletedSteps = new List<string> { "fly" } });
        _context.Items[10] = 2;
        runner.Update(_context, 0.1f);

        runner.GetChapterProgress(out int completed, out int total);

        Assert.Equal(1, completed);
        Assert.Equal(2, total);
        Assert.Equal(0.4f, runner.GetCurrentStepFraction(), 3);
    }
}

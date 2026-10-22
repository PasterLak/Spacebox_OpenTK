using Spacebox.Game.Player;
using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Tutorial.Tasks;

namespace Spacebox.Game.Tutorial;

public sealed class TutorialRunner
{
    private const string ChapterRewardPrefix = TutorialRewardKeys.ChapterPrefix;

    private static readonly IReadOnlyList<TutorialReward> NoRewards = Array.Empty<TutorialReward>();

    private readonly List<ITutorialTask> _tasks = new List<ITutorialTask>();
    private readonly List<bool> _reported = new List<bool>();
    private int _chapterIndex = -1;
    private int _stepIndex = -1;
    private float _stepTime;
    private GameMode _mode = GameMode.Survival;

    public TutorialDefinition Definition { get; private set; }
    public TutorialProgress Progress { get; private set; }
    public IReadOnlyList<ITutorialTask> Tasks => _tasks;
    public int ChapterIndex => _chapterIndex;
    public int StepIndex => _stepIndex;
    public float StepTime => _stepTime;
    public bool IsLoaded => Definition != null && Progress != null;
    public bool IsRunning => IsLoaded && Progress.Enabled && !Progress.Finished && CurrentStep != null;
    public bool IsStepVisible => IsRunning && _stepTime >= CurrentStep.Delay;
    public TutorialChapter CurrentChapter => IsLoaded && _chapterIndex >= 0 ? Definition.Chapters[_chapterIndex] : null;
    public TutorialStep CurrentStep => CurrentChapter != null && _stepIndex >= 0 ? CurrentChapter.Steps[_stepIndex] : null;

    public event Action<TutorialStep> StepStarted;
    public event Action<ITutorialTask> TaskCompleted;
    public event Action<TutorialStep, IReadOnlyList<TutorialReward>, bool> StepCompleted;
    public event Action<TutorialChapter, IReadOnlyList<TutorialReward>> ChapterCompleted;
    public event Action<TutorialDefinition> Finished;

    public void Load(TutorialDefinition definition, TutorialProgress progress, ITutorialContext context)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Progress = progress ?? new TutorialProgress();
        Progress.Normalize();
        Progress.TutorialId = definition.Id;
        _mode = context.GameMode;

        ClearStep();
        if (!Progress.Finished) SelectStep(Progress.StepId, context, true);
    }

    public void Unload()
    {
        ClearStep();
        Definition = null;
        Progress = null;
    }

    public void Update(ITutorialContext context, float deltaTime)
    {
        if (!IsRunning) return;

        _mode = context.GameMode;
        if (!CurrentStep.IsAvailableIn(_mode))
        {
            AdvanceFrom(context, CurrentChapter, CurrentStep);
            return;
        }

        _stepTime += deltaTime;
        if (_stepTime < CurrentStep.Delay) return;

        for (int i = 0; i < _tasks.Count; i++)
        {
            if (!_tasks[i].IsDone) _tasks[i].Update(context, deltaTime);
        }

        ReportCompletedTasks();
        if (IsCurrentStepDone()) CompleteStep(context, false);
    }

    public void OnActivity(in PlayerActivityEvent activity)
    {
        if (!IsStepVisible) return;

        for (int i = 0; i < _tasks.Count; i++)
        {
            if (!_tasks[i].IsDone) _tasks[i].OnActivity(activity);
        }

        ReportCompletedTasks();
    }

    public bool Skip(ITutorialContext context)
    {
        if (!IsRunning || !CurrentStep.Skippable) return false;

        CompleteStep(context, true);
        return true;
    }

    public void Stop()
    {
        if (!IsLoaded) return;

        SyncProgress();
        Progress.Enabled = false;
    }

    public void Resume(ITutorialContext context)
    {
        if (!IsLoaded) return;

        Progress.Enabled = true;
        Progress.Declined = false;
        if (CurrentStep == null && !Progress.Finished) SelectStep(Progress.StepId, context, true);
    }

    public void Restart(ITutorialContext context)
    {
        if (!IsLoaded) return;

        Progress.CompletedSteps.Clear();
        Progress.TaskProgress.Clear();
        Progress.StepId = "";
        Progress.Finished = false;
        Progress.Enabled = true;
        Progress.Declined = false;
        _mode = context.GameMode;

        ClearStep();
        SelectStep(null, context, false);
    }

    public void SyncProgress()
    {
        if (!IsLoaded) return;

        Progress.TaskProgress.Clear();
        for (int i = 0; i < _tasks.Count; i++)
            Progress.TaskProgress.Add(_tasks[i].Current);
    }

    public float GetCurrentStepFraction()
    {
        int required = 0;
        float sum = 0f;
        for (int i = 0; i < _tasks.Count; i++)
        {
            if (_tasks[i].Definition.Optional) continue;
            required++;
            sum += _tasks[i].IsDone ? 1f : _tasks[i].Current / _tasks[i].Target;
        }

        if (required == 0) return CurrentStep == null ? 0f : Math.Clamp((_stepTime - CurrentStep.Delay) / CurrentStep.ReadSeconds, 0f, 1f);
        return sum / required;
    }

    public void GetChapterProgress(out int completed, out int total)
    {
        completed = 0;
        total = 0;
        var chapter = CurrentChapter;
        if (chapter == null) return;

        for (int i = 0; i < chapter.Steps.Count; i++)
        {
            var step = chapter.Steps[i];
            if (!step.IsAvailableIn(_mode)) continue;

            total++;
            if (Progress.CompletedSteps.Contains(step.Id)) completed++;
        }
    }

    private bool IsCurrentStepDone()
    {
        var step = CurrentStep;
        if (step == null) return false;

        bool hasRequired = false;
        for (int i = 0; i < _tasks.Count; i++)
        {
            if (_tasks[i].Definition.Optional) continue;
            hasRequired = true;
            if (!_tasks[i].IsDone) return false;
        }

        return hasRequired || _stepTime >= step.Delay + step.ReadSeconds;
    }

    private void ReportCompletedTasks()
    {
        for (int i = 0; i < _tasks.Count; i++)
        {
            if (_reported[i] || !_tasks[i].IsDone) continue;

            _reported[i] = true;
            TaskCompleted?.Invoke(_tasks[i]);
        }
    }

    private void CompleteStep(ITutorialContext context, bool skipped)
    {
        var chapter = CurrentChapter;
        var step = CurrentStep;

        if (!Progress.CompletedSteps.Contains(step.Id)) Progress.CompletedSteps.Add(step.Id);
        var rewards = !skipped && TryMarkRewarded(step.Id) ? step.Rewards : NoRewards;
        StepCompleted?.Invoke(step, rewards, skipped);

        AdvanceFrom(context, chapter, step);
    }

    private void AdvanceFrom(ITutorialContext context, TutorialChapter chapter, TutorialStep step)
    {
        Definition.TryFindStep(step.Id, out int chapterIndex, out int stepIndex);
        bool found = FindNextStep(chapterIndex, stepIndex + 1, out int nextChapter, out int nextStep);

        if (!found || nextChapter != chapterIndex)
        {
            var rewards = TryMarkRewarded(ChapterRewardPrefix + chapter.Id) ? chapter.Rewards : NoRewards;
            ChapterCompleted?.Invoke(chapter, rewards);
        }

        Progress.TaskProgress.Clear();
        ClearStep();

        if (!found)
        {
            Progress.Finished = true;
            Progress.StepId = "";
            Finished?.Invoke(Definition);
            return;
        }

        StartStep(nextChapter, nextStep, context, false);
    }

    private void SelectStep(string stepId, ITutorialContext context, bool restore)
    {
        if (!string.IsNullOrEmpty(stepId) &&
            Definition.TryFindStep(stepId, out int chapterIndex, out int stepIndex) &&
            !Progress.CompletedSteps.Contains(stepId) &&
            Definition.Chapters[chapterIndex].Steps[stepIndex].IsAvailableIn(_mode))
        {
            StartStep(chapterIndex, stepIndex, context, restore);
            return;
        }

        if (FindNextStep(0, 0, out int nextChapter, out int nextStep))
        {
            StartStep(nextChapter, nextStep, context, false);
            return;
        }

        Progress.Finished = true;
        Progress.StepId = "";
    }

    private bool FindNextStep(int chapterIndex, int stepIndex, out int foundChapter, out int foundStep)
    {
        for (int c = Math.Max(0, chapterIndex); c < Definition.Chapters.Count; c++)
        {
            var steps = Definition.Chapters[c].Steps;
            for (int s = c == chapterIndex ? Math.Max(0, stepIndex) : 0; s < steps.Count; s++)
            {
                var step = steps[s];
                if (Progress.CompletedSteps.Contains(step.Id) || !step.IsAvailableIn(_mode)) continue;

                foundChapter = c;
                foundStep = s;
                return true;
            }
        }

        foundChapter = -1;
        foundStep = -1;
        return false;
    }

    private void StartStep(int chapterIndex, int stepIndex, ITutorialContext context, bool restore)
    {
        ClearStep();
        _chapterIndex = chapterIndex;
        _stepIndex = stepIndex;

        var step = CurrentStep;
        Progress.StepId = step.Id;

        for (int i = 0; i < step.Tasks.Count; i++)
        {
            var task = TutorialTaskFactory.Create(step.Tasks[i]);
            if (task != null) _tasks.Add(task);
        }

        if (restore && Progress.TaskProgress.Count == _tasks.Count)
        {
            for (int i = 0; i < _tasks.Count; i++) _tasks[i].Restore(Progress.TaskProgress[i]);
        }

        for (int i = 0; i < _tasks.Count; i++)
        {
            _tasks[i].Start(context);
            _reported.Add(_tasks[i].IsDone);
        }

        StepStarted?.Invoke(step);
    }

    private void ClearStep()
    {
        _tasks.Clear();
        _reported.Clear();
        _chapterIndex = -1;
        _stepIndex = -1;
        _stepTime = 0f;
    }

    private bool TryMarkRewarded(string key)
    {
        if (Progress.Rewarded.Contains(key)) return false;

        Progress.Rewarded.Add(key);
        return true;
    }
}

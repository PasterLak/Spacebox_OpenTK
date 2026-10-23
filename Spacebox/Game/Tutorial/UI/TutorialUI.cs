using Engine;
using Engine.Audio;
using Engine.InputPro;
using Engine.CanvasUI;
using Spacebox.Game.GUI;
using Spacebox.Game.Tutorial.Tasks;
using System.Numerics;

namespace Spacebox.Game.Tutorial.UI;

public sealed class TutorialUI : IGameUiController
{
    private const string ToggleAction = "tutorial";
    private const float WidthRatio = 0.21f;
    private const float MinWidthInLines = 22f;
    private const float StrikeTime = 0.35f;
    private const float FlashTime = 0.7f;
    private const float PulseTime = 0.9f;
    private const float FadeInTime = 0.35f;
    private const float FooterSeconds = 15f;
    private const float UiOpenAlpha = 0.55f;

    private sealed class TaskLayout
    {
        public readonly List<string> Lines = new List<string>();
        public string Counter;
    }

    private readonly List<string> _textLines = new List<string>();
    private readonly List<string> _hintLines = new List<string>();
    private readonly List<string> _titleLines = new List<string>();
    private readonly List<TaskLayout> _taskLayouts = new List<TaskLayout>();
    private readonly Dictionary<ITutorialTask, float> _doneTimes = new Dictionary<ITutorialTask, float>();
    private readonly Dictionary<string, AudioSource> _customSounds = new Dictionary<string, AudioSource>();

    private string _layoutStepId;
    private float _layoutWidth;
    private float _layoutSize;
    private float _clock;
    private float _stepStartedAt;
    private float _stepCompletedAt = -10f;
    private float _footerUntil;
    private bool _initialized;
    private AudioSource _tickSound;
    private AudioSource _stepSound;
    private AudioSource _chapterSound;
    private AudioSource _clickSound;
    private readonly WorldLoadingController _loading;
    private readonly ChatController _chat;

    public TutorialUI(WorldLoadingController loading, ChatController chat)
    {
        _loading = loading;
        _chat = chat;
    }

    public bool IsInitialized => _initialized;

    public void Initialize(Spacebox.Game.Player.LocalAstronaut player)
    {
        if (_initialized || Application.IsHeadless) return;

        _initialized = true;
        _tickSound = CreateSound("Resources/Audio/UI/click2.ogg", 0.5f);
        _stepSound = CreateSound("crafted", 0.7f);
        _chapterSound = CreateSound("analyzer_complete", 0.8f);
        _clickSound = CreateSound("click1", 1f);

        var runner = TutorialService.Runner;
        runner.StepStarted += OnStepStarted;
        runner.TaskCompleted += OnTaskCompleted;
        runner.StepCompleted += OnStepCompleted;
        runner.ChapterCompleted += OnChapterCompleted;
        runner.Finished += OnFinished;
        _footerUntil = _clock + FooterSeconds;
    }

    public void Reset()
    {
        if (!_initialized) return;

        var runner = TutorialService.Runner;
        runner.StepStarted -= OnStepStarted;
        runner.TaskCompleted -= OnTaskCompleted;
        runner.StepCompleted -= OnStepCompleted;
        runner.ChapterCompleted -= OnChapterCompleted;
        runner.Finished -= OnFinished;

        _tickSound?.Dispose();
        _stepSound?.Dispose();
        _chapterSound?.Dispose();
        _clickSound?.Dispose();
        _tickSound = _stepSound = _chapterSound = _clickSound = null;
        foreach (var sound in _customSounds.Values) sound?.Dispose();
        _customSounds.Clear();
        _doneTimes.Clear();
        _layoutStepId = null;
        _initialized = false;
        TutorialToasts.Clear();
    }

    public void PlayClick()
    {
        Play(_clickSound);
    }

    public void Update()
    {
        if (!_initialized) return;
        _clock += Time.Delta;
        if (!TutorialService.IsAttached) return;

        if (UIManager.IsUIMode || Debug.IsVisible || _chat.FocusInput || !TutorialService.Runner.IsRunning) return;
        if (InputManager.Instance.HasAction(ToggleAction) && Input.IsActionDown(ToggleAction))
        {
            TutorialService.SetCollapsed(!TutorialService.IsCollapsed);
            _footerUntil = _clock + FooterSeconds * 0.5f;
        }
    }

    public bool IsShown => _initialized && Settings.ShowInterface && !_loading.IsActive;

    public void Draw(TutorialCanvas drawList, Vector2 display, float baseSize)
    {
        if (!IsShown) return;

        float width = MathF.Max(display.X * WidthRatio, baseSize * MinWidthInLines);
        float margin = baseSize * 0.9f;
        float top = margin + (_chat.IsShownOnRight ? display.Y * 0.5f : 0f);
        var topLeft = new Vector2(display.X - width - margin, top);

        float bottom = top;
        var runner = TutorialService.Runner;
        if (runner.IsRunning && !Debug.IsVisible)
        {
            float alpha = UIManager.IsUIMode ? UiOpenAlpha : 1f;
            bottom = TutorialService.IsCollapsed
                ? DrawCollapsed(drawList, runner, topLeft, width, baseSize, alpha)
                : DrawPanel(drawList, runner, topLeft, width, baseSize, alpha);
        }

        TutorialToasts.Draw(drawList, new Vector2(display.X - margin, bottom + baseSize * 0.5f), display, width, baseSize, _clock);
    }

    private float DrawPanel(TutorialCanvas drawList, TutorialRunner runner, Vector2 topLeft, float width, float baseSize, float alpha)
    {
        var font = drawList.Font;
        float pad = baseSize * 0.7f;
        float inner = width - pad * 2f;
        EnsureLayout(runner, font, inner, baseSize);

        float height = Layout(drawList, runner, font, topLeft, width, baseSize, alpha, false);
        var max = topLeft + new Vector2(width, height);
        float pulse = 1f - Math.Clamp((_clock - _stepCompletedAt) / PulseTime, 0f, 1f);
        uint border = TutorialDraw.Lerp(TutorialDraw.PanelBorder, TutorialDraw.Gold, pulse);

        drawList.Panel(topLeft, max, TutorialDraw.WithAlpha(TutorialDraw.PanelBackground, alpha), TutorialDraw.WithAlpha(border, alpha), baseSize * 0.45f, MathF.Max(1f, MathF.Round(baseSize * (0.08f + 0.12f * pulse))));
        Layout(drawList, runner, font, topLeft, width, baseSize, alpha, true);
        return max.Y;
    }

    private float Layout(TutorialCanvas drawList, TutorialRunner runner, CanvasFont font, Vector2 topLeft, float width, float baseSize, float alpha, bool draw)
    {
        float pad = baseSize * 0.7f;
        float smallSize = baseSize * 0.9f;
        float titleSize = baseSize * 1.15f;
        float line = baseSize * 1.3f;
        float x = topLeft.X + pad;
        float right = topLeft.X + width - pad;
        float y = topLeft.Y + pad;

        var chapter = runner.CurrentChapter;
        runner.GetChapterProgress(out int completed, out int total);
        if (draw)
        {
            TutorialDraw.Diamond(drawList, new Vector2(x + smallSize * 0.4f, y + smallSize * 0.55f), smallSize * 0.35f, TutorialDraw.WithAlpha(TutorialDraw.Gold, alpha));
            string counter = $"{Math.Min(completed + 1, total)} / {total}";
            float counterWidth = TutorialDraw.Measure(font, smallSize, counter);
            string chapterTitle = TutorialDraw.Truncate(font, smallSize, (chapter?.Title ?? "").ToUpperInvariant(), right - x - smallSize * 1.2f - counterWidth - pad);
            drawList.Text(smallSize, new Vector2(x + smallSize * 1.1f, y), TutorialDraw.WithAlpha(TutorialDraw.Gold, alpha), chapterTitle);
            drawList.Text(smallSize, new Vector2(right - counterWidth, y), TutorialDraw.WithAlpha(TutorialDraw.TextFaint, alpha), counter);
        }
        y += smallSize * 1.35f;

        float barHeight = MathF.Max(3f, baseSize * 0.32f);
        if (draw)
        {
            float fraction = total > 0 ? Math.Clamp((completed + runner.GetCurrentStepFraction()) / total, 0f, 1f) : 0f;
            var barMin = new Vector2(x, y);
            var barMax = new Vector2(right, y + barHeight);
            drawList.RectFilled(barMin, barMax, TutorialDraw.WithAlpha(TutorialDraw.BarBackground, alpha), barHeight * 0.5f);
            if (fraction > 0f)
                drawList.RectFilled(barMin, new Vector2(x + (right - x) * fraction, barMax.Y), TutorialDraw.WithAlpha(TutorialDraw.Lerp(TutorialDraw.Gold, TutorialDraw.Green, fraction), alpha), barHeight * 0.5f);
        }
        y += barHeight + pad * 0.8f;

        if (!runner.IsStepVisible) return y - topLeft.Y + pad * 0.3f;

        float fade = alpha * TutorialDraw.EaseOut((_clock - _stepStartedAt) / FadeInTime);
        var step = runner.CurrentStep;
        float iconSize = titleSize * 1.5f;
        float titleX = x;
        if (!step.Icon.IsEmpty)
        {
            if (draw) TutorialDraw.Icon(drawList, step.Icon, new Vector2(x, y - titleSize * 0.15f), iconSize, fade);
            titleX += iconSize + baseSize * 0.4f;
        }

        float titleTop = y;
        for (int i = 0; i < _titleLines.Count; i++)
        {
            if (draw) drawList.Text(titleSize, new Vector2(titleX, y), TutorialDraw.WithAlpha(TutorialDraw.TextBright, fade), _titleLines[i]);
            y += titleSize * 1.2f;
        }
        y = MathF.Max(y, titleTop + (step.Icon.IsEmpty ? 0f : iconSize)) + baseSize * 0.25f;

        for (int i = 0; i < _textLines.Count; i++)
        {
            if (draw) drawList.Text(baseSize, new Vector2(x, y), TutorialDraw.WithAlpha(TutorialDraw.TextDim, fade), _textLines[i]);
            y += line * 0.92f;
        }
        if (_textLines.Count > 0) y += baseSize * 0.4f;

        var tasks = runner.Tasks;
        for (int i = 0; i < tasks.Count && i < _taskLayouts.Count; i++)
            y = DrawTask(drawList, font, tasks[i], _taskLayouts[i], x, right, y, baseSize, fade, draw) + baseSize * 0.2f;

        if (_hintLines.Count > 0)
        {
            y += baseSize * 0.3f;
            for (int i = 0; i < _hintLines.Count; i++)
            {
                if (draw) drawList.Text(smallSize, new Vector2(x, y), TutorialDraw.WithAlpha(i == 0 ? TutorialDraw.Gold : TutorialDraw.TextFaint, fade * 0.9f), _hintLines[i]);
                y += smallSize * 1.25f;
            }
        }

        if (_clock < _footerUntil)
        {
            y += baseSize * 0.2f;
            if (draw)
            {
                string footer = $"[{KeyName(ToggleAction)}] hide   |   Esc > Tutorial: options";
                drawList.Text(smallSize * 0.9f, new Vector2(x, y), TutorialDraw.WithAlpha(TutorialDraw.TextFaint, fade * 0.8f), TutorialDraw.Truncate(font, smallSize * 0.9f, footer, right - x));
            }
            y += smallSize * 1.1f;
        }

        return y - topLeft.Y + pad * 0.6f;
    }

    private float DrawTask(TutorialCanvas drawList, CanvasFont font, ITutorialTask task, TaskLayout layout, float x, float right, float y, float baseSize, float alpha, bool draw)
    {
        float line = baseSize * 1.3f;
        float box = baseSize * 0.85f;
        float iconSize = line;
        bool hasIcon = !task.Definition.Icon.IsEmpty;
        float textX = x + box + baseSize * 0.5f + (hasIcon ? iconSize + baseSize * 0.3f : 0f);
        float rowHeight = MathF.Max(line * layout.Lines.Count, line);
        bool done = task.IsDone;
        _doneTimes.TryGetValue(task, out float doneAt);
        float sinceDone = done && doneAt > 0f ? _clock - doneAt : float.MaxValue;

        if (!draw) return y + rowHeight + (ShowsBar(task) ? baseSize * 0.35f : 0f);

        if (sinceDone < FlashTime)
        {
            float flash = 1f - sinceDone / FlashTime;
            drawList.RectFilled(new Vector2(x - baseSize * 0.25f, y - baseSize * 0.1f), new Vector2(right + baseSize * 0.25f, y + rowHeight), TutorialDraw.WithAlpha(TutorialDraw.DarkGreen, alpha * flash * 0.6f), baseSize * 0.25f);
        }

        TutorialDraw.CheckBox(drawList, new Vector2(x, y + (line - box) * 0.4f), box, done, task.Definition.Optional, alpha);
        if (hasIcon) TutorialDraw.Icon(drawList, task.Definition.Icon, new Vector2(x + box + baseSize * 0.4f, y + (line - iconSize) * 0.3f), iconSize, alpha);

        uint color = done ? TutorialDraw.Green : task.Definition.Optional ? TutorialDraw.Cyan : TutorialDraw.TextBright;
        float strike = done ? (sinceDone == float.MaxValue ? 1f : TutorialDraw.EaseOut(sinceDone / StrikeTime)) : 0f;
        float lineY = y;
        for (int i = 0; i < layout.Lines.Count; i++)
        {
            drawList.Text(baseSize, new Vector2(textX, lineY), TutorialDraw.WithAlpha(color, alpha), layout.Lines[i]);
            if (strike > 0f)
            {
                float lineWidth = TutorialDraw.Measure(font, baseSize, layout.Lines[i]);
                float middle = lineY + baseSize * 0.55f;
                drawList.Line(new Vector2(textX - baseSize * 0.1f, middle), new Vector2(textX + (lineWidth + baseSize * 0.2f) * strike, middle), TutorialDraw.WithAlpha(TutorialDraw.Green, alpha), MathF.Max(1.5f, baseSize * 0.1f));
            }
            lineY += line;
        }

        if (!done && task.ShowsCounter)
        {
            string counter = $"{TutorialText.FormatNumber(MathF.Floor(task.Current))} / {TutorialText.FormatNumber(task.Target)}";
            drawList.Text(baseSize, new Vector2(right - TutorialDraw.Measure(font, baseSize, counter), y), TutorialDraw.WithAlpha(TutorialDraw.Gold, alpha), counter);
        }

        float bottom = y + rowHeight;
        if (ShowsBar(task))
        {
            float barHeight = MathF.Max(2f, baseSize * 0.18f);
            var min = new Vector2(textX, bottom + baseSize * 0.02f);
            var max = new Vector2(right, min.Y + barHeight);
            drawList.RectFilled(min, max, TutorialDraw.WithAlpha(TutorialDraw.BarBackground, alpha), barHeight);
            drawList.RectFilled(min, new Vector2(min.X + (max.X - min.X) * (task.Current / task.Target), max.Y), TutorialDraw.WithAlpha(TutorialDraw.Gold, alpha), barHeight);
            bottom += baseSize * 0.35f;
        }

        return bottom;
    }

    private static bool ShowsBar(ITutorialTask task)
    {
        return !task.IsDone && !task.ShowsCounter && task.Target > 1f && task.Current > 0f;
    }

    private float DrawCollapsed(TutorialCanvas drawList, TutorialRunner runner, Vector2 topLeft, float width, float baseSize, float alpha)
    {
        var font = drawList.Font;
        float pad = baseSize * 0.5f;
        float height = baseSize * 1.3f + pad * 2f;
        var max = topLeft + new Vector2(width, height);
        runner.GetChapterProgress(out int completed, out int total);

        drawList.RectFilled(topLeft, max, TutorialDraw.WithAlpha(TutorialDraw.PanelBackground, alpha), baseSize * 0.4f);
        float x = topLeft.X + pad;
        float y = topLeft.Y + pad;
        TutorialDraw.Diamond(drawList, new Vector2(x + baseSize * 0.4f, y + baseSize * 0.6f), baseSize * 0.35f, TutorialDraw.WithAlpha(TutorialDraw.Gold, alpha));

        string counter = $"{Math.Min(completed + 1, total)}/{total}";
        x += baseSize * 1.1f;
        drawList.Text(baseSize, new Vector2(x, y), TutorialDraw.WithAlpha(TutorialDraw.Gold, alpha), counter);
        x += TutorialDraw.Measure(font, baseSize, counter) + baseSize * 0.6f;

        string title = runner.IsStepVisible ? Format(runner.CurrentStep.Title, 0f) : "...";
        string progress = $"{(int)MathF.Round(runner.GetCurrentStepFraction() * 100f)}%";
        float progressWidth = TutorialDraw.Measure(font, baseSize, progress);
        drawList.Text(baseSize, new Vector2(x, y), TutorialDraw.WithAlpha(TutorialDraw.TextBright, alpha), TutorialDraw.Truncate(font, baseSize, title, max.X - pad - progressWidth - baseSize * 0.6f - x));
        drawList.Text(baseSize, new Vector2(max.X - pad - progressWidth, y), TutorialDraw.WithAlpha(TutorialDraw.Green, alpha), progress);
        return max.Y;
    }

    private void EnsureLayout(TutorialRunner runner, CanvasFont font, float width, float baseSize)
    {
        var step = runner.CurrentStep;
        if (step == null) return;
        if (_layoutStepId == step.Id && MathF.Abs(_layoutWidth - width) < 0.5f && MathF.Abs(_layoutSize - baseSize) < 0.01f && _taskLayouts.Count == runner.Tasks.Count) return;

        _layoutStepId = step.Id;
        _layoutWidth = width;
        _layoutSize = baseSize;

        float titleWidth = width - (step.Icon.IsEmpty ? 0f : baseSize * 1.15f * 1.5f + baseSize * 0.4f);
        TutorialDraw.Wrap(font, baseSize * 1.15f, Format(step.Title, 0f), titleWidth, _titleLines);
        TutorialDraw.Wrap(font, baseSize, Format(step.Text, 0f), width, _textLines);
        TutorialDraw.Wrap(font, baseSize * 0.9f, string.IsNullOrWhiteSpace(step.Hint) ? "" : "Tip: " + Format(step.Hint, 0f), width, _hintLines);

        _taskLayouts.Clear();
        float box = baseSize * 0.85f;
        foreach (var task in runner.Tasks)
        {
            var layout = new TaskLayout();
            float labelWidth = width - box - baseSize * 0.5f - (task.Definition.Icon.IsEmpty ? 0f : baseSize * 1.6f) - (task.ShowsCounter ? baseSize * 3.5f : 0f);
            string label = (task.Definition.Optional ? "Bonus: " : "") + Format(task.Definition.Label, task.Target);
            TutorialDraw.Wrap(font, baseSize, label, labelWidth, layout.Lines);
            _taskLayouts.Add(layout);
        }
    }

    private string Format(string text, float target)
    {
        return TutorialText.Format(text, TutorialService.Text, target);
    }

    private string KeyName(string action)
    {
        return TutorialService.Text?.KeyName(action) ?? action;
    }

    private void OnStepStarted(TutorialStep step)
    {
        _stepStartedAt = _clock;
        _layoutStepId = null;
        _doneTimes.Clear();
    }

    private void OnTaskCompleted(ITutorialTask task)
    {
        _doneTimes[task] = _clock;
        Play(_tickSound);
    }

    private void OnStepCompleted(TutorialStep step, IReadOnlyList<TutorialReward> rewards, bool skipped)
    {
        _stepCompletedAt = _clock;
        if (skipped) return;

        if (!string.IsNullOrWhiteSpace(step.Data.CompleteSound)) PlayOnce(step.Data.CompleteSound);
        else Play(_stepSound);
        TutorialToasts.Add("Step complete!", Format(step.Title, 0f), rewards, step.Icon, false, _clock);
    }

    private void OnChapterCompleted(TutorialChapter chapter, IReadOnlyList<TutorialReward> rewards)
    {
        Play(_chapterSound);
        TutorialToasts.Add("Chapter complete", Format(chapter.Title, 0f), rewards, chapter.Icon, true, _clock);
    }

    private void OnFinished(TutorialDefinition definition)
    {
        TutorialToasts.Add("Tutorial complete!", "You know the basics. Good luck out there!", null, TutorialIcon.None, true, _clock + 0.5f);
    }

    private static AudioSource CreateSound(string clip, float volume)
    {
        var audioClip = Resources.Load<AudioClip>(clip);
        return audioClip == null ? null : new AudioSource(audioClip, AudioChannel.UI) { Volume = volume };
    }

    private static void Play(AudioSource source)
    {
        if (source == null) return;
        if (source.IsPlaying) source.Stop();
        source.Play();
    }

    private void PlayOnce(string clip)
    {
        if (!_customSounds.TryGetValue(clip, out var source))
        {
            source = CreateSound(clip, 0.7f);
            _customSounds[clip] = source;
        }

        Play(source ?? _stepSound);
    }
}

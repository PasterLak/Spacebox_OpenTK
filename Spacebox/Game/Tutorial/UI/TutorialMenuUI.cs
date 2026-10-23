using Engine;
using Spacebox.Game.GUI;

namespace Spacebox.Game.Tutorial.UI;

public static class TutorialMenuUI
{
    public const string WindowId = "tutorialMenu";

    private static readonly List<(string Label, Action Click)> _buttons = new List<(string, Action)>();
    private static bool _confirmRestart;
    private static bool _confirmStop;

    public static bool IsOpen => UIManager.IsOpen(WindowId);

    public static void Open()
    {
        _confirmRestart = false;
        _confirmStop = false;
        UIManager.Open(WindowId);
    }

    public static IReadOnlyList<(string Label, Action Click)> Buttons => _buttons;

    public static string Description => DescribeState(TutorialService.Runner);

    public static void Refresh()
    {
        BuildButtons(TutorialService.Runner);
    }

    public static void Press(int index)
    {
        if (index < 0 || index >= _buttons.Count) return;
        TutorialUI.PlayClick();
        _buttons[index].Click();
    }

    private static void BuildButtons(TutorialRunner runner)
    {
        _buttons.Clear();
        bool available = TutorialService.HasTutorialFor(CurrentMode());

        if (runner.IsRunning)
        {
            _buttons.Add((TutorialService.IsCollapsed ? "Show panel" : "Collapse panel", () => TutorialService.SetCollapsed(!TutorialService.IsCollapsed)));
            if (runner.CurrentStep.Skippable) _buttons.Add(("Skip this step", () => TutorialService.Skip()));
            _buttons.Add((_confirmStop ? "Click again to stop" : "Stop tutorial", () =>
            {
                if (!_confirmStop)
                {
                    _confirmStop = true;
                    return;
                }

                TutorialService.Stop();
                UIManager.CloseTop();
            }));
        }
        else if (available)
        {
            bool canResume = runner.IsLoaded && !runner.Progress.Finished;
            _buttons.Add((canResume ? "Resume tutorial" : "Start tutorial", () =>
            {
                TutorialService.Resume();
                UIManager.CloseAll();
            }));
        }

        if (available && runner.IsLoaded)
        {
            _buttons.Add((_confirmRestart ? "Click again to restart" : "Restart from the beginning", () =>
            {
                if (!_confirmRestart)
                {
                    _confirmRestart = true;
                    return;
                }

                TutorialService.Restart();
                UIManager.CloseAll();
            }));
        }

        _buttons.Add(("Back", UIManager.CloseTop));
    }

    private static string DescribeState(TutorialRunner runner)
    {
        if (!TutorialService.HasTutorialFor(CurrentMode())) return "There is no tutorial for this game mode.";
        if (!runner.IsLoaded) return "Learn the game step by step: flying, mining, crafting, machines and ships.";
        if (runner.Progress.Finished) return $"{runner.Definition.Name}: finished. Well done!";
        if (!runner.IsRunning) return $"{runner.Definition.Name}: stopped. Your progress is kept.";

        runner.GetChapterProgress(out int completed, out int total);
        string step = TutorialText.Format(runner.CurrentStep.Title, TutorialService.Text);
        return $"{runner.CurrentChapter.Title} ({Math.Min(completed + 1, total)}/{total})\nCurrent step: {step}";
    }

    private static Spacebox.Game.Player.GameModes.GameMode CurrentMode()
    {
        return Spacebox.Game.Generation.World.Instance?.Player?.GameMode ?? Spacebox.Game.Player.GameModes.GameMode.Survival;
    }
}

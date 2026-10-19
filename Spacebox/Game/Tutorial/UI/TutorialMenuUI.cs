using Engine;
using ImGuiNET;
using Spacebox.Game.GUI;
using Spacebox.Game.GUI.Menu;
using System.Numerics;

namespace Spacebox.Game.Tutorial.UI;

public static class TutorialMenuUI
{
    public const string WindowId = "tutorialMenu";

    private static readonly Vector4 TitleColor = new Vector4(1f, 0.77f, 0.16f, 1f);
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

    public static void Render()
    {
        var runner = TutorialService.Runner;
        var display = ImGui.GetIO().DisplaySize;
        BuildButtons(runner);

        float windowWidth = display.X * 0.2f;
        float buttonHeight = display.Y * 0.045f;
        float spacing = buttonHeight * 0.35f;
        float textHeight = ImGui.GetTextLineHeightWithSpacing();
        float headerHeight = textHeight * 5.5f;
        float windowHeight = headerHeight + _buttons.Count * (buttonHeight + spacing) + spacing * 2f;
        var windowPos = GameMenu.CenterNextWindow(windowWidth, windowHeight);

        ImGui.SetNextWindowPos(windowPos);
        ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));
        ImGui.Begin("TutorialMenu", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);
        GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), display.Y, 0.005f);

        float padding = windowWidth * 0.06f;
        ImGui.SetCursorPos(new Vector2(padding, spacing));
        ImGui.TextColored(TitleColor, "TUTORIAL");
        ImGui.SetCursorPosX(padding);
        ImGui.PushTextWrapPos(windowWidth - padding);
        ImGui.TextWrapped(DescribeState(runner));
        ImGui.PopTextWrapPos();

        float buttonWidth = windowWidth * 0.9f;
        float y = headerHeight;
        foreach (var (label, click) in _buttons)
        {
            ImGui.SetCursorPos(new Vector2((windowWidth - buttonWidth) * 0.5f, y));
            GameMenu.CenterButtonWithBackground(label, buttonWidth, buttonHeight, () =>
            {
                TutorialUI.PlayClick();
                click();
            });
            y += buttonHeight + spacing;
        }

        ImGui.End();
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

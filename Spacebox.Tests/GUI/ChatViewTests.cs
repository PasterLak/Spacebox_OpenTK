using Engine.CanvasUI;
using Engine.CanvasUI.Validation;
using Engine.Commands;
using OpenTK.Mathematics;
using Spacebox.Game;
using Spacebox.Game.GUI;
using Spacebox.Game.GUI.Canvas;
using Spacebox.Game.GUI.Canvas.Screens;

namespace Spacebox.Tests.GUI;

public class ChatViewTests
{
    private static readonly LayoutRules Rules = new LayoutRules { MinHitTargetPixels = 12f };

    public static IEnumerable<object[]> Designs() => new[] { new object[] { GameUiDesign.Classic }, new object[] { GameUiDesign.Modern } };

    private static (CanvasRoot Root, ChatView View) Build(GameUiDesign design)
    {
        var root = new CanvasRoot { Theme = GameCanvasThemes.Create(design, ShowcaseFixture.Assets.Font) };
        GameCanvas.Configure(root.Stretch);
        var view = root.Add(new ChatView(), UiLayer.Popup);
        view.Visible = true;
        return (root, view);
    }

    private static ChatLog SampleLog(int count)
    {
        var log = new ChatLog();
        for (int i = 0; i < count; i++)
        {
            string text = i % 5 == 0
                ? $"Player{i}: a much longer chat message that has to wrap over several lines because it does not fit into the chat width at all"
                : $"Player{i}: hello";
            log.Add(text, i % 3 == 0 ? Color4.Yellow : Color4.White, i);
        }
        return log;
    }

    private static readonly CommandSuggestion[] Suggestions =
    {
        new CommandSuggestion("give", "<item> [count] gives items to a player with a long description"),
        new CommandSuggestion("gamemode", "<mode>"),
        new CommandSuggestion("goto", "")
    };

    [Fact]
    public void Opacity_StaysFullThenFadesOutBeforeTheHideDelay()
    {
        Assert.Equal(1f, ChatLog.Opacity(0f, 10f));
        Assert.Equal(1f, ChatLog.Opacity(9f, 10f));
        Assert.Equal(0.5f, ChatLog.Opacity(9.5f, 10f), 3);
        Assert.Equal(0f, ChatLog.Opacity(10f, 10f));
        Assert.Equal(0f, ChatLog.Opacity(25f, 10f));
    }

    [Fact]
    public void Log_DropsOldestMessagesAndTracksChanges()
    {
        var log = new ChatLog(3);
        for (int i = 0; i < 5; i++) log.Add("m" + i, Color4.White, i);

        Assert.Equal(3, log.Count);
        Assert.Equal("m2", log.Messages[0].Text);
        Assert.Equal(5, log.Version);
        Assert.True(log.HasVisible(10f, 10f));
        Assert.False(log.HasVisible(14f, 10f));

        log.Clear();
        Assert.Equal(0, log.Count);
        Assert.False(log.HasVisible(4f, 10f));
    }

    [Fact]
    public void ClosedChat_HidesExpiredMessagesAndShowsAllWhileTyping()
    {
        var (_, view) = Build(GameUiDesign.Classic);
        var log = SampleLog(6);

        view.Present(log, 13.5f, 10f, false, true);
        var labels = ShowcaseFixture.Descendants<Label>(view).Where(l => l.Text.StartsWith("Player")).ToList();
        Assert.Equal(6, labels.Count);
        Assert.Equal(2, labels.Count(l => l.Visible));
        Assert.False(view.InputField.Visible);

        view.Present(log, 13.5f, 10f, true, true);
        Assert.All(labels, l => Assert.True(l.Visible));
        Assert.True(view.InputField.Visible);
    }

    [Theory]
    [MemberData(nameof(Designs))]
    public void ClosedChat_LaysOutCleanly(GameUiDesign design)
    {
        var (root, view) = Build(design);
        view.Present(SampleLog(40), 40f, 10f, false, true);

        var report = LayoutHarness.ValidateAll(root, ScreenProfiles.All, Rules);
        Assert.True(report.IsClean, report.Format());
    }

    [Theory]
    [MemberData(nameof(Designs))]
    public void TypingWithSuggestions_LaysOutCleanly(GameUiDesign design)
    {
        var (root, view) = Build(design);
        view.Present(SampleLog(40), 40f, 10f, true, true);
        view.SyncInput("/g", 1);
        view.PresentSuggestions("/give <item> [count] [player]", Suggestions, 1, 1);

        var report = LayoutHarness.ValidateAll(root, ScreenProfiles.All, Rules);
        Assert.True(report.IsClean, report.Format());
        Assert.True(root.WantsKeyboard);
    }

    [Theory]
    [MemberData(nameof(Designs))]
    public void TypingOnTheRight_LaysOutCleanly(GameUiDesign design)
    {
        var (root, view) = Build(design);
        view.Present(SampleLog(3), 3f, 10f, true, false);
        view.SyncInput("/", 1);
        view.PresentSuggestions("", Suggestions, 0, 1);

        var report = LayoutHarness.ValidateAll(root, ScreenProfiles.All, Rules);
        Assert.True(report.IsClean, report.Format());
    }

    [Fact]
    public void StoppingInput_ReleasesKeyboardFocus()
    {
        var (root, view) = Build(GameUiDesign.Classic);
        var log = SampleLog(2);
        view.Present(log, 2f, 10f, true, true);
        view.SyncInput("hi", 1);
        Assert.True(root.WantsKeyboard);

        view.Present(log, 2f, 10f, false, true);
        Assert.False(root.WantsKeyboard);
    }
}

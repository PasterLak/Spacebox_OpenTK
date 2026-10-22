using System.Text.Json.Nodes;
using Spacebox.Editor.Data;

namespace Spacebox.Tests.Editor;

public sealed class TutorialOutlineTests
{
    private static JsonObject Tutorial() => JsonNode.Parse("""
        {
          "Id": "t",
          "Chapters": [
            { "Id": "a", "Title": "First", "Steps": [ { "Id": "s1", "Title": "One" }, { "Id": "s2", "Title": "Two" } ] },
            { "Id": "b", "Title": "Second", "Optional": true, "Steps": [] }
          ]
        }
        """)!.AsObject();

    [Fact]
    public void Nodes_ListSettingsThenChaptersWithTheirSteps()
    {
        var outline = new TutorialOutline(Tutorial());
        var nodes = outline.Nodes();

        Assert.Equal(5, nodes.Count);
        Assert.Equal(TutorialNode.Settings, nodes[0]);
        Assert.Equal(new TutorialNode(TutorialNodeKind.Step, 0, 1), nodes[3]);
        Assert.Equal("2. Two", outline.Label(nodes[3]));
        Assert.Equal("2. Second (optional)", outline.Label(nodes[4]));
    }

    [Fact]
    public void Get_ReturnsOnlyTheSelectedPart()
    {
        var root = Tutorial();
        var outline = new TutorialOutline(root);

        Assert.Same(root, outline.Get(TutorialNode.Settings));
        Assert.Equal("s2", outline.Get(new TutorialNode(TutorialNodeKind.Step, 0, 1))!["Id"]!.GetValue<string>());
        Assert.Null(outline.Get(new TutorialNode(TutorialNodeKind.Step, 1, 0)));
    }

    [Fact]
    public void AddStep_InsertsAfterTheSelectedStepWithAUniqueId()
    {
        var outline = new TutorialOutline(Tutorial());
        var added = outline.AddStep(new TutorialNode(TutorialNodeKind.Step, 0, 0));

        Assert.Equal(new TutorialNode(TutorialNodeKind.Step, 0, 1), added);
        Assert.Equal("step", outline.Get(added)!["Id"]!.GetValue<string>());
        Assert.Equal("s2", outline.Get(new TutorialNode(TutorialNodeKind.Step, 0, 2))!["Id"]!.GetValue<string>());

        var second = outline.AddStep(new TutorialNode(TutorialNodeKind.Chapter, 1));
        Assert.Equal(new TutorialNode(TutorialNodeKind.Step, 1, 0), second);
    }

    [Fact]
    public void AddChapter_GoesAfterTheSelectedChapterAndAddStepCreatesOneWhenEmpty()
    {
        var outline = new TutorialOutline(Tutorial());
        var chapter = outline.AddChapter(new TutorialNode(TutorialNodeKind.Step, 0, 1));
        Assert.Equal(new TutorialNode(TutorialNodeKind.Chapter, 1), chapter);
        Assert.Equal("Second", outline.Get(new TutorialNode(TutorialNodeKind.Chapter, 2))!["Title"]!.GetValue<string>());

        var empty = new TutorialOutline(new JsonObject());
        var step = empty.AddStep(TutorialNode.Settings);
        Assert.Equal(new TutorialNode(TutorialNodeKind.Step, 0, 0), step);
    }

    [Fact]
    public void MoveDuplicateAndRemove_KeepTheSelectionOnTheMovedItem()
    {
        var outline = new TutorialOutline(Tutorial());
        var first = new TutorialNode(TutorialNodeKind.Step, 0, 0);

        Assert.False(outline.CanMove(first, -1));
        var moved = outline.Move(first, 1);
        Assert.Equal(new TutorialNode(TutorialNodeKind.Step, 0, 1), moved);
        Assert.Equal("s1", outline.Get(moved)!["Id"]!.GetValue<string>());

        var copy = outline.Duplicate(moved);
        Assert.Equal("s1_2", outline.Get(copy)!["Id"]!.GetValue<string>());

        var afterRemove = outline.Remove(new TutorialNode(TutorialNodeKind.Chapter, 1));
        Assert.Equal(new TutorialNode(TutorialNodeKind.Chapter, 0), afterRemove);
        Assert.Equal(TutorialNode.Settings, outline.Remove(new TutorialNode(TutorialNodeKind.Chapter, 0)));
    }
}

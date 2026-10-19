using Spacebox.Game.Player.GameModes;
using Xunit;

namespace Spacebox.Tests.Tutorial;

public class TutorialParserTests
{
    [Fact]
    public void Parse_ResolvesChaptersStepsTasksAndRewards()
    {
        var warnings = new List<string>();
        var tutorial = TutorialTestData.Parse(TutorialTestData.TwoChapters, warnings);

        Assert.Empty(warnings);
        Assert.Equal("basics", tutorial.Id);
        Assert.Equal(2, tutorial.Chapters.Count);
        Assert.Equal("fly", tutorial.Chapters[0].Steps[0].Id);
        Assert.Equal(10, tutorial.Chapters[0].Steps[1].Tasks[0].ItemId);
        Assert.True(tutorial.Chapters[0].Steps[1].Tasks[1].Optional);
        Assert.Equal(40, tutorial.Chapters[0].Rewards[0].ItemId);
        Assert.Equal(2, tutorial.Chapters[0].Rewards[0].Count);
        Assert.Equal(GameMode.Creative, tutorial.Chapters[1].Steps[1].RequiredMode);
        Assert.True(tutorial.SupportsMode(GameMode.Spectator));
    }

    [Fact]
    public void Parse_DropsInvalidTasksWithWarnings()
    {
        var warnings = new List<string>();
        var tutorial = TutorialTestData.Parse("""
        {
          "Chapters": [ { "Steps": [ { "Tasks": [
            { "Type": "teleport" },
            { "Type": "craft", "Item": "unknown_thing" },
            { "Type": "press" },
            { "Type": "have_item" },
            { "Type": "craft", "Item": "furnace" }
          ] } ] } ]
        }
        """, warnings);

        Assert.Equal(4, warnings.Count);
        Assert.Single(tutorial.Chapters[0].Steps[0].Tasks);
        Assert.Equal("chapter1.1", tutorial.Chapters[0].Steps[0].Id);
        Assert.Equal("test", tutorial.Id);
    }

    [Fact]
    public void Parse_IgnoresDuplicateStepIdsAndReadsIcons()
    {
        var warnings = new List<string>();
        var tutorial = TutorialTestData.Parse("""
        {
          "GameModes": ["Survival"],
          "Chapters": [ { "Icon": "ui:slot", "Steps": [
            { "Id": "a", "Icon": "furnace" },
            { "Id": "a" }
          ] } ]
        }
        """, warnings);

        Assert.Single(warnings);
        Assert.Single(tutorial.Chapters[0].Steps);
        Assert.Equal("slot", tutorial.Chapters[0].Icon.UiName);
        Assert.Equal(20, tutorial.Chapters[0].Steps[0].Icon.ItemId);
        Assert.False(tutorial.SupportsMode(GameMode.Creative));
    }

    [Fact]
    public void Parse_ReturnsNullForBrokenOrEmptyFiles()
    {
        var warnings = new List<string>();

        Assert.Null(TutorialTestData.Parse("{ not json", warnings));
        Assert.Null(TutorialTestData.Parse("""{ "Chapters": [] }""", warnings));
        Assert.Equal(2, warnings.Count);
    }

    [Fact]
    public void Parse_TaskWithoutItemGetsTaskIcon()
    {
        var tutorial = TutorialTestData.Parse("""
        { "Chapters": [ { "Steps": [ { "Tasks": [ { "Type": "craft", "Item": "cobalt_drill" }, { "Type": "move", "Icon": "ui:arrow" } ] } ] } ] }
        """);

        var tasks = tutorial.Chapters[0].Steps[0].Tasks;
        Assert.Equal(30, tasks[0].Icon.ItemId);
        Assert.Equal("arrow", tasks[1].Icon.UiName);
        Assert.Equal("Craft {item:cobalt_drill}", tasks[0].Label);
        Assert.Equal("Travel {target} blocks", tasks[1].Label);
    }
}

using Spacebox.Game.Player.GameModes;
using Spacebox.Game.Tutorial;

namespace Spacebox.Tests.Tutorial;

public class TutorialRewardClaimsTests
{
    private const string SurvivalOnly = """
    {
      "Id": "basics",
      "GameModes": ["Survival"],
      "Chapters": [
        {
          "Id": "first",
          "Title": "First",
          "Reward": [{ "Item": "power", "Count": 2 }],
          "Steps": [
            { "Id": "fly", "Title": "Fly", "Tasks": [ { "Type": "press", "Action": "forward" } ], "Reward": [{ "Item": "power", "Count": 1 }] },
            { "Id": "creative_step", "RequiresGameMode": "Creative", "Tasks": [ { "Type": "wait", "Seconds": 1 } ], "Reward": [{ "Item": "power", "Count": 1 }] }
          ]
        }
      ]
    }
    """;

    [Fact]
    public void StepAndChapterRewards_ResolveWithTheTutorialId()
    {
        var tutorial = TutorialTestData.Parse(SurvivalOnly);

        var step = TutorialRewardClaims.Resolve(tutorial, "fly", GameMode.Survival, GameMode.Survival, out string stepKey);
        var chapter = TutorialRewardClaims.Resolve(tutorial, TutorialRewardKeys.ForChapter("first"), GameMode.Survival, GameMode.Survival, out string chapterKey);

        Assert.Single(step);
        Assert.Equal("basics/fly", stepKey);
        Assert.Equal(2, chapter[0].Count);
        Assert.Equal("basics/chapter:first", chapterKey);
    }

    [Fact]
    public void Claims_AreRejectedForMissingTutorialsUnknownKeysAndOtherModes()
    {
        var tutorial = TutorialTestData.Parse(SurvivalOnly);

        Assert.Empty(TutorialRewardClaims.Resolve(null, "fly", GameMode.Survival, GameMode.Survival, out _));
        Assert.Empty(TutorialRewardClaims.Resolve(tutorial, "missing", GameMode.Survival, GameMode.Survival, out var unknownKey));
        Assert.Null(unknownKey);
        Assert.Empty(TutorialRewardClaims.Resolve(tutorial, "fly", GameMode.Creative, GameMode.Creative, out _));
        Assert.Empty(TutorialRewardClaims.Resolve(tutorial, "creative_step", GameMode.Survival, GameMode.Survival, out _));
    }
}

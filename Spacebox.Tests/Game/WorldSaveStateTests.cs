using Spacebox.Game;
using Spacebox.Game.Generation;
using Spacebox.Game.Player.GameModes;

namespace Spacebox.Tests.Game;

[Collection(GameAssetsCollection.Name)]
public class WorldSaveStateTests
{
    private const string Player = "{\"PositionX\":1,\"Health\":100,\"Statistics\":{\"TimePlayed\":5}}";

    [Fact]
    public void NothingIsUnsavedBeforeTrackingStarts()
    {
        WorldSaveState.Reset();
        WorldSaveState.MarkChanged();

        Assert.False(WorldSaveState.HasUnsavedChanges("{\"PositionX\":2}"));
    }

    [Fact]
    public void WorldChangesAreUnsavedUntilTheNextSave()
    {
        WorldSaveState.Reset();
        WorldSaveState.MarkSaved(Player);
        Assert.False(WorldSaveState.HasUnsavedChanges(Player));

        WorldSaveState.MarkChanged();
        Assert.True(WorldSaveState.HasUnsavedChanges(Player));

        WorldSaveState.MarkSaved(Player);
        Assert.False(WorldSaveState.HasUnsavedChanges(Player));
        WorldSaveState.Reset();
    }

    [Fact]
    public void ChangedGameModeIsUnsaved()
    {
        WorldSaveState.Reset();
        WorldSaveState.MarkSaved(Player);

        Assert.False(WorldSaveState.HasUnsavedChanges(Player, GameMode.Survival, GameMode.Survival));
        Assert.True(WorldSaveState.HasUnsavedChanges(Player, GameMode.Creative, GameMode.Survival));
        WorldSaveState.Reset();
        Assert.False(WorldSaveState.HasUnsavedChanges(Player, GameMode.Creative, GameMode.Survival));
    }

    [Fact]
    public void PlayerChangesCountButStatisticsDoNot()
    {
        WorldSaveState.Reset();
        WorldSaveState.MarkSaved(Player);

        Assert.False(WorldSaveState.HasUnsavedChanges("{\"PositionX\":1,\"Health\":100,\"Statistics\":{\"TimePlayed\":99}}"));
        Assert.True(WorldSaveState.HasUnsavedChanges("{\"PositionX\":7,\"Health\":100,\"Statistics\":{\"TimePlayed\":5}}"));
        WorldSaveState.Reset();
    }

    [Fact]
    public void AutosaveInterval_IsZeroWhenOff()
    {
        var settings = new GameplaySettings { Autosave = false, AutosaveMinutes = 5 };
        Assert.Equal(0f, settings.AutosaveIntervalSeconds);

        settings.Autosave = true;
        Assert.Equal(300f, settings.AutosaveIntervalSeconds);
    }

    [Fact]
    public void AutosaveMinutes_OutOfRangeFallsBackToDefault()
    {
        var defaults = new GameplaySettings();
        var settings = new GameplaySettings { AutosaveMinutes = 0 };

        Assert.True(settings.Validate(defaults));
        Assert.Equal(defaults.AutosaveMinutes, settings.AutosaveMinutes);
    }

    [Fact]
    public void AutosaveIsOffByDefaultOutsideFinalBuilds()
    {
        Assert.False(new GameplaySettings().Autosave);
    }

    [Fact]
    public void LeavingSavesAutomaticallyOnlyInFinalBuilds()
    {
        Assert.False(WorldSavePolicy.SavesOnLeave);
    }
}

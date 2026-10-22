using Spacebox.Game.Backups;

namespace Spacebox.Tests.Game;

public sealed class BackupRetentionTests
{
    [Fact]
    public void OldestRotatedBackupsAreDeleted_ManualAndOtherWorldsStay()
    {
        var names = new[]
        {
            "World_auto_20261020_100000.zip",
            "World_auto_20261021_100000.zip",
            "World_stop_20261022_100000.zip",
            "World_auto_20261023_100000.zip",
            "World_manual_20261001_100000.zip",
            "World_0.1.2_20260901_100000.zip",
            "Other_auto_20261001_100000.zip",
            "World_auto_broken.zip"
        };

        var delete = BackupRetention.SelectToDelete(names, "World", new[] { "auto", "stop" }, 2);

        Assert.Equal(new[] { "World_auto_20261021_100000.zip", "World_auto_20261020_100000.zip" }, delete);
    }

    [Fact]
    public void KeepZero_DeletesAllRotated()
    {
        var delete = BackupRetention.SelectToDelete(new[] { "W_auto_20261020_100000.zip" }, "W", new[] { "auto" }, 0);
        Assert.Single(delete);
    }
}

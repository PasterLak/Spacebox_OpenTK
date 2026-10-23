using System.Text.Json;
using Spacebox.Network.Moderation;

namespace Spacebox.Tests.Network;

public sealed class ModerationDataTests
{
    [Fact]
    public void UnbanName_AlsoRemovesTheAddressBannedWithIt()
    {
        var data = new ModerationData();
        data.BanName("Alice");
        data.BanAddress("Alice", "10.0.0.5");

        Assert.True(data.IsBanned("someone", "10.0.0.5"));
        Assert.True(data.UnbanName("alice"));

        Assert.False(data.IsBanned("Alice", "10.0.0.5"));
        Assert.Empty(data.BannedAddresses);
        Assert.Empty(data.BannedAddressOwners);
    }

    [Fact]
    public void UnbanName_KeepsAnAddressStillUsedByAnotherBannedName()
    {
        var data = new ModerationData();
        data.BanName("Alice");
        data.BanAddress("Alice", "10.0.0.5");
        data.BanName("Bob");
        data.BanAddress("Bob", "10.0.0.5");

        data.UnbanName("Alice");

        Assert.True(data.IsBanned("Carol", "10.0.0.5"));
        data.UnbanName("Bob");
        Assert.False(data.IsBanned("Carol", "10.0.0.5"));
    }

    [Fact]
    public void UnbanAddress_ForgetsItsOwner()
    {
        var data = new ModerationData();
        data.BanName("Alice");
        data.BanAddress("Alice", "10.0.0.5");

        Assert.True(data.UnbanAddress("10.0.0.5"));

        Assert.Empty(data.BannedAddressOwners);
        Assert.True(data.IsBanned("Alice", ""));
    }

    [Fact]
    public void OldFiles_WithoutOwners_StillLoad()
    {
        var data = JsonSerializer.Deserialize<ModerationData>("{ \"BannedNames\": [\"Alice\"], \"BannedAddresses\": [\"10.0.0.5\"], \"MutedNames\": null }")!;
        data.ApplyMissingDefaults();

        Assert.NotNull(data.MutedNames);
        Assert.Empty(data.BannedAddressOwners);
        Assert.True(data.UnbanName("Alice"));
        Assert.True(data.IsBanned("Bob", "10.0.0.5"));
    }

    [Fact]
    public void Owners_SurviveSaving()
    {
        var data = new ModerationData();
        data.BanName("Alice");
        data.BanAddress("Alice", "10.0.0.5");

        var loaded = JsonSerializer.Deserialize<ModerationData>(JsonSerializer.Serialize(data))!;
        loaded.ApplyMissingDefaults();
        loaded.UnbanName("Alice");

        Assert.False(loaded.IsBanned("Alice", "10.0.0.5"));
    }

    [Fact]
    public void Folder_FallsBackToTheConfiguredWorldWhileNoWorldIsLoaded()
    {
        string root = Path.Combine(Path.GetTempPath(), "spacebox_moderation_" + Guid.NewGuid().ToString("N"));
        string world = ModerationFolder.DedicatedWorld(root, "ServerWorld")!;
        Directory.CreateDirectory(world);
        try
        {
            Assert.Equal("loaded", ModerationFolder.Resolve("loaded", world));
            Assert.Equal(world, ModerationFolder.Resolve(null, world));
            Assert.Null(ModerationFolder.Resolve(null, Path.Combine(root, "Missing")));
            Assert.Null(ModerationFolder.Resolve(null, null));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}

using Spacebox.Network.Admin;

namespace Spacebox.Tests.Network;

public sealed class PlayerBadgesTests
{
    [Fact]
    public void Developer_AlwaysGetsTheDeveloperBadge()
    {
        Assert.Equal("[DEVELOPER] PasterLak", PlayerBadges.Label("PasterLak", true));
        Assert.Equal("[DEVELOPER] PasterLak", PlayerBadges.Label("PasterLak", false));
        Assert.True(PlayerBadges.IsDeveloper(" pasterlak "));
    }

    [Fact]
    public void OtherPlayers_KeepTheAdminBadge()
    {
        Assert.Equal("[ADMIN] Vlad", PlayerBadges.Label("Vlad", true));
        Assert.Equal("Vlad", PlayerBadges.Label("Vlad", false));
        Assert.False(PlayerBadges.IsDeveloper("PasterLak2"));
    }
}

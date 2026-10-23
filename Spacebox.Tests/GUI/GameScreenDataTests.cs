using Spacebox.Game.GUI;
using Spacebox.Game.GUI.Canvas.Windows;
using Spacebox.Game.Player;
using Spacebox.Network;

namespace Spacebox.Tests.GUI;

public class GameScreenDataTests
{
    [Fact]
    public void StatisticsRows_HaveEverySectionAndDeathCauses()
    {
        var statistics = new PlayerStatistics { PlayTimeSeconds = 3 * 3600 + 25 * 60, SessionsPlayed = 2, BlocksPlaced = 1234567 };
        statistics.DeathCauses["Explosion"] = 2;

        var rows = StatisticsRows.Build(statistics, "Day 1");

        var headers = rows.Where(r => r.IsHeader).Select(r => r.Label).ToArray();
        Assert.Equal(new[] { "General", "Building", "Items", "Health", "Combat", "Exploration" }, headers);
        Assert.Contains(rows, r => r.Label == "Play Time" && r.Value == "3 h 25 min (2 sessions)");
        Assert.Contains(rows, r => r.Label == "Blocks Placed" && r.Value == "1 234 567");
        Assert.Contains(rows, r => r.Label.Trim() == "Explosion" && r.Value == "2");
        Assert.Contains(rows, r => r.Label == "Game time" && r.Value == "Day 1");
    }

    [Fact]
    public void StatisticsRows_EmptyWithoutStatistics()
    {
        Assert.Empty(StatisticsRows.Build(null, ""));
    }

    [Theory]
    [InlineData(5000, "5 000 m")]
    [InlineData(25000, "25.0 km")]
    public void Distance_SwitchesToKilometres(double meters, string expected)
    {
        Assert.Equal(expected, StatisticsRows.FormatDistance(meters).Replace(',', '.'));
    }

    [Theory]
    [InlineData(30, false, StatusKind.Good)]
    [InlineData(120, false, StatusKind.Warning)]
    [InlineData(300, false, StatusKind.Bad)]
    [InlineData(300, true, StatusKind.Muted)]
    public void PingKind_FollowsThresholds(int ping, bool host, StatusKind expected)
    {
        Assert.Equal(expected, PlayerListUI.PingKind(new PlayerListEntry { Ping = ping, IsHost = host }));
    }

    [Fact]
    public void PlayerLabel_MarksHostYouAndMuted()
    {
        var player = new PlayerListEntry { Peer = 3, Name = "Ann", IsHost = true, IsMuted = true };
        string label = PlayerListUI.Label(player, 3);
        Assert.Contains("(host)", label);
        Assert.Contains("(you)", label);
        Assert.Contains("[muted]", label);
        Assert.False(PlayerListUI.CanModeratePlayer(player, 3));
        Assert.True(PlayerListUI.CanModeratePlayer(new PlayerListEntry { Peer = 4 }, 3));
    }
}

using Spacebox.Game.GUI;

namespace Spacebox.Tests.GUI;

public class WorldListOrderTests
{
    private static WorldInfo World(string name, string edited, string opened = null) => new WorldInfo { Name = name, LastEditDate = edited, LastOpenedDate = opened };

    [Fact]
    public void NewestActivity_ComesFirst()
    {
        var worlds = new List<WorldInfo>
        {
            World("Old", "2026-01-01 10:00:00"),
            World("OpenedToday", "2026-01-02 10:00:00", "2026-10-24 09:00:00"),
            World("Created", "2026-10-24 08:00:00"),
            World("NoDate", null)
        };

        WorldListOrder.Sort(worlds);

        Assert.Equal(new[] { "OpenedToday", "Created", "Old", "NoDate" }, worlds.Select(w => w.Name));
    }

    [Fact]
    public void EditDateCounts_WhenNewerThanOpened()
    {
        var a = World("A", "2026-10-24 12:00:00", "2026-10-24 09:00:00");
        var b = World("B", "2026-10-24 10:00:00", "2026-10-24 11:00:00");
        Assert.True(WorldListOrder.Compare(a, b) < 0);
    }

    [Fact]
    public void SameTime_FallsBackToName()
    {
        var worlds = new List<WorldInfo> { World("beta", null), World("Alpha", null) };
        WorldListOrder.Sort(worlds);
        Assert.Equal("Alpha", worlds[0].Name);
    }

    [Fact]
    public void Parse_IgnoresBrokenDates()
    {
        Assert.Equal(DateTime.MinValue, WorldListOrder.Parse("not a date"));
        Assert.Equal(new DateTime(2026, 10, 24, 9, 30, 0), WorldListOrder.Parse("2026-10-24 09:30:00"));
    }
}

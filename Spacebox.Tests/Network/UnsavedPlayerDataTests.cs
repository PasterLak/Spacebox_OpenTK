using Spacebox.Network.Players;

namespace Spacebox.Tests.Network;

public class UnsavedPlayerDataTests
{
    [Fact]
    public void DepartedPlayer_IsWrittenOnlyOnFlush()
    {
        var data = new UnsavedPlayerData();
        var written = new List<string>();

        data.Store("Anna", "{\"a\":1}");
        Assert.Empty(written);

        data.Flush((name, json) => written.Add(name + json));

        Assert.Equal(new[] { "Anna{\"a\":1}" }, written);
        Assert.Equal(0, data.Count);
    }

    [Fact]
    public void RejoiningPlayer_GetsTheUnsavedDataBack()
    {
        var data = new UnsavedPlayerData();
        data.Store("Anna", "new");

        Assert.True(data.TryTake("anna", out var json));
        Assert.Equal("new", json);
        Assert.False(data.TryTake("Anna", out _));
    }

    [Fact]
    public void Clear_DropsUnsavedData()
    {
        var data = new UnsavedPlayerData();
        data.Store("Anna", "x");
        data.Clear();

        int writes = 0;
        data.Flush((_, _) => writes++);
        Assert.Equal(0, writes);
    }
}

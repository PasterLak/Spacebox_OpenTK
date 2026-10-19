using Spacebox.Network;
using Xunit;

namespace Spacebox.Tests.Network;

public class ServerFolderNameTests
{
    [Fact]
    public void For_UsesAddressAndPort()
    {
        Assert.Equal("192.168.1.5_7777", ServerFolderName.For("192.168.1.5", 7777));
        Assert.Equal("play.example.com_25000", ServerFolderName.For(" Play.Example.com ", 25000));
    }

    [Fact]
    public void For_DiffersForServersWithTheSameName()
    {
        Assert.NotEqual(ServerFolderName.For("10.0.0.1", 7777), ServerFolderName.For("10.0.0.2", 7777));
        Assert.NotEqual(ServerFolderName.For("10.0.0.1", 7777), ServerFolderName.For("10.0.0.1", 7778));
    }

    [Fact]
    public void For_IsAValidFolderName()
    {
        string name = ServerFolderName.For("::1", 7777);

        Assert.DoesNotContain(name, c => Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0);
        Assert.Equal("unknown_7777", ServerFolderName.For(null, 7777));
    }
}

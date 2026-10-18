using Spacebox.Network;

namespace Spacebox.Tests.Network;

public class LaunchOptionsTests
{
    [Fact]
    public void Parse_ReadsFlagsAddressPortAndName()
    {
        LaunchOptions.Parse(["--CONNECT", "192.168.1.5:27500", "--name", "Pilot", "--hidden", "--unknown", "--server"]);

        Assert.Equal("192.168.1.5", LaunchOptions.ConnectAddress);
        Assert.Equal(27500, LaunchOptions.ConnectPort);
        Assert.Equal("Pilot", LaunchOptions.PlayerName);
        Assert.True(LaunchOptions.HasConnectTarget);
        Assert.True(LaunchOptions.IsHidden);
        Assert.True(LaunchOptions.IsDedicated);

        LaunchOptions.Parse(["--connect", "example.org", "--name"]);

        Assert.Equal("example.org", LaunchOptions.ConnectAddress);
        Assert.Equal(27500, LaunchOptions.ConnectPort);
        Assert.Equal("Pilot", LaunchOptions.PlayerName);

        LaunchOptions.Parse(["--connect", "host:notaport"]);

        Assert.Equal("host:notaport", LaunchOptions.ConnectAddress);
    }
}

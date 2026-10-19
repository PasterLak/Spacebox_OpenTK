using System.Net;
using Engine.Networking.Query;

namespace Engine.Tests.Networking;

public class LocalBroadcastAddressesTests
{
    [Theory]
    [InlineData("192.168.1.20", "255.255.255.0", "192.168.1.255")]
    [InlineData("10.4.3.2", "255.255.0.0", "10.4.255.255")]
    [InlineData("172.16.5.9", "255.255.255.252", "172.16.5.11")]
    public void GetBroadcastAddress_CombinesAddressAndMask(string address, string mask, string expected)
    {
        Assert.Equal(IPAddress.Parse(expected), LocalBroadcastAddresses.GetBroadcastAddress(IPAddress.Parse(address), IPAddress.Parse(mask)));
    }

    [Fact]
    public void GetBroadcastAddress_RejectsIPv6AndNull()
    {
        Assert.Null(LocalBroadcastAddresses.GetBroadcastAddress(IPAddress.IPv6Loopback, IPAddress.Parse("255.255.255.0")));
        Assert.Null(LocalBroadcastAddresses.GetBroadcastAddress(IPAddress.Parse("10.0.0.1"), null));
    }
}

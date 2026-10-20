using Engine.Web;

namespace Engine.Tests;

public class NetworkAddressesTests
{
    [Fact]
    public void DescribeUrls_ListsLocalhostAndEveryLanAddress()
    {
        string text = NetworkAddresses.DescribeUrls(7777, "/dashboard", new[] { "192.168.1.5", "10.0.0.2" });

        Assert.Equal("http://localhost:7777/dashboard or http://192.168.1.5:7777/dashboard or http://10.0.0.2:7777/dashboard", text);
        Assert.Equal("http://localhost:80/x", NetworkAddresses.DescribeUrls(80, "/x", Array.Empty<string>()));
    }

    [Fact]
    public void LocalIPv4_ReturnsOnlyPrivateAddresses()
    {
        foreach (var address in NetworkAddresses.LocalIPv4())
        {
            Assert.True(NetworkAddresses.IsLocal(address));
            Assert.False(address.StartsWith("127.", StringComparison.Ordinal));
        }
    }
}

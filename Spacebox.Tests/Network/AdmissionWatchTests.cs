using Spacebox.Network;

namespace Spacebox.Tests.Network;

public class AdmissionWatchTests
{
    [Fact]
    public void PeersWithoutJoinRequest_ExpireAfterTheShortTimeout()
    {
        var watch = new AdmissionWatch();
        watch.Connected(2);

        Assert.Empty(watch.Update(AdmissionWatch.JoinRequestTimeout - 1f, _ => false));
        Assert.Equal(new[] { 2 }, watch.Update(2f, _ => false));
        Assert.Equal(0, watch.Count);
    }

    [Fact]
    public void PeersLoggingIn_GetTheLongTimeout()
    {
        var watch = new AdmissionWatch();
        watch.Connected(2);
        watch.Requested(2);

        Assert.Empty(watch.Update(AdmissionWatch.JoinRequestTimeout + 10f, _ => false));
        Assert.Equal(new[] { 2 }, watch.Update(AdmissionWatch.AdmissionTimeout, _ => false));
    }

    [Fact]
    public void AdmittedOrDisconnectedPeers_AreNeverKicked()
    {
        var watch = new AdmissionWatch();
        watch.Connected(2);
        watch.Connected(3);
        watch.Forget(3);

        Assert.Empty(watch.Update(1f, peer => peer == 2));
        Assert.Equal(0, watch.Count);
    }
}

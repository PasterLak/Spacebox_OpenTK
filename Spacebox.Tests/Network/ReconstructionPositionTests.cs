using OpenTK.Mathematics;
using Spacebox.Network.Players;

namespace Spacebox.Tests.Network;

public class ReconstructionPositionTests
{
    [Fact]
    public void ReportedPosition_NearTheTrackedOneIsAccepted()
    {
        Assert.True(ReconstructionNetwork.AcceptsReportedPosition(new Vector3(100f, 0f, 0f), new Vector3(110f, 5f, 0f)));
    }

    [Fact]
    public void ReportedPosition_FarAwayIsRejected()
    {
        Assert.False(ReconstructionNetwork.AcceptsReportedPosition(new Vector3(100f, 0f, 0f), new Vector3(5000f, 0f, 0f)));
    }

    [Fact]
    public void ReportedPosition_NotFiniteIsRejected()
    {
        Assert.False(ReconstructionNetwork.AcceptsReportedPosition(Vector3.Zero, new Vector3(float.NaN, 0f, 0f)));
    }
}

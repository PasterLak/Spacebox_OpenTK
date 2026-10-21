using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Tests.Game;

public class GravityWalkerResetTests
{
    [Fact]
    public void Reset_ClearsVelocityAndState()
    {
        var walker = new GravityWalker();
        walker.AddImpulse(new Vector3(5f, 3f, -2f));

        walker.Reset();

        Assert.Equal(Vector3.Zero, walker.Velocity);
        Assert.False(walker.IsActive);
        Assert.False(walker.IsGrounded);
    }
}

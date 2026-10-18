using Spacebox.Game.Player;

namespace Spacebox.Tests;

public class ProjectileParametersTests
{
    [Fact]
    public void ImpactExplosion_IsOffByDefault()
    {
        var parameters = new ProjectileParameters(1, new ProjectileJSON { ID = "bullet" });

        Assert.False(parameters.ImpactSphere);
        Assert.Equal(0f, parameters.ImpactExplosionRadius);
    }

    [Theory]
    [InlineData(3f, 3f)]
    [InlineData(-2f, 0f)]
    public void ImpactExplosionRadius_IsReadAndClamped(float json, float expected)
    {
        var parameters = new ProjectileParameters(2, new ProjectileJSON { ID = "impulse", ImpactSphere = true, ImpactExplosionRadius = json });

        Assert.True(parameters.ImpactSphere);
        Assert.Equal(expected, parameters.ImpactExplosionRadius);
    }
}

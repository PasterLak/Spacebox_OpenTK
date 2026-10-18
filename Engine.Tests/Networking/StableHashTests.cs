using Engine.Networking;

namespace Engine.Tests.Networking;

public class StableHashTests
{
    [Theory]
    [InlineData("", 2166136261u)]
    [InlineData("a", 0xE40C292Cu)]
    [InlineData("foobar", 0xBF9CF968u)]
    public void Compute_MatchesFnv1a(string value, uint expected)
    {
        Assert.Equal(expected, StableHash.Compute(value));
    }

    [Fact]
    public void Compute_IsDeterministicAndCaseSensitive()
    {
        Assert.Equal(StableHash.Compute("ContainerChangeMessage"), StableHash.Compute("ContainerChangeMessage"));
        Assert.NotEqual(StableHash.Compute("Message"), StableHash.Compute("message"));
    }
}

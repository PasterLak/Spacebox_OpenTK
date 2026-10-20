using Engine.PostProcessing.Ssao;

namespace Engine.Tests.PostProcessing;

public class SsaoKernelTests
{
    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    public void Build_ReturnsSamplesInsideUnitHemisphere(int count)
    {
        var kernel = SsaoKernel.Build(count);

        Assert.Equal(count, kernel.Length);
        foreach (var sample in kernel)
        {
            Assert.True(sample.Z > 0f);
            Assert.InRange(sample.Length, 0.05f, 1.0001f);
        }
    }

    [Fact]
    public void Build_PlacesLaterSamplesFartherOut()
    {
        var kernel = SsaoKernel.Build(16);

        for (int i = 1; i < kernel.Length; i++)
            Assert.True(kernel[i].Length >= kernel[i - 1].Length - 0.0001f);
    }

    [Fact]
    public void Build_IsDeterministicPerSeed()
    {
        Assert.Equal(SsaoKernel.Build(16, 7), SsaoKernel.Build(16, 7));
        Assert.NotEqual(SsaoKernel.Build(16, 7), SsaoKernel.Build(16, 8));
    }

    [Fact]
    public void Build_WithNoSamples_ReturnsEmpty()
    {
        Assert.Empty(SsaoKernel.Build(0));
    }
}

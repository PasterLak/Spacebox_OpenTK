using Engine.Diagnostics;

namespace Engine.Tests.Diagnostics;

public sealed class SystemReportTests
{
    [Fact]
    public void ProcCpuInfo_GivesTheModelName()
    {
        const string text = "processor\t: 0\nvendor_id\t: AuthenticAMD\nmodel name\t: AMD Ryzen 7  5800X 8-Core Processor\ncpu MHz\t\t: 3800.000\n";
        Assert.Equal("AMD Ryzen 7 5800X 8-Core Processor", CpuName.FromProcCpuInfo(text));
        Assert.Equal("", CpuName.FromProcCpuInfo("processor : 0\n"));
        Assert.Equal("", CpuName.FromProcCpuInfo(null));
    }

    [Fact]
    public void Lines_ListHardwareAndSkipGpuOnHeadlessServers()
    {
        var desktop = new SystemReport { Cpu = "Test CPU", LogicalCores = 8, MemoryBytes = 16L * 1024 * 1024 * 1024, GpuRenderer = "Radeon RX 6600", GpuVendor = "ATI Technologies Inc.", OpenGlVersion = "3.3.0", GlslVersion = "3.30", Monitor = "1920x1080", GameVersion = "0.1.5" };
        var lines = desktop.Lines();
        Assert.Contains("[System] CPU: Test CPU, 8 logical cores", lines);
        Assert.Contains("[System] RAM: 16.0 GB", lines);
        Assert.Contains("[System] GPU: Radeon RX 6600 (ATI Technologies Inc.)", lines);
        Assert.Contains("[System] Monitor: 1920x1080", lines);

        var server = new SystemReport { Cpu = "", LogicalCores = 2 };
        var serverLines = server.Lines();
        Assert.DoesNotContain(serverLines, line => line.StartsWith("[System] GPU"));
        Assert.Contains("[System] CPU: unknown, 2 logical cores", serverLines);
        Assert.Contains("[System] RAM: unknown", serverLines);
    }
}

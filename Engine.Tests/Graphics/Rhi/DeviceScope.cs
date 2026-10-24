using Engine.Graphics.Rhi;

namespace Engine.Tests.Graphics.Rhi;

public sealed class DeviceScope : IDisposable
{
    private readonly IGraphicsDevice _previous;

    public DeviceScope()
    {
        Device = new RecordingGraphicsDevice();
        _previous = Gpu.Install(Device);
    }

    public RecordingGraphicsDevice Device { get; }

    public void Dispose()
    {
        Gpu.Install(_previous);
    }
}

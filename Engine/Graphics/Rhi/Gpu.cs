namespace Engine.Graphics.Rhi;

public static class Gpu
{
    private static IGraphicsDevice _device = NullGraphicsDevice.Instance;

    public static IGraphicsDevice Device => _device;

    public static bool IsAvailable => _device is not NullGraphicsDevice;

    public static IGraphicsDevice Install(IGraphicsDevice device)
    {
        var previous = _device;
        _device = device ?? NullGraphicsDevice.Instance;
        return previous;
    }
}

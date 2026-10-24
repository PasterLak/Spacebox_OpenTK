using Engine.Graphics;

namespace Engine.Tests.Graphics.Rhi;

[Collection(GlobalStateCollection.Name)]
public sealed class UniformBufferTests : IDisposable
{
    private struct Item
    {
        public float A, B, C, D;
    }

    private readonly DeviceScope _scope = new DeviceScope();

    public void Dispose() => _scope.Dispose();

    [Fact]
    public void Create_AllocatesFullSizeBeforeBinding()
    {
        var buffer = new UniformBuffer<Item>("UniformBufferTests_Create", 8);

        var calls = _scope.Device.Calls;
        int allocate = calls.FindIndex(c => c.StartsWith("AllocateBuffer Uniform", StringComparison.Ordinal));
        int bind = calls.FindIndex(c => c.StartsWith("BindBufferBase Uniform", StringComparison.Ordinal));
        Assert.True(allocate >= 0 && bind > allocate);
        Assert.EndsWith($" {8 * 16} Dynamic", calls[allocate]);
        Assert.Equal(128, buffer.SizeInBytes);
    }

    [Fact]
    public void Update_WritesOnlyUsedPartAndSkipsEmpty()
    {
        var buffer = new UniformBuffer<Item>("UniformBufferTests_Update", 4);
        _scope.Device.Clear();

        buffer.Update(ReadOnlySpan<Item>.Empty);
        Assert.Empty(_scope.Device.Calls);

        buffer.Update(new Item[2]);
        Assert.Single(_scope.Device.Calls);
        Assert.EndsWith(" 0 32", _scope.Device.Calls[0]);

        _scope.Device.Clear();
        buffer.Update(new Item[9]);
        Assert.EndsWith(" 0 64", _scope.Device.Calls[0]);
        Assert.DoesNotContain(_scope.Device.Calls, c => c.StartsWith("BufferData", StringComparison.Ordinal));
    }
}

using Engine.Graphics.Rhi;
using System.Collections.Concurrent;

namespace Engine.Graphics
{
    public static class BindingAllocator
    {
        static int _next = 0;
        static readonly ConcurrentDictionary<string, int> _map = new();
        public static int GetOrAssign(string block) => _map.GetOrAdd(block, _ => _next++);
    }

    public sealed class UniformBuffer<T> where T : unmanaged
    {
        BufferHandle _handle;
        public int Binding { get; }

        public UniformBuffer(string blockName)
        {
            Binding = BindingAllocator.GetOrAssign(blockName);
            if (Application.IsHeadless) return;

            EnsureCreated();
        }

        void EnsureCreated()
        {
            if (_handle.IsValid || !Gpu.IsAvailable) return;

            _handle = Gpu.Device.CreateBuffer();
            Gpu.Device.BindBufferBase(BufferKind.Uniform, Binding, _handle);
        }

        public void Update(ReadOnlySpan<T> data)
        {
            if (!_handle.IsValid)
            {
                if (Application.IsHeadless) return;
                EnsureCreated();
                if (!_handle.IsValid) return;
            }

            Gpu.Device.BufferData(BufferKind.Uniform, _handle, data, BufferUsage.Dynamic);
        }
    }
}

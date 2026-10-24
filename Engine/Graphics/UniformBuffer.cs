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
        public int Capacity { get; }
        public int SizeInBytes => Capacity * System.Runtime.CompilerServices.Unsafe.SizeOf<T>();

        public UniformBuffer(string blockName, int capacity = 1)
        {
            Binding = BindingAllocator.GetOrAssign(blockName);
            Capacity = Math.Max(1, capacity);
            if (Application.IsHeadless) return;

            EnsureCreated();
        }

        void EnsureCreated()
        {
            if (_handle.IsValid || !Gpu.IsAvailable) return;

            _handle = Gpu.Device.CreateBuffer();
            Gpu.Device.AllocateBuffer(BufferKind.Uniform, _handle, SizeInBytes, BufferUsage.Dynamic);
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

            if (data.Length == 0) return;
            if (data.Length > Capacity) data = data[..Capacity];
            Gpu.Device.BufferSubData(BufferKind.Uniform, _handle, 0, data);
        }
    }
}

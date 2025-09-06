using OpenTK.Graphics.OpenGL4;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        readonly int _handle;
        public int Binding { get; }

        public UniformBuffer(string blockName)
        {
            Binding = BindingAllocator.GetOrAssign(blockName);
            _handle = GL.GenBuffer();
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, Binding, _handle);
        }

        public void Update(ReadOnlySpan<T> data)
        {
            GL.BindBuffer(BufferTarget.UniformBuffer, _handle);
            GL.BufferData(BufferTarget.UniformBuffer,
                data.Length * Unsafe.SizeOf<T>(),
                ref MemoryMarshal.GetReference(data),
                BufferUsageHint.DynamicDraw);
        }
    }
}

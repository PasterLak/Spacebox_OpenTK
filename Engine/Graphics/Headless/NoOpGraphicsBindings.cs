using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK;

namespace Engine.Graphics.Headless;

public sealed unsafe class NoOpGraphicsBindings : IBindingsContext
{
    private static readonly IntPtr ReturnZero = (IntPtr)(delegate* unmanaged<nint>)&Zero;

    public static bool IsSupported => RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.Arm64;

    public IntPtr GetProcAddress(string procName) => ReturnZero;

    public static void Install()
    {
        OpenTK.Graphics.OpenGL4.GL.LoadBindings(new NoOpGraphicsBindings());
    }

    [UnmanagedCallersOnly]
    private static nint Zero() => 0;
}

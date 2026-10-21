namespace Engine.Graphics.Debugging;

public static class RenderDebugView
{
    private static readonly RenderDebugMode[] Modes = Enum.GetValues<RenderDebugMode>();

    public static RenderDebugMode Mode { get; set; } = RenderDebugMode.Lit;

    public static bool IsWireframe => Mode == RenderDebugMode.Wireframe;
    public static bool ShowsBuffer => IsBufferMode(Mode);

    public static bool IsBufferMode(RenderDebugMode mode)
    {
        return mode is RenderDebugMode.Normals or RenderDebugMode.Depth or RenderDebugMode.GBufferMask;
    }

    public static void Next() => Mode = Step(Mode, 1);

    public static void Previous() => Mode = Step(Mode, -1);

    public static RenderDebugMode Step(RenderDebugMode mode, int direction)
    {
        int index = Array.IndexOf(Modes, mode);
        return Modes[((index + direction) % Modes.Length + Modes.Length) % Modes.Length];
    }

    public static string Describe(RenderDebugMode mode) => mode switch
    {
        RenderDebugMode.Wireframe => "Wireframe: triangle edges of every mesh",
        RenderDebugMode.Normals => "G-buffer normals (view space, RGB = XYZ)",
        RenderDebugMode.Depth => "Depth buffer (linear, near = white)",
        RenderDebugMode.GBufferMask => "G-buffer mask: green world, blue held item, black none",
        RenderDebugMode.Ssao => "SSAO occlusion (needs SSAO enabled)",
        _ => "Lit"
    };
}

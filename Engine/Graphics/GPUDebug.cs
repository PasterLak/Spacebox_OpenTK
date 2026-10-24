using Engine.Graphics.Rhi;

namespace Engine.Graphics
{
    public static class GPUDebug
    {
        public static bool Enabled { get; private set; }
        public static bool Supported { get; private set; }

        public static void Initialize(bool enable, bool installCallback = true, bool sync = true)
        {
            Enabled = enable;
            if (!Enabled) return;

            Supported = Gpu.Device.InitializeDebugOutput(installCallback, sync);
        }

        public static void SetEnabled(bool enable) => Enabled = enable;

        public static IDisposable Group(string name)
        {
            if (Enabled) Gpu.Device.PushDebugGroup(name);
            return new Region();
        }

        public static void EndGroup()
        {
            if (!Enabled) return;
            Gpu.Device.PopDebugGroup();
        }

        public static void Marker(string name)
        {
            if (!Enabled) return;
            Gpu.Device.InsertDebugMarker(name);
        }

        public static void Label(DebugObjectKind kind, int handle, string name)
        {
            if (!Enabled || handle == 0) return;
            Gpu.Device.SetDebugLabel(kind, handle, name);
        }

        public static void LabelProgram(int handle, string name) => Label(DebugObjectKind.Program, handle, name);
        public static void LabelShader(int handle, string name) => Label(DebugObjectKind.Shader, handle, name);
        public static void LabelTexture(int handle, string name) => Label(DebugObjectKind.Texture, handle, name);
        public static void LabelBuffer(int handle, string name) => Label(DebugObjectKind.Buffer, handle, name);
        public static void LabelVertexArray(int handle, string name) => Label(DebugObjectKind.VertexArray, handle, name);
        public static void LabelFramebuffer(int handle, string name) => Label(DebugObjectKind.Framebuffer, handle, name);
        public static void LabelRenderbuffer(int handle, string name) => Label(DebugObjectKind.Renderbuffer, handle, name);
        public static void LabelQuery(int handle, string name) => Label(DebugObjectKind.Query, handle, name);
        public static void LabelSampler(int handle, string name) => Label(DebugObjectKind.Sampler, handle, name);

        struct Region : IDisposable
        {
            public void Dispose() => EndGroup();
        }
    }
}

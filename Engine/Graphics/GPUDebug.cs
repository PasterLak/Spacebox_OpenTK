
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace Engine.Graphics
{
    public static class GPUDebug
    {
        public static bool Enabled { get; private set; }
        public static bool HasKHR { get; private set; }
        public static bool HasEXT { get; private set; }

        static DebugProc? _callbackKeepAlive;

        public static void Initialize(bool enable, bool installCallback = true, bool sync = true)
        {
            Enabled = enable;

            int major = GL.GetInteger(GetPName.MajorVersion);
            int minor = GL.GetInteger(GetPName.MinorVersion);
            HasKHR = major > 4 || major == 4 && minor >= 3 || HasExtension("GL_KHR_debug");
            HasEXT = HasExtension("GL_EXT_debug_marker");

            if (!Enabled) return;

            if (HasKHR)
            {
                GL.Enable(EnableCap.DebugOutput);
                if (sync) GL.Enable(EnableCap.DebugOutputSynchronous);

                if (installCallback)
                {
                    _callbackKeepAlive = (src, type, id, severity, length, message, userParam) =>
                    {
                        string txt = Marshal.PtrToStringAnsi(message, length) ?? "";
                        System.Diagnostics.Debug.WriteLine($"[GL] {type} {severity} {id}: {txt}");
                    };
                    GL.DebugMessageCallback(_callbackKeepAlive!, nint.Zero);

                    GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare,
                        DebugSeverityControl.DontCare, 0, Array.Empty<int>(), true);
                }
            }
        }

        public static void SetEnabled(bool enable) => Enabled = enable;

        public static IDisposable Group(string name)
        {
            if (Enabled)
            {
                if (HasKHR)
                    GL.PushDebugGroup(DebugSourceExternal.DebugSourceApplication, 0, name.Length, name);
                else if (HasEXT)
                    GL.Ext.PushGroupMarker(0, name);
            }
            return new Region();
        }

        public static void EndGroup()
        {
            if (!Enabled) return;
            if (HasKHR) GL.PopDebugGroup();
            else if (HasEXT) GL.Ext.PopGroupMarker();
        }

        public static void Marker(string name)
        {
            if (!Enabled) return;
            if (HasKHR)
                GL.DebugMessageInsert(DebugSourceExternal.DebugSourceApplication,
                                      DebugType.DebugTypeMarker, 0,
                                      DebugSeverity.DebugSeverityNotification, name.Length, name);
            else if (HasEXT)
                GL.Ext.InsertEventMarker(0, name);
        }

        public static void Label(ObjectLabelIdentifier id, int handle, string name)
        {
            if (!Enabled || !HasKHR || handle == 0) return;
            GL.ObjectLabel(id, handle, name.Length, name);
        }

        public static void LabelProgram(int handle, string name) => Label(ObjectLabelIdentifier.Program, handle, name);
        public static void LabelShader(int handle, string name) => Label(ObjectLabelIdentifier.Shader, handle, name);
        public static void LabelTexture(int handle, string name) => Label(ObjectLabelIdentifier.Texture, handle, name);
        public static void LabelBuffer(int handle, string name) => Label(ObjectLabelIdentifier.Buffer, handle, name);
        public static void LabelVertexArray(int handle, string name) => Label(ObjectLabelIdentifier.VertexArray, handle, name);
        public static void LabelFramebuffer(int handle, string name) => Label(ObjectLabelIdentifier.Framebuffer, handle, name);
        public static void LabelRenderbuffer(int handle, string name) => Label(ObjectLabelIdentifier.Renderbuffer, handle, name);
        public static void LabelQuery(int handle, string name) => Label(ObjectLabelIdentifier.Query, handle, name);
        public static void LabelSampler(int handle, string name) => Label(ObjectLabelIdentifier.Sampler, handle, name);

        public static void SetFilter(DebugSourceControl source, DebugTypeControl type,
                                     DebugSeverityControl severity, bool enabled)
        {
            if (!Enabled || !HasKHR) return;
            GL.DebugMessageControl(source, type, severity, 0, Array.Empty<int>(), enabled);
        }

        static bool HasExtension(string ext)
        {
            int n = GL.GetInteger(GetPName.NumExtensions);
            for (int i = 0; i < n; i++)
            {
                string s = GL.GetString(StringNameIndexed.Extensions, i);
                if (s == ext) return true;
            }
            return false;
        }

        struct Region : IDisposable
        {
            public void Dispose() => EndGroup();
        }
    }
}

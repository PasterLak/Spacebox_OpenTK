using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;

namespace Engine.Graphics.Rhi.OpenGL;

internal sealed class OpenGlDebugOutput
{
    private DebugProc? _callbackKeepAlive;
    private bool _hasKhr;
    private bool _hasExt;

    public bool Initialize(bool installCallback, bool synchronous)
    {
        int major = GL.GetInteger(GetPName.MajorVersion);
        int minor = GL.GetInteger(GetPName.MinorVersion);
        _hasKhr = major > 4 || major == 4 && minor >= 3 || HasExtension("GL_KHR_debug");
        _hasExt = HasExtension("GL_EXT_debug_marker");

        if (_hasKhr)
        {
            GL.Enable(EnableCap.DebugOutput);
            if (synchronous) GL.Enable(EnableCap.DebugOutputSynchronous);

            if (installCallback)
            {
                _callbackKeepAlive = (src, type, id, severity, length, message, userParam) =>
                {
                    string txt = Marshal.PtrToStringAnsi(message, length) ?? "";
                    System.Diagnostics.Debug.WriteLine($"[GL] {type} {severity} {id}: {txt}");
                };
                GL.DebugMessageCallback(_callbackKeepAlive, IntPtr.Zero);
                GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare,
                    DebugSeverityControl.DontCare, 0, Array.Empty<int>(), true);
            }
        }

        return _hasKhr || _hasExt;
    }

    public void PushGroup(string name)
    {
        if (_hasKhr) GL.PushDebugGroup(DebugSourceExternal.DebugSourceApplication, 0, name.Length, name);
        else if (_hasExt) GL.Ext.PushGroupMarker(0, name);
    }

    public void PopGroup()
    {
        if (_hasKhr) GL.PopDebugGroup();
        else if (_hasExt) GL.Ext.PopGroupMarker();
    }

    public void Marker(string name)
    {
        if (_hasKhr)
            GL.DebugMessageInsert(DebugSourceExternal.DebugSourceApplication, DebugType.DebugTypeMarker, 0,
                DebugSeverity.DebugSeverityNotification, name.Length, name);
        else if (_hasExt)
            GL.Ext.InsertEventMarker(0, name);
    }

    public void Label(DebugObjectKind kind, int id, string label)
    {
        if (!_hasKhr || id == 0) return;
        GL.ObjectLabel(GlEnumMap.ToGl(kind), id, label.Length, label);
    }

    private static bool HasExtension(string extension)
    {
        int count = GL.GetInteger(GetPName.NumExtensions);
        for (int i = 0; i < count; i++)
        {
            if (GL.GetString(StringNameIndexed.Extensions, i) == extension) return true;
        }
        return false;
    }
}

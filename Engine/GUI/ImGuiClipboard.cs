using System.Runtime.InteropServices;
using ImGuiNET;
using OpenTK.Windowing.Desktop;

namespace Engine.GUI;

public static class ImGuiClipboard
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetTextDelegate(IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetTextDelegate(IntPtr userData, IntPtr text);

    private static readonly GetTextDelegate GetText = OnGetText;
    private static readonly SetTextDelegate SetText = OnSetText;
    private static NativeWindow _window;
    private static IntPtr _buffer;

    public static void Install(ImGuiIOPtr io)
    {
        io.GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(GetText);
        io.SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(SetText);
        io.ClipboardUserData = IntPtr.Zero;
    }

    public static void SetWindow(NativeWindow window)
    {
        _window = window;
    }

    private static IntPtr OnGetText(IntPtr userData)
    {
        string text = "";
        try
        {
            text = _window?.ClipboardString ?? "";
        }
        catch (Exception ex)
        {
            Debug.Warning($"[ImGui] Could not read the clipboard: {ex.Message}");
        }

        if (_buffer != IntPtr.Zero) Marshal.FreeCoTaskMem(_buffer);
        _buffer = Marshal.StringToCoTaskMemUTF8(text);
        return _buffer;
    }

    private static void OnSetText(IntPtr userData, IntPtr text)
    {
        if (_window == null || text == IntPtr.Zero) return;

        try
        {
            _window.ClipboardString = Marshal.PtrToStringUTF8(text) ?? "";
        }
        catch (Exception ex)
        {
            Debug.Warning($"[ImGui] Could not write the clipboard: {ex.Message}");
        }
    }
}

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Engine.Audio;

[SupportedOSPlatform("windows")]
public sealed class WindowsDefaultOutputMonitor
{
    private const int Render = 0;
    private const int Multimedia = 1;

    private readonly IMMDeviceEnumerator _enumerator;
    private string _lastId;

    private WindowsDefaultOutputMonitor(IMMDeviceEnumerator enumerator)
    {
        _enumerator = enumerator;
        _lastId = DefaultId();
    }

    public static WindowsDefaultOutputMonitor TryCreate()
    {
        try
        {
            var type = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"));
            return type == null ? null : new WindowsDefaultOutputMonitor((IMMDeviceEnumerator)Activator.CreateInstance(type));
        }
        catch (Exception ex) when (ex is COMException || ex is InvalidCastException || ex is NotSupportedException)
        {
            Debug.Warning($"[Audio] Windows audio device notifications are not available: {ex.Message}");
            return null;
        }
    }

    public bool PollChanged()
    {
        string id = DefaultId();
        if (id == null || id == _lastId) return false;
        bool changed = _lastId != null;
        _lastId = id;
        return changed;
    }

    private string DefaultId()
    {
        try
        {
            if (_enumerator.GetDefaultAudioEndpoint(Render, Multimedia, out var device) != 0 || device == null) return null;
            return device.GetId(out var id) == 0 ? id : null;
        }
        catch (COMException)
        {
            return null;
        }
    }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, out IntPtr instance);
        [PreserveSig] int OpenPropertyStore(int access, out IntPtr store);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
    }
}

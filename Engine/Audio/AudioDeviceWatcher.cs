using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Audio.OpenAL;

namespace Engine.Audio;

public sealed unsafe class AudioDeviceWatcher
{
    private const string ReopenExtension = "ALC_SOFT_reopen_device";
    private const string DisconnectExtension = "ALC_EXT_disconnect";
    private const string SystemEventsExtension = "ALC_SOFT_system_events";
    private const AlcGetInteger Connected = (AlcGetInteger)0x313;
    private const int PlaybackDevice = 0x19D4;
    private const int DefaultDeviceChanged = 0x19D6;
    private const double CheckIntervalSeconds = 1.0;
    private const double RetryIntervalSeconds = 5.0;

    private static int _defaultChanged;

    private readonly ALDevice _device;
    private readonly delegate* unmanaged[Cdecl]<IntPtr, byte*, int*, byte> _reopen;
    private readonly bool _canDetectDisconnect;
    private readonly bool _systemEvents;
    private readonly WindowsDefaultOutputMonitor _windowsMonitor;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _nextCheck;

    public bool FollowsDefaultDevice { get; }

    private AudioDeviceWatcher(ALDevice device, IntPtr reopen)
    {
        _device = device;
        _reopen = (delegate* unmanaged[Cdecl]<IntPtr, byte*, int*, byte>)reopen;
        _canDetectDisconnect = ALC.IsExtensionPresent(device, DisconnectExtension);
        _systemEvents = SubscribeToDefaultDeviceChanges();
        if (!_systemEvents && OperatingSystem.IsWindows()) _windowsMonitor = WindowsDefaultOutputMonitor.TryCreate();
        FollowsDefaultDevice = _systemEvents || _windowsMonitor != null;
        _nextCheck = CheckIntervalSeconds;
    }

    public static AudioDeviceWatcher TryCreate(ALDevice device)
    {
        if (device == ALDevice.Null || !ALC.IsExtensionPresent(device, ReopenExtension)) return null;
        IntPtr reopen = ALC.GetProcAddress(device, "alcReopenDeviceSOFT");
        return reopen == IntPtr.Zero ? null : new AudioDeviceWatcher(device, reopen);
    }

    public void Update()
    {
        double now = _clock.Elapsed.TotalSeconds;
        if (now < _nextCheck) return;
        _nextCheck = now + CheckIntervalSeconds;

        bool defaultChanged = Interlocked.Exchange(ref _defaultChanged, 0) != 0 || DefaultChangedOnWindows();
        bool connected = !_canDetectDisconnect || ALC.GetInteger(_device, Connected) != 0;
        if (!AudioDeviceSwitchRules.ShouldReopen(defaultChanged, connected)) return;

        if (_reopen(_device.Handle, null, null) == 0)
        {
            _nextCheck = now + RetryIntervalSeconds;
            Debug.Warning("[Audio] Could not switch the sound output to the new default device");
            return;
        }

        Debug.Log($"[Audio] Sound output switched to '{ALC.GetString(_device, AlcGetString.AllDevicesSpecifier)}'");
    }

    private bool DefaultChangedOnWindows() => OperatingSystem.IsWindows() && _windowsMonitor != null && _windowsMonitor.PollChanged();

    private static bool SubscribeToDefaultDeviceChanges()
    {
        if (!ALC.IsExtensionPresent(ALDevice.Null, SystemEventsExtension)) return false;

        var control = (delegate* unmanaged[Cdecl]<int, int*, byte, byte>)ALC.GetProcAddress(ALDevice.Null, "alcEventControlSOFT");
        var setCallback = (delegate* unmanaged[Cdecl]<delegate* unmanaged[Cdecl]<int, int, IntPtr, int, byte*, IntPtr, void>, IntPtr, void>)ALC.GetProcAddress(ALDevice.Null, "alcEventCallbackSOFT");
        if (control == null || setCallback == null) return false;

        setCallback(&OnSystemEvent, IntPtr.Zero);
        int events = DefaultDeviceChanged;
        return control(1, &events, 1) != 0;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void OnSystemEvent(int eventType, int deviceType, IntPtr device, int length, byte* message, IntPtr userParam)
    {
        if (eventType == DefaultDeviceChanged && deviceType == PlaybackDevice) Interlocked.Exchange(ref _defaultChanged, 1);
    }
}

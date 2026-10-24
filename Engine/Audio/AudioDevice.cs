using OpenTK.Audio.OpenAL;


namespace Engine.Audio
{
    public class AudioDevice : IDisposable
    {
        private static AudioDevice instance = null;
        private static readonly object padlock = new object();

        public ALDevice Device { get; private set; }
        public ALContext Context { get; private set; }

        private bool isDisposed = false;
        private AudioDeviceWatcher _watcher;

        private static bool _unavailable;

        public static bool IsEnabled => !Application.IsHeadless && !_unavailable;

        AudioDevice()
        {
            if (!IsEnabled) return;

            try
            {
                Setup();
            }
            catch (Exception ex) when (ex is DllNotFoundException || ex is InvalidOperationException || ex is EntryPointNotFoundException)
            {
                _unavailable = true;
                Device = ALDevice.Null;
                Context = ALContext.Null;
                Debug.Warning($"[Audio] OpenAL is not available, the game runs without sound: {ex.Message}");
            }
        }

        public static AudioDevice Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new AudioDevice();
                    }
                    return instance;
                }
            }
        }

        private void Setup()
        {
            Device = ALC.OpenDevice(null);
            if (Device == ALDevice.Null)
            {
                throw new InvalidOperationException("Failed to open the default audio device.");
            }

            Context = ALC.CreateContext(Device, (int[])null);
            if (Context == ALContext.Null)
            {
                ALC.CloseDevice(Device);
                throw new InvalidOperationException("Failed to create an audio context.");
            }

            if (!ALC.MakeContextCurrent(Context))
            {
                ALC.DestroyContext(Context);
                ALC.CloseDevice(Device);
                throw new InvalidOperationException("Failed to make the context current.");
            }

            CheckALError("Initializing AudioManager");
            _watcher = AudioDeviceWatcher.TryCreate(Device);
            string switching = _watcher == null ? "no automatic switching" : _watcher.FollowsDefaultDevice ? "follows the system default" : "switches only when disconnected";
            Debug.Log($"[Audio] Output: {ALC.GetString(Device, AlcGetString.AllDevicesSpecifier)} ({switching})");
        }

        public void Update()
        {
            _watcher?.Update();
        }

        public static IReadOnlyList<string> OutputDevices()
        {
            if (!IsEnabled) return Array.Empty<string>();
            try
            {
                return ALC.GetString(ALDevice.Null, AlcGetStringList.AllDevicesSpecifier) ?? new List<string>();
            }
            catch (Exception ex) when (ex is DllNotFoundException || ex is EntryPointNotFoundException)
            {
                return Array.Empty<string>();
            }
        }

        public bool CanSwitchOutput => _watcher != null;

        public void SetPreferredOutput(string deviceName)
        {
            if (_watcher != null) _watcher.Preferred = deviceName;
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            if (Context == ALContext.Null) return;

            ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(Context);
            ALC.CloseDevice(Device);
        }

        private void CheckALError(string operation)
        {
            ALError error = AL.GetError();
            if (error != ALError.NoError)
            {
                throw new InvalidOperationException($"OpenAL error during {operation}: {AL.GetErrorString(error)}");
            }
        }
    }
}

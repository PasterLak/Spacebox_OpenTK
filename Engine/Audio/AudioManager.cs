using OpenTK.Audio.OpenAL;


namespace Engine.Audio
{
    public class AudioManager : IDisposable
    {
        private static AudioManager instance = null;
        private static readonly object padlock = new object();

        public ALDevice Device { get; private set; }
        public ALContext Context { get; private set; }

        private bool isDisposed = false;

        AudioManager()
        {
           
            Setup();
        }

        public static AudioManager Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new AudioManager();
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
        }

        public void Update()
        {
          
        }

        public void Dispose()
        {
            if (isDisposed) return;

            ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(Context);
            ALC.CloseDevice(Device);

            isDisposed = true;
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

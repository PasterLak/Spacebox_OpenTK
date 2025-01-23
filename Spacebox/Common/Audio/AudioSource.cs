
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;

namespace Spacebox.Common.Audio
{
    public class AudioSource : IDisposable
    {
        private readonly int handle;
        public readonly AudioClip Clip;
        private bool isDisposed = false;
        private bool isPlaying = false;
        private bool isPaused = false;
        private readonly object playLock = new object();
        private Thread playbackThread;

        public Vector3 Position = new Vector3(0,0,0);
        public bool IsPlaying => isPlaying;

        public bool IsLooped
        {
            get
            {
                AL.GetSource(handle, ALSourceb.Looping, out bool looped);
                return looped;
            }
            set
            {
                AL.Source(handle, ALSourceb.Looping, value);
                CheckALError("Setting looping");
            }
        }

        private float _volume = 1.0f;
        public float Volume
        {
            get => _volume;
            set
            {
                _volume = MathHelper.Clamp(value, 0f, 1f);
                AL.Source(handle, ALSourcef.Gain, _volume);
                CheckALError("Setting volume");
            }
        }

        public AudioSource(AudioClip clip)
        {
            this.Clip = clip;

            handle = AL.GenSource();

            if (!clip.IsStreaming)
            {
                AL.Source(handle, ALSourcei.Buffer, clip.Buffer);
                AL.Source(handle, ALSourcef.Gain, _volume);
                AL.Source(handle, ALSource3f.Position, Position.X, Position.Y, Position.Z);
                CheckALError("Initializing AudioSource");
            }
            else
            {
                AL.Source(handle, ALSourcef.Gain, _volume);
                AL.Source(handle, ALSource3f.Position, Position.X, Position.Y, Position.Z);
                CheckALError("Initializing streaming AudioSource");
            }

           // DisposablesUnloader.Add(this);
        }

        public void Play()
        {
            lock (playLock)
            {
                if (isDisposed) return;

                if (isPlaying)
                {
                    Stop();
                }

                if (!Clip.IsStreaming)
                {
                    AL.SourcePlay(handle);
                }
                else
                {
                    Clip.Stream(handle);
                    AL.SourcePlay(handle);
                }
                CheckALError("Playing");

                isPlaying = true;
                isPaused = false;
                //Console.WriteLine("Playback started.");

                if (playbackThread == null || !playbackThread.IsAlive)
                {
                    playbackThread = new Thread(MonitorPlayback)
                    {
                        IsBackground = true
                    };
                    playbackThread.Start();
                }
            }
        }

        public void Pause()
        {
            lock (playLock)
            {
                if (isDisposed) return;

                AL.SourcePause(handle);
                CheckALError("Pausing");

                isPaused = true;
                isPlaying = false;
               
            }
        }

        public void Stop()
        {
            lock (playLock)
            {
                if (isDisposed) return;

                AL.SourceStop(handle);
                CheckALError("Stopping");

                isPlaying = false;
                isPaused = false;
               // Console.WriteLine("Playback stopped.");
            }
        }

        public void Dispose()
        {
            lock (playLock)
            {
                if (isDisposed) return;

                isPlaying = false;
                isDisposed = true;

                if (playbackThread != null && playbackThread.IsAlive)
                {
                    playbackThread.Join();
                }

                AL.SourceStop(handle);
                AL.DeleteSource(handle);
              
            }
        }

        private void MonitorPlayback()
        {
            while (isPlaying && !isDisposed)
            {
                if (Clip.IsStreaming)
                {
                    Clip.Stream(handle);
                }

                AL.GetSource(handle, ALGetSourcei.SourceState, out int state);
                ALSourceState sourceState = (ALSourceState)state;

                if (sourceState == ALSourceState.Stopped)
                {
                    if (IsLooped && !isDisposed)
                    {
                        if (Clip.IsStreaming)
                        {
                            // Additional streaming initialization can be done here if needed
                        }

                        AL.SourceRewind(handle);
                        AL.SourcePlay(handle);
                    }
                    else
                    {
                        lock (playLock)
                        {
                            isPlaying = false;
                        }
                       // Console.WriteLine("Playback finished.");
                        break;
                    }
                }

                Thread.Sleep(100);
            }
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

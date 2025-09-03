using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;

namespace Engine.Audio
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
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public Vector3 Position = new Vector3(0, 0, 0);
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
            if (clip == null)
                Debug.Error("[AudioSource] AudioClip was null!");

            this.Clip = clip;
            clip.AudioSource = this;
           // Debug.Log("[AudioSource] Created AudioSource for clip: " + clip.Name);
            handle = AL.GenSource();
            //Debug.Log("[AudioSource] Generated OpenAL source with handle: " + handle);
            if (!clip.IsStreaming)
            {
                AL.Source(handle, ALSourcei.Buffer, clip.Buffer);
                AL.Source(handle, ALSourcef.Gain, _volume);
                AL.Source(handle, ALSource3f.Position, Position.X, Position.Y, Position.Z);
                //Debug.Error("[AudioSource] Bound buffer " + clip.Buffer + " to source " + handle);
                CheckALError("Initializing AudioSource");
            }
            else
            {
                AL.Source(handle, ALSourcef.Gain, _volume);
                AL.Source(handle, ALSource3f.Position, Position.X, Position.Y, Position.Z);
                //Debug.Error("[AudioSource] Prepared streaming AudioSource with handle: " + handle);
                CheckALError("Initializing streaming AudioSource");
            }
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


                if (playbackThread == null || !playbackThread.IsAlive)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                    playbackThread = new Thread(() => MonitorPlayback(cancellationTokenSource.Token))
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
            if (isDisposed) return;
            if (!isPlaying) return;

            lock (playLock)
            {
                try
                {
                    AL.SourceStop(handle);
                    CheckALError("Stopping");
                }
                catch (InvalidOperationException ex)
                {
                    Debug.Error("[AudioSource] Error stopping AudioSource (ignored): " + ex.Message);
                }
                isPlaying = false;
                isPaused = false;
            }
        }

        public void SetVolumeByDistance(float currentDistance, float maxDistance)
        {
            if (currentDistance <= 0)
            {
                Volume = 1f;
            }
            else
            {
                float minDistance = maxDistance * 0.1f;
                Volume = MathHelper.Clamp(minDistance / (minDistance + currentDistance), 0f, 1f);

            }
        }

        public void Dispose()
        {

            lock (playLock)
            {
                if (isDisposed) return;
                isPlaying = false;
                isDisposed = true;


                cancellationTokenSource.Cancel();
                playbackThread?.Join();

                AL.SourceStop(handle);
                AL.DeleteSource(handle);


                if (Clip != null)
                {
                    Clip.AudioSource = null;
                }

                GC.SuppressFinalize(this);
            }
        }

        private void MonitorPlayback(CancellationToken token)
        {
            while (!token.IsCancellationRequested && !isDisposed)
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
                throw new InvalidOperationException($"[AudioSource] OpenAL error during {operation}: {AL.GetErrorString(error)}");
            }
        }
    }
}

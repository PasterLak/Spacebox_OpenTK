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

        private Vector3 _position = Vector3.Zero;
        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                if (!isDisposed)
                {
                    AL.Source(handle, ALSource3f.Position, value.X, value.Y, value.Z);
                    CheckALError("Setting position");
                }
            }
        }
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

        private float _pitch = 1.0f;
        public float Pitch
        {
            get => _pitch;
            set
            {
                _pitch = MathHelper.Clamp(value, 0.5f, 2.0f);
                AL.Source(handle, ALSourcef.Pitch, _pitch);
                CheckALError("Setting pitch");
            }
        }

    
        public void Setup3D(float referenceDistance = 1.0f, float maxDistance = 100.0f, float rolloffFactor = 1.0f)
        {
            if (isDisposed) return;

            if (Clip.IsStereo())
            {
                Debug.Error("[AudioSource] The sound has 2 channels (stereo). It is recommended to use 1 channel (mono) for 3D sound! File:" + Clip.FileFullPath);
            }

           
            AL.Source(handle, ALSourceb.SourceRelative, false); 
            AL.Source(handle, ALSourcef.ReferenceDistance, referenceDistance);
            AL.Source(handle, ALSourcef.MaxDistance, maxDistance);
            AL.Source(handle, ALSourcef.RolloffFactor, rolloffFactor);
          

            CheckALError("Setting up 3D audio");


        }

  
        public void SetPitchByValue(float value, float minValue, float maxValue, float minPitch = 0.5f, float maxPitch = 2.0f)
        {
            if (minValue == maxValue) return;
            if (minValue > maxValue)
            {
                var m = maxValue;
                maxValue = minValue; 
                minValue = m;
            }

                float normalizedValue = MathHelper.Clamp((value - minValue) / (maxValue - minValue), 0f, 1f);
            float targetPitch = MathHelper.Lerp(minPitch, maxPitch, normalizedValue);
            Pitch = targetPitch;
        }

        public AudioSource(AudioClip clip)
        {
            if (clip == null)
                Debug.Error("[AudioSource] AudioClip was null!");

            this.Clip = clip;
            clip.AudioSource = this;
     
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
            return;
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

using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;

namespace Engine.Audio
{
    public class AudioSource : IDisposable
    {
        private readonly int handle;
        private AudioClip _clip;
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

        public AudioClip Clip
        {
            get => _clip;
            set => SetClip(value);
        }

        public bool HasClip => _clip != null;
        public bool IsPlaying => isPlaying && HasClip;

        public bool IsLooped
        {
            get
            {
                if (isDisposed) return false;
                AL.GetSource(handle, ALSourceb.Looping, out bool looped);
                return looped;
            }
            set
            {
                if (isDisposed) return;
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
                if (!isDisposed)
                {
                    AL.Source(handle, ALSourcef.Gain, _volume);
                    CheckALError("Setting volume");
                }
            }
        }

        private float _pitch = 1.0f;
        public float Pitch
        {
            get => _pitch;
            set
            {
                _pitch = MathHelper.Clamp(value, 0.5f, 2.0f);
                if (!isDisposed)
                {
                    AL.Source(handle, ALSourcef.Pitch, _pitch);
                    CheckALError("Setting pitch");
                }
            }
        }

        public AudioSource() : this(null) { }

        public AudioSource(AudioClip clip)
        {
            handle = AL.GenSource();
            AL.Source(handle, ALSourcef.Gain, _volume);
            AL.Source(handle, ALSource3f.Position, Position.X, Position.Y, Position.Z);
            CheckALError("Initializing AudioSource");

            SetClip(clip);
        }

        private void SetClip(AudioClip newClip)
        {
            if (newClip == null) return;

                lock (playLock)
            {
                if (isDisposed) return;

                bool wasPlaying = isPlaying;
                if (wasPlaying) Stop();

                if (_clip != null)
                    _clip.AudioSource = null;

                _clip = newClip;

                if (_clip != null)
                {
                    _clip.AudioSource = this;
                    if (!_clip.IsStreaming)
                    {
                        AL.Source(handle, ALSourcei.Buffer, _clip.Buffer);
                        CheckALError("Setting buffer");
                    }

                    if (wasPlaying) Play();
                }
                else
                {
                    AL.Source(handle, ALSourcei.Buffer, 0);
                    CheckALError("Clearing buffer");
                }
            }
        }

        public void Setup3D(float referenceDistance = 1.0f, float maxDistance = 100.0f, float rolloffFactor = 1.0f)
        {
            if (isDisposed) return;

            if (_clip != null && _clip.IsStereo())
            {
                Debug.Error("[AudioSource] The sound has 2 channels (stereo). It is recommended to use 1 channel (mono) for 3D sound! File:" + _clip.FileFullPath);
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
                var temp = maxValue;
                maxValue = minValue;
                minValue = temp;
            }

            float normalizedValue = MathHelper.Clamp((value - minValue) / (maxValue - minValue), 0f, 1f);
            float targetPitch = MathHelper.Lerp(minPitch, maxPitch, normalizedValue);
            Pitch = targetPitch;
        }

        public void Play()
        {
            lock (playLock)
            {
                if (isDisposed || _clip == null) return;

                if (isPlaying) Stop();

                if (!_clip.IsStreaming)
                {
                    AL.SourcePlay(handle);
                }
                else
                {
                    _clip.Stream(handle);
                    AL.SourcePlay(handle);
                }
                CheckALError("Playing");

                isPlaying = true;
                isPaused = false;

                if (playbackThread == null || !playbackThread.IsAlive)
                {
                    cancellationTokenSource?.Cancel();
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
                if (isDisposed || !isPlaying) return;
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
                if (isDisposed || !isPlaying) return;

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

        public void Dispose()
        {
            lock (playLock)
            {
                if (isDisposed) return;
                isPlaying = false;
                isDisposed = true;

                cancellationTokenSource?.Cancel();
                playbackThread?.Join();

                try
                {
                    AL.SourceStop(handle);
                    AL.DeleteSource(handle);
                }
                catch { }

                if (_clip != null)
                    _clip.AudioSource = null;

                cancellationTokenSource?.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        private void MonitorPlayback(CancellationToken token)
        {
            while (!token.IsCancellationRequested && !isDisposed)
            {
                lock (playLock)
                {
                    if (!isPlaying || _clip == null) break;

                    if (_clip.IsStreaming)
                        _clip.Stream(handle);

                    AL.GetSource(handle, ALGetSourcei.SourceState, out int state);
                    ALSourceState sourceState = (ALSourceState)state;

                    if (sourceState == ALSourceState.Stopped)
                    {
                        if (IsLooped && !isDisposed && _clip != null)
                        {
                            AL.SourceRewind(handle);
                            AL.SourcePlay(handle);
                        }
                        else
                        {
                            isPlaying = false;
                            break;
                        }
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
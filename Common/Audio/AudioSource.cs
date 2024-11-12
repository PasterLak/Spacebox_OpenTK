using System;
using System.Threading;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;

namespace Spacebox.Common.Audio
{
    public class AudioSource : IDisposable
    {
        private readonly int source;
        private readonly AudioClip clip;
        private bool isDisposed = false;
        private bool isPlaying = false;
        private bool isPaused = false;
        private readonly object playLock = new object();
        private Thread playbackThread;

        public bool IsPlaying => isPlaying;

        public bool IsLooped
        {
            get
            {
                AL.GetSource(source, ALSourceb.Looping, out bool looped);
                return looped;
            }
            set
            {
                AL.Source(source, ALSourceb.Looping, value);
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
                AL.Source(source, ALSourcef.Gain, _volume);
                CheckALError("Setting volume");
            }
        }

        public AudioSource(AudioClip clip)
        {
            this.clip = clip;

            source = AL.GenSource();

            if (!clip.IsStreaming)
            {
                AL.Source(source, ALSourcei.Buffer, clip.Buffer);
                AL.Source(source, ALSourcef.Gain, _volume);
                AL.Source(source, ALSource3f.Position, 0.0f, 0.0f, 0.0f);
                CheckALError("Initializing AudioSource");
            }
            else
            {
                AL.Source(source, ALSourcef.Gain, _volume);
                AL.Source(source, ALSource3f.Position, 0.0f, 0.0f, 0.0f);
                CheckALError("Initializing streaming AudioSource");
            }

            DisposablesUnloader.Add(this);
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

                if (!clip.IsStreaming)
                {
                    AL.SourcePlay(source);
                }
                else
                {
                    clip.Stream(source);
                    AL.SourcePlay(source);
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

                AL.SourcePause(source);
                CheckALError("Pausing");

                isPaused = true;
                isPlaying = false;
                //Console.WriteLine("Playback paused.");
            }
        }

        public void Stop()
        {
            lock (playLock)
            {
                if (isDisposed) return;

                AL.SourceStop(source);
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

                AL.SourceStop(source);
                AL.DeleteSource(source);
                //Console.WriteLine("AudioSource disposed.");
            }
        }

        private void MonitorPlayback()
        {
            while (isPlaying && !isDisposed)
            {
                if (clip.IsStreaming)
                {
                    clip.Stream(source);
                }

                AL.GetSource(source, ALGetSourcei.SourceState, out int state);
                ALSourceState sourceState = (ALSourceState)state;

                if (sourceState == ALSourceState.Stopped)
                {
                    if (IsLooped && !isDisposed)
                    {
                        if (clip.IsStreaming)
                        {
                            // Additional streaming initialization can be done here if needed
                        }

                        AL.SourceRewind(source);
                        AL.SourcePlay(source);
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

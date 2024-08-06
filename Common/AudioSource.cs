using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class AudioSource : IDisposable
    {
        private ALDevice device;
        private ALContext context;
        private int buffer;
        private int source;
        private bool isPaused = false;
        private bool isPlaying = false;

        public bool IsLooped = false;
        private float _volume = 1.0f;
        private Thread playbackThread;
        private bool isDisposed = false;
        private readonly object playLock = new object();

        public float Volume
        {
            get { return _volume; }
            set { _volume = MathHelper.Clamp(value, 0f, 1f); }
        }

        // Accept shared device and context
        public AudioSource(string filename, ALDevice sharedDevice, ALContext sharedContext)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException("Audio file was not found: " + filename);
            }

            device = sharedDevice;
            context = sharedContext;

            // Generate buffer and source
            buffer = AL.GenBuffer();
            source = AL.GenSource();

            int channels, bitsPerSample, sampleRate;
            byte[] soundData = LoadWave(File.Open(filename, FileMode.Open), out channels, out bitsPerSample, out sampleRate);
            ALFormat soundFormat = GetSoundFormat(channels, bitsPerSample);

            // Pin the sound data and get a pointer to it
            GCHandle handle = GCHandle.Alloc(soundData, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                AL.BufferData(buffer, soundFormat, pointer, soundData.Length, sampleRate);
                CheckALError();
            }
            finally
            {
                handle.Free();
            }

            AL.Source(source, ALSourcei.Buffer, buffer);
            AL.Source(source, ALSourcef.Gain, _volume);  // Set the gain (volume)
            AL.Source(source, ALSource3f.Position, 0.0f, 0.0f, 0.0f); // Set the position of the sound source
            CheckALError();
        }

        public void Play()
        {
            lock (playLock)
            {
                if (isPlaying)
                {
                    Stop();
                    Play();
                    return;
                }

                if (isPaused)
                {
                    AL.SourcePlay(source);
                    isPaused = false;
                }
                else
                {
                    AL.SourcePlay(source);
                }
                isPlaying = true;
                Console.WriteLine("Playing");
                CheckALError();

                // Start or resume playback monitoring
                if (playbackThread == null || !playbackThread.IsAlive)
                {
                    playbackThread = new Thread(MonitorPlayback)
                    {
                        IsBackground = true // Ensures the thread doesn't prevent application exit
                    };
                    playbackThread.Start();
                }
            }
        }

        public void Pause()
        {
            lock (playLock)
            {
                AL.SourcePause(source);
                isPaused = true;
                isPlaying = false;
                Console.WriteLine("Paused");
                CheckALError();
            }
        }

        public void Stop()
        {
            lock (playLock)
            {
                AL.SourceStop(source);
                isPlaying = false;
                isPaused = false;
                Console.WriteLine("Stopped");
                CheckALError();
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

                AL.DeleteSource(source);
                AL.DeleteBuffer(buffer);

                if (ALC.GetCurrentContext() == context)
                {
                    ALC.MakeContextCurrent(ALContext.Null);
                }

                ALC.DestroyContext(context);
                ALC.CloseDevice(device);

                Console.WriteLine("Disposed");
            }
        }

        private void MonitorPlayback()
        {
            while (isPlaying && !isDisposed)
            {
                // Check the state of the source
                AL.GetSource(source, ALGetSourcei.SourceState, out int state);
                ALSourceState sourceState = (ALSourceState)state;

                if (sourceState == ALSourceState.Stopped)
                {
                    if (IsLooped && !isDisposed)
                    {
                        // Restart playback if looping is enabled
                        AL.SourceRewind(source);
                        AL.SourcePlay(source);
                    }
                    else
                    {
                        // Stop monitoring if not looping
                        lock (playLock)
                        {
                            isPlaying = false;
                        }
                        Console.WriteLine("Playback finished");
                    }
                }

                // Sleep briefly to avoid busy waiting
                Thread.Sleep(100);
            }
        }

        private byte[] LoadWave(Stream stream, out int channels, out int bits, out int rate)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // Read the RIFF header
                string signature = new string(reader.ReadChars(4));
                if (signature != "RIFF")
                    throw new NotSupportedException("File is not a valid WAV file.");

                int riffChunkSize = reader.ReadInt32();

                string format = new string(reader.ReadChars(4));
                if (format != "WAVE")
                    throw new NotSupportedException("File is not a valid WAV file.");

                // Read fmt sub-chunk
                string fmtSignature = new string(reader.ReadChars(4));
                if (fmtSignature != "fmt ")
                    throw new NotSupportedException("WAV file format not supported.");

                int fmtChunkSize = reader.ReadInt32();
                int audioFormat = reader.ReadInt16();
                int numChannels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                int byteRate = reader.ReadInt32();
                int blockAlign = reader.ReadInt16();
                int bitsPerSample = reader.ReadInt16();

                // Read data sub-chunk
                string dataSignature = new string(reader.ReadChars(4));
                if (dataSignature != "data")
                    throw new NotSupportedException("WAV file format not supported.");

                int dataChunkSize = reader.ReadInt32();

                channels = numChannels;
                bits = bitsPerSample;
                rate = sampleRate;

                return reader.ReadBytes(dataChunkSize);
            }
        }

        private ALFormat GetSoundFormat(int channels, int bits)
        {
            return (channels, bits) switch
            {
                (1, 8) => ALFormat.Mono8,
                (1, 16) => ALFormat.Mono16,
                (2, 8) => ALFormat.Stereo8,
                (2, 16) => ALFormat.Stereo16,
                _ => throw new NotSupportedException("WAV file format not supported.")
            };
        }

        private void CheckALError()
        {
            ALError error = AL.GetError();
            if (error != ALError.NoError)
            {
                throw new InvalidOperationException($"OpenAL error: {AL.GetErrorString(error)}");
            }
        }
    }
}

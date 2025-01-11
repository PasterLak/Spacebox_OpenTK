using System.Runtime.InteropServices;
using OpenTK.Audio.OpenAL;

namespace Spacebox.Common.Audio
{
    public class AudioClip : IDisposable
    {
        public string Name { get; private set; }
        public string FileFullPath { get; private set; }
        public bool IsStreaming { get; private set; }
        public int Buffer { get; private set; }

        private bool isDisposed = false;
      

        private FileStream fileStream;
        private BinaryReader reader;
        private readonly int bufferSize = 4096;
        private readonly int bufferCount = 4;
        private int[] buffers;
        private bool isStreamFinished = false;
        private int sampleRate;

        public AudioClip(string filename, AudioLoadMode loadMode = AudioLoadMode.LoadIntoMemory)
        {
        
            string resolvedPath = AudioPathResolver.ResolvePath(filename, AppDomain.CurrentDomain.BaseDirectory, SoundManager.AllowedExtensions);
            if (resolvedPath == null)
            {
                throw new FileNotFoundException($"Audio file for '{filename}' not found.");
            }
            FileFullPath = resolvedPath;
            Name = Path.GetFileNameWithoutExtension(resolvedPath);
            IsStreaming = loadMode == AudioLoadMode.Stream;

            if (!IsStreaming)
            {
                Buffer = SoundLoader.LoadSound(FileFullPath, out sampleRate);
            }
            else
            {
                InitializeStreaming();
            }

          
        }


        private void InitializeStreaming()
        {
            var (data, channels, bitsPerSample, sr) = SoundLoader.LoadWave(FileFullPath);
            sampleRate = sr;

            fileStream = File.OpenRead(FileFullPath);
            reader = new BinaryReader(fileStream);

            reader.ReadBytes(44);

            buffers = AL.GenBuffers(bufferCount);
            CheckALError("Generating streaming buffers");

            for (int i = 0; i < bufferCount; i++)
            {
                FillBuffer(buffers[i]);
            }
        }

        private void FillBuffer(int buffer)
        {
            byte[] data = reader.ReadBytes(bufferSize);
            if (data.Length == 0)
            {
                isStreamFinished = true;
                return;
            }

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                IntPtr dataPtr = handle.AddrOfPinnedObject();
                AL.BufferData(buffer, ALFormat.Mono16, dataPtr, data.Length, sampleRate);
            }
            finally
            {
                handle.Free();
            }

            CheckALError("Filling streaming buffer");
        }

        public void Stream(int source)
        {
            AL.GetSource(source, ALGetSourcei.BuffersProcessed, out int processed);
            while (processed-- > 0)
            {
                int buffer = AL.SourceUnqueueBuffer(source);
                if (buffer == 0)
                    break;

                FillBuffer(buffer);

                if (!isStreamFinished)
                {
                    AL.SourceQueueBuffer(source, buffer);
                }
            }

            CheckALError("Streaming");
        }

        public void Dispose()
        {
            if (isDisposed) return;

            if (!IsStreaming)
            {
                SoundManager.RemoveClip(FileFullPath);
            }
            else
            {
                reader?.Dispose();
                fileStream?.Dispose();
                if (buffers != null)
                {
                    AL.DeleteBuffers(buffers);
                    CheckALError("Deleting streaming buffers");
                }
            }

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

using OpenTK.Audio.OpenAL;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Engine.Audio
{
    public class AudioClip : IResource
    {
        public string Name { get; private set; }
        public string FileFullPath { get; private set; }
        public bool IsStreaming { get; private set; }
        public int Buffer { get; private set; }
        private int _monoBuffer;
        private bool? _isStereo;
        private readonly HashSet<AudioSource> _sources = new HashSet<AudioSource>();
        private readonly List<AudioSource> _sourcesToDispose = new List<AudioSource>();

        internal void AddSource(AudioSource source)
        {
            lock (_sources) _sources.Add(source);
        }

        internal void RemoveSource(AudioSource source)
        {
            lock (_sources) _sources.Remove(source);
        }
      
        private static readonly List<string> AllowedExtensions = new List<string> { ".wav", ".ogg" };
        private FileStream fileStream;
        private BinaryReader reader;
        private readonly int bufferSize = 4096;
        private readonly int bufferCount = 4;
        private int[] buffers;
        private bool isStreamFinished = false;
        private int sampleRate;
        public bool IsDisposed { get; set; }
        public AudioClip()
        {

        }
        public AudioClip(string filename, AudioLoadMode loadMode = AudioLoadMode.LoadIntoMemory)
        {
            ResourceScopes.Track(this);
            IsStreaming = loadMode == AudioLoadMode.Stream;
            if (!AudioDevice.IsEnabled)
            {
                FileFullPath = filename;
                Name = Path.GetFileNameWithoutExtension(filename);
                return;
            }

            string resolvedPath = AudioPathResolver.ResolvePath(filename, AppDomain.CurrentDomain.BaseDirectory, AllowedExtensions);
            if (resolvedPath == null)
            {
                throw new FileNotFoundException($"[AudioClip] Audio file for '{filename}' not found.");
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
                // InitializeStreaming();
            }


        }

        public int GetSpatialBuffer()
        {
            if (Buffer == 0 || !IsStereo()) return Buffer;
            if (_monoBuffer == 0) _monoBuffer = SoundLoader.LoadSound(FileFullPath, out _, true);
            return _monoBuffer;
        }

        public bool IsStereo()
        {
            _isStereo ??= ReadIsStereo();
            return _isStereo.Value;
        }

        private bool ReadIsStereo()
        {
            if (Buffer == 0) return false;

            try
            {
                AL.GetBuffer(Buffer, ALGetBufferi.Channels, out int channels);
                AL.GetBuffer(Buffer, ALGetBufferi.Frequency, out int frequency);

                if (channels > 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.Error("[AudioClip] Error getting channels and frequency: " + FileFullPath);
            }
            return false;
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

            if (IsDisposed || Resources.BlocksDispose(this)) return;

            lock (_sources)
            {
                _sourcesToDispose.AddRange(_sources);
                _sources.Clear();
            }

            for (int i = 0; i < _sourcesToDispose.Count; i++)
            {
                _sourcesToDispose[i].Dispose();
            }
            _sourcesToDispose.Clear();
            ResourceScopes.Forget(this);


            reader?.Dispose();
            fileStream?.Dispose();
            if (buffers != null)
            {
                AL.DeleteBuffers(buffers);
                CheckALError("Deleting streaming buffers");
            }
            if (Buffer != 0) AL.DeleteBuffer(Buffer);
            if (_monoBuffer != 0) AL.DeleteBuffer(_monoBuffer);

            IsDisposed = true;
        }

        private void CheckALError(string operation)
        {
            OpenAlErrors.Check("AudioClip", operation);
        }

        public string GetResourceInfo()
        {
            return $"AudioClip: {Name}, Path: {FileFullPath}, Streaming: {IsStreaming}";
        }

        public int GetHandle() { return Buffer; }

        public IResource Load(string path)
        {
            return new AudioClip(path);
        }
    }
}

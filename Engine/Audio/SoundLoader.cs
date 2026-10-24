
using System.Runtime.InteropServices;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;

namespace Engine.Audio
{
    public static class SoundLoader
    {
        public static (byte[] data, int channels, int bitsPerSample, int sampleRate) LoadWave(string filename)
        {
            using (var reader = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                string signature = new string(reader.ReadChars(4));
                if (signature != "RIFF")
                    throw new NotSupportedException("[SoundLoader] File is not a valid WAV file.");

                int riffChunkSize = reader.ReadInt32();

                string format = new string(reader.ReadChars(4));
                if (format != "WAVE")
                    throw new NotSupportedException("[SoundLoader] File is not a valid WAVE file.");

                string fmtSignature = new string(reader.ReadChars(4));
                if (fmtSignature != "fmt ")
                    throw new NotSupportedException("[SoundLoader] WAV file format not supported: missing fmt subchunk.");

                int fmtChunkSize = reader.ReadInt32();
                int audioFormat = reader.ReadInt16();
                int numChannels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                int byteRate = reader.ReadInt32();
                int blockAlign = reader.ReadInt16();
                int bitsPerSample = reader.ReadInt16();

                if (fmtChunkSize > 16)
                {
                    reader.ReadBytes(fmtChunkSize - 16);
                }

                string dataSignature = new string(reader.ReadChars(4));
                if (dataSignature != "data")
                    throw new NotSupportedException("[SoundLoader] WAV file format not supported: missing data subchunk.");

                int dataChunkSize = reader.ReadInt32();

                byte[] data = reader.ReadBytes(dataChunkSize);

                return (data, numChannels, bitsPerSample, sampleRate);
            }
        }

        public static ALFormat GetSoundFormat(int channels, int bits)
        {
            return (channels, bits) switch
            {
                (1, 8) => ALFormat.Mono8,
                (1, 16) => ALFormat.Mono16,
                (2, 8) => ALFormat.Stereo8,
                (2, 16) => ALFormat.Stereo16,
                _ => throw new NotSupportedException("[SoundLoader] WAV file has an unsupported format.")
            };
        }

        public static int LoadSound(string path, out int sampleRate, bool forceMono = false)
        {

            var (data, channels, bitsPerSample, sr) = AudioFormatLoader.LoadAudio(path);

            sampleRate = sr;
            if (forceMono && channels == 2)
            {
                data = PcmDownmix.StereoToMono(data, bitsPerSample);
                channels = 1;
            }
            ALFormat format = GetSoundFormat(channels, bitsPerSample);
            int length = PcmLayout.AlignedLength(data.Length, channels, bitsPerSample);
            if (length <= 0)
            {
                Debug.Error($"[SoundLoader] Sound {path} has no samples");
                return 0;
            }

            OpenAlErrors.Clear("SoundLoader", $"loading {path}");
            int buffer = AL.GenBuffer();

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);

            try
            {
                IntPtr dataPtr = handle.AddrOfPinnedObject();
                AL.BufferData(buffer, format, dataPtr, length, sampleRate);

            }
            catch (Exception ex)
            {
                AL.DeleteBuffer(buffer);
                buffer = 0;
                Debug.Error($"[SoundLoader] Failed to load sound {path}: {ex.Message}");
                //throw new InvalidOperationException($"[SoundLoader] Failed to load sound {path}: {ex.Message}");
                
            }
            finally
            {
                handle.Free();

            }

            if (buffer == 0) return 0;
            if (!OpenAlErrors.Check("SoundLoader", $"loading sound {path}"))
            {
                AL.DeleteBuffer(buffer);
                return 0;
            }

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Debug.Log($"[Audio][{buffer}] Loaded! - {TrimToRelativePath(path, baseDirectory)}", Color4.Turquoise);

            return buffer;
        }

        public static string TrimToRelativePath(string fullPath, string baseDirectory)
        {
            if (string.IsNullOrEmpty(fullPath)) return fullPath;
            if (string.IsNullOrEmpty(baseDirectory)) return Path.GetFileName(fullPath);

            string normalizedFullPath = Path.GetFullPath(fullPath);
            string normalizedBaseDir = Path.GetFullPath(baseDirectory);

            if (!normalizedFullPath.StartsWith(normalizedBaseDir))
                return Path.GetFileName(fullPath);

            return normalizedFullPath.Substring(normalizedBaseDir.Length).TrimStart(Path.DirectorySeparatorChar);
        }

    }
}

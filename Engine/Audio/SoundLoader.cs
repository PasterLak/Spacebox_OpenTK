using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK.Audio.OpenAL;

namespace Engine.Audio
{
    public static class SoundLoader
    {
        public static (byte[] data, int channels, int bitsPerSample, int sampleRate) LoadWave(string filename)
        {
            using (var reader = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                string signature = new string(reader.ReadChars(4));
                if (signature != "RIFF")
                    throw new NotSupportedException("File is not a valid WAV file.");

                int riffChunkSize = reader.ReadInt32();

                string format = new string(reader.ReadChars(4));
                if (format != "WAVE")
                    throw new NotSupportedException("File is not a valid WAVE file.");

                string fmtSignature = new string(reader.ReadChars(4));
                if (fmtSignature != "fmt ")
                    throw new NotSupportedException("WAV file format not supported: missing fmt subchunk.");

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
                    throw new NotSupportedException("WAV file format not supported: missing data subchunk.");

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
                _ => throw new NotSupportedException("WAV file has an unsupported format.")
            };
        }

        public static int LoadSound(string path, out int sampleRate)
        {

            var (data, channels, bitsPerSample, sr) = AudioFormatLoader.LoadAudio(path);

            sampleRate = sr;
            ALFormat format = GetSoundFormat(channels, bitsPerSample);

            int buffer = AL.GenBuffer();

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);

            try
            {
                IntPtr dataPtr = handle.AddrOfPinnedObject();
                AL.BufferData(buffer, format, dataPtr, data.Length, sampleRate);

            }
            finally
            {
                handle.Free();

            }

            CheckALError($"Loading sound {path}");

            return buffer;
        }

        private static void CheckALError(string operation)
        {
            ALError error = AL.GetError();
            if (error != ALError.NoError)
            {
                throw new InvalidOperationException($"OpenAL error during {operation}: {AL.GetErrorString(error)}");
            }
        }
    }
}

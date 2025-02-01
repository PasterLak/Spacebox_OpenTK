
using NVorbis;

namespace Spacebox.Engine.Audio
{
    public class OggLoader : IAudioLoader
    {
        public (byte[] data, int channels, int bitsPerSample, int sampleRate) Load(string filename)
        {
            using var vorbis = new VorbisReader(filename);
            var floatBuffer = new float[4096];
            var shortList = new List<short>();
            while (true)
            {
                int samplesRead = vorbis.ReadSamples(floatBuffer, 0, floatBuffer.Length);
                if (samplesRead == 0) break;
                for (int i = 0; i < samplesRead; i++)
                {
                    float f = floatBuffer[i];
                    short s = (short)Math.Clamp((int)(f * 32767f), -32768, 32767);
                    shortList.Add(s);
                }
            }
            byte[] data = new byte[shortList.Count * 2];
            Buffer.BlockCopy(shortList.ToArray(), 0, data, 0, data.Length);
            return (data, vorbis.Channels, 16, vorbis.SampleRate);
        }
    }
}

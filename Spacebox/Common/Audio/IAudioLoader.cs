
namespace Spacebox.Common.Audio
{
    public interface IAudioLoader
    {
         (byte[] data, int channels, int bitsPerSample, int sampleRate) Load(string filename);
    }
}

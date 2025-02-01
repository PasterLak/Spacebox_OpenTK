
namespace Spacebox.Engine.Audio
{
    public static class AudioFormatLoader
    {
        public static (byte[] data, int channels, int bitsPerSample, int sampleRate) LoadAudio(string filename)
        {
            string ext = Path.GetExtension(filename).ToLower();
            IAudioLoader loader = ext switch
            {
                ".wav" => new WaveLoader(),
                ".ogg" => new OggLoader(),
                _ => throw new NotSupportedException("Unsupported audio format.")
            };
            return loader.Load(filename);
        }
    }
}

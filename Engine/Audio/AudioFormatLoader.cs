
namespace Engine.Audio
{
    public static class AudioFormatLoader
    {
        public static (byte[] data, int channels, int bitsPerSample, int sampleRate) LoadAudio(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
           
            IAudioLoader loader = ext switch
            {
                ".wav" => new WaveLoader(),
                ".ogg" => new OggLoader(),
                _ => throw new NotSupportedException("[AudioFormatLoader] Unsupported audio format! FileName: " + path)
            };
            return loader.Load(path);
        }
    }
}

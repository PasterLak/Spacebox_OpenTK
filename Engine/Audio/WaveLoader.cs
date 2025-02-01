
namespace Engine.Audio
{
    public class WaveLoader : IAudioLoader
    {
        public  (byte[] data, int channels, int bitsPerSample, int sampleRate) Load(string filename)
        {
            using var reader = new BinaryReader(File.Open(filename, FileMode.Open));
            var sig = new string(reader.ReadChars(4));
            if (sig != "RIFF") throw new NotSupportedException("Not a valid WAV file.");
            reader.ReadInt32();
            var fmt = new string(reader.ReadChars(4));
            if (fmt != "WAVE") throw new NotSupportedException("Not a valid WAVE file.");
            var fmtSig = new string(reader.ReadChars(4));
            if (fmtSig != "fmt ") throw new NotSupportedException("Missing fmt subchunk.");
            var fmtSize = reader.ReadInt32();
            reader.ReadInt16();
            var chans = reader.ReadInt16();
            var rate = reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt16();
            var bits = reader.ReadInt16();
            if (fmtSize > 16) reader.ReadBytes(fmtSize - 16);
            var dataSig = new string(reader.ReadChars(4));
            if (dataSig != "data") throw new NotSupportedException("Missing data subchunk.");
            var dataSize = reader.ReadInt32();
            var data = reader.ReadBytes(dataSize);
            return (data, chans, bits, rate);
        }
    }
}

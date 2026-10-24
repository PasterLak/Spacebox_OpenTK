namespace Engine.Audio;

public static class PcmLayout
{
    public static int AlignedLength(int length, int channels, int bitsPerSample)
    {
        int frame = Math.Max(1, channels * bitsPerSample / 8);
        return length - length % frame;
    }
}

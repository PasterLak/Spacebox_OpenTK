namespace Engine.Audio;

public static class PcmDownmix
{
    public static byte[] StereoToMono(byte[] data, int bitsPerSample)
    {
        if (data == null) return Array.Empty<byte>();

        return bitsPerSample switch
        {
            8 => StereoToMono8(data),
            16 => StereoToMono16(data),
            _ => throw new NotSupportedException($"[PcmDownmix] {bitsPerSample}-bit audio is not supported.")
        };
    }

    private static byte[] StereoToMono8(byte[] data)
    {
        int frames = data.Length / 2;
        var mono = new byte[frames];
        for (int i = 0; i < frames; i++)
            mono[i] = (byte)((data[i * 2] + data[i * 2 + 1] + 1) / 2);
        return mono;
    }

    private static byte[] StereoToMono16(byte[] data)
    {
        int frames = data.Length / 4;
        var mono = new byte[frames * 2];
        for (int i = 0; i < frames; i++)
        {
            int left = (short)(data[i * 4] | (data[i * 4 + 1] << 8));
            int right = (short)(data[i * 4 + 2] | (data[i * 4 + 3] << 8));
            short mixed = (short)((left + right) / 2);
            mono[i * 2] = (byte)mixed;
            mono[i * 2 + 1] = (byte)(mixed >> 8);
        }
        return mono;
    }
}

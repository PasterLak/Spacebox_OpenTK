using System.IO.Compression;
using SharpNBT;

namespace Spacebox.Network;

public static class NbtSerializer
{
    public static byte[] ToBytes(CompoundTag tag)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, true))
        using (var writer = new TagWriter(gzip, FormatOptions.Java))
        {
            writer.WriteTag(tag);
        }
        return output.ToArray();
    }

    public static CompoundTag FromBytes(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new TagReader(gzip, FormatOptions.Java);
        return reader.ReadTag<CompoundTag>();
    }
}

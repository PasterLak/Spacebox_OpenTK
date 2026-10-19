using Engine;
using Engine.Networking.Query;

namespace Spacebox.Network.Discovery;

public static class ServerIconFile
{
    public const int MaxBytes = ServerQueryProtocol.MaxBlobSize;

    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public static bool IsPng(byte[] data)
    {
        if (data == null || data.Length < PngSignature.Length) return false;

        for (int i = 0; i < PngSignature.Length; i++)
        {
            if (data[i] != PngSignature[i]) return false;
        }
        return true;
    }

    public static bool IsValid(byte[] data)
    {
        return IsPng(data) && data.Length <= MaxBytes;
    }

    public static byte[] TryRead(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (!Path.IsPathRooted(path)) path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        if (!File.Exists(path)) return null;

        try
        {
            var data = File.ReadAllBytes(path);
            if (IsValid(data)) return data;

            Debug.Warning($"[Server] Icon '{path}' must be a PNG up to {MaxBytes / 1024} KB, it is ignored");
        }
        catch (Exception ex)
        {
            Debug.Error($"[Server] Failed to read icon '{path}': {ex.Message}");
        }

        return null;
    }
}

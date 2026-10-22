using Engine;
using Engine.Networking.Query;

namespace Spacebox.Network.Discovery;

public static class ServerIconFile
{
    public const int MaxBytes = ServerQueryProtocol.MaxBlobSize;

    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };
    private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg" };

    public static bool IsPng(byte[] data)
    {
        return StartsWith(data, PngSignature);
    }

    public static bool IsJpeg(byte[] data)
    {
        return StartsWith(data, JpegSignature);
    }

    public static bool IsValid(byte[] data)
    {
        return (IsPng(data) || IsJpeg(data)) && data.Length <= MaxBytes;
    }

    public static string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        path = MultiplayerFolders.ResolveConfigPath(path);
        if (File.Exists(path)) return path;

        foreach (var extension in Extensions)
        {
            var candidate = Path.ChangeExtension(path, extension);
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    public static byte[] TryRead(string path)
    {
        path = ResolvePath(path);
        if (path == null) return null;

        try
        {
            var data = File.ReadAllBytes(path);
            if (IsValid(data)) return data;

            Debug.Warning($"[Server] Icon '{path}' must be a PNG or JPEG up to {MaxBytes / 1024} KB, it is ignored");
        }
        catch (Exception ex)
        {
            Debug.Error($"[Server] Failed to read icon '{path}': {ex.Message}");
        }

        return null;
    }

    private static bool StartsWith(byte[] data, byte[] signature)
    {
        if (data == null || data.Length < signature.Length) return false;

        for (int i = 0; i < signature.Length; i++)
        {
            if (data[i] != signature[i]) return false;
        }
        return true;
    }
}

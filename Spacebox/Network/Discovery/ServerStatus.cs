using Engine.Networking;
using Engine.Networking.Query;

namespace Spacebox.Network.Discovery;

public sealed class ServerStatus
{
    public const byte FormatVersion = 1;
    public const int MaxNameLength = 64;
    public const int MaxDescriptionLength = 600;

    public long InstanceId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string GameVersion { get; set; } = "";
    public string ModId { get; set; } = "";
    public string WorldName { get; set; } = "";
    public byte GameMode { get; set; }
    public int Players { get; set; }
    public int MaxPlayers { get; set; }
    public bool IsDedicated { get; set; }

    public byte[] Serialize()
    {
        string description = Truncate(Description, MaxDescriptionLength);
        while (true)
        {
            var bytes = Write(description);
            if (bytes.Length <= ServerQueryProtocol.MaxInfoSize || description.Length == 0) return bytes;
            description = description.Substring(0, description.Length / 2);
        }
    }

    public static bool TryDeserialize(byte[] data, out ServerStatus status)
    {
        status = null;
        if (data == null || data.Length == 0) return false;

        try
        {
            var reader = new NetReader(data);
            if (reader.ReadByte() != FormatVersion) return false;

            status = new ServerStatus
            {
                InstanceId = reader.ReadLong(),
                Name = reader.ReadString() ?? "",
                Description = reader.ReadString() ?? "",
                GameVersion = reader.ReadString() ?? "",
                ModId = reader.ReadString() ?? "",
                WorldName = reader.ReadString() ?? "",
                GameMode = reader.ReadByte(),
                Players = reader.ReadInt(),
                MaxPlayers = reader.ReadInt(),
                IsDedicated = reader.ReadBool()
            };
            return true;
        }
        catch (Exception)
        {
            status = null;
            return false;
        }
    }

    private byte[] Write(string description)
    {
        var writer = new NetWriter(256);
        writer.Write(FormatVersion);
        writer.Write(InstanceId);
        writer.Write(Truncate(Name, MaxNameLength));
        writer.Write(description);
        writer.Write(GameVersion ?? "");
        writer.Write(ModId ?? "");
        writer.Write(Truncate(WorldName, MaxNameLength));
        writer.Write(GameMode);
        writer.Write(Players);
        writer.Write(MaxPlayers);
        writer.Write(IsDedicated);
        return writer.ToArray();
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}

using Engine.Networking;
using Spacebox.Scenes;

namespace Spacebox.Network;

public enum SessionMode : byte
{
    Singleplayer,
    Host,
    Client,
    Dedicated
}

public static class GameSession
{
    public static SessionMode Mode { get; private set; } = SessionMode.Singleplayer;
    public static string PlayerName { get; private set; } = "Player";
    public static string SkinColor { get; private set; } = "Yellow";
    public static string HostAddress { get; private set; } = "127.0.0.1";
    public static int Port { get; private set; } = NetworkManager.DefaultPort;
    public static RemoteWorldInfo RemoteWorld { get; private set; }
    public static string PendingPlayerJson { get; set; }

    public static bool IsRemoteClient => Mode == SessionMode.Client;
    public static bool IsDedicated => Mode == SessionMode.Dedicated;
    public static bool HasLocalPlayer => Mode != SessionMode.Dedicated;

    public static void SetPlayer(string name, string skinColor)
    {
        PlayerName = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim();
        SkinColor = string.IsNullOrWhiteSpace(skinColor) ? "Yellow" : skinColor;
    }

    public static void BeginSingleplayer()
    {
        Mode = SessionMode.Singleplayer;
        RemoteWorld = null;
    }

    public static void BeginHost(int port)
    {
        Mode = SessionMode.Host;
        Port = port;
        RemoteWorld = null;
    }

    public static void BeginClient(string address, int port)
    {
        Mode = SessionMode.Client;
        HostAddress = string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address.Trim();
        Port = port;
        RemoteWorld = null;
    }

    public static void BeginDedicated(int port)
    {
        Mode = SessionMode.Dedicated;
        Port = port;
        RemoteWorld = null;
    }

    public static void SetRemoteWorld(RemoteWorldInfo info)
    {
        RemoteWorld = info;
    }
}

public sealed class RemoteWorldInfo
{
    public string ServerName;
    public string WorldName;
    public string Seed;
    public string ModId;
    public byte GameMode;
    public int Day;
    public int Ticks;
    public string GameSetHash;
    public string[] Extensions = Array.Empty<string>();
    public string[] TexturePacks = Array.Empty<string>();
    public string[] TexturePackFolders = Array.Empty<string>();
    public string GameSetFolder;
    public string GameSetCacheName;
    public bool UseLocalGameSet = true;
    public float[] SpawnPoint;

    public SpaceSceneArgs ToSceneArgs(string skinColor, string nickname)
    {
        return new SpaceSceneArgs
        {
            worldName = WorldName,
            modId = ModId,
            extensions = Extensions ?? Array.Empty<string>(),
            serverTexturePacks = TexturePackFolders ?? Array.Empty<string>(),
            seed = Seed,
            modfolderName = string.IsNullOrEmpty(GameSetFolder) ? GameSetLocator.FindFolder(ModId) : GameSetFolder,
            SearchForGameSetInLocalFolder = UseLocalGameSet,
            gameSetServerFolder = GameSetCacheName ?? "",
            skinColor = skinColor,
            nickname = nickname
        };
    }
}

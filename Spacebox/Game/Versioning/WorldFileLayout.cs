using Spacebox.Game.Generation.Tools;

namespace Spacebox.Game.Versioning;

public static class WorldFileLayout
{
    public const string WorldFile = "world.json";
    public const string LocalPlayerFile = "localplayer.json";
    public const string LegacyPlayerFile = "player.json";
    public const string PlayersFolder = "players";
    public const string DropsFile = "drop.json";
    public const string TagsFile = "tags.json";
    public const string SectorsFolder = "Sectors";
    public const string SectorExtension = ".sector";

    public static IEnumerable<string> PlayerFiles(string worldFolder)
    {
        if (!Directory.Exists(worldFolder)) yield break;

        foreach (var name in new[] { LocalPlayerFile, LegacyPlayerFile })
        {
            string path = Path.Combine(worldFolder, name);
            if (File.Exists(path)) yield return path;
        }

        string players = Path.Combine(worldFolder, PlayersFolder);
        if (!Directory.Exists(players)) yield break;

        foreach (var file in Directory.GetFiles(players, "*.json")) yield return file;
    }

    public static string DropsPath(string worldFolder)
    {
        return Path.Combine(worldFolder, DropsFile);
    }

    public static IEnumerable<string> SectorFiles(string worldFolder)
    {
        return FilesInSectors(worldFolder, "*" + SectorExtension);
    }

    public static IEnumerable<string> EntityFilePaths(string worldFolder)
    {
        foreach (var file in FilesInSectors(worldFolder, "*" + EntityFiles.Extension)) yield return file;

        string entities = EntityFiles.FolderOf(worldFolder ?? "");
        if (!Directory.Exists(entities)) yield break;

        foreach (var file in Directory.GetFiles(entities, "*" + EntityFiles.Extension)) yield return file;
    }

    private static IEnumerable<string> FilesInSectors(string worldFolder, string pattern)
    {
        string sectors = Path.Combine(worldFolder ?? "", SectorsFolder);
        return Directory.Exists(sectors) ? Directory.GetFiles(sectors, pattern, SearchOption.AllDirectories) : Array.Empty<string>();
    }
}

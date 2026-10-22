using System.Text.RegularExpressions;
using OpenTK.Mathematics;

namespace Spacebox.Game.Versioning;

public static class SectorFolderName
{
    private static readonly Regex Pattern = new Regex(@"^Sector([+-]\d+)([+-]\d+)([+-]\d+)$", RegexOptions.CultureInvariant);

    public static bool TryParse(string folderName, out Vector3i index)
    {
        index = default;
        var match = Pattern.Match(folderName ?? "");
        if (!match.Success
            || !int.TryParse(match.Groups[1].Value, out int x)
            || !int.TryParse(match.Groups[2].Value, out int y)
            || !int.TryParse(match.Groups[3].Value, out int z))
            return false;

        index = new Vector3i(x, y, z);
        return true;
    }
}

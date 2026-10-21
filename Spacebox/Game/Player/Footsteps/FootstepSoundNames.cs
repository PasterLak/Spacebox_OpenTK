namespace Spacebox.Game.Player.Footsteps;

public static class FootstepSoundNames
{
    public static string[] KeepExisting(string[] names, Func<string, bool> exists, Action<string> onMissing)
    {
        if (names == null || names.Length == 0) return Array.Empty<string>();

        var result = new List<string>(names.Length);
        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];
            if (string.IsNullOrEmpty(name)) continue;
            if (exists(name)) result.Add(name);
            else onMissing?.Invoke(name);
        }
        return result.ToArray();
    }
}

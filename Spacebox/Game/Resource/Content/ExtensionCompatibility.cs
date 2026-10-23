namespace Spacebox.Game.Resource.Content;

public static class ExtensionCompatibility
{
    public static string Problem(InstalledGameSet extension, InstalledGameSet baseSet)
    {
        if (extension == null || baseSet == null) return "The GameSet is not installed";

        var config = extension.Config;
        if (!ExtensionTargets.Matches(config.Extends, baseSet))
            return $"Made for {ExtensionTargets.Describe(config.Extends)}, not for {baseSet.DisplayName}";

        string version = baseSet.Config.Version ?? "";
        if (!string.IsNullOrWhiteSpace(config.MinBaseVersion) && GameSetVersion.Compare(version, config.MinBaseVersion) < 0)
            return $"Needs {baseSet.DisplayName} {config.MinBaseVersion} or newer (installed: {version})";
        if (!string.IsNullOrWhiteSpace(config.MaxBaseVersion) && GameSetVersion.Compare(version, config.MaxBaseVersion) > 0)
            return $"Works with {baseSet.DisplayName} up to {config.MaxBaseVersion} (installed: {version})";
        return null;
    }
}

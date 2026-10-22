namespace Spacebox.Network.Admin;

public static class PlayerBadges
{
    public const string DeveloperPrefix = "[DEVELOPER] ";

    private static readonly string[] Developers = { "PasterLak" };

    public static bool IsDeveloper(string name) => ServerAdmins.Contains(Developers, name);

    public static string Label(string name, bool admin) => IsDeveloper(name) ? DeveloperPrefix + name : ServerAdmins.Label(name, admin);
}

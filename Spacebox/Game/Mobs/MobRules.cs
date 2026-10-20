namespace Spacebox.Game.Mobs;

public static class MobRules
{
    public const float ProvokedDuration = 30f;

    public static bool FriendlyMobs { get; set; }

    public static bool IsHostile(bool friendlyMobs, bool provoked) => !friendlyMobs || provoked;
}

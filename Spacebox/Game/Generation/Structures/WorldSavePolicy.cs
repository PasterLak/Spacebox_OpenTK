namespace Spacebox.Game.Generation;

public static class WorldSavePolicy
{
#if FINAL
    public const bool SavesOnLeave = true;
#else
    public const bool SavesOnLeave = false;
#endif
}

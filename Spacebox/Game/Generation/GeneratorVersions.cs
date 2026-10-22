namespace Spacebox.Game.Generation;

public static class GeneratorVersions
{
    public const int Legacy = 1;
    public const int Rules = 2;
    public const int Current = Rules;

    public static int Normalize(int version) => version < Legacy ? Legacy : version;

    public static bool UsesRules(int version) => version >= Rules;
}

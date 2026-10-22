namespace Spacebox.Game.Generation;

public static class GeneratorVersions
{
    public const int Legacy = 1;
    public const int Rules = 2;
    public const int Shapes = 3;
    public const int Current = Shapes;

    public static int Normalize(int version) => version < Legacy ? Legacy : version;

    public static bool UsesRules(int version) => version >= Rules;

    public static bool UsesShapes(int version) => version >= Shapes;
}

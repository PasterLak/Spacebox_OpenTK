using Spacebox.Game.Mobs.Brains;

namespace Spacebox.Game.Mobs;

public static class MobBrainFactory
{
    private static readonly Dictionary<string, Func<MobContext, IMobBrain>> _builders =
        new Dictionary<string, Func<MobContext, IMobBrain>>(StringComparer.OrdinalIgnoreCase)
        {
            ["parasite"] = context => new ParasiteBrain(context)
        };

    public static void Register(string behaviour, Func<MobContext, IMobBrain> builder)
    {
        _builders[behaviour] = builder;
    }

    public static bool IsRegistered(string behaviour)
    {
        return !string.IsNullOrEmpty(behaviour) && _builders.ContainsKey(behaviour);
    }

    public static IMobBrain Create(MobContext context)
    {
        return _builders[context.Definition.Behaviour](context);
    }
}

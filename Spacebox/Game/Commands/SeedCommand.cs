using Engine;
using Engine.Commands;
using Spacebox.Game.Generation;

namespace Spacebox.Game.Commands;

public sealed class SeedCommand : CommandBase
{
    public override string Name => "seed";
    public override string Description => "Show the world name, seed and GameSet";

    public override void Execute(string[] args)
    {
        var info = World.WorldData?.Info;
        if (info == null)
        {
            Debug.Error("No world is loaded");
            return;
        }

        Debug.Log(Describe(info.Name, info.Seed, info.ModId));
    }

    public static string Describe(string world, string seed, string modId)
    {
        return $"World '{world}', seed {(string.IsNullOrEmpty(seed) ? "unknown" : seed)}, GameSet {(string.IsNullOrEmpty(modId) ? "unknown" : modId)}";
    }
}

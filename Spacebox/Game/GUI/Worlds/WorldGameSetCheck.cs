using Spacebox.Game.Resource.Content;
using Spacebox.Network;

namespace Spacebox.Game.GUI;

public static class WorldGameSetCheck
{
    public static string[] ExtensionsOf(WorldInfo world)
    {
        return WorldExtensionList.Normalize(world?.Extensions);
    }

    public static GameSetStackResult Resolve(WorldInfo world)
    {
        return GameSetStackResolver.Resolve(GameSetLocator.Local, world.ModId, ExtensionsOf(world));
    }

    public static bool TryResolve(WorldInfo world, out string error)
    {
        var result = Resolve(world);
        error = result.Success ? "" : $"World '{world.Name}' cannot be loaded:\n{result.ErrorText}";
        return result.Success;
    }
}

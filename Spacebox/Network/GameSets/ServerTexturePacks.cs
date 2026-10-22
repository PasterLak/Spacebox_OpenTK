using Spacebox.Game.Resource.TexturePacks;
using Spacebox.Network.Dedicated;

namespace Spacebox.Network;

public static class ServerTexturePacks
{
    public static IReadOnlyList<string> Ids => GameSession.IsDedicated
        ? DedicatedServer.Config?.TexturePacks ?? new List<string>()
        : TexturePackService.Enabled;

    public static IReadOnlyList<TexturePack> Resolve()
    {
        return TexturePackCatalog.Active(TexturePackService.Available, Ids);
    }

    public static string[] IdsOf(IReadOnlyList<TexturePack> packs)
    {
        var ids = new string[packs.Count];
        for (int i = 0; i < packs.Count; i++) ids[i] = packs[i].Id;
        return ids;
    }
}

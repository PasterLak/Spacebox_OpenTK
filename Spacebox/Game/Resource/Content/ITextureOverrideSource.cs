namespace Spacebox.Game.Resource.Content;

public interface ITextureOverrideSource
{
    IReadOnlyDictionary<string, string> BlockTextures { get; }
    IReadOnlyDictionary<string, string> ItemTextures { get; }
    IReadOnlyDictionary<string, string> Emissions { get; }
}

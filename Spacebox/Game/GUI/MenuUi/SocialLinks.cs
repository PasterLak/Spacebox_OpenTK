using Engine;

namespace Spacebox.Game.GUI.MenuUi;

public readonly record struct SocialLink(string Url, string IconPath);

public static class SocialLinks
{
    private static readonly string[] ExtraLinkLanguages = { "ru", "uk", "kk", "be" };

    public static IReadOnlyList<SocialLink> All()
    {
        var links = new List<SocialLink>
        {
            new SocialLink("https://www.youtube.com/@SpaceboxGame", "Resources/Sprites/UI/youtube.png"),
            new SocialLink("https://discord.gg/9kY3dweXtY", "Resources/Sprites/UI/discord.png"),
            new SocialLink("https://pasterlak.itch.io/spacebox", "Resources/Sprites/UI/itchio.png")
        };

        if (Application.IsSystemLanguageOneOf(ExtraLinkLanguages))
        {
            links.Add(new SocialLink("https://t.me/spacebox_game", "Resources/Sprites/UI/telegram.png"));
            links.Add(new SocialLink("https://vk.com/spacebox_official", "Resources/Sprites/UI/vk.png"));
        }
        return links;
    }
}

namespace Spacebox.Game.GUI.Showcase;

public static class ShowcaseVariants
{
    public static IReadOnlyList<IInterfaceVariant> CreateAll() => new IInterfaceVariant[]
    {
        new ClassicVariant(),
        new TutorialVariant(),
        new CompactVariant(),
        new SidebarVariant(),
        new TerminalVariant()
    };
}

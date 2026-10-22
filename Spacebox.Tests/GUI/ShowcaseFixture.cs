using Engine.CanvasUI;
using Spacebox.Game.GUI.Showcase;

namespace Spacebox.Tests.GUI;

public sealed class ShowcaseFixture
{
    private static readonly Lazy<ShowcaseAssets> SharedAssets = new Lazy<ShowcaseAssets>(() => ShowcaseAssets.Load(AppContext.BaseDirectory, _ => null!));

    public static ShowcaseAssets Assets => SharedAssets.Value;

    public CanvasRoot Root { get; }
    public ShowcaseHost Host { get; }
    public IReadOnlyList<IInterfaceVariant> Variants { get; }

    public ShowcaseFixture(int variant = 0)
    {
        Root = new CanvasRoot();
        Variants = ShowcaseVariants.CreateAll();
        var state = new ShowcaseState();
        state.Fill(Assets);
        Host = new ShowcaseHost(Root, Assets, state, Variants);
        Host.Show(variant);
        Host.Update(0f);
    }

    public static IEnumerable<object[]> VariantIndices()
    {
        int count = ShowcaseVariants.CreateAll().Count;
        for (int i = 0; i < count; i++) yield return new object[] { i };
    }

    public static IEnumerable<T> Descendants<T>(CanvasItem item) where T : CanvasItem
    {
        foreach (var child in item.Children)
        {
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
}

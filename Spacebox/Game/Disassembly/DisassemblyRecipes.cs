using Engine;

namespace Spacebox.Game.Disassembly;

public static class DisassemblyRecipes
{
    public const string ProcessingType = "disassembler";

    private static DisassemblyRecipeResolver _resolver;

    public static DisassemblySettings Settings { get; private set; }

    public static void Initialize(DisassemblySettings settings)
    {
        Settings = settings;
        var providers = CreateProviders();
        _resolver = CreateResolver(settings, providers);

        foreach (var issue in DisassemblyBalanceAudit.FindIssues(settings, providers))
            Debug.Warning("[Disassembly] Unbalanced: " + issue);
    }

    public static bool TryGet(Item item, out DisassemblyRecipe recipe)
    {
        if (_resolver == null) Initialize(DisassemblySettings.CreateDefault());
        return _resolver.TryGet(item, out recipe);
    }

    public static void Clear()
    {
        _resolver = null;
        Settings = null;
    }

    private static IReadOnlyList<IDisassemblySourceProvider> CreateProviders()
    {
        return
        [
            new CraftingDisassemblySourceProvider(GameAssets.Blueprints),
            new ProcessingDisassemblySourceProvider(GameAssets.Recipes, ProcessingType)
        ];
    }

    private static DisassemblyRecipeResolver CreateResolver(DisassemblySettings settings, IReadOnlyList<IDisassemblySourceProvider> providers)
    {
        return new DisassemblyRecipeResolver(
        [
            new ConfiguredDisassemblyRule(settings),
            new LegacyRecipeDisassemblyRule(GameAssets.Recipes, ProcessingType),
            new DerivedDisassemblyRule(settings, providers)
        ]);
    }
}

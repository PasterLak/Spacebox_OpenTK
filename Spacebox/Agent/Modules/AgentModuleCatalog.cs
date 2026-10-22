namespace Spacebox.Agent.Modules;

public static class AgentModuleCatalog
{
    public static IReadOnlyList<IAgentModule> Create(AgentRouter router)
    {
        var navigation = new NavigationModule();
        return new IAgentModule[]
        {
            new HelpModule(router),
            new StateModule(navigation),
            new CommandModule(AgentHost.Log),
            new InputModule(),
            new LookModule(),
            new WorldModule(),
            new InventoryModule(),
            navigation,
            new UiModule(),
            new CanvasModule(),
            new ScreenshotModule(),
            new WaitModule(navigation),
            new AppModule()
        };
    }
}

namespace Spacebox.Agent;

public interface IAgentModule
{
    void Map(AgentRouter router);
}

public interface IAgentTicker
{
    void Tick();
}

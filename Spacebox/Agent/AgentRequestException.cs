namespace Spacebox.Agent;

public sealed class AgentRequestException : Exception
{
    public int Status { get; }

    public AgentRequestException(string message, int status = 400) : base(message)
    {
        Status = status;
    }
}

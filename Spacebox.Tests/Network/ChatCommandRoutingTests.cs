using Spacebox.Network.Admin;

namespace Spacebox.Tests.Network;

public class ChatCommandRoutingTests
{
    [Theory]
    [InlineData("gm 1", "gm")]
    [InlineData("  TP 1 2 3", "tp")]
    [InlineData("help", "help")]
    [InlineData("", "")]
    public void CommandName_IsTheFirstWordInLowerCase(string line, string name)
    {
        Assert.Equal(name, ChatCommands.CommandName(line));
    }

    private sealed class ThreadedLogCommand : Engine.Commands.CommandBase
    {
        public override string Name => "chatcapturethreadtest";
        public override string Description => "test";

        public override void Execute(string[] args)
        {
            Engine.Debug.Log("from the command");
            var worker = new System.Threading.Thread(() => Engine.Debug.Log("from a worker"));
            worker.Start();
            worker.Join();
        }
    }

    [Fact]
    public void Run_CapturesOnlyLinesFromTheCommandThread()
    {
        if (Engine.Commands.CommandManager.GetCommand("chatcapturethreadtest") == null)
            Engine.Commands.CommandManager.RegisterGlobalCommand(new ThreadedLogCommand());

        var output = ChatCommands.Run("chatcapturethreadtest", "Admin");

        Assert.Contains(output, line => line.Text == "from the command");
        Assert.DoesNotContain(output, line => line.Text == "from a worker");
    }

    [Fact]
    public void BuildLine_JoinsNameAndArguments()
    {
        Assert.Equal("gm 1 Anna", ServerCommandForwarder.BuildLine("gm", new[] { "1", "Anna" }));
        Assert.Equal("save", ServerCommandForwarder.BuildLine("save", System.Array.Empty<string>()));
    }
}

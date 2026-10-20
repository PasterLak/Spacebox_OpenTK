using Engine.Commands;

namespace Engine.Tests;

public class CommandCompletionTests
{
    private sealed class TestCommand : CommandBase, IArgumentSuggestions
    {
        private readonly string _name;
        private readonly string[][] _arguments;

        public TestCommand(string name, params string[][] arguments)
        {
            _name = name;
            _arguments = arguments;
        }

        public override string Name => _name;
        public override string Description => _name + " usage";
        public override void Execute(string[] args) { }

        public void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
        {
            if (argumentIndex < _arguments.Length) results.AddRange(_arguments[argumentIndex]);
        }
    }

    private static readonly List<CommandBase> Commands = new()
    {
        new TestCommand("give", new[] { "iron_ingot", "iron_block", "glass" }, new[] { "1", "64" }),
        new TestCommand("gm", new[] { "0", "1", "2" }),
        new TestCommand("help")
    };

    private static List<CommandSuggestion> Suggest(string line)
    {
        var results = new List<CommandSuggestion>();
        CommandCompletion.Suggest(line, Commands, results);
        return results;
    }

    [Fact]
    public void EmptyLine_ListsAllCommandsSorted()
    {
        Assert.Equal(new[] { "give", "gm", "help" }, Suggest("").Select(s => s.Text));
    }

    [Fact]
    public void CommandPrefix_FiltersIgnoringCase()
    {
        var results = Suggest("G");

        Assert.Equal(new[] { "give", "gm" }, results.Select(s => s.Text));
        Assert.Equal("give usage", results[0].Hint);
    }

    [Fact]
    public void Arguments_ComeFromTheCommand()
    {
        Assert.Equal(new[] { "iron_block", "iron_ingot" }, Suggest("give ir").Select(s => s.Text));
        Assert.Equal(new[] { "1", "64" }, Suggest("give glass ").Select(s => s.Text));
        Assert.Empty(Suggest("help x"));
        Assert.Empty(Suggest("unknown "));
    }

    [Fact]
    public void Apply_ReplacesTheLastWord()
    {
        Assert.Equal("give iron_ingot ", CommandCompletion.Apply("give ir", "iron_ingot"));
        Assert.Equal("give glass 64 ", CommandCompletion.Apply("give glass ", "64"));
        Assert.Equal("gm ", CommandCompletion.Apply("g", "gm"));
        Assert.Equal("help ", CommandCompletion.Apply("", "help"));
    }

    [Fact]
    public void Usage_ShowsAfterTheCommandName()
    {
        Assert.Equal("", CommandCompletion.Usage("gi", Commands));
        Assert.Equal("give usage", CommandCompletion.Usage("give ", Commands));
        Assert.Equal("give usage", CommandCompletion.Usage("give iron", Commands));
    }
}

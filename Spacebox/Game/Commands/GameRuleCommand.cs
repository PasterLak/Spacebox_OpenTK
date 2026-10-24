using Engine;
using Engine.Commands;
using Spacebox.Network;
using Spacebox.Network.Admin;

namespace Spacebox.Game.Commands;

public sealed class GameRuleCommand : CommandBase, IArgumentSuggestions
{
    public override string Name => "gamerule";
    public static string RuleNames => string.Join(", ", ServerRulesService.Rules.Select(rule => rule.Name));

    public override string Description => $"Server rules. Usage: gamerule [name] [value] ({RuleNames})";

    public override void Execute(string[] args)
    {
        if (ServerCommandForwarder.TryForward(Name, args)) return;

        if (args.Length == 0)
        {
            foreach (var rule in ServerRulesService.Rules)
                Debug.Log($"{rule.Name}: {rule.Text} ({rule.Label})");
            return;
        }

        if (!ServerRulesService.TryFind(args[0], out var found))
        {
            Debug.Error($"Unknown rule <{args[0]}>, see gamerule");
            return;
        }

        if (args.Length == 1)
        {
            Debug.Log($"{found.Name}: {found.Text}");
            return;
        }

        if (!ServerRulesService.Set(found, args[1]))
        {
            Debug.Error($"Use {found.Usage}");
            return;
        }

        Debug.Success($"{found.Label}: {found.Text}");
    }

    public void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results)
    {
        if (argumentIndex == 0)
        {
            foreach (var rule in ServerRulesService.Rules) results.Add(rule.Name);
        }
        else if (argumentIndex == 1 && previousArguments.Count > 0 && ServerRulesService.TryFind(previousArguments[0], out var found) && found is Spacebox.Game.Rules.SwitchRuleDefinition)
        {
            results.Add("on");
            results.Add("off");
        }
    }
}

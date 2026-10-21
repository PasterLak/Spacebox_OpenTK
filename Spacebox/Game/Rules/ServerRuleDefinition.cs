namespace Spacebox.Game.Rules;

public sealed record ServerRuleDefinition(string Name, string Label, Func<bool> Get, Action<bool> Set);

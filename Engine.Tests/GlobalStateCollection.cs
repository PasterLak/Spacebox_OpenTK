namespace Engine.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GlobalStateCollection
{
    public const string Name = "Engine global state";
}

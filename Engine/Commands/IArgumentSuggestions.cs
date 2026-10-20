namespace Engine.Commands;

public interface IArgumentSuggestions
{
    void SuggestArguments(int argumentIndex, IReadOnlyList<string> previousArguments, List<string> results);
}

namespace Spacebox.Game.Tutorial;

public static class TutorialRewardKeys
{
    public const string ChapterPrefix = "chapter:";

    public static string ForStep(string stepId) => stepId;

    public static string ForChapter(string chapterId) => ChapterPrefix + chapterId;

    public static IReadOnlyList<TutorialReward> Find(TutorialDefinition tutorial, string key)
    {
        if (tutorial == null || string.IsNullOrEmpty(key)) return Array.Empty<TutorialReward>();

        bool chapter = key.StartsWith(ChapterPrefix, StringComparison.Ordinal);
        string id = chapter ? key.Substring(ChapterPrefix.Length) : key;
        foreach (var item in tutorial.Chapters)
        {
            if (chapter)
            {
                if (item.Id == id) return item.Rewards;
                continue;
            }

            foreach (var step in item.Steps)
            {
                if (step.Id == id) return step.Rewards;
            }
        }
        return Array.Empty<TutorialReward>();
    }
}

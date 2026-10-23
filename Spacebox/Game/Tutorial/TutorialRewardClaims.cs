using Spacebox.Game.Player.GameModes;

namespace Spacebox.Game.Tutorial;

public static class TutorialRewardClaims
{
    public static IReadOnlyList<TutorialReward> Resolve(TutorialDefinition tutorial, string rewardKey, GameMode baseMode, GameMode modeId, out string claimKey)
    {
        claimKey = null;
        if (tutorial == null || string.IsNullOrEmpty(rewardKey)) return Array.Empty<TutorialReward>();
        if (!tutorial.SupportsMode(baseMode) && !tutorial.SupportsMode(modeId)) return Array.Empty<TutorialReward>();
        if (!IsStepAvailable(tutorial, rewardKey, baseMode, modeId)) return Array.Empty<TutorialReward>();

        var rewards = TutorialRewardKeys.Find(tutorial, rewardKey);
        if (rewards.Count > 0) claimKey = tutorial.Id + "/" + rewardKey;
        return rewards;
    }

    private static bool IsStepAvailable(TutorialDefinition tutorial, string rewardKey, GameMode baseMode, GameMode modeId)
    {
        if (rewardKey.StartsWith(TutorialRewardKeys.ChapterPrefix, StringComparison.Ordinal)) return true;

        foreach (var chapter in tutorial.Chapters)
        {
            foreach (var step in chapter.Steps)
            {
                if (step.Id == rewardKey) return step.IsAvailableIn(baseMode) || step.IsAvailableIn(modeId);
            }
        }
        return false;
    }
}

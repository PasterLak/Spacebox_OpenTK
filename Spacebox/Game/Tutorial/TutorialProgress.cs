namespace Spacebox.Game.Tutorial;

public class TutorialProgress
{
    public string TutorialId { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool Declined { get; set; } = false;
    public bool Collapsed { get; set; } = false;
    public bool Finished { get; set; } = false;
    public string StepId { get; set; } = "";
    public List<float> TaskProgress { get; set; } = new List<float>();
    public List<string> CompletedSteps { get; set; } = new List<string>();
    public List<string> Rewarded { get; set; } = new List<string>();

    public static TutorialProgress StartOver(string tutorialId, TutorialProgress previous)
    {
        var progress = new TutorialProgress { TutorialId = tutorialId ?? "" };
        if (previous?.Rewarded != null) progress.Rewarded.AddRange(previous.Rewarded);
        return progress;
    }

    public static TutorialProgress CreateDeclined(string tutorialId)
    {
        return new TutorialProgress { TutorialId = tutorialId ?? "", Enabled = false, Declined = true };
    }

    public void Normalize()
    {
        TutorialId ??= "";
        StepId ??= "";
        TaskProgress ??= new List<float>();
        CompletedSteps ??= new List<string>();
        Rewarded ??= new List<string>();
    }
}

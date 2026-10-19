namespace Spacebox.Game.Tutorial;

public class TutorialJSON
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "Tutorial";
    public string Description { get; set; } = "";
    public List<string> GameModes { get; set; } = new List<string>();
    public int Order { get; set; } = 0;
    public List<TutorialChapterJSON> Chapters { get; set; } = new List<TutorialChapterJSON>();
}

public class TutorialChapterJSON
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Icon { get; set; } = "";
    public bool Optional { get; set; } = false;
    public List<TutorialRewardJSON> Reward { get; set; } = new List<TutorialRewardJSON>();
    public List<TutorialStepJSON> Steps { get; set; } = new List<TutorialStepJSON>();
}

public class TutorialStepJSON
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public string Hint { get; set; } = "";
    public string Icon { get; set; } = "";
    public bool Skippable { get; set; } = true;
    public string RequiresGameMode { get; set; } = "";
    public float Delay { get; set; } = 0f;
    public float ReadSeconds { get; set; } = 6f;
    public string CompleteSound { get; set; } = "";
    public List<TutorialRewardJSON> Reward { get; set; } = new List<TutorialRewardJSON>();
    public List<TutorialTaskJSON> Tasks { get; set; } = new List<TutorialTaskJSON>();
}

public class TutorialTaskJSON
{
    public string Type { get; set; } = "";
    public string Label { get; set; } = "";
    public string Icon { get; set; } = "";
    public bool Optional { get; set; } = false;
    public int Count { get; set; } = 1;
    public string Item { get; set; } = "";
    public string Block { get; set; } = "";
    public string Action { get; set; } = "";
    public float Seconds { get; set; } = 1f;
    public float Distance { get; set; } = 10f;
    public float Degrees { get; set; } = 90f;
    public string Stat { get; set; } = "";
    public float Delta { get; set; } = 1f;
    public string Window { get; set; } = "";
    public string What { get; set; } = "";
    public bool State { get; set; } = true;
    public bool? Flight { get; set; }
    public string Slot { get; set; } = "";
    public string Machine { get; set; } = "";
    public List<TutorialTaskJSON> Tasks { get; set; } = new List<TutorialTaskJSON>();
}

public class TutorialRewardJSON
{
    public string Item { get; set; } = "";
    public int Count { get; set; } = 1;
}

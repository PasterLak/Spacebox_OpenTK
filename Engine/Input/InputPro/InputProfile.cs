
using System.Text.Json.Serialization;

namespace Engine.InputPro;

public class InputProfile
{
    public string Name { get; set; } = "Default";
    public Dictionary<string, InputAction> Actions { get; set; } = new();
    [JsonIgnore]
    public List<InputAction> ActionOrder { get; set; } = new();
    public Dictionary<string, InputAxis> Axes { get; set; } = new();

    public void KeepExistingActions(InputProfile previous)
    {
        if (previous == null) return;

        foreach (var id in Actions.Keys.ToArray())
        {
            if (!previous.Actions.TryGetValue(id, out var existing)) continue;

            var loaded = Actions[id];
            existing.CopySettingsFrom(loaded);
            Actions[id] = existing;

            int index = ActionOrder.IndexOf(loaded);
            if (index >= 0) ActionOrder[index] = existing;
        }

        foreach (var pair in previous.Actions)
        {
            if (Actions.ContainsKey(pair.Key)) continue;
            Actions[pair.Key] = pair.Value;
            ActionOrder.Add(pair.Value);
        }
    }
}
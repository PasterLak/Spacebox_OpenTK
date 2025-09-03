
using System.Text.Json.Serialization;

namespace Engine.InputPro;

public class InputProfile
{
    public string Name { get; set; } = "Default";
    public Dictionary<string, InputAction> Actions { get; set; } = new();
    [JsonIgnore]
    public List<string> ActionOrder { get; set; } = new();
    public Dictionary<string, InputAxis> Axes { get; set; } = new();
}
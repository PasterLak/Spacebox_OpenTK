
using System.Text.Json.Serialization;


namespace Engine.InputPro;

[JsonConverter(typeof(InputBindingConverter))]
public abstract class InputBinding
{
    public abstract bool IsPressed(InputState state);
    public abstract bool IsReleased(InputState state);
    public abstract bool IsHeld(InputState state);
    public abstract string GetDisplayName();
}

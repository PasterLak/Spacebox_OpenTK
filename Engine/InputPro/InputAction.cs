using System.Text.Json.Serialization;

namespace Engine.InputPro;

public class InputAction
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<InputBinding> Bindings { get; set; } = new();
    public bool Enabled { get; set; } = true;
    public float DeadZone { get; set; } = 0.1f;

    [JsonIgnore]
    private readonly Dictionary<InputEventType, List<Action>> callbacks = new();
    [JsonIgnore]
    private readonly Dictionary<InputEventType, List<Action<float>>> valueCallbacks = new();
    [JsonIgnore]
    private bool wasPressed;
    [JsonIgnore]
    private bool isPressed;

    public InputAction() { }
    public InputAction(string name, string description = "")
    {
        Name = name;
        Description = description;
    }

    public void AddBinding(InputBinding binding)
    {
        Bindings.Add(binding);
    }

    public void RemoveBinding(InputBinding binding)
    {
        Bindings.Remove(binding);
    }

    public void ClearBindings()
    {
        Bindings.Clear();
    }

    public void Subscribe(InputEventType eventType, Action callback)
    {
        if (!callbacks.ContainsKey(eventType))
            callbacks[eventType] = new List<Action>();
        callbacks[eventType].Add(callback);
    }

    public void Subscribe(InputEventType eventType, Action<float> callback)
    {
        if (!valueCallbacks.ContainsKey(eventType))
            valueCallbacks[eventType] = new List<Action<float>>();
        valueCallbacks[eventType].Add(callback);
    }

    public void Unsubscribe(InputEventType eventType, Action callback)
    {
        if (callbacks.ContainsKey(eventType))
            callbacks[eventType].Remove(callback);
    }

    public void Unsubscribe(InputEventType eventType, Action<float> callback)
    {
        if (valueCallbacks.ContainsKey(eventType))
            valueCallbacks[eventType].Remove(callback);
    }

    public void Update(InputState state)
    {
        if (!Enabled) return;

        wasPressed = isPressed;
        isPressed = Bindings.Any(b => b.IsHeld(state));

        if (!wasPressed && isPressed)
        {
            InvokeCallbacks(InputEventType.Pressed, 1.0f);
        }
        else if (wasPressed && !isPressed)
        {
            InvokeCallbacks(InputEventType.Released, 0.0f);
        }
        else if (isPressed)
        {
            InvokeCallbacks(InputEventType.Held, 1.0f);
        }
    }

    private void InvokeCallbacks(InputEventType eventType, float value)
    {
        if (callbacks.ContainsKey(eventType))
        {
            foreach (var callback in callbacks[eventType])
                callback?.Invoke();
        }

        if (valueCallbacks.ContainsKey(eventType))
        {
            foreach (var callback in valueCallbacks[eventType])
                callback?.Invoke(value);
        }
    }

    public bool IsPressed() => Enabled && !wasPressed && isPressed;
    public bool IsReleased() => Enabled && wasPressed && !isPressed;
    public bool IsHeld() => Enabled && isPressed;
    public float GetValue() => Enabled &&  isPressed ? 1.0f : 0.0f;
}
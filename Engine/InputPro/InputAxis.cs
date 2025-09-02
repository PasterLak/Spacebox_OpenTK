using OpenTK.Mathematics;
using System.Text.Json.Serialization;

namespace Engine.InputPro;


public class InputAxis
{
    public string Name { get; set; }
    public string PositiveAction { get; set; }
    public string NegativeAction { get; set; }
    public float Sensitivity { get; set; } = 1.0f;
    public float Gravity { get; set; } = 3.0f;
    public float DeadZone { get; set; } = 0.1f;
    public bool Snap { get; set; } = true;

    [JsonIgnore]
    private float currentValue;
    [JsonIgnore]
    private float targetValue;

    public InputAxis() { }
    public InputAxis(string name, string positive, string negative)
    {
        Name = name;
        PositiveAction = positive;
        NegativeAction = negative;
    }

    internal void Update(InputManager manager)
    {
        targetValue = 0;

        if (manager.IsActionHeld(PositiveAction))
            targetValue += 1;
        if (manager.IsActionHeld(NegativeAction))
            targetValue -= 1;

        if (Snap && Math.Sign(currentValue) != Math.Sign(targetValue) && targetValue != 0)
            currentValue = 0;

        if (Math.Abs(targetValue) > 0.01f)
        {
            currentValue = MathHelper.Lerp(currentValue, targetValue, Sensitivity * Time.Delta);
        }
        else
        {
            currentValue = MathHelper.Lerp(currentValue, 0, Gravity * Time.Delta);
        }

        if (Math.Abs(currentValue) < DeadZone)
            currentValue = 0;

        currentValue = Math.Clamp(currentValue, -1, 1);
    }

    public float GetValue() => currentValue;
    public float GetRawValue() => targetValue;
}
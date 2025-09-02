using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine.InputPro;

public class InputRemapper
{
    private InputManager input;
    private string actionToRemap;
    private bool isRemapping;

    public InputRemapper()
    {
        input = InputManager.Instance;
    }

    public void StartRemapping(string actionName)
    {
        actionToRemap = actionName;
        isRemapping = true;
    }

    public bool Update(KeyboardState keyboard, MouseState mouse)
    {
        if (!isRemapping) return false;

        foreach (Keys key in Enum.GetValues<Keys>())
        {
            if (key == Keys.Unknown) continue;

            if (keyboard.IsKeyPressed(key))
            {
                if (key == Keys.Escape)
                {
                    CancelRemapping();
                    return true;
                }

                RemapToKey(key);
                return true;
            }
        }

        foreach (MouseButton button in Enum.GetValues<MouseButton>())
        {
            if (mouse.IsButtonPressed(button))
            {
                RemapToMouseButton(button);
                return true;
            }
        }

        return false;
    }

    private void RemapToKey(Keys key)
    {
        var action = input.GetAction(actionToRemap);
        if (action != null)
        {
            action.Bindings.Clear(); 
            action.AddBinding(new KeyBinding(key));

        }
        isRemapping = false;
    }

    private void RemapToMouseButton(MouseButton button)
    {
        var action = input.GetAction(actionToRemap);
        if (action != null)
        {
            action.Bindings.Clear();
            action.AddBinding(new MouseButtonBinding(button));
           
        }
        isRemapping = false;
    }

    public void CancelRemapping()
    {
        isRemapping = false;
        actionToRemap = null;
    }
}



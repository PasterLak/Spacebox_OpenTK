using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Text.Json;

namespace Engine.InputPro;

public class InputManager
{
    private static InputManager instance;
    public static InputManager Instance => instance ??= new InputManager();

    private GameWindow window;
    private InputState currentState;
    private InputState previousState;
    private InputProfile activeProfile;
    private Dictionary<string, InputProfile> profiles = new();

    public bool Enabled { get; set; } = true;
    public Vector2 MouseSensitivity { get; set; } = Vector2.One;

    private InputManager()
    {
        activeProfile = new InputProfile { Name = "Default" };
        profiles["Default"] = activeProfile;
    }

    public void Initialize(GameWindow gameWindow)
    {
        window = gameWindow;
        currentState = new InputState
        {
            Keyboard = window.KeyboardState,
            Mouse = window.MouseState,
            MousePosition = window.MousePosition
        };
        previousState = new InputState
        {
            Keyboard = window.KeyboardState,
            Mouse = window.MouseState,
            MousePosition = window.MousePosition
        };
    }

    public void Update()
    {
        if (!Enabled || window == null) return;

        previousState.Keyboard = currentState.Keyboard;
        previousState.Mouse = currentState.Mouse;
        previousState.MousePosition = currentState.MousePosition;

        currentState.Keyboard = window.KeyboardState;
        currentState.Mouse = window.MouseState;
        currentState.MousePosition = window.MousePosition;
        currentState.MouseDelta = (currentState.MousePosition - previousState.MousePosition) * MouseSensitivity;
        currentState.ScrollDelta = currentState.Mouse.ScrollDelta;

        foreach (var action in activeProfile.Actions.Values)
        {
            action.Update(currentState);
        }

        foreach (var axis in activeProfile.Axes.Values)
        {
            axis.Update(this);
        }
    }

    public InputAction AddAction(string name, string description = "")
    {
        var action = new InputAction(name, description);
        activeProfile.Actions[name] = action;
        return action;
    }

    public InputAction AddAction(string name, Keys key, string description = "")
    {
        var action = AddAction(name, description);
        action.AddBinding(new KeyBinding(key));
        return action;
    }

    public InputAction AddAction(string name, MouseButton button, string description = "")
    {
        var action = AddAction(name, description);
        action.AddBinding(new MouseButtonBinding(button));
        return action;
    }

    public InputAxis AddAxis(string name, string positiveAction, string negativeAction)
    {
        var axis = new InputAxis(name, positiveAction, negativeAction);
        activeProfile.Axes[name] = axis;
        return axis;
    }

    public void RemoveAction(string name)
    {
        activeProfile.Actions.Remove(name);
    }

    public void RemoveAxis(string name)
    {
        activeProfile.Axes.Remove(name);
    }

    public InputAction GetAction(string id)
    {
        if(activeProfile.Actions.TryGetValue(id, out var action))
        {
            return action;
        }
        else
        {            
            Debug.Error($"[InputManager] Action '{id}' not found in profile '{activeProfile.Name}'");
            return AddAction(id);
        }
    }

    public InputAxis GetAxis(string id)
    {
        return activeProfile.Axes.TryGetValue(id, out var axis) ? axis : null;
    }

    public bool IsActionPressed(string name)
    {
        return GetAction(name)?.IsPressed() ?? false;
    }

    public bool IsActionReleased(string name)
    {
        return GetAction(name)?.IsReleased() ?? false;
    }

    public bool IsActionHeld(string name)
    {
        return GetAction(name)?.IsHeld() ?? false;
    }

    public float GetActionValue(string name)
    {
        return GetAction(name)?.GetValue() ?? 0f;
    }

    public float GetAxisValue(string name)
    {
        return GetAxis(name)?.GetValue() ?? 0f;
    }

    public float GetAxisRawValue(string name)
    {
        return GetAxis(name)?.GetRawValue() ?? 0f;
    }

    public Vector2 GetMousePosition() => currentState?.MousePosition ?? Vector2.Zero;
    public Vector2 GetMouseDelta() => currentState?.MouseDelta ?? Vector2.Zero;
    public Vector2 GetScrollDelta() => currentState?.ScrollDelta ?? Vector2.Zero;

    public void SaveConfiguration(string path)
    {

        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new InputBindingConverter() }
            };
            var json = JsonSerializer.Serialize(profiles, options);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.Error($"[InputManager] Failed to save input configuration: {ex.Message}");
        }
        finally
        {
            Debug.Success($"[InputManager] Input configuration saved to {path} (profiles: {profiles.Count})");
        }
    }

    public void LoadConfiguration(string path)
    {
   
        if (!File.Exists(path))
        {
            Debug.Error($"[InputManager] Input configuration file not found: {path}");
            return;
        }
        
        try
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                Converters = { new InputBindingConverter() }
            };
            profiles = JsonSerializer.Deserialize<Dictionary<string, InputProfile>>(json, options) ?? new();

            if (profiles.ContainsKey("Default"))
                activeProfile = profiles["Default"];
        }
        catch (Exception ex)
        {
            Debug.Error($"[InputManager] Failed to load input configuration: {ex.Message}");
        }finally
        {
            Debug.Success($"[InputManager] Input configuration loaded from {path} (profiles: {profiles.Count})");
        }
    }

    public void CreateProfile(string name)
    {
        profiles[name] = new InputProfile { Name = name };
    }

    public void SwitchProfile(string name)
    {
        if (profiles.TryGetValue(name, out var profile))
            activeProfile = profile;
    }

    public void DeleteProfile(string name)
    {
        if (name != "Default" && profiles.ContainsKey(name))
        {
            profiles.Remove(name);
            if (activeProfile.Name == name)
                SwitchProfile("Default");
        }
    }

    public IEnumerable<string> GetProfileNames() => profiles.Keys;

    public void SetCursorState(CursorState state)
    {
        if (window != null)
            window.CursorState = state;
    }

    public void CenterCursor()
    {
        if (window != null)
            window.MousePosition = new Vector2(window.Size.X / 2f, window.Size.Y / 2f);
    }
}

public static class InputManagerExtensions
{
    public static Dictionary<string, InputAction> GetAllActions(this InputManager manager)
    {
        var profile = manager.GetActiveProfile();
        return profile?.Actions ?? new Dictionary<string, InputAction>();
    }

    public static InputProfile GetActiveProfile(this InputManager manager)
    {
        var profileField = typeof(InputManager).GetField("activeProfile",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return profileField?.GetValue(manager) as InputProfile;
    }
}
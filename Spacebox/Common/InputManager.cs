using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Common
{
    public static class InputManager
    {
        public static bool Enabled { get;  set; } = true;
        private static Dictionary<string, InputAction> actions = new Dictionary<string, InputAction>();

        public static InputAction AddAction(string name, Keys key, bool isGlobal = false)
        {
            if (!actions.ContainsKey(name))
            {
                actions[name] = new InputAction(name, key, isGlobal);

                return actions[name];
            }

            Debug.Error($"[InputManager] Action with name <{name}> was already added!");
            return new InputAction(name + "2", key, isGlobal);
        }

        public static InputAction AddAction(string name, MouseButton button, bool isGlobal = false)
        {
            if (!actions.ContainsKey(name))
            {
                actions[name] = new InputAction(name, button, isGlobal);

                return actions[name];
            }
    
            Debug.Error($"[InputManager] Action with name <{name}> was already added!");

            return new InputAction(name + "2", button, isGlobal);
        }

        public static void RemoveAction(string name)
        {
            if (actions.ContainsKey(name))
                actions.Remove(name);
        }

        public static void RemoveAllActions(bool onlyNonGlobal = false)
        {
            if (onlyNonGlobal)
            {
                var keysToRemove = new List<string>();
                foreach (var kvp in actions)
                {
                    if (!kvp.Value.IsStatic)
                        keysToRemove.Add(kvp.Key);
                }
                foreach (var key in keysToRemove)
                {
                    actions.Remove(key);
                }
            }
            else
            {
                actions.Clear();
            }
        }

        public static void SetDescription(string name, string description)
        {
            if (actions.ContainsKey(name))
            {
                actions[name].Description = description;
            }
        }

        public static void ChangeKey(string name, Keys newKey)
        {
            if (actions.ContainsKey(name))
            {
                actions[name].Key = newKey;
                actions[name].MouseButton = null;
            }
        }

        public static void ChangeMouseButton(string name, MouseButton newButton)
        {
            if (actions.ContainsKey(name))
            {
                actions[name].MouseButton = newButton;
                actions[name].Key = null;
            }
        }

        public static InputAction RegisterCallback(string name, Action callback)
        {
            if (actions.ContainsKey(name))
            {
                actions[name].Callbacks.Add(callback);
                return actions[name];
            }
               
            return null;
        }

        public static void UnregisterCallback(string name, Action callback)
        {
            if (actions.ContainsKey(name))
                actions[name].Callbacks.Remove(callback);
        }

        public static bool IsActionPressed(string name)
        {
            if (actions.ContainsKey(name))
            {
                var action = actions[name];
                if (action.Key.HasValue && Input.IsKeyDown(action.Key.Value))
                    return true;
                if (action.MouseButton.HasValue && Input.IsMouseButtonDown(action.MouseButton.Value))
                    return true;
            }
            return false;
        }

        public static void Update()
        {
            if(!Enabled) return;

            foreach (var action in actions.Values)
            {
                bool isPressed = false;
                if (action.Key.HasValue)
                    isPressed = Input.IsKeyDown(action.Key.Value);
                else if (action.MouseButton.HasValue)
                    isPressed = Input.IsMouseButtonDown(action.MouseButton.Value);

                if (isPressed)
                {
                    foreach (var callback in action.Callbacks)
                        callback.Invoke();
                }
            }
        }

       
    }
}

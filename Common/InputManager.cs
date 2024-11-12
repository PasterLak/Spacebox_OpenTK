
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Spacebox.Common
{
    public static class InputManager
    {
        private static Dictionary<string, InputAction> actions = new Dictionary<string, InputAction>();

        public static void AddAction(string name, Keys key, bool isGlobal = false)
        {
            if (!actions.ContainsKey(name))
                actions[name] = new InputAction(name, key, isGlobal);
        }

        public static void AddAction(string name, MouseButton button, bool isGlobal = false)
        {
            if (!actions.ContainsKey(name))
                actions[name] = new InputAction(name, button, isGlobal);
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

        public static void RegisterCallback(string name, Action callback)
        {
            if (actions.ContainsKey(name))
                actions[name].Callbacks.Add(callback);
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

        private class InputAction
        {
            public string Name { get; private set; }
            public Keys? Key { get; set; }
            public MouseButton? MouseButton { get; set; }
            public List<Action> Callbacks { get; private set; }
            public bool IsStatic { get; set; }

            public InputAction(string name, Keys key, bool isStatic = false)
            {
                Name = name;
                Key = key;
                IsStatic = isStatic;
                Callbacks = new List<Action>();
            }

            public InputAction(string name, MouseButton button, bool isStatic = false)
            {
                Name = name;
                MouseButton = button;
                IsStatic = isStatic;
                Callbacks = new List<Action>();
            }
        }
    }
}

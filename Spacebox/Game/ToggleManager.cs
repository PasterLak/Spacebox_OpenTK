using Spacebox.Common;
using System;

namespace Spacebox.Game
{
    public class ToggleManager
    {

        public static ToggleManager Instance { get; private set; }

        private Dictionary<string, Toggi> toggles = new Dictionary<string, Toggi>();

        public ToggleManager()
        {
            Instance = this;
        }

        public Toggi Register(string name)
        {
            return Register(new Toggi(name));
        }
        public Toggi Register(Toggi toggleable)
        {
            if (Exists(toggleable.Name))
            {
                Debug.Error($"[ToggleManager][Register] {toggleable.Name} is already registered!");
                return toggleable;
            }

            toggles.Add(toggleable.Name, toggleable);

            return toggleable;
        }

        public void AddSlave(string toggle, string slave)
        {
            if (Exists(toggle) && Exists(slave))
            {
                // toggles[toggle].slaves.Add(toggles[slave].toggle);
            }
        }

        public void Unregister(Toggi toggleable)
        {
            if (Exists(toggleable.Name))
            {
                toggles.Remove(toggleable.Name);
            }
        }

        public void Unregister(string toggleName)
        {
            if (Exists(toggleName))
            {
                toggles.Remove(toggleName);
            }
        }

        public void SetState(string toggleName, bool state)
        {
            Toggi? tg = null;
            if (!toggles.TryGetValue(toggleName, out tg))
            {
                Debug.Error($"[ToggleManager][SetState] {toggleName} was not found!");
                return;
            }

            if (tg == null)
            {
                toggles.Remove(toggleName);
                Debug.Error($"[ToggleManager][SetState] {toggleName} was null and was removed!");
                return;
            }

            tg.SetState(state);
        }

        public bool IsActiveAndExists(string toggleName)
        {
            if (toggles.TryGetValue(toggleName, out Toggi tg))
            {
                return tg != null && tg.State;
            }

            return false;
        }

        public bool Exists(string toggleName)
        {
            return toggles.ContainsKey(toggleName);
        }


    }
}

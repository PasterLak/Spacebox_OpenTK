using Spacebox.Engine;

namespace Spacebox.Engine
{
    public class ToggleManager
    {

        private static readonly Dictionary<string, Toggi> toggles = new Dictionary<string, Toggi>();

        private static readonly List<Toggi> openedWindows = new List<Toggi>();

        public static int OpenedWindowsCount => openedWindows.Count;
        public ToggleManager()
        {

        }

        public static void DisableAll()
        {
            foreach (var tg in toggles)
            {
                tg.Value.Disable();
            }
        }
        public static void DisableAllWindowsButThis(string name)
        {
            if (OpenedWindowsCount == 0) return;

            Toggi t = null;
            foreach (var tg in openedWindows)
            {
                if (tg.Name != name)
                    tg.Disable();
                else
                    t = tg;
            }

            openedWindows.Clear();
            if (t != null)
                openedWindows.Add(t);
        }


        public static void DisableAllWindows()
        {
            if (OpenedWindowsCount == 0) return;

            foreach (var tg in openedWindows)
            {
                tg.Disable();
            }

            openedWindows.Clear();
        }

        public static Toggi Register(string name)
        {
            return Register(new Toggi(name));
        }

        public static Toggi Register(Toggi toggleable)
        {
            if (Exists(toggleable.Name))
            {
                Debug.Error($"[ToggleManager][Register] {toggleable.Name} is already registered!");
                return toggleable;
            }

            toggles.Add(toggleable.Name, toggleable);

            return toggleable;
        }

        public static void Unregister(Toggi toggleable)
        {
            if (Exists(toggleable.Name))
            {
                toggles.Remove(toggleable.Name);
            }
        }

        public static void Unregister(string toggleName)
        {
            if (Exists(toggleName))
            {
                toggles.Remove(toggleName);
            }
        }

        public static void SetState(string toggleName, bool state)
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

            if (tg.IsUI)
            {
                if (state)
                {
                    if (!openedWindows.Contains(tg))
                    {
                        openedWindows.Add(tg);
                    }
                }
                else
                {
                    if (openedWindows.Contains(tg))
                    {
                        openedWindows.Remove(tg);
                    }
                }
            }
        }

        public static bool IsActiveAndExists(string toggleName)
        {
            if (toggles.TryGetValue(toggleName, out Toggi tg))
            {
                return tg != null && tg.State;
            }
            return false;
        }

        public static bool Exists(string toggleName)
        {
            return toggles.ContainsKey(toggleName);
        }

        public static void Dispose()
        {
            toggles.Clear();
            openedWindows.Clear();
        }
    }
}

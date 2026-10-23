using Engine;
using Engine.InputPro;

namespace Spacebox.Game.GUI
{
    public static class InputOverlay
    {
        private const string ToggleAction = "inputOverlay";
        private const string DebugCategory = "Debug";
        private const int MaxColumns = 3;

        private static readonly (string Key, string Name)[] DebugKeys =
        {
            ("`", "Console"),
            ("F3", "Show FPS"),
            ("F4", "Visual debug"),
            ("F7", "FPS limiter off"),
            ("F8", "Hide interface"),
            ("F9", "UI debugger"),
            ("F10", "Render debug views"),
            ("F11", "Fullscreen"),
            ("F12", "Screenshot"),
            ("Alt+F4", "Close the game")
        };

        public sealed class Group
        {
            public string Title;
            public readonly List<(string Key, string Name)> Entries = new List<(string, string)>();
        }

        private static readonly List<Group> _groups = new List<Group>();
        private static readonly List<List<Group>> _columns = new List<List<Group>>();

        public static bool IsVisible = false;

        public static int Version { get; private set; }

        public static IReadOnlyList<List<Group>> Columns => _columns;

        public static string ToggleKey => KeyNames.ForAction(ToggleAction) ?? "F6";

        public static void Update()
        {
            if (Debug.IsVisible || Chat.FocusInput) return;
            if (InputManager.Instance.HasAction(ToggleAction) && Input.IsActionDown(ToggleAction)) Toggle();
        }

        public static void Toggle()
        {
            IsVisible = !IsVisible;
            if (IsVisible) Rebuild();
        }

        public static void EnsureBuilt()
        {
            if (_groups.Count == 0) Rebuild();
        }

        private static void Rebuild()
        {
            _groups.Clear();
            var byCategory = new Dictionary<string, Group>(StringComparer.OrdinalIgnoreCase);

            foreach (var (_, action) in InputManager.Instance.GetAllActions())
            {
                if (!action.HasBindings) continue;

                string category = string.IsNullOrWhiteSpace(action.Category) ? "Other" : action.Category;
                if (!byCategory.TryGetValue(category, out var group))
                {
                    group = new Group { Title = category };
                    byCategory[category] = group;
                    _groups.Add(group);
                }

                group.Entries.Add((KeyNames.Pretty(action.Bindings[0].GetDisplayName()), string.IsNullOrWhiteSpace(action.Name) ? action.Description : action.Name));
            }

            var debug = new Group { Title = DebugCategory };
            debug.Entries.AddRange(DebugKeys);
            _groups.Add(debug);

            BuildColumns();
            Version++;
        }

        private static void BuildColumns()
        {
            _columns.Clear();
            int columnCount = Math.Clamp(_groups.Count, 1, MaxColumns);
            var heights = new int[columnCount];
            for (int i = 0; i < columnCount; i++) _columns.Add(new List<Group>());

            foreach (var group in _groups)
            {
                int target = 0;
                for (int i = 1; i < columnCount; i++)
                {
                    if (heights[i] < heights[target]) target = i;
                }

                _columns[target].Add(group);
                heights[target] += group.Entries.Count + 2;
            }
        }
    }
}

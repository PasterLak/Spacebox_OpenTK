using Engine;
using Engine.InputPro;
using ImGuiNET;
using System.Numerics;

namespace Spacebox.Game.GUI
{
    public static class InputOverlay
    {
        private const string ToggleAction = "inputOverlay";
        private const string DebugCategory = "Debug";
        private const int MaxColumns = 3;

        private static readonly Vector4 TitleColor = new Vector4(1f, 0.77f, 0.16f, 1f);
        private static readonly Vector4 CategoryColor = new Vector4(1f, 0.77f, 0.16f, 1f);
        private static readonly Vector4 KeyColor = new Vector4(0.45f, 0.85f, 0.95f, 1f);
        private static readonly Vector4 HintColor = new Vector4(0.6f, 0.62f, 0.68f, 1f);

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

        private sealed class Group
        {
            public string Title;
            public readonly List<(string Key, string Name)> Entries = new List<(string, string)>();
        }

        private static readonly List<Group> _groups = new List<Group>();
        private static readonly List<List<Group>> _columns = new List<List<Group>>();

        public static bool IsVisible = false;

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

        public static void OnGUI()
        {
            if (!IsVisible) return;
            if (_groups.Count == 0) Rebuild();

            var io = ImGui.GetIO();
            ImGui.SetNextWindowPos(io.DisplaySize * 0.5f, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSizeConstraints(Vector2.Zero, io.DisplaySize * 0.95f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.06f, 0.08f, 0.11f, 0.9f));
            ImGui.Begin("InputOverlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
                                        ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings);

            ImGui.TextColored(TitleColor, "CONTROLS");
            ImGui.SameLine();
            ImGui.TextColored(HintColor, $"   [{KeyNames.ForAction(ToggleAction) ?? "F6"}] close");
            ImGui.Separator();

            if (ImGui.BeginTable("InputOverlayColumns", _columns.Count, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.PadOuterX))
            {
                ImGui.TableNextRow();
                for (int c = 0; c < _columns.Count; c++)
                {
                    ImGui.TableSetColumnIndex(c);
                    DrawColumn(_columns[c]);
                }
                ImGui.EndTable();
            }

            ImGui.End();
            ImGui.PopStyleColor();
        }

        private static void DrawColumn(List<Group> groups)
        {
            float keyWidth = 0f;
            foreach (var group in groups)
                foreach (var entry in group.Entries)
                    keyWidth = MathF.Max(keyWidth, ImGui.CalcTextSize($"[{entry.Key}]").X);

            keyWidth += ImGui.GetStyle().ItemSpacing.X * 2f;
            float start = ImGui.GetCursorPosX();

            for (int g = 0; g < groups.Count; g++)
            {
                if (g > 0) ImGui.Spacing();
                ImGui.TextColored(CategoryColor, groups[g].Title.ToUpperInvariant());

                foreach (var (key, name) in groups[g].Entries)
                {
                    ImGui.TextColored(KeyColor, $"[{key}]");
                    ImGui.SameLine(start + keyWidth);
                    ImGui.Text(name);
                }
            }

            ImGui.Dummy(new Vector2(keyWidth * 3f, 0f));
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

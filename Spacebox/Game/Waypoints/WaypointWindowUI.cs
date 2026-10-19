using Engine;
using Engine.InputPro;
using ImGuiNET;
using OpenTK.Mathematics;
using Spacebox.Game.Generation;
using Spacebox.Game.GUI;
using Spacebox.Game.GUI.Menu;
using NVector2 = System.Numerics.Vector2;
using NVector3 = System.Numerics.Vector3;
using NVector4 = System.Numerics.Vector4;

namespace Spacebox.Game.Waypoints;

public static class WaypointWindowUI
{
    public const string WindowId = "waypoints";
    private const string ToggleAction = "waypoints";

    private static readonly NVector4 TitleColor = new NVector4(1f, 0.77f, 0.16f, 1f);
    private static readonly NVector4 HintColor = new NVector4(0.62f, 0.64f, 0.7f, 1f);

    private static Tag _selected;
    private static string _nameBuffer = "";
    private static bool _confirmDelete;

    public static bool IsOpen => UIManager.IsOpen(WindowId);

    public static void Update()
    {
        if (Chat.FocusInput || Debug.IsVisible || TagManager.Instance == null) return;
        if (ImGui.GetIO().WantTextInput) return;
        if (!InputManager.Instance.HasAction(ToggleAction) || !Input.IsActionDown(ToggleAction)) return;

        if (IsOpen)
        {
            if (UIManager.IsTop(WindowId)) UIManager.CloseTop();
        }
        else if (!UIManager.IsUIMode)
        {
            _confirmDelete = false;
            UIManager.Open(WindowId);
        }
    }

    public static void Reset()
    {
        _selected = null;
        _nameBuffer = "";
        _confirmDelete = false;
    }

    public static void OnGUI()
    {
        if (!IsOpen) return;

        var display = ImGui.GetIO().DisplaySize;
        float width = MathF.Max(display.X * 0.34f, 520f);
        float height = display.Y * 0.52f;
        var position = GameMenu.CenterNextWindow(width, height);

        ImGui.SetNextWindowPos(position);
        ImGui.SetNextWindowSize(new NVector2(width, height));
        ImGui.Begin("Waypoints", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar);
        GameMenu.DrawElementColors(position, new NVector2(width, height), display.Y, 0.005f);

        float padding = width * 0.035f;
        float buttonHeight = height * 0.09f;
        ImGui.SetCursorPos(new NVector2(padding, padding * 0.7f));
        ImGui.TextColored(TitleColor, "WAYPOINTS");
        ImGui.SameLine();
        ImGui.TextColored(HintColor, "   Only you can see your waypoints.");

        var waypoints = WaypointService.Collect();
        if (_selected != null && !WaypointService.Contains(_selected)) Select(null);

        float top = ImGui.GetCursorPosY() + padding * 0.3f;
        float contentHeight = height - top - buttonHeight - padding * 1.6f;
        float listWidth = (width - padding * 3f) * 0.55f;
        float editorWidth = width - padding * 3f - listWidth;

        ImGui.SetCursorPos(new NVector2(padding, top));
        DrawList(waypoints, new NVector2(listWidth, contentHeight));

        ImGui.SetCursorPos(new NVector2(padding * 2f + listWidth, top));
        DrawEditor(new NVector2(editorWidth, contentHeight));

        float buttonsY = height - buttonHeight - padding * 0.8f;
        float buttonWidth = (width - padding * 3f) * 0.5f;
        var player = World.Instance?.Player;
        GameMenu.ButtonWithBackground2("Add at my position", new NVector2(buttonWidth, buttonHeight), new NVector2(padding, buttonsY), () =>
        {
            if (player == null) return;
            Select(WaypointService.Add(player.Position));
        });
        GameMenu.ButtonWithBackground2("Close", new NVector2(buttonWidth, buttonHeight), new NVector2(padding * 2f + buttonWidth, buttonsY), () => UIManager.CloseTop());

        ImGui.End();
    }

    private static void DrawList(IReadOnlyList<Tag> waypoints, NVector2 size)
    {
        ImGui.BeginChild("WaypointList", size, ImGuiChildFlags.Border);

        if (waypoints.Count == 0)
        {
            ImGui.PushTextWrapPos(size.X - 8f);
            ImGui.TextColored(HintColor, "No waypoints yet. Fly to a place you want to find again and press \"Add at my position\".");
            ImGui.PopTextWrapPos();
        }

        var player = World.Instance?.Player;
        float square = ImGui.GetTextLineHeight() * 0.8f;
        var drawList = ImGui.GetWindowDrawList();

        for (int i = 0; i < waypoints.Count; i++)
        {
            var tag = waypoints[i];
            var rowStart = ImGui.GetCursorScreenPos();
            if (ImGui.Selectable($"##waypoint{tag.Id}", tag == _selected, ImGuiSelectableFlags.SpanAvailWidth)) Select(tag);

            var color = new NVector4(tag.Color.R, tag.Color.G, tag.Color.B, 1f);
            float y = rowStart.Y + (ImGui.GetTextLineHeight() - square) * 0.5f;
            drawList.AddRectFilled(new NVector2(rowStart.X + 2f, y), new NVector2(rowStart.X + 2f + square, y + square), ImGui.GetColorU32(color), 2f);

            ImGui.SameLine(square + ImGui.GetStyle().ItemSpacing.X * 2f);
            if (tag.Visible) ImGui.Text(tag.Text);
            else ImGui.TextColored(HintColor, tag.Text + " (hidden)");

            if (player != null)
            {
                string distance = FormatDistance(Vector3.Distance(player.Position, tag.WorldPosition));
                ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - ImGui.CalcTextSize(distance).X);
                ImGui.TextColored(HintColor, distance);
            }
        }

        ImGui.EndChild();
    }

    private static void DrawEditor(NVector2 size)
    {
        ImGui.BeginChild("WaypointEditor", size, ImGuiChildFlags.Border);

        if (_selected == null)
        {
            ImGui.PushTextWrapPos(size.X - 8f);
            ImGui.TextColored(HintColor, "Select a waypoint to change its name and color.");
            ImGui.PopTextWrapPos();
            ImGui.EndChild();
            return;
        }

        float inner = size.X - ImGui.GetStyle().WindowPadding.X * 2f;
        ImGui.Text("Name");
        ImGui.SetNextItemWidth(inner);
        if (ImGui.InputText("##waypointName", ref _nameBuffer, (uint)WaypointDefaults.MaxNameLength)) WaypointService.Rename(_selected, _nameBuffer);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            _nameBuffer = _selected.Text;
            WaypointService.Save();
        }

        ImGui.Spacing();
        ImGui.Text("Color");
        DrawPalette(inner);

        var custom = new NVector3(_selected.Color.R, _selected.Color.G, _selected.Color.B);
        ImGui.SetNextItemWidth(inner);
        if (ImGui.ColorEdit3("##waypointColor", ref custom, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel))
            WaypointService.SetColor(_selected, new Color4(custom.X, custom.Y, custom.Z, 1f));
        if (ImGui.IsItemDeactivatedAfterEdit()) WaypointService.Save();
        ImGui.SameLine();
        ImGui.TextColored(HintColor, "Custom color");

        ImGui.Spacing();
        bool visible = _selected.Visible;
        if (ImGui.Checkbox("Show marker", ref visible))
        {
            WaypointService.SetVisible(_selected, visible);
            WaypointService.Save();
        }

        var player = World.Instance?.Player;
        if (player != null)
        {
            ImGui.Spacing();
            ImGui.TextColored(HintColor, $"Distance: {FormatDistance(Vector3.Distance(player.Position, _selected.WorldPosition))}");
        }

        ImGui.Spacing();
        ImGui.PushStyleColor(ImGuiCol.Button, new NVector4(0.55f, 0.16f, 0.16f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new NVector4(0.7f, 0.2f, 0.2f, 1f));
        if (ImGui.Button(_confirmDelete ? "Click again to delete" : "Delete", new NVector2(inner, ImGui.GetFrameHeight() * 1.2f)))
        {
            if (_confirmDelete)
            {
                WaypointService.Remove(_selected);
                Select(null);
            }
            else
            {
                _confirmDelete = true;
            }
        }
        ImGui.PopStyleColor(2);

        ImGui.EndChild();
    }

    private static void DrawPalette(float width)
    {
        var palette = WaypointDefaults.Palette;
        float spacing = ImGui.GetStyle().ItemSpacing.X;
        float button = MathF.Min(ImGui.GetFrameHeight(), (width - spacing * (palette.Length - 1)) / palette.Length);

        for (int i = 0; i < palette.Length; i++)
        {
            if (i > 0) ImGui.SameLine();
            var color = new NVector4(palette[i].R, palette[i].G, palette[i].B, 1f);
            if (ImGui.ColorButton($"##palette{i}", color, ImGuiColorEditFlags.NoTooltip, new NVector2(button, button)))
            {
                WaypointService.SetColor(_selected, palette[i]);
                WaypointService.Save();
            }
        }
    }

    private static void Select(Tag tag)
    {
        _selected = tag;
        _nameBuffer = tag?.Text ?? "";
        _confirmDelete = false;
    }

    private static string FormatDistance(float meters)
    {
        return meters >= 1000f ? $"{meters / 1000f:0.0} km" : $"{meters:0} m";
    }
}

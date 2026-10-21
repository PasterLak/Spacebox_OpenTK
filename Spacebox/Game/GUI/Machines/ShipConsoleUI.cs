using System.Numerics;
using Engine;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Ships;
using Spacebox.Network;

namespace Spacebox.Game.GUI;

public static class ShipConsoleUI
{
    private const string WindowId = "shipConsole";

    private static readonly Vector4 TitleColor = new Vector4(1f, 1f, 0f, 1f);
    private static readonly Vector4 HintColor = new Vector4(0.7f, 0.7f, 0.7f, 1f);
    private static readonly Vector4 WaitingColor = new Vector4(1f, 0.6f, 0f, 1f);

    private static readonly int[] _groupCounts = new int[ConsoleBindings.GroupColors.Length];
    private static int _waitingGroup = -1;
    private static bool _skipOpenKey;

    public static bool IsOpen => UIManager.IsOpen(WindowId);

    public static void HandleInput()
    {
        if (!ShipPilot.IsPiloting || Debug.IsVisible || Chat.FocusInput) return;
        if (UIManager.IsUIMode || !Input.IsActionDown("inventory")) return;

        ShipBindingTool.Exit();
        _waitingGroup = -1;
        _skipOpenKey = true;
        UIManager.Open(WindowId);
    }

    public static void OnGUI()
    {
        if (!IsOpen)
        {
            DrawHud();
            return;
        }

        var console = ShipPilot.Console;
        var entity = console?.Entity;
        var bindings = entity?.ShipControl.GetBindings(console.LocalCell);
        if (bindings == null)
        {
            Close();
            return;
        }

        if (_waitingGroup >= 0)
        {
            if (TryReadKey(out int key))
            {
                ShipNetwork.RequestSetKey(entity, console.LocalCell, _waitingGroup, key);
                ShipKeyMemory.Remember(console, bindings, _waitingGroup, key);
                _waitingGroup = -1;
            }
        }
        else if (_skipOpenKey)
        {
            _skipOpenKey = false;
        }
        else if (Input.IsKeyDown(Keys.Escape) || Input.IsActionDown("inventory"))
        {
            Close();
            return;
        }

        var displaySize = ImGui.GetIO().DisplaySize;
        float windowWidth = displaySize.Y * 0.55f;
        float windowHeight = displaySize.Y * 0.6f;
        float padding = windowHeight / 24f;
        float contentWidth = windowWidth - padding * 2f;

        var windowPos = GameMenu.CenterNextWindow3(windowWidth, windowHeight);
        var windowSize = new Vector2(windowWidth, windowHeight);

        ImGui.Begin("ShipConsole", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                                   ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar |
                                   ImGuiWindowFlags.NoScrollWithMouse);

        GameMenu.DrawElementColors(windowPos, windowSize, displaySize.Y);

        ImGui.SetCursorPos(new Vector2(padding, padding));
        ImGui.TextColored(TitleColor, GameAssets.GetBlockDataById(console.Id)?.Name ?? "Ship Console");

        ImGui.SetCursorPosX(padding);
        DrawShipInfo(console);

        ImGui.SetCursorPosX(padding);
        ImGui.Separator();
        DrawGroups(bindings, padding, contentWidth);

        ImGui.SetCursorPosX(padding);
        ImGui.Separator();
        DrawActions(padding, contentWidth);

        ImGui.End();
    }

    private static void DrawHud()
    {
        var console = ShipPilot.Console;
        if (console?.Entity == null || UIManager.IsUIMode) return;

        ShipBindingTool.DrawLabels();

        var displaySize = ImGui.GetIO().DisplaySize;
        float speed = console.Entity.Ship?.Motion.Velocity.Length ?? 0f;
        string text = ShipBindingTool.IsActive
            ? $"BINDING  {ShipBindingTool.DescribeHovered() ?? "Look at a ship block"}\nLMB / wheel: change group   RMB: back to seat"
            : $"Speed {speed:0.0} m/s\nInventory key: console   Use: leave";

        var size = ImGui.CalcTextSize(text);
        ImGui.SetNextWindowPos(new Vector2((displaySize.X - size.X) * 0.5f, displaySize.Y * 0.78f));
        ImGui.SetNextWindowBgAlpha(0.35f);
        ImGui.Begin("ShipHud", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize |
                               ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoFocusOnAppearing |
                               ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoNav);
        ImGui.TextUnformatted(text);
        ImGui.End();
    }

    public static void Reset()
    {
        _waitingGroup = -1;
        _skipOpenKey = false;
    }

    private static void DrawShipInfo(ShipConsoleBlock console)
    {
        var control = console.Entity.ShipControl;
        var ship = console.Entity.Ship;
        float speed = ship?.Motion.Velocity.Length ?? 0f;

        ImGui.Text($"Thrusters: {control.Thrusters.Count}   Cannons: {control.Cannons.Count}   Mass: {console.Entity.Mass:0}");
        ImGui.Text($"Speed: {speed:0.0} m/s");
        if (console.Data.Flight)
            ImGui.TextColored(HintColor, "Movement keys fly, roll keys roll, mouse steers (hold Alt to look around), sprint brakes");
    }

    private static void DrawGroups(ConsoleBindings bindings, float padding, float width)
    {
        Array.Clear(_groupCounts);
        foreach (var pair in bindings.Bound)
            if (pair.Value < _groupCounts.Length) _groupCounts[pair.Value]++;

        float rowHeight = ImGui.GetFrameHeight();
        for (int group = 0; group < bindings.GroupCount; group++)
        {
            ImGui.SetCursorPosX(padding);
            var color = ConsoleBindings.GroupColors[group % ConsoleBindings.GroupColors.Length];
            ImGui.ColorButton($"##groupColor{group}", new Vector4(color.R, color.G, color.B, 1f), ImGuiColorEditFlags.NoTooltip, new Vector2(rowHeight, rowHeight));
            ImGui.SameLine();
            ImGui.Text($"Group {group + 1}: {_groupCounts[group]} blocks");
            ImGui.SameLine(width * 0.6f);

            string label = _waitingGroup == group ? "Press a key..." : ShipKeys.GetName(bindings.GetKey(group));
            if (_waitingGroup == group) ImGui.PushStyleColor(ImGuiCol.Text, WaitingColor);
            if (ImGui.Button($"{label}##groupKey{group}", new Vector2(width * 0.4f, 0f)))
                _waitingGroup = _waitingGroup == group ? -1 : group;
            if (_waitingGroup == group) ImGui.PopStyleColor();
        }

        ImGui.SetCursorPosX(padding);
        ImGui.TextColored(HintColor, bindings.Camera.HasValue ? "Camera: bound" : "Camera: not bound");
    }

    private static void DrawActions(float padding, float width)
    {
        float spacing = ImGui.GetStyle().ItemSpacing.X;
        float buttonWidth = (width - spacing) * 0.5f;

        ImGui.SetCursorPosX(padding);
        if (ImGui.Button("Bind blocks", new Vector2(buttonWidth, 0f)))
        {
            Close();
            ShipBindingTool.Enter();
        }

        ImGui.SameLine();
        if (ImGui.Button("Leave seat", new Vector2(buttonWidth, 0f)))
        {
            Close();
            ShipPilot.Unmount(InventoryUI.Player);
        }

        ImGui.SetCursorPosX(padding);
        ImGui.TextColored(HintColor, "Binding: LMB or mouse wheel changes group (camera: toggle), RMB exits");
    }

    private static bool TryReadKey(out int key)
    {
        key = ShipKeys.None;
        if (Input.IsKeyDown(Keys.Escape) || Input.IsKeyDown(Keys.Backspace) || Input.IsKeyDown(Keys.Delete))
            return true;

        if (Input.IsMouseButtonDown(MouseButton.Left) && !ImGui.IsAnyItemHovered())
        {
            key = ShipKeys.MouseLeft;
            return true;
        }

        if (Input.IsMouseButtonDown(MouseButton.Right))
        {
            key = ShipKeys.MouseRight;
            return true;
        }

        var keyboard = Input.Keyboard;
        foreach (Keys candidate in KeyValues)
        {
            if (!keyboard.IsKeyPressed(candidate)) continue;
            key = (int)candidate;
            return true;
        }

        return false;
    }

    private static readonly Keys[] KeyValues = BuildKeyValues();

    private static Keys[] BuildKeyValues()
    {
        var values = (Keys[])Enum.GetValues(typeof(Keys));
        var result = new List<Keys>(values.Length);
        for (int i = 0; i < values.Length; i++)
            if (values[i] > 0 && !result.Contains(values[i])) result.Add(values[i]);
        return result.ToArray();
    }

    private static void Close()
    {
        _waitingGroup = -1;
        UIManager.CloseAll();
    }
}

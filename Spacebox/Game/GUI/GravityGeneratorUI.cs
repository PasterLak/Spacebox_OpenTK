using System.Numerics;
using Engine;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI;

public static class GravityGeneratorUI
{
    private const string WindowId = "gravityGenerator";
    private const float PresetTolerance = 0.005f;

    private static readonly Vector4 TitleColor = new Vector4(1f, 1f, 0f, 1f);
    private static readonly Vector4 NoPowerColor = new Vector4(1f, 0f, 0f, 1f);
    private static readonly Vector4 WarningColor = new Vector4(1f, 0.6f, 0f, 1f);
    private static readonly Vector4 WorkingColor = new Vector4(0f, 1f, 0.4f, 1f);
    private static readonly Vector4 SelectedPresetColor = new Vector4(0.25f, 0.45f, 0.8f, 1f);

    private static GravityGeneratorBlock _block;
    private static string _blockName = "Gravity Generator";

    public static void Open(GravityGeneratorBlock block, LocalAstronaut player)
    {
        _block = block;
        _blockName = GameAssets.GetBlockDataById(block.Id)?.Name ?? _blockName;
        UIManager.Open(WindowId);
    }

    public static void OnGUI()
    {
        if (!UIManager.IsOpen(WindowId) || _block == null) return;

        if (Input.IsKeyDown(Keys.Escape) || Input.IsActionDown("inventory"))
        {
            Close();
            return;
        }

        var displaySize = ImGui.GetIO().DisplaySize;
        float windowWidth = displaySize.Y * 0.5f;
        float windowHeight = displaySize.Y * 0.42f;
        float padding = windowHeight / 18f;
        float contentWidth = windowWidth - padding * 2f;

        var windowPos = GameMenu.CenterNextWindow3(windowWidth, windowHeight);
        var windowSize = new Vector2(windowWidth, windowHeight);

        ImGui.Begin("GravityGenerator", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                                        ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar |
                                        ImGuiWindowFlags.NoScrollWithMouse);

        GameMenu.DrawElementColors(windowPos, windowSize, displaySize.Y);

        ImGui.SetCursorPos(new Vector2(padding, padding));
        ImGui.TextColored(TitleColor, _blockName);

        var data = _block.Data;
        int radius = _block.RadiusSetting;
        float strength = _block.Strength;
        bool isEnabled = _block.IsGravityEnabled;
        bool insideOnly = _block.InsideOnly;

        ImGui.SetCursorPos(new Vector2(padding, padding * 3f));
        ImGui.Checkbox("Gravity", ref isEnabled);
        ImGui.SetCursorPosX(padding);
        ImGui.Checkbox("Only inside closed rooms", ref insideOnly);

        ImGui.SetCursorPosX(padding);
        ImGui.Text("Radius");
        ImGui.SetCursorPosX(padding);
        ImGui.SetNextItemWidth(contentWidth);
        ImGui.SliderInt("##gravityRadius", ref radius, data.MinRadius, data.MaxRadius, "%d blocks");

        ImGui.SetCursorPosX(padding);
        ImGui.Text("Gravity");
        ImGui.SetCursorPosX(padding);
        ImGui.SetNextItemWidth(contentWidth);
        ImGui.SliderFloat("##gravityStrength", ref strength, data.MinStrength, data.MaxStrength, "%.2f m/s2");

        ImGui.SetCursorPosX(padding);
        DrawPresets(ref strength, contentWidth);

        _block.Configure(radius, strength, isEnabled, insideOnly);
        if (!ImGui.IsAnyItemActive()) _block.ApplyPendingPower();

        ImGui.SetCursorPosX(padding);
        ImGui.Text($"Power needed: {_block.RequiredPower} EU");
        DrawStatus(padding);

        ImGui.End();
    }

    private static void DrawPresets(ref float strength, float width)
    {
        var presets = _block.Data.Presets;
        if (presets.Count == 0) return;

        float spacing = ImGui.GetStyle().ItemSpacing.X;
        float buttonWidth = (width - spacing * (presets.Count - 1)) / presets.Count;

        for (int i = 0; i < presets.Count; i++)
        {
            var preset = presets[i];
            if (i > 0) ImGui.SameLine();

            bool isSelected = MathF.Abs(strength - preset.Strength) < PresetTolerance;
            if (isSelected) ImGui.PushStyleColor(ImGuiCol.Button, SelectedPresetColor);
            if (ImGui.Button(preset.Name, new Vector2(buttonWidth, 0f))) strength = preset.Strength;
            if (isSelected) ImGui.PopStyleColor();
        }
    }

    private static void DrawStatus(float x)
    {
        ImGui.SetCursorPosX(x);

        if (!_block.IsGravityEnabled)
        {
            ImGui.TextDisabled("Off");
            return;
        }

        if (!_block.IsActive)
        {
            ImGui.TextColored(NoPowerColor, "NO POWER");
            return;
        }

        if (_block.InsideOnly)
        {
            _block.UpdateRoom();
            if (!_block.Room.IsSealed)
            {
                ImGui.TextColored(WarningColor, "Room is not closed");
                return;
            }

            ImGui.TextColored(WorkingColor, $"Working (room: {_block.Room.CellCount} blocks)");
            return;
        }

        ImGui.TextColored(WorkingColor, "Working");
    }

    private static void Close()
    {
        _block?.ApplyPendingPower();
        _block = null;
        UIManager.CloseAll();
    }

    public static void Reset()
    {
        _block = null;
    }
}

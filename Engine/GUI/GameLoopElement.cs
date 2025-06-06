﻿using ImGuiNET;


namespace Engine.GUI
{
    internal class GameLoopElement : OverlayElement
    {
       
        public override void OnGUIText()
        {
            ImGui.Text($" ");
            ImGui.Text($"[GAME LOOP DEBUG]");
            ImGui.Text($" ");
            ImGui.Text($"Render Time: {Time.RenderTime:F2} ms");
            ImGui.Text($"Avg Render Time: {Time.AverageRenderTime:F2} ms");
            ImGui.Text($"Update Time: {Time.UpdateTime:F2} ms");
            ImGui.Text($"Avg Update Time: {Time.AverageUpdateTime:F2} ms");
            ImGui.Text($"OnGUI Time: {Time.OnGUITime:F2} ms");
            ImGui.Text($"Avg OnGUI Time: {Time.AverageOnGUITime:F2} ms");
            ImGui.Text($" ");
            ImGui.Text($"Render: {Time.RenderTimePercent} %, Update: {Time.UpdateTimePercent}%, OnGUI: {Time.OnGUITimePercent}%");
            ImGui.Text($" ");
            ImGui.Text($"Console On (F1): {Debug.IsVisible}");
            ImGui.Text($"Debug On (F4): {VisualDebug.Enabled}");
            ImGui.Text($"FrameLimiter On (F7): {(FrameLimiter.TargetFPS == 120 ? true : false)}");
            ImGui.Text($"Wireframe Mode (F10) ");
            ImGui.Text($" ");
          
           /* if (GameBlocks.IsInitialized)
            {
                var size = GameBlocks.AtlasBlocks.SizeBlocks * GameBlocks.AtlasBlocks.BlockSizePixels;
                var size2 = GameBlocks.AtlasItems.SizeBlocks * GameBlocks.AtlasItems.BlockSizePixels;
                ImGui.Text($"Atlas Blocks: {size}x{size}, Items: {size2}x{size2}");
            }*/
        }
    }
}

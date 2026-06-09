using Engine;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Physics;
using Spacebox.Game.Resource;
using System.Numerics;

namespace Spacebox.Game.GUI
{
    public class AnalyzerUI
    {
        private static AnalyzerBlock analyzerBlock;
        private static SpaceEntity currentEntity;
        private static string blockName = "Analyzer";
        private static readonly Random random = new Random();

        private static bool _hasPower = false;

        public static void Initialize()
        {
        }

        public static void Open(AnalyzerBlock block, ref HitInfo hit)
        {
            analyzerBlock = block;
            currentEntity = hit.chunk.SpaceEntity;
            blockName = GameAssets.GetBlockDataById(block.Id).Name;

            UIManager.Open("analyzer");
        }

        public static void Close()
        {
            analyzerBlock = null;
            currentEntity = null;
            UIManager.CloseTop();
        }

        private static void CheckPower()
        {
            if (currentEntity == null || analyzerBlock == null)
            {
                _hasPower = false;
                return;
            }

            _hasPower = analyzerBlock.IsActive;
        }

        public static void OnGUI()
        {
            if (!UIManager.IsOpen("analyzer") || analyzerBlock == null || currentEntity == null) return;

            if (Input.IsActionDown("inventory") || Input.IsKeyDown(Keys.Escape))
            {
                Close();
                return;
            }

            CheckPower();

            var io = ImGui.GetIO();
            var displaySize = io.DisplaySize;

            float windowWidth = displaySize.X * 0.25f;
            float windowHeight = displaySize.Y * 0.45f;

            Vector2 windowPos = new Vector2(
                (displaySize.X - windowWidth) * 0.5f,
                (displaySize.Y - windowHeight) * 0.5f);

            var padding = windowHeight / 25f;
            var paddingV = new Vector2(padding, padding);

            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));

            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoCollapse |
                                           ImGuiWindowFlags.NoDecoration |
                                           ImGuiWindowFlags.NoScrollbar;

            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 12f);

            if (ImGui.Begin("Analyzer", windowFlags))
            {
                GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y);

                ImGui.SetCursorPos(paddingV);
                ImGui.TextColored(new Vector4(0, 1, 1, 1), blockName);

                ImGui.SetCursorPos(paddingV + new Vector2(0, padding * 1.5f));

                Vector2 contentAvail = ImGui.GetContentRegionAvail();
                contentAvail.Y -= padding;

                if (ImGui.BeginChild("Content", contentAvail, ImGuiChildFlags.None))
                {
                    if (!_hasPower)
                    {
                        string noPowerText = "NO POWER";
                        var textSize = ImGui.CalcTextSize(noPowerText);
                        ImGui.SetCursorPos(new Vector2((contentAvail.X - textSize.X) * 0.5f, (contentAvail.Y - textSize.Y) * 0.5f));
                        ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), noPowerText);
                    }
                    else
                    {
                        long currentAbsoluteTick = GameTime.Day * 24000L + GameTime.DayTick;

                        float currentWindowWidth = ImGui.GetWindowWidth();
                        float buttonHeight = displaySize.Y * 0.035f;
                        float buttonWidth = currentWindowWidth * 0.85f;
                        float buttonOffsetX = (currentWindowWidth - buttonWidth) * 0.5f;

                        if (!analyzerBlock.IsScanning && !analyzerBlock.ScanComplete)
                        {
                            ImGui.TextWrapped("Scanning allows determining the approximate composition of the asteroid.");
                            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), $"Scan time: {analyzerBlock.ScanDurationTicks / 20f} s.");
                            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), $"Deviation: {analyzerBlock.DeviationPercentage}%");

                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding * 0.5f);

                            ImGui.SetCursorPosX(buttonOffsetX);
                            if (ImGui.Button("Scan Asteroid", new Vector2(buttonWidth, buttonHeight)))
                            {
                                analyzerBlock.IsScanning = true;
                                analyzerBlock.ScanStartAbsoluteTick = currentAbsoluteTick;
                            }
                        }
                        else if (analyzerBlock.IsScanning)
                        {
                            float progress = (currentAbsoluteTick - analyzerBlock.ScanStartAbsoluteTick) / (float)analyzerBlock.ScanDurationTicks;

                            if (progress >= 1f)
                            {
                                progress = 1f;
                                CompleteScan();
                            }

                            string scanText = "Scanning in progress...";
                            float textWidth = ImGui.CalcTextSize(scanText).X;
                            ImGui.SetCursorPosX((currentWindowWidth - textWidth) * 0.5f);
                            ImGui.TextColored(new Vector4(1, 1, 0, 1), scanText);

                            ImGui.SetCursorPosX(buttonOffsetX);
                            ImGui.ProgressBar(progress, new Vector2(buttonWidth, buttonHeight * 0.8f), $"{(int)(progress * 100)}%");
                        }
                        else if (analyzerBlock.ScanComplete)
                        {
                            ImGui.SetCursorPosX(buttonOffsetX);
                            if (ImGui.Button("Rescan", new Vector2(buttonWidth, buttonHeight)))
                            {
                                analyzerBlock.ScanComplete = false;
                                analyzerBlock.IsScanning = true;
                                analyzerBlock.ScanStartAbsoluteTick = currentAbsoluteTick;
                                analyzerBlock.CachedMinerals.Clear();
                            }

                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding * 0.5f);
                            ImGui.TextColored(new Vector4(1f, 1f, 0f, 1f), "Analysis Results:");

                            string formattedMass = analyzerBlock.CachedMass.ToString("N0").Replace(",", ".");
                            ImGui.Text($"Estimated Mass: {formattedMass} tn");
                            ImGui.Separator();

                            ImGui.Text("Detected Minerals:");

                            if (analyzerBlock.CachedMinerals.Count == 0)
                            {
                                ImGui.TextColored(new Vector4(1, 0, 0, 1), "No minerals detected.");
                            }
                            else
                            {
                                var sortedMinerals = analyzerBlock.CachedMinerals.OrderByDescending(k => k.Value).ToList();

                                string approxSign = analyzerBlock.DeviationPercentage == 0f ? "" : "~";
                                float iconSize = ImGui.GetTextLineHeight() * 1.4f;

                                if (ImGui.BeginChild("ResourcesList", ImGui.GetContentRegionAvail(), ImGuiChildFlags.None))
                                {
                                    float innerAvailX = ImGui.GetContentRegionAvail().X;

                                    foreach (var kvp in sortedMinerals)
                                    {
                                        float startY = ImGui.GetCursorPosY();

                                        IntPtr icon = IntPtr.Zero;
                                        if (GameAssets.TryGetItemByBlockID(kvp.Key, out Item item) && item != null)
                                        {
                                            icon = item.IconTextureId;
                                        }

                                        if (icon != IntPtr.Zero)
                                        {
                                            ImGui.Image(icon, new Vector2(iconSize, iconSize));
                                            ImGui.SameLine();
                                        }
                                        else
                                        {
                                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + iconSize + ImGui.GetStyle().ItemSpacing.X);
                                        }

                                        ImGui.SetCursorPosY(startY + (iconSize - ImGui.GetTextLineHeight()) * 0.5f);

                                        var bData = GameAssets.GetBlockDataById(kvp.Key);
                                        string minName = bData != null ? bData.Name : $"Unknown ID: {kvp.Key}";
                                        ImGui.TextColored(new Vector4(0.8f, 1f, 0.8f, 1f), minName);

                                        string countText = $"{approxSign}{kvp.Value}";
                                        float countWidth = ImGui.CalcTextSize(countText).X;

                                        ImGui.SameLine(innerAvailX - countWidth - ImGui.GetStyle().ItemSpacing.X);
                                        ImGui.TextColored(new Vector4(0, 1, 0.5f, 1), countText);

                                        ImGui.SetCursorPosY(startY + iconSize + ImGui.GetStyle().ItemSpacing.Y);
                                    }
                                }
                                ImGui.EndChild();
                            }
                        }
                    }
                }
                ImGui.EndChild();
            }
            ImGui.End();
            ImGui.PopStyleVar();
        }

        private static void CompleteScan()
        {
            analyzerBlock.IsScanning = false;
            analyzerBlock.ScanComplete = true;

            analyzerBlock.CachedMass = currentEntity.Mass;

            analyzerBlock.CachedMinerals.Clear();
            var exactCounts = new Dictionary<short, int>();

            foreach (var chunk in currentEntity.Chunks)
            {
                if (chunk.IsDisposed) continue;

                for (int x = 0; x < Chunk.Size; x++)
                {
                    for (int y = 0; y < Chunk.Size; y++)
                    {
                        for (int z = 0; z < Chunk.Size; z++)
                        {
                            var block = chunk.Blocks[x, y, z];
                            if (block != null && block is MineralBlock)
                            {
                                if (!exactCounts.ContainsKey(block.Id))
                                    exactCounts[block.Id] = 0;

                                exactCounts[block.Id]++;
                            }
                        }
                    }
                }
            }

            float deviation = analyzerBlock.DeviationPercentage / 100f;

            foreach (var kvp in exactCounts)
            {
                double mineralMultiplier = deviation == 0f ? 1.0 : 1.0 + (random.NextDouble() * 2.0 - 1.0) * deviation;
                int estimatedCount = (int)(kvp.Value * mineralMultiplier);

                if (estimatedCount < 0) estimatedCount = 0;

                if (estimatedCount > 0)
                {
                    analyzerBlock.CachedMinerals[kvp.Key] = estimatedCount;
                }
            }
        }
    }
}
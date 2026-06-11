using Engine;
using Engine.Audio;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.GUI.Menu;
using Spacebox.Game.Physics;
using Spacebox.Game.Resource;
using Spacebox.GUI;
using System;
using System.Linq;
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
        private static bool _wasVisible = false;

        private static ulong _lastCheckedMass = ulong.MaxValue;
        private static int _lastBlockCount = 0;

        private static AudioSource openSound;
        private static AudioSource closeSound;
        private static AudioSource scanSound;
        private static AudioSource finishSound;
        private static AudioSource clickSound;

        public static void Initialize()
        {
            if (openSound == null)
            {
                openSound = new AudioSource(Resources.Load<AudioClip>("openBlock1"));
                openSound.Volume = 1f;
            }
            if (closeSound == null)
            {
                closeSound = new AudioSource(Resources.Load<AudioClip>("openBlock4"));
                closeSound.Volume = 1f;
            }
            if (scanSound == null)
            {
                scanSound = new AudioSource(Resources.Load<AudioClip>("analyzer_scanning"));
                scanSound.Volume = 1f;
                scanSound.IsLooped = true;
            }
            if (finishSound == null)
            {
                finishSound = new AudioSource(Resources.Load<AudioClip>("analyzer_complete"));
                finishSound.Volume = 0.5f;
            }
            if (clickSound == null)
            {
                clickSound = new AudioSource(Resources.Load<AudioClip>("click1"));
                clickSound.Volume = 0.8f;
            }
        }

        public static void Open(AnalyzerBlock block, ref HitInfo hit)
        {
            Initialize();
            analyzerBlock = block;
            currentEntity = hit.chunk.SpaceEntity;
            blockName = GameAssets.GetBlockDataById(block.Id).Name;

            CountBlocksIfMassChanged();

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

        private static void ApplyButtonStyles()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1.0f);
            ImGui.PushStyleColor(ImGuiCol.Button, Theme.Colors.Deep.ToUInt());
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
        }

        private static void PopButtonStyles()
        {
            ImGui.PopStyleColor(4);
            ImGui.PopStyleVar();
        }

        private static void CountBlocksIfMassChanged()
        {
            if (currentEntity == null) return;
            if (currentEntity.Mass == _lastCheckedMass) return;

            _lastCheckedMass = currentEntity.Mass;
            _lastBlockCount = 0;

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
                            if (block != null && block.Id != 0)
                            {
                                _lastBlockCount++;
                            }
                        }
                    }
                }
            }
        }

        public static void OnGUI()
        {
            bool isVisible = UIManager.IsOpen("analyzer");

            if (isVisible != _wasVisible)
            {
                if (isVisible)
                {
                    openSound?.Play();
                    if (analyzerBlock != null && analyzerBlock.IsScanning)
                    {
                        if (scanSound != null && !scanSound.IsPlaying)
                        {
                            scanSound.Play();
                        }
                    }
                }
                else
                {
                    closeSound?.Play();
                    scanSound?.Stop();
                }
                _wasVisible = isVisible;
            }

            if (!isVisible || analyzerBlock == null || currentEntity == null) return;

            if (Input.IsActionDown("inventory") || Input.IsKeyDown(Keys.Escape))
            {
                Close();
                return;
            }

            CheckPower();
            CountBlocksIfMassChanged();

            var io = ImGui.GetIO();
            var displaySize = io.DisplaySize;

            float windowWidth = displaySize.X * 0.25f;
            float windowHeight = displaySize.Y * 0.45f;

            Vector2 windowPos = new Vector2(
                (displaySize.X - windowWidth) * 0.5f,
                (displaySize.Y - windowHeight) * 0.5f);

            var padding = windowHeight * 0.04f;
            var paddingV = new Vector2(padding, padding);

            float buttonHeight = displaySize.Y * 0.035f;
            float buttonAreaHeight = buttonHeight + padding * 2f;

            float mainScrollbarSize = Math.Max(windowWidth * 0.025f, 4f);
            float innerScrollbarSize = Math.Max(windowWidth * 0.015f, 2f);

            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(windowWidth, windowHeight));

            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoCollapse |
                                           ImGuiWindowFlags.NoDecoration |
                                           ImGuiWindowFlags.NoScrollbar;

            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, mainScrollbarSize);

            if (ImGui.Begin("Analyzer", windowFlags))
            {
                GameMenu.DrawElementColors(windowPos, new Vector2(windowWidth, windowHeight), displaySize.Y);

                ImGui.SetCursorPos(paddingV);
                ImGui.TextColored(new Vector4(1.0f, 1.0f, 0.0f, 1f), blockName);

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + windowHeight * 0.01f);

                ImGui.SetCursorPosX(padding);
                ImGui.TextWrapped("Scanning allows determining the approximate composition of the asteroid.");

                float totalScanSeconds = (_lastBlockCount / 1000f) * analyzerBlock.TimePer1000Blocks;
                if (totalScanSeconds <= analyzerBlock.TimePer1000Blocks) totalScanSeconds = analyzerBlock.TimePer1000Blocks;
                long totalTicksNeeded = (long)(totalScanSeconds * 20f);

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + windowHeight * 0.01f);
                ImGui.SetCursorPosX(padding);
                ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 1f), $"Scan time: {totalScanSeconds:0.0} s.");

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + windowHeight * 0.005f);
                ImGui.SetCursorPosX(padding);
                ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 1f), $"Deviation: {analyzerBlock.DeviationPercentage}%");

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + windowHeight * 0.01f);
                ImGui.Separator();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + windowHeight * 0.01f);

                Vector2 contentAvail = ImGui.GetContentRegionAvail();
                float middleHeight = !_hasPower ? contentAvail.Y - padding : contentAvail.Y - buttonAreaHeight;

                ImGui.SetCursorPosX(padding);
                if (ImGui.BeginChild("MiddleContent", new Vector2(contentAvail.X - padding * 2f, middleHeight), ImGuiChildFlags.None))
                {
                    if (!_hasPower)
                    {
                        string noPowerText = "NO POWER";
                        var textSize = ImGui.CalcTextSize(noPowerText);
                        ImGui.SetCursorPos(new Vector2((ImGui.GetWindowWidth() - textSize.X) * 0.5f, (ImGui.GetContentRegionAvail().Y - textSize.Y) * 0.5f));
                        ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), noPowerText);
                    }
                    else if (analyzerBlock.IsScanning)
                    {
                        long currentAbsoluteTick = GameTime.Day * 24000L + GameTime.DayTick;
                        float progress = (currentAbsoluteTick - analyzerBlock.ScanStartAbsoluteTick) / (float)totalTicksNeeded;

                        if (progress >= 1f)
                        {
                            progress = 1f;
                            CompleteScan();
                        }

                        string scanText = "Scanning in progress...";
                        float textWidth = ImGui.CalcTextSize(scanText).X;
                        float barWidth = ImGui.GetWindowWidth() * 0.85f;

                        float availY = ImGui.GetContentRegionAvail().Y;
                        float totalH = ImGui.GetTextLineHeight() + windowHeight * 0.02f + buttonHeight * 0.8f;
                        float startY = (availY - totalH) * 0.5f;

                        ImGui.SetCursorPos(new Vector2((ImGui.GetWindowWidth() - textWidth) * 0.5f, startY));
                        ImGui.TextColored(new Vector4(1, 1, 0, 1), scanText);

                        ImGui.SetCursorPos(new Vector2((ImGui.GetWindowWidth() - barWidth) * 0.5f, startY + ImGui.GetTextLineHeight() + windowHeight * 0.02f));
                        ImGui.ProgressBar(progress, new Vector2(barWidth, buttonHeight * 0.8f), $"{(int)(progress * 100)}%");
                    }
                    else if (analyzerBlock.ScanComplete)
                    {
                        string formattedMass = analyzerBlock.CachedMass.ToString("N0").Replace(",", ".");
                        ImGui.TextColored(new Vector4(1f, 1f, 0f, 1f), $"Estimated Mass: {formattedMass} tn");

                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + windowHeight * 0.01f);
                        ImGui.Separator();
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + windowHeight * 0.01f);

                        ImGui.Text("Detected Minerals:");
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + windowHeight * 0.01f);

                        if (analyzerBlock.CachedMinerals.Count == 0)
                        {
                            ImGui.TextColored(new Vector4(1, 0, 0, 1), "No minerals detected.");
                        }
                        else
                        {
                            var sortedMinerals = analyzerBlock.CachedMinerals.OrderByDescending(k => k.Value).ToList();
                            string approxSign = analyzerBlock.DeviationPercentage == 0f ? "" : "~";

                            float rowHeight = ImGui.GetTextLineHeight() * 1.5f;
                            float iconSize = ImGui.GetTextLineHeight() * 1.2f;

                            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, innerScrollbarSize);
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

                                    float textOffsetY = (rowHeight - ImGui.GetTextLineHeight()) * 0.5f;
                                    float iconOffsetY = (rowHeight - iconSize) * 0.5f;

                                    ImGui.SetCursorPosY(startY + iconOffsetY);
                                    if (icon != IntPtr.Zero)
                                    {
                                        ImGui.Image(icon, new Vector2(iconSize, iconSize));
                                        ImGui.SameLine();
                                    }
                                    else
                                    {
                                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + iconSize + ImGui.GetStyle().ItemSpacing.X);
                                    }

                                    ImGui.SetCursorPosY(startY + textOffsetY);
                                    var bData = GameAssets.GetBlockDataById(kvp.Key);
                                    string minName = bData != null ? bData.Name : $"Unknown ID: {kvp.Key}";
                                    ImGui.TextColored(new Vector4(0.8f, 1f, 0.8f, 1f), minName);

                                    string countText = $"{approxSign}{kvp.Value}";
                                    float countWidth = ImGui.CalcTextSize(countText).X;

                                    ImGui.SameLine(innerAvailX - countWidth - ImGui.GetStyle().ItemSpacing.X);
                                    ImGui.SetCursorPosY(startY + textOffsetY);
                                    ImGui.TextColored(new Vector4(0, 1, 0.5f, 1), countText);

                                    ImGui.SetCursorPosY(startY + rowHeight);
                                }
                            }
                            ImGui.EndChild();
                            ImGui.PopStyleVar();
                        }
                    }
                    else
                    {
                        string emptyText = "Start scanning to collect data.";
                        float textW = ImGui.CalcTextSize(emptyText).X;
                        float textH = ImGui.GetTextLineHeight();
                        ImGui.SetCursorPos(new Vector2((ImGui.GetWindowWidth() - textW) * 0.5f, (ImGui.GetContentRegionAvail().Y - textH) * 0.5f));
                        ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 1f), emptyText);
                    }
                }
                ImGui.EndChild();

                if (_hasPower)
                {
                    ImGui.SetCursorPosY(windowHeight - buttonHeight - padding);
                    float totalBtnWidth = windowWidth * 0.6f;

                    ApplyButtonStyles();

                    if (analyzerBlock.IsScanning)
                    {
                        ImGui.SetCursorPosX((windowWidth - totalBtnWidth) * 0.5f);

                        if (ImGui.Button("Cancel Scan", new Vector2(totalBtnWidth, buttonHeight)))
                        {
                            clickSound?.Play();
                            scanSound?.Stop();
                            analyzerBlock.IsScanning = false;
                            analyzerBlock.ScanStartAbsoluteTick = 0;
                            if (analyzerBlock.chunk != null) analyzerBlock.chunk.IsModified = true;
                        }
                    }
                    else if (analyzerBlock.ScanComplete)
                    {
                        float btnWidth = (totalBtnWidth - ImGui.GetStyle().ItemSpacing.X) * 0.5f;
                        ImGui.SetCursorPosX((windowWidth - totalBtnWidth) * 0.5f);

                        if (ImGui.Button("Rescan", new Vector2(btnWidth, buttonHeight)))
                        {
                            clickSound?.Play();
                            if (scanSound != null && !scanSound.IsPlaying)
                            {
                                scanSound.Play();
                            }
                            analyzerBlock.ScanComplete = false;
                            analyzerBlock.IsScanning = true;
                            analyzerBlock.ScanStartAbsoluteTick = GameTime.Day * 24000L + GameTime.DayTick;
                            analyzerBlock.CachedMinerals.Clear();
                            if (analyzerBlock.chunk != null) analyzerBlock.chunk.IsModified = true;
                        }

                        ImGui.SameLine();

                        if (ImGui.Button("Clear Data", new Vector2(btnWidth, buttonHeight)))
                        {
                            clickSound?.Play();
                            analyzerBlock.ScanComplete = false;
                            analyzerBlock.CachedMinerals.Clear();
                            analyzerBlock.CachedMass = 0;
                            if (analyzerBlock.chunk != null) analyzerBlock.chunk.IsModified = true;
                        }
                    }
                    else
                    {
                        ImGui.SetCursorPosX((windowWidth - totalBtnWidth) * 0.5f);

                        if (ImGui.Button("Scan Asteroid", new Vector2(totalBtnWidth, buttonHeight)))
                        {
                            clickSound?.Play();
                            if (scanSound != null && !scanSound.IsPlaying)
                            {
                                scanSound.Play();
                            }
                            analyzerBlock.IsScanning = true;
                            analyzerBlock.ScanStartAbsoluteTick = GameTime.Day * 24000L + GameTime.DayTick;
                            if (analyzerBlock.chunk != null) analyzerBlock.chunk.IsModified = true;
                        }
                    }

                    PopButtonStyles();
                }
            }
            ImGui.End();
            ImGui.PopStyleVar();
        }

        private static void CompleteScan()
        {
            scanSound?.Stop();
            finishSound?.Play();
            analyzerBlock.IsScanning = false;
            analyzerBlock.ScanComplete = true;

            if (analyzerBlock.chunk != null)
            {
                analyzerBlock.chunk.IsModified = true;
            }

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

        public static void Dispose()
        {
            analyzerBlock = null;
            
        }
    }
}
using Engine;
using Engine.Audio;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Spacebox.Game.Generation;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Physics;
using Spacebox.Game.Resource;
using Spacebox.GUI;
using Spacebox.Network;
using System;
using System.Linq;

namespace Spacebox.Game.GUI
{
    public class AnalyzerUI
    {
        private static AnalyzerBlock analyzerBlock;
        private static SpaceEntity currentEntity;
        private static string blockName = "Analyzer";
        private static bool _wasScanning;

        private static bool _hasPower = false;
        private static bool _wasVisible = false;

        private static ulong _lastCheckedMass = ulong.MaxValue;
        private static int _lastBlockCount = 0;

        private static AudioSource scanSound;
        private static AudioSource finishSound;
        private static AudioSource clickSound;

        public static void Initialize()
        {
            if (scanSound == null || scanSound.IsDisposed)
            {
                scanSound = new AudioSource(Resources.Load<AudioClip>("analyzer_scanning"), AudioChannel.UI);
                scanSound.Volume = 1f;
                scanSound.IsLooped = true;
            }
            if (finishSound == null || finishSound.IsDisposed)
            {
                finishSound = new AudioSource(Resources.Load<AudioClip>("analyzer_complete"), AudioChannel.UI);
                finishSound.Volume = 0.5f;
            }
            if (clickSound == null || clickSound.IsDisposed)
            {
                clickSound = new AudioSource(Resources.Load<AudioClip>("click1"), AudioChannel.UI);
                clickSound.Volume = 0.8f;
            }
        }

        public static void Open(AnalyzerBlock block, ref HitInfo hit)
        {
            Initialize();
            analyzerBlock = block;
            currentEntity = hit.chunk.SpaceEntity;
            blockName = GameAssets.GetBlockDataById(block.Id).Name;

            _lastCheckedMass = ulong.MaxValue;
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

        private static void CountBlocksIfMassChanged()
        {
            if (currentEntity == null) return;
            if (currentEntity.Mass == _lastCheckedMass) return;

            _lastCheckedMass = currentEntity.Mass;
            _lastBlockCount = AnalyzerScanner.CountBlocks(currentEntity);
        }

        private static void UpdateScanSounds()
        {
            bool isScanning = analyzerBlock.IsScanning;
            if (isScanning == _wasScanning) return;

            if (isScanning)
            {
                if (scanSound != null && !scanSound.IsPlaying) scanSound.Play();
            }
            else
            {
                scanSound?.Stop();
                if (analyzerBlock.ScanComplete) finishSound?.Play();
            }

            _wasScanning = isScanning;
        }

        public static bool IsOpen => UIManager.IsOpen("analyzer") && analyzerBlock != null && currentEntity != null;
        public static AnalyzerBlock Block => analyzerBlock;
        public static string BlockName => blockName;
        public static bool HasPower => _hasPower;

        public static float ScanSeconds => analyzerBlock == null ? 0f
            : analyzerBlock.IsScanning && analyzerBlock.ScanDurationTicks > 0 ? analyzerBlock.ScanDurationTicks / 20f
            : AnalyzerScanner.GetScanSeconds(analyzerBlock, _lastBlockCount);

        public static void Update()
        {
            bool isVisible = UIManager.IsOpen("analyzer");

            if (isVisible != _wasVisible)
            {
                if (isVisible)
                {
                    _wasScanning = false;
                }
                else
                {
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
            UpdateScanSounds();
        }

        public static void Request(AnalyzerAction action)
        {
            if (analyzerBlock == null) return;
            clickSound?.Play();
            AnalyzerNetwork.Request(analyzerBlock, action);
        }

        public static void Dispose()
        {
            analyzerBlock = null;
            currentEntity = null;
            
        }
    }
}
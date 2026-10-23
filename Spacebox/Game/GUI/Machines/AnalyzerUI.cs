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
    public sealed class AnalyzerUI : IGameUiController
    {
        private AnalyzerBlock analyzerBlock;
        private SpaceEntity currentEntity;
        private string blockName = "Analyzer";
        private bool _wasScanning;

        private bool _hasPower = false;
        private bool _wasVisible = false;

        private ulong _lastCheckedMass = ulong.MaxValue;
        private int _lastBlockCount = 0;

        private AudioSource scanSound;
        private AudioSource finishSound;
        private AudioSource clickSound;

        private void EnsureSounds()
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

        public void Open(AnalyzerBlock block, ref HitInfo hit)
        {
            EnsureSounds();
            analyzerBlock = block;
            currentEntity = hit.chunk.SpaceEntity;
            blockName = GameAssets.GetBlockDataById(block.Id).Name;

            _lastCheckedMass = ulong.MaxValue;
            CountBlocksIfMassChanged();

            UIManager.Open("analyzer");
        }

        public void Close()
        {
            analyzerBlock = null;
            currentEntity = null;
            UIManager.CloseTop();
        }

        private void CheckPower()
        {
            if (currentEntity == null || analyzerBlock == null)
            {
                _hasPower = false;
                return;
            }

            _hasPower = analyzerBlock.IsActive;
        }

        private void CountBlocksIfMassChanged()
        {
            if (currentEntity == null) return;
            if (currentEntity.Mass == _lastCheckedMass) return;

            _lastCheckedMass = currentEntity.Mass;
            _lastBlockCount = AnalyzerScanner.CountBlocks(currentEntity);
        }

        private void UpdateScanSounds()
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

        public bool IsOpen => UIManager.IsOpen("analyzer") && analyzerBlock != null && currentEntity != null;
        public AnalyzerBlock Block => analyzerBlock;
        public string BlockName => blockName;
        public bool HasPower => _hasPower;

        public float ScanSeconds => analyzerBlock == null ? 0f
            : analyzerBlock.IsScanning && analyzerBlock.ScanDurationTicks > 0 ? analyzerBlock.ScanDurationTicks / 20f
            : AnalyzerScanner.GetScanSeconds(analyzerBlock, _lastBlockCount);

        public void OnGUI()
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

        public void Request(AnalyzerAction action)
        {
            if (analyzerBlock == null) return;
            clickSound?.Play();
            AnalyzerNetwork.Request(analyzerBlock, action);
        }

        public void Reset()
        {
            analyzerBlock = null;
            currentEntity = null;
            _wasScanning = false;
            _wasVisible = false;
            scanSound?.Dispose();
            finishSound?.Dispose();
            clickSound?.Dispose();
            scanSound = finishSound = clickSound = null;
        }
    }
}
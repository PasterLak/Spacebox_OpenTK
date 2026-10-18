using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.GUI;

namespace Spacebox.Game.Player
{
    public class RemoteAstronaut : Astronaut
    {
        private const float NameTagHeight = 0.9f;

        private GUI.Tag _nameTag;
        private bool _isAliveVisual = true;

        public string PlayerName { get; }

        public RemoteAstronaut(string playerName, string skinColor, Vector3 position) : base(position, false)
        {
            PlayerName = playerName;
            Name = $"RemoteAstronaut_{playerName}";

            CreateModel(0, string.IsNullOrEmpty(skinColor) ? "Yellow" : skinColor);
            Flashlight.Enabled = false;

            if (TagManager.Instance != null)
            {
                _nameTag = TagManager.Instance.CreateTag(playerName, position, Color4.White);
                _nameTag.TextAlignment = GUI.Tag.Alignment.Center;
            }
        }

        public override void Update()
        {
            if (_nameTag != null)
                _nameTag.WorldPosition = Position + UpLocal * NameTagHeight;

            base.Update();
        }

        public void SetAliveVisual(bool isAlive)
        {
            _isAliveVisual = isAlive;
            if (HandVisualizer != null) HandVisualizer.Enabled = isAlive;
            if (!isAlive && Flashlight != null) Flashlight.Enabled = false;
            if (_nameTag != null) _nameTag.Text = isAlive ? PlayerName : PlayerName + " (dead)";
        }

        public void OnDisconnect()
        {
            if (_nameTag != null)
            {
                TagManager.Instance?.ReleaseTag(_nameTag);
                _nameTag = null;
            }

            Flashlight?.Destroy();

            if (HandVisualizer != null)
            {
                HandVisualizer.SetItem(null);
                RemoveChild(HandVisualizer);
            }
        }

        protected override void UpdateVisuals()
        {
            if (AstBody != null) AstBody.Enabled = _isAliveVisual;
            if (AstHelmet != null) AstHelmet.Enabled = _isAliveVisual;
            if (AstTank != null) AstTank.Enabled = _isAliveVisual && HasOxygen;
        }
    }
}

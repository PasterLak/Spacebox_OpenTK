using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;

namespace Spacebox.Game.Player
{
    public class RemoteAstronaut : Astronaut, IDrillable
    {
        private const float NameTagHeight = 0.9f;
        private const float BodyRadius = 0.4f;

        private readonly SphereBody _body;
        private float _pendingDrillDamage;

        public event Action<int>? DrillDamaged;

        private GUI.Tag _nameTag;
        private bool _isAliveVisual = true;

        public string PlayerName { get; }

        public RemoteAstronaut(string playerName, string skinColor, Vector3 position) : base(position, false)
        {
            PlayerName = playerName;
            Name = $"RemoteAstronaut_{playerName}";

            CreateModel(0, string.IsNullOrEmpty(skinColor) ? "Yellow" : skinColor);
            Flashlight.Enabled = false;
            _body = AttachComponent(new SphereBody(World.Instance?.Physics, BodyRadius) { IsKinematic = true });

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

        public void TakeDrillDamage(float damage)
        {
            if (!_isAliveVisual) return;

            _pendingDrillDamage += damage;
            int wholeDamage = (int)_pendingDrillDamage;
            if (wholeDamage <= 0) return;

            _pendingDrillDamage -= wholeDamage;
            DrillDamaged?.Invoke(wholeDamage);
        }

        public void SetAliveVisual(bool isAlive)
        {
            _isAliveVisual = isAlive;
            _body.Enabled = isAlive;
            if (HandVisualizer != null) HandVisualizer.Enabled = isAlive;
            if (!isAlive && Flashlight != null) Flashlight.Enabled = false;
            if (_nameTag != null) _nameTag.Text = isAlive ? PlayerName : PlayerName + " (dead)";
        }

        public void OnDisconnect()
        {
            DetachComponent(_body);

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
            if (ArmorVisual != null) ArmorVisual.Visible = _isAliveVisual;
        }
    }
}

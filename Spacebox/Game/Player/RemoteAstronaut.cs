using Engine;
using OpenTK.Mathematics;
using Spacebox.Game.GUI;
using Spacebox.Game.Generation;
using Spacebox.Game.Physics;
using Spacebox.Game.Effects;
using Engine.Audio;

namespace Spacebox.Game.Player
{
    public class RemoteAstronaut : Astronaut, IDrillable
    {
        private const float NameTagHeight = 0.9f;
        private const float BodyRadius = 0.4f;

        private const float UseSoundMinDistance = 3f;
        private const float UseSoundMaxDistance = 40f;
        private const float UseSoundRolloff = 1.5f;

        private readonly SphereBody _body;
        private readonly RemoteDrillDust _drillDust;
        private readonly PlayerEffects _effects;
        private AudioSource? _useSound;
        private float _pendingDrillDamage;

        public event Action<int>? DrillDamaged;

        private GUI.Tag _nameTag;
        private bool _isAliveVisual = true;
        public bool IsAliveVisual => _isAliveVisual;

        public string PlayerName { get; }

        public RemoteAstronaut(string playerName, string skinColor, Vector3 position) : base(position, false)
        {
            PlayerName = playerName;
            Name = $"RemoteAstronaut_{playerName}";

            CreateModel(0, string.IsNullOrEmpty(skinColor) ? "Yellow" : skinColor);
            Flashlight.Enabled = false;
            _body = AttachComponent(new SphereBody(World.Instance?.Physics, BodyRadius) { IsKinematic = true });
            _effects = AddChild(new PlayerEffects());
            _drillDust = new RemoteDrillDust(this);

            if (GameScene.Current?.Tags != null)
            {
                _nameTag = GameScene.Current.Tags.CreateTag(playerName, position, Color4.White);
                _nameTag.TextAlignment = GUI.Tag.Alignment.Center;
            }
        }

        public override void Update()
        {
            if (_nameTag != null)
                _nameTag.WorldPosition = Position + UpLocal * NameTagHeight;

            _drillDust.Update(_isAliveVisual && HeldItemVisual != null && HeldItemVisual.IsUsing, HeldItem);
            base.Update();
        }

        public void PlayItemUse(Item item)
        {
            if (item == null || !_isAliveVisual) return;

            ItemUseCue.PlayEffects(_effects, item);

            var clip = ItemUseCue.GetSound(item);
            if (clip == null) return;

            if (_useSound == null)
            {
                _useSound = new AudioSource(clip);
                _useSound.Setup3D(UseSoundMinDistance, UseSoundMaxDistance, UseSoundRolloff);
            }

            if (_useSound.IsPlaying) _useSound.Stop();
            _useSound.Clip = clip;
            _useSound.Position = Position;
            _useSound.Play();
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
            _drillDust.Dispose();
            _useSound?.Dispose();
            _useSound = null;

            if (_nameTag != null)
            {
                GameScene.Current?.Tags?.ReleaseTag(_nameTag);
                _nameTag = null;
            }

            Flashlight?.Destroy();
            Occlusion.Release();

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

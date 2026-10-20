using Engine;
using OpenTK.Mathematics;

namespace Spacebox.Game.Player
{
    public class HandItemVisualizer : Node3D
    {
        private readonly Astronaut _owner;
        private readonly HeldItemModels _models;

        private const float VibrationSpeed = 55f;
        private const float VibrationAmount = 0.012f;
        private const float KickDistance = 0.08f;
        private const float KickRecovery = 12f;

        private float _kick;
        private float _time;

        public bool IsUsing { get; set; }

        public HandItemVisualizer(Astronaut owner)
        {
            _owner = owner;
            Name = "HandItemVisualizer";
            _models = new HeldItemModels(this);
        }

        public void SetItem(Item? item)
        {
            _models.SetItem(item);
            UpdateVisibilityState();
        }

        public void PlayKick()
        {
            _kick = 1f;
        }

        public override void Update()
        {
            base.Update();
            if (_owner.IsMain) return;

            float deltaTime = Time.Delta;
            _time += deltaTime;
            _kick = MathF.Max(0f, _kick - KickRecovery * deltaTime * _kick - deltaTime);

            var offset = new Vector3(0f, 0f, _kick * KickDistance);
            if (IsUsing)
                offset += new Vector3(MathF.Sin(_time * VibrationSpeed) * VibrationAmount, MathF.Cos(_time * VibrationSpeed * 1.3f) * VibrationAmount * 0.5f, 0f);

            _models.SetOffset(offset);
        }

        public override void Render()
        {
            if (!Enabled) return;

            UpdateVisibilityState();

            if (_owner.IsMain) return;

            base.Render();
        }

        private void UpdateVisibilityState()
        {
            _models.SetVisible(!_owner.IsMain);
        }
    }
}
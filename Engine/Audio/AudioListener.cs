using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using Engine.Components;

namespace Engine.Audio
{
    public class AudioListener : Component
    {
        private Vector3 _lastPosition = Vector3.Zero;
        private Vector3 _lastForward = -Vector3.UnitZ;
        private Vector3 _lastUp = Vector3.UnitY;
        private Vector3 _velocity = Vector3.Zero;
        private float _gain = 1.0f;
        private float _dopplerFactor = 1.0f;
        private float _speedOfSound = 343.3f;
        private DistanceModel _distanceModel = DistanceModel.InverseDistanceClamped;
        private bool _isDirty = true;

        public float Gain
        {
            get => _gain;
            set
            {
                _gain = MathHelper.Clamp(value, 0f, 1f);
                if (AudioDevice.IsEnabled) AL.Listener(ALListenerf.Gain, _gain);
            }
        }

        public Vector3 Velocity
        {
            get => _velocity;
            set
            {
                _velocity = value;
                _isDirty = true;
            }
        }

        public float DopplerFactor
        {
            get => _dopplerFactor;
            set
            {
                _dopplerFactor = MathHelper.Clamp(value, 0f, 10f);
                if (AudioDevice.IsEnabled) AL.DopplerFactor(_dopplerFactor);
            }
        }

        public float SpeedOfSound
        {
            get => _speedOfSound;
            set
            {
                _speedOfSound = MathHelper.Clamp(value, 0.1f, 10000f);
                if (AudioDevice.IsEnabled) AL.SpeedOfSound(_speedOfSound);
            }
        }

        public DistanceModel DistanceModel
        {
            get => _distanceModel;
            set
            {
                _distanceModel = value;
                if (AudioDevice.IsEnabled) AL.DistanceModel((ALDistanceModel)value);
            }
        }

        public override void Start()
        {
            if (!AudioDevice.IsEnabled) return;

            AL.DopplerFactor(_dopplerFactor);
            AL.SpeedOfSound(_speedOfSound);
            AL.DistanceModel((ALDistanceModel)_distanceModel);
            UpdateListener();

            Debug.Log($"OpenAL Version: {AL.Get(ALGetString.Version)}");
            //Debug.Log($"OpenAL Extensions: {AL.Get(ALGetString.Extensions)}");
        }

        public override void OnUpdate()
        {
            if (Owner == null || !AudioDevice.IsEnabled) return;

            Vector3 currentPosition = Owner.PositionWorld;
            Vector3 currentForward = Owner.ForwardLocal;
            Vector3 currentUp = Owner.UpLocal;

            if (_isDirty ||
                currentPosition != _lastPosition ||
                currentForward != _lastForward ||
                currentUp != _lastUp)
            {
                _velocity = ListenerVelocity.Compute(currentPosition - _lastPosition, Time.Delta, _speedOfSound);

                _lastPosition = currentPosition;
                _lastForward = currentForward;
                _lastUp = currentUp;
                _isDirty = false;

                UpdateListener();
            }
        }

        private void UpdateListener()
        {
           
            try
            {
                AL.Listener(ALListener3f.Position, _lastPosition.X, _lastPosition.Y, _lastPosition.Z);
                AL.Listener(ALListener3f.Velocity, _velocity.X, _velocity.Y, _velocity.Z);

                float[] orientation = {
                    _lastForward.X, _lastForward.Y, _lastForward.Z,
                    _lastUp.X, _lastUp.Y, _lastUp.Z
                };
                AL.Listener(ALListenerfv.Orientation, orientation);

                ALError error = AL.GetError();
                if (error != ALError.NoError)
                    Debug.Error($"[AudioListener] OpenAL error while updating the listener: {AL.GetErrorString(error)}");
            }
            catch (System.Exception ex)
            {
                Debug.Error($"[AudioListener] Error updating listener: {ex.Message}");
            }
        }

        public override void OnDetached()
        {
            if (!AudioDevice.IsEnabled)
            {
                base.OnDetached();
                return;
            }

            try
            {
                AL.Listener(ALListener3f.Position, 0, 0, 0);
                AL.Listener(ALListener3f.Velocity, 0, 0, 0);
                float[] orientation = { 0, 0, -1, 0, 1, 0 };
                AL.Listener(ALListenerfv.Orientation, orientation);
                AL.DopplerFactor(1.0f);
                AL.SpeedOfSound(343.3f);
                AL.DistanceModel(ALDistanceModel.InverseDistanceClamped);
            }
            catch (System.Exception ex)
            {
                Debug.Error($"[AudioListener] Error resetting listener: {ex.Message}");
            }

            base.OnDetached();
        }
    }

    public enum DistanceModel
    {
        None = ALDistanceModel.None,
        InverseDistance = ALDistanceModel.InverseDistance,
        InverseDistanceClamped = ALDistanceModel.InverseDistanceClamped,
        LinearDistance = ALDistanceModel.LinearDistance,
        LinearDistanceClamped = ALDistanceModel.LinearDistanceClamped,
        ExponentDistance = ALDistanceModel.ExponentDistance,
        ExponentDistanceClamped = ALDistanceModel.ExponentDistanceClamped
    }
}
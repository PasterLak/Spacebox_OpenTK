using OpenTK.Mathematics;

namespace Spacebox.Game.Player
{
    public class CameraSway
    {
        public float InitialIntensity { get; set; }
        public float InitialFrequency { get; set; }
        public float MaxIntensity { get; set; }
        public float MaxFrequency { get; set; }
        public float SpeedThreshold { get; set; }
        public bool Enabled { get; set; }

        private float _currentIntensity = 0f;
        private float _currentFrequency = 0f;
        private float _phaseX = 0f;
        private float _phaseY = 0f;
        private float _intensityRampRate;
        private float _frequencyRampRate;

        public CameraSway(float initialIntensity = 0.02f, float initialFrequency = 1.0f, float maxIntensity = 0.05f, float maxFrequency = 2.0f, float speedThreshold = 5.0f, float intensityRampRate = 2.0f, float frequencyRampRate = 2.0f)
        {
            InitialIntensity = initialIntensity;
            InitialFrequency = initialFrequency;
            MaxIntensity = maxIntensity;
            MaxFrequency = maxFrequency;
            SpeedThreshold = speedThreshold;
            Enabled = true;
            _intensityRampRate = intensityRampRate;
            _frequencyRampRate = frequencyRampRate;
            _currentIntensity = 0f;
            _currentFrequency = 0f;
        }

        public void Update(float speed, float deltaTime)
        {
            if (!Enabled)
            {
                _currentIntensity = MathHelper.Lerp(_currentIntensity, 0f, _intensityRampRate * deltaTime);
                _currentFrequency = MathHelper.Lerp(_currentFrequency, 0f, _frequencyRampRate * deltaTime);
                return;
            }

            if (speed > SpeedThreshold)
            {
                float factor = MathHelper.Clamp((speed - SpeedThreshold) / SpeedThreshold, 0f, 1f);
                float targetIntensity = InitialIntensity + (MaxIntensity - InitialIntensity) * factor;
                float targetFrequency = InitialFrequency + (MaxFrequency - InitialFrequency) * factor;

                _currentIntensity = MathHelper.Lerp(_currentIntensity, targetIntensity, _intensityRampRate * deltaTime);
                _currentFrequency = MathHelper.Lerp(_currentFrequency, targetFrequency, _frequencyRampRate * deltaTime);
            }
            else
            {
                _currentIntensity = MathHelper.Lerp(_currentIntensity, 0f, _intensityRampRate * deltaTime);
                _currentFrequency = MathHelper.Lerp(_currentFrequency, 0f, _frequencyRampRate * deltaTime);
            }

            _phaseX += _currentFrequency * deltaTime;
            _phaseY += _currentFrequency * deltaTime;
        }

        public Quaternion GetSwayRotation()
        {
            float swayX = MathF.Sin(_phaseX) * _currentIntensity;
            float swayY = MathF.Sin(_phaseY) * _currentIntensity;
            return Quaternion.FromEulerAngles(swayX, swayY, 0f);
        }

        public void SetParameters(float initialIntensity, float initialFrequency, float maxIntensity, float maxFrequency, float speedThreshold, float intensityRampRate, float frequencyRampRate)
        {
            InitialIntensity = initialIntensity;
            InitialFrequency = initialFrequency;
            MaxIntensity = maxIntensity;
            MaxFrequency = maxFrequency;
            SpeedThreshold = speedThreshold;
            _intensityRampRate = intensityRampRate;
            _frequencyRampRate = frequencyRampRate;
        }

        public void EnableSway(bool enabled)
        {
            Enabled = enabled;
            if (!enabled)
            {
                _currentIntensity = 0f;
                _currentFrequency = 0f;
            }
            else
            {
                _currentIntensity = InitialIntensity;
                _currentFrequency = InitialFrequency;
            }
        }
    }
}

using Engine;
using OpenTK.Mathematics;
using System;

namespace Spacebox.Game.Player
{
    public class ItemSway
    {
        public float SwayMultiplier { get; set; } = 0.5f;
        public float DampingFactor { get; set; } = 8.0f;
        public float MaxSwayAngle { get; set; } = 3.0f;
        public bool EnableSway { get; set; } = true;

        public Vector3 PushBackAxis { get; set; } = new Vector3(-1, 0, 0);

        public float PushBackMultiplier { get; set; } = 0.00005f;
        public float MaxPushBack { get; set; } = 0.015f;

        public float PullForwardMultiplier { get; set; } = 0.00003f;
        public float MaxPullForward { get; set; } = 0.015f;

        public Vector3 LateralAxis { get; set; } = new Vector3(0, 0, -1);
        public float LateralMultiplier { get; set; } = 0.00003f;
        public float MaxLateral { get; set; } = 0.015f;

        public float MoveSmoothing { get; set; } = 8.0f;
        public float ReturnSmoothing { get; set; } = 12.0f;

        private Vector3 _currentSwayRotation = Vector3.Zero;
        private float _currentOffset = 0f;
        private float _currentLateralOffset = 0f;

        private Matrix4 _cachedMatrix = Matrix4.Identity;
        private bool _isDirty = true;

        public Matrix4 GetSwayMatrix()
        {
            if (_isDirty)
            {
                RecalculateMatrix();
                _isDirty = false;
            }
            return _cachedMatrix;
        }

        public void Update(Vector3 velocity, Vector3 playerFront, Vector3 playerRight)
        {
            if (!EnableSway)
            {
                ReturnToOrigin();
                return;
            }

            UpdateSway(Input.Mouse.Delta);
            UpdateOffsets(velocity, playerFront, playerRight);

            _isDirty = true;
        }

        private void UpdateSway(Vector2 mouseDelta)
        {
            float swayX = Math.Clamp(-mouseDelta.X * SwayMultiplier, -MaxSwayAngle, MaxSwayAngle);
            float swayY = Math.Clamp(-mouseDelta.Y * SwayMultiplier, -MaxSwayAngle, MaxSwayAngle);

            Vector3 targetSway = new Vector3(
                MathHelper.DegreesToRadians(swayY),
                MathHelper.DegreesToRadians(-swayX),
                0f
            );

            _currentSwayRotation = Vector3.Lerp(_currentSwayRotation, targetSway, DampingFactor * Time.Delta);
        }

        private void UpdateOffsets(Vector3 velocity, Vector3 playerFront, Vector3 playerRight)
        {
            float speedDot = Vector3.Dot(velocity, playerFront);
            float targetOffset = 0f;

            if (speedDot > 0f)
            {
                targetOffset = Math.Min(speedDot * speedDot * PushBackMultiplier, MaxPushBack);
            }
            else if (speedDot < 0f)
            {
                targetOffset = Math.Max(-(speedDot * speedDot * PullForwardMultiplier), -MaxPullForward);
            }

            float smoothing = Math.Abs(targetOffset) > Math.Abs(_currentOffset) ? MoveSmoothing : ReturnSmoothing;
            _currentOffset = Lerp(_currentOffset, targetOffset, smoothing * Time.Delta);

            float lateralDot = Vector3.Dot(velocity, playerRight);
            float targetLateral = 0f;

            if (lateralDot > 0f)
            {
                targetLateral = Math.Min(lateralDot * lateralDot * LateralMultiplier, MaxLateral);
            }
            else if (lateralDot < 0f)
            {
                targetLateral = Math.Max(-(lateralDot * lateralDot * LateralMultiplier), -MaxLateral);
            }

            float lateralSmoothing = Math.Abs(targetLateral) > Math.Abs(_currentLateralOffset) ? MoveSmoothing : ReturnSmoothing;
            _currentLateralOffset = Lerp(_currentLateralOffset, targetLateral, lateralSmoothing * Time.Delta);
        }

        private void ReturnToOrigin()
        {
            if (_currentSwayRotation == Vector3.Zero && Math.Abs(_currentOffset) < 0.0001f && Math.Abs(_currentLateralOffset) < 0.0001f)
            {
                _currentOffset = 0f;
                _currentLateralOffset = 0f;
                return;
            }

            _currentSwayRotation = Vector3.Lerp(_currentSwayRotation, Vector3.Zero, DampingFactor * Time.Delta);
            _currentOffset = Lerp(_currentOffset, 0f, ReturnSmoothing * Time.Delta);
            _currentLateralOffset = Lerp(_currentLateralOffset, 0f, ReturnSmoothing * Time.Delta);
            _isDirty = true;
        }

        private void RecalculateMatrix()
        {
            Quaternion rotation = Quaternion.FromEulerAngles(_currentSwayRotation);

            Vector3 translation = new Vector3(
                PushBackAxis.X * _currentOffset + LateralAxis.X * _currentLateralOffset,
                PushBackAxis.Y * _currentOffset + LateralAxis.Y * _currentLateralOffset,
                PushBackAxis.Z * _currentOffset + LateralAxis.Z * _currentLateralOffset
            );

            _cachedMatrix = Matrix4.CreateFromQuaternion(rotation) * Matrix4.CreateTranslation(translation);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Math.Clamp(t, 0f, 1f);
        }
    }
}
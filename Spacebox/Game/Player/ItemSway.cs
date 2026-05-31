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

        public float PushBackMultiplier { get; set; } = 0.0001f;
        public float MaxPushBack { get; set; } = 0.015f;

        public float PullForwardMultiplier { get; set; } = 0.00002f;
        public float MaxPullForward { get; set; } = 0.015f;

        public float MoveSmoothing { get; set; } = 8.0f;
        public float ReturnSmoothing { get; set; } = 12.0f;

        private Vector3 _currentSwayRotation = Vector3.Zero;
        private float _currentOffset = 0f;

        public Matrix4 GetSwayMatrix()
        {
            Matrix4 rotation = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(_currentSwayRotation.X)) *
                               Matrix4.CreateRotationY(MathHelper.DegreesToRadians(_currentSwayRotation.Y)) *
                               Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(_currentSwayRotation.Z));

            Matrix4 translation = Matrix4.CreateTranslation(
                PushBackAxis.X * _currentOffset,
                PushBackAxis.Y * _currentOffset,
                PushBackAxis.Z * _currentOffset
            );

            return rotation * translation;
        }

        public void Update(Vector3 velocity, Vector3 playerFront)
        {
            if (!EnableSway)
            {
                _currentSwayRotation = Vector3.Lerp(_currentSwayRotation, Vector3.Zero, DampingFactor * Time.Delta);
                _currentOffset = Lerp(_currentOffset, 0f, ReturnSmoothing * Time.Delta);
                return;
            }

            Vector2 mouseDelta = Input.Mouse.Delta;

            float swayX = Math.Clamp(-mouseDelta.X * SwayMultiplier, -MaxSwayAngle, MaxSwayAngle);
            float swayY = Math.Clamp(-mouseDelta.Y * SwayMultiplier, -MaxSwayAngle, MaxSwayAngle);

            Vector3 targetSway = new Vector3(swayY, -swayX, 0);
            _currentSwayRotation = Vector3.Lerp(_currentSwayRotation, targetSway, DampingFactor * Time.Delta);

            float speedDot = Vector3.Dot(velocity, playerFront);
            float targetOffset = 0f;

            if (speedDot > 0)
            {
                targetOffset = Math.Min(speedDot * speedDot * PushBackMultiplier, MaxPushBack);
            }
            else
            {
                targetOffset = Math.Max(-(speedDot * speedDot * PullForwardMultiplier), -MaxPullForward);
            }

            float smoothing = Math.Abs(targetOffset) > Math.Abs(_currentOffset) ? MoveSmoothing : ReturnSmoothing;
            _currentOffset = Lerp(_currentOffset, targetOffset, smoothing * Time.Delta);
        }

        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Math.Clamp(t, 0f, 1f);
        }
    }
}
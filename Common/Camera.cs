using OpenTK.Mathematics;
using System;

namespace Spacebox_OpenTK.Common
{
    public class Camera
    {
        // Directions vectors defining the camera's orientation
        private Vector3 _front = -Vector3.UnitZ;
        private Vector3 _up = Vector3.UnitY;
        private Vector3 _right = Vector3.UnitX;

        // Rotation around the X and Y axes (radians)
        private float _pitch;
        private float _yaw = -MathHelper.PiOver2; // Start facing forward along the negative Z axis

        // Field of view (radians)
        private float _fov = MathHelper.PiOver2;

        public Camera(Vector3 position, float aspectRatio)
        {
            Position = position;
            AspectRatio = aspectRatio;
            UpdateVectors(); // Initialize direction vectors
        }

        // Camera's position in world space
        public Vector3 Position { get; set; }

        // The camera's rotation represented as a quaternion
        public Quaternion Rotation
        {
            get => Quaternion.FromEulerAngles(_pitch, _yaw, 0.0f);
            set
            {
                var euler = value.ToEulerAngles();
                _pitch = euler.X;
                _yaw = euler.Y;
                UpdateVectors();
            }
        }

        // Aspect ratio of the viewport, used for the projection matrix
        public float AspectRatio { private get; set; }

        // Read-only properties for direction vectors
        public Vector3 Front => _front;
        public Vector3 Up => _up;
        public Vector3 Right => _right;

        // Pitch (up/down rotation) in degrees
        public float Pitch
        {
            get => MathHelper.RadiansToDegrees(_pitch);
            set
            {
                var angle = MathHelper.Clamp(value, -89f, 89f);
                _pitch = MathHelper.DegreesToRadians(angle);
                UpdateVectors();
            }
        }

        // Yaw (left/right rotation) in degrees
        public float Yaw
        {
            get => MathHelper.RadiansToDegrees(_yaw);
            set
            {
                _yaw = MathHelper.DegreesToRadians(value);
                UpdateVectors();
            }
        }

        // Field of view in degrees, clamped between 1 and 90
        public float Fov
        {
            get => MathHelper.RadiansToDegrees(_fov);
            set
            {
                var angle = MathHelper.Clamp(value, 1f, 90f);
                _fov = MathHelper.DegreesToRadians(angle);
            }
        }

        // Returns the view matrix calculated using LookAt
        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + _front, _up);
        }

        // Returns the projection matrix for the camera
        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, 0.01f, 100f);
        }

        // Update the front, right, and up vectors based on the current pitch and yaw
        private void UpdateVectors()
        {
            // Calculate the new front vector
            _front.X = MathF.Cos(_pitch) * MathF.Cos(_yaw);
            _front.Y = MathF.Sin(_pitch);
            _front.Z = MathF.Cos(_pitch) * MathF.Sin(_yaw);
            _front = Vector3.Normalize(_front);

            // Recalculate right and up vectors
            _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
            _up = Vector3.Normalize(Vector3.Cross(_right, _front));
        }
    }
}

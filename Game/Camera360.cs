using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public class Camera360 : Camera
    {
        private Quaternion _rotation = Quaternion.Identity;

        public Camera360(Vector3 position, float aspectRatio)
            : base(position, aspectRatio)
        {
        }

        protected override void UpdateVectors()
        {
            // Transform base vectors using the rotation quaternion
            _front = Vector3.Transform(-Vector3.UnitZ, _rotation);
            _up = Vector3.Transform(Vector3.UnitY, _rotation);
            _right = Vector3.Transform(Vector3.UnitX, _rotation);
        }

        /// <summary>
        /// Rotates the camera based on mouse movement.
        /// </summary>
        /// <param name="deltaX">Change in mouse X position.</param>
        /// <param name="deltaY">Change in mouse Y position.</param>
        public void Rotate(float deltaX, float deltaY)
        {
            float sensitivity = 0.002f;

            // Get local axes before updating
            Vector3 localUp = _up;
            Vector3 localRight = _right;

            // Create quaternions for yaw and pitch rotations
            Quaternion rotationYaw = Quaternion.FromAxisAngle(localUp, -deltaX * sensitivity);
            Quaternion rotationPitch = Quaternion.FromAxisAngle(localRight, -deltaY * sensitivity);

            // Apply rotations
            _rotation = rotationYaw * _rotation;
            _rotation = rotationPitch * _rotation;

            // Normalize the quaternion to prevent drift
            _rotation = Quaternion.Normalize(_rotation);

            // Update orientation vectors
            UpdateVectors();
        }

        /// <summary>
        /// Rolls the camera around the front axis.
        /// </summary>
        /// <param name="deltaZ">Amount to roll.</param>
        public void Roll(float deltaZ)
        {
            float sensitivity = 0.002f;

            // Use the local front vector as the axis for rolling
            Vector3 localFront = _front;

            // Create a quaternion for the roll rotation
            Quaternion rotationRoll = Quaternion.FromAxisAngle(localFront, deltaZ * sensitivity);

            // Apply the roll rotation
            _rotation = rotationRoll * _rotation;

            // Normalize the quaternion
            _rotation = Quaternion.Normalize(_rotation);

            // Update orientation vectors
            UpdateVectors();
        }

        /// <summary>
        /// Sets the camera's rotation directly from a quaternion.
        /// </summary>
        /// <param name="rotation">The rotation quaternion.</param>
        public void SetRotation(Quaternion rotation)
        {
            _rotation = Quaternion.Normalize(rotation);
            UpdateVectors();
        }

        /// <summary>
        /// Gets the current rotation quaternion of the camera.
        /// </summary>
        /// <returns>The rotation quaternion.</returns>
        public Quaternion GetRotation()
        {
            return _rotation;
        }
    }
}

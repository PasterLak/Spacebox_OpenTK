using OpenTK.Mathematics;
using Engine;

namespace Engine
{
    public class Camera360 : Camera
    {
        private Quaternion _rotation = Quaternion.Identity;

        public Camera360(Vector3 position, bool isMainCamera = true)
            : base(position, isMainCamera)
        {
        }

        protected override void UpdateVectors()
        {
            _front = Vector3.Transform(-Vector3.UnitZ, _rotation);
            _up = Vector3.Transform(Vector3.UnitY, _rotation);
            _right = Vector3.Transform(Vector3.UnitX, _rotation);

            //Rotation = _rotation.ToEulerAngles() * 360f;
        }


        public void Rotate(float deltaX, float deltaY)
        {
            const float sensitivity = 0.002f;

            Vector3 localUp = _up;
            Vector3 localRight = _right;

            Quaternion rotationYaw = Quaternion.FromAxisAngle(localUp, -deltaX * sensitivity);
            Quaternion rotationPitch = Quaternion.FromAxisAngle(localRight, -deltaY * sensitivity);

            _rotation = rotationYaw * _rotation;
            _rotation = rotationPitch * _rotation;

            _rotation = Quaternion.Normalize(_rotation);


            UpdateVectors();
        }


        public void Roll(float deltaZ)
        {
            const float sensitivity = 0.002f;


            Vector3 localFront = _front;


            Quaternion rotationRoll = Quaternion.FromAxisAngle(localFront, deltaZ * sensitivity);

            _rotation = rotationRoll * _rotation;

            _rotation = Quaternion.Normalize(_rotation);


            UpdateVectors();
        }


        public void SetRotation(Quaternion rotation)
        {
            _rotation = Quaternion.Normalize(rotation);
            UpdateVectors();
        }


        public Quaternion GetRotation()
        {
            return _rotation;
        }
    }
}
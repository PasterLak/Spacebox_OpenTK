// Файл: Camera360.cs
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
            // Трансформируем базовые векторы с помощью кватерниона
            _front = Vector3.Transform(-Vector3.UnitZ, _rotation);
            _up = Vector3.Transform(Vector3.UnitY, _rotation);
            _right = Vector3.Transform(Vector3.UnitX, _rotation);
        }



        public void Rotate(float deltaX, float deltaY)
        {
            float sensitivity = 0.002f;

            // Получаем локальные оси до обновления
            Vector3 localUp = _up;
            Vector3 localRight = _right;

            // Создаем кватернионы вращения вокруг локальных осей
            Quaternion rotationYaw = Quaternion.FromAxisAngle(localUp, -deltaX * sensitivity);
            Quaternion rotationPitch = Quaternion.FromAxisAngle(localRight, -deltaY * sensitivity);

            // Применяем вращения
            _rotation = rotationYaw * _rotation;
            _rotation = rotationPitch * _rotation;

            // Нормализуем кватернион
            _rotation = Quaternion.Normalize(_rotation);

            // Обновляем векторы ориентации
            UpdateVectors();
        }


        public void Roll(float deltaZ)
        {
            float sensitivity = 0.002f;

            // Используем локальный фронтальный вектор (до обновления) в качестве оси вращения
            Vector3 localFront = _front;

            // Создаем кватернион вращения вокруг локальной оси Z
            Quaternion rotationRoll = Quaternion.FromAxisAngle(localFront, deltaZ * sensitivity);

            // Применяем вращение
            _rotation = rotationRoll * _rotation;

            // Нормализуем кватернион
            _rotation = Quaternion.Normalize(_rotation);

            // Обновляем векторы ориентации
            UpdateVectors();
        }



    }
}

using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class Transform
    {
        /// <summary>
        /// Позиция объекта в мировом пространстве.
        /// </summary>
        public Vector3 Position { get; set; } = Vector3.Zero;

        /// <summary>
        /// Вращение объекта в виде углов Эйлера (в градусах).
        /// </summary>
        public Vector3 Rotation { get; set; } = Vector3.Zero; // Euler angles in degrees

        /// <summary>
        /// Масштаб объекта.
        /// </summary>
        public Vector3 Scale { get; set; } = Vector3.One;

        /// <summary>
        /// Получает матрицу модели, основанную на позиции, вращении и масштабе.
        /// </summary>
        /// <returns>Матрица модели.</returns>
        public Matrix4 GetModelMatrix()
        {
            var translation = Matrix4.CreateTranslation(Position);
            var rotationX = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(Rotation.X));
            var rotationY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(Rotation.Y));
            var rotationZ = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation.Z));
            var rotation = rotationZ * rotationY * rotationX;
            var scale = Matrix4.CreateScale(Scale);
            return scale * rotation * translation;
        }

        /// <summary>
        /// Перемещает объект на заданный вектор.
        /// </summary>
        /// <param name="translation">Вектор перемещения.</param>
        public void Translate(Vector3 translation)
        {
            Position += translation;
        }

        /// <summary>
        /// Вращает объект на заданные углы Эйлера (в градусах).
        /// </summary>
        /// <param name="eulerAngles">Углы Эйлера для вращения.</param>
        public void Rotate(Vector3 eulerAngles)
        {
            Rotation += eulerAngles;

            // Ограничение углов вращения для предотвращения переполнения
            Rotation = new Vector3(
                WrapAngleDegrees(Rotation.X),
                WrapAngleDegrees(Rotation.Y),
                WrapAngleDegrees(Rotation.Z)
            );
        }

        /// <summary>
        /// Вращает объект вокруг заданной оси на определённый угол (в градусах).
        /// </summary>
        /// <param name="angleDegrees">Угол вращения в градусах.</param>
        /// <param name="axis">Ось вращения.</param>
        public void Rotate(float angleDegrees, Vector3 axis)
        {
            Quaternion rotationQuat = Quaternion.FromAxisAngle(Vector3.Normalize(axis), MathHelper.DegreesToRadians(angleDegrees));
            Quaternion currentRotation = Quaternion.FromEulerAngles(MathHelper.DegreesToRadians(Rotation.X), MathHelper.DegreesToRadians(Rotation.Y), MathHelper.DegreesToRadians(Rotation.Z));
            currentRotation = Quaternion.Normalize(rotationQuat * currentRotation);
            Vector3 euler = currentRotation.ToEulerAngles();

            Rotation = new Vector3(
                MathHelper.RadiansToDegrees(euler.X),
                MathHelper.RadiansToDegrees(euler.Y),
                MathHelper.RadiansToDegrees(euler.Z)
            );
        }

        /// <summary>
        /// Устанавливает позицию объекта.
        /// </summary>
        /// <param name="position">Новая позиция.</param>
        public void SetPosition(Vector3 position)
        {
            Position = position;
        }

        /// <summary>
        /// Устанавливает вращение объекта.
        /// </summary>
        /// <param name="eulerAngles">Углы Эйлера для вращения (в градусах).</param>
        public void SetRotation(Vector3 eulerAngles)
        {
            Rotation = new Vector3(
                WrapAngleDegrees(eulerAngles.X),
                WrapAngleDegrees(eulerAngles.Y),
                WrapAngleDegrees(eulerAngles.Z)
            );
        }

        /// <summary>
        /// Устанавливает масштаб объекта.
        /// </summary>
        /// <param name="scale">Новый масштаб.</param>
        public void SetScale(Vector3 scale)
        {
            Scale = scale;
        }

        /// <summary>
        /// Сбрасывает трансформацию до значений по умолчанию.
        /// </summary>
        public void Reset()
        {
            Position = Vector3.Zero;
            Rotation = Vector3.Zero;
            Scale = Vector3.One;
        }

        /// <summary>
        /// Вектор, указывающий вперед относительно текущего вращения.
        /// </summary>
        public Vector3 Forward
        {
            get
            {
                Quaternion rotation = Quaternion.FromEulerAngles(
                    MathHelper.DegreesToRadians(Rotation.X),
                    MathHelper.DegreesToRadians(Rotation.Y),
                    MathHelper.DegreesToRadians(Rotation.Z));
                return Vector3.Transform(Vector3.UnitZ, rotation);
            }
        }

        /// <summary>
        /// Вектор, указывающий вверх относительно текущего вращения.
        /// </summary>
        public Vector3 Up
        {
            get
            {
                Quaternion rotation = Quaternion.FromEulerAngles(
                    MathHelper.DegreesToRadians(Rotation.X),
                    MathHelper.DegreesToRadians(Rotation.Y),
                    MathHelper.DegreesToRadians(Rotation.Z));
                return Vector3.Transform(Vector3.UnitY, rotation);
            }
        }

        /// <summary>
        /// Вектор, указывающий вправо относительно текущего вращения.
        /// </summary>
        public Vector3 Right
        {
            get
            {
                Quaternion rotation = Quaternion.FromEulerAngles(
                    MathHelper.DegreesToRadians(Rotation.X),
                    MathHelper.DegreesToRadians(Rotation.Y),
                    MathHelper.DegreesToRadians(Rotation.Z));
                return Vector3.Transform(Vector3.UnitX, rotation);
            }
        }

        /// <summary>
        /// Переводит точку из локального пространства в мировое.
        /// </summary>
        /// <param name="point">Точка в локальном пространстве.</param>
        /// <returns>Точка в мировом пространстве.</returns>
        /*public Vector3 TransformPoint(Vector3 point)
        {
            return Vector3.Transform(point, GetModelMatrix());
        }*/

        /// <summary>
        /// Переводит направление из локального пространства в мировое.
        /// </summary>
        /// <param name="direction">Направление в локальном пространстве.</param>
        /// <returns>Направление в мировом пространстве.</returns>
        public Vector3 TransformDirection(Vector3 direction)
        {
            Quaternion rotation = Quaternion.FromEulerAngles(
                MathHelper.DegreesToRadians(Rotation.X),
                MathHelper.DegreesToRadians(Rotation.Y),
                MathHelper.DegreesToRadians(Rotation.Z));
            return Vector3.Transform(Vector3.Normalize(direction), rotation);
        }

        /// <summary>
        /// Вращает трансформацию так, чтобы она смотрела на заданную точку.
        /// </summary>
        /// <param name="target">Целевая точка для взгляда.</param>
        /// <param name="up">Вектор, указывающий вверх.</param>
        public void LookAt(Vector3 target)
        {
            Vector3 up = Vector3.UnitY;
            Vector3 direction = Vector3.Normalize(target - Position);
            if (direction == Vector3.Zero)
                return; // Невозможно смотреть на себя

            // Вычисление правого вектора
            Vector3 right = Vector3.Normalize(Vector3.Cross(up, direction));
            // Пересчитываем настоящий вектор "вверх"
            Vector3 trueUp = Vector3.Cross(direction, right);

            // Создаём матрицу вращения
            Matrix4 rotationMatrix = new Matrix4(
                new Vector4(right, 0),
                new Vector4(trueUp, 0),
                new Vector4(direction, 0),
                new Vector4(0, 0, 0, 1)
            );

            // Извлекаем углы Эйлера из матрицы вращения
            float pitch, yaw, roll;

            if (rotationMatrix.M31 < 1)
            {
                if (rotationMatrix.M31 > -1)
                {
                    pitch = MathF.Asin(-rotationMatrix.M31);
                    yaw = MathF.Atan2(rotationMatrix.M21, rotationMatrix.M11);
                    roll = MathF.Atan2(rotationMatrix.M32, rotationMatrix.M33);
                }
                else
                {
                    // rotationMatrix.M31 <= -1
                    pitch = MathHelper.PiOver2;
                    yaw = -MathF.Atan2(-rotationMatrix.M12, rotationMatrix.M22);
                    roll = 0;
                }
            }
            else
            {
                // rotationMatrix.M31 >= 1
                pitch = -MathHelper.PiOver2;
                yaw = MathF.Atan2(-rotationMatrix.M12, rotationMatrix.M22);
                roll = 0;
            }

           
            Rotation = new Vector3(MathHelper.RadiansToDegrees(pitch), MathHelper.RadiansToDegrees(yaw), MathHelper.RadiansToDegrees(roll));
        }

        /// <summary>
        /// Ограничивает угол в градусах до диапазона [0, 360).
        /// </summary>
        /// <param name="angle">Угол в градусах.</param>
        /// <returns>Ограниченный угол.</returns>
        private float WrapAngleDegrees(float angle)
        {
            angle %= 360f;
            if (angle < 0)
                angle += 360f;
            return angle;
        }
    }
}

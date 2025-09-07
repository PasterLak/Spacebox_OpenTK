using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Spacebox.Game.Generation.Blocks
{
    public static class BlockRotationHelper
    {
        public static Matrix4 CalculateTransformMatrix(Direction baseFrontDirection, Direction facingDirection, Rotation rotation)
        {
            Vector3 baseFrontVector = GetDirectionVector(baseFrontDirection);
            Vector3 targetVector = GetDirectionVector(facingDirection);

            Quaternion directionRotation = CreateFromToRotation(baseFrontVector, targetVector);
            Quaternion additionalRotation = GetRotationQuaternion(baseFrontDirection, rotation, directionRotation);

            Quaternion finalRotation = additionalRotation * directionRotation;

            return Matrix4.CreateFromQuaternion(finalRotation);
        }

        private static Quaternion CreateFromToRotation(Vector3 from, Vector3 to)
        {
            from = Vector3.Normalize(from);
            to = Vector3.Normalize(to);

            float dot = Vector3.Dot(from, to);

            if (dot > 0.999999f)
                return Quaternion.Identity;

            if (dot < -0.999999f)
            {
                Vector3 axis = Vector3.Cross(Vector3.UnitX, from);
                if (axis.LengthSquared < 0.001f)
                    axis = Vector3.Cross(Vector3.UnitY, from);
                axis = Vector3.Normalize(axis);
                return Quaternion.FromAxisAngle(axis, MathHelper.Pi);
            }

            Vector3 cross = Vector3.Cross(from, to);
            float s = MathF.Sqrt((1 + dot) * 2);
            float invs = 1f / s;

            return new Quaternion(cross.X * invs, cross.Y * invs, cross.Z * invs, s * 0.5f);
        }

        public static Vector3 GetDirectionVector(Direction direction)
        {
            return direction switch
            {
                Direction.Forward => Vector3.UnitZ,
                Direction.Back => -Vector3.UnitZ,
                Direction.Left => -Vector3.UnitX,
                Direction.Right => Vector3.UnitX,
                Direction.Up => Vector3.UnitY,
                Direction.Down => -Vector3.UnitY,
                _ => Vector3.UnitZ
            };
        }

        private static Quaternion GetRotationQuaternion(Direction baseFrontDirection, Rotation rotation, Quaternion directionRotation)
        {
            Vector3 localAxis = GetDirectionVector(baseFrontDirection);
            Vector3 rotatedAxis = Vector3.Transform(localAxis, directionRotation);

            float angle = rotation switch
            {
                Rotation.None => 0f,
                Rotation.Right => -90f,
                Rotation.Half => 180f,
                Rotation.Left => 90f,
                _ => 0f
            };

            return Quaternion.FromAxisAngle(Vector3.Normalize(rotatedAxis), MathHelper.DegreesToRadians(angle));
        }

        public static Dictionary<Direction, Vector3> GetTransformedFaceVectors(Direction baseFrontDirection, Direction facingDirection, Rotation rotation)
        {
            Matrix4 transformMatrix = CalculateTransformMatrix(baseFrontDirection, facingDirection, rotation);

            return new Dictionary<Direction, Vector3>
            {
                [Direction.Forward] = Vector3.TransformVector(Vector3.UnitZ, transformMatrix).Normalized(),
                [Direction.Back] = Vector3.TransformVector(-Vector3.UnitZ, transformMatrix).Normalized(),
                [Direction.Left] = Vector3.TransformVector(-Vector3.UnitX, transformMatrix).Normalized(),
                [Direction.Right] = Vector3.TransformVector(Vector3.UnitX, transformMatrix).Normalized(),
                [Direction.Up] = Vector3.TransformVector(Vector3.UnitY, transformMatrix).Normalized(),
                [Direction.Down] = Vector3.TransformVector(-Vector3.UnitY, transformMatrix).Normalized()
            };
        }

        public static Vector3 GetFaceUp(Dictionary<Direction, Vector3> faceVectors, Direction face)
        {
            return face switch
            {
                Direction.Forward or Direction.Back or Direction.Left or Direction.Right => faceVectors[Direction.Up],
                Direction.Up => faceVectors[Direction.Back],
                Direction.Down => faceVectors[Direction.Forward],
                _ => Vector3.UnitY
            };
        }

        public static Vector3 GetFaceRight(Dictionary<Direction, Vector3> faceVectors, Direction face)
        {
            return face switch
            {
                Direction.Forward => faceVectors[Direction.Right],
                Direction.Back => faceVectors[Direction.Left],
                Direction.Left => faceVectors[Direction.Forward],
                Direction.Right => faceVectors[Direction.Back],
                Direction.Up or Direction.Down => faceVectors[Direction.Right],
                _ => Vector3.UnitX
            };
        }
    }
}
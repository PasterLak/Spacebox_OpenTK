using System;
using OpenTK.Mathematics;
using Spacebox.Game.Generation.Blocks;
using Spacebox.Game.Resource;

namespace Spacebox.Game.Generation.Tools
{
    public static class BlockPlacementHelper
    {
        private static readonly Vector3[] DirectionVectors = {
            -Vector3.UnitY,
            Vector3.UnitY,
            -Vector3.UnitX,
            Vector3.UnitX,
            -Vector3.UnitZ,
            Vector3.UnitZ
        };

        public static void CalculateOrientation(
            Vector3 playerPosition,
            Vector3 blockPosition,
            Vector3 playerUp,
            Vector3 surfaceNormal,
            bool isSurfaceMode,
            BlockData blockData,
            Rotation additionalRotation,
            out Direction finalDir,
            out Rotation finalRot)
        {
            if (blockData.AllSidesAreSame())
            {
                finalDir = Direction.Up;
                finalRot = Rotation.None;
                return;
            }

            Vector3 toPlayer = (playerPosition - blockPosition).Normalized();

            FindBestMatch(
                toPlayer,
                playerUp,
                surfaceNormal,
                isSurfaceMode,
                blockData.BaseFrontDirection,
                out var bestDir,
                out var bestRot
            );

            finalDir = bestDir;
            int combinedRot = ((int)bestRot + (int)additionalRotation) % 4;
            finalRot = (Rotation)combinedRot;
        }

        private static void FindBestMatch(
            Vector3 toPlayer,
            Vector3 playerUp,
            Vector3 surfaceNormal,
            bool isSurfaceMode,
            Direction baseFrontDir,
            out Direction bestDir,
            out Rotation bestRot)
        {
            float maxScore = -float.MaxValue;
            bestDir = Direction.Up;
            bestRot = Rotation.None;

            Vector3 flatToPlayer = Vector3.Zero;
            if (isSurfaceMode)
            {
                flatToPlayer = (toPlayer - Vector3.Dot(toPlayer, surfaceNormal) * surfaceNormal).Normalized();
                if (flatToPlayer.LengthSquared < 0.01f) flatToPlayer = Vector3.UnitX;
            }

            bool isVerticalSurface = MathF.Abs(surfaceNormal.Y) > 0.7f;

            for (int d = 0; d < 6; d++)
            {
                Direction dir = (Direction)d;
                for (int r = 0; r < 4; r++)
                {
                    Rotation rot = (Rotation)r;

                    Matrix4 transform = BlockRotationHelper.CalculateTransformMatrix(baseFrontDir, dir, rot);

                    Vector3 worldModelY = Vector3.TransformVector(Vector3.UnitY, transform);
                    Vector3 worldModelZ = Vector3.TransformVector(Vector3.UnitZ, transform);
                    Vector3 worldModelFace = Vector3.TransformVector(GetVectorFromDirection(baseFrontDir), transform);

                    if (isSurfaceMode)
                    {
                        if (isVerticalSurface)
                        {
                            if (Vector3.Dot(worldModelY, surfaceNormal) > 0.9f)
                            {
                                float score = Vector3.Dot(worldModelZ, flatToPlayer);
                                if (score > maxScore)
                                {
                                    maxScore = score;
                                    bestDir = dir;
                                    bestRot = rot;
                                }
                            }
                        }
                        else
                        {
                            if (Vector3.Dot(worldModelFace, surfaceNormal) > 0.9f)
                            {
                                float score = Vector3.Dot(worldModelY, playerUp);
                                if (score > maxScore)
                                {
                                    maxScore = score;
                                    bestDir = dir;
                                    bestRot = rot;
                                }
                            }
                        }
                    }
                    else
                    {
                        Vector3 snapDir = GetDominantAxis(toPlayer);
                        if (Vector3.Dot(worldModelFace, snapDir) > 0.9f)
                        {
                            float score = 100f + Vector3.Dot(worldModelY, playerUp);
                            if (score > maxScore)
                            {
                                maxScore = score;
                                bestDir = dir;
                                bestRot = rot;
                            }
                        }
                    }
                }
            }
        }

        private static Vector3 GetDominantAxis(Vector3 v)
        {
            float x = MathF.Abs(v.X);
            float y = MathF.Abs(v.Y);
            float z = MathF.Abs(v.Z);

            if (x > y && x > z) return v.X > 0 ? Vector3.UnitX : -Vector3.UnitX;
            if (y > x && y > z) return v.Y > 0 ? Vector3.UnitY : -Vector3.UnitY;
            return v.Z > 0 ? Vector3.UnitZ : -Vector3.UnitZ;
        }

        private static Vector3 GetVectorFromDirection(Direction direction)
        {
            return DirectionVectors[(int)direction];
        }
    }
}
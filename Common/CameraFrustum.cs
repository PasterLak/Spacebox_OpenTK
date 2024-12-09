using OpenTK.Mathematics;
using Spacebox.Extensions;

namespace Spacebox.Common
{
    public class CameraFrustum
    {
        private readonly System.Numerics.Plane[] _planes = new System.Numerics.Plane[6];

        public Matrix4 ViewMatrix { get; private set; }
        public Matrix4 ProjectionMatrix { get; private set; }

        public CameraFrustum Copy()
        {
            CameraFrustum frustum = new CameraFrustum();
            frustum.UpdateFrustum(ViewMatrix, ProjectionMatrix);
            
            return frustum;
        }

        public void UpdateFrustum(Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            ViewMatrix = viewMatrix;
            ProjectionMatrix = projectionMatrix;

            Matrix4 clipMatrix = ViewMatrix * ProjectionMatrix;

            // Извлекаем плоскости из комбинированной матрицы
            // Левая плоскость
            _planes[0] = CreatePlane(
                clipMatrix.M14 + clipMatrix.M11,
                clipMatrix.M24 + clipMatrix.M21,
                clipMatrix.M34 + clipMatrix.M31,
                clipMatrix.M44 + clipMatrix.M41
            );

            // Правая плоскость
            _planes[1] = CreatePlane(
                clipMatrix.M14 - clipMatrix.M11,
                clipMatrix.M24 - clipMatrix.M21,
                clipMatrix.M34 - clipMatrix.M31,
                clipMatrix.M44 - clipMatrix.M41
            );

            // Нижняя плоскость
            _planes[2] = CreatePlane(
                clipMatrix.M14 + clipMatrix.M12,
                clipMatrix.M24 + clipMatrix.M22,
                clipMatrix.M34 + clipMatrix.M32,
                clipMatrix.M44 + clipMatrix.M42
            );

            // Верхняя плоскость
            _planes[3] = CreatePlane(
                clipMatrix.M14 - clipMatrix.M12,
                clipMatrix.M24 - clipMatrix.M22,
                clipMatrix.M34 - clipMatrix.M32,
                clipMatrix.M44 - clipMatrix.M42
            );

            // Ближняя плоскость
            _planes[4] = CreatePlane(
                clipMatrix.M14 + clipMatrix.M13,
                clipMatrix.M24 + clipMatrix.M23,
                clipMatrix.M34 + clipMatrix.M33,
                clipMatrix.M44 + clipMatrix.M43
            );

            // Дальняя плоскость
            _planes[5] = CreatePlane(
                clipMatrix.M14 - clipMatrix.M13,
                clipMatrix.M24 - clipMatrix.M23,
                clipMatrix.M34 - clipMatrix.M33,
                clipMatrix.M44 - clipMatrix.M43
            );

            // Нормализуем плоскости
            for (int i = 0; i < 6; i++)
            {
                float length = (float)Math.Sqrt(_planes[i].Normal.X * _planes[i].Normal.X +
                                                _planes[i].Normal.Y * _planes[i].Normal.Y +
                                                _planes[i].Normal.Z * _planes[i].Normal.Z);

                _planes[i].Normal.X /= length;
                _planes[i].Normal.Y /= length;
                _planes[i].Normal.Z /= length;
                _planes[i].D /= length;
            }
        }



        private System.Numerics.Plane CreatePlane(float a, float b, float c, float d)
        {
            return new System.Numerics.Plane(a, b, c, d);
        }


        public Vector3[] GetCorners()
        {
            Vector3[] corners = new Vector3[8];

            Matrix4 inverseMatrix = (ProjectionMatrix * ViewMatrix).Inverted();

            int index = 0;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector4 clipSpaceCorner = new Vector4(x, y, z, 1.0f);
                        Vector4 worldSpaceCorner = inverseMatrix * clipSpaceCorner;
                        worldSpaceCorner /= worldSpaceCorner.W;
                        corners[index++] = worldSpaceCorner.Xyz;
                    }
                }
            }

            return corners;
        }


        public bool IsInFrustum(BoundingVolume volume)
        {
            if (volume is BoundingBox box)
            {
                Vector3[] corners = new Vector3[8];
                corners[0] = box.Min;
                corners[1] = new Vector3(box.Max.X, box.Min.Y, box.Min.Z);
                corners[2] = new Vector3(box.Min.X, box.Max.Y, box.Min.Z);
                corners[3] = new Vector3(box.Min.X, box.Min.Y, box.Max.Z);
                corners[4] = new Vector3(box.Max.X, box.Max.Y, box.Min.Z);
                corners[5] = new Vector3(box.Min.X, box.Max.Y, box.Max.Z);
                corners[6] = new Vector3(box.Max.X, box.Min.Y, box.Max.Z);
                corners[7] = box.Max;

                foreach (var plane in _planes)
                {
                    int outside = 0;
                    foreach (var corner in corners)
                    {
                        if (DistanceToPlane(plane, corner) < 0)
                            outside++;
                    }
                    if (outside == 8)
                        return false; // Все вершины за плоскостью
                }
                return true;
            }
            else if (volume is BoundingSphere sphere)
            {
                foreach (var plane in _planes)
                {
                    if (DistanceToPlane(plane, sphere.Center) < -sphere.Radius)
                        return false;
                }
                return true;
            }
            return false;
        }

        private float DistanceToPlane(System.Numerics.Plane plane, Vector3 point)
        {
            return System.Numerics.Vector3.Dot(plane.Normal, point.ToSystemVector3()) + plane.D;
        }
    }
}

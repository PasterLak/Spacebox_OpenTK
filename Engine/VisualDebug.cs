using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Engine.Physics;

namespace Engine
{
    public static class VisualDebug
    {
        private static Shader _shader;
        private static MeshBuffer _bufferPoints;
        private static MeshBuffer _bufferLines;
        private static MeshBuffer _bufferTriangles;
        private static List<float> _points = new List<float>();
        private static List<float> _lines = new List<float>();
        private static List<float> _triangles = new List<float>();
        public static Matrix4 ProjectionMatrix { get; set; }
        public static Matrix4 ViewMatrix { get; set; }
        public static bool Enabled = false;
        public static bool ShowPlayerCollision = false;
        private static List<Collision> _collisions = new List<Collision>();
        public static CameraFrustum CameraFrustum;
        private static float LineWidth = 1.0f;

        static VisualDebug()
        {
            Initialize();
        }

        private static void Initialize()
        {
            _shader = Resources.Load<Shader>("Shaders/debug", true);

            _bufferPoints = new MeshBuffer(new BufferAttribute[]
            {
                new BufferAttribute { Name = "position", Size = 3 },
                new BufferAttribute { Name = "color", Size = 4 },
                new BufferAttribute { Name = "size", Size = 1 }
            });
            _bufferPoints.SetAttributes();

            _bufferLines = new MeshBuffer(new BufferAttribute[]
            {
                new BufferAttribute { Name = "position", Size = 3 },
                new BufferAttribute { Name = "color", Size = 4 }
            });

            _bufferLines.SetAttributes();

            _bufferTriangles = new MeshBuffer(new BufferAttribute[]
            {
                new BufferAttribute { Name = "position", Size = 3 },
                new BufferAttribute { Name = "color", Size = 4 }
            });
            _bufferTriangles.SetAttributes();
        }

        public static void DrawTransform(Node3D transform)
        {
            if (transform == null) return;
            DrawLine(transform.Position, transform.Position + new Vector3(0, 0, 1), new Color4(0, 0, 1, 1));
            DrawBoundingSphere(new BoundingSphere(transform.Position, 0.1f), Color4.Blue);
        }

        public static void AddCollisionToDraw(Collision collision)
        {
            _collisions.Add(collision);
        }

        public static void RemoveCollisionToDraw(Collision collision)
        {
            if (_collisions.Contains(collision))
                _collisions.Remove(collision);
        }

        public static void DrawPoint(Vector3 position, float size, Color4 color)
        {
            if (!Enabled) return;
            _points.Add(position.X);
            _points.Add(position.Y);
            _points.Add(position.Z);
            _points.Add(color.R);
            _points.Add(color.G);
            _points.Add(color.B);
            _points.Add(color.A);
            _points.Add(size);
        }

        public static void DrawLine(Vector3 start, Vector3 end, Color4 color)
        {
            if (!Enabled) return;
            _lines.Add(start.X);
            _lines.Add(start.Y);
            _lines.Add(start.Z);
            _lines.Add(color.R);
            _lines.Add(color.G);
            _lines.Add(color.B);
            _lines.Add(color.A);
            _lines.Add(end.X);
            _lines.Add(end.Y);
            _lines.Add(end.Z);
            _lines.Add(color.R);
            _lines.Add(color.G);
            _lines.Add(color.B);
            _lines.Add(color.A);
        }

        public static void DrawSquare(Vector3 position, Vector2 size, Color4 color)
        {
            if (!Enabled) return;
            Vector3[] corners = new Vector3[4];
            corners[0] = position;
            corners[1] = position + new Vector3(size.X, 0, 0);
            corners[2] = position + new Vector3(size.X, size.Y, 0);
            corners[3] = position + new Vector3(0, size.Y, 0);
            AddTriangle(corners[0], corners[1], corners[2], color);
            AddTriangle(corners[2], corners[3], corners[0], color);
        }

        public static void DrawRay(Ray ray, Color4 color)
        {
            if (!Enabled) return;
            if (ray == null) return;
            Vector3 endPoint = ray.Origin + ray.Direction * ray.Length;
            DrawLine(ray.Origin, endPoint, color);
        }

        private static void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color4 color)
        {
            if (!Enabled) return;
            AddVertexToTriangles(v1, color);
            AddVertexToTriangles(v2, color);
            AddVertexToTriangles(v3, color);
        }

        private static void AddVertexToTriangles(Vector3 position, Color4 color)
        {
            if (!Enabled) return;
            _triangles.Add(position.X);
            _triangles.Add(position.Y);
            _triangles.Add(position.Z);
            _triangles.Add(color.R);
            _triangles.Add(color.G);
            _triangles.Add(color.B);
            _triangles.Add(color.A);
        }

        public static void DrawPosition(Vector3 pos, Color4 color)
        {
            DrawPosition(pos, 16, color);
        }

        public static void DrawPosition(Vector3 pos, byte segments, Color4 color)
        {
            BoundingSphere s = new BoundingSphere(pos, 1f);
            DrawBoundingSphere(s, color, segments);
        }

        public static void DrawBoundingBox(BoundingBox box, Color4 color)
        {
            if (!Enabled) return;
            if (box == null) return;
            Vector3 min = box.Min;
            Vector3 max = box.Max;
            Vector3[] corners = new Vector3[8];
            corners[0] = new Vector3(min.X, min.Y, min.Z);
            corners[1] = new Vector3(max.X, min.Y, min.Z);
            corners[2] = new Vector3(max.X, max.Y, min.Z);
            corners[3] = new Vector3(min.X, max.Y, min.Z);
            corners[4] = new Vector3(min.X, min.Y, max.Z);
            corners[5] = new Vector3(max.X, min.Y, max.Z);
            corners[6] = new Vector3(max.X, max.Y, max.Z);
            corners[7] = new Vector3(min.X, max.Y, max.Z);
            DrawLine(corners[0], corners[1], color);
            DrawLine(corners[1], corners[2], color);
            DrawLine(corners[2], corners[3], color);
            DrawLine(corners[3], corners[0], color);
            DrawLine(corners[4], corners[5], color);
            DrawLine(corners[5], corners[6], color);
            DrawLine(corners[6], corners[7], color);
            DrawLine(corners[7], corners[4], color);
            DrawLine(corners[0], corners[4], color);
            DrawLine(corners[1], corners[5], color);
            DrawLine(corners[2], corners[6], color);
            DrawLine(corners[3], corners[7], color);
        }

        public static void DrawSphere(Vector3 center, float radius, Color4 color)
        {
            DrawSphere(center, radius, 16, color);
        }

        public static void DrawSphere(Vector3 center, float radius, byte segments, Color4 color)
        {
            if (!Enabled) return;
            if (radius <= 0) return;
            BoundingSphere sphere = new BoundingSphere(center, radius);
            DrawBoundingSphere(sphere, color, segments);
        }

        public static void DrawBoundingSphere(BoundingSphere sphere, Color4 color)
        {
            DrawBoundingSphere(sphere, color, 16);
        }

        public static void DrawBoundingSphere(BoundingSphere sphere, Color4 color, byte segments)
        {
            if (!Enabled) return;
            if (sphere == null) return;
            byte slices = segments;
            byte stacks = segments;
            float radius = sphere.Radius;
            Vector3 center = sphere.Center;
            for (byte i = 0; i <= stacks; i++)
            {
                float theta = MathHelper.Pi * i / stacks;
                float sinTheta = MathF.Sin(theta);
                float cosTheta = MathF.Cos(theta);
                Vector3 prevPoint = new Vector3(
                    radius * sinTheta * MathF.Cos(0),
                    radius * cosTheta,
                    radius * sinTheta * MathF.Sin(0)) + center;
                for (byte j = 1; j <= slices; j++)
                {
                    float phi = 2 * MathHelper.Pi * j / slices;
                    Vector3 currentPoint = new Vector3(
                        radius * sinTheta * MathF.Cos(phi),
                        radius * cosTheta,
                        radius * sinTheta * MathF.Sin(phi)) + center;
                    DrawLine(prevPoint, currentPoint, color);
                    prevPoint = currentPoint;
                }
            }
            for (byte j = 0; j < slices; j++)
            {
                float phi = 2 * MathHelper.Pi * j / slices;
                Vector3 prevPoint = new Vector3(
                    radius * MathF.Sin(0) * MathF.Cos(phi),
                    radius * MathF.Cos(0),
                    radius * MathF.Sin(0) * MathF.Sin(phi)) + center;
                for (byte i = 1; i <= stacks; i++)
                {
                    float theta = MathHelper.Pi * i / stacks;
                    Vector3 currentPoint = new Vector3(
                        radius * MathF.Sin(theta) * MathF.Cos(phi),
                        radius * MathF.Cos(theta),
                        radius * MathF.Sin(theta) * MathF.Sin(phi)) + center;
                    DrawLine(prevPoint, currentPoint, color);
                    prevPoint = currentPoint;
                }
            }
        }

        public static void DrawFrustum(CameraFrustum frustum, Color4 color)
        {
            if (!Enabled) return;
            Vector3[] frustumCorners = frustum.GetCorners();
            DrawLine(frustumCorners[0], frustumCorners[1], color);
            DrawLine(frustumCorners[1], frustumCorners[2], color);
            DrawLine(frustumCorners[2], frustumCorners[3], color);
            DrawLine(frustumCorners[3], frustumCorners[0], color);
            DrawLine(frustumCorners[4], frustumCorners[5], color);
            DrawLine(frustumCorners[5], frustumCorners[6], color);
            DrawLine(frustumCorners[6], frustumCorners[7], color);
            DrawLine(frustumCorners[7], frustumCorners[4], color);
            DrawLine(frustumCorners[0], frustumCorners[4], color);
            DrawLine(frustumCorners[1], frustumCorners[5], color);
            DrawLine(frustumCorners[2], frustumCorners[6], color);
            DrawLine(frustumCorners[3], frustumCorners[7], color);
        }

        public static void DrawAxes(Vector3 pos)
        {
            DrawLine(pos, pos + Vector3.UnitX * 2, Color4.Red);
            DrawLine(pos, pos + Vector3.UnitY * 2, Color4.Green);
            DrawLine(pos, pos + Vector3.UnitZ * 2, Color4.Blue);
        }

        public static void Render()
        {
            if (!Enabled) return;
            if (_points.Count == 0 && _lines.Count == 0 && _triangles.Count == 0)
                return;
            foreach (var col in _collisions)
            {
                col.DrawDebug();
            }
            _shader.Use();
            Matrix4 matrix = Matrix4.Identity;
            if (Camera.Main != null && Camera.Main.CameraRelativeRender)
            {
                matrix *= Matrix4.CreateTranslation(Vector3.Zero - Camera.Main.Position);
            }
            _shader.SetMatrix4("model", matrix);
            _shader.SetMatrix4("view", ViewMatrix);
            _shader.SetMatrix4("projection", ProjectionMatrix);
            GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
            if (_points.Count > 0)
            {
                GL.BindVertexArray(_bufferPoints.VAO);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _bufferPoints.VBO);
                float[] pointsArray = _points.ToArray();
                GL.BufferData(BufferTarget.ArrayBuffer, pointsArray.Length * sizeof(float), pointsArray, BufferUsageHint.DynamicDraw);
                GL.DrawArrays(PrimitiveType.Points, 0, pointsArray.Length / 8);
                _points.Clear();
            }
            if (_lines.Count > 0)
            {
                GL.BindVertexArray(_bufferLines.VAO);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _bufferLines.VBO);
                float[] linesArray = _lines.ToArray();
                GL.BufferData(BufferTarget.ArrayBuffer, linesArray.Length * sizeof(float), linesArray, BufferUsageHint.DynamicDraw);
                GL.LineWidth(LineWidth);
                GL.DrawArrays(PrimitiveType.Lines, 0, linesArray.Length / 7);
                _lines.Clear();
            }
            if (_triangles.Count > 0)
            {
                GL.BindVertexArray(_bufferTriangles.VAO);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _bufferTriangles.VBO);
                float[] trianglesArray = _triangles.ToArray();
                GL.BufferData(BufferTarget.ArrayBuffer, trianglesArray.Length * sizeof(float), trianglesArray, BufferUsageHint.DynamicDraw);
                GL.DrawArrays(PrimitiveType.Triangles, 0, trianglesArray.Length / 7);
                _triangles.Clear();
            }
            GL.BindVertexArray(0);
            GL.Disable(EnableCap.CullFace);
        }

        public static void SetLineWidth(float width)
        {
            LineWidth = width;
        }

        public static void Log(string data)
        {
            Console.WriteLine(data);
        }

        public static void Log(int data)
        {
            Console.WriteLine(data);
        }

        public static void Log(float data)
        {
            Console.WriteLine(data);
        }

        public static void Log(byte data)
        {
            Console.WriteLine(data);
        }

        public static void Clear()
        {
            _points.Clear();
            _lines.Clear();
            _triangles.Clear();
            _collisions.Clear();
        }
    }
}

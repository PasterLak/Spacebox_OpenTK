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
        private static readonly List<float> _points = new();
        private static readonly List<float> _lines = new();
        private static readonly List<float> _triangles = new();
        public static Matrix4 ProjectionMatrix { get; set; }
        public static Matrix4 ViewMatrix { get; set; }
        public static bool Enabled = false;
     
        private static float LineWidth = 1.0f;

        static VisualDebug()
        {
            Initialize();
            
        }

        private static void Initialize()
        {
            _shader = Resources.Load<Shader>("Shaders/debug", true);

            _bufferPoints = new MeshBuffer(new[]
            {
                new BufferAttribute { Name = "position", Size = 3 },
                new BufferAttribute { Name = "color", Size = 4 },
                new BufferAttribute { Name = "size", Size = 1 }
            });
            _bufferPoints.SetAttributes();

            _bufferLines = new MeshBuffer(new[]
            {
                new BufferAttribute { Name = "position", Size = 3 },
                new BufferAttribute { Name = "color", Size = 4 }
            });
            _bufferLines.SetAttributes();

            _bufferTriangles = new MeshBuffer(new[]
            {
                new BufferAttribute { Name = "position", Size = 3 },
                new BufferAttribute { Name = "color", Size = 4 }
            });
            _bufferTriangles.SetAttributes();
        }

        public static void DrawTransform(Node3D transform)
        {
            if (transform == null) return;
            var pos = transform.PositionWorld;
            DrawLine(pos, pos + Vector3.UnitZ, new Color4(0, 0, 1, 1));
            DrawBoundingSphere(new BoundingSphere(pos, 0.1f), Color4.Blue);
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
            Vector3[] corners =
            {
                position,
                position + new Vector3(size.X, 0, 0),
                position + new Vector3(size.X, size.Y, 0),
                position + new Vector3(0, size.Y, 0)
            };
            AddTriangle(corners[0], corners[1], corners[2], color);
            AddTriangle(corners[2], corners[3], corners[0], color);
        }

        public static void DrawRay(Ray ray, Color4 color)
        {
            if (!Enabled || ray == null) return;
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

        public static void DrawOBB(BoundingBoxOBB obb, Color4 color)
        {
            if (!Enabled) return;
            Vector3[] corners = obb.GetCorners();
            DrawLine(corners[0], corners[1], color);
            DrawLine(corners[1], corners[3], color);
            DrawLine(corners[3], corners[2], color);
            DrawLine(corners[2], corners[0], color);
            DrawLine(corners[4], corners[5], color);
            DrawLine(corners[5], corners[7], color);
            DrawLine(corners[7], corners[6], color);
            DrawLine(corners[6], corners[4], color);
            DrawLine(corners[0], corners[4], color);
            DrawLine(corners[1], corners[5], color);
            DrawLine(corners[2], corners[6], color);
            DrawLine(corners[3], corners[7], color);
        }

        public static void DrawBoundingBox(BoundingBox box, Color4 color)
        {
            if (!Enabled || box == null) return;
            Vector3 min = box.Min;
            Vector3 max = box.Max;
            Vector3[] corners =
            {
                new Vector3(min.X, min.Y, min.Z),
                new Vector3(max.X, min.Y, min.Z),
                new Vector3(max.X, max.Y, min.Z),
                new Vector3(min.X, max.Y, min.Z),
                new Vector3(min.X, min.Y, max.Z),
                new Vector3(max.X, min.Y, max.Z),
                new Vector3(max.X, max.Y, max.Z),
                new Vector3(min.X, max.Y, max.Z)
            };
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
            if (!Enabled || radius <= 0) return;
            BoundingSphere sphere = new BoundingSphere(center, radius);
            DrawBoundingSphere(sphere, color, segments);
        }

        public static void DrawBoundingSphere(BoundingSphere sphere, Color4 color)
        {
            DrawBoundingSphere(sphere, color, 16);
        }

        public static void DrawBoundingSphere(BoundingSphere sphere, Color4 color, byte segments)
        {
            if (!Enabled || sphere == null) return;
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

        public static void DrawAxes(Vector3 pos)
        {
            DrawLine(pos, pos + Vector3.UnitX * 2, Color4.Red);
            DrawLine(pos, pos + Vector3.UnitY * 2, Color4.Green);
            DrawLine(pos, pos + Vector3.UnitZ * 2, Color4.Blue);
        }
        public static void DrawAxes(Node3D node, float length = 1f)
        {
            if (!Enabled || node == null) return;

            Matrix4 model = node.GetRenderModelMatrix();
            Vector3 origin = node.PositionWorld;

            Vector3 xDir = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitX, model)) * length;
            Vector3 yDir = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, model)) * length;
            Vector3 zDir = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, model)) * length;

            DrawLine(origin, origin + xDir, Color4.Red);
            DrawLine(origin, origin + yDir, Color4.Green);
            DrawLine(origin, origin + zDir, Color4.Blue);
        }

        public static void DrawBox(Vector3 center, Vector3 size, Color4 color)
        {
            var h = size * 0.5f;
            var corners = new Vector3[8];
            corners[0] = center + new Vector3(-h.X, -h.Y, -h.Z);
            corners[1] = center + new Vector3(h.X, -h.Y, -h.Z);
            corners[2] = center + new Vector3(h.X, h.Y, -h.Z);
            corners[3] = center + new Vector3(-h.X, h.Y, -h.Z);
            corners[4] = center + new Vector3(-h.X, -h.Y, h.Z);
            corners[5] = center + new Vector3(h.X, -h.Y, h.Z);
            corners[6] = center + new Vector3(h.X, h.Y, h.Z);
            corners[7] = center + new Vector3(-h.X, h.Y, h.Z);

            // bottom
            DrawLine(corners[0], corners[1], color);
            DrawLine(corners[1], corners[2], color);
            DrawLine(corners[2], corners[3], color);
            DrawLine(corners[3], corners[0], color);
            // top
            DrawLine(corners[4], corners[5], color);
            DrawLine(corners[5], corners[6], color);
            DrawLine(corners[6], corners[7], color);
            DrawLine(corners[7], corners[4], color);
            // sides
            for (int i = 0; i < 4; i++)
                DrawLine(corners[i], corners[i + 4], color);
        }

        public static void DrawCone(Vector3 apex, Vector3 direction, float angleDeg, float height, Color4 color, int segments = 24)
        {
            var axis = direction.Normalized();
            // build orthonormal basis (u,v) around axis
            Vector3 u = Vector3.Cross(axis, Vector3.UnitY).LengthSquared < 0.001f
                ? Vector3.Cross(axis, Vector3.UnitX)
                : Vector3.Cross(axis, Vector3.UnitY);
            u.Normalize();
            Vector3 v = Vector3.Cross(axis, u).Normalized();

            float radius = MathF.Tan(MathHelper.DegreesToRadians(angleDeg)) * height;
            var baseCenter = apex + axis * height;
            var pts = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                float theta = 2 * MathHelper.Pi * i / segments;
                pts[i] = baseCenter + (u * MathF.Cos(theta) + v * MathF.Sin(theta)) * radius;
            }

            // draw base circle
            for (int i = 0; i < segments; i++)
                DrawLine(pts[i], pts[(i + 1) % segments], color);
            // draw sides
            for (int i = 0; i < segments; i++)
                DrawLine(apex, pts[i], color);
        }

        public static void DrawPlane(Vector3 center, Vector3 normal, float width, float height, Color4 color)
        {
            var n = normal.Normalized();
            Vector3 u = Vector3.Cross(n, Vector3.UnitY).LengthSquared < 0.001f
                ? Vector3.Cross(n, Vector3.UnitX)
                : Vector3.Cross(n, Vector3.UnitY);
            u.Normalize();
            Vector3 v = Vector3.Cross(n, u).Normalized();

            var hw = width * 0.5f;
            var hh = height * 0.5f;
            var corners = new[]
            {
        center + u * -hw + v * -hh,
        center + u *  hw + v * -hh,
        center + u *  hw + v *  hh,
        center + u * -hw + v *  hh,
    };

            for (int i = 0; i < 4; i++)
                DrawLine(corners[i], corners[(i + 1) % 4], color);
        }

        public static void DrawDisk(Vector3 center, Vector3 normal, float radius, Color4 color, int segments = 32)
        {
            var n = normal.Normalized();
            Vector3 u = Vector3.Cross(n, Vector3.UnitY).LengthSquared < 0.001f
                ? Vector3.Cross(n, Vector3.UnitX)
                : Vector3.Cross(n, Vector3.UnitY);
            u.Normalize();
            Vector3 v = Vector3.Cross(n, u).Normalized();

            Vector3 prev = center + u * radius;
            for (int i = 1; i <= segments; i++)
            {
                float theta = 2 * MathHelper.Pi * i / segments;
                Vector3 next = center + (u * MathF.Cos(theta) + v * MathF.Sin(theta)) * radius;
                DrawLine(prev, next, color);
                prev = next;
            }
        }


        public static void Render()
        {
         
            if (!Enabled) return;
            if (_points.Count == 0 && _lines.Count == 0 && _triangles.Count == 0) return;

            Camera cam = Camera.Main;
            if (cam != null)
            {
                ProjectionMatrix = cam.GetProjectionMatrix();
                ViewMatrix = cam.GetViewMatrix();
            }
            else return;
            
            _shader.Use();
            _shader.SetMatrix4("model", Matrix4.Identity * Matrix4.CreateTranslation(-RenderSpace.Origin));
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

        public static void Clear()
        {
            _points.Clear();
            _lines.Clear();
            _triangles.Clear();
        }
    }
}

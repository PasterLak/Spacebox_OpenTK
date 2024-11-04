using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Diagnostics;

namespace Spacebox.Common
{
    public static class Debug
    {
        private static Shader _shader;

        private static int _vaoPoints;
        private static int _vboPoints;

        private static int _vaoLines;
        private static int _vboLines;

        private static int _vaoTriangles;
        private static int _vboTriangles;

        private static List<float> _points = new List<float>();
        private static List<float> _lines = new List<float>();
        private static List<float> _triangles = new List<float>();

        public static Matrix4 ProjectionMatrix { get; set; }
        public static Matrix4 ViewMatrix { get; set; }

        public static bool ShowDebug = false;
        public static bool ShowPlayerCollision = false;

        private static List<Collision> _collisions = new List<Collision>();

        static Debug()
        {
            Initialize();
        }

        private static void Initialize()
        {
            
            _shader = ShaderManager.AddPermanentShader("Shaders/debug");

            // Initialize Points VAO and VBO
            _vaoPoints = GL.GenVertexArray();
            _vboPoints = GL.GenBuffer();
            GL.BindVertexArray(_vaoPoints);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboPoints);

            int stridePoints = sizeof(float) * 8; // 3 position + 4 color + 1 size

            // Position attribute
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stridePoints, 0);

            // Color attribute
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, stridePoints, sizeof(float) * 3);

            // Point size attribute
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stridePoints, sizeof(float) * 7);

            GL.BindVertexArray(0);

            // Initialize Lines VAO and VBO
            _vaoLines = GL.GenVertexArray();
            _vboLines = GL.GenBuffer();
            GL.BindVertexArray(_vaoLines);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboLines);

            int strideLines = sizeof(float) * 7; // 3 position + 4 color

            // Position attribute
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, strideLines, 0);

            // Color attribute
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, strideLines, sizeof(float) * 3);

            GL.BindVertexArray(0);

            // Initialize Triangles VAO and VBO
            _vaoTriangles = GL.GenVertexArray();
            _vboTriangles = GL.GenBuffer();
            GL.BindVertexArray(_vaoTriangles);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboTriangles);

            int strideTriangles = sizeof(float) * 7; // 3 position + 4 color

            // Position attribute
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, strideTriangles, 0);

            // Color attribute
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, strideTriangles, sizeof(float) * 3);

            GL.BindVertexArray(0);
        }

        public static void DrawTransform(Node3D transform)
        {
            if (transform == null) return;

            DrawLine(transform.Position, transform.Position + new Vector3(0, 0, 1), new Color4(0, 0, 1, 1));
        }

        public static void AddCollisionToDraw(Collision collision)
        {
            _collisions.Add(collision);
        }
        public static void RemoveCollisionToDraw(Collision collision)
        {
            if(_collisions.Contains(collision))
            _collisions.Remove(collision);
        }

        public static void DrawPoint(Vector3 position, float size, Color4 color)
        {
            _points.Add(position.X);
            _points.Add(position.Y);
            _points.Add(position.Z);
            _points.Add(color.R);
            _points.Add(color.G);
            _points.Add(color.B);
            _points.Add(color.A);
            _points.Add(size); // Add point size
        }

        public static void DrawLine(Vector3 start, Vector3 end, Color4 color)
        {
            // Start point
            _lines.Add(start.X);
            _lines.Add(start.Y);
            _lines.Add(start.Z);
            _lines.Add(color.R);
            _lines.Add(color.G);
            _lines.Add(color.B);
            _lines.Add(color.A);

            // End point
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
          
            if(ray == null) return;

            Vector3 endPoint = ray.Origin + ray.Direction * ray.Length;
   
            DrawLine(ray.Origin, endPoint, color);
        }

     

        private static void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color4 color)
        {
            AddVertexToTriangles(v1, color);
            AddVertexToTriangles(v2, color);
            AddVertexToTriangles(v3, color);
        }

        private static void AddVertexToTriangles(Vector3 position, Color4 color)
        {
            _triangles.Add(position.X);
            _triangles.Add(position.Y);
            _triangles.Add(position.Z);
            _triangles.Add(color.R);
            _triangles.Add(color.G);
            _triangles.Add(color.B);
            _triangles.Add(color.A);
        }

        /// <summary>
        /// Рисует BoundingBox как набор линий.
        /// </summary>
        /// <param name="box">BoundingBox для отрисовки.</param>
        /// <param name="color">Цвет линий.</param>
        public static void DrawBoundingBox(BoundingBox box, Color4 color)
        {
            if (box == null) return;

            Vector3 min = box.Min;
            Vector3 max = box.Max;

            // Определение 8 углов BoundingBox
            Vector3[] corners = new Vector3[8];
            corners[0] = new Vector3(min.X, min.Y, min.Z);
            corners[1] = new Vector3(max.X, min.Y, min.Z);
            corners[2] = new Vector3(max.X, max.Y, min.Z);
            corners[3] = new Vector3(min.X, max.Y, min.Z);
            corners[4] = new Vector3(min.X, min.Y, max.Z);
            corners[5] = new Vector3(max.X, min.Y, max.Z);
            corners[6] = new Vector3(max.X, max.Y, max.Z);
            corners[7] = new Vector3(min.X, max.Y, max.Z);

            // Нижняя грань
            DrawLine(corners[0], corners[1], color);
            DrawLine(corners[1], corners[2], color);
            DrawLine(corners[2], corners[3], color);
            DrawLine(corners[3], corners[0], color);

            // Верхняя грань
            DrawLine(corners[4], corners[5], color);
            DrawLine(corners[5], corners[6], color);
            DrawLine(corners[6], corners[7], color);
            DrawLine(corners[7], corners[4], color);

            // Вертикальные линии
            DrawLine(corners[0], corners[4], color);
            DrawLine(corners[1], corners[5], color);
            DrawLine(corners[2], corners[6], color);
            DrawLine(corners[3], corners[7], color);
        }

        /// <summary>
        /// Рисует BoundingSphere как набор линий (wireframe).
        /// </summary>
        /// <param name="sphere">BoundingSphere для отрисовки.</param>
        /// <param name="color">Цвет линий.</param>
        public static void DrawBoundingSphere(BoundingSphere sphere, Color4 color)
        {
            if (sphere == null) return;

            int slices = 16; // Количество сегментов по долготе
            int stacks = 16; // Количество сегментов по широте
            float radius = sphere.Radius;
            Vector3 center = sphere.Center;

            // Рисуем линии по широте
            for (int i = 0; i <= stacks; i++)
            {
                float theta = MathHelper.Pi * i / stacks;
                float sinTheta = MathF.Sin(theta);
                float cosTheta = MathF.Cos(theta);

                Vector3 prevPoint = new Vector3(
                    radius * sinTheta * MathF.Cos(0),
                    radius * cosTheta,
                    radius * sinTheta * MathF.Sin(0)) + center;

                for (int j = 1; j <= slices; j++)
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

            // Рисуем линии по долготе
            for (int j = 0; j < slices; j++)
            {
                float phi = 2 * MathHelper.Pi * j / slices;
                Vector3 prevPoint = new Vector3(
                    radius * MathF.Sin(0) * MathF.Cos(phi),
                    radius * MathF.Cos(0),
                    radius * MathF.Sin(0) * MathF.Sin(phi)) + center;

                for (int i = 1; i <= stacks; i++)
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


        public static void Render() // in Window loop
        {
            if (!ShowDebug) return;

            if (_points.Count == 0 && _lines.Count == 0 && _triangles.Count == 0)
                return;

            foreach(var col in _collisions) {
                col.DrawDebug();
            }

            _shader.Use();

            _shader.SetMatrix4("model", Matrix4.Identity);
            _shader.SetMatrix4("view", ViewMatrix);
            _shader.SetMatrix4("projection", ProjectionMatrix);

            GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);

            // Render Points
            if (_points.Count > 0)
            {
                GL.BindVertexArray(_vaoPoints);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vboPoints);
                GL.BufferData(BufferTarget.ArrayBuffer, _points.Count * sizeof(float), _points.ToArray(), BufferUsageHint.DynamicDraw);

                GL.DrawArrays(PrimitiveType.Points, 0, _points.Count / 8);
                _points.Clear();
            }

            // Render Lines
            if (_lines.Count > 0)
            {
                GL.BindVertexArray(_vaoLines);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vboLines);
                GL.BufferData(BufferTarget.ArrayBuffer, _lines.Count * sizeof(float), _lines.ToArray(), BufferUsageHint.DynamicDraw);

                // Set line width globally
                GL.LineWidth(LineWidth);

                GL.DrawArrays(PrimitiveType.Lines, 0, _lines.Count / 7);
                _lines.Clear();
            }

            // Render Triangles
            if (_triangles.Count > 0)
            {
                GL.BindVertexArray(_vaoTriangles);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vboTriangles);
                GL.BufferData(BufferTarget.ArrayBuffer, _triangles.Count * sizeof(float), _triangles.ToArray(), BufferUsageHint.DynamicDraw);

                GL.DrawArrays(PrimitiveType.Triangles, 0, _triangles.Count / 7);
                _triangles.Clear();
            }

            GL.BindVertexArray(0);

            GL.Disable(EnableCap.CullFace);
        }

        private static float LineWidth = 1.0f;

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
            Console.Clear();
        }
    }
}

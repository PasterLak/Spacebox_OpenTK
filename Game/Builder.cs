using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public class Builder
    {
        private Shader _shader;
        private int _vao;
        private int _vbo;
        private int _ebo;
        private List<float> _vertices = new List<float>();
        private List<uint> _indices = new List<uint>();
        private List<Cube> _cubes = new List<Cube>();

        private static readonly int[] CubeLines = {
            0, 1, 1, 2, 2, 3, 3, 0,
            4, 5, 5, 6, 6, 7, 7, 4,
            0, 4, 1, 5, 2, 6, 3, 7
        };

        private static readonly Vector3[] CubeVertices = {
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f)
        };

        private static readonly uint[] CubeIndices = {
            0, 1, 2,
            2, 3, 0,
            4, 5, 6,
            6, 7, 4,
            4, 0, 3,
            3, 7, 4,
            1, 5, 6,
            6, 2, 1,
            3, 2, 6,
            6, 7, 3,
            4, 5, 1,
            1, 0, 4
        };

        public Builder()
        {
            Initialize();
        }

        private void Initialize()
        {
            _shader = new Shader("Shaders/vertex_shader");
            _shader.Use();

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);

            int stride = sizeof(float) * 9;

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, sizeof(float) * 3);

            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, stride, sizeof(float) * 5);

            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, stride, sizeof(float) * 8);

            GL.BindVertexArray(0);
        }

        public void AddCube(Vector3 position, CubeType type, Color4 color, Vector2 textureUV = default)
        {
            Cube cube = new Cube(position, type, color, textureUV);
            _cubes.Add(cube);
        }

        public void RemoveCube(Cube cube)
        {
            _cubes.Remove(cube);
        }

        public void Render(Camera camera, int textureAtlasID)
        {
            _shader.Use();

            _shader.SetMatrix4("model", Matrix4.Identity);
            _shader.SetMatrix4("view", camera.GetViewMatrix());
            _shader.SetMatrix4("projection", camera.GetProjectionMatrix());
            _shader.SetVector3("cameraPosition", camera.Position);
            _shader.SetFloat("fogDensity", 0.05f);
            _shader.SetVector3("fogColor", new Vector3(0, 0, 0));
            _shader.SetVector3("ambientColor", Spacebox.Common.Lighting.AmbientColor);

            _vertices.Clear();
            _indices.Clear();

            uint currentIndex = 0;

            foreach (var cube in _cubes)
            {
                if (cube.Type == CubeType.Wireframe)
                {
                    Vector3 halfSize = new Vector3(0.5f);

                    Vector3 min = cube.Position - halfSize;
                    Vector3 max = cube.Position + halfSize;

                    Vector3[] corners = new Vector3[8];
                    corners[0] = new Vector3(min.X, min.Y, min.Z);
                    corners[1] = new Vector3(max.X, min.Y, min.Z);
                    corners[2] = new Vector3(max.X, max.Y, min.Z);
                    corners[3] = new Vector3(min.X, max.Y, min.Z);
                    corners[4] = new Vector3(min.X, min.Y, max.Z);
                    corners[5] = new Vector3(max.X, min.Y, max.Z);
                    corners[6] = new Vector3(max.X, max.Y, max.Z);
                    corners[7] = new Vector3(min.X, max.Y, max.Z);

                    for (int i = 0; i < CubeLines.Length; i += 2)
                    {
                        int start = CubeLines[i];
                        int end = CubeLines[i + 1];

                        Vector3 startPos = corners[start];
                        Vector3 endPos = corners[end];

                        _vertices.Add(startPos.X);
                        _vertices.Add(startPos.Y);
                        _vertices.Add(startPos.Z);
                        _vertices.Add(0.0f);
                        _vertices.Add(0.0f);
                        _vertices.Add(cube.Color.R);
                        _vertices.Add(cube.Color.G);
                        _vertices.Add(cube.Color.B);
                        _vertices.Add(0.0f);

                        _vertices.Add(endPos.X);
                        _vertices.Add(endPos.Y);
                        _vertices.Add(endPos.Z);
                        _vertices.Add(0.0f);
                        _vertices.Add(0.0f);
                        _vertices.Add(cube.Color.R);
                        _vertices.Add(cube.Color.G);
                        _vertices.Add(cube.Color.B);
                        _vertices.Add(0.0f);

                        _indices.Add(currentIndex);
                        _indices.Add(currentIndex + 1);
                        currentIndex += 2;
                    }
                }
                else if (cube.Type == CubeType.Textured)
                {
                    for (int i = 0; i < CubeVertices.Length; i++)
                    {
                        Vector3 vertex = CubeVertices[i];
                        Vector2 uv = GetUVForFace((Face)(i / 4));

                        _vertices.Add(vertex.X + cube.Position.X);
                        _vertices.Add(vertex.Y + cube.Position.Y);
                        _vertices.Add(vertex.Z + cube.Position.Z);
                        _vertices.Add(uv.X);
                        _vertices.Add(uv.Y);
                        _vertices.Add(cube.Color.R);
                        _vertices.Add(cube.Color.G);
                        _vertices.Add(cube.Color.B);
                        _vertices.Add(1.0f);

                        _indices.Add(currentIndex);
                        currentIndex++;
                    }
                }
            }

            if (_vertices.Count > 0 && _indices.Count > 0)
            {
                GL.BindVertexArray(_vao);

                GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
                GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Count * sizeof(float), _vertices.ToArray(), BufferUsageHint.DynamicDraw);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
                GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Count * sizeof(uint), _indices.ToArray(), BufferUsageHint.DynamicDraw);

                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, textureAtlasID);
                _shader.SetInt("textureAtlas", 1);

                GL.DrawElements(PrimitiveType.Triangles, _indices.Count, DrawElementsType.UnsignedInt, 0);

                GL.BindVertexArray(0);
            }
        }

        private Vector2 GetUVForFace(Face face)
        {
            int atlasColumns = 4;
            int atlasRows = 4;
            float uvSize = 1.0f / atlasColumns;

            int faceIndex = (int)face;

            int column = faceIndex % atlasColumns;
            int row = faceIndex / atlasColumns;

            float u = column * uvSize;
            float v = row * uvSize;

            return new Vector2(u, v);
        }
    }
}

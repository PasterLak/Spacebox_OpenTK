﻿
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace Engine
{
    public class CubeRenderer : Node3D, IDisposable
    {
        public bool Enabled = true;
        public MaterialBase Material;
        private MeshBuffer _buffer;

        private Vector3 _position;
        private Color4 _color = Color4.White;
        public Color4 Color
        {
            get => _color;
            set { _color = value; }
        }

        private readonly float[] _vertices = new float[]
    {
    // Front face
    -0.5f, -0.5f,  0.5f,   0f, 0f, 1f,   0f, 0f,
     0.5f, -0.5f,  0.5f,   0f, 0f, 1f,   1f, 0f,
     0.5f,  0.5f,  0.5f,   0f, 0f, 1f,   1f, 1f,
    -0.5f,  0.5f,  0.5f,   0f, 0f, 1f,   0f, 1f,

    // Back face
    -0.5f, -0.5f, -0.5f,   0f, 0f, -1f,  0f, 0f,
     0.5f, -0.5f, -0.5f,   0f, 0f, -1f,  1f, 0f,
     0.5f,  0.5f, -0.5f,   0f, 0f, -1f,  1f, 1f,
    -0.5f,  0.5f, -0.5f,   0f, 0f, -1f,  0f, 1f,

    // Left face
    -0.5f, -0.5f, -0.5f,  -1f, 0f, 0f,   0f, 0f,
    -0.5f, -0.5f,  0.5f,  -1f, 0f, 0f,   1f, 0f,
    -0.5f,  0.5f,  0.5f,  -1f, 0f, 0f,   1f, 1f,
    -0.5f,  0.5f, -0.5f,  -1f, 0f, 0f,   0f, 1f,

    // Right face
     0.5f, -0.5f, -0.5f,   1f, 0f, 0f,   0f, 0f,
     0.5f, -0.5f,  0.5f,   1f, 0f, 0f,   1f, 0f,
     0.5f,  0.5f,  0.5f,   1f, 0f, 0f,   1f, 1f,
     0.5f,  0.5f, -0.5f,   1f, 0f, 0f,   0f, 1f,

    // Top face
    -0.5f,  0.5f, -0.5f,   0f, 1f, 0f,   0f, 0f,
    -0.5f,  0.5f,  0.5f,   0f, 1f, 0f,   1f, 0f,
     0.5f,  0.5f,  0.5f,   0f, 1f, 0f,   1f, 1f,
     0.5f,  0.5f, -0.5f,   0f, 1f, 0f,   0f, 1f,

    // Bottom face
    -0.5f, -0.5f, -0.5f,   0f, -1f, 0f,  0f, 0f,
    -0.5f, -0.5f,  0.5f,   0f, -1f, 0f,  1f, 0f,
     0.5f, -0.5f,  0.5f,   0f, -1f, 0f,  1f, 1f,
     0.5f, -0.5f, -0.5f,   0f, -1f, 0f,  0f, 1f,
};


        private readonly uint[] _indices = new uint[]
{
    // Front 
    0, 1, 2,
    2, 3, 0,

    // Back 
    6, 5, 4,
    4, 7, 6,

    // Left 
    8, 9, 10,
    10, 11, 8,

    // Right 
    14, 13, 12,
    12, 15, 14,

    // Top 
    16, 17, 18,
    18, 19, 16,

    // Bottom 
    22, 21, 20,
    20, 23, 22
};


        public CubeRenderer(Vector3 position, MeshBuffer buffer)
        {
            _position = position;
            Position = position;
            Material = new ColorMaterial();

            _buffer = buffer;
            _buffer.BindBuffer(ref _vertices, ref _indices);
            _buffer.SetAttributes();
        }

        public CubeRenderer(Vector3 position)
        {
            _position = position;
            Position = position;
            Material = new ColorMaterial();
            var attrs = new BufferAttribute[]
            {
                new BufferAttribute { Name = "aPos",    Size = 3 },
                new BufferAttribute { Name = "aNormal", Size = 3 },
                new BufferAttribute { Name = "aTexCoords", Size = 2 }

            };
            _buffer = new MeshBuffer(attrs);
            _buffer.BindBuffer(ref _vertices, ref _indices);
            _buffer.SetAttributes();
        }

        public void Render()
        {
            if (!Enabled) return;
            if (Camera.Main == null) return;
            var cam = Camera.Main;

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.DepthTest);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);


            var matModel = GetModelMatrix();


            Material.Color = _color;
            Material.SetUniforms(matModel);
            Material.Use();

            GL.BindVertexArray(_buffer.VAO);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            GL.Disable(EnableCap.CullFace);
        }

        public void Dispose()
        {
            _buffer?.Dispose();
            //_shader?.Dispose();
        }
    }
}

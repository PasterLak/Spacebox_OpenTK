using System;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;


namespace Spacebox.Common;

public class Axes : Node3D
{
    private int vertexArray;
    private int vertexBuffer;

    private Shader _shader;

    private float[] vertices;

    public float Length { get; set; }

    public Axes(Vector3 position, float length)
    {
        Position = position;
        Length = length;
        Rotation = Vector3.Zero;

        _shader = ShaderManager.GetShader("Shaders/axes");

        UpdateVertices();
        SetupBuffers();
    }

    public Axes(Vector3 position, float length, Vector3 rotation)
    {
        Position = position;
        Length = length;
        Rotation = rotation;

        _shader = ShaderManager.GetShader("Shaders/axes");

        UpdateVertices();
        SetupBuffers();
    }

    private void SetupBuffers()
    {
       
        vertexArray = GL.GenVertexArray();
        GL.BindVertexArray(vertexArray);

        
        vertexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);

       
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

       
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    private void UpdateVertices()
    {
      
        vertices = new float[]
        {
            // X 
            0.0f, 0.0f, 0.0f,  1.0f, 0.0f, 0.0f,
            Length, 0.0f, 0.0f,  1.0f, 0.0f, 0.0f,

            // Y 
            0.0f, 0.0f, 0.0f,  0.0f, 1.0f, 0.0f,
            0.0f, Length, 0.0f,  0.0f, 1.0f, 0.0f,

            // Z 
            0.0f, 0.0f, 0.0f,  0.0f, 0.0f, 1.0f,
            0.0f, 0.0f, Length,  0.0f, 0.0f, 1.0f,
        };

       
        if (vertexBuffer != 0)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
    }

    public void Render(Camera camera)
    {
        Render(camera.GetViewMatrix(), camera.GetProjectionMatrix());
    }
    public void Render(Matrix4 view, Matrix4 projection)
    {
       
        _shader.Use();

        Matrix4 model = GetModelMatrix();

      
        int modelLocation = GL.GetUniformLocation(_shader.Handle, "model");
        int viewLocation = GL.GetUniformLocation(_shader.Handle, "view");
        int projectionLocation = GL.GetUniformLocation(_shader.Handle, "projection");

       
        GL.UniformMatrix4(modelLocation, false, ref model);
        GL.UniformMatrix4(viewLocation, false, ref view);
        GL.UniformMatrix4(projectionLocation, false, ref projection);

        GL.BindVertexArray(vertexArray);
        GL.DrawArrays(PrimitiveType.Lines, 0, 6);
        GL.BindVertexArray(0);
    }


    public void SetPosition(Vector3 newPosition)
    {
        Position = newPosition;
        UpdateVertices();
    }

    public void SetLength(float newLength)
    {
        Length = newLength;
        UpdateVertices();
    }

    public void SetRotation(Vector3 newRotation)
    {
        Rotation = newRotation;
        UpdateVertices();
    }
}
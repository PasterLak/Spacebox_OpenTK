using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;


namespace Spacebox.Common;

public class Axes
{
    private int vertexArray;
    private int vertexBuffer;

    private Shader _shader;

    // Positions and colors for vertices will be calculated dynamically
    private float[] vertices;

    public Vector3 Position { get; set; }
    public float Length { get; set; }
    public Vector3 Rotation { get; set; } // Rotation in degrees

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
        // Create Vertex Array Object
        vertexArray = GL.GenVertexArray();
        GL.BindVertexArray(vertexArray);

        // Create Vertex Buffer Object
        vertexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);

        // Set up vertex attribute pointers
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        // Unbind buffers for safety
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    private void UpdateVertices()
    {
        // Update the vertices based on position and length
        vertices = new float[]
        {
            // X axis (red)
            0.0f, 0.0f, 0.0f,  1.0f, 0.0f, 0.0f,
            Length, 0.0f, 0.0f,  1.0f, 0.0f, 0.0f,

            // Y axis (green)
            0.0f, 0.0f, 0.0f,  0.0f, 1.0f, 0.0f,
            0.0f, Length, 0.0f,  0.0f, 1.0f, 0.0f,

            // Z axis (blue)
            0.0f, 0.0f, 0.0f,  0.0f, 0.0f, 1.0f,
            0.0f, 0.0f, Length,  0.0f, 0.0f, 1.0f,
        };

        // Update buffer data if it was already created
        if (vertexBuffer != 0)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
    }

    public void Render(Matrix4 view, Matrix4 projection)
    {
       
        _shader.Use();

        // Calculate the model matrix based on position, rotation, and length
        Matrix4 model = Matrix4.Identity *
                        Matrix4.CreateScale(Length) *
                        Matrix4.CreateRotationX(MathHelper.DegreesToRadians(Rotation.X)) *
                        Matrix4.CreateRotationY(MathHelper.DegreesToRadians(Rotation.Y)) *
                        Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation.Z)) *
                        Matrix4.CreateTranslation(Position);

        // Set uniforms for the shader
        int modelLocation = GL.GetUniformLocation(_shader.Handle, "model");
        int viewLocation = GL.GetUniformLocation(_shader.Handle, "view");
        int projectionLocation = GL.GetUniformLocation(_shader.Handle, "projection");

        // Pass the matrices to the shader
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
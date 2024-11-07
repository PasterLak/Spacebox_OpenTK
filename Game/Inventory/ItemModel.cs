using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public class ItemModel : Node3D, IDisposable
    {
        public Mesh Mesh { get; private set; }
        public Texture2D Texture { get; private set; }

        public ItemModel(Mesh mesh, Texture2D texture)
        {
            Mesh = mesh;
            Texture = texture;

            Texture.UpdateTexture(true);

        }

        /*
         * 
         * Matrix4 model = player.GetModelMatrix();

            model =  Matrix4.CreateFromQuaternion(player.GetRotation())* Matrix4.CreateTranslation(player.Front);
        */

        Vector3 offset = new Vector3(0.19f, -0.35f, 0.25f);
        float additionalRotationAngle = MathHelper.DegreesToRadians(90.0f);
        public void Draw(Shader shader)
        {

            if(Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.V))
            {
                offset.X += 0.05f;
                Debug.Log(offset.ToString());
            }
            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.B))
            {
                offset.X -= 0.05f;
                Debug.Log(offset.ToString());
            }
            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.N))
            {
                offset.Z += 0.05f;
                Debug.Log(offset.ToString());
            }
            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.M))
            {
                offset.Z -= 0.05f;
                Debug.Log(offset.ToString());
            }
            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.J))
            {
                offset.Y += 0.05f;
                Debug.Log(offset.ToString());
            }
            if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.K))
            {
                offset.Y -= 0.05f;
                Debug.Log(offset.ToString());
            }
            Astronaut player = Camera.Main as Astronaut;
            GL.Disable(EnableCap.DepthTest);
            Matrix4 model = player.GetModelMatrix();

            

            Matrix4 view = player.GetViewMatrix();

            // Извлеките только вращение из матрицы вида
            Matrix4 rotation = new Matrix4(
                view.M11, view.M12, view.M13, 0,
                view.M21, view.M22, view.M23, 0,
                view.M31, view.M32, view.M33, 0,
                0, 0, 0, 1
            );
            rotation.Transpose();

            
            Matrix4 additionalRotation = Matrix4.CreateRotationY(additionalRotationAngle);


            model =
                 Matrix4.CreateTranslation(offset) *
                 additionalRotation *
                rotation *
                Matrix4.CreateTranslation(player.Position);

            shader.Use();
            shader.SetMatrix4("model", model);
            shader.SetMatrix4("view", Camera.Main.GetViewMatrix());
            shader.SetMatrix4("projection", Camera.Main.GetProjectionMatrix());
            shader.SetVector4("color", new Vector4(1,1f, 1, 1));

            Texture.Use(TextureUnit.Texture0);
            shader.SetInt("texture0", 0);


            Mesh.Draw(shader);
            GL.Enable(EnableCap.DepthTest);

        }

        public void Dispose()
        {
            Mesh?.Dispose();
            Texture?.Dispose();
        }
    }
}

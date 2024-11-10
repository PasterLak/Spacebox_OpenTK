using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.Game
{
    public class ItemModel : Node3D, IDisposable
    {
        public Mesh Mesh { get; private set; }
        public Texture2D Texture { get; private set; }

        private Camera itemCamera;
        public ItemModel(Mesh mesh, Texture2D texture)
        {
            Mesh = mesh;
            Texture = texture;

            Texture.UpdateTexture(true);

            itemCamera = new Camera360(Vector3.Zero, false);

        }

        /*
         * 
         * Matrix4 model = player.GetModelMatrix();

            model =  Matrix4.CreateFromQuaternion(player.GetRotation())* Matrix4.CreateTranslation(player.Front);
        */

        //Vector3 offset = new Vector3(0.19f, -0.35f, 0.25f);   // model size 0.01

        Vector3 offset = new Vector3(0.19f, -0.6f, 0.35f); // 0.02
        float additionalRotationAngle = MathHelper.DegreesToRadians(90.0f);

        public bool debug = false;
        Matrix4 model;

        private Shader shader;
        public void SetColor(Vector3 color)
        {
            if (shader == null) return;

            shader.SetVector3("color", color);
        }
        public void Draw(Shader shader)
        {
            if(this.shader == null)
            {
                this.shader = shader;
            }
            
            if (!debug)
            {
                if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.V))
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
                 model = player.GetModelMatrix();



                Matrix4 view = player.GetViewMatrix();

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
            }
            if(debug)
            {
                model = Matrix4.Identity;
            }

            shader.Use();
            shader.SetMatrix4("model", model);
            shader.SetMatrix4("view", Camera.Main.GetViewMatrix());
            shader.SetMatrix4("projection", Camera.Main.GetProjectionMatrix());
            

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

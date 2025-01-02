using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.Game.Player;

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
            Mesh.EnableDepthTest = true;
            Mesh.EnableBlend = false;
            Mesh.EnableAlpha = false;
            Texture = texture;

            Texture.UpdateTexture(true);

            itemCamera = new Camera360(Vector3.Zero, false);

            //shader.SetVector3("lightColor", new Vector3(1, 1, 1));
           // shader.SetVector3("objectColor", new Vector3(1, 1, 1));
           offset = new Vector3(0.06f, -0.12f, 0.07f);
        }

        /*
         * 
         * Matrix4 model = player.GetModelMatrix();

            model =  Matrix4.CreateFromQuaternion(player.GetRotation())* Matrix4.CreateTranslation(player.Front);
        */

        //Vector3 offset = new Vector3(0.19f, -0.35f, 0.25f);   // model size 0.01

        Vector3 offset = new Vector3(0.29f, -0.6f, 0.35f); // 0.02 
        float additionalRotationAngle = MathHelper.DegreesToRadians(90.0f);

        public bool debug = true;
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

            if (debug)
            {
                const float step = 0.01f;
                if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.V))
                {
                    offset.X += step;
                    Debug.Log(offset.ToString());
                }
                if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.B))
                {
                    offset.X -= step;
                    Debug.Log(offset.ToString());
                }
                if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.N))
                {
                    offset.Z += step;
                    Debug.Log(offset.ToString());
                }
                if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.M))
                {
                    offset.Z -= step;
                    Debug.Log(offset.ToString());
                }
                if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.J))
                {
                    offset.Y += step;
                    Debug.Log(offset.ToString());
                }
                if (Input.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.K))
                {
                    offset.Y -= step;
                    Debug.Log(offset.ToString());
                }

            }

                model = itemCamera.GetModelMatrix();



                Matrix4 view = itemCamera.GetViewMatrix();

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
                    Matrix4.CreateTranslation(itemCamera.Position);
            

            shader.Use();
            shader.SetMatrix4("model", model);
            shader.SetMatrix4("view", itemCamera.GetViewMatrix());
            shader.SetMatrix4("projection", itemCamera.GetProjectionMatrix());

            shader.SetVector3("lightColor", new Vector3(1, 1, 1));  // can be opt
            shader.SetVector3("objectColor", new Vector3(1, 1, 1));

            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(false);

            Texture.Use(TextureUnit.Texture0);
            shader.SetInt("texture0", 0);


            Mesh.Draw(shader);

            GL.DepthMask(true);
            GL.Disable(EnableCap.DepthTest);

        }

        public void Dispose()
        {
            Mesh?.Dispose();
            Texture?.Dispose();
        }
    }
}

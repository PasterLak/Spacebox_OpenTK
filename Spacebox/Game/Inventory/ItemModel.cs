using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Spacebox.Engine;
using Spacebox.Engine;
using Spacebox.Game.Player;

namespace Spacebox.Game
{
    public class ItemModel : Node3D, IDisposable
    {
        public Mesh Mesh { get; private set; }
        public Texture2D Texture { get; private set; }

        private Vector3 offset = new Vector3(0.06f, -0.12f, 0.07f);
        private float additionalRotationAngle = MathHelper.DegreesToRadians(90.0f);

        public bool debug = false;
        private Matrix4 model;


        private Shader shader;
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
            itemCamera.FOV = 80;
        }

        public void SetColor(Vector3 color)
        {
            if (shader == null) return;

            //shader.SetVector3("color", color);
        }
        public virtual void Draw(Shader shader)
        {

            if (this.shader == null)
            {
                this.shader = shader;
               // shader.SetVector3("lightColor", new Vector3(1,1, 1));
                //shader.SetVector3("objectColor", new Vector3(1, 1, 1));
            }

            if (debug) PlaceModelDebug();

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
            // additionalRotation = additionalRotation * rotation;
            model =
                 Matrix4.CreateTranslation(offset) *
                 Matrix4.CreateTranslation(Position) *
                 additionalRotation *
                 //rotation *
                 //Matrix4.CreateFromQuaternion(Quaternion.FromEulerAngles(Rotation)) *
                 Matrix4.CreateTranslation(itemCamera.Position);


            shader.Use();
            shader.SetMatrix4("model", model);
            shader.SetMatrix4("view", itemCamera.GetViewMatrix());
            shader.SetMatrix4("projection", itemCamera.GetProjectionMatrix());

            GL.Enable(EnableCap.DepthTest);
            //GL.DepthMask(false);

            Texture.Use(TextureUnit.Texture0);
            shader.SetInt("texture0", 0);

            Mesh.Draw(shader);

            GL.DepthMask(true);
            GL.Disable(EnableCap.DepthTest);

        }

        private void PlaceModelDebug()
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

        public void Dispose()
        {
            Mesh?.Dispose();
            Texture?.Dispose();
        }
    }
}

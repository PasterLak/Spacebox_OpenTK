using OpenTK.Mathematics;
using Engine;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;


namespace Spacebox.Game
{
    public class ItemModel : Node3D, IDisposable
    {
        public Mesh Mesh { get; private set; }
     
        public bool EnableRender = true;
        public Vector3 offset = new Vector3(0.06f, -0.12f, 0.07f);
        private float additionalRotationAngle = MathHelper.DegreesToRadians(90.0f);

        public bool debug = false;
        private Matrix4 model;

        public ItemMaterial Material { get; private set; }
        private Camera itemCamera;
        public bool UseMainCamera { get; set; } = false;

        public ItemModel(Mesh mesh, Texture2D texture)
        {
            Mesh = mesh;
         
            texture.FilterMode = FilterMode.Nearest;

            Material = new ItemMaterial(texture);

            itemCamera = new Camera360Base(Vector3.Zero, false);
            itemCamera.AspectRatio = Window.Instance.GetAspectRatio();
            itemCamera.FOV = 80;
            itemCamera.DepthNear = 0.01f;
            itemCamera.DepthFar = 100f;
            

            Window.OnResized += Resize;
        }
        ~ItemModel()
        {
            Window.OnResized -= Resize;
        }
        public void Resize(Vector2 size)
        {
            if (itemCamera == null) return;
            if (Camera.Main == null) return;
            itemCamera.AspectRatio = size.X / size.Y;
        }
        public void SetColor(Vector3 color)
        {
           
        }
        public override void Render()
        {
            
            base.Render();
            if (!EnableRender)
            {
                return;
            }

            if (debug)
                PlaceModelDebug();

           
            if (UseMainCamera && Camera.Main != null)
            {
               
                model =
                     Matrix4.CreateTranslation(offset) *
                     Matrix4.CreateTranslation(RenderSpace.ToRender(Position)) *
                     Matrix4.CreateRotationY(additionalRotationAngle) *
                      Matrix4.CreateTranslation(Camera.Main.Position);
                var cam = Camera.Main as Astronaut;

                Material.Apply(model);
               
            }
            else
            {
                model = itemCamera.GetRenderModelMatrix();

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
                     Matrix4.CreateTranslation(Position) *
                     additionalRotation *
                     Matrix4.CreateTranslation(itemCamera.Position);

                Material.Action = () =>
                {
                    Material.Shader.Use();
                    Material.Shader.SetMatrix4("model", model);
                    Material.Shader.SetMatrix4("view", itemCamera.GetViewMatrix());
                    Material.Shader.SetMatrix4("projection", itemCamera.GetProjectionMatrix());
                };
            }

            Material.Apply(model);

            Mesh.Render();

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
            Material = null;
            itemCamera = null;
            Mesh?.Dispose();
            Destroy();

        }
    }
}

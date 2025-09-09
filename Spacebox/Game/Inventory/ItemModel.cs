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
       
        public float DrawSpeed { get; set; } = 3.0f;
        public Vector3 StartPosition { get; set; } = new Vector3(0.5f, -0.8f, -0.2f);
        public Vector3 EndPosition { get; set; } = new Vector3(0.06f, -0.12f, 0.07f);

        private bool isAnimating = false;
        private float animationTime = 0f;
        private Vector3 animatedOffset;

        private float additionalRotationAngle = MathHelper.DegreesToRadians(90.0f);

        public bool debug = false;
        private Matrix4 model;

        public ItemMaterial Material { get; private set; }
        private Camera itemCamera;
        public bool UseMainCamera { get; set; } = false;

        public float SwayMultiplier { get; set; } = 0.5f;
        public float DampingFactor { get; set; } = 8.0f;
        public float MaxSwayAngle { get; set; } = 3.0f;

        public bool EnableSway = true;

        private Vector3 currentSwayRotation = Vector3.Zero;

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
            animatedOffset = EndPosition;



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

        public void PlayDrawAnimation()
        {
            isAnimating = true;
            animationTime = 0f;
            animatedOffset = StartPosition;
        }

        public void ResetToEnd()
        {
            isAnimating = false;
            animationTime = 1f;
            animatedOffset = EndPosition;
        }

        private float EaseOutCubic(float t)
        {
            return 1f - MathF.Pow(1f - t, 3f);
        }

        public override void Update()
        {
            if (!EnableRender) return;
            
            base.Update();

            if (isAnimating)
            {
                animationTime += DrawSpeed * Time.Delta;

                if (animationTime >= 1f)
                {
                    animationTime = 1f;
                    isAnimating = false;
                }

                float easedTime = EaseOutCubic(animationTime);
                animatedOffset = Vector3.Lerp(StartPosition, EndPosition, easedTime);
            }

            if (!isAnimating)
            {
                Vector2 mouseDelta = Input.Mouse.Delta;

                var sway = EnableSway ? SwayMultiplier : 0f;
                float swayX = -mouseDelta.X * sway;
                float swayY = -mouseDelta.Y * sway;

                swayX = Math.Clamp(swayX, -MaxSwayAngle, MaxSwayAngle);
                swayY = Math.Clamp(swayY, -MaxSwayAngle, MaxSwayAngle);

                Vector3 targetSway = new Vector3(swayY, -swayX, 0);
                currentSwayRotation = Vector3.Lerp(currentSwayRotation, targetSway,
                                                  DampingFactor * Time.Delta);
            }
        }
        public override void Render()
        {
            
            base.Render();
            if (!EnableRender)
            {
                return;
            }

            if(Input.IsActionDown("zoom"))
            {
                itemCamera.FOV = 60;
            }
            if (Input.IsActionUp("zoom"))
            {
                itemCamera.FOV = 80;
            }

            if (debug)
                PlaceModelDebug();

            Matrix4 swayMatrix = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(currentSwayRotation.X)) *
                         Matrix4.CreateRotationY(MathHelper.DegreesToRadians(currentSwayRotation.Y)) *
                         Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(currentSwayRotation.Z));



            if (UseMainCamera && Camera.Main != null)
            {
                Position = Camera.Main.PositionWorld;
                model =
                     Matrix4.CreateTranslation(animatedOffset) *  swayMatrix *
                     Matrix4.CreateTranslation(Camera.Main.CameraRelativeRender ?  RenderSpace.ToRender(Position) : Position) *
                     Matrix4.CreateRotationY(additionalRotationAngle );
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

                var mtx =  SwayMultiplier <= 0 ? Matrix4.CreateTranslation(animatedOffset) : Matrix4.CreateTranslation(animatedOffset) * swayMatrix;
                model =
                     mtx *
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

            var offset = Position;
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

            Position = offset;
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

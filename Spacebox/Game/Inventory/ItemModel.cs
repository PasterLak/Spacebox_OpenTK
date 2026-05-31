using Engine;
using Engine.Light;
using OpenTK.Mathematics;
using Spacebox.Game.Player;
using Spacebox.Game.Resource;
using System;

namespace Spacebox.Game
{
    public class ItemModel : Node3D
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

        public Matrix4 SwayMatrix { get; set; } = Matrix4.Identity;

        public ItemModel(Mesh mesh, Texture2D texture, Texture2D emission)
        {
            Mesh = mesh;

            texture.FilterMode = FilterMode.Nearest;

            Material = new ItemMaterial(texture, emission);

            itemCamera = new Camera360Base(Vector3.Zero, false);
            itemCamera.AspectRatio = SpaceboxWindow.Instance.GetAspectRatio();
            itemCamera.FOV = 80;
            itemCamera.DepthNear = 0.01f;
            itemCamera.DepthFar = 100f;
            animatedOffset = EndPosition;


            SpaceboxWindow.OnResized += Resize;
        }

        ~ItemModel()
        {
            SpaceboxWindow.OnResized -= Resize;
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
        }

        public override void Render()
        {
            base.Render();

            if (!EnableRender)
            {
                return;
            }

            if (Input.IsActionDown("zoom"))
            {
                itemCamera.FOV = 60;
            }
            if (Input.IsActionUp("zoom"))
            {
                itemCamera.FOV = 80;
            }

            if (debug)
                PlaceModelDebug();

            if (UseMainCamera && Camera.Main != null)
            {
                Position = Camera.Main.PositionWorld;

                model =
                     Matrix4.CreateTranslation(animatedOffset) * SwayMatrix *
                     Matrix4.CreateTranslation(Camera.Main.CameraRelativeRender ? RenderSpace.ToRender(Position) : Position) *
                     Matrix4.CreateRotationY(additionalRotationAngle);

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

                var mtx = Matrix4.CreateTranslation(animatedOffset) * SwayMatrix;

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

        public override void Destroy()
        {
            base.Destroy();
            if (Material != null)
            {
                Material.MainTexture?.Dispose();
            }
            Material = null;
            itemCamera = null;
            Mesh?.Dispose();
        }
    }
}
using Engine.Components;
using Engine.Utils;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine
{
    public class FreeCamera : Camera
    {
        private float _moveSpeed = 10f;
        private float _mouseSensitivity = 0.12f;
        private float _pitch;
        private float _yaw = -90f;

        private ModelRendererComponent _renderer;

        public FreeCamera(Vector3 position, bool isMain = true) : base(position, isMain) 
        {
            Input.HideCursor();
            Name = "FreeCamera";

           var c = AttachComponent(new SphereCollider());
            c.DebugCollision = false;

            Model m = new Model(GenMesh.CreateSphere(4), new ColorMaterial());
             _renderer = AttachComponent(new ModelRendererComponent(m));
            _renderer.Model.Material.Color = Color4.Blue;
            _renderer.Model.Material.RenderFace = RenderFace.Front;
        }

        public override void Update()
        {
            float dt = Time.Delta;

            //VisualDebug.DrawAxes(Position + Forward * 3f);
            if (IsMain)
            {
                _renderer.Enabled = false;
                Vector3 dir = Vector3.Zero;

                float speedShift = (Input.IsKey(Keys.LeftShift)) ? 3 : 1;
                if (Input.IsKey(Keys.W)) dir += _front;
                if (Input.IsKey(Keys.S)) dir -= _front;
                if (Input.IsKey(Keys.A)) dir -= _right;
                if (Input.IsKey(Keys.D)) dir += _right;
                if (Input.IsKey(Keys.Space)) dir += Vector3.UnitY;
                if (Input.IsKey(Keys.LeftControl)) dir -= Vector3.UnitY;
                if (dir != Vector3.Zero) Position += dir.Normalized() * _moveSpeed * speedShift * dt;

                Vector2 md = Input.Mouse.Delta;
                _yaw += md.X * _mouseSensitivity;
                _pitch -= md.Y * _mouseSensitivity;
                _pitch = MathHelper.Clamp(_pitch, -89f, 89f);

                UpdateVectors();
            }
            else
            {
                _renderer.Enabled = true;
               
            }
            
            base.Update();
        }

        protected override void UpdateVectors()
        {
            Vector3 f;
            f.X = MathF.Cos(MathHelper.DegreesToRadians(_yaw)) * MathF.Cos(MathHelper.DegreesToRadians(_pitch));
            f.Y = MathF.Sin(MathHelper.DegreesToRadians(_pitch));
            f.Z = MathF.Sin(MathHelper.DegreesToRadians(_yaw)) * MathF.Cos(MathHelper.DegreesToRadians(_pitch));
            _front = f.Normalized();
            _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
            _up = Vector3.Normalize(Vector3.Cross(_right, _front));

           // Rotation = Qu
        }
    }
}

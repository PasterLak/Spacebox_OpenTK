using OpenTK.Mathematics;

namespace Engine
{
    public class Transform3D
    {
        private Vector3 _position = Vector3.Zero;
        private Vector3 _rotation = Vector3.Zero;
        private Vector3 _scale = Vector3.One;
        private bool _dirty = true;
        private bool _relative = false;
        private Matrix4 _cached;
        public Node3D? Owner { get; set; }

        public virtual Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                MarkDirty();
            }
        }

        public Vector3 Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                MarkDirty();
            }
        }

        public virtual Vector3 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                MarkDirty();
            }
        }

        public Matrix4 GetModelMatrix()
        {
            var relativeToCamera = Camera.Main != null && Camera.Main.CameraRelativeRender;
            if (_relative != relativeToCamera)
            {
                _relative = relativeToCamera;
                _dirty = true;
            }

            var pos = _position;
            if (relativeToCamera && Owner?.Parent == null)
                pos -= Camera.Main!.Position;

            var local = BuildLocalMatrix(pos);
            return Owner?.Parent != null ? local * Owner.Parent.GetModelMatrix() : local;
        }

        public Matrix4 GetModelMatrixPoor() => BuildLocalMatrix(_position);

        public void ResetTransform()
        {
            _position = Vector3.Zero;
            _rotation = Vector3.Zero;
            _scale = Vector3.One;
            MarkDirty();
        }

        protected void MarkDirty()
        {
            if (_dirty) return;
            _dirty = true;
            if (Owner == null) return;
            for (int i = 0; i < Owner.Children.Count; i++)
                Owner.Children[i].MarkDirty();
        }

        private Matrix4 BuildLocalMatrix(Vector3 pos)
        {
            if (!_dirty) return _cached;

            var translation = Matrix4.CreateTranslation(pos);
            var rotX = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(_rotation.X));
            var rotY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(_rotation.Y));
            var rotZ = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(_rotation.Z));
            var rotation = rotZ * rotY * rotX;
            var scale = Owner?.Resizable == false ? Matrix4.Identity : Matrix4.CreateScale(_scale);

            _cached = scale * rotation * translation;
            _dirty = false;
            return _cached;
        }
    }
}

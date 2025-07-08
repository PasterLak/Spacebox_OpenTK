using OpenTK.Mathematics;

namespace Engine
{
    public class Transform3D
    {
        private Vector3 _position = Vector3.Zero;
        private Vector3 _rotation = Vector3.Zero;
        private Vector3 _scale = Vector3.One;
        private bool _dirty = true;
        private Matrix4 _cached;

        protected Node3D? Owner { get; set; }
        
      
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

        public Matrix4 GetRenderModelMatrix()
        {
            return GetModelMatrix() *
                   Matrix4.CreateTranslation(-RenderSpace.Origin);
        }

        public Matrix4 GetModelMatrix()
        {

            Matrix4 local = BuildLocalMatrix();

            return Owner?.Parent != null
                   ? local * Owner.Parent.GetModelMatrix()
                   : local;
        }
 
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

        private Matrix4 BuildLocalMatrix()
        {
            if (!_dirty) return _cached;

            var translation = Matrix4.CreateTranslation(_position);
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

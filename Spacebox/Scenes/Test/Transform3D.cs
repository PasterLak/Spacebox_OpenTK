using Engine;
using OpenTK.Mathematics;
using SharpNBT;
using System;


namespace Spacebox.Scenes.Test
{
    public class Transform3D
    {
        public virtual Vector3 Position
        {
            get => position;
            set { position = value; MarkDirty(); }
        }
        Vector3 position = Vector3.Zero;
        public Quaternion Rotation
        {
            get => rotation;
            set { rotation = value; MarkDirty(); }
        }
        Quaternion rotation = Quaternion.Identity; // Euler
        public virtual Vector3 Scale
        {
            get => scale;
            set { scale = value; MarkDirty(); }
        }
        Vector3 scale = Vector3.One;

        private Matrix4 _localMatrix = Matrix4.Identity;
        private Matrix4 _worldMatrix = Matrix4.Identity;
        private bool _localDirty = true;
        private bool _worldDirty = true;

        public SceneNode? Owner { get; set; }

        public Matrix4 LocalMatrix
        {
            get
            {
                if (_localDirty)
                {
                    _localMatrix = Matrix4.CreateScale(Scale)
                                   * Matrix4.CreateFromQuaternion(Rotation)
                                   * Matrix4.CreateTranslation(Position);
                    _localDirty = false;
                    _worldDirty = true;
                }
                return _localMatrix;
            }
        }
        private bool relative = false;
        public Matrix4 WorldMatrix
        {
            get
            {
                bool relativeToCamera = (Camera.Main != null && Camera.Main.CameraRelativeRender);

                if (relative != relativeToCamera)
                {
                    relative = relativeToCamera;
                    MarkDirty();
                }

                var pos = Position;

                if (relativeToCamera)
                {
                    if (Owner. Parent == null)
                        pos = Position - Camera.Main.Position;

                    MarkDirty();
                }

                if (Owner.Parent != null)
                {
                    return GetModelMatrixPoor(pos) * Owner.Parent.WorldMatrix;
                }
                else
                {
                    return GetModelMatrixPoor(pos);
                }
            }
        }

        private Matrix4 GetModelMatrixPoor(Vector3 pos)
        {
            /*if (!dirty)
            {
                return cachedModelMatrix;
            }*/

            var translation = Matrix4.CreateTranslation(pos);
            var rotationX = Matrix4.CreateFromQuaternion(Rotation);
  
           // var rotation = rotationZ * rotationY * rotationX;
            var scale = Matrix4.CreateScale(Scale) ;

           // dirty = false;
            return scale * rotationX * translation;
            //return cachedModelMatrix;
        }

        public Matrix4 WorldMatrix2
        {
            get
            {
                bool relativeToCamera = Camera.Main != null && Camera.Main.CameraRelativeRender;

                if (_worldDirty || (relativeToCamera && Owner?.Parent == null))
                {
                    Vector3 position = Position;

                
                    if (relativeToCamera && (Owner?.Parent == null))
                    {
                        position -= Camera.Main.Position;
                    }

                    Matrix4 localMatrix = Matrix4.CreateScale(Scale)
                                         * Matrix4.CreateFromQuaternion(Rotation)
                                         * Matrix4.CreateTranslation(position);

                    _worldMatrix = Owner?.Parent != null
                        ? localMatrix * Owner.Parent.WorldMatrix
                        : localMatrix;

                    _worldDirty = false;
                }

                return _worldMatrix;
            }
        }


        public Matrix4 WorldMatrixOIld
        {
            get
            {
                if (_worldDirty)
                {
                   

                    _worldMatrix = Owner?.Parent != null
                        ? LocalMatrix * Owner.Parent.WorldMatrix
                        : LocalMatrix;
                    _worldDirty = false;
                }
                return _worldMatrix;
            }
        }
        private void MarkDirty()
        {
            _localDirty = true;
            _worldDirty = true;
        }

        public void MarkDirtyRecursive()
        {
            MarkDirty();
            foreach (var child in Owner?.Children ?? new List<SceneNode>())
                child.MarkDirtyRecursive();
        }

        public Vector3 GetWorldPosition() => new Vector3(WorldMatrix.M41, WorldMatrix.M42, WorldMatrix.M43);

        public Quaternion GetWorldRotation()
        {
            if (Owner?.Parent != null)
                return Owner.Parent.GetWorldRotation() * Rotation;
            return Rotation;
        }

        public void Reset()
        {
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = Vector3.One;
            MarkDirty();
        }
    }
}

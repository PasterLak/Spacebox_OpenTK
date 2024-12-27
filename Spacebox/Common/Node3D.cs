﻿using OpenTK.Mathematics;
using static System.Net.Mime.MediaTypeNames;

namespace Spacebox.Common
{
    public class Node3D :  IEquatable<Node3D>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = "Tranform";

        public virtual Vector3 Position
        {
            get => position;
            set { position = value; MarkDirty(); }
        }
        Vector3 position = Vector3.Zero;
        public Vector3 Rotation
        {
            get => rotation;
            set { rotation = value; MarkDirty(); }
        }
        Vector3 rotation = Vector3.Zero; // Euler
        public virtual Vector3 Scale
        {
            get => scale;
            set { scale = value; MarkDirty(); }
        }
        Vector3 scale = Vector3.One;

        public bool Resizable { get; protected set; } = true;

        public Node3D Parent { get;  set; } = null;
        public bool HasParent => Parent != null;

        public List<Node3D> Children { get; protected set; } = new List<Node3D>();
        public bool HasChildren => Children.Count > 0;

        private bool dirty = true;
        private Matrix4 cachedModelMatrix;

        public virtual void AddChild(Node3D node)
        {
            if (Children.Contains(node)) return;
           
            Children.Add(node);
            node.Parent = this;
            
        }

        private void MarkDirty()
        {
            if (!dirty)
            {
                dirty = true;
                for (int i = 0; i < Children.Count; i++)
                    Children[i].MarkDirty();
            }
        }

        public void LookAt(Vector3 target)
        {
            var look = Matrix4.LookAt(Position, target, Vector3.UnitY);
            look.Transpose();
            look.ExtractRotation().ToEulerAngles(out var euler);
          
            Rotation = euler; GetModelMatrixPoor();
        }
        /*
         
         
void lookat(const float3& from, const float3& to, const float3& arbitraryUp, mat4& m) 
{ 
    float3 forward = from - to; 
    normalize(forward); 
    float3 right = cross(arbitraryUp, forward); 
    normalize(right); 
    float3 up = cross(forward, right); 
 
    m[0][0] = right.x,   m[0][1] = right.y,   m[0][2] = right.z; 
    m[1][0] = up.x,      m[1][1] = up.y,      m[1][2] = up.z; 
    m[2][0] = forward.x, m[2][1] = forward.y, m[2][2] = forward.z; 
    m[3][0] = from.x,    m[3][1] = from.y,    m[3][2] = from.z; 
} 
         * */
        public void LookAt2(Vector3 target)
        {
            Vector3 forward = Position - target;
            forward.Normalize();

            Vector3 right = Vector3.Cross(Vector3.UnitY, forward);
            right.Normalize();

            Vector3 up = Vector3.Cross(forward, right);
        }

        /*
        
public var target : GameObject;
public var lookSpeed : float;
public var lookThreshold : float;
public var lookSensitivity : float;

function Update()
{
	AdjustableLookAt(target.transform);
}

function AdjustableLookAt(targetTransform : Transform)
{
	//Calculate the vector to the target
	var toTarget = targetTransform.position - transform.position;
	
	//Rotate if the angle to the target is outside the threshold
	if (Mathf.Abs(Vector3.Angle(transform.forward, toTarget)) > lookThreshold || Mathf.Abs(Vector3.Angle(transform.forward, toTarget)) < -lookThreshold)
	{
		//Check whether we should rotate CW or CCW to the target
		if (Vector3.Dot(Vector3.Cross(toTarget, transform.forward), transform.up) > lookSensitivity)
		{
			//We should rotate CCW
			transform.Rotate(transform.up * lookSpeed);
		}
		else if (Vector3.Dot(Vector3.Cross(toTarget, transform.forward), transform.up) < -lookSensitivity)
		{
			//We should rotate CW
			transform.Rotate(transform.up * -lookSpeed);
		}
	}
}
         */
        public void LookAt3(Node3D target)
        {


            const float lookSpeed = 0.5f;
            const float lookThreshold = 1;
            const float lookSensitivity = 0.1f;

            var _front = Vector3.Transform(-Vector3.UnitZ, Quaternion.FromEulerAngles(target.Rotation));
            var _up = Vector3.Transform(Vector3.UnitY, Quaternion.FromEulerAngles(target.Rotation));
            


            var toTarget = target.position - Position;

            if(MathF.Abs(Vector3.CalculateAngle(_front, toTarget)) > lookThreshold || MathF.Abs(Vector3.CalculateAngle(_front, toTarget)) > -lookThreshold)
            {
                if (Vector3.Dot(Vector3.Cross(toTarget, _front), _up) > lookSensitivity)
                {
                    //We should rotate CCW
                    Rotate(_up * lookSpeed);
                }
                else if (Vector3.Dot(Vector3.Cross(toTarget, _front), _up) < -lookSensitivity)
                {
                    //We should rotate CW
                    Rotate(_up * -lookSpeed);
                }
            }
        }

        public void Rotate(Vector3 rot)
        {
            Rotate(rot.X, rot.Y, rot.Z);
        }

        public void Rotate(float x,float y,float z)
        {
            Rotation += new Vector3(x,y,z);
        }

        private bool relative = false;
        public Matrix4 GetModelMatrix()
        {

            bool relativeToCamera = false;

            if (Camera.Main != null && Camera.Main.CameraRelativeRender) relativeToCamera = true;

            if(relative != relativeToCamera)
            {
                relative = relativeToCamera;
                dirty = true;
            }

            var pos = Position;

            if(relativeToCamera )
            {
                if(Parent == null)
                pos = Position - Camera.Main.Position;

                dirty = true;
            }

            if (Parent != null)
            {
                return GetModelMatrixPoor(pos) * Parent.GetModelMatrix(); 
            }
            else
            {
                return GetModelMatrixPoor(pos);
            }
        }


        public Matrix4 GetModelMatrixPoor()
        {
            return GetModelMatrixPoor(Position);
        }
        private Matrix4 GetModelMatrixPoor(Vector3 pos)
        {
            if(!dirty)
            {
                return cachedModelMatrix;
            }

            var translation = Matrix4.CreateTranslation(pos);
            var rotationX = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(Rotation.X));
            var rotationY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(Rotation.Y));
            var rotationZ = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation.Z));
            var rotation = rotationZ * rotationY * rotationX;
            var scale = Resizable ? Matrix4.CreateScale(Scale) : Matrix4.Identity;

            dirty = false;
            cachedModelMatrix = scale * rotation * translation;
            return cachedModelMatrix;
        }

        public Vector3 GetWorldPosition()
        {
            if (Parent != null)
            {
                if(Rotation == Vector3.Zero && Parent.Rotation == Vector3.Zero)
                {
                    return Parent.GetWorldPosition() + Position;
                }

                return LocalToWorld(Position, Parent);
            }
            else
            {
                return Position;
            }
        }

        public static Vector3 WorldToLocal(Vector3 worldPosition, Node3D node)
        {
            Matrix4 invModel = node.GetModelMatrixPoor().Inverted();
            return Vector3.TransformPosition(worldPosition, invModel);
        }

        public static Vector3 LocalToWorld(Vector3 localPosition, Node3D node)
        {
            return Vector3.TransformPosition(localPosition, node.GetModelMatrixPoor());
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Node3D);
        }

        public bool Equals(Node3D other)
        {
            if (ReferenceEquals(other, null))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

     
        public static bool operator ==(Node3D left, Node3D right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);

            return left.Equals(right);
        }

       
        public static bool operator !=(Node3D left, Node3D right)
        {
            return !(left == right);
        }

    }
}

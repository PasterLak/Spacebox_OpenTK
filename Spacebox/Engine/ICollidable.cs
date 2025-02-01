using OpenTK.Mathematics;
using Spacebox.Engine.Physics;

namespace Spacebox.Engine
{

    public interface ICollidable
    {
        bool IsStatic { get;  }
        BoundingVolume BoundingVolume { get; }

        void UpdateBounding();

        void OnCollisionEnter(ICollidable other);
        void OnCollisionExit(ICollidable other);

    }
}

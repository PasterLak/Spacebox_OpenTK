﻿using OpenTK.Mathematics;
using Spacebox.Common.Physics;

namespace Spacebox.Common
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

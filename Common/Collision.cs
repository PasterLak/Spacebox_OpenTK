using OpenTK.Mathematics;
using System.Reflection.Metadata.Ecma335;

namespace Spacebox.Common
{
    public abstract class Collision : Transform
    {
        public bool IsStatic { get; protected set; }
        public bool IsTrigger { get; protected set; } = false;


        public BoundingVolume BoundingVolume { get; protected set; }

        private Vector3 _offset = Vector3.Zero;
        public Vector3 Offset
        {
            get => _offset; 
            set
            {
                Offset = value;
                UpdateBounding();
            }
        }
        public Vector3 CollisionScale { get;  set; } = Vector3.One;

        public CollisionManager CollisionManager { get;  set; }
        public CollisionLayer Layer { get; set; } = CollisionLayer.Default;

        private HashSet<Collision> _currentColliders = new HashSet<Collision>();

        private Color4 debugColor = Color4.Gray;
        protected Collision(BoundingVolume boundingVolume, bool isStatic)
           
        {
            BoundingVolume = boundingVolume;
            IsStatic = isStatic;
            Position = boundingVolume.Center;

            Debug.AddCollisionToDraw(this);
            //UpdateBounding();
        }

        public void UpdateBounding()
        {
 
            if (BoundingVolume is BoundingBox box)
            {
                box.Center = base.Position + Offset;
                //box.Size = base.Scale;
            }
            else if (BoundingVolume is BoundingSphere sphere)
            {
                sphere.Center = base.Position + Offset;
            }
        }

        public override Vector3 Position { get { 
                return base.Position; }
            set
            {
                base.Position = value;
                UpdateBounding();
            }
        }

        public override Vector3 Scale
        {
            get
            {
                return base.Scale;
            }
            set
            {
                base.Scale = value;
                UpdateBounding();
            }
        }

        private Color4 colorBeforeContact;

        public virtual void OnCollisionEnter(Collision other) {
            colorBeforeContact = debugColor;
            SetCollisionDebugColor(Color4.Green);
        }

        public virtual void OnCollisionExit(Collision other) {
            SetCollisionDebugColor(colorBeforeContact);
        }

        public BoundingVolume GetBoundingVolumeAt(Vector3 position)
        {
            if (BoundingVolume is BoundingBox box)
            {
                return new BoundingBox(position, box.Size);
            }
            else if (BoundingVolume is BoundingSphere sphere)
            {
                return new BoundingSphere(position, sphere.Radius);
            }
            else
            {
                throw new NotSupportedException("Unsupported BoundingVolume type.");
            }
        }



        public void SetCollisionDebugColor(Color4 color)
        {
            debugColor = color;
        }
        public void DrawDebug()
        {
            DrawDebug(debugColor);
        }
        public void DrawDebug(Color4 color)
        {
            if (BoundingVolume is BoundingBox box)
            {
                Debug.DrawBoundingBox(box, color);
            }
            else if (BoundingVolume is BoundingSphere sphere)
            {
                Debug.DrawBoundingSphere(sphere, color);
            }
        }

        public static CollisionLayer GetLayerByName(string layerName)
        {
            return layerName switch
            {
                "Default" => CollisionLayer.Default,
                "Terrain" => CollisionLayer.Terrain,
                "Player" => CollisionLayer.Player,
                "Enemy" => CollisionLayer.Enemy,
                "Projectile" => CollisionLayer.Projectile,
                _ => CollisionLayer.Default,
            };
        }
    }



        [Flags]
        public enum CollisionLayer
        {
            None = 0,
            Default = 1 << 0,      // 1
            Terrain = 1 << 1,      // 2
            Player = 1 << 2,       // 4
            Enemy = 1 << 3,        // 8
            Projectile = 1 << 4,   // 16
                                   // Добавьте другие слои по мере необходимости
            All = ~0                // Все слои
        }

  


}

using OpenTK.Mathematics;

namespace Spacebox.Common
{
    public class Trigger : StaticBody
    {
        public Trigger(Vector3 center, Vector3 size)
            : base(new BoundingBox(center, size))
        {
            Position = center;
            Init();



        }
        public Trigger( BoundingBox boundingBox)
            : base(boundingBox)
        {
            Position = boundingBox.Center;
            Init();
        }

        private void Init()
        {
            SetCollisionDebugColor(Color4.Cyan);
            IsTrigger = true;

            Name = "Trigger";
        }

        public override void OnCollisionEnter(Collision other)
        {
          if(other is StaticBody) { return; }

                Console.WriteLine("Trigger Enter!");
            SetCollisionDebugColor(Color4.Green);

        }

        public override void OnCollisionExit(Collision other)
        {
            if (other is StaticBody) { return; }
            Console.WriteLine("Trigger Exit!");
            SetCollisionDebugColor(Color4.Cyan);
        }

    }
}

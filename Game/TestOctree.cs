using OpenTK.Mathematics;
using Spacebox.Common;
using Spacebox.GUI;
using System.Collections.Generic;

namespace Spacebox.Game
{

    public class BBlock
    {
        public BoundingBox box;
        public BBlock(BoundingBox box)
        {
            this.box = box;
        }
    }
    public class TestOctree
    {

        Octree<BBlock> octree;
        public TestOctree() {

            octree = new Octree<BBlock>(8, new Vector3(-5,-5,-5), 1,1);

            Add(new Vector3(-1, -1, -1));
            Add(new Vector3(-1, 2, -1));
            Add(new Vector3(0, 0, 0));

            //TagManager.RegisterTag(new Tag("(0,0,0)", octree.Position, Color4.White));
        }

        public void Draw(Astronaut astronaut)
        {
            

            

            HashSet<BBlock> result = new HashSet<BBlock>();

            octree.FindDataInRadius(astronaut.Position, 5f,  result);

            foreach (BBlock b in result)
            {
                //b.box.Size = new Vector3(0.9f, 0.9f, 0.9f);
                VisualDebug.DrawBoundingBox(b.box, Color4.Blue);
            }

            octree.DrawDebug();

        }

        public void Add(Vector3 pos)
        {
            pos += octree.Position;
            // + new Vector3(0.5f,0.5f, 0.5f)
            var b = new BoundingBox(pos  + new Vector3(0.5f, 0.5f, 0.5f), Vector3.One);
            //var b = BoundingBox.CreateFromMinMax(Vector3.Zero, Vector3.One);
            //var b2 = BoundingBox.CreateFromMinMax(Vector3.Zero, new Vector3(0.9f, 0.9f, 0.9f));
            octree.Add(new BBlock(b), b);
        }


    }
}

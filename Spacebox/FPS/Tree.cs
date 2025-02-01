using Engine;
using OpenTK.Mathematics;


namespace Spacebox.FPS
{
    public class Tree : Node3D, INotTransparent, ITransparent
    {

        Model trunk;
        Model leaves;

        public Model GetModel() { return trunk; }

        public Tree(Vector3 pos, Shader shader)
        {
            Position = pos;

            trunk = new Model("Resources/Models/tree0.obj",
               new Material(shader, TextureManager.GetTexture("Resources/Textures/Game/wood.png")));

            trunk.Position = pos;


            leaves = new Model("Resources/Models/leaves.obj",
                new Material(shader, TextureManager.GetTexture("Resources/Textures/Game/leaves.png")));

            leaves.Position = pos;

            Name = "tree";
        }


        public void Draw(Camera camera)
        {
            trunk.Draw(camera);

        }

        public void DrawTransparent(Camera camera)
        {
            leaves.Draw(camera);
        }
    }
}

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
               new Material(shader, Resources.Get<Texture2D>("Resources/Textures/Game/wood.png")));

            trunk.Position = pos;


            leaves = new Model("Resources/Models/leaves.obj",
                new Material(shader, Resources.Get<Texture2D>("Resources/Textures/Game/leaves.png")));

            leaves.Position = pos;

            Name = "tree";
        }


        public void Render(Camera camera)
        {
            trunk.Render(camera);

        }

        public void DrawTransparent(Camera camera)
        {
            leaves.Render(camera);
        }
    }
}

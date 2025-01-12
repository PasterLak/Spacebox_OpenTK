
using OpenTK.Mathematics;
using Spacebox.Common;

namespace Spacebox.FPS
{
    public class TestGraph
    {
        public static void Test()
        {
            SceneGraph scene = new SceneGraph();

            Node3D root1 = new Node3D { Name = "Root1", Position = new Vector3(0, 0, 0) };
            Node3D root2 = new Node3D { Name = "Root2", Position = new Vector3(5, 0, 0) };

            scene.AddRoot(root1);
            scene.AddRoot(root2);

            Node3D child1 = new Node3D { Name = "Child1", Position = new Vector3(1, 0, 0) };
            Node3D child2 = new Node3D { Name = "Child2", Position = new Vector3(-1, 0, 0) };
            Node3D grandChild = new Node3D { Name = "GrandChild", Position = new Vector3(0, 1, 0) };

            root1.AddChild(child1);
            root1.AddChild(child2);
            child1.AddChild(grandChild);

            scene.UpdateTransforms();

            scene.Traverse(node =>
            {
                Matrix4 modelMatrix = node.GetModelMatrix();
                Debug.WriteLine($"Node: {node.Name}, Model Matrix:\n{modelMatrix}\n");
            });

            Node3D searchByName = scene.FindNode("GrandChild");
            if (searchByName != null)
            {
                Debug.WriteLine($"Found node by name: {searchByName.Name}, ID: {searchByName.Id}");
            }
            Debug.WriteLine("-------------------");
            scene.PrintHierarchy();
            Debug.WriteLine("-------------------");
            Guid searchId = child2.Id;
            Node3D searchById = scene.FindNode(searchId);
            if (searchById != null)
            {
                Debug.WriteLine($"Found node by ID: {searchById.Name}, ID: {searchById.Id}");
            }

            child1.Position = new Vector3(2, 0, 0);
            scene.UpdateTransforms();

            Debug.WriteLine("After changing Child1's position:");
            scene.Traverse(node =>
            {
                Matrix4 modelMatrix = node.GetModelMatrix();
                Debug.WriteLine($"Node: {node.Name}, Model Matrix:\n{modelMatrix}\n");
            });

            root1.Children.Remove(child2);
            child2.Parent = null;
            scene.RemoveRoot(child2);

            Debug.WriteLine("After removing Child2 from Root1:");
            scene.Traverse(node =>
            {
                Matrix4 modelMatrix = node.GetModelMatrix();
                Debug.WriteLine($"Node: {node.Name}, Model Matrix:\n{modelMatrix}\n");
            });
            Debug.WriteLine("-------------------");
            scene.PrintHierarchy();
            Debug.WriteLine("-------------------");
            scene.AddRoot(child2);
            Debug.WriteLine("After adding Child2 as a root node:");
            scene.Traverse(node =>
            {
                Matrix4 modelMatrix = node.GetModelMatrix();
                Debug.WriteLine($"Node: {node.Name}, Model Matrix:\n{modelMatrix}\n");
            });

            Node3D searchByIdAfterMove = scene.FindNode(searchId);
            if (searchByIdAfterMove != null)
            {
                Debug.WriteLine($"Found node by ID after moving: {searchByIdAfterMove.Name}, ID: {searchByIdAfterMove.Id}");
            }
        }
    }
}

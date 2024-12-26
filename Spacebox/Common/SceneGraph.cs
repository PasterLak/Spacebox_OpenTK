
namespace Spacebox.Common
{
    public class SceneGraph
    {
        public List<Node3D> Roots { get; } = new List<Node3D>();
        Dictionary<Guid, Node3D> nodeLookup = new Dictionary<Guid, Node3D>();

        public void AddRoot(Node3D node)
        {
            if (!Roots.Contains(node))
            {
                Roots.Add(node);
                AddToLookup(node);
            }
        }

        public void RemoveRoot(Node3D node)
        {
            if (Roots.Remove(node))
                RemoveFromLookup(node);
        }

        public void AddToLookup(Node3D node)
        {
            
            nodeLookup[node.Id] = node;

            for (int i = 0; i < node.Children.Count; i++)
                AddToLookup(node.Children[i]);
        }

        private void RemoveFromLookup(Node3D node)
        {
            nodeLookup.Remove(node.Id);
            for (int i = 0; i < node.Children.Count; i++)
                RemoveFromLookup(node.Children[i]);
        }

        public void Traverse(Action<Node3D> action)
        {
            for (int i = 0; i < Roots.Count; i++)
                VisitNode(Roots[i], action);
        }

        private void VisitNode(Node3D node, Action<Node3D> action)
        {
            action(node);
            for (int i = 0; i < node.Children.Count; i++)
                VisitNode(node.Children[i], action);
        }

        public Node3D FindNode(Guid id)
        {
            nodeLookup.TryGetValue(id, out var node);
            return node;
        }

        public Node3D FindNode(string name)
        {
            Node3D result = null;
            Traverse(n =>
            {
                if (n.Name == name) result = n;
            });
            return result;
        }

        public void UpdateTransforms()
        {
            for (int i = 0; i < Roots.Count; i++)
                Roots[i].GetModelMatrix();
        }

        public void PrintHierarchy()
        {
            Debug.WriteLine($"----------- [SceneGraph] -----------");
            foreach (var root in Roots)
            {
                PrintNode(root, 0);
            }
            Debug.WriteLine($"------------------------------------");
        }

        private void PrintNode(Node3D node, int depth)
        {
            string indent = new string(' ', depth * 2);
            Debug.WriteLine($"{indent}{node.Name}-{node.Position}");
            foreach (var child in node.Children)
            {
                PrintNode(child, depth + 1);
            }
        }
    }
}

using ImGuiNET;

using System.Drawing;
using System.Numerics;

namespace Engine.UI
{
    public enum Anchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,

        Center,
        Top,
        Bottom,
        Left,
        Right


    }
    public abstract class Node2D
    {
        public Guid Id { get; } = Guid.NewGuid();

        protected Vector2 basePosition { get; private set; } = Vector2.Zero;
        public Vector2 Position { get; set; } = Vector2.Zero;
        protected Vector2 _position { get; set; } = new Vector2(0, 0);
        protected Vector2 _size { get; set; } = new Vector2(50, 50);
        public Vector2 Size { get; set; } = new Vector2(50, 50);
        public List<Node2D> Children { get; } = new List<Node2D>();

        private Node2D? _parent;
        public Node2D? Parent
        {
            get => _parent;
            set
            {
                if (_parent == value)
                    return;
                _parent?.Children.Remove(this);
                _parent = value;
                if (_parent != null && !_parent.Children.Contains(this))
                    _parent.Children.Add(this);

            }
        }

        public bool HasParent => Parent is not null;

        public Anchor Anchor { get; set; } = Anchor.BottomLeft;

        public Node2D()
        {
            _position = Position;
            //OnResized()
        }

        public virtual void Draw()
        {
            GetRealPosition();
            foreach (var child in Children)
            {

                child.Draw();
            }
        }

        public void Traverse(Action<Node2D> action)
        {
            action(this);
            foreach (var child in Children)
                child.Traverse(action);
        }
        public void AddChild(Node2D child)
        {
            if (child == null) return;
            if (child == this) return;
            if (child.Parent == this) return;
            child.Parent = this;
        }

        public void RemoveChild(Node2D child)
        {
            if (child == null) return;
            if (child.Parent != this) return;
            child.Parent = null;
        }

        public static Vector2 CalculateSize(Vector2 virtualSize, OpenTK.Mathematics.Vector2i virtualScreenSize, OpenTK.Mathematics.Vector2 sizeNow)
        {
            /*if (virtualSize == Vector2.Zero) return Vector2.Zero;

            var xyRatio = virtualSize.X / virtualSize.Y;
            var yDelta = virtualScreenSize.X / virtualSize.Y;
            var yNew = sizeNow.Y / yDelta;


            return new Vector2(yNew * xyRatio, yNew);*/

            var deltaX =  sizeNow.X / virtualScreenSize.X;
            var deltaY = sizeNow.Y / virtualScreenSize.Y;

            return new Vector2(virtualSize.X * deltaX, virtualSize.Y * deltaY); 
        }

        public void OnResized(Canvas canvas)
        {
            _size = CalculateSize(Size, canvas.VirtualSize, canvas.ScreenSize);
            _position = CalculateSize(Position, canvas.VirtualSize, canvas.ScreenSize);
            GetRealPosition();
            foreach (var child in Children)
            {

                child.OnResized(canvas);
            }
        }
        public void GetRealPosition()
        {
            var io = ImGui.GetIO();
            switch (Anchor)
            {
                case Anchor.Center:
                    basePosition = new Vector2(io.DisplaySize.X / 2f, io.DisplaySize.Y / 2f) - _size / 2f;
                    break;
                case Anchor.TopLeft:
                    basePosition = new Vector2(0, 0) + _position;
                    break;
                case Anchor.TopRight:
                    basePosition = new Vector2(io.DisplaySize.X - _size.X, 0)
                        + new Vector2(-_position.X, _position.Y);
                    break;
                case Anchor.BottomLeft:
                    basePosition = new Vector2(0, io.DisplaySize.Y - _size.Y)
                         + new Vector2(_position.X, -_position.Y);
                    break;
                case Anchor.BottomRight:
                    basePosition = new Vector2(io.DisplaySize.X - _size.X, io.DisplaySize.Y - _size.Y) - _position;
                    break;


                case Anchor.Right:
                    basePosition = new Vector2(io.DisplaySize.X - _size.X,
                        io.DisplaySize.Y * 0.5f - _size.Y * 0.5f)
                        + new Vector2(-_position.X, _position.Y);
                    break;
                case Anchor.Left:
                    basePosition = new Vector2(0,
                        io.DisplaySize.Y * 0.5f - _size.Y * 0.5f)
                        + new Vector2(_position.X, _position.Y);
                    break;
                case Anchor.Top:
                    basePosition = new Vector2(io.DisplaySize.X * 0.5f - _size.X * 0.5f, 0)
                        + new Vector2(-_position.X, _position.Y);
                    break;
                case Anchor.Bottom:
                    basePosition = new Vector2(io.DisplaySize.X * 0.5f - _size.X * 0.5f, io.DisplaySize.Y - _size.Y)
                        + new Vector2(_position.X, -_position.Y);
                    break;

                default:
                    break;
            }
        }
    }
}

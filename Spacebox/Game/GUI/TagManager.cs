using OpenTK.Mathematics;
using ImGuiNET;
using Engine;
using Engine.Components;

namespace Spacebox.Game.GUI
{
    public class TagManager : Component
    {
        public static TagManager Instance { get; private set; }

        private HashSet<Tag> _tags = new HashSet<Tag>();

        private Vector2 screenSize = new Vector2(512,256);

        public TagManager()
        {
            Instance = this;
            screenSize = Window.Instance.ClientSize;
        }

        public void RegisterTag(Tag tag)
        {
            if (!_tags.Contains(tag))
            {
                _tags.Add(tag);
            }
            else
            {
                Debug.Error("New Tag was not registered, this tag is already registered!");
            }
        }

        public void UnregisterTag(Tag tag)
        {
            if (_tags.Contains(tag))
            {
                _tags.Remove(tag);
            }
        }

        public void UnregisterTagByText(string text)
        {
            foreach (Tag tag in _tags)
            {
                if (tag.Text == text)
                {
                    _tags.Remove(tag);
                    return;
                }
            }
        }

        public void ClearTags()
        {
            _tags.Clear();
        }

        public override void OnAttached(Node3D onOwner)
        {
            base.OnAttached(onOwner);

            Window.OnResized += OnResized;
            OnResized(Window.Instance.ClientSize);
        }
        public override void OnDetached()
        {
            base.OnDetached();
            ClearTags();
            Window.OnResized -= OnResized;
        }

        public override void OnGUI()
        {
            
            var camera = Camera.Main;

            if (camera == null) return;
            if (_tags.Count == 0) return;
            if (!Settings.ShowInterface) return;

           
            ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero, ImGuiCond.Always);
            ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);

            ImGui.Begin("TagWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground |
                                     ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
                                     ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus |
                                     ImGuiWindowFlags.NoBringToFrontOnFocus);

            foreach (var tag in _tags)
            {
                Vector2? screenPosNullable = camera.WorldToScreenPoint(tag.WorldPosition, (int)screenSize.X, (int)screenSize.Y);
                if (screenPosNullable.HasValue)
                {
                    Vector2 screenPos = screenPosNullable.Value;
                    if (screenPos.X > 0 && screenPos.X <= (int)screenSize.X &&
                        screenPos.Y > 0 && screenPos.Y <= (int)screenSize.Y)
                    {
                        var textSize = ImGui.CalcTextSize(tag.Text);
                        ImGui.SetCursorPos(tag.GetTextPosition(screenPos.ToSystemVector2(), textSize));
                        var drawList = ImGui.GetWindowDrawList();
               
                        float distanceSquared = Vector3.DistanceSquared(camera.Position , tag.WorldPosition);
                        float newFontSize = Tag.CalculateFontSize(distanceSquared);
                        drawList.AddText(LoadFont(), newFontSize, ImGui.GetCursorPos(), tag.ColorUint, tag.Text);
                    }
                }
            }

            ImGui.End();
        }

        public void OnResized(Vector2 screenSize)
        {
            this.screenSize = screenSize;
            Tag.SetFontSizes(screenSize);
        }

        private static ImFontPtr LoadFont()
        {
            var io = ImGui.GetIO();
            return io.FontDefault;
        }
    }
}

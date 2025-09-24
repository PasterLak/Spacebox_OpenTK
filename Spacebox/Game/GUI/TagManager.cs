using Engine;
using Engine.Components;
using ImGuiNET;
using OpenTK.Mathematics;
using Spacebox.Game.Player;

namespace Spacebox.Game.GUI
{
    public class TagManager : Component
    {
        public static TagManager Instance { get; private set; }

        private readonly HashSet<Tag> _activeTags = new HashSet<Tag>();
        private readonly List<Tag> _visibleTags = new List<Tag>(256);
        private Vector2 _screenSize;
        private ImFontPtr _font;
        private Pool<Tag> _tagPool;

        public TagManager()
        {
            Instance = this;
            _screenSize = SpaceboxWindow.Instance.ClientSize;
            InitializePool();
        }

        private void InitializePool()
        {
            _tagPool = new Pool<Tag>(
                initialCount: 64,
                initializeFunc: tag => tag,
                onTakeFunc: null,
                resetFunc: tag => tag.Reset(),
                isActiveFunc: tag => tag.Enabled,
                setActiveFunc: (tag, active) => tag.Enabled = active,
                autoExpand: true
            );
        }

        public Tag CreateTag(string text, Vector3 worldPosition, Color4 color, bool isStatic = false, Tag.Alignment alignment = Tag.Alignment.Center)
        {
            var tag = _tagPool.Take();
            tag.Initialize(text, worldPosition, color, isStatic, alignment);
            _activeTags.Add(tag);
            return tag;
        }

        public bool ReleaseTag(Tag tag)
        {
            if (tag == null || !_activeTags.Contains(tag))
                return false;

            _activeTags.Remove(tag);
            _tagPool.Release(tag);
            return true;
        }

        public bool ReleaseTagByText(string text)
        {
            foreach (var tag in _activeTags)
            {
                if (tag.Text == text)
                {
                    _activeTags.Remove(tag);
                    _tagPool.Release(tag);
                    return true;
                }
            }
            return false;
        }

        public void ClearAllTags()
        {
            foreach (var tag in _activeTags)
            {
                _tagPool.Release(tag);
            }
            _activeTags.Clear();
        }

        public override void OnAttached(Node3D onOwner)
        {
            base.OnAttached(onOwner);
            SpaceboxWindow.OnResized += OnResized;
            OnResized(SpaceboxWindow.Instance.ClientSize);
            _font = ImGui.GetIO().FontDefault;
        }

        public override void OnDetached()
        {
            base.OnDetached();
            ClearAllTags();
            _tagPool = null;
            SpaceboxWindow.OnResized -= OnResized;
        }

        public override void OnGUI()
        {
            if (!ShouldRenderTags()) return;

            SetupWindow();
            FilterVisibleTags();
            RenderTags();
            ImGui.End();
        }

        private bool ShouldRenderTags()
        {
            return Camera.Main != null && _activeTags.Count > 0 && Settings.ShowInterface;
        }

        private void SetupWindow()
        {
            ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero, ImGuiCond.Always);
            ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);
            ImGui.Begin("TagWindow",
                ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground |
                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoInputs |
                ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus |
                ImGuiWindowFlags.NoBringToFrontOnFocus);
        }

        private void FilterVisibleTags()
        {
            _visibleTags.Clear();
            var camera = Camera.Main as Astronaut;

            if (camera == null) return;

            foreach (var tag in _activeTags)
            {
                if (!tag.Enabled) continue;
                if (CameraFrustum.IsBehindCameraDot(tag.WorldPosition, camera.PositionWorld, camera.Front))
                    continue;

                _visibleTags.Add(tag);
            }
        }

        private void RenderTags()
        {
            var camera = Camera.Main;
            var drawList = ImGui.GetWindowDrawList();

            foreach (var tag in _visibleTags)
            {
                var screenPos = camera.WorldToScreenPoint(tag.WorldPosition);

                if (screenPos == Vector2.Zero) continue;
                if (!IsOnScreen(screenPos)) continue;

                var distanceSquared = Vector3.DistanceSquared(camera.PositionWorld, tag.WorldPosition);

                if (distanceSquared > Settings.ENTITY_SEARCH_RADIUS * Settings.ENTITY_SEARCH_RADIUS)
                {
                   continue;
                }

                RenderTag(tag, screenPos, drawList, camera, distanceSquared);
            }
        }

        private bool IsOnScreen(Vector2 screenPos)
        {
            return screenPos.X > 0 && screenPos.X <= _screenSize.X &&
                   screenPos.Y > 0 && screenPos.Y <= _screenSize.Y;
        }

        private void RenderTag(Tag tag, Vector2 screenPos, ImDrawListPtr drawList, Camera camera, float distanceSquared)
        {
            var textSize = ImGui.CalcTextSize(tag.Text);
            var cursorPos = tag.GetTextPosition(screenPos.ToSystemVector2(), textSize);

            ImGui.SetCursorPos(cursorPos);

            var fontSize = Tag.CalculateFontSize(distanceSquared);

            drawList.AddText(_font, fontSize, ImGui.GetCursorPos(), tag.ColorUint, tag.Text);
        }

        private void OnResized(Vector2 screenSize)
        {
            _screenSize = screenSize;
            Tag.SetFontSizes(screenSize);
        }

        public List<Tag> GetStaticTags() => _activeTags.Where(tag => tag.IsStatic).ToList();

        public int ActiveTagsCount => _activeTags.Count;
        public int PooledTagsCount => _tagPool?.AvailableObjects ?? 0;
        public int TotalTagsCount => _tagPool?.TotalObjects ?? 0;
    }
}
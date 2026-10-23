using Engine;
using Engine.Components;
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
        private Pool<Tag> _tagPool;

        public Func<RadarSettings?>? RadarSource { get; set; }
        public RadarSettings? Radar => RadarSource != null ? RadarSource() : RadarSettings.Unlimited;

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
              
                setActiveFunc: (tag, active) => tag.Visible = active,
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

        public bool TryGetTagByName(string text, out Tag foundTag)
        {
            foreach (var tag in _activeTags)
            {
                if (tag.Text == text)
                {
                    foundTag = tag;
                    return true;
                }
            }
            foundTag = null;
            return false;
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
        }

        public override void OnDetached()
        {
            base.OnDetached();
            ClearAllTags();
            _tagPool = null;
            SpaceboxWindow.OnResized -= OnResized;
            Instance = null;
        }

        public readonly struct ScreenTag
        {
            public readonly Tag Tag;
            public readonly Vector2 Screen;
            public readonly float DistanceSquared;

            public ScreenTag(Tag tag, Vector2 screen, float distanceSquared)
            {
                Tag = tag;
                Screen = screen;
                DistanceSquared = distanceSquared;
            }
        }

        public void CollectScreenTags(List<ScreenTag> result)
        {
            result.Clear();
            if (!ShouldRenderTags()) return;

            FilterVisibleTags();
            CollectOnScreen(result);
        }

        private bool ShouldRenderTags()
        {
            return Camera.Main != null && _activeTags.Count > 0 && Settings.ShowInterface;
        }

        private void FilterVisibleTags()
        {
            _visibleTags.Clear();
            var camera = Camera.Main;

            if (camera == null) return;

            foreach (var tag in _activeTags)
            {
                if (!tag.Visible) continue;
                if (CameraFrustum.IsBehindCamera(tag.WorldPosition, camera.PositionWorld, camera.Front))
                    continue;

                _visibleTags.Add(tag);
            }
        }

        private void CollectOnScreen(List<ScreenTag> result)
        {
            var camera = Camera.Main;

            var radar = Radar;
            float generalRangeSquared = Settings.ENTITY_SEARCH_RADIUS * Settings.ENTITY_SEARCH_RADIUS;
            float radarRangeSquared = radar != null ? radar.Range * radar.Range : -1f;

            foreach (var tag in _visibleTags)
            {
                var screenPos = camera.WorldToScreenPoint(tag.WorldPosition);

                if (screenPos == Vector2.Zero) continue;
                if (!IsOnScreen(screenPos)) continue;

                var distanceSquared = Vector3.DistanceSquared(camera.PositionWorld, tag.WorldPosition);

                float maxDistanceSquared = tag.Category == Tag.TagCategory.Entity ? radarRangeSquared : generalRangeSquared;
                if (distanceSquared > maxDistanceSquared)
                {
                    continue;
                }

                result.Add(new ScreenTag(tag, screenPos, distanceSquared));
            }
        }

        private bool IsOnScreen(Vector2 screenPos)
        {
            return screenPos.X > 0 && screenPos.X <= _screenSize.X &&
                   screenPos.Y > 0 && screenPos.Y <= _screenSize.Y;
        }

        private void OnResized(Vector2 screenSize)
        {
            _screenSize = screenSize;
        }

        public List<Tag> GetStaticTags() => _activeTags.Where(tag => tag.IsStatic).ToList();

        public void CollectStaticTags(List<Tag> result)
        {
            result.Clear();
            foreach (var tag in _activeTags)
            {
                if (tag.IsStatic) result.Add(tag);
            }
        }

        public int ActiveTagsCount => _activeTags.Count;
        public int PooledTagsCount => _tagPool?.AvailableObjects ?? 0;
        public int TotalTagsCount => _tagPool?.TotalObjects ?? 0;

    }
}
using Engine;
using Engine.Components;
using Engine.Utils;
using ImGuiNET;
using OpenTK.Mathematics;
using System.Text.Json;

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
            screenSize = SpaceboxWindow.Instance.ClientSize;
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

            SpaceboxWindow.OnResized += OnResized;
            OnResized(SpaceboxWindow.Instance.ClientSize);
        }
        public override void OnDetached()
        {
            base.OnDetached();
            ClearTags();
            SpaceboxWindow.OnResized -= OnResized;
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

        public List<Tag> GetStaticTags()
        {
            return _tags.Where(tag => tag.IsStatic).ToList();
        }

        public static bool LoadTags(string worldPath)
        {
            if (string.IsNullOrEmpty(worldPath) || TagManager.Instance == null) {

                Debug.Error("[TagLoader] path is null or TagManager instance is null");
                return false;
            }
               
            string tagsFilePath = Path.Combine(worldPath, "tags.json");

            if (!File.Exists(tagsFilePath))
                return true;

            try
            {
               
                var tagsList = JsonFixer.LoadJsonSafe< List<TagJSON>>(tagsFilePath);
                if (tagsList == null || tagsList.Count == 0)
                    return true;

                int loadedCount = 0;
                foreach (var tagJson in tagsList)
                {
                    if (string.IsNullOrEmpty(tagJson.Text))
                        continue;

                    try
                    {
                        var tag = tagJson.CreateTag();
                        TagManager.Instance.RegisterTag(tag);
                        loadedCount++;
                      
                    }
                    catch (Exception ex)
                    {
                        Debug.Error($"[TagLoader] Failed to create tag '{tagJson.Text}': {ex.Message}");
                    }
                }

                Debug.Log($"[TagLoader] Loaded {loadedCount} tags from {tagsFilePath}");
                return true;
            }
            catch (JsonException ex)
            {
                Debug.Error($"[TagLoader] Invalid JSON in tags file: {ex.Message}");
                return false;
            }
            catch (IOException ex)
            {
                Debug.Error($"[TagLoader] IO error reading tags file: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.Error($"[TagLoader] Unexpected error loading tags: {ex.Message}");
                return false;
            }
        }

        public static bool SaveTags(string worldPath)
        {
            if (string.IsNullOrEmpty(worldPath) || TagManager.Instance == null)
            {
                Debug.Error("[TagLoader] path is null or TagManager instance is null");
                return false;
            }

            try
            {
                Directory.CreateDirectory(worldPath);

                var staticTags = TagManager.Instance.GetStaticTags();
                if (staticTags.Count == 0)
                {
                    string tagsFilePath = Path.Combine(worldPath, "tags.json");
                    if (File.Exists(tagsFilePath))
                        File.Delete(tagsFilePath);
                    return true;
                }

                var tagsJsonList = new List<TagJSON>();
                foreach (var tag in staticTags)
                {
                    if (tag.IsStatic && !string.IsNullOrEmpty(tag.Text))
                    {
                        tagsJsonList.Add(new TagJSON(tag));
                    }
                }

                if (tagsJsonList.Count == 0)
                    return true;

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                };

                string json = JsonSerializer.Serialize(tagsJsonList, options);
                string filePath = Path.Combine(worldPath, "tags.json");

                File.WriteAllText(filePath, json);

                Debug.Log($"[TagLoader] Saved {tagsJsonList.Count} static tags to {filePath}");
                return true;
            }
            catch (IOException ex)
            {
                Debug.Error($"[TagLoader] IO error saving tags: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.Error($"[TagLoader] Unexpected error saving tags: {ex.Message}");
                return false;
            }
        }
    }
}

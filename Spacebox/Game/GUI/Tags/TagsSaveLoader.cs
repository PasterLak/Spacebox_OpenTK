using Spacebox.Game.Generation.Tools.SaveData;
using Spacebox.Game.Versioning;
﻿
using Engine;
using Engine.Utils;
using System.Text.Json;

namespace Spacebox.Game.GUI
{
    public class TagsSaveLoader
    {
        public static bool LoadTags(string worldPath)
        {
            if (string.IsNullOrEmpty(worldPath) || GameScene.Current?.Tags == null)
            {

                Debug.Error("[TagLoader] path is null or TagManager instance is null");
                return false;
            }

            string tagsFilePath = Path.Combine(worldPath, WorldFileLayout.TagsFile);
            string legacyTagsFilePath = Path.Combine(worldPath, WorldFileLayout.LegacyTagsFile);

            if (!SaveDataFile.Exists(tagsFilePath, legacyTagsFilePath))
                return true;

            try
            {

                var tagsList = SaveDataFile.Read<List<TagJSON>>(tagsFilePath, legacyTagsFilePath, new JsonSerializerOptions { AllowTrailingCommas = true });
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
            if (string.IsNullOrEmpty(worldPath) || GameScene.Current?.Tags == null)
            {
                Debug.Error("[TagLoader] path is null or TagManager instance is null");
                return false;
            }

            try
            {
                Directory.CreateDirectory(worldPath);

                var staticTags = GameScene.Current.Tags.GetStaticTags();
                if (staticTags.Count == 0)
                {
                    SaveDataFile.Delete(Path.Combine(worldPath, WorldFileLayout.TagsFile), Path.Combine(worldPath, WorldFileLayout.LegacyTagsFile));
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

                string filePath = Path.Combine(worldPath, WorldFileLayout.TagsFile);
                SaveDataFile.Write(filePath, tagsJsonList, Path.Combine(worldPath, WorldFileLayout.LegacyTagsFile));

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

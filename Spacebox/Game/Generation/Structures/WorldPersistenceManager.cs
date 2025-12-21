using System.Collections.Concurrent;
using SharpNBT;
using Spacebox.Game.Generation.Tools;
using Engine;
using Engine.Multithreading;
using OpenTK.Mathematics;
using Spacebox.Game.GameMath;

namespace Spacebox.Game.Generation
{
    public static class WorldPersistenceManager
    {
        private struct CachedEntityData
        {
            public CompoundTag Tag;
            public Vector3i SectorIndex;
            public string Name;
        }

        private static ConcurrentDictionary<long, CachedEntityData> _modifiedEntitiesCache = new();

        public static void CacheUnloadedEntity(SpaceEntity entity)
        {
            if (!entity.IsModified) return;

            var tag = NBTHelper.SpaceEntityToTag(entity);
            var sectorIndex = SpaceMath.Sector.GetSectorIndex(entity.PositionWorld);

            var data = new CachedEntityData
            {
                Tag = tag,
                SectorIndex = sectorIndex,
                Name = entity.Name
            };

            _modifiedEntitiesCache.AddOrUpdate(entity.EntityID, data, (key, oldValue) => data);
        }

        public static bool TryGetCachedEntity(long id, out CompoundTag tag)
        {
            if (_modifiedEntitiesCache.TryGetValue(id, out var data))
            {
                tag = data.Tag;
                return true;
            }
            tag = null;
            return false;
        }

        public static void SaveAllCacheToDiskAsync(string worldPath)
        {
            if (_modifiedEntitiesCache.IsEmpty) return;

            var entitiesToSave = _modifiedEntitiesCache.ToArray();
            _modifiedEntitiesCache.Clear();

            WorkerPoolManager.Enqueue(token =>
            {
                SaveEntitiesInternal(entitiesToSave, worldPath);
                Debug.Success($"[Persistence] Background save complete. Saved {entitiesToSave.Length} entities.");

            }, WorkerPoolManager.Priority.Low);
        }

        public static void SaveAllCacheToDiskNow(string worldPath)
        {
            if (_modifiedEntitiesCache.IsEmpty) return;

            var entitiesToSave = _modifiedEntitiesCache.ToArray();
            _modifiedEntitiesCache.Clear();

            Debug.Log($"[Persistence] Saving {entitiesToSave.Length} entities synchronously...");
            SaveEntitiesInternal(entitiesToSave, worldPath);
            Debug.Success("[Persistence] Synchronous save complete.");
        }

        private static void SaveEntitiesInternal(KeyValuePair<long, CachedEntityData>[] entities, string worldPath)
        {
            string sectorsRoot = Path.Combine(worldPath, "Sectors");
            if (!Directory.Exists(sectorsRoot))
            {
                Directory.CreateDirectory(sectorsRoot);
            }

            foreach (var kvp in entities)
            {
                long id = kvp.Key;
                CachedEntityData data = kvp.Value;

                string sectorFolderName = SpaceMath.Sector.IndexToFolderName(data.SectorIndex);
                string sectorPath = Path.Combine(sectorsRoot, sectorFolderName);

                try
                {
                    if (!Directory.Exists(sectorPath))
                    {
                        Directory.CreateDirectory(sectorPath);
                    }

                    string filePath = Path.Combine(sectorPath, data.Name + ".entity");
                    NbtFile.Write(filePath, data.Tag, FormatOptions.Java, CompressionType.GZip);
                }
                catch (Exception ex)
                {
                    Debug.Error($"[Persistence] Failed to save entity {id} to sector {sectorFolderName}: {ex.Message}");
                    // try again later
                    _modifiedEntitiesCache.TryAdd(id, data);
                }
            }
        }

        public static void Dispose()
        {
            _modifiedEntitiesCache.Clear();
        }
    }
}
using Engine.Utils;
using System.Collections.Concurrent;
using SharpNBT;
using Spacebox.Game.Generation.Tools;
using Engine;
using Engine.Multithreading;
using OpenTK.Mathematics;
using Spacebox.Game.GameMath;
using Spacebox.Game.Generation.Structures;

namespace Spacebox.Game.Generation
{
    public static class WorldPersistenceManager
    {
        private struct CachedEntityData
        {
            public CompoundTag Tag;
            public Vector3i SectorIndex;
            public Vector3 Local;
            public string Name;
        }

        public static EntityCatalog Catalog { get; private set; } = new EntityCatalog();

        private static ConcurrentDictionary<long, CachedEntityData> _modifiedEntitiesCache = new();

        private sealed class CachedSectorData
        {
            public CompoundTag Tag;
            public string[] Destroyed;
        }

        private static readonly ConcurrentDictionary<Vector3i, CachedSectorData> _sectorCache = new();

        public static void CacheUnloadedSector(Sector sector)
        {
            _sectorCache[sector.PositionIndex] = new CachedSectorData
            {
                Tag = NBTHelper.SectorOnlyToTag(sector),
                Destroyed = sector.EntitiesDestroyed.ToArray()
            };
        }

        public static bool TryPeekCachedSector(Vector3i index, out CompoundTag tag, out string[] destroyed)
        {
            tag = null;
            destroyed = Array.Empty<string>();
            if (!_sectorCache.TryGetValue(index, out var data)) return false;

            tag = data.Tag;
            destroyed = data.Destroyed;
            return true;
        }

        public static void ForgetCachedSector(Vector3i index)
        {
            _sectorCache.TryRemove(index, out _);
        }

        public static bool HasUnsavedData => !_modifiedEntitiesCache.IsEmpty || !_sectorCache.IsEmpty;

        public static void CacheUnloadedEntity(SpaceEntity entity)
        {
            if (!entity.IsModified) return;

            var tag = NBTHelper.SpaceEntityToTag(entity);
            var sectorIndex = entity.Sector?.PositionIndex ?? SpaceMath.Sector.GetSectorIndex(entity.PositionWorld);

            CacheEntityTag(entity.EntityID, tag, sectorIndex, entity.Name);
        }

        public static void CacheEntityTag(long id, CompoundTag tag, Vector3i sectorIndex, string name)
        {
            var data = new CachedEntityData
            {
                Tag = tag,
                SectorIndex = sectorIndex,
                Local = EntityTagLocation.TryReadLocal(tag, out var local) ? local : Vector3.Zero,
                Name = name
            };

            _modifiedEntitiesCache.AddOrUpdate(id, data, (key, oldValue) => data);
        }

        public static void ForgetEntity(long id)
        {
            _modifiedEntitiesCache.TryRemove(id, out _);
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

        public static bool TryGetCachedEntity(long id, Vector3i sectorIndex, out CompoundTag tag)
        {
            if (_modifiedEntitiesCache.TryGetValue(id, out var data) && data.SectorIndex == sectorIndex)
            {
                tag = data.Tag;
                return true;
            }
            tag = null;
            return false;
        }

        public static bool IsCachedInOtherSector(long id, Vector3i sectorIndex)
        {
            return _modifiedEntitiesCache.TryGetValue(id, out var data) && data.SectorIndex != sectorIndex;
        }

        public static void CollectCachedEntities(Vector3i sectorIndex, List<NotGeneratedEntity> output)
        {
            foreach (var pair in _modifiedEntitiesCache)
            {
                var data = pair.Value;
                if (data.SectorIndex != sectorIndex || data.Tag == null) continue;

                var x = data.Tag.Get<FloatTag>(NBTKey.ENTITY.local_x);
                var y = data.Tag.Get<FloatTag>(NBTKey.ENTITY.local_y);
                var z = data.Tag.Get<FloatTag>(NBTKey.ENTITY.local_z);
                if (x == null || y == null || z == null) continue;

                output.Add(new NotGeneratedEntity
                {
                    Id = pair.Key,
                    positionInSector = new Vector3(x.Value, y.Value, z.Value),
                    FileName = data.Name,
                    radiusBlocks = 32
                });
            }
        }

        public static void LoadCatalog(string worldPath)
        {
            Catalog = EntityCatalogFile.Load(worldPath, true);
            Debug.Log($"[Persistence] Entity index loaded: {Catalog.Count} saved entities");
        }

        public static void SaveCatalog(string worldPath)
        {
            try
            {
                EntityCatalogFile.Write(worldPath, Catalog);
            }
            catch (Exception ex)
            {
                Debug.Error($"[Persistence] Failed to save the entity index: {ex.Message}");
            }
        }

        public static bool TryLocate(long id, out EntityRecord record)
        {
            if (_modifiedEntitiesCache.TryGetValue(id, out var data))
            {
                record = new EntityRecord(id, data.Name, data.SectorIndex, data.Local);
                return true;
            }

            return Catalog.TryGet(id, out record);
        }

        public static void CollectStoredEntities(Vector3i sectorIndex, List<EntityRecord> output)
        {
            Catalog.CollectInSector(sectorIndex, output);
        }

        public static void WriteEntityFile(string worldPath, SpaceEntity entity, CompoundTag tag)
        {
            var sectorIndex = entity.Sector?.PositionIndex ?? SpaceMath.Sector.GetSectorIndex(entity.PositionWorld);
            var local = entity.PositionWorld - SpaceMath.Sector.GetSectorPosition(sectorIndex);
            WriteEntityFile(worldPath, new EntityRecord(entity.EntityID, entity.Name, sectorIndex, local), tag);
        }

        private static void WriteEntityFile(string worldPath, EntityRecord record, CompoundTag tag)
        {
            string folder = EntityFiles.FolderOf(worldPath);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string newPath = EntityFiles.PathOf(folder, record.Name);
            string oldPath = Catalog.TryGet(record.Id, out var old) && old.Name != record.Name ? EntityFiles.PathOf(folder, old.Name) : null;

            AtomicFile.Write(newPath, temp => NbtFile.Write(temp, tag, FormatOptions.Java, CompressionType.GZip));
            Catalog.Upsert(record);

            if (oldPath != null && !string.Equals(Path.GetFullPath(oldPath), Path.GetFullPath(newPath), StringComparison.OrdinalIgnoreCase) && File.Exists(oldPath))
                File.Delete(oldPath);
        }

        public static void DeleteEntityFile(string worldPath, string name)
        {
            string path = EntityFiles.PathInWorld(worldPath, name);
            if (File.Exists(path)) File.Delete(path);
            Catalog.RemoveByName(EntityFiles.NameOf(name));
        }

        public static void SaveAllCacheToDiskAsync(string worldPath)
        {
            if (_modifiedEntitiesCache.IsEmpty) return;

            var entitiesToSave = _modifiedEntitiesCache.ToArray();
            _modifiedEntitiesCache.Clear();

            WorkerPoolManager.Enqueue(token =>
            {
                SaveEntitiesInternal(entitiesToSave, worldPath);
                SaveCatalog(worldPath);
                Debug.Success($"[Persistence] Background save complete. Saved {entitiesToSave.Length} entities.");

            }, WorkerPoolManager.Priority.Low);
        }

        public static void SaveAllCacheToDiskNow(string worldPath)
        {
            SaveCachedSectors(worldPath);
            if (_modifiedEntitiesCache.IsEmpty) return;

            var entitiesToSave = _modifiedEntitiesCache.ToArray();
            _modifiedEntitiesCache.Clear();

            Debug.Log($"[Persistence] Saving {entitiesToSave.Length} entities synchronously...");
            SaveEntitiesInternal(entitiesToSave, worldPath);
            Debug.Success("[Persistence] Synchronous save complete.");
        }

        private static void SaveEntitiesInternal(KeyValuePair<long, CachedEntityData>[] entities, string worldPath)
        {
            foreach (var kvp in entities)
            {
                long id = kvp.Key;
                CachedEntityData data = kvp.Value;

                try
                {
                    WriteEntityFile(worldPath, new EntityRecord(id, data.Name, data.SectorIndex, data.Local), data.Tag);
                }
                catch (Exception ex)
                {
                    Debug.Error($"[Persistence] Failed to save entity {id}: {ex.Message}");
                    // try again later
                    _modifiedEntitiesCache.TryAdd(id, data);
                }
            }
        }

        private static void SaveCachedSectors(string worldPath)
        {
            foreach (var pair in _sectorCache.ToArray())
            {
                try
                {
                    WorldSaveLoad.WriteSectorData(worldPath, pair.Key, pair.Value.Tag, pair.Value.Destroyed);
                    _sectorCache.TryRemove(pair.Key, out _);
                }
                catch (Exception ex)
                {
                    Debug.Error($"[Persistence] Failed to save sector {pair.Key}: {ex.Message}");
                }
            }
        }

        public static void Dispose()
        {
            _modifiedEntitiesCache.Clear();
            _sectorCache.Clear();
            Catalog = new EntityCatalog();
        }
    }
}
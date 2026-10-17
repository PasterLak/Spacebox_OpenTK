using Engine;
using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Game.Generation.Structures;


namespace Spacebox.Game.Generation.Tools
{

    public class WorldSaveLoad
    {
        // Worlds ->  Sectors   ->  Sector+3-7+2    ->    e42.entity -> chunks data
        //            world.json
        //            player.json


        public static void SaveWorld(string worldPath, Dictionary<Vector3i, Sector> loadedSectors)
        {
            if (!Validate(worldPath))
            {
                Debug.Error("[WorldSaveLoad] SaveWorld validation failed! Path: " + worldPath);
                return;
            }

            foreach (var sector in loadedSectors.Values)
            {
                if (!sector.IsModified) continue;

                SaveSector(sector, Path.Combine(worldPath, "Sectors"));
            }


        }

        public static string GetSectorFolderPath(string worldPath, Vector3i sectorIndex)
        {
            string folderName = Sector.IndexToFolderName(sectorIndex);
            string sectorsPath = Path.Combine(worldPath, "Sectors");
            return Path.Combine(sectorsPath, folderName);
        }

        public static bool CanLoadSectorHere(Vector3i sectorIndex, out string sectorFolderPath)
        {
            if (World.WorldData == null)
            {
                Debug.Error("[WorldSaveLoad] NULL World.Data in CanLoadSectorHere");
            }
            sectorFolderPath = GetSectorFolderPath(World.WorldData.WorldFolderPath, sectorIndex);

            return Directory.Exists(sectorFolderPath);

        }

        public static void LoadSectorData(Sector sector)
        {
            string folderPath = GetSectorFolderPath(World.WorldData.WorldFolderPath, sector.PositionIndex);
            string filePath = Path.Combine(folderPath, sector.ToFolderName() + ".sector");

            if (TryLoadSectorDataFile(filePath, out var tag))
            {
                NBTHelper.TagToSectorOnly(tag, sector);
            }
        }

        public static bool IsThereSectorFileHere(Vector3i sectorIndex)
        {
            return CanLoadSectorHere(sectorIndex, out var filePath);
        }

        private static bool TryLoadSectorDataFile(string filePath, out CompoundTag tag)
        {
            tag = null;
            if (File.Exists(filePath))
            {
                tag = NbtFile.Read(filePath, FormatOptions.Java, CompressionType.GZip);

                if (tag == null) { return false; }

                return true;
            }

            return false;
        }
     

        public static SpaceEntity? LoadSpaceEntityFromFile(string entityFilePath, Sector sector)
        {
            if (File.Exists(entityFilePath))
            {
                CompoundTag tag = NbtFile.Read(entityFilePath, FormatOptions.Java, CompressionType.GZip);

                return NBTHelper.TagToSpaceEntity(tag, sector);
            }

            return null;
        }
        public static CompoundTag? LoadSpaceEntityTagFromFile(string entityFilePath)
        {
            if (File.Exists(entityFilePath))
            {
                CompoundTag tag = NbtFile.Read(entityFilePath, FormatOptions.Java, CompressionType.GZip);

                return tag;
            }

            return null;
        }


        private static void SaveSector(Sector sector, string sectorsPath)
        {
            string sectorFolderName = sector.ToFolderName();
            string sectorFolderPath = Path.Combine(sectorsPath, sectorFolderName);
            string filePath = Path.Combine(sectorFolderPath, sectorFolderName + ".entity");
            var entities = sector.Entities;

            if (!Directory.Exists(sectorsPath))
            {
                Directory.CreateDirectory(sectorsPath);
            }

            if (!Directory.Exists(sectorFolderPath))
            {
                Directory.CreateDirectory(sectorFolderPath);
            }

            foreach (var deleted in sector.EntitiesDestroyed)
            {
                string deletedEntityPath = Path.Combine(sectorFolderPath, deleted + ".entity");
                if (File.Exists(deletedEntityPath))
                {
                    File.Delete(deletedEntityPath);

                }
            }

            for (int i = 0; i < entities.Count; i++)
            {
                if (!entities[i].IsModified) continue;

                var entity = entities[i];
                var entityTag = NBTHelper.SpaceEntityToTag(entity);

                Debug.Success("[WorldSaveLoad] Entity was saved: " + entities[i].EntityID + "  pos: " + entities[i].PositionWorld);
                //NbtFile.WriteAsync(Path.Combine(sectorFolderPath, entity.Name + ".entity"), entityTag, FormatOptions.Java, CompressionType.GZip);
                NbtFile.Write(Path.Combine(sectorFolderPath, entity.Name + ".entity"), entityTag, FormatOptions.Java, CompressionType.GZip);
            }

            //NbtFile.WriteAsync(Path.Combine(sectorFolderPath, sectorFolderName + ".sector"), NBTHelper.SectorOnlyToTag(sector), FormatOptions.Java, CompressionType.GZip);
            NbtFile.Write(Path.Combine(sectorFolderPath, sectorFolderName + ".sector"), NBTHelper.SectorOnlyToTag(sector), FormatOptions.Java, CompressionType.GZip);

        }

        public static List<NotGeneratedEntity> ScanAllEntities(Vector3i sectorIndex)
        {
            var list = new List<NotGeneratedEntity>();
            if (!CanLoadSectorHere(sectorIndex, out var folderPath)) return list;

            var files = Directory.GetFiles(folderPath, "*.entity");
            foreach (var file in files)
            {
                try
                {

                    var tag = NbtFile.Read(file, FormatOptions.Java, CompressionType.GZip);

                    if (tag == null) continue;

                    long id = tag.Get<LongTag>(NBTKey.ENTITY.id).Value;

                    float x = tag.Get<FloatTag>(NBTKey.ENTITY.local_x).Value;
                    float y = tag.Get<FloatTag>(NBTKey.ENTITY.local_y).Value;
                    float z = tag.Get<FloatTag>(NBTKey.ENTITY.local_z).Value;

                    string fileNameNoExt = Path.GetFileNameWithoutExtension(file);

                    var meta = new NotGeneratedEntity
                    {
                        Id = id,
                        positionInSector = new Vector3(x, y, z),
                        FileName = fileNameNoExt,

                        radiusBlocks = 32
                    };
                    list.Add(meta);
                }
                catch { }
            }
            return list;
        }

        private static bool Validate(string path)
        {

            var sector = World.CurrentSector;

            if (sector == null) return false;

            if (!Path.Exists(path)) return false;

            return true;
        }
    }
}

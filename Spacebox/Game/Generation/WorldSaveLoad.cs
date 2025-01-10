

using OpenTK.Mathematics;
using SharpNBT;
using Spacebox.Common;
using Spacebox.Game.GUI;
using System.Security.Cryptography.X509Certificates;

namespace Spacebox.Game.Generation
{

    public interface ISaveable
    {
        void Save(string data, string filePath);
    }

    public interface ILoadable
    {
        string Load(string filePath);
    }
    public class WorldSaveLoad
    {
        // Worlds ->  Sectors   ->  Sector+3-7+2    ->    e42.entity -> chunks data
        //            world.json
        //            player.json

        public static void SaveWorld(string worldPath)
        {
            if (!Validate(worldPath)) return;

            SaveSector(World.CurrentSector, Path.Combine(worldPath, "Sectors"));

        }

        public static string GetSectorFolderPath(string worldPath, Vector3i sectorIndex)
        {
            string folderName = Sector.IndexToFolderName(sectorIndex);
            string sectorsPath = Path.Combine(worldPath, "Sectors");
            return Path.Combine(sectorsPath, folderName);
        }

        public static bool CanLoadSectorHere(Vector3i sectorIndex, out string sectorFolderPath)
        {
            sectorFolderPath = GetSectorFolderPath(World.Data.WorldFolderPath, sectorIndex);
            Debug.Success("Sector folder path:" + sectorFolderPath);
            return Directory.Exists(sectorFolderPath);

        }

        public static bool IsThereSectorFileHere(Vector3i sectorIndex)
        {
            return CanLoadSectorHere(sectorIndex, out var filePath);
        }

        public static bool TryLoadSectorDataFile(string filePath, out CompoundTag tag)
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

        public static SpaceEntity[] LoadSpaceEntities(Sector sector)
        {
            string sectorFolderPath = GetSectorFolderPath(World.Data.WorldFolderPath, sector.PositionIndex);

            return LoadSpaceEntities(sectorFolderPath, sector);
        }

        public static SpaceEntity[] LoadSpaceEntities(string sectorFolderPath, Sector sector)
        {

            List<SpaceEntity> spaceEntities = new List<SpaceEntity>();

            if (Directory.Exists(sectorFolderPath))
            {
                var files = Directory.GetFiles(sectorFolderPath);

                foreach (var file in files)
                {
                    if (Path.GetExtension(file) != ".entity") continue;

                    var e = LoadSpaceEntityFromFile(file, sector);

                    var fileName = Path.GetFileNameWithoutExtension(file);

                    if (e.Name != fileName)
                    {
                        Debug.Warning("[WorldSaveLoad] Entity name was changed because file name was changed: " + e.Name + " to " + fileName);
                        e.Name = fileName;
                        
                    }

                    if (e != null)
                    {
                        spaceEntities.Add(e);
                        Debug.Success("[WorldSaveLoad] Entity found: " + e.Name);
                    }
                    else
                    {
                        Debug.Error("[WorldSaveLoad] Entity loaded from file was null! File path: : " + file);
                    }

                }
            }
            else
            {
                Debug.Error("[WorldSaveLoad] Wrong sector folder path: " + sectorFolderPath);
            }

            return spaceEntities.ToArray();
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

            for (int i = 0; i < entities.Count; i++)
            {
                if (!entities[i].IsModified) continue;

                var entity = entities[i];
                var entityTag = NBTHelper.SpaceEntityToTag(entity);

                Debug.Success("Entity was saved: " + entities[i].Name  + "  pos: "+ entities[i].PositionWorld);
                NbtFile.WriteAsync(Path.Combine(sectorFolderPath, entity.Name + ".entity"), entityTag, FormatOptions.Java, CompressionType.GZip);
            }

            NbtFile.WriteAsync(Path.Combine(sectorFolderPath, sectorFolderName + ".sector"), NBTHelper.SectorOnlyToTag(sector), FormatOptions.Java, CompressionType.GZip);

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
